namespace LibraryManagement.Models;

/// <summary>
/// 图书信息实体类，对应 tbl_Book 表
/// </summary>
public class Book
{
    /// <summary>图书编号（主键）</summary>
    public string BookID { get; set; }

    /// <summary>书名</summary>
    public string BookName { get; set; }

    /// <summary>作者</summary>
    public string Author { get; set; }

    /// <summary>出版社</summary>
    public string Publisher { get; set; }

    // 设为 nullable：部分捐赠或古籍类图书可能无法确认出版日期，数据库中允许为 NULL
    /// <summary>出版日期</summary>
    public DateTime? PublishDate { get; set; }

    // 设为 nullable：ISBN 标准始于 1970 年，早期图书及非正式出版物可能没有 ISBN
    /// <summary>ISBN 号</summary>
    public string ISBN { get; set; }

    // 设为 nullable：赠书、捐赠书籍无采购价格，强制填写会导致录入流程受阻
    /// <summary>价格</summary>
    public decimal? Price { get; set; }

    /// <summary>类别编号（外键）</summary>
    public string CategoryID { get; set; }

    /// <summary>馆藏数量</summary>
    public int TotalCount { get; set; }

    // 该字段不由界面直接编辑，而是在借书/还书事务中由业务逻辑增减，避免人工修改导致库存数据不一致
    /// <summary>可借数量（由借还逻辑维护，不直接修改）</summary>
    public int AvailableCount { get; set; }

    // 非数据库字段：该值通过 JOIN tbl_BookCategory 查询填充，仅用于界面展示，避免界面层再次发起单独查询
    /// <summary>类别名称（联表查询时填充，非数据库字段）</summary>
    public string CategoryName { get; set; }
}
