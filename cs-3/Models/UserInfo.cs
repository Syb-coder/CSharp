namespace CampusMart.Models;

/// <summary>
/// 系统用户实体类（对应 tbl_User 表）
/// </summary>
/// <remarks>
/// 与 cs-2 的 User 差异化命名。字段名严格对齐数据库列名。
/// </remarks>
public class UserInfo
{
    /// <summary>用户编号（自增主键）</summary>
    public int UserID { get; set; }

    /// <summary>登录用户名（唯一）</summary>
    public string LoginName { get; set; }

    /// <summary>密码（SHA-256 哈希值）</summary>
    public string Password { get; set; }

    /// <summary>真实姓名</summary>
    public string RealName { get; set; }

    /// <summary>角色：管理员 / 操作员</summary>
    public string Role { get; set; }
}
