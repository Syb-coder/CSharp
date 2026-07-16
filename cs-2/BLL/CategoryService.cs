using CampusShop.Common;
using CampusShop.DAL;
using CampusShop.Models;

namespace CampusShop.BLL;

/// <summary>
/// 商品类别业务服务类，处理类别的增删改查及关联性校验
/// </summary>
public class CategoryService
{
    private readonly CategoryDAL _categoryDAL = new();

    /// <summary>
    /// 查询全部类别
    /// </summary>
    /// <returns>类别列表</returns>
    public List<Category> GetAllCategories()
    {
        return _categoryDAL.GetAllCategories();
    }

    /// <summary>
    /// 按名称关键字检索
    /// </summary>
    /// <param name="keyword">名称关键字</param>
    /// <returns>类别列表</returns>
    public List<Category> SearchByName(string keyword)
    {
        return _categoryDAL.SearchByName(keyword);
    }

    /// <summary>
    /// 新增类别
    /// </summary>
    /// <param name="categoryID">类别编号</param>
    /// <param name="categoryName">类别名称</param>
    /// <param name="parentCategoryID">父类别编号（可为空）</param>
    /// <param name="categoryDesc">类别描述（可为空）</param>
    /// <exception cref="BusinessException">校验失败</exception>
    public void AddCategory(string categoryID, string categoryName, string parentCategoryID, string categoryDesc)
    {
        if (ValidationHelper.IsNullOrWhiteSpace(categoryID))
        {
            throw new BusinessException("类别编号不能为空");
        }
        if (ValidationHelper.IsExceedLength(categoryID, 10))
        {
            throw new BusinessException("类别编号长度不能超过10个字符");
        }
        if (ValidationHelper.IsNullOrWhiteSpace(categoryName))
        {
            throw new BusinessException("类别名称不能为空");
        }
        if (ValidationHelper.IsExceedLength(categoryName, 20))
        {
            throw new BusinessException("类别名称长度不能超过20个字符");
        }
        if (ValidationHelper.IsExceedLength(categoryDesc, 100))
        {
            throw new BusinessException("类别描述长度不能超过100个字符");
        }

        // 父类别编号非空时校验其存在性，防止引用不存在的父类别
        if (!string.IsNullOrEmpty(parentCategoryID))
        {
            if (_categoryDAL.GetCategoryByID(parentCategoryID) == null)
            {
                throw new BusinessException("父类别编号不存在");
            }
        }

        Category category = new()
        {
            CategoryID = categoryID,
            CategoryName = categoryName,
            ParentCategoryID = string.IsNullOrEmpty(parentCategoryID) ? null : parentCategoryID,
            CategoryDesc = string.IsNullOrEmpty(categoryDesc) ? null : categoryDesc,
            AddTime = DateTime.Now
        };

        if (!_categoryDAL.InsertCategory(category))
        {
            throw new BusinessException("类别编号已存在");
        }
    }

    /// <summary>
    /// 修改类别（编号不可改）
    /// </summary>
    /// <param name="categoryID">类别编号</param>
    /// <param name="categoryName">类别名称</param>
    /// <param name="parentCategoryID">父类别编号（可为空）</param>
    /// <param name="categoryDesc">类别描述（可为空）</param>
    /// <exception cref="BusinessException">校验失败</exception>
    public void UpdateCategory(string categoryID, string categoryName, string parentCategoryID, string categoryDesc)
    {
        if (_categoryDAL.GetCategoryByID(categoryID) == null)
        {
            throw new BusinessException("类别不存在");
        }
        if (ValidationHelper.IsNullOrWhiteSpace(categoryName))
        {
            throw new BusinessException("类别名称不能为空");
        }
        if (ValidationHelper.IsExceedLength(categoryName, 20))
        {
            throw new BusinessException("类别名称长度不能超过20个字符");
        }
        if (ValidationHelper.IsExceedLength(categoryDesc, 100))
        {
            throw new BusinessException("类别描述长度不能超过100个字符");
        }

        // 防止将类别的父类别设为自身，造成自引用循环
        if (!string.IsNullOrEmpty(parentCategoryID))
        {
            if (parentCategoryID == categoryID)
            {
                throw new BusinessException("不能将类别的父类别设为自身");
            }
            if (_categoryDAL.GetCategoryByID(parentCategoryID) == null)
            {
                throw new BusinessException("父类别编号不存在");
            }
        }

        Category category = new()
        {
            CategoryID = categoryID,
            CategoryName = categoryName,
            ParentCategoryID = string.IsNullOrEmpty(parentCategoryID) ? null : parentCategoryID,
            CategoryDesc = string.IsNullOrEmpty(categoryDesc) ? null : categoryDesc
        };

        _categoryDAL.UpdateCategory(category);
    }

    /// <summary>
    /// 删除类别（存在关联商品时拒绝删除）
    /// </summary>
    /// <param name="categoryID">类别编号</param>
    /// <exception cref="BusinessException">校验失败</exception>
    public void DeleteCategory(string categoryID)
    {
        if (_categoryDAL.GetCategoryByID(categoryID) == null)
        {
            throw new BusinessException("类别不存在");
        }

        // 删除前检查是否存在关联商品，保证数据引用完整性
        if (_categoryDAL.HasProducts(categoryID))
        {
            throw new BusinessException("存在关联商品，无法删除");
        }

        if (!_categoryDAL.DeleteCategory(categoryID))
        {
            throw new BusinessException("删除失败");
        }
    }
}
