using Microsoft.Data.SqlClient;
using LibrarySys.DAL;
using LibrarySys.Models;
using LibrarySys.Common;

namespace LibrarySys.BLL;

/// <summary>
/// Dashboard 首页统计数据（聚合6个统计指标 + 排行 + 提醒）
/// </summary>
public class DashboardData
{
    /// <summary>馆藏图书总数</summary>
    public int TotalBooks { get; set; }

    /// <summary>注册读者总数</summary>
    public int TotalReaders { get; set; }

    /// <summary>当前借出数量</summary>
    public int ActiveBorrows { get; set; }

    /// <summary>逾期未还数量</summary>
    public int OverdueCount { get; set; }

    /// <summary>未缴罚款数</summary>
    public int UnpaidFines { get; set; }

    /// <summary>活跃预约数</summary>
    public int ActiveReservations { get; set; }

    /// <summary>借阅热度排行 Top10</summary>
    public List<BorrowRankItem> Ranking { get; set; } = new();

    /// <summary>逾期读者列表</summary>
    public List<BorrowInfo> OverdueReaders { get; set; } = new();
}

/// <summary>
/// 借阅热度排行项（统计面板用，非数据库实体）
/// </summary>
public class BorrowRankItem
{
    /// <summary>书名</summary>
    public string BookName { get; set; } = string.Empty;

    /// <summary>借阅次数</summary>
    public int BorrowCount { get; set; }
}

/// <summary>
/// 统计业务逻辑类（统计面板用，聚合查询全库数据）
/// </summary>
public class StatisticBiz
{
    private readonly BookDao _bookDao = new();
    private readonly ReaderDao _readerDao = new();
    private readonly BorrowDao _borrowDao = new();

    /// <summary>馆藏图书总量（不同图书种数）</summary>
    public int GetTotalBooks()
    {
        const string sql = "SELECT COUNT(1) FROM T_Book";
        return ExecuteScalarInt(sql);
    }

    /// <summary>注册读者总数</summary>
    public int GetTotalReaders()
    {
        const string sql = "SELECT COUNT(1) FROM T_Reader";
        return ExecuteScalarInt(sql);
    }

    /// <summary>当前借出未还的图书数量</summary>
    public int GetActiveBorrowCount() => _borrowDao.CountActiveBorrow();

    /// <summary>当前逾期未还的图书数量</summary>
    public int GetOverdueCount() => _borrowDao.CountOverdue();

    /// <summary>未缴罚款数量</summary>
    public int GetUnpaidFineCount()
    {
        const string sql = "SELECT COUNT(1) FROM T_Fine WHERE fineStatus = @status";
        return ExecuteScalarInt(sql, new SqlParameter("@status", BusinessConstants.FINE_UNPAID));
    }

    /// <summary>
    /// 获取 Dashboard 首页所需的全部统计数据（一次查询聚合所有指标）
    /// </summary>
    public DashboardData GetDashboardData()
    {
        return new DashboardData
        {
            TotalBooks = GetTotalBooks(),
            TotalReaders = GetTotalReaders(),
            ActiveBorrows = GetActiveBorrowCount(),
            OverdueCount = GetOverdueCount(),
            UnpaidFines = GetUnpaidFineCount(),
            ActiveReservations = new ReservationBiz().CountActive(),
            Ranking = GetBorrowRankingTop10(),
            OverdueReaders = GetOverdueReaders()
        };
    }

    /// <summary>
    /// 借阅热度排行 Top10（按借阅次数降序）
    /// </summary>
    /// <returns>排行列表，最多 10 项</returns>
    public List<BorrowRankItem> GetBorrowRankingTop10()
    {
        // GROUP BY bookName 聚合借阅次数，TOP 10 限制结果集大小
        const string sql = @"SELECT TOP 10 b.bookName, COUNT(*) AS borrowCount
                             FROM T_Borrow br
                             INNER JOIN T_Book b ON br.bookID = b.bookID
                             GROUP BY b.bookName
                             ORDER BY borrowCount DESC";
        List<BorrowRankItem> list = new();
        using SqlConnection conn = new(DBConnection.GetConnectionString());
        conn.Open();
        using SqlCommand cmd = new(sql, conn);
        using SqlDataReader reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(new BorrowRankItem
            {
                BookName = reader.GetString(0),
                BorrowCount = reader.GetInt32(1)
            });
        }
        return list;
    }

    /// <summary>
    /// 逾期读者列表（当前借出且超过应还日期）
    /// </summary>
    /// <returns>逾期借阅记录列表，包含读者姓名、书名、应还日期</returns>
    public List<BorrowInfo> GetOverdueReaders()
    {
        const string sql = @"SELECT br.borrowID, br.readerID, br.bookID, br.borrowDate, br.dueDate, br.returnDate, br.status,
                                   r.readerName, b.bookName
                            FROM T_Borrow br
                            INNER JOIN T_Reader r ON br.readerID = r.readerID
                            INNER JOIN T_Book b ON br.bookID = b.bookID
                            WHERE br.status = @status AND br.dueDate < GETDATE()
                            ORDER BY br.dueDate ASC";
        List<BorrowInfo> list = new();
        using SqlConnection conn = new(DBConnection.GetConnectionString());
        conn.Open();
        using SqlCommand cmd = new(sql, conn);
        cmd.Parameters.AddWithValue("@status", BusinessConstants.STATUS_BORROWED);
        using SqlDataReader reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(new BorrowInfo
            {
                BorrowID = reader.GetInt32(0),
                ReaderID = reader.GetString(1),
                BookID = reader.GetString(2),
                BorrowDate = reader.GetDateTime(3),
                DueDate = reader.GetDateTime(4),
                ReturnDate = reader.IsDBNull(5) ? null : reader.GetDateTime(5),
                Status = reader.GetString(6),
                ReaderName = reader.GetString(7),
                BookName = reader.GetString(8)
            });
        }
        return list;
    }

    /// <summary>执行 COUNT 聚合查询并返回整数结果</summary>
    private static int ExecuteScalarInt(string sql, SqlParameter? param = null)
    {
        using SqlConnection conn = new(DBConnection.GetConnectionString());
        conn.Open();
        using SqlCommand cmd = new(sql, conn);
        if (param != null) cmd.Parameters.Add(param);
        return (int)cmd.ExecuteScalar();
    }
}
