using LibrarySys.DAL;
using LibrarySys.Models;

namespace LibrarySys.BLL;

/// <summary>
/// 图书类型业务逻辑类
/// </summary>
public class BookTypeBiz
{
    private readonly BookTypeDao _dao = new();

    /// <summary>查询全部图书类型</summary>
    public List<BookTypeInfo> GetAll() => _dao.FindAll();

    /// <summary>按编号查询图书类型</summary>
    public BookTypeInfo GetById(string typeID) => _dao.FindById(typeID);

    /// <summary>
    /// 新增图书类型
    /// </summary>
    /// <param name="entity">类型实体</param>
    /// <exception cref="BusinessException">编号已存在 / 编号或名称为空</exception>
    public void Add(BookTypeInfo entity)
    {
        // 卫语句：必填项校验，提前返回避免无效数据库操作
        if (string.IsNullOrWhiteSpace(entity.TypeID))
            throw new BusinessException("类型编号不能为空");
        if (string.IsNullOrWhiteSpace(entity.TypeName))
            throw new BusinessException("类型名称不能为空");

        // 唯一性校验：避免主键冲突
        if (_dao.FindById(entity.TypeID) != null)
            throw new BusinessException($"类型编号 {entity.TypeID} 已存在");

        _dao.Insert(entity);
    }

    /// <summary>
    /// 修改图书类型名称
    /// </summary>
    /// <param name="entity">类型实体</param>
    /// <exception cref="BusinessException">类型不存在 / 名称为空</exception>
    public void Update(BookTypeInfo entity)
    {
        if (string.IsNullOrWhiteSpace(entity.TypeName))
            throw new BusinessException("类型名称不能为空");
        if (_dao.FindById(entity.TypeID) == null)
            throw new BusinessException($"类型编号 {entity.TypeID} 不存在");

        _dao.Update(entity);
    }

    /// <summary>
    /// 删除图书类型（有关联图书时拦截）
    /// </summary>
    /// <param name="typeID">类型编号</param>
    /// <exception cref="BusinessException">该类型下有关联图书</exception>
    public void Delete(string typeID)
    {
        // 删除前校验：有关联图书则拦截，避免外键约束异常或孤儿数据
        if (_dao.HasBooks(typeID))
            throw new BusinessException("该类型下有关联图书，无法删除");

        _dao.Delete(typeID);
    }

    /// <summary>按名称关键字模糊查询</summary>
    public List<BookTypeInfo> Search(string keyword) => _dao.SearchByName(keyword);
}
