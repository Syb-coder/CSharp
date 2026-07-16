using HotelSys.Common;
using HotelSys.DAL;
using HotelSys.Models;

namespace HotelSys.BLL;

/// <summary>
/// 客房业务逻辑类（PRD 4.2.5）
/// 客房增删改查、房态维护（空闲↔维护）
/// 房态的正常流转（入住→在住、结账→空闲、预订→预留）由业务驱动，
/// 此处仅提供手动维护（空闲↔维护）接口
/// </summary>
public class RoomManager
{
    private readonly RoomDao _dao = new();

    /// <summary>查询全部客房（含房型名称和单价）</summary>
    public List<RoomInfo> GetAll() => _dao.FindAll();

    /// <summary>按房号查询单条记录</summary>
    public RoomInfo GetById(string roomNo) => _dao.FindById(roomNo);

    /// <summary>按楼层查询客房（PRD 6.5 接口契约）</summary>
    public List<RoomInfo> GetByFloor(int floor) => _dao.GetByFloor(floor);

    /// <summary>查询所有空闲房间（入住/换房时选择，PRD 6.5 接口契约）</summary>
    public List<RoomInfo> GetFreeRooms() => _dao.GetFreeRooms();

    /// <summary>按房态查询客房（房态看板用）</summary>
    public List<RoomInfo> GetByStatus(string status) => _dao.GetByStatus(status);

    /// <summary>
    /// 新增客房（默认状态为空闲，PRD 4.2.5）
    /// </summary>
    /// <param name="entity">客房实体</param>
    /// <exception cref="BusinessException">必填项为空 / 房号已存在</exception>
    public void Add(RoomInfo entity)
    {
        ValidateRoom(entity);

        // 房号唯一性校验
        if (_dao.FindById(entity.RoomNo) != null)
            throw new BusinessException($"房号 {entity.RoomNo} 已存在");

        _dao.Insert(entity);

        LogManager.Record(CurrentUser.UserName, BusinessConstants.LOG_ADD,
            $"新增客房 {entity.RoomNo}", $"房型:{entity.TypeID} 楼层:{entity.Floor}");
    }

    /// <summary>
    /// 修改客房信息（不含房态，房态由业务驱动）
    /// </summary>
    /// <param name="entity">客房实体</param>
    /// <exception cref="BusinessException">必填项为空</exception>
    public void Update(RoomInfo entity)
    {
        ValidateRoom(entity);

        _dao.Update(entity);

        LogManager.Record(CurrentUser.UserName, BusinessConstants.LOG_UPDATE,
            $"修改客房 {entity.RoomNo}", $"房型:{entity.TypeID} 楼层:{entity.Floor}");
    }

    /// <summary>
    /// 删除客房（删除前校验是否有在住或预留记录，PRD 4.2.5）
    /// </summary>
    /// <param name="roomNo">房号</param>
    /// <exception cref="BusinessException">存在在住或预留记录，禁止删除</exception>
    public void Delete(string roomNo)
    {
        // 删除前校验：有在住或预留的房间不可删除，避免业务数据悬空
        int activeCount = _dao.CountActiveByRoom(roomNo);
        if (activeCount > 0)
            throw new BusinessException($"该房间有在住或预留记录，无法删除");

        _dao.Delete(roomNo);

        LogManager.Record(CurrentUser.UserName, BusinessConstants.LOG_DELETE, $"删除客房 {roomNo}");
    }

    /// <summary>
    /// 更新房间状态（房态状态机驱动，PRD 6.5 接口契约）
    /// 仅允许手动切换"空闲↔维护"，其他状态流转由入住/退房/预订业务驱动
    /// </summary>
    /// <param name="roomNo">房号</param>
    /// <param name="newStatus">新状态（维护/空闲）</param>
    /// <exception cref="BusinessException">房间不存在 / 非法状态切换</exception>
    public void UpdateStatus(string roomNo, string newStatus)
    {
        RoomInfo room = _dao.FindById(roomNo)
            ?? throw new BusinessException($"房号 {roomNo} 不存在");

        // 状态机校验：手动操作仅允许 空闲↔维护 互转
        // 在住/预留 状态由入住/退房/预订业务自动流转，禁止手动修改
        bool toMaintenance = newStatus == RoomStatusConstants.MAINTENANCE
                             && room.RoomStatus == RoomStatusConstants.FREE;
        bool toFree = newStatus == RoomStatusConstants.FREE
                      && room.RoomStatus == RoomStatusConstants.MAINTENANCE;
        if (!toMaintenance && !toFree)
        {
            throw new BusinessException($"房态 {room.RoomStatus} 无法手动切换为 {newStatus}（仅允许空闲↔维护互转）");
        }

        _dao.UpdateStatus(roomNo, newStatus);

        // 操作类型根据切换方向记录
        string logType = toMaintenance ? BusinessConstants.LOG_SET_MAINTENANCE : BusinessConstants.LOG_RESTORE_FREE;
        LogManager.Record(CurrentUser.UserName, logType, $"房号 {roomNo} 状态变更为 {newStatus}");
    }

    /// <summary>
    /// 客房数据基础校验（新增/修改共用）
    /// </summary>
    private static void ValidateRoom(RoomInfo entity)
    {
        if (string.IsNullOrWhiteSpace(entity.RoomNo))
            throw new BusinessException("房号不能为空");
        if (string.IsNullOrWhiteSpace(entity.TypeID))
            throw new BusinessException("请选择房型");
        if (entity.Floor <= 0)
            throw new BusinessException("楼层必须大于0");
        if (entity.BedCount <= 0)
            throw new BusinessException("床位数必须大于0");
    }
}
