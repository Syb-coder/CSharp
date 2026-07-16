using LibrarySys.DAL;
using LibrarySys.Models;

namespace LibrarySys.BLL;

/// <summary>
/// 图书业务逻辑类
/// </summary>
public class BookBiz
{
    private readonly BookDao _dao = new();

    /// <summary>查询全部图书</summary>
    public List<BookInfo> GetAll() => _dao.FindAll();

    /// <summary>按编号查询图书</summary>
    public BookInfo GetById(string bookID) => _dao.FindById(bookID);

    /// <summary>
    /// 新增图书
    /// </summary>
    /// <param name="entity">图书实体</param>
    /// <exception cref="BusinessException">编号已存在 / 必填项为空 / 馆藏数量非法</exception>
    public void Add(BookInfo entity)
    {
        // 必填项校验
        if (string.IsNullOrWhiteSpace(entity.BookID))
            throw new BusinessException("图书编号不能为空");
        if (string.IsNullOrWhiteSpace(entity.BookName))
            throw new BusinessException("书名不能为空");
        if (string.IsNullOrWhiteSpace(entity.TypeID))
            throw new BusinessException("请选择图书类型");
        if (entity.TotalCount < 0)
            throw new BusinessException("馆藏数量不能为负数");
        if (entity.Price.HasValue && entity.Price <= 0)
            throw new BusinessException("价格必须大于 0");

        // 唯一性校验
        if (_dao.FindById(entity.BookID) != null)
            throw new BusinessException($"图书编号 {entity.BookID} 已存在");

        _dao.Insert(entity);
    }

    /// <summary>
    /// 修改图书信息
    /// </summary>
    /// <param name="entity">图书实体</param>
    /// <exception cref="BusinessException">图书不存在 / 必填项为空</exception>
    public void Update(BookInfo entity)
    {
        if (string.IsNullOrWhiteSpace(entity.BookName))
            throw new BusinessException("书名不能为空");
        if (entity.TotalCount < 0)
            throw new BusinessException("馆藏数量不能为负数");
        if (entity.Price.HasValue && entity.Price <= 0)
            throw new BusinessException("价格必须大于 0");
        if (_dao.FindById(entity.BookID) == null)
            throw new BusinessException($"图书编号 {entity.BookID} 不存在");

        _dao.Update(entity);
    }

    /// <summary>
    /// 删除图书（业务校验后级联删除历史记录）
    /// 硬约束：有未归还借阅、未缴罚款、活跃预约时禁止删除
    /// 历史记录（已还借阅、已缴罚款、已完成/取消预约）在事务中一并删除
    /// </summary>
    /// <param name="bookID">图书编号</param>
    /// <exception cref="BusinessException">存在业务约束导致无法删除</exception>
    public void Delete(string bookID)
    {
        if (_dao.HasActiveBorrow(bookID))
            throw new BusinessException("该图书有未归还的借阅记录，无法删除");
        if (_dao.HasUnpaidFine(bookID))
            throw new BusinessException("该图书存在未缴清的罚款记录，无法删除");
        if (_dao.HasActiveReservation(bookID))
            throw new BusinessException("该图书有进行中的预约（排队中/待取书），请先取消预约再删除");

        _dao.DeleteCascade(bookID);
    }

    /// <summary>按书名、类型、出版社多条件组合查询</summary>
    public List<BookInfo> Search(string bookName, string typeID, string publisher)
        => _dao.Search(bookName, typeID, publisher);
}
