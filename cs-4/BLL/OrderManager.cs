using System.Data;
using CampusStore.Common;
using CampusStore.DAL;
using CampusStore.Models;
using Microsoft.Data.SqlClient;

namespace CampusStore.BLL;

/// <summary>
/// 订单业务逻辑类
/// </summary>
/// <remarks>
/// 与 cs-2/cs-3 差异化：
///   - cs-2/cs-3 在 BLL 层显式管理 SqlTransaction 并扣减库存
///   - cs-4 通过 sp_Order_Create 存储过程内部事务 + tr_OrderItem_Insert 触发器
///     完成订单+明细插入和库存扣减，BLL 层无需管理事务，职责更纯粹
/// </remarks>
public class OrderManager
{
    private readonly OrderRepository _repo = new();

    /// <summary>
    /// 创建订单
    /// </summary>
    /// <param name="order">订单信息（含明细列表）</param>
    /// <returns>新订单 ID</returns>
    /// <exception cref="BusinessException">购物车为空或触发器库存检查失败时抛出</exception>
    public int CreateOrder(OrderInfo order)
    {
        // 业务校验
        if (order == null)
            throw new BusinessException("订单信息不能为空");
        if (order.Items == null || order.Items.Count == 0)
            throw new BusinessException("购物车为空，请先添加商品");

        // 校验收货信息（选填字段，但填写时需符合格式）
        if (ValidateUtil.IsExceedLength(order.ReceiverName, 20))
            throw new BusinessException("收货人姓名长度不能超过 20 个字符");
        if (ValidateUtil.IsExceedLength(order.ReceiverPhone, 15))
            throw new BusinessException("收货人手机号长度不能超过 15 个字符");
        if (!ValidateUtil.IsValidPhone(order.ReceiverPhone))
            throw new BusinessException("收货人手机号格式不正确");
        if (ValidateUtil.IsExceedLength(order.ReceiverAddress, 200))
            throw new BusinessException("收货地址长度不能超过 200 个字符");

        // 校验明细数量必须大于 0
        foreach (var item in order.Items)
        {
            if (item.Quantity <= 0)
                throw new BusinessException($"商品 [{item.ProductName}] 购买数量必须大于 0");
        }

        // 默认未结款
        if (string.IsNullOrWhiteSpace(order.PaymentStatus))
            order.PaymentStatus = "未结款";

        try
        {
            // 调用存储过程：内部开启事务，逐条插入明细，
            // 由 tr_OrderItem_Insert 触发器自动扣减库存并检查库存不足
            return _repo.CreateOrder(order);
        }
        catch (SqlException ex) when (ex.Message.Contains("库存不足"))
        {
            // 触发器抛出的库存不足错误转为业务异常
            throw new BusinessException(ex.Message, ex);
        }
    }

    /// <summary>
    /// 确认结款
    /// </summary>
    /// <exception cref="BusinessException">订单已结款或不存在时抛出</exception>
    public int Confirm(int orderID)
    {
        if (orderID <= 0)
            throw new BusinessException("请选择要结款的订单");

        int result = _repo.Confirm(orderID);
        if (result == 0)
            throw new BusinessException("该订单已结款或不存在，无需重复操作");
        return result;
    }

    /// <summary>
    /// 查询全部订单
    /// </summary>
    public DataTable GetAll() => _repo.GetAll();

    /// <summary>
    /// 按收货人名称、结款状态组合条件检索
    /// </summary>
    public DataTable Search(string receiverName, string paymentStatus)
        => _repo.Search(receiverName, paymentStatus);

    /// <summary>
    /// 按订单编号查询订单详情（含明细列表）
    /// </summary>
    public OrderInfo GetByID(int orderID) => _repo.GetByID(orderID);

    /// <summary>
    /// 查询订单明细列表
    /// </summary>
    public List<OrderItemInfo> GetItems(int orderID) => _repo.GetItems(orderID);
}
