namespace LibraryManagement.Models;

/// <summary>
/// 系统用户实体类，对应 tbl_User 表
/// </summary>
public class User
{
    /// <summary>用户名（主键）</summary>
    public string UserName { get; set; }

    /// <summary>用户密码（SHA-256 哈希值）</summary>
    public string UserPassword { get; set; }

    /// <summary>权限：管理员 / 普通用户</summary>
    public string UserPurview { get; set; }
}
