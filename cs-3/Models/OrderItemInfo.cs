namespace CampusMart.Models;

/// <summary>
/// 订单商品明细实体类（对应 tbl_OrderItem 表）
/// </summary>
/// <remarks>
/// 与 cs-2 的 OrderItem 差异化命名。
/// 字段差异：小计字段名为 subtotal（cs-2 为 totalPrice）。
/// 商品名称和单价为下单快照值，下单后不随商品信息修改而变化。
/// </remarks>
public class OrderItemInfo
{
    /// <summary>明细编号（自增主键）</summary>
    public int ItemID { get; set; }

    /// <summary>订单编号（外键）</summary>
    public int OrderID { get; set; }

    /// <summary>商品编号（外键）</summary>
    public int ProductID { get; set; }

    /// <summary>商品名称（下单时快照）</summary>
    public string ProductName { get; set; }

    /// <summary>单价（下单时快照）</summary>
    public decimal UnitPrice { get; set; }

    /// <summary>购买数量（大于 0）</summary>
    public int Quantity { get; set; }

    /// <summary>小计金额（单价 × 数量）</summary>
    public decimal Subtotal { get; set; }
}
