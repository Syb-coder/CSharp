using CampusMart.Common;
using CampusMart.DAL;
using CampusMart.Models;

namespace CampusMart.BLL;

/// <summary>
/// 订单业务逻辑类
/// </summary>
/// <remarks>
/// 与 cs-2 的 OrderService 差异化命名（OrderBiz）。
/// 核心差异：事务在 OrderDao 内部管理（cs-2 在 BLL 管理事务），BLL 只负责业务校验和快照填充。
/// 支付状态取值为"未支付/已支付"（cs-2 为"待支付/已支付"）。
/// </remarks>
public class OrderBiz
{
    private readonly OrderDao _orderDao = new();
    private readonly GoodsDao _goodsDao = new();

    /// <summary>
    /// 查询全部订单（含操作员姓名）
    /// </summary>
    /// <returns>订单列表</returns>
    public List<OrderInfo> GetAllOrders()
    {
        return _orderDao.GetAllOrders();
    }

    /// <summary>
    /// 按多条件组合检索订单
    /// </summary>
    /// <param name="orderNo">订单号关键字（空则不限）</param>
    /// <param name="receiverName">收货人姓名关键字（空则不限）</param>
    /// <param name="paymentStatus">支付状态（空则不限）</param>
    /// <param name="dateFrom">下单日期下限（null 则不限）</param>
    /// <param name="dateTo">下单日期上限（null 则不限）</param>
    /// <returns>匹配的订单列表</returns>
    public List<OrderInfo> SearchOrders(string orderNo, string receiverName, string paymentStatus, DateTime? dateFrom, DateTime? dateTo)
    {
        return _orderDao.SearchOrders(orderNo, receiverName, paymentStatus, dateFrom, dateTo);
    }

    /// <summary>
    /// 查询指定订单的商品明细列表
    /// </summary>
    /// <param name="orderID">订单编号</param>
    /// <returns>订单明细列表</returns>
    public List<OrderItemInfo> GetOrderItems(int orderID)
    {
        return _orderDao.GetOrderItems(orderID);
    }

    /// <summary>
    /// 创建订单
    /// 业务流程：校验收货信息 → 校验每条明细商品存在性和库存 → 填充快照值 → 计算总金额 → 调用 DAO 事务提交
    /// </summary>
    /// <param name="order">订单头信息（orderNo/orderDate/paymentStatus 由本方法填充）</param>
    /// <param name="items">订单明细列表（productName/unitPrice/subtotal 由本方法填充快照值）</param>
    /// <exception cref="BusinessException">业务校验失败</exception>
    public void CreateOrder(OrderInfo order, List<OrderItemInfo> items)
    {
        // ===== 收货信息校验 =====
        if (ValidateUtil.IsNullOrWhiteSpace(order.ReceiverName))
        {
            throw new BusinessException("收货人姓名不能为空");
        }
        if (ValidateUtil.IsExceedLength(order.ReceiverName, 20))
        {
            throw new BusinessException("收货人姓名长度不能超过20个字符");
        }
        if (!ValidateUtil.IsValidMobilePhone(order.ReceiverPhone))
        {
            throw new BusinessException("请输入正确的11位手机号码");
        }
        if (ValidateUtil.IsExceedLength(order.ReceiverAddress, 200))
        {
            throw new BusinessException("收货地址长度不能超过200个字符");
        }
        if (ValidateUtil.IsNullOrWhiteSpace(order.PaymentMethod))
        {
            throw new BusinessException("请选择支付方式");
        }

        // ===== 订单明细校验 =====
        if (items == null || items.Count == 0)
        {
            throw new BusinessException("请至少添加一种商品后再提交订单");
        }

        // 逐条校验商品存在性和库存，同时填充快照值（商品名称、单价）
        // 快照策略：下单后商品信息修改不影响已生成的订单明细
        foreach (OrderItemInfo item in items)
        {
            GoodsInfo product = _goodsDao.GetGoodsByID(item.ProductID)
                ?? throw new BusinessException($"商品编号 {item.ProductID} 不存在");

            if (item.Quantity <= 0)
            {
                throw new BusinessException($"商品【{product.ProductName}】的购买数量必须大于0");
            }
            if (product.StockQty == 0)
            {
                throw new BusinessException($"商品【{product.ProductName}】已售罄，无法加入订单");
            }
            if (item.Quantity > product.StockQty)
            {
                throw new BusinessException($"商品【{product.ProductName}】库存不足，当前库存仅剩 {product.StockQty} 件");
            }

            // 填充快照值：商品名称和单价在下单时从商品表读取并固化
            item.ProductName = product.ProductName;
            item.UnitPrice = product.UnitPrice;
            item.Subtotal = item.UnitPrice * item.Quantity;
        }

        // ===== 订单头信息填充 =====
        order.TotalAmount = items.Sum(i => i.Subtotal);
        order.OrderDate = DateTime.Now;
        order.PaymentStatus = PaymentConstants.UNPAID; // 订单创建后默认"未支付"
        order.PaymentTime = null; // 未支付状态下支付时间为 null

        // ===== 调用 DAO 执行事务（插入订单头 + 明细 + 扣减库存）=====
        // 事务在 OrderDao.CreateOrder 内部管理，BLL 无需感知连接与事务
        try
        {
            _orderDao.CreateOrder(order, items);
        }
        catch (InvalidOperationException ex)
        {
            // DAL 层库存不足异常转换为业务异常，便于 UI 层统一处理
            throw new BusinessException(ex.Message);
        }
    }

    /// <summary>
    /// 确认支付：将订单状态从"未支付"更新为"已支付"，并记录支付时间
    /// </summary>
    /// <param name="orderID">订单编号</param>
    /// <exception cref="BusinessException">订单不存在或已支付</exception>
    public void ConfirmPayment(int orderID)
    {
        OrderInfo order = _orderDao.GetAllOrders().FirstOrDefault(o => o.OrderID == orderID)
            ?? throw new BusinessException("订单不存在");

        if (order.PaymentStatus == PaymentConstants.PAID)
        {
            throw new BusinessException("该订单已支付，无需重复操作");
        }

        _orderDao.UpdatePaymentStatus(orderID, PaymentConstants.PAID);
    }
}
