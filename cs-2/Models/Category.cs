namespace CampusShop.Models;

/// <summary>
/// 商品类别实体类：对应 tbl_Category 表
/// 支持树状层级结构（通过 parentCategoryID 引用本表）
/// </summary>
public class Category
{
    /// <summary>类别编号（主键）</summary>
    public string CategoryID { get; set; }

    /// <summary>类别名称</summary>
    public string CategoryName { get; set; }

    /// <summary>父类别编号（外键，引用本表 categoryID；为空表示根类别）</summary>
    public string ParentCategoryID { get; set; }

    /// <summary>类别描述</summary>
    public string CategoryDesc { get; set; }

    /// <summary>添加时间</summary>
    public DateTime? AddTime { get; set; }

    /// <summary>父类别名称（仅查询时使用，非数据库字段）</summary>
    public string ParentCategoryName { get; set; }
}
