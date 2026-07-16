namespace LibrarySys.Common;

/// <summary>
/// 业务常量定义类，集中管理全系统业务常量，避免魔法值
/// </summary>
public static class BusinessConstants
{
    /// <summary>默认借阅期限（天），PRD 第8节</summary>
    public const int DEFAULT_BORROW_DAYS = 45;

    /// <summary>读者同时借阅图书上限，PRD 第8节</summary>
    public const int MAX_BORROW_LIMIT = 3;

    /// <summary>逾期每天罚款金额（元），PRD 第8节</summary>
    public const decimal FINE_PER_DAY = 0.3m;

    /// <summary>预约有效期/取书保留期（天），PRD 第8节</summary>
    public const int RESERVE_VALID_DAYS = 3;

    /// <summary>App.config 中数据库连接字符串键名</summary>
    public const string CONN_STR_KEY = "LibraryDB";

    // ===== 枚举性常量（取值范围固定的字段值） =====

    /// <summary>管理员权限标识</summary>
    public const string ROLE_ADMIN = "管理员";

    /// <summary>普通用户权限标识</summary>
    public const string ROLE_USER = "普通用户";

    /// <summary>借阅状态-借出中</summary>
    public const string STATUS_BORROWED = "借出";

    /// <summary>借阅状态-已归还</summary>
    public const string STATUS_RETURNED = "已还";

    /// <summary>罚款状态-未缴</summary>
    public const string FINE_UNPAID = "未缴";

    /// <summary>罚款状态-已缴</summary>
    public const string FINE_PAID = "已缴";

    // ===== 预约相关常量 =====

    /// <summary>预约状态-排队中</summary>
    public const string RESERVE_QUEUING = "排队中";

    /// <summary>预约状态-待取书</summary>
    public const string RESERVE_WAITING = "待取书";

    /// <summary>预约状态-已完成</summary>
    public const string RESERVE_COMPLETED = "已完成";

    /// <summary>预约状态-已取消</summary>
    public const string RESERVE_CANCELLED = "已取消";

    // ===== 操作日志类型常量 =====

    /// <summary>日志类型-登录</summary>
    public const string LOG_LOGIN = "登录";

    /// <summary>日志类型-新增</summary>
    public const string LOG_ADD = "新增";

    /// <summary>日志类型-修改</summary>
    public const string LOG_UPDATE = "修改";

    /// <summary>日志类型-删除</summary>
    public const string LOG_DELETE = "删除";

    /// <summary>日志类型-借书</summary>
    public const string LOG_BORROW = "借书";

    /// <summary>日志类型-还书</summary>
    public const string LOG_RETURN = "还书";

    /// <summary>日志类型-预约</summary>
    public const string LOG_RESERVE = "预约";

    /// <summary>日志类型-取消预约</summary>
    public const string LOG_CANCEL_RESERVE = "取消预约";

    /// <summary>日志类型-缴费</summary>
    public const string LOG_PAY = "缴费";
}
