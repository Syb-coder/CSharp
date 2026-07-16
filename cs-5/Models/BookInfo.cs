namespace LibrarySys.Models;

/// <summary>
/// 图书信息实体类（对应 T_Book 表）
/// </summary>
public class BookInfo
{
    /// <summary>图书编号（主键）</summary>
    public string BookID { get; set; }

    /// <summary>书名</summary>
    public string BookName { get; set; }

    /// <summary>作者</summary>
    public string Author { get; set; }

    /// <summary>出版社</summary>
    public string Publisher { get; set; }

    /// <summary>出版日期</summary>
    public DateTime? PublishDate { get; set; }

    /// <summary>ISBN 号</summary>
    public string ISBN { get; set; }

    /// <summary>价格</summary>
    public decimal? Price { get; set; }

    /// <summary>类型编号（外键，引用 T_BookType）</summary>
    public string TypeID { get; set; }

    /// <summary>馆藏数量</summary>
    public int TotalCount { get; set; }

    /// <summary>可借数量（冗余字段，借还书事务维护一致性）</summary>
    public int AvailableCount { get; set; }

    /// <summary>类型名称（联表查询时填充，非数据库字段）</summary>
    public string TypeName { get; set; }
}
