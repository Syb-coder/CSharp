using System.Data;
using CampusStore.Common;
using CampusStore.DAL;
using CampusStore.Models;

namespace CampusStore.BLL;

/// <summary>
/// 商品业务逻辑类
/// </summary>
public class ProductManager
{
    private readonly ProductRepository _repo = new();

    /// <summary>
    /// 添加商品
    /// </summary>
    public int Add(ProductInfo info)
    {
        ValidateProduct(info);
        return _repo.Add(info);
    }

    /// <summary>
    /// 修改商品
    /// </summary>
    public int Update(ProductInfo info)
    {
        if (info.ProductID <= 0)
            throw new BusinessException("请选择要修改的商品");
        ValidateProduct(info);
        return _repo.Update(info);
    }

    /// <summary>
    /// 删除商品
    /// </summary>
    /// <exception cref="BusinessException">存在订单明细时抛出</exception>
    public int Delete(int productID)
    {
        if (productID <= 0)
            throw new BusinessException("请选择要删除的商品");

        int result = _repo.Delete(productID);
        if (result == 0)
            throw new BusinessException("存在订单明细记录，无法删除");
        return result;
    }

    /// <summary>
    /// 查询全部商品（通过视图 v_Product_Detail）
    /// </summary>
    public DataTable GetAll() => _repo.GetAll();

    /// <summary>
    /// 按商品名称、类别、供货商组合条件检索
    /// </summary>
    public DataTable Search(string productName, int? categoryID, int? supplierID)
        => _repo.Search(productName, categoryID, supplierID);

    /// <summary>
    /// 按商品编号查询
    /// </summary>
    public ProductInfo GetByID(int productID) => _repo.GetByID(productID);

    /// <summary>
    /// 查询全部商品（实体列表，供下拉框使用）
    /// </summary>
    public List<ProductInfo> GetAllList() => _repo.GetAllList();

    /// <summary>
    /// 商品字段校验
    /// </summary>
    private static void ValidateProduct(ProductInfo info)
    {
        if (ValidateUtil.IsNullOrWhiteSpace(info.ProductName))
            throw new BusinessException("请输入商品名称");
        if (ValidateUtil.IsExceedLength(info.ProductName, 50))
            throw new BusinessException("商品名称长度不能超过 50 个字符");
        if (info.CategoryID <= 0)
            throw new BusinessException("请选择商品类别");
        if (info.SupplierID <= 0)
            throw new BusinessException("请选择供货商");
        if (!ValidateUtil.IsGreaterThanZero(info.UnitPrice))
            throw new BusinessException("单价必须大于 0");
        if (!ValidateUtil.IsNonNegative(info.StockQuantity))
            throw new BusinessException("库存数量不能为负数");
        if (ValidateUtil.IsExceedLength(info.Origin, 50))
            throw new BusinessException("产地长度不能超过 50 个字符");
        if (!ValidateUtil.IsNotFutureDate(info.ProduceDate))
            throw new BusinessException("生产日期不能晚于当前日期");
    }
}
