namespace CampusStore.Models;

/// <summary>
/// 系统用户实体类
/// </summary>
/// <remarks>对应数据库表 tbl_User，包含管理员/店员两类角色</remarks>
public class UserInfo
{
    /// <summary>用户编号（主键，自增）</summary>
    public int UserID { get; set; }

    /// <summary>登录名（唯一）</summary>
    public string LoginName { get; set; } = string.Empty;

    /// <summary>密码（SHA-256 哈希值，64 位十六进制字符串）</summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>角色（管理员/店员）</summary>
    public string Role { get; set; } = string.Empty;
}
