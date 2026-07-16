namespace HotelSys.Models;

/// <summary>
/// 系统用户信息实体类（对应 T_User 表，PRD 5.1.1）
/// </summary>
public class UserInfo
{
    /// <summary>用户名（主键）</summary>
    public string UserName { get; set; }

    /// <summary>密码（SHA-256 哈希值，64 位十六进制字符串）</summary>
    public string UserPassword { get; set; }

    /// <summary>权限：管理员 / 前台</summary>
    public string UserPurview { get; set; }

    /// <summary>真实姓名（可空）</summary>
    public string RealName { get; set; }
}
