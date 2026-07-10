using Microsoft.Data.SqlClient;
using LibraryManagement.DAL;
using LibraryManagement.Models;

namespace LibraryManagement.BLL;

/// <summary>
/// 借阅业务服务类，处理借书、还书、查询等业务逻辑
/// 借还书操作在数据库事务中完成，保证借阅记录与图书库存的原子性
/// </summary>
public class BorrowService
{
    private readonly BorrowDAL _borrowDAL = new();
    private readonly BookDAL _bookDAL = new();
    private readonly ReaderDAL _readerDAL = new();

    /// <summary>
    /// 查询全部借阅记录
    /// </summary>
    /// <returns>借阅记录列表</returns>
    public List<BorrowRecord> GetAllBorrows()
    {
        return _borrowDAL.GetAllBorrows();
    }

    /// <summary>
    /// 多条件查询借阅记录
    /// </summary>
    /// <param name="readerID">读者编号（可空）</param>
    /// <param name="bookID">图书编号（可空）</param>
    /// <param name="status">状态（可空）</param>
    /// <param name="startDate">借出日期起始（可空）</param>
    /// <param name="endDate">借出日期截止（可空）</param>
    /// <returns>借阅记录列表</returns>
    public List<BorrowRecord> SearchBorrows(string readerID, string bookID, string status, DateTime? startDate, DateTime? endDate)
    {
        return _borrowDAL.SearchBorrows(readerID, bookID, status, startDate, endDate);
    }

    /// <summary>
    /// 办理借书（事务操作：插入借阅记录 + 扣减可借数量）
    /// </summary>
    /// <param name="readerID">读者编号</param>
    /// <param name="bookID">图书编号</param>
    /// <exception cref="BusinessException">校验失败</exception>
    public void BorrowBook(string readerID, string bookID)
    {
        // 前置校验：读者是否存在
        Reader reader = _readerDAL.GetReaderById(readerID)
            ?? throw new BusinessException("读者不存在");

        // 前置校验：图书是否存在
        Book book = _bookDAL.GetBookById(bookID)
            ?? throw new BusinessException("图书不存在");

        // 前置校验：可借数量是否大于0
        if (book.AvailableCount <= 0)
        {
            throw new BusinessException("该图书已全部借出，无法借阅");
        }

        // 前置校验：读者是否有超期未还图书（限制继续借书）
        if (_borrowDAL.HasOverdueBorrow(readerID))
        {
            throw new BusinessException("该读者有超期未还图书，请先归还");
        }

        // 前置校验：是否重复借阅同一本未归还的图书
        if (_borrowDAL.ExistsUnreturnedBorrow(readerID, bookID))
        {
            throw new BusinessException("该读者已借阅此书且未归还");
        }

        // 前置校验：读者借阅数量是否达到上限
        int currentBorrowCount = _borrowDAL.CountBorrowingByReader(readerID);
        if (currentBorrowCount >= BusinessConstants.MAX_BORROW_LIMIT)
        {
            throw new BusinessException($"读者已达到借阅上限（{BusinessConstants.MAX_BORROW_LIMIT}本），请先归还");
        }

        // 构造借阅记录
        DateTime borrowDate = DateTime.Today;
        DateTime dueDate = borrowDate.AddDays(BusinessConstants.DEFAULT_BORROW_DAYS);
        BorrowRecord record = new()
        {
            ReaderID = readerID,
            BookID = bookID,
            BorrowDate = borrowDate,
            DueDate = dueDate,
            Status = BusinessConstants.STATUS_BORROWED
        };

        // 在事务中执行：插入借阅记录 + 扣减可借数量
        using SqlConnection conn = new(DBConnection.GetConnectionString());
        conn.Open();
        using SqlTransaction transaction = conn.BeginTransaction();
        try
        {
            // 插入借阅记录
            if (!_borrowDAL.InsertBorrow(conn, transaction, record))
            {
                throw new BusinessException("插入借阅记录失败");
            }

            // 扣减可借数量（受 availableCount > 0 条件保护，并发安全）
            if (!_bookDAL.DecreaseAvailableCount(conn, transaction, bookID))
            {
                throw new BusinessException("图书可借数量不足，借阅失败（可能已被并发借出）");
            }

            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    /// <summary>
    /// 办理还书（事务操作：更新归还信息 + 恢复可借数量）
    /// </summary>
    /// <param name="borrowID">借阅编号</param>
    /// <returns>逾期天数（0表示未逾期，正数表示逾期天数）</returns>
    /// <exception cref="BusinessException">校验失败</exception>
    public int ReturnBook(int borrowID)
    {
        // 查询借阅记录
        List<BorrowRecord> records = _borrowDAL.SearchBorrows(null, null, BusinessConstants.STATUS_BORROWED, null, null);
        BorrowRecord record = records.FirstOrDefault(r => r.BorrowID == borrowID)
            ?? throw new BusinessException("借阅记录不存在或已归还");

        DateTime returnDate = DateTime.Today;
        int overdueDays = 0;

        // 计算逾期天数：归还日期 > 应还日期时为逾期
        if (returnDate > record.DueDate)
        {
            overdueDays = (returnDate - record.DueDate).Days;
        }

        // 在事务中执行：更新归还信息 + 恢复可借数量
        using SqlConnection conn = new(DBConnection.GetConnectionString());
        conn.Open();
        using SqlTransaction transaction = conn.BeginTransaction();
        try
        {
            // 更新借阅记录状态为已还
            if (!_borrowDAL.UpdateReturn(conn, transaction, borrowID, returnDate))
            {
                throw new BusinessException("更新归还信息失败");
            }

            // 恢复可借数量
            if (!_bookDAL.IncreaseAvailableCount(conn, transaction, record.BookID))
            {
                throw new BusinessException("恢复图书可借数量失败");
            }

            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }

        return overdueDays;
    }
}
