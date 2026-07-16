using CampusMart.Common;
using CampusMart.DAL;
using CampusMart.Models;

namespace CampusMart.BLL;

/// <summary>
/// 商品类别业务逻辑类（扁平结构）
/// </summary>
/// <remarks>
/// 与 cs-2 的 CategoryService 差异化命名（CategoryBiz）。
/// </remarks>
public class CategoryBiz
{
    private readonly CategoryDao _categoryDao = new();

    /// <summary>
    /// 查询全部商品类别
    /// </summary>
    /// <returns>类别列表</returns>
    public List<CategoryInfo> GetAllCategories()
    {
        return _categoryDao.GetAllCategories();
    }

    /// <summary>
    /// 按类别名称关键字检索
    /// </summary>
    /// <param name="keyword">类别名称关键字</param>
    /// <returns>匹配的类别列表</returns>
    public List<CategoryInfo> SearchByName(string keyword)
    {
        if (ValidateUtil.IsNullOrWhiteSpace(keyword))
        {
            return _categoryDao.GetAllCategories();
        }
        return _categoryDao.SearchByName(keyword);
    }

    /// <summary>
    /// 新增商品类别
    /// </summary>
    /// <param name="categoryName">类别名称</param>
    /// <param name="categoryDesc">类别描述</param>
    /// <exception cref="BusinessException">校验失败</exception>
    public void AddCategory(string categoryName, string categoryDesc)
    {
        if (ValidateUtil.IsNullOrWhiteSpace(categoryName))
        {
            throw new BusinessException("类别名称不能为空");
        }
        if (ValidateUtil.IsExceedLength(categoryName, 20))
        {
            throw new BusinessException("类别名称长度不能超过20个字符");
        }
        if (ValidateUtil.IsExceedLength(categoryDesc, 100))
        {
            throw new BusinessException("类别描述长度不能超过100个字符");
        }

        CategoryInfo category = new()
        {
            CategoryName = categoryName,
            CategoryDesc = categoryDesc
        };

        _categoryDao.InsertCategory(category);
    }

    /// <summary>
    /// 修改商品类别（类别编号为主键，不允许修改）
    /// </summary>
    /// <param name="categoryID">类别编号</param>
    /// <param name="categoryName">类别名称</param>
    /// <param name="categoryDesc">类别描述</param>
    /// <exception cref="BusinessException">校验失败</exception>
    public void UpdateCategory(int categoryID, string categoryName, string categoryDesc)
    {
        if (_categoryDao.GetCategoryByID(categoryID) == null)
        {
            throw new BusinessException("类别不存在");
        }
        if (ValidateUtil.IsNullOrWhiteSpace(categoryName))
        {
            throw new BusinessException("类别名称不能为空");
        }
        if (ValidateUtil.IsExceedLength(categoryName, 20))
        {
            throw new BusinessException("类别名称长度不能超过20个字符");
        }
        if (ValidateUtil.IsExceedLength(categoryDesc, 100))
        {
            throw new BusinessException("类别描述长度不能超过100个字符");
        }

        CategoryInfo category = new()
        {
            CategoryID = categoryID,
            CategoryName = categoryName,
            CategoryDesc = categoryDesc
        };

        _categoryDao.UpdateCategory(category);
    }

    /// <summary>
    /// 删除商品类别（存在关联商品时拒绝删除）
    /// </summary>
    /// <param name="categoryID">类别编号</param>
    /// <exception cref="BusinessException">存在关联商品或类别不存在</exception>
    public void DeleteCategory(int categoryID)
    {
        if (_categoryDao.GetCategoryByID(categoryID) == null)
        {
            throw new BusinessException("类别不存在");
        }
        if (_categoryDao.HasRelatedProducts(categoryID))
        {
            throw new BusinessException("存在关联商品，无法删除");
        }

        _categoryDao.DeleteCategory(categoryID);
    }
}
