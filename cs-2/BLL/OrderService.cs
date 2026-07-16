using CampusShop.Common;
using CampusShop.DAL;
using CampusShop.Models;
using Microsoft.Data.SqlClient;

namespace CampusShop.BLL;

/// <summary>
/// 订单业务服务类
/// 核心逻辑：下单操作在同一事务中插入订单主表 + 明细表 + 扣减库存，保证原子性
/// </summary>
public class OrderService
{
    private readonly OrderDAL _orderDAL = new();
    private readonly ProductDAL _productDAL = new();

    /// <summary>
    /// 查询全部订单
    /// </summary>
    /// <returns>订单列表</returns>
    public List<Order> GetAllOrders()
    {
        return _orderDAL.GetAllOrders();
    }

    /// <summary>
    /// 按多条件检索订单
    /// </summary>
    /// <param name="receiverName">收货人名称关键字</param>
    /// <param name="paymentStatus">支付状态</param>
    /// <param name="startDate">开始日期</param>
    /// <param name="endDate">结束日期</param>
    /// <returns>订单列表</returns>
    public List<Order> SearchOrders(string receiverName, string paymentStatus, DateTime? startDate, DateTime? endDate)
    {
        return _orderDAL.SearchOrders(receiverName, paymentStatus, startDate, endDate);
    }

    /// <summary>
    /// 执行下单操作
    /// 事务流程：校验商品与库存 → 开启事务 → 插入订单主表 → 逐条插入明细 + 扣减库存 → 提交事务
    /// </summary>
    /// <param name="order">订单实体（含明细列表）</param>
    /// <exception cref="BusinessException">校验失败</exception>
    public void CreateOrder(Order order)
    {
        // ===== 前置校验 =====
        if (order.Items == null || order.Items.Count == 0)
        {
            throw new BusinessException("订单明细不能为空，请至少添加一件商品");
        }
        if (ValidationHelper.IsExceedLength(order.ReceiverName, 20))
        {
            throw new BusinessException("收货人姓名长度不能超过20个字符");
        }
        if (ValidationHelper.IsExceedLength(order.ReceiverPhone, 15))
        {
            throw new BusinessException("收货人手机号长度不能超过15个字符");
        }
        if (ValidationHelper.IsExceedLength(order.ReceiverAddress, 200))
        {
            throw new BusinessException("收货地址长度不能超过200个字符");
        }
        if (ValidationHelper.IsExceedLength(order.PaymentMethod, 20))
        {
            throw new BusinessException("支付方式长度不能超过20个字符");
        }

        // 校验每条明细的商品存在性和库存
        foreach (OrderItem item in order.Items)
        {
            Product product = _productDAL.GetProductByID(item.ProductID)
                ?? throw new BusinessException($"商品编号 {item.ProductID} 不存在");

            if (item.Quantity <= 0)
            {
                throw new BusinessException($"商品 {product.ProductName} 的购买数量必须大于0");
            }
            if (item.Quantity > product.StockQuantity)
            {
                throw new BusinessException($"商品 {product.ProductName} 库存不足，当前库存仅剩 {product.StockQuantity} 件");
            }

            // 填充快照字段：商品名称和单价在下单时从商品表读取并固化
            item.ProductName = product.ProductName;
            item.UnitPrice = product.UnitPrice;
            item.TotalPrice = item.Quantity * product.UnitPrice;
        }

        // 计算订单总金额
        order.TotalAmount = order.Items.Sum(i => i.TotalPrice);
        order.OrderDate = DateTime.Today;
        order.PaymentStatus = string.IsNullOrEmpty(order.PaymentStatus) ? "待支付" : order.PaymentStatus;
        // 待支付状态下 paymentTime 为 null
        if (order.PaymentStatus == "已支付")
        {
            order.PaymentTime = DateTime.Now;
        }

        // ===== 事务执行：保证订单主表 + 明细表 + 库存扣减的原子性 =====
        using SqlConnection conn = new(DBConnection.GetConnectionString());
        conn.Open();
        using SqlTransaction transaction = conn.BeginTransaction();
        try
        {
            // 步骤1：插入订单主表，获取新生成的 orderID
            int orderID = _orderDAL.InsertOrder(transaction, conn, order);

            // 步骤2：逐条插入明细并扣减库存
            foreach (OrderItem item in order.Items)
            {
                item.OrderID = orderID;
                _orderDAL.InsertOrderItem(transaction, conn, item);
                _productDAL.DeductStock(transaction, conn, item.ProductID, item.Quantity);
            }

            transaction.Commit();
        }
        catch (InvalidOperationException ex)
        {
            try { transaction.Rollback(); } catch { }
            throw new BusinessException(ex.Message);
        }
        catch
        {
            try { transaction.Rollback(); } catch { }
            throw;
        }
    }

    /// <summary>
    /// 确认支付：将订单状态从"待支付"更新为"已支付"
    /// </summary>
    /// <param name="orderID">订单编号</param>
    /// <exception cref="BusinessException">校验失败</exception>
    public void ConfirmPayment(int orderID)
    {
        List<Order> orders = _orderDAL.SearchOrders("", "", null, null);
        Order order = orders.FirstOrDefault(o => o.OrderID == orderID)
            ?? throw new BusinessException("订单不存在");

        if (order.PaymentStatus == "已支付")
        {
            throw new BusinessException("该订单已支付，无需重复操作");
        }

        _orderDAL.UpdatePaymentStatus(orderID, "已支付", DateTime.Now);
    }
}
