namespace HotelSys.Models;

/// <summary>
/// 操作日志实体类（对应 T_OperateLog 表，PRD 5.1.8）
/// 日志不可修改、不可删除，仅支持查询
/// </summary>
public class OperateLogInfo
{
    /// <summary>日志编号（主键，自增）</summary>
    public int LogID { get; set; }

    /// <summary>操作者用户名</summary>
    public string UserName { get; set; }

    /// <summary>操作时间</summary>
    public DateTime OperateTime { get; set; }

    /// <summary>操作类型（登录/入住/退房/续住/换房/预订/取消预订/消费/设维护/恢复空闲等）</summary>
    public string OperateType { get; set; }

    /// <summary>操作对象（如"房间：301"、"入住单：1001"）</summary>
    public string OperateContent { get; set; }

    /// <summary>详细说明（可空）</summary>
    public string Detail { get; set; }
}
