using HotelSys.Common;
using HotelSys.DAL;
using HotelSys.Models;

namespace HotelSys.BLL;

/// <summary>
/// 客户信息业务逻辑类（PRD 4.2.6 / F-07 / F-08）
/// 客户增删改查、多条件搜索、入住历史查询
/// 删除前校验是否有在住记录
/// </summary>
public class CustomerManager
{
    private readonly CustomerDao _dao = new();
    private readonly CheckInDao _checkInDao = new();

    /// <summary>查询全部客户</summary>
    public List<CustomerInfo> GetAll() => _dao.FindAll();

    /// <summary>按客户编号查询单条记录</summary>
    public CustomerInfo GetById(int customerID) => _dao.GetById(customerID);

    /// <summary>
    /// 多条件模糊查询（姓名/证件号/手机号，PRD F-07 / 6.5 接口契约）
    /// 三个条件为空时返回全部，支持组合查询
    /// </summary>
    /// <param name="name">姓名关键字（模糊匹配，空表示不限）</param>
    /// <param name="idNumber">证件号关键字（模糊匹配，空表示不限）</param>
    /// <param name="phone">手机号关键字（模糊匹配，空表示不限）</param>
    public List<CustomerInfo> Search(string name, string idNumber, string phone)
        => _dao.Search(name, idNumber, phone);

    /// <summary>
    /// 查询客户入住历史（PRD F-08 / 6.5 接口契约）
    /// </summary>
    /// <param name="customerID">客户编号</param>
    /// <returns>该客户的历史入住记录列表（按入住时间倒序）</returns>
    public List<CheckInInfo> GetHistory(int customerID)
        => _checkInDao.GetByCustomer(customerID);

    /// <summary>
    /// 新增客户，返回新生成的客户编号
    /// </summary>
    /// <param name="entity">客户实体</param>
    /// <returns>新生成的客户编号（用于入住登记时关联）</returns>
    /// <exception cref="BusinessException">必填项为空</exception>
    public int Add(CustomerInfo entity)
    {
        ValidateCustomer(entity);

        int newId = _dao.Insert(entity);

        LogManager.Record(CurrentUser.UserName, BusinessConstants.LOG_ADD,
            $"新增客户 {entity.CustomerName}", $"编号:{newId} 证件:{entity.IdNumber}");

        return newId;
    }

    /// <summary>
    /// 修改客户信息
    /// </summary>
    /// <param name="entity">客户实体</param>
    /// <exception cref="BusinessException">必填项为空</exception>
    public void Update(CustomerInfo entity)
    {
        ValidateCustomer(entity);

        _dao.Update(entity);

        LogManager.Record(CurrentUser.UserName, BusinessConstants.LOG_UPDATE,
            $"修改客户 {entity.CustomerName}", $"编号:{entity.CustomerID}");
    }

    /// <summary>
    /// 删除客户（删除前校验是否有在住记录，PRD 4.2.6 / 6.5 接口契约）
    /// </summary>
    /// <param name="customerID">客户编号</param>
    /// <exception cref="BusinessException">存在在住记录，禁止删除</exception>
    public void Delete(int customerID)
    {
        // 删除前校验：有在住记录的客户不可删除，避免丢失在住业务数据
        int occupiedCount = _dao.CountOccupiedByCustomer(customerID);
        if (occupiedCount > 0)
            throw new BusinessException($"该客户有 {occupiedCount} 条在住记录，无法删除");

        _dao.Delete(customerID.ToString());

        LogManager.Record(CurrentUser.UserName, BusinessConstants.LOG_DELETE, $"删除客户 编号:{customerID}");
    }

    /// <summary>
    /// 客户数据基础校验（新增/修改共用）
    /// </summary>
    private static void ValidateCustomer(CustomerInfo entity)
    {
        if (string.IsNullOrWhiteSpace(entity.CustomerName))
            throw new BusinessException("客户姓名不能为空");
        if (string.IsNullOrWhiteSpace(entity.IdType))
            throw new BusinessException("请选择证件类型");
        if (string.IsNullOrWhiteSpace(entity.IdNumber))
            throw new BusinessException("证件号码不能为空");
    }
}
