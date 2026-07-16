namespace CampusMart.Models;

/// <summary>
/// 商品信息实体类（对应 tbl_Product 表）
/// </summary>
/// <remarks>
/// 与 cs-2 的 Product 差异化命名（GoodsInfo）。
/// 字段差异：无 specification；字段名 stockQty（cs-2 为 stockQuantity）、produceDate（cs-2 为 productionDate）。
/// 扩展字段 CategoryName / SupplierName 用于联表查询结果展示，非数据库列。
/// </remarks>
public class GoodsInfo
{
    /// <summary>商品编号（自增主键）</summary>
    public int ProductID { get; set; }

    /// <summary>商品名称（必填）</summary>
    public string ProductName { get; set; }

    /// <summary>类别编号（外键）</summary>
    public int CategoryID { get; set; }

    /// <summary>单价（必填，大于 0）</summary>
    public decimal UnitPrice { get; set; }

    /// <summary>产地</summary>
    public string Origin { get; set; }

    /// <summary>生产日期</summary>
    public DateTime? ProduceDate { get; set; }

    /// <summary>库存数量（大于等于 0，由订单业务逻辑维护）</summary>
    public int StockQty { get; set; }

    /// <summary>供货商编号（外键）</summary>
    public int SupplierID { get; set; }

    // ===== 以下为联表查询扩展字段，非 tbl_Product 实际列 =====

    /// <summary>所属类别名称（联表 tbl_Category 获取，用于列表展示）</summary>
    public string CategoryName { get; set; }

    /// <summary>供货商名称（联表 tbl_Supplier 获取，用于列表展示）</summary>
    public string SupplierName { get; set; }
}
