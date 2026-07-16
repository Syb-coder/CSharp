using CampusShop.Common;
using CampusShop.DAL;
using CampusShop.Models;

namespace CampusShop.BLL;

/// <summary>
/// 商品业务服务类，处理商品的增删改查及关联性校验
/// </summary>
public class ProductService
{
    private readonly ProductDAL _productDAL = new();
    private readonly CategoryDAL _categoryDAL = new();
    private readonly SupplierDAL _supplierDAL = new();

    /// <summary>
    /// 查询全部商品
    /// </summary>
    /// <returns>商品列表</returns>
    public List<Product> GetAllProducts()
    {
        return _productDAL.GetAllProducts();
    }

    /// <summary>
    /// 按多条件组合检索
    /// </summary>
    /// <param name="name">商品名称关键字</param>
    /// <param name="categoryID">类别编号</param>
    /// <param name="supplierID">供货商编号</param>
    /// <returns>商品列表</returns>
    public List<Product> SearchProducts(string name, string categoryID, string supplierID)
    {
        return _productDAL.SearchProducts(name, categoryID, supplierID);
    }

    /// <summary>
    /// 新增商品
    /// </summary>
    /// <exception cref="BusinessException">校验失败</exception>
    public void AddProduct(string productID, string productName, string specification,
        decimal unitPrice, int stockQuantity, string categoryID, string supplierID,
        string origin, DateTime? productionDate)
    {
        if (ValidationHelper.IsNullOrWhiteSpace(productID))
        {
            throw new BusinessException("商品编号不能为空");
        }
        if (ValidationHelper.IsExceedLength(productID, 20))
        {
            throw new BusinessException("商品编号长度不能超过20个字符");
        }
        if (ValidationHelper.IsNullOrWhiteSpace(productName))
        {
            throw new BusinessException("商品名称不能为空");
        }
        if (ValidationHelper.IsExceedLength(productName, 50))
        {
            throw new BusinessException("商品名称长度不能超过50个字符");
        }
        if (ValidationHelper.IsExceedLength(specification, 30))
        {
            throw new BusinessException("规格长度不能超过30个字符");
        }
        if (unitPrice <= 0)
        {
            throw new BusinessException("单价必须大于0");
        }
        if (stockQuantity < 0)
        {
            throw new BusinessException("库存数量不能为负数");
        }
        if (ValidationHelper.IsNullOrWhiteSpace(categoryID))
        {
            throw new BusinessException("请选择商品类别");
        }
        if (_categoryDAL.GetCategoryByID(categoryID) == null)
        {
            throw new BusinessException("商品类别不存在");
        }
        if (ValidationHelper.IsNullOrWhiteSpace(supplierID))
        {
            throw new BusinessException("请选择供货商");
        }
        if (_supplierDAL.GetSupplierByID(supplierID) == null)
        {
            throw new BusinessException("供货商不存在");
        }
        if (ValidationHelper.IsExceedLength(origin, 50))
        {
            throw new BusinessException("产地长度不能超过50个字符");
        }

        Product product = new()
        {
            ProductID = productID,
            ProductName = productName,
            Specification = specification,
            UnitPrice = unitPrice,
            StockQuantity = stockQuantity,
            CategoryID = categoryID,
            SupplierID = supplierID,
            Origin = string.IsNullOrEmpty(origin) ? null : origin,
            ProductionDate = productionDate
        };

        if (!_productDAL.InsertProduct(product))
        {
            throw new BusinessException("商品编号已存在");
        }
    }

    /// <summary>
    /// 修改商品（编号不可改）
    /// </summary>
    /// <exception cref="BusinessException">校验失败</exception>
    public void UpdateProduct(string productID, string productName, string specification,
        decimal unitPrice, int stockQuantity, string categoryID, string supplierID,
        string origin, DateTime? productionDate)
    {
        if (_productDAL.GetProductByID(productID) == null)
        {
            throw new BusinessException("商品不存在");
        }
        if (ValidationHelper.IsNullOrWhiteSpace(productName))
        {
            throw new BusinessException("商品名称不能为空");
        }
        if (ValidationHelper.IsExceedLength(productName, 50))
        {
            throw new BusinessException("商品名称长度不能超过50个字符");
        }
        if (ValidationHelper.IsExceedLength(specification, 30))
        {
            throw new BusinessException("规格长度不能超过30个字符");
        }
        if (unitPrice <= 0)
        {
            throw new BusinessException("单价必须大于0");
        }
        if (stockQuantity < 0)
        {
            throw new BusinessException("库存数量不能为负数");
        }
        if (_categoryDAL.GetCategoryByID(categoryID) == null)
        {
            throw new BusinessException("商品类别不存在");
        }
        if (_supplierDAL.GetSupplierByID(supplierID) == null)
        {
            throw new BusinessException("供货商不存在");
        }
        if (ValidationHelper.IsExceedLength(origin, 50))
        {
            throw new BusinessException("产地长度不能超过50个字符");
        }

        Product product = new()
        {
            ProductID = productID,
            ProductName = productName,
            Specification = specification,
            UnitPrice = unitPrice,
            StockQuantity = stockQuantity,
            CategoryID = categoryID,
            SupplierID = supplierID,
            Origin = string.IsNullOrEmpty(origin) ? null : origin,
            ProductionDate = productionDate
        };

        _productDAL.UpdateProduct(product);
    }

    /// <summary>
    /// 删除商品（存在订单明细记录时拒绝删除）
    /// </summary>
    /// <param name="productID">商品编号</param>
    /// <exception cref="BusinessException">校验失败</exception>
    public void DeleteProduct(string productID)
    {
        if (_productDAL.GetProductByID(productID) == null)
        {
            throw new BusinessException("商品不存在");
        }

        // 删除前检查是否存在订单明细记录，保证数据引用完整性
        if (_productDAL.HasOrderItems(productID))
        {
            throw new BusinessException("存在订单明细记录，无法删除");
        }

        if (!_productDAL.DeleteProduct(productID))
        {
            throw new BusinessException("删除失败");
        }
    }

    /// <summary>
    /// 根据商品编号查询（供订单模块使用）
    /// </summary>
    /// <param name="productID">商品编号</param>
    /// <returns>商品实体，未找到返回 null</returns>
    public Product GetProductByID(string productID)
    {
        return _productDAL.GetProductByID(productID);
    }
}
