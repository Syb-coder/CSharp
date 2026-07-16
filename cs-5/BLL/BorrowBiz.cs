using Microsoft.Data.SqlClient;
using LibrarySys.DAL;
using LibrarySys.Models;
using LibrarySys.Common;
using System.Linq;

namespace LibrarySys.BLL;

/// <summary>
/// 图书借阅业务逻辑类（含借书/还书事务，cs-5 核心业务模块）
/// </summary>
public class BorrowBiz
{
    private readonly BorrowDao _borrowDao = new();
    private readonly ReaderDao _readerDao = new();
    private readonly BookDao _bookDao = new();
    private readonly FineDao _fineDao = new();

    /// <summary>查询全部借阅记录</summary>
    public List<BorrowInfo> GetAll() => _borrowDao.FindAll();

    /// <summary>按借阅编号查询</summary>
    public BorrowInfo GetById(int borrowID) => _borrowDao.FindById(borrowID);

    /// <summary>多条件查询借阅记录</summary>
    public List<BorrowInfo> Search(string readerID, string bookID, string status, DateTime? startDate, DateTime? endDate)
        => _borrowDao.Search(readerID, bookID, status, startDate, endDate);

    /// <summary>
    /// 借书核心逻辑（无日志版本，向后兼容，默认当天借出）
    /// </summary>
    public void Borrow(string readerID, string bookID) => Borrow(readerID, bookID, DateTime.Today, null);

    /// <summary>
    /// 借书核心逻辑（无日志版本，支持指定借书日期）
    /// </summary>
    public void Borrow(string readerID, string bookID, DateTime borrowDate) => Borrow(readerID, bookID, borrowDate, null);

    /// <summary>
    /// 借书核心逻辑（带日志，默认当天借出）
    /// </summary>
    /// <param name="readerID">读者编号</param>
    /// <param name="bookID">图书编号</param>
    /// <param name="userName">操作者用户名（null 时不记录日志）</param>
    /// <exception cref="BusinessException">读者/图书不存在 / 可借数量不足 / 有未缴罚款 / 有逾期未还 / 达到借阅上限 / 重复借阅</exception>
    public void Borrow(string readerID, string bookID, string? userName) => Borrow(readerID, bookID, DateTime.Today, userName);

