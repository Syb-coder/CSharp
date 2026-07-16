namespace HotelSys.Models;

/// <summary>
/// 客房信息实体类（对应 T_Room 表，PRD 5.1.3）
/// </summary>
public class RoomInfo
{
    /// <summary>房间号（主键，如"301"）</summary>
    public string RoomNo { get; set; }

    /// <summary>客房类型编号（外键→T_RoomType）</summary>
    public string TypeID { get; set; }

    /// <summary>楼层</summary>
    public int Floor { get; set; }

    /// <summary>床位数（默认从房型继承，可覆盖）</summary>
    public int BedCount { get; set; }

    /// <summary>房间状态：空闲/在住/预留/维护</summary>
    public string RoomStatus { get; set; }

    /// <summary>备注（可空）</summary>
    public string Remark { get; set; }

    // ===== 关联显示字段（JOIN 查询时填充，非数据库列） =====

    /// <summary>房型名称（关联 T_RoomType，用于显示）</summary>
    public string TypeName { get; set; }

    /// <summary>房型单价（关联 T_RoomType，用于入住时计算押金建议值）</summary>
    public decimal Price { get; set; }
}
