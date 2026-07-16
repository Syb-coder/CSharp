namespace CampusStore.Models;

/// <summary>
/// 商品类别实体类
/// </summary>
/// <remarks>
/// 对应数据库表 tbl_Category，采用扁平结构+排序号设计（非树状层级）。
/// </remarks>
public class CategoryInfo
{
    /// <summary>类别编号（主键，自增）</summary>
    public int CategoryID { get; set; }

    /// <summary>类别名称</summary>
    public string CategoryName { get; set; } = string.Empty;

    /// <summary>类别描述</summary>
    public string CategoryDesc { get; set; } = string.Empty;

    /// <summary>添加时间（系统自动填充）</summary>
    public DateTime AddTime { get; set; }

    /// <summary>排序号（用于列表显示顺序）</summary>
    public int SortOrder { get; set; }

    /// <summary>关联商品数量（来自视图 v_Category_List 统计列）</summary>
    public int ProductCount { get; set; }
}
