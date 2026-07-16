using HotelSys.Common;
using HotelSys.DAL;
using HotelSys.Models;

namespace HotelSys.BLL;

/// <summary>
/// 消费记账业务逻辑类（PRD 4.2.9 / F-17）
/// 为在住客人录入附加消费（餐饮/小商品/洗衣/其他）
/// 退房结账时汇总消费总额预填到结算页
/// </summary>
public class ConsumeManager
{
    private readonly ConsumeDao _dao = new();
    private readonly CheckInDao _checkInDao = new();

    /// <summary>查询全部消费记录</summary>
    public List<ConsumeInfo> GetAll() => _dao.FindAll();

    /// <summary>按消费编号查询单条记录</summary>
    public ConsumeInfo GetById(int consumeID) => _dao.FindById(consumeID.ToString());

    /// <summary>
    /// 按入住单查询消费明细（PRD F-17 / 6.5 接口契约）
    /// </summary>
    /// <param name="checkInID">入住单号</param>
    public List<ConsumeInfo> GetByCheckIn(int checkInID) => _dao.GetByCheckIn(checkInID);

    /// <summary>
    /// 汇总指定入住单的消费总额（结算退房时预填参考值，PRD F-18 / 6.5 接口契约）
    /// </summary>
    /// <param name="checkInID">入住单号</param>
    /// <returns>消费总额（无记录返回0）</returns>
    public decimal GetTotalByCheckIn(int checkInID) => _dao.GetTotalByCheckIn(checkInID);

    /// <summary>按日期范围查询消费明细</summary>
    public List<ConsumeInfo> GetByDateRange(DateTime from, DateTime to) => _dao.GetByDateRange(from, to);

    /// <summary>
    /// 新增消费记录（PRD F-17 / 6.5 接口契约）
    /// 仅允许为"在住"状态的入住单录入消费
    /// </summary>
    /// <param name="entity">消费实体</param>
    /// <exception cref="BusinessException">入住单不存在 / 入住单已退房 / 项目名为空 / 金额≤0</exception>
    public void Add(ConsumeInfo entity)
    {
        // 校验入住单存在且为在住状态，避免为已退房客人录入消费
        CheckInInfo checkIn = _checkInDao.GetById(entity.CheckInID)
            ?? throw new BusinessException($"入住单号 {entity.CheckInID} 不存在");

        if (checkIn.Status != BusinessConstants.CHECKIN_OCCUPIED)
            throw new BusinessException($"入住单 {entity.CheckInID} 已退房，无法录入消费");

        if (string.IsNullOrWhiteSpace(entity.ItemName))
            throw new BusinessException("消费项目不能为空");

        // 消费金额必须大于0，避免0元或负数消费导致账目异常
        if (entity.Amount <= 0)
            throw new BusinessException("消费金额必须大于0");

        _dao.Insert(entity);

        LogManager.Record(CurrentUser.UserName, BusinessConstants.LOG_CONSUME,
            $"录入消费 入住单:{entity.CheckInID} {entity.ItemName}", $"金额:{entity.Amount}");
    }

    /// <summary>
    /// 修改消费记录
    /// </summary>
    /// <param name="entity">消费实体</param>
    /// <exception cref="BusinessException">项目名为空 / 金额≤0</exception>
    public void Update(ConsumeInfo entity)
    {
        if (string.IsNullOrWhiteSpace(entity.ItemName))
            throw new BusinessException("消费项目不能为空");
        if (entity.Amount <= 0)
            throw new BusinessException("消费金额必须大于0");

        _dao.Update(entity);

        LogManager.Record(CurrentUser.UserName, BusinessConstants.LOG_UPDATE,
            $"修改消费 编号:{entity.ConsumeID}", $"项目:{entity.ItemName} 金额:{entity.Amount}");
    }

    /// <summary>
    /// 删除消费记录
    /// </summary>
    /// <param name="consumeID">消费编号</param>
    public void Delete(int consumeID)
    {
        _dao.Delete(consumeID.ToString());

        LogManager.Record(CurrentUser.UserName, BusinessConstants.LOG_DELETE, $"删除消费 编号:{consumeID}");
    }
}
