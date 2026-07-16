namespace HotelSys.Models;

/// <summary>
/// 入住记录实体类（对应 T_CheckIn 表，PRD 5.1.6，核心业务表）
/// 结账快照字段（actualDays/roomCharge/otherCharge/consumeAmount/totalAmount）
/// 在退房结账时以操作员手动填写/确认的最终值为准写入数据库
/// </summary>
public class CheckInInfo
{
    /// <summary>入住单号（主键，自增）</summary>
    public int CheckInID { get; set; }

    /// <summary>客户编号（外键→T_Customer）</summary>
    public int CustomerID { get; set; }

    /// <summary>房间号（外键→T_Room）</summary>
    public string RoomNo { get; set; }

    /// <summary>入住时间</summary>
    public DateTime CheckInTime { get; set; }

    /// <summary>预计退房时间</summary>
    public DateTime ExpectCheckOut { get; set; }

    /// <summary>实际退房时间（可空，结账时填写）</summary>
    public DateTime? CheckOutTime { get; set; }

    /// <summary>实际入住天数（可空，结账时手动填写）</summary>
    public int? ActualDays { get; set; }

    /// <summary>押金金额</summary>
    public decimal Deposit { get; set; }

    /// <summary>住宿费（可空，结账时手动填写，系统预填参考值）</summary>
    public decimal? RoomCharge { get; set; }

    /// <summary>其他费用（可空，默认0，结账时手动填写：赔偿/折扣等）</summary>
    public decimal? OtherCharge { get; set; }

    /// <summary>附加消费总额（可空，默认0，结账时手动填写，系统预填参考值）</summary>
    public decimal? ConsumeAmount { get; set; }

    /// <summary>总费用（可空，结账时手动填写最终值）</summary>
    public decimal? TotalAmount { get; set; }

    /// <summary>入住状态：在住/已结账/已取消</summary>
    public string Status { get; set; }

    /// <summary>备注（可空，换房时记录换房轨迹）</summary>
    public string Remark { get; set; }

    // ===== 关联显示字段（JOIN 查询时填充，非数据库列） =====

    /// <summary>客户姓名（关联 T_Customer）</summary>
    public string CustomerName { get; set; }

    /// <summary>房型名称（关联 T_Room→T_RoomType）</summary>
    public string TypeName { get; set; }

    /// <summary>房型单价（关联 T_Room→T_RoomType，用于费用计算）</summary>
    public decimal Price { get; set; }

    /// <summary>手机号（关联 T_Customer，用于显示）</summary>
    public string Phone { get; set; }
}
