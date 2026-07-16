using CampusMart.Common;
using CampusMart.DAL;
using CampusMart.Models;

namespace CampusMart.BLL;

/// <summary>
/// 供货商业务逻辑类
/// </summary>
/// <remarks>
/// 与 cs-2 的 SupplierService 差异化命名（SupplierBiz）。
/// </remarks>
public class SupplierBiz
{
    private readonly SupplierDao _supplierDao = new();

    /// <summary>
    /// 查询全部供货商
    /// </summary>
    /// <returns>供货商列表</returns>
    public List<SupplierInfo> GetAllSuppliers()
    {
        return _supplierDao.GetAllSuppliers();
    }

    /// <summary>
    /// 按供货商名称或法人代表关键字检索
    /// </summary>
    /// <param name="keyword">名称或法人关键字</param>
    /// <returns>匹配的供货商列表</returns>
    public List<SupplierInfo> SearchSuppliers(string keyword)
    {
        if (ValidateUtil.IsNullOrWhiteSpace(keyword))
        {
            return _supplierDao.GetAllSuppliers();
        }
        return _supplierDao.SearchByNameOrLegalPerson(keyword);
    }

    /// <summary>
    /// 新增供货商
    /// </summary>
    /// <param name="supplier">供货商实体</param>
    /// <exception cref="BusinessException">校验失败</exception>
    public void AddSupplier(SupplierInfo supplier)
    {
        ValidateSupplier(supplier);
        _supplierDao.InsertSupplier(supplier);
    }

    /// <summary>
    /// 修改供货商信息
    /// </summary>
    /// <param name="supplier">供货商实体</param>
    /// <exception cref="BusinessException">校验失败</exception>
    public void UpdateSupplier(SupplierInfo supplier)
    {
        ValidateSupplier(supplier);
        _supplierDao.UpdateSupplier(supplier);
    }

    /// <summary>
    /// 删除供货商（存在关联商品时拒绝删除）
    /// </summary>
    /// <param name="supplierID">供货商编号</param>
    /// <exception cref="BusinessException">存在关联商品</exception>
    public void DeleteSupplier(int supplierID)
    {
        if (_supplierDao.HasRelatedProducts(supplierID))
        {
            throw new BusinessException("存在关联商品，无法删除");
        }

        _supplierDao.DeleteSupplier(supplierID);
    }

    /// <summary>
    /// 供货商字段校验（供新增和修改共用）
    /// </summary>
    /// <param name="supplier">供货商实体</param>
    /// <exception cref="BusinessException">校验失败</exception>
    private static void ValidateSupplier(SupplierInfo supplier)
    {
        if (ValidateUtil.IsNullOrWhiteSpace(supplier.SupplierName))
        {
            throw new BusinessException("供货商名称不能为空");
        }
        if (ValidateUtil.IsExceedLength(supplier.SupplierName, 50))
        {
            throw new BusinessException("供货商名称长度不能超过50个字符");
        }
        if (ValidateUtil.IsExceedLength(supplier.LegalPerson, 20))
        {
            throw new BusinessException("法人代表长度不能超过20个字符");
        }
        if (ValidateUtil.IsExceedLength(supplier.ContactPerson, 20))
        {
            throw new BusinessException("联系人长度不能超过20个字符");
        }
        if (!ValidateUtil.IsValidContactPhone(supplier.Phone))
        {
            throw new BusinessException("联系电话格式不合法（允许数字、空格、连字符，长度6-15）");
        }
        if (ValidateUtil.IsExceedLength(supplier.Address, 100))
        {
            throw new BusinessException("地址长度不能超过100个字符");
        }
    }
}
