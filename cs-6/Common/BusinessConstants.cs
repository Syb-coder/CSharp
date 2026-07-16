namespace HotelSys.Common;

/// <summary>
/// 业务常量定义类，集中管理全系统业务常量，避免魔法值（PRD 第8节）
/// </summary>
public static class BusinessConstants
{
    /// <summary>App.config 中数据库连接字符串键名</summary>
    public const string CONN_STR_KEY = "HotelDB";

    /// <summary>默认押金按1天房费收取（PRD 第8节）</summary>
    public const int DEFAULT_DEPOSIT_DAYS = 1;

    // ===== 角色权限 =====

    /// <summary>管理员权限标识</summary>
    public const string ROLE_ADMIN = "管理员";

    /// <summary>前台操作员权限标识（区别于 cs-5 的"普通用户"）</summary>
    public const string ROLE_USER = "前台";

    // ===== 预订状态（PRD 5.1.5） =====

    /// <summary>预订状态-待入住</summary>
    public const string RESERVE_WAITING = "待入住";

    /// <summary>预订状态-已入住</summary>
    public const string RESERVE_CHECKEDIN = "已入住";

    /// <summary>预订状态-已取消</summary>
    public const string RESERVE_CANCELLED = "已取消";

    /// <summary>预订状态-已过期</summary>
    public const string RESERVE_EXPIRED = "已过期";

    // ===== 入住状态（PRD 5.1.6） =====

    /// <summary>入住状态-在住</summary>
    public const string CHECKIN_OCCUPIED = "在住";

    /// <summary>入住状态-已结账</summary>
    public const string CHECKIN_CHECKEDOUT = "已结账";

    /// <summary>入住状态-已取消</summary>
    public const string CHECKIN_CANCELLED = "已取消";

    // ===== 操作日志类型（PRD 4.2.13） =====

    public const string LOG_LOGIN = "登录";
    public const string LOG_LOGOUT = "登出";
    public const string LOG_CHECKIN = "入住";
    public const string LOG_CHECKOUT = "退房";
    public const string LOG_EXTEND = "续住";
    public const string LOG_CHANGE_ROOM = "换房";
    public const string LOG_RESERVE = "预订";
    public const string LOG_CANCEL_RESERVE = "取消预订";
    public const string LOG_CONSUME = "消费";
    public const string LOG_SET_MAINTENANCE = "设维护";
    public const string LOG_RESTORE_FREE = "恢复空闲";
    public const string LOG_ADD = "新增";
    public const string LOG_UPDATE = "修改";
    public const string LOG_DELETE = "删除";

    // ===== 证件类型（PRD 4.2.6） =====

    public static readonly string[] ID_TYPES = { "身份证", "护照", "军官证", "其他" };

    // ===== 消费项目（PRD 4.2.9） =====

    public static readonly string[] CONSUME_ITEMS = { "餐饮", "小商品", "洗衣", "其他" };

    // ===== 性别 =====

    public static readonly string[] GENDERS = { "男", "女" };
}
