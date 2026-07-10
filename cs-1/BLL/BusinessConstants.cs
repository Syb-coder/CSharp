namespace LibraryManagement.BLL;

/// <summary>
/// 业务常量定义，集中管理魔法值
/// </summary>
public static class BusinessConstants
{
    /// <summary>默认借阅期限（天）</summary>
    public const int DEFAULT_BORROW_DAYS = 30;

    /// <summary>读者同时借阅图书上限</summary>
    public const int MAX_BORROW_LIMIT = 5;

    /// <summary>管理员角色标识</summary>
    public const string ROLE_ADMIN = "管理员";

    /// <summary>普通用户角色标识</summary>
    public const string ROLE_USER = "普通用户";

    /// <summary>借出状态</summary>
    public const string STATUS_BORROWED = "借出";

    /// <summary>已还状态</summary>
    public const string STATUS_RETURNED = "已还";
}
