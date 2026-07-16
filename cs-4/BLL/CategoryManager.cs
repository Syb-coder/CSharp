using System.Data;
using CampusStore.Common;
using CampusStore.DAL;

namespace CampusStore.BLL;

/// <summary>
/// 商品类别业务逻辑类
/// </summary>
public class CategoryManager
{
    private readonly CategoryRepository _repo = new();

    /// <summary>
    /// 添加类别
    /// </summary>
    public int Add(string categoryName, string categoryDesc, int sortOrder)
    {
        if (ValidateUtil.IsNullOrWhiteSpace(categoryName))
            throw new BusinessException("请输入类别名称");
        if (ValidateUtil.IsExceedLength(categoryName, 20))
            throw new BusinessException("类别名称长度不能超过 20 个字符");
        if (ValidateUtil.IsExceedLength(categoryDesc, 100))
            throw new BusinessException("类别描述长度不能超过 100 个字符");

        return _repo.Add(categoryName, categoryDesc, sortOrder);
    }

    /// <summary>
    /// 修改类别
    /// </summary>
    public int Update(int categoryID, string categoryName, string categoryDesc, int sortOrder)
    {
        if (categoryID <= 0)
            throw new BusinessException("请选择要修改的类别");
        if (ValidateUtil.IsNullOrWhiteSpace(categoryName))
            throw new BusinessException("请输入类别名称");
        if (ValidateUtil.IsExceedLength(categoryName, 20))
            throw new BusinessException("类别名称长度不能超过 20 个字符");
        if (ValidateUtil.IsExceedLength(categoryDesc, 100))
            throw new BusinessException("类别描述长度不能超过 100 个字符");

        return _repo.Update(categoryID, categoryName, categoryDesc, sortOrder);
    }

    /// <summary>
    /// 删除类别
    /// </summary>
    /// <exception cref="BusinessException">存在关联商品时抛出</exception>
    public int Delete(int categoryID)
    {
        if (categoryID <= 0)
            throw new BusinessException("请选择要删除的类别");

        int result = _repo.Delete(categoryID);
        if (result == 0)
            throw new BusinessException("存在关联商品，无法删除");
        return result;
    }

    /// <summary>
    /// 查询全部类别（按排序号升序）
    /// </summary>
    public DataTable GetAll() => _repo.GetAll();

    /// <summary>
    /// 按名称关键字模糊检索
    /// </summary>
    public DataTable Search(string keyword) => _repo.Search(keyword);
}
