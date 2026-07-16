namespace CampusMart.Common;

/// <summary>
/// 角色常量定义类
/// </summary>
/// <remarks>
/// 集中管理角色取值，避免在业务代码中硬编码字符串字面量。
/// 与 cs-2 的 BusinessConstants.ROLE_ADMIN 差异化命名。
/// </remarks>
public static class RoleConstants
{
    /// <summary>管理员角色：可操作全部功能</summary>
    public const string ADMIN = "管理员";

    /// <summary>操作员角色：仅可操作商品查询、供货商查询、订单管理、Excel 导出</summary>
    public const string OPERATOR = "操作员";
}

/// <summary>
/// 支付状态常量定义类
/// </summary>
/// <remarks>
/// 取值与数据库 CHECK 约束一致。与 cs-2 的"待支付/已支付"差异化（本系统为"未支付/已支付"）。
/// </remarks>
public static class PaymentConstants
{
    /// <summary>未支付：订单创建后的初始状态</summary>
    public const string UNPAID = "未支付";

    /// <summary>已支付：管理员确认收款后变更的状态</summary>
    public const string PAID = "已支付";
}

/// <summary>
/// 支付方式常量定义类
/// </summary>
public static class PaymentMethodConstants
{
    /// <summary>现金支付</summary>
    public const string CASH = "现金";

    /// <summary>微信支付</summary>
    public const string WECHAT = "微信";

    /// <summary>支付宝支付</summary>
    public const string ALIPAY = "支付宝";
}
