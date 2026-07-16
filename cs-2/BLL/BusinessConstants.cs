namespace CampusShop.BLL;

/// <summary>
/// 业务常量定义，集中管理魔法值
/// </summary>
// 魔法值集中管理的意义：将散落在各业务代码中的硬编码值（如"管理员"等）
// 统一收口到此处，修改业务规则只需改一处，避免遗漏导致的逻辑不一致
public static class BusinessConstants
{
    /// <summary>管理员角色标识</summary>
    public const string ROLE_ADMIN = "管理员";

    /// <summary>普通用户角色标识</summary>
    public const string ROLE_USER = "普通用户";
}
