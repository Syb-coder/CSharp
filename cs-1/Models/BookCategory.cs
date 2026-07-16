namespace LibraryManagement.Models;

/// <summary>
/// 图书类别实体类，对应 tbl_BookCategory 表
/// </summary>
// 独立建表而非在 Book 表中直接存类别名称，是为了消除类别名称冗余、保证引用一致性
// 删除或重命名类别时只需操作本表，无需逐条更新图书记录
public class BookCategory
{
    /// <summary>类别编号（主键）</summary>
    public string CategoryID { get; set; }

    /// <summary>类别名称</summary>
    public string CategoryName { get; set; }

    /// <summary>可借阅天数</summary>
    public int BorrowDays { get; set; }

    /// <summary>单日逾期罚款标准（元/天）</summary>
    public decimal FinePerDay { get; set; }
}
