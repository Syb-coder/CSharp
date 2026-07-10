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

        // 新增时可借数量等于馆藏数量
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

        // 修改时需保证可借数量不超过馆藏数量
        Book existingBook = _bookDAL.GetBookById(book.BookID)
            ?? throw new BusinessException("图书不存在");

        int borrowedCount = existingBook.TotalCount - existingBook.AvailableCount;
        book.AvailableCount = book.TotalCount - borrowedCount;

        if (book.AvailableCount < 0)
        {
            throw new BusinessException($"馆藏数量不能小于当前借出数量（{borrowedCount}本）");
        }

        if (!_bookDAL.UpdateBook(book))
        {
            throw new BusinessException("图书不存在");
        }
    }

    /// <summary>
    /// 删除图书（存在未归还借阅时不允许删除）
    /// </summary>
    /// <param name="bookID">图书编号</param>
    /// <exception cref="BusinessException">存在未归还借阅或删除失败</exception>
    public void DeleteBook(string bookID)
    {
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
