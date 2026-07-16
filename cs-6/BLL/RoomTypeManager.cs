using HotelSys.Common;
using HotelSys.DAL;
using HotelSys.Models;

namespace HotelSys.BLL;

/// <summary>
/// 客房类型业务逻辑类（PRD 4.2.4）
/// 房型增删改查，删除前校验是否有关联客房
/// </summary>
public class RoomTypeManager
{
    private readonly RoomTypeDao _dao = new();

    /// <summary>查询全部客房类型</summary>
    public List<RoomTypeInfo> GetAll() => _dao.FindAll();

    /// <summary>按类型编号查询单条记录</summary>
    public RoomTypeInfo GetById(string typeID) => _dao.FindById(typeID);

    /// <summary>
    /// 新增客房类型
    /// </summary>
    /// <param name="entity">房型实体</param>
    /// <exception cref="BusinessException">必填项为空 / 单价≤0 / 床位数≤0 / 类型名已存在</exception>
    public void Add(RoomTypeInfo entity)
    {
        ValidateRoomType(entity);

        // 类型名称唯一性校验
        if (_dao.FindByName(entity.TypeName) != null)
            throw new BusinessException($"房型名称 {entity.TypeName} 已存在");

        _dao.Insert(entity);

        LogManager.Record(CurrentUser.UserName, BusinessConstants.LOG_ADD,
            $"新增房型 {entity.TypeName}", $"编号:{entity.TypeID} 单价:{entity.Price}");
    }

    /// <summary>
    /// 修改客房类型
    /// </summary>
    /// <param name="entity">房型实体</param>
    /// <exception cref="BusinessException">必填项为空 / 单价≤0 / 床位数≤0</exception>
    public void Update(RoomTypeInfo entity)
    {
        ValidateRoomType(entity);

        _dao.Update(entity);

        LogManager.Record(CurrentUser.UserName, BusinessConstants.LOG_UPDATE,
            $"修改房型 {entity.TypeID}", $"名称:{entity.TypeName} 单价:{entity.Price}");
    }

    /// <summary>
    /// 删除客房类型（删除前校验是否有关联客房，PRD 4.2.4）
    /// </summary>
    /// <param name="typeID">类型编号</param>
    /// <exception cref="BusinessException">存在关联客房，禁止删除</exception>
    public void Delete(string typeID)
    {
        // 删除前校验：有关联客房的房型不可删除，避免产生孤儿房号记录
        int roomCount = _dao.CountRoomsByType(typeID);
        if (roomCount > 0)
            throw new BusinessException($"该房型下还有 {roomCount} 间客房，无法删除");

        _dao.Delete(typeID);

        LogManager.Record(CurrentUser.UserName, BusinessConstants.LOG_DELETE, $"删除房型 {typeID}");
    }

    /// <summary>
    /// 房型数据基础校验（新增/修改共用）
    /// </summary>
    private static void ValidateRoomType(RoomTypeInfo entity)
    {
        if (string.IsNullOrWhiteSpace(entity.TypeID))
            throw new BusinessException("类型编号不能为空");
        if (string.IsNullOrWhiteSpace(entity.TypeName))
            throw new BusinessException("类型名称不能为空");
        // 房价必须大于0，避免0元房费导致营收统计失真
        if (entity.Price <= 0)
            throw new BusinessException("房价必须大于0");
        if (entity.BedCount <= 0)
            throw new BusinessException("床位数必须大于0");
    }
}
