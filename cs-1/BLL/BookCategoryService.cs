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
    /// <param name="borrowDays">可借阅天数</param>
    /// <param name="finePerDay">单日逾期罚款标准（元/天）</param>
    /// <exception cref="BusinessException">校验失败</exception>
    public void AddCategory(string categoryID, string categoryName, int borrowDays, decimal finePerDay)
    {
        // 校验顺序设计：先校验非空再校验长度，避免对 null 值调用长度检查导致异常
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
        // 借阅天数必须为正整数
        if (borrowDays <= 0)
        {
            throw new BusinessException("可借阅天数必须大于0");
        }
        // 罚款标准必须为正数
        if (finePerDay <= 0)
        {
            throw new BusinessException("单日逾期罚款标准必须大于0");
        }

        BookCategory category = new()
        {
            CategoryID = categoryID,
            CategoryName = categoryName,
            BorrowDays = borrowDays,
            FinePerDay = finePerDay
        };

        // InsertCategory 返回 false 表示主键冲突（编号已存在），转换为业务异常提示用户
        if (!_categoryDAL.InsertCategory(category))
        {
            throw new BusinessException("类别编号已存在");
        }
    }

    /// <summary>
    /// 修改类别信息（编号不可修改）
    /// </summary>
    /// <param name="categoryID">类别编号</param>
    /// <param name="categoryName">新类别名称</param>
    /// <param name="borrowDays">新可借阅天数</param>
    /// <param name="finePerDay">新单日逾期罚款标准</param>
    /// <exception cref="BusinessException">校验失败</exception>
    public void UpdateCategory(string categoryID, string categoryName, int borrowDays, decimal finePerDay)
    {
        if (ValidationHelper.IsNullOrWhiteSpace(categoryName))
        {
            throw new BusinessException("类别名称不能为空");
        }
        if (ValidationHelper.IsExceedLength(categoryName, 20))
        {
            throw new BusinessException("类别名称长度不能超过20个字符");
        }
        // 借阅天数必须为正整数
        if (borrowDays <= 0)
        {
            throw new BusinessException("可借阅天数必须大于0");
        }
        // 罚款标准必须为正数
        if (finePerDay <= 0)
        {
            throw new BusinessException("单日逾期罚款标准必须大于0");
        }

        if (!_categoryDAL.UpdateCategory(categoryID, categoryName, borrowDays, finePerDay))
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
        // 参照完整性保护：删除前检查是否有图书关联到此类别
        // 若直接物理删除，关联图书的 categoryID 将变为无效引用（悬空外键），导致查询异常
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
