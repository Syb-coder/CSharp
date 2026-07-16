using HotelSys.Common;
using HotelSys.DAL;
using HotelSys.Models;

namespace HotelSys.BLL;

/// <summary>
/// 退房结算辅助类（PRD 4.2.10）
/// 提供退房参考值自动计算与预填逻辑，实际退房保存由 CheckInManager.CheckOut 完成
/// 参考值仅作初始填充，最终保存的数字以操作员手动填写/确认的值为准
/// </summary>
public class CheckOutManager
{
    private readonly CheckInDao _checkInDao = new();
    private readonly ConsumeDao _consumeDao = new();

    /// <summary>
    /// 计算退房结算参考值（PRD 4.2.10）
    /// 自动计算实际入住天数、参考住宿费、参考附加消费、参考总费用、应收应退
    /// </summary>
    /// <param name="checkInID">入住单号</param>
    /// <returns>包含参考值的入住单实体（UI 据此预填各输入框）</returns>
    /// <exception cref="BusinessException">入住单不存在</exception>
    public CheckInInfo CalculateReference(int checkInID)
    {
        CheckInInfo checkIn = _checkInDao.GetById(checkInID)
            ?? throw new BusinessException($"入住单号 {checkInID} 不存在");

        // 实际入住天数=入住日期到今天的天数+1（入住当天算1天，PRD 4.2.10）
        int actualDays = Math.Max(1, (DateTime.Now.Date - checkIn.CheckInTime.Date).Days + 1);
        checkIn.ActualDays = actualDays;

        // 参考住宿费=实际入住天数×房型单价（房型单价来自 JOIN 查询的 Price 字段）
        checkIn.RoomCharge = actualDays * checkIn.Price;

        // 参考附加消费=该入住单下所有消费记录总额
        checkIn.ConsumeAmount = _consumeDao.GetTotalByCheckIn(checkInID);

        // 其他费用默认0（赔偿/折扣等由前台手动录入）
        checkIn.OtherCharge = 0;

        // 参考总费用=住宿费+其他费用+附加消费
        checkIn.TotalAmount = checkIn.RoomCharge + checkIn.OtherCharge + checkIn.ConsumeAmount;

        return checkIn;
    }

    /// <summary>
    /// 计算应收应退金额（PRD 4.2.10）
    /// 应收应退=总金额-押金，正数为应收（客人需补交），负数为应退（需退还客人）
    /// </summary>
    /// <param name="totalAmount">总金额</param>
    /// <param name="deposit">已收押金</param>
    /// <returns>正数表示应收，负数表示应退</returns>
    public decimal CalculateBalance(decimal totalAmount, decimal deposit)
        => totalAmount - deposit;
}
