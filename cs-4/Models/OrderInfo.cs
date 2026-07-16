namespace CampusStore.Models;

/// <summary>
/// 订单主表实体类
/// </summary>
/// <remarks>
/// 对应数据库表 tbl_Order。
/// totalAmount 为"逻辑计算列"，由 tr_OrderItem_Insert/Delete 触发器维护，
/// 非 SQL Server 计算列公式实现（因跨表 SUM 无法用 PERSISTED 表达式实现）。
/// </remarks>
public class OrderInfo
{
    /// <summary>订单编号（主键，自增）</summary>
    public int OrderID { get; set; }

    /// <summary>下单日期（系统自动填充）</summary>
    public DateTime OrderDate { get; set; }

    /// <summary>支付方式（现金/微信/支付宝）</summary>
    public string PaymentMethod { get; set; } = string.Empty;

    /// <summary>结款时间（未结款时为 null）</summary>
    public DateTime? PaymentTime { get; set; }

    /// <summary>结款状态（未结款/已结款）</summary>
    public string PaymentStatus { get; set; } = string.Empty;

    /// <summary>收货人姓名</summary>
    public string ReceiverName { get; set; } = string.Empty;

    /// <summary>收货人手机号</summary>
    public string ReceiverPhone { get; set; } = string.Empty;

    /// <summary>收货地址</summary>
    public string ReceiverAddress { get; set; } = string.Empty;

    /// <summary>订单总金额（触发器维护）</summary>
    public decimal TotalAmount { get; set; }

    /// <summary>订单明细列表（查询订单明细时填充）</summary>
    public List<OrderItemInfo> Items { get; set; } = new();
}
