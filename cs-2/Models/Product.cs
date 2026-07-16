namespace CampusShop.Models;

/// <summary>
/// 商品信息实体类：对应 tbl_Product 表
/// </summary>
public class Product
{
    /// <summary>商品编号（主键）</summary>
    public string ProductID { get; set; }

    /// <summary>商品名称</summary>
    public string ProductName { get; set; }

    /// <summary>规格</summary>
    public string Specification { get; set; }

    /// <summary>单价</summary>
    public decimal UnitPrice { get; set; }

    /// <summary>库存数量（冗余字段，由售卖业务逻辑维护，不直接修改）</summary>
    public int StockQuantity { get; set; }

    /// <summary>类别编号（外键，引用 tbl_Category）</summary>
    public string CategoryID { get; set; }

    /// <summary>供货商编号（外键，引用 tbl_Supplier）</summary>
    public string SupplierID { get; set; }

    /// <summary>产地</summary>
    public string Origin { get; set; }

    /// <summary>生产日期</summary>
    public DateTime? ProductionDate { get; set; }

    /// <summary>类别名称（仅查询时使用，非数据库字段）</summary>
    public string CategoryName { get; set; }

    /// <summary>供货商名称（仅查询时使用，非数据库字段）</summary>
    public string SupplierName { get; set; }
}
