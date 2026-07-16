using System.Data;
using CampusStore.Common;
using CampusStore.DAL;
using CampusStore.Models;

namespace CampusStore.BLL;

/// <summary>
/// 供货商业务逻辑类
/// </summary>
public class SupplierManager
{
    private readonly SupplierRepository _repo = new();

    /// <summary>
    /// 添加供货商
    /// </summary>
    public int Add(SupplierInfo info)
    {
        ValidateSupplier(info);
        return _repo.Add(info);
    }

    /// <summary>
    /// 修改供货商
    /// </summary>
    public int Update(SupplierInfo info)
    {
        if (info.SupplierID <= 0)
            throw new BusinessException("请选择要修改的供货商");
        ValidateSupplier(info);
        return _repo.Update(info);
    }

    /// <summary>
    /// 删除供货商
    /// </summary>
    /// <exception cref="BusinessException">存在关联商品时抛出</exception>
    public int Delete(int supplierID)
    {
        if (supplierID <= 0)
            throw new BusinessException("请选择要删除的供货商");

        int result = _repo.Delete(supplierID);
        if (result == 0)
            throw new BusinessException("存在关联商品，无法删除");
        return result;
    }

    /// <summary>
    /// 查询全部供货商
    /// </summary>
    public DataTable GetAll() => _repo.GetAll();

    /// <summary>
    /// 按名称关键字模糊检索
    /// </summary>
    public DataTable Search(string keyword) => _repo.Search(keyword);

    /// <summary>
    /// 查询全部供货商（实体列表，供下拉框使用）
    /// </summary>
    public List<SupplierInfo> GetAllList() => _repo.GetAllList();

    /// <summary>
    /// 供货商字段校验
    /// </summary>
    private static void ValidateSupplier(SupplierInfo info)
    {
        if (ValidateUtil.IsNullOrWhiteSpace(info.SupplierName))
            throw new BusinessException("请输入供货商名称");
        if (ValidateUtil.IsExceedLength(info.SupplierName, 50))
            throw new BusinessException("供货商名称长度不能超过 50 个字符");
        if (ValidateUtil.IsExceedLength(info.LegalPerson, 20))
            throw new BusinessException("法人代表长度不能超过 20 个字符");
        if (ValidateUtil.IsExceedLength(info.ContactPerson, 20))
            throw new BusinessException("联系人长度不能超过 20 个字符");
        if (ValidateUtil.IsExceedLength(info.Phone, 15))
            throw new BusinessException("联系电话长度不能超过 15 个字符");
        if (ValidateUtil.IsExceedLength(info.Address, 100))
            throw new BusinessException("地址长度不能超过 100 个字符");
        if (!ValidateUtil.IsValidPhone(info.Phone))
            throw new BusinessException("联系电话格式不正确");
        if (!ValidateUtil.IsNotFutureDate(info.RegisterDate))
            throw new BusinessException("注册日期不能晚于当前日期");
    }
}
