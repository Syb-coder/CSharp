namespace LibrarySys.Models;

/// <summary>
/// 图书类型信息实体类（对应 T_BookType 表）
/// </summary>
public class BookTypeInfo
{
    /// <summary>类型编号（主键）</summary>
    public string TypeID { get; set; }

    /// <summary>类型名称</summary>
    public string TypeName { get; set; }
}
