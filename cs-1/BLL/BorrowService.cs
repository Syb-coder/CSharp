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
    private readonly BookCategoryDAL _categoryDAL = new();

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
        // 校验顺序设计（由轻到重、由快到慢）：
        // 1. 存在性校验（读者/图书是否存在）- 单行查询，代价最低，最先执行
        // 2. 可借性校验（库存是否充足）- 内存判断，无需额外查询
        // 3. 超期校验（读者是否有超期未还）- 查询借阅表
        // 4. 重复借阅校验（同一本书是否已借未还）- 查询借阅表
        // 5. 上限校验（借阅数量是否达上限）- 聚合查询，代价最高，最后执行
        // 越靠前的校验越容易不通过，可以尽早拦截，减少不必要的数据库查询

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
        // 业务规则：有超期未还的读者被"冻结"借书权限，督促其先归还逾期图书
        if (_borrowDAL.HasOverdueBorrow(readerID))
        {
            throw new BusinessException("该读者有超期未还图书，请先归还");
        }

        // 前置校验：是否重复借阅同一本未归还的图书
        // 业务规则：同一读者不能同时持有多本相同图书，避免资源独占
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
        // 应还日期 = 借书日 + 该图书类别对应的借阅天数（不同类别可设置不同借阅期限）
        BookCategory category = _categoryDAL.GetCategoryById(book.CategoryID)
            ?? throw new BusinessException("图书类别不存在");
        DateTime dueDate = borrowDate.AddDays(category.BorrowDays);
        BorrowRecord record = new()
        {
            ReaderID = readerID,
            BookID = bookID,
            BorrowDate = borrowDate,
            DueDate = dueDate,
            Status = BusinessConstants.STATUS_BORROWED
        };

        // 事务保证原子性：插入借阅记录和扣减库存必须同时成功或同时失败
        // 若分开执行：插入成功但扣减失败会导致库存虚高；扣减成功但插入失败会导致库存丢失
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
            // 即使前置校验通过，并发场景下可能有另一请求抢先扣减了最后一个库存
            // 此处 SQL 的 WHERE availableCount > 0 会感知到并返回 false
            if (!_bookDAL.DecreaseAvailableCount(conn, transaction, bookID))
            {
                throw new BusinessException("图书可借数量不足，借阅失败（可能已被并发借出）");
            }

            transaction.Commit();
        }
        catch
        {
            // 任何异常都回滚事务，确保不会出现"半完成"状态
            transaction.Rollback();
            throw;
        }
    }

    /// <summary>
    /// 办理还书（事务操作：更新归还信息 + 恢复可借数量）
    /// </summary>
    /// <param name="borrowID">借阅编号</param>
    /// <returns>元组 (逾期天数, 逾期罚款金额)。逾期天数=0表示未逾期；罚款金额根据图书类别的单日罚款标准计算</returns>
    /// <exception cref="BusinessException">校验失败</exception>
    public (int overdueDays, decimal fineAmount) ReturnBook(int borrowID)
    {
        // 按主键直接查询单条借阅记录，避免加载全部在借记录到内存
        BorrowRecord record = _borrowDAL.GetBorrowById(borrowID)
            ?? throw new BusinessException("借阅记录不存在");

        // 校验记录状态：只有"借出"状态的记录才能办理还书，防止重复归还
        if (record.Status != BusinessConstants.STATUS_BORROWED)
        {
            throw new BusinessException("该借阅记录已归还，无需重复操作");
        }

        DateTime returnDate = DateTime.Today;
        int overdueDays = 0;
        decimal fineAmount = 0;

        // 逾期天数计算：归还日期超过应还日期才算逾期，未逾期返回 0
        // 使用日期差而非时间差，因为借阅以"天"为单位管理
        if (returnDate > record.DueDate)
        {
            overdueDays = (returnDate - record.DueDate).Days;

            // 根据图书所属类别的单日罚款标准计算逾期罚款金额
            // 先通过图书ID获取图书信息，再通过图书的类别ID获取类别的罚款标准
            Book book = _bookDAL.GetBookById(record.BookID);
            if (book != null)
            {
                BookCategory category = _categoryDAL.GetCategoryById(book.CategoryID);
                if (category != null)
                {
                    fineAmount = overdueDays * category.FinePerDay;
                }
            }
        }

        // 事务保证原子性：更新归还信息和恢复库存必须同时成功或同时失败
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
            // 回滚事务，防止"已还状态更新但库存未恢复"或反之的数据不一致
            transaction.Rollback();
            throw;
        }

        return (overdueDays, fineAmount);
    }
}
