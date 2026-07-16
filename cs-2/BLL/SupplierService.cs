using CampusShop.Common;
using CampusShop.DAL;
using CampusShop.Models;

namespace CampusShop.BLL;

/// <summary>
/// 供货商业务服务类，处理供货商的增删改查及关联性校验
/// </summary>
public class SupplierService
{
    private readonly SupplierDAL _supplierDAL = new();

    /// <summary>
    /// 查询全部供货商
    /// </summary>
    /// <returns>供货商列表</returns>
    public List<Supplier> GetAllSuppliers()
    {
        return _supplierDAL.GetAllSuppliers();
    }

    /// <summary>
    /// 按编号或名称检索
    /// </summary>
    /// <param name="idKeyword">编号关键字</param>
    /// <param name="nameKeyword">名称关键字</param>
    /// <returns>供货商列表</returns>
    public List<Supplier> SearchSuppliers(string idKeyword, string nameKeyword)
    {
        return _supplierDAL.SearchSuppliers(idKeyword, nameKeyword);
    }

    /// <summary>
    /// 新增供货商
    /// </summary>
    /// <exception cref="BusinessException">校验失败</exception>
    public void AddSupplier(string supplierID, string supplierName,
        string contactPerson, string phone, string address,
        string legalPerson, DateTime? registerDate)
    {
        if (ValidationHelper.IsNullOrWhiteSpace(supplierID))
        {
            throw new BusinessException("供货商编号不能为空");
        }
        if (ValidationHelper.IsExceedLength(supplierID, 20))
        {
            throw new BusinessException("供货商编号长度不能超过20个字符");
        }
        if (ValidationHelper.IsNullOrWhiteSpace(supplierName))
        {
            throw new BusinessException("供货商名称不能为空");
        }
        if (ValidationHelper.IsExceedLength(supplierName, 50))
        {
            throw new BusinessException("供货商名称长度不能超过50个字符");
        }
        if (ValidationHelper.IsExceedLength(contactPerson, 20))
        {
            throw new BusinessException("联系人长度不能超过20个字符");
        }
        if (!ValidationHelper.IsValidPhone(phone))
        {
            throw new BusinessException("联系电话格式不合法（允许数字、空格、连字符，长度6-15）");
        }
        if (ValidationHelper.IsExceedLength(address, 100))
        {
            throw new BusinessException("地址长度不能超过100个字符");
        }
        if (ValidationHelper.IsExceedLength(legalPerson, 20))
        {
            throw new BusinessException("法人代表长度不能超过20个字符");
        }

        Supplier supplier = new()
        {
            SupplierID = supplierID,
            SupplierName = supplierName,
            ContactPerson = contactPerson,
            Phone = phone,
            Address = address,
            LegalPerson = string.IsNullOrEmpty(legalPerson) ? null : legalPerson,
            RegisterDate = registerDate
        };

        if (!_supplierDAL.InsertSupplier(supplier))
        {
            throw new BusinessException("供货商编号已存在");
        }
    }

    /// <summary>
    /// 修改供货商（编号不可改）
    /// </summary>
    /// <exception cref="BusinessException">校验失败</exception>
    public void UpdateSupplier(string supplierID, string supplierName,
        string contactPerson, string phone, string address,
        string legalPerson, DateTime? registerDate)
    {
        if (_supplierDAL.GetSupplierByID(supplierID) == null)
        {
            throw new BusinessException("供货商不存在");
        }
        if (ValidationHelper.IsNullOrWhiteSpace(supplierName))
        {
            throw new BusinessException("供货商名称不能为空");
        }
        if (ValidationHelper.IsExceedLength(supplierName, 50))
        {
            throw new BusinessException("供货商名称长度不能超过50个字符");
        }
        if (ValidationHelper.IsExceedLength(contactPerson, 20))
        {
            throw new BusinessException("联系人长度不能超过20个字符");
        }
        if (!ValidationHelper.IsValidPhone(phone))
        {
            throw new BusinessException("联系电话格式不合法（允许数字、空格、连字符，长度6-15）");
        }
        if (ValidationHelper.IsExceedLength(address, 100))
        {
            throw new BusinessException("地址长度不能超过100个字符");
        }
        if (ValidationHelper.IsExceedLength(legalPerson, 20))
        {
            throw new BusinessException("法人代表长度不能超过20个字符");
        }

        Supplier supplier = new()
        {
            SupplierID = supplierID,
            SupplierName = supplierName,
            ContactPerson = contactPerson,
            Phone = phone,
            Address = address,
            LegalPerson = string.IsNullOrEmpty(legalPerson) ? null : legalPerson,
            RegisterDate = registerDate
        };

        _supplierDAL.UpdateSupplier(supplier);
    }

    /// <summary>
    /// 删除供货商（存在关联商品时拒绝删除）
    /// </summary>
    /// <param name="supplierID">供货商编号</param>
    /// <exception cref="BusinessException">校验失败</exception>
    public void DeleteSupplier(string supplierID)
    {
        if (_supplierDAL.GetSupplierByID(supplierID) == null)
        {
            throw new BusinessException("供货商不存在");
        }

        // 删除前检查是否存在关联商品
        if (_supplierDAL.HasProducts(supplierID))
        {
            throw new BusinessException("存在关联商品，无法删除");
        }

        if (!_supplierDAL.DeleteSupplier(supplierID))
        {
            throw new BusinessException("删除失败");
        }
    }
}
