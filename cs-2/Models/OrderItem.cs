namespace CampusShop.Models;

/// <summary>
/// 订单商品明细实体类：对应 tbl_OrderItem 表
/// 一笔订单可包含多条商品明细
/// </summary>
public class OrderItem
{
    /// <summary>明细编号（主键，自增）</summary>
    public int ItemID { get; set; }

    /// <summary>订单编号（外键，引用 tbl_Order）</summary>
    public int OrderID { get; set; }

    /// <summary>商品编号（外键，引用 tbl_Product）</summary>
    public string ProductID { get; set; }

    /// <summary>商品名称（下单时的名称快照，后续修改商品名称不影响此值）</summary>
    public string ProductName { get; set; }

    /// <summary>单价（下单时的商品单价快照）</summary>
    public decimal UnitPrice { get; set; }

    /// <summary>购买数量</summary>
    public int Quantity { get; set; }

    /// <summary>总价（quantity × unitPrice）</summary>
    public decimal TotalPrice { get; set; }
}
