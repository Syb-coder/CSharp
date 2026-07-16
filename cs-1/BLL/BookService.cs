using LibraryManagement.Common;
using LibraryManagement.DAL;
using LibraryManagement.Models;

namespace LibraryManagement.BLL;

/// <summary>
/// 图书业务服务类，处理图书增删改查及关联检查
/// </summary>
public class BookService
{
    private readonly BookDAL _bookDAL = new();

    /// <summary>
    /// 查询全部图书
    /// </summary>
    /// <returns>图书列表</returns>
    public List<Book> GetAllBooks()
    {
        return _bookDAL.GetAllBooks();
    }

    /// <summary>
    /// 多条件查询图书
    /// </summary>
    /// <param name="bookName">书名（可空）</param>
    /// <param name="author">作者（可空）</param>
    /// <param name="categoryID">类别编号（可空）</param>
    /// <param name="isbn">ISBN（可空）</param>
    /// <returns>图书列表</returns>
    public List<Book> SearchBooks(string bookName, string author, string categoryID, string isbn)
    {
        return _bookDAL.SearchBooks(bookName, author, categoryID, isbn);
    }

    /// <summary>
    /// 新增图书
    /// </summary>
    /// <param name="book">图书实体</param>
    /// <exception cref="BusinessException">校验失败</exception>
    public void AddBook(Book book)
    {
        ValidateBook(book);

        // 新增时可借数量等于馆藏数量：刚入库的图书尚未被借出
        book.AvailableCount = book.TotalCount;

        if (!_bookDAL.InsertBook(book))
        {
            throw new BusinessException("图书编号已存在");
        }
    }

    /// <summary>
    /// 修改图书信息
    /// </summary>
    /// <param name="book">图书实体</param>
    /// <exception cref="BusinessException">校验失败</exception>
    public void UpdateBook(Book book)
    {
        ValidateBook(book);

        // 修改时需重新计算可借数量：馆藏总数可能被调整，但已借出数量是历史事实不应丢失
        Book existingBook;
        try
        {
            existingBook = _bookDAL.GetBookById(book.BookID)
                ?? throw new BusinessException("图书不存在");
        }
        catch (BusinessException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new BusinessException($"[查询阶段] {ex.Message}");
        }

        // 已借出数量 = 原馆藏 - 原可借，这个差值反映了当前未归还的借阅记录数
        int borrowedCount = existingBook.TotalCount - existingBook.AvailableCount;
        // 新可借数量 = 新馆藏 - 已借出数量，保留借出历史不被覆盖
        book.AvailableCount = book.TotalCount - borrowedCount;

        if (book.AvailableCount < 0)
        {
            // 新馆藏数量小于已借出数量，逻辑上不可能（不能让已借出的书"消失"），必须拦截
            throw new BusinessException($"馆藏数量不能小于当前借出数量（{borrowedCount}本）");
        }

        try
        {
            if (!_bookDAL.UpdateBook(book))
            {
                throw new BusinessException("图书不存在");
            }
        }
        catch (BusinessException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new BusinessException($"[更新阶段] {ex.Message}");
        }
    }

    /// <summary>
    /// 删除图书（存在未归还借阅时不允许删除）
    /// </summary>
    /// <param name="bookID">图书编号</param>
    /// <exception cref="BusinessException">存在未归还借阅或删除失败</exception>
    public void DeleteBook(string bookID)
    {
        // 参照完整性保护：存在未归还借阅记录的图书不允许删除
        // 否则借阅记录中的 bookID 将变成悬空引用，还书时无法恢复库存
        int unreturnedCount = _bookDAL.CountUnreturnedBorrows(bookID);
        if (unreturnedCount > 0)
        {
            throw new BusinessException($"存在未归还借阅（{unreturnedCount}条），无法删除");
        }

        if (!_bookDAL.DeleteBook(bookID))
        {
            throw new BusinessException("图书不存在或已被删除");
        }
    }

    /// <summary>
    /// 校验图书字段合法性
    /// </summary>
    /// <param name="book">图书实体</param>
    /// <exception cref="BusinessException">校验失败</exception>
    private static void ValidateBook(Book book)
    {
        // 校验顺序：必填字段非空 -> 长度限制 -> 可选字段长度 -> 业务值范围
        // 前置校验失败立即返回，避免后续校验对非法值操作
        if (ValidationHelper.IsNullOrWhiteSpace(book.BookID))
        {
            throw new BusinessException("图书编号不能为空");
        }
        if (ValidationHelper.IsExceedLength(book.BookID, 20))
        {
            throw new BusinessException("图书编号长度不能超过20个字符");
        }
        if (ValidationHelper.IsNullOrWhiteSpace(book.BookName))
        {
            throw new BusinessException("书名不能为空");
        }
        if (ValidationHelper.IsExceedLength(book.BookName, 100))
        {
            throw new BusinessException("书名长度不能超过100个字符");
        }
        if (!ValidationHelper.IsNullOrWhiteSpace(book.Author)
            && ValidationHelper.IsExceedLength(book.Author, 50))
        {
            throw new BusinessException("作者长度不能超过50个字符");
        }
        if (!ValidationHelper.IsNullOrWhiteSpace(book.Publisher)
            && ValidationHelper.IsExceedLength(book.Publisher, 50))
        {
            throw new BusinessException("出版社长度不能超过50个字符");
        }
        if (!ValidationHelper.IsNullOrWhiteSpace(book.ISBN)
            && ValidationHelper.IsExceedLength(book.ISBN, 13))
        {
            throw new BusinessException("ISBN长度不能超过13个字符");
        }
        if (book.Price.HasValue && book.Price.Value <= 0)
        {
            throw new BusinessException("价格必须大于0");
        }
        if (ValidationHelper.IsNullOrWhiteSpace(book.CategoryID))
        {
            throw new BusinessException("请选择图书类别");
        }
        if (book.TotalCount < 0)
        {
            throw new BusinessException("馆藏数量不能为负数");
        }
    }
}
