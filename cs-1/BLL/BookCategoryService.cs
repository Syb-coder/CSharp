using LibraryManagement.Common;
using LibraryManagement.DAL;
using LibraryManagement.Models;

namespace LibraryManagement.BLL;

/// <summary>
/// 图书类别业务服务类，处理类别增删改查及关联检查
/// </summary>
public class BookCategoryService
{
    private readonly BookCategoryDAL _categoryDAL = new();

    /// <summary>
    /// 查询全部图书类别
    /// </summary>
    /// <returns>类别列表</returns>
    public List<BookCategory> GetAllCategories()
    {
        return _categoryDAL.GetAllCategories();
    }

    /// <summary>
    /// 按名称关键字查询类别
    /// </summary>
    /// <param name="keyword">关键字（可空）</param>
    /// <returns>类别列表</returns>
    public List<BookCategory> SearchCategories(string keyword)
    {
        return _categoryDAL.SearchCategories(keyword);
    }

    /// <summary>
    /// 新增图书类别
    /// </summary>
    /// <param name="categoryID">类别编号</param>
    /// <param name="categoryName">类别名称</param>
    /// <exception cref="BusinessException">校验失败</exception>
    public void AddCategory(string categoryID, string categoryName)
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

        BookCategory category = new()
        {
            CategoryID = categoryID,
            CategoryName = categoryName
        };

        if (!_categoryDAL.InsertCategory(category))
        {
            throw new BusinessException("类别编号已存在");
        }
    }

    /// <summary>
    /// 修改类别名称（编号不可修改）
    /// </summary>
    /// <param name="categoryID">类别编号</param>
    /// <param name="categoryName">新类别名称</param>
    /// <exception cref="BusinessException">校验失败</exception>
    public void UpdateCategory(string categoryID, string categoryName)
    {
        if (ValidationHelper.IsNullOrWhiteSpace(categoryName))
        {
            throw new BusinessException("类别名称不能为空");
        }
        if (ValidationHelper.IsExceedLength(categoryName, 20))
        {
            throw new BusinessException("类别名称长度不能超过20个字符");
        }

        if (!_categoryDAL.UpdateCategory(categoryID, categoryName))
        {
            throw new BusinessException("类别不存在");
        }
    }

    /// <summary>
    /// 删除图书类别（存在关联图书时不允许删除）
    /// </summary>
    /// <param name="categoryID">类别编号</param>
    /// <exception cref="BusinessException">存在关联图书或删除失败</exception>
    public void DeleteCategory(string categoryID)
    {
        int bookCount = _categoryDAL.CountBooksByCategory(categoryID);
        if (bookCount > 0)
        {
            throw new BusinessException($"存在关联图书（{bookCount}本），无法删除");
        }

        if (!_categoryDAL.DeleteCategory(categoryID))
        {
            throw new BusinessException("类别不存在或已被删除");
        }
    }
}
