namespace LibraryManagement.Models;

/// <summary>
/// 图书类别实体类，对应 tbl_BookCategory 表
/// </summary>
public class BookCategory
{
    /// <summary>类别编号（主键）</summary>
    public string CategoryID { get; set; }

    /// <summary>类别名称</summary>
    public string CategoryName { get; set; }
}
