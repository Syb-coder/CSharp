namespace CampusStore.Models;

/// <summary>
/// 商品实体类
/// </summary>
/// <remarks>
/// 对应数据库表 tbl_Product 及视图 v_Product_Detail。
/// displayNo 为数据库 PERSISTED 计算列，格式 SP00001。
/// </remarks>
public class ProductInfo
{
    /// <summary>商品编号（主键，自增，内部使用）</summary>
    public int ProductID { get; set; }

    /// <summary>可读编号（计算列，格式 SP00001）</summary>
    public string DisplayNo { get; set; } = string.Empty;

    /// <summary>商品名称</summary>
    public string ProductName { get; set; } = string.Empty;

    /// <summary>类别编号（外键）</summary>
    public int CategoryID { get; set; }

    /// <summary>单价</summary>
    public decimal UnitPrice { get; set; }

    /// <summary>产地</summary>
    public string Origin { get; set; } = string.Empty;

    /// <summary>生产日期</summary>
    public DateTime? ProduceDate { get; set; }

    /// <summary>库存数量</summary>
    public int StockQuantity { get; set; }

    /// <summary>供货商编号（外键）</summary>
    public int SupplierID { get; set; }

    /* 以下字段来自视图 v_Product_Detail 的 JOIN 结果，仅用于列表展示 */

    /// <summary>类别名称（来自视图 JOIN，仅查询时填充）</summary>
    public string CategoryName { get; set; } = string.Empty;

    /// <summary>供货商名称（来自视图 JOIN，仅查询时填充）</summary>
    public string SupplierName { get; set; } = string.Empty;
}
