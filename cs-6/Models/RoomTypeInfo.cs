namespace HotelSys.Models;

/// <summary>
/// 客房类型信息实体类（对应 T_RoomType 表，PRD 5.1.2）
/// </summary>
public class RoomTypeInfo
{
    /// <summary>类型编号（主键，如 RT01）</summary>
    public string TypeID { get; set; }

    /// <summary>类型名称（豪华套间/标准套间/三人间/标准间/单人间/其它）</summary>
    public string TypeName { get; set; }

    /// <summary>单价（元/天）</summary>
    public decimal Price { get; set; }

    /// <summary>默认床位数</summary>
    public int BedCount { get; set; }

    /// <summary>描述（可空）</summary>
    public string Description { get; set; }
}
