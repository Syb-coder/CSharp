namespace HotelSys.Models;

/// <summary>
/// 今日营业概览统计实体（PRD 4.2.3 / 4.2.11）
/// 用于看板顶部4个统计卡片和营业统计页面展示
/// </summary>
public class StatisticsInfo
{
    /// <summary>今日入住数（含已结账和已取消的当日入住记录）</summary>
    public int TodayCheckInCount { get; set; }

    /// <summary>今日退房数（当日结账的入住记录数）</summary>
    public int TodayCheckOutCount { get; set; }

    /// <summary>当前在住数（status='在住'的入住记录数）</summary>
    public int OccupiedCount { get; set; }

    /// <summary>今日营收（当日结账入住单的 totalAmount 之和）</summary>
    public decimal TodayRevenue { get; set; }
}
