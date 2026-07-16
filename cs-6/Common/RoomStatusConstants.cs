namespace HotelSys.Common;

/// <summary>
/// 房间状态常量类（PRD 5.1.3 / 第8节）
/// 房态由业务驱动自动变更，不允许手动随意修改（PRD 4.2.5）
/// </summary>
public static class RoomStatusConstants
{
    /// <summary>空闲：可入住</summary>
    public const string FREE = "空闲";

    /// <summary>在住：有客人入住</summary>
    public const string OCCUPIED = "在住";

    /// <summary>预留：已被预订</summary>
    public const string RESERVED = "预留";

    /// <summary>维护：维修/清洁中，不可售</summary>
    public const string MAINTENANCE = "维护";

    /// <summary>所有合法房态集合</summary>
    public static readonly string[] ALL = { FREE, OCCUPIED, RESERVED, MAINTENANCE };
}
