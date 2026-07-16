namespace LibrarySys.Models;

/// <summary>
/// 读者信息实体类（对应 T_Reader 表）
/// </summary>
public class ReaderInfo
{
    /// <summary>读者编号（主键）</summary>
    public string ReaderID { get; set; }

    /// <summary>姓名</summary>
    public string ReaderName { get; set; }

    /// <summary>性别：男 / 女</summary>
    public string ReaderSex { get; set; }

    /// <summary>联系电话</summary>
    public string Phone { get; set; }

    /// <summary>所在院系</summary>
    public string Department { get; set; }

    /// <summary>注册日期</summary>
    public DateTime? RegisterDate { get; set; }
}
