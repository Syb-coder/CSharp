namespace LibraryManagement.Models;

/// <summary>
/// 借阅信息实体类，对应 tbl_Borrow 表
/// </summary>
public class BorrowRecord
{
    /// <summary>借阅编号（主键，自增）</summary>
    public int BorrowID { get; set; }

    /// <summary>读者编号（外键）</summary>
    public string ReaderID { get; set; }

    /// <summary>图书编号（外键）</summary>
    public string BookID { get; set; }

    /// <summary>借出日期</summary>
    public DateTime BorrowDate { get; set; }

    /// <summary>应还日期</summary>
    public DateTime DueDate { get; set; }

    // 设为 nullable：读者尚未归还图书时该字段为 NULL，归还时才写入实际还书日期
    // 这是判断"借出/已还"状态的冗余依据之一，与 Status 字段互为校验
    /// <summary>归还日期（未归还时为 null）</summary>
    public DateTime? ReturnDate { get; set; }

    /// <summary>借阅状态：借出 / 已还</summary>
    public string Status { get; set; }

    // 以下为联表查询时填充的展示字段，非本表字段

    // 借阅列表界面需同时展示读者姓名和书名，若不使用联表填充则每行记录需额外查询两次
    // 通过 JOIN 一次性取出后填充到此字段，减少数据库往返次数
    /// <summary>读者姓名（联表查询填充）</summary>
    public string ReaderName { get; set; }

    /// <summary>书名（联表查询填充）</summary>
    public string BookName { get; set; }
}
