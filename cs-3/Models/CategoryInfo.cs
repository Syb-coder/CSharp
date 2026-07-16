namespace CampusMart.Models;

/// <summary>
/// 商品类别实体类（对应 tbl_Category 表，扁平结构）
/// </summary>
/// <remarks>
/// 与 cs-2 的 Category 差异化命名。本系统类别为扁平结构，无父子层级。
/// </remarks>
public class CategoryInfo
{
    /// <summary>类别编号（自增主键）</summary>
    public int CategoryID { get; set; }

    /// <summary>类别名称（必填）</summary>
    public string CategoryName { get; set; }

    /// <summary>类别描述（选填）</summary>
    public string CategoryDesc { get; set; }

    /// <summary>添加时间（系统自动填充）</summary>
    public DateTime AddTime { get; set; }
}
