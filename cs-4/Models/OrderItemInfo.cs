namespace CampusStore.Models;

/// <summary>
/// 订单商品明细实体类
/// </summary>
/// <remarks>
/// 对应数据库表 tbl_OrderItem。
/// amount 为 PERSISTED 计算列 = unitPrice × quantity，数据库自动维护。
/// productName 和 unitPrice 为下单时从商品表读取的快照值，后续商品信息修改不影响已存在订单。
/// </remarks>
public class OrderItemInfo
{
    /// <summary>明细编号（主键，自增）</summary>
    public int ItemID { get; set; }

    /// <summary>订单编号（外键）</summary>
    public int OrderID { get; set; }

    /// <summary>商品编号（外键）</summary>
    public int ProductID { get; set; }

    /// <summary>商品名称（快照值，下单时从商品表读取并固化）</summary>
    public string ProductName { get; set; } = string.Empty;

    /// <summary>单价（快照值，下单时从商品表读取并固化）</summary>
    public decimal UnitPrice { get; set; }

    /// <summary>购买数量</summary>
    public int Quantity { get; set; }

    /// <summary>小计金额（计算列 = unitPrice × quantity）</summary>
    public decimal Amount { get; set; }
}