    /// <summary>
    /// 借书核心逻辑：校验借阅资格 + 事务插入借阅记录并扣减库存 + 记录操作日志
    /// </summary>
    /// <param name="readerID">读者编号</param>
    /// <param name="bookID">图书编号</param>
    /// <param name="borrowDate">借书日期</param>
    /// <param name="userName">操作者用户名（null 时不记录日志）</param>
    /// <exception cref="BusinessException">读者/图书不存在 / 可借数量不足 / 有未缴罚款 / 有逾期未还 / 达到借阅上限 / 重复借阅</exception>
    public void Borrow(string readerID, string bookID, DateTime borrowDate, string? userName)
    {
        // ===== 1. 借阅资格校验 =====
        ReaderInfo reader = _readerDao.FindById(readerID)
            ?? throw new BusinessException($"读者编号 {readerID} 不存在");

        BookInfo book = _bookDao.FindById(bookID)
            ?? throw new BusinessException($"图书编号 {bookID} 不存在");
        if (book.AvailableCount <= 0)
            throw new BusinessException("该图书已全部借出，无法借阅");

        // 有逾期未还图书的读者禁止借书（PRD AC-10b）
        List<string> overdueBooks = _readerDao.GetOverdueBookNames(readerID);
        if (overdueBooks.Count > 0)
            throw new BusinessException($"该读者有逾期未还图书：{string.Join("、", overdueBooks)}，请先归还后再借书");

        // 有未缴清罚款的读者限制借书（PRD AC-10c）
        if (_readerDao.HasUnpaidFine(readerID))
        {
            List<FineInfo> unpaidFines = _fineDao.FindUnpaidByReader(readerID);
            decimal totalFine = unpaidFines.Sum(f => f.FineAmount);
            throw new BusinessException($"该读者有未缴清罚款共计 {totalFine:F2} 元（{unpaidFines.Count} 条），请先处理罚款后再借书");
        }

        // 借阅上限校验（PRD AC-07）
        int activeCount = _readerDao.CountActiveBorrow(readerID);
        if (activeCount >= BusinessConstants.MAX_BORROW_LIMIT)
            throw new BusinessException($"读者已达到借阅上限（{BusinessConstants.MAX_BORROW_LIMIT}本），请先归还");

        // 重复借阅校验：同一读者不能重复借阅同一本未归还的图书
        if (_borrowDao.FindActiveByReaderAndBook(readerID, bookID) != null)
            throw new BusinessException("该读者已借阅此书且未归还");

        // ===== 2. 事务执行：插入借阅记录 + 扣减可借数量 =====
        // 应还日期 = 借出日期 + 借阅期限（45 天）
        DateTime dueDate = borrowDate.AddDays(BusinessConstants.DEFAULT_BORROW_DAYS);

        using SqlConnection conn = new(DBConnection.GetConnectionString());
        conn.Open();
        using SqlTransaction tran = conn.BeginTransaction();
        try
        {
            // 插入借阅记录
            const string insertSql = @"INSERT INTO T_Borrow (readerID, bookID, borrowDate, dueDate, returnDate, status)
                                        VALUES (@readerID, @bookID, @borrowDate, @dueDate, NULL, @status)";
            using (SqlCommand cmd = new(insertSql, conn, tran))
            {
                cmd.Parameters.AddWithValue("@readerID", readerID);
                cmd.Parameters.AddWithValue("@bookID", bookID);
                cmd.Parameters.AddWithValue("@borrowDate", borrowDate);
                cmd.Parameters.AddWithValue("@dueDate", dueDate);
                cmd.Parameters.AddWithValue("@status", BusinessConstants.STATUS_BORROWED);
                cmd.ExecuteNonQuery();
            }

            // 扣减可借数量（并发控制：WHERE availableCount > 0 防止超卖）
            const string updateSql = "UPDATE T_Book SET availableCount = availableCount - 1 WHERE bookID = @bookID AND availableCount > 0";
            int affected;
            using (SqlCommand cmd = new(updateSql, conn, tran))
            {
                cmd.Parameters.AddWithValue("@bookID", bookID);
                affected = cmd.ExecuteNonQuery();
            }
            // 受影响行数为 0 表示并发场景下库存已被其他事务扣减完
            if (affected == 0)
                throw new BusinessException("该图书已全部借出，无法借阅（并发冲突）");

            tran.Commit();
        }
        catch (BusinessException)
        {
            // 业务异常直接抛出，不掩盖
            try { tran.Rollback(); } catch { /* 忽略 Rollback 异常，避免掩盖原始业务异常（cs-0 经验四） */ }
            throw;
        }
        catch
        {
            // 系统异常：回滚后重新抛出
            try { tran.Rollback(); } catch { }
            throw;
        }

        // ===== 3. 记录操作日志（事务外独立执行，不影响主流程） =====
        if (!string.IsNullOrEmpty(userName))
        {
            try
            {
                LogBiz.Log(userName, BusinessConstants.LOG_BORROW,
                    $"借书：{bookID}", $"读者 {reader.ReaderName}({readerID}) 借阅了 {book.BookName}({bookID})");
            }
            catch { /* 日志写入失败不影响主流程 */ }
        }
    }

    /// <summary>
    /// 还书核心逻辑（无日志版本，向后兼容）
    /// </summary>
    public void Return(int borrowID) => Return(borrowID, null);

