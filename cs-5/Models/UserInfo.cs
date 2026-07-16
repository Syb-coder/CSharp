namespace LibrarySys.Models;

/// <summary>
/// 系统用户信息实体类（对应 T_User 表）
/// </summary>
public class UserInfo
{
    /// <summary>用户名（主键）</summary>
    public string UserName { get; set; }

    /// <summary>用户密码（MD5 哈希值，32 位十六进制字符串）</summary>
    public string UserPassword { get; set; }

    /// <summary>权限：管理员 / 普通用户</summary>
    public string UserPurview { get; set; }
}
