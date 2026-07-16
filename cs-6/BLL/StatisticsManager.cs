using HotelSys.DAL;
using HotelSys.Models;

namespace HotelSys.BLL;

/// <summary>
/// 营业统计业务逻辑类（PRD 4.2.3 / 4.2.11 / 6.5 接口契约）
/// 今日营业概览、入住率、房型入住排行、日期范围营收
/// 统计聚合 SQL 统一放在 DAL 层 CheckInDao 中，BLL 层仅负责调用和组装
/// </summary>
public class StatisticsManager
{
    private readonly CheckInDao _checkInDao = new();
    private readonly RoomDao _roomDao = new();

    /// <summary>
    /// 获取今日营业概览（PRD 4.2.3 看板4个卡片 / 6.5 接口契约）
    /// </summary>
    /// <returns>包含今日入住数、今日退房数、当前在住数、今日营收的统计实体</returns>
    public StatisticsInfo GetTodayOverview()
    {
        return new StatisticsInfo
        {
            TodayCheckInCount = _checkInDao.GetTodayCheckInCount(),
            TodayCheckOutCount = _checkInDao.GetTodayCheckOutCount(),
            OccupiedCount = _checkInDao.GetOccupiedCount(),
            TodayRevenue = _checkInDao.GetTodayRevenue()
        };
    }

    /// <summary>
    /// 计算当前入住率（PRD 4.2.11 / 6.5 接口契约）
    /// 入住率=当前在住房间数/(空闲+在住)×100%，排除维护中的房间
    /// </summary>
    /// <returns>入住率百分比（0~100），无可用房间时返回0</returns>
    public decimal GetOccupancyRate()
    {
        int occupiedCount = _checkInDao.GetOccupiedCount();

        // 可用房间数=空闲+在住（排除维护中的房间，PRD 4.2.11）
        int freeCount = _roomDao.GetByStatus(HotelSys.Common.RoomStatusConstants.FREE).Count;

        int availableCount = freeCount + occupiedCount;
        if (availableCount == 0) return 0m;

        // 入住率=在住数/可用房间数×100，保留2位小数
        return Math.Round((decimal)occupiedCount / availableCount * 100, 2);
    }

    /// <summary>
    /// 房型入住排行 Top5（PRD 4.2.11 / 6.5 接口契约）
    /// 按已结账入住记录数统计最受欢迎的房型
    /// </summary>
    /// <returns>房型名称与入住次数的键值对列表，按次数降序排列</returns>
    public List<KeyValuePair<string, int>> GetRoomTypeRanking()
        => _checkInDao.GetRoomTypeRanking();

    /// <summary>
    /// 按日期范围查询营收（PRD 4.2.11 / 6.5 接口契约）
    /// </summary>
    /// <param name="from">起始日期</param>
    /// <param name="to">结束日期</param>
    /// <returns>该日期范围内已结账入住单的 totalAmount 之和</returns>
    public decimal GetRevenueByDateRange(DateTime from, DateTime to)
        => _checkInDao.GetRevenueByDateRange(from, to);

    /// <summary>
    /// 按日期范围查询结账记录数（用于计算平均客单价）
    /// </summary>
    /// <param name="from">起始日期</param>
    /// <param name="to">结束日期</param>
    /// <returns>该日期范围内的结账记录数</returns>
    public int GetCheckOutCountByDateRange(DateTime from, DateTime to)
        => _checkInDao.GetCheckOutCountByDateRange(from, to);
}