    /// <summary>
    /// 还书核心逻辑：事务更新借阅状态 + 恢复库存 + 逾期自动生成罚款 + 触发预约通知
    /// </summary>
    /// <param name="borrowID">借阅编号</param>
    /// <param name="userName">操作者用户名（null 时不记录日志）</param>
    /// <exception cref="BusinessException">借阅记录不存在 / 已归还</exception>
    public void Return(int borrowID, string? userName)
    {
        // ===== 1. 校验借阅记录 =====
        BorrowInfo borrow = _borrowDao.FindById(borrowID)
            ?? throw new BusinessException($"借阅编号 {borrowID} 不存在");
        if (borrow.Status != BusinessConstants.STATUS_BORROWED)
            throw new BusinessException("该图书已归还，无需重复操作");

        DateTime returnDate = DateTime.Today;

        // ===== 2. 事务执行：更新借阅状态 + 恢复可借数量 =====
        using SqlConnection conn = new(DBConnection.GetConnectionString());
        conn.Open();
        using SqlTransaction tran = conn.BeginTransaction();
        try
        {
            // 更新借阅记录状态为"已还"，设置归还日期
            const string updateBorrowSql = @"UPDATE T_Borrow SET status = @status, returnDate = @returnDate
                                             WHERE borrowID = @borrowID";
            using (SqlCommand cmd = new(updateBorrowSql, conn, tran))
            {
                cmd.Parameters.AddWithValue("@status", BusinessConstants.STATUS_RETURNED);
                cmd.Parameters.AddWithValue("@returnDate", returnDate);
                cmd.Parameters.AddWithValue("@borrowID", borrowID);
                cmd.ExecuteNonQuery();
            }

            // 恢复图书可借数量
            const string updateBookSql = "UPDATE T_Book SET availableCount = availableCount + 1 WHERE bookID = @bookID";
            using (SqlCommand cmd = new(updateBookSql, conn, tran))
            {
                cmd.Parameters.AddWithValue("@bookID", borrow.BookID);
                cmd.ExecuteNonQuery();
            }

            tran.Commit();
        }
        catch
        {
            try { tran.Rollback(); } catch { }
            throw;
        }

        // ===== 3. 逾期罚款生成（事务外独立执行） =====
        // 罚款是还书的衍生操作，即使罚款生成失败也不影响还书成功
        // PRD：归还日期超过应还日期时自动计算逾期天数并生成罚款（0.3元/天）
        if (returnDate > borrow.DueDate)
        {
            int overdueDays = (returnDate - borrow.DueDate).Days;
            // 逾期天数为 0 不生成罚款（PRD：归还日期等于应还日期不算逾期）
            if (overdueDays > 0)
            {
                decimal fineAmount = overdueDays * BusinessConstants.FINE_PER_DAY;

                // 检查是否已生成罚款（同一条借阅记录最多一条罚款）
                if (!_fineDao.ExistsByBorrowID(borrowID))
                {
                    FineInfo fine = new()
                    {
                        ReaderID = borrow.ReaderID,
                        BookID = borrow.BookID,
                        BorrowID = borrowID,
                        OverdueDays = overdueDays,
                        FineAmount = fineAmount,
                        FineStatus = BusinessConstants.FINE_UNPAID,
                        CreateDate = returnDate,
                        PayDate = null
                    };
                    _fineDao.Insert(fine);
                }
            }
        }

        // ===== 4. 触发预约通知（还书后检查是否有排队预约） =====
        try
        {
            var reservationBiz = new ReservationBiz();
            reservationBiz.NotifyAfterReturn(borrow.BookID, userName ?? "系统");
        }
        catch { /* 预约通知失败不影响主流程 */ }

        // ===== 5. 记录操作日志 =====
        if (!string.IsNullOrEmpty(userName))
        {
            try
            {
                LogBiz.Log(userName, BusinessConstants.LOG_RETURN,
                    $"还书：{borrow.BookID}", $"读者 {borrow.ReaderID} 归还了 {borrow.BookID}，借阅编号 {borrowID}");
            }
            catch { /* 日志写入失败不影响主流程 */ }
        }
    }
}
