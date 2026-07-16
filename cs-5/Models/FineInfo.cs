namespace LibrarySys.Models;

/// <summary>
/// 罚款信息实体类（对应 T_Fine 表）
/// </summary>
public class FineInfo
{
    /// <summary>罚款编号（自增主键）</summary>
    public int FineID { get; set; }

    /// <summary>读者编号（外键）</summary>
    public string ReaderID { get; set; }

    /// <summary>图书编号（外键）</summary>
    public string BookID { get; set; }

    /// <summary>借阅编号（外键，唯一约束：同一条借阅记录最多一条罚款）</summary>
    public int BorrowID { get; set; }

    /// <summary>逾期天数</summary>
    public int OverdueDays { get; set; }

    /// <summary>罚款金额（逾期天数 × 0.5 元）</summary>
    public decimal FineAmount { get; set; }

    /// <summary>罚款状态：未缴 / 已缴</summary>
    public string FineStatus { get; set; }

    /// <summary>生成日期</summary>
    public DateTime CreateDate { get; set; }

    /// <summary>缴费日期（未缴费时为 null）</summary>
    public DateTime? PayDate { get; set; }

    // ===== 以下字段为联表查询时填充，非 T_Fine 表原生字段 =====

    /// <summary>读者姓名（联表 T_Reader）</summary>
    public string ReaderName { get; set; }

    /// <summary>书名（联表 T_Book）</summary>
    public string BookName { get; set; }
}
