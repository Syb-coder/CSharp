namespace CampusShop.Models;

/// <summary>
/// 订单实体类：对应 tbl_Order 表
/// 一笔订单可包含多个商品明细（tbl_OrderItem）
/// </summary>
public class Order
{
    /// <summary>订单编号（主键，自增）</summary>
    public int OrderID { get; set; }

    /// <summary>下单日期</summary>
    public DateTime OrderDate { get; set; }

    /// <summary>支付方式（现金/微信/支付宝等）</summary>
    public string PaymentMethod { get; set; }

    /// <summary>支付时间</summary>
    public DateTime? PaymentTime { get; set; }

    /// <summary>支付状态：待支付 / 已支付</summary>
    public string PaymentStatus { get; set; }

    /// <summary>收货人姓名</summary>
    public string ReceiverName { get; set; }

    /// <summary>收货人手机号</summary>
    public string ReceiverPhone { get; set; }

    /// <summary>收货地址</summary>
    public string ReceiverAddress { get; set; }

    /// <summary>订单总金额（所有明细总价之和）</summary>
    public decimal TotalAmount { get; set; }

    /// <summary>订单明细列表（仅查询时使用，非 tbl_Order 表字段）</summary>
    public List<OrderItem> Items { get; set; } = new();
}
