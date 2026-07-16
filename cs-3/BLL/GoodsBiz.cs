using CampusMart.Common;
using CampusMart.DAL;
using CampusMart.Models;

namespace CampusMart.BLL;

/// <summary>
/// 商品业务逻辑类
/// </summary>
/// <remarks>
/// 与 cs-2 的 ProductService 差异化命名（GoodsBiz）。
/// </remarks>
public class GoodsBiz
{
    private readonly GoodsDao _goodsDao = new();

    /// <summary>
    /// 查询全部商品（含类别名、供货商名）
    /// </summary>
    /// <returns>商品列表</returns>
    public List<GoodsInfo> GetAllGoods()
    {
        return _goodsDao.GetAllGoods();
    }

    /// <summary>
    /// 按多条件组合检索商品
    /// </summary>
    /// <param name="productName">商品名称关键字（空则不限）</param>
    /// <param name="categoryID">类别编号（0 表示不限）</param>
    /// <param name="supplierID">供货商编号（0 表示不限）</param>
    /// <returns>匹配的商品列表</returns>
    public List<GoodsInfo> SearchGoods(string productName, int categoryID, int supplierID)
    {
        return _goodsDao.SearchGoods(productName, categoryID, supplierID);
    }

    /// <summary>
    /// 新增商品
    /// </summary>
    /// <param name="goods">商品实体</param>
    /// <exception cref="BusinessException">校验失败</exception>
    public void AddGoods(GoodsInfo goods)
    {
        ValidateGoods(goods);
        _goodsDao.InsertGoods(goods);
    }

    /// <summary>
    /// 修改商品信息
    /// </summary>
    /// <param name="goods">商品实体</param>
    /// <exception cref="BusinessException">校验失败</exception>
    public void UpdateGoods(GoodsInfo goods)
    {
        if (_goodsDao.GetGoodsByID(goods.ProductID) == null)
        {
            throw new BusinessException("商品不存在");
        }
        ValidateGoods(goods);
        _goodsDao.UpdateGoods(goods);
    }

    /// <summary>
    /// 删除商品（存在订单明细记录时拒绝删除）
    /// </summary>
    /// <param name="productID">商品编号</param>
    /// <exception cref="BusinessException">存在订单记录</exception>
    public void DeleteGoods(int productID)
    {
        if (_goodsDao.HasOrderItems(productID))
        {
            throw new BusinessException("存在订单记录，无法删除");
        }

        _goodsDao.DeleteGoods(productID);
    }

    /// <summary>
    /// 商品字段校验（供新增和修改共用）
    /// </summary>
    /// <param name="goods">商品实体</param>
    /// <exception cref="BusinessException">校验失败</exception>
    private static void ValidateGoods(GoodsInfo goods)
    {
        if (ValidateUtil.IsNullOrWhiteSpace(goods.ProductName))
        {
            throw new BusinessException("商品名称不能为空");
        }
        if (ValidateUtil.IsExceedLength(goods.ProductName, 50))
        {
            throw new BusinessException("商品名称长度不能超过50个字符");
        }
        if (goods.CategoryID <= 0)
        {
            throw new BusinessException("请选择商品类别");
        }
        if (goods.SupplierID <= 0)
        {
            throw new BusinessException("请选择供货商");
        }
        if (goods.UnitPrice <= 0)
        {
            throw new BusinessException("单价必须大于0");
        }
        if (ValidateUtil.IsExceedLength(goods.Origin, 50))
        {
            throw new BusinessException("产地长度不能超过50个字符");
        }
        if (goods.StockQty < 0)
        {
            throw new BusinessException("库存数量不能为负数");
        }
    }
}
