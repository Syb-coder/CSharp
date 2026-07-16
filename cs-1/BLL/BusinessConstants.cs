namespace LibraryManagement.BLL;

/// <summary>
/// 业务常量定义，集中管理魔法值
/// </summary>
// 魔法值集中管理的意义：将散落在各业务代码中的硬编码值（如"30"、"5"、"管理员"等）
// 统一收口到此处，修改业务规则只需改一处，避免遗漏导致的逻辑不一致
public static class BusinessConstants
{
    /// <summary>默认借阅期限（天）</summary>
    // 30 天是图书馆通行的借阅周期，兼顾读者的阅读时间和图书的周转效率
    public const int DEFAULT_BORROW_DAYS = 30;

    /// <summary>读者同时借阅图书上限</summary>
    // 5本上限防止个别读者囤积图书，保证图书资源的公平分配
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
