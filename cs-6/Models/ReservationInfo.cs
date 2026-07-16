namespace HotelSys.Models;

/// <summary>
/// 预订记录实体类（对应 T_Reservation 表，PRD 5.1.5）
/// </summary>
public class ReservationInfo
{
    /// <summary>预订编号（主键，自增）</summary>
    public int ReserveID { get; set; }

    /// <summary>客户编号（外键→T_Customer）</summary>
    public int CustomerID { get; set; }

    /// <summary>预留房间号（外键→T_Room）</summary>
    public string RoomNo { get; set; }

    /// <summary>预计入住日期</summary>
    public DateTime ExpectCheckIn { get; set; }

    /// <summary>预计入住天数</summary>
    public int ExpectDays { get; set; }

    /// <summary>联系电话</summary>
    public string ContactPhone { get; set; }

    /// <summary>预订时间</summary>
    public DateTime ReserveTime { get; set; }

    /// <summary>预订状态：待入住/已入住/已取消/已过期</summary>
    public string Status { get; set; }

    /// <summary>备注（可空）</summary>
    public string Remark { get; set; }

    // ===== 关联显示字段（JOIN 查询时填充，非数据库列） =====

    /// <summary>客户姓名（关联 T_Customer，用于显示）</summary>
    public string CustomerName { get; set; }

    /// <summary>房型名称（关联 T_Room→T_RoomType，用于显示）</summary>
    public string TypeName { get; set; }

    /// <summary>楼层（关联 T_Room，用于显示）</summary>
    public int Floor { get; set; }
}
