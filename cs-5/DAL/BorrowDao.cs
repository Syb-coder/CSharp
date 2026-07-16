using Microsoft.Data.SqlClient;
using LibrarySys.Models;
using LibrarySys.Common;

namespace LibrarySys.DAL;

/// <summary>
/// 借阅信息数据访问类，继承泛型基类 BaseRepository&lt;BorrowInfo&gt;
/// </summary>
public class BorrowDao : BaseRepository<BorrowInfo>
{
    /// <summary>查询全部借阅记录（联表 T_Reader、T_Book 获取姓名和书名）</summary>
    public override List<BorrowInfo> FindAll()
    {
        const string sql = @"SELECT br.borrowID, br.readerID, br.bookID, br.borrowDate, br.dueDate, br.returnDate, br.status,
                                   r.readerName, b.bookName
                            FROM T_Borrow br
                            LEFT JOIN T_Reader r ON br.readerID = r.readerID
                            LEFT JOIN T_Book b ON br.bookID = b.bookID
                            ORDER BY br.borrowID DESC";
        List<BorrowInfo> list = new();
        using SqlConnection conn = new(DBConnection.GetConnectionString());
        conn.Open();
        using SqlCommand cmd = new(sql, conn);
        using SqlDataReader reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(MapReader(reader));
        }
        return list;
    }

    /// <summary>按借阅编号查询单条记录（borrowID 为自增整型，需将 string 转 int）</summary>
    public override BorrowInfo FindById(string id)
    {
        if (!int.TryParse(id, out int borrowID))
            return null;
        return FindById(borrowID);
    }

    /// <summary>按借阅编号查询（整型主键重载）</summary>
    public BorrowInfo FindById(int borrowID)
    {
        const string sql = @"SELECT br.borrowID, br.readerID, br.bookID, br.borrowDate, br.dueDate, br.returnDate, br.status,
                                   r.readerName, b.bookName
                            FROM T_Borrow br
                            LEFT JOIN T_Reader r ON br.readerID = r.readerID
                            LEFT JOIN T_Book b ON br.bookID = b.bookID
                            WHERE br.borrowID = @borrowID";
        using SqlConnection conn = new(DBConnection.GetConnectionString());
        conn.Open();
        using SqlCommand cmd = new(sql, conn);
        cmd.Parameters.AddWithValue("@borrowID", borrowID);
        using SqlDataReader reader = cmd.ExecuteReader();
        return reader.Read() ? MapReader(reader) : null;
    }

    /// <summary>新增借阅记录</summary>
    public override int Insert(BorrowInfo entity)
    {
        const string sql = @"INSERT INTO T_Borrow (readerID, bookID, borrowDate, dueDate, returnDate, status)
                             VALUES (@readerID, @bookID, @borrowDate, @dueDate, @returnDate, @status)";
        using SqlConnection conn = new(DBConnection.GetConnectionString());
        conn.Open();
        using SqlCommand cmd = new(sql, conn);
        cmd.Parameters.AddWithValue("@readerID", entity.ReaderID);
        cmd.Parameters.AddWithValue("@bookID", entity.BookID);
        cmd.Parameters.AddWithValue("@borrowDate", entity.BorrowDate);
        cmd.Parameters.AddWithValue("@dueDate", entity.DueDate);
        cmd.Parameters.AddWithValue("@returnDate", (object)entity.ReturnDate ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@status", entity.Status);
        return cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// 新增借阅记录并返回自增主键 borrowID
    /// 用于还书和罚款逻辑中需要引用 borrowID 的场景
    /// </summary>
    /// <param name="entity">借阅实体</param>
    /// <returns>新生成的 borrowID，失败返回 0</returns>
    public int InsertAndReturnId(BorrowInfo entity)
    {
        const string sql = @"INSERT INTO T_Borrow (readerID, bookID, borrowDate, dueDate, returnDate, status)
                             VALUES (@readerID, @bookID, @borrowDate, @dueDate, @returnDate, @status);
                             SELECT CAST(SCOPE_IDENTITY() AS INT);";
        using SqlConnection conn = new(DBConnection.GetConnectionString());
        conn.Open();
        using SqlCommand cmd = new(sql, conn);
        cmd.Parameters.AddWithValue("@readerID", entity.ReaderID);
        cmd.Parameters.AddWithValue("@bookID", entity.BookID);
        cmd.Parameters.AddWithValue("@borrowDate", entity.BorrowDate);
        cmd.Parameters.AddWithValue("@dueDate", entity.DueDate);
        cmd.Parameters.AddWithValue("@returnDate", (object)entity.ReturnDate ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@status", entity.Status);
        // ExecuteScalar 返回 SELECT 语句的第一行第一列，即新生成的自增 ID
        return (int)cmd.ExecuteScalar();
    }

    /// <summary>更新借阅记录（归还时更新 returnDate 和 status）</summary>
    public override int Update(BorrowInfo entity)
    {
        const string sql = @"UPDATE T_Borrow SET readerID = @readerID, bookID = @bookID, borrowDate = @borrowDate,
                             dueDate = @dueDate, returnDate = @returnDate, status = @status
                             WHERE borrowID = @borrowID";
        using SqlConnection conn = new(DBConnection.GetConnectionString());
        conn.Open();
        using SqlCommand cmd = new(sql, conn);
        cmd.Parameters.AddWithValue("@readerID", entity.ReaderID);
        cmd.Parameters.AddWithValue("@bookID", entity.BookID);
        cmd.Parameters.AddWithValue("@borrowDate", entity.BorrowDate);
        cmd.Parameters.AddWithValue("@dueDate", entity.DueDate);
        cmd.Parameters.AddWithValue("@returnDate", (object)entity.ReturnDate ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@status", entity.Status);
        cmd.Parameters.AddWithValue("@borrowID", entity.BorrowID);
        return cmd.ExecuteNonQuery();
    }

    /// <summary>按借阅编号删除</summary>
    public override int Delete(string id)
    {
        if (!int.TryParse(id, out int borrowID))
            return 0;
        const string sql = "DELETE FROM T_Borrow WHERE borrowID = @borrowID";
        using SqlConnection conn = new(DBConnection.GetConnectionString());
        conn.Open();
        using SqlCommand cmd = new(sql, conn);
        cmd.Parameters.AddWithValue("@borrowID", borrowID);
        return cmd.ExecuteNonQuery();
    }

    // ===== 以下为 Borrow 特有查询 =====

    /// <summary>按读者、图书、状态、日期范围多条件查询借阅记录</summary>
    public List<BorrowInfo> Search(string readerID, string bookID, string status, DateTime? startDate, DateTime? endDate)
    {
        List<string> conditions = new();
        if (!string.IsNullOrWhiteSpace(readerID))
            conditions.Add("br.readerID LIKE '%' + @readerID + '%'");
        if (!string.IsNullOrWhiteSpace(bookID))
            conditions.Add("br.bookID LIKE '%' + @bookID + '%'");
        if (!string.IsNullOrWhiteSpace(status))
            conditions.Add("br.status = @status");
        if (startDate.HasValue)
            conditions.Add("br.borrowDate >= @startDate");
        if (endDate.HasValue)
            conditions.Add("br.borrowDate <= @endDate");

        string sql = @"SELECT br.borrowID, br.readerID, br.bookID, br.borrowDate, br.dueDate, br.returnDate, br.status,
                              r.readerName, b.bookName
                       FROM T_Borrow br
                       LEFT JOIN T_Reader r ON br.readerID = r.readerID
                       LEFT JOIN T_Book b ON br.bookID = b.bookID";
        if (conditions.Count > 0)
            sql += " WHERE " + string.Join(" AND ", conditions);
        sql += " ORDER BY br.borrowID DESC";

        List<BorrowInfo> list = new();
        using SqlConnection conn = new(DBConnection.GetConnectionString());
        conn.Open();
        using SqlCommand cmd = new(sql, conn);
        if (!string.IsNullOrWhiteSpace(readerID))
            cmd.Parameters.AddWithValue("@readerID", readerID);
        if (!string.IsNullOrWhiteSpace(bookID))
            cmd.Parameters.AddWithValue("@bookID", bookID);
        if (!string.IsNullOrWhiteSpace(status))
            cmd.Parameters.AddWithValue("@status", status);
        if (startDate.HasValue)
            cmd.Parameters.AddWithValue("@startDate", startDate.Value);
        if (endDate.HasValue)
            cmd.Parameters.AddWithValue("@endDate", endDate.Value);
        using SqlDataReader reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(MapReader(reader));
        }
        return list;
    }

    /// <summary>查询读者当前借出未还的记录（借书时校验重复借阅）</summary>
    public BorrowInfo FindActiveByReaderAndBook(string readerID, string bookID)
    {
        const string sql = @"SELECT br.borrowID, br.readerID, br.bookID, br.borrowDate, br.dueDate, br.returnDate, br.status,
                                   r.readerName, b.bookName
                            FROM T_Borrow br
                            LEFT JOIN T_Reader r ON br.readerID = r.readerID
                            LEFT JOIN T_Book b ON br.bookID = b.bookID
                            WHERE br.readerID = @readerID AND br.bookID = @bookID AND br.status = @status";
        using SqlConnection conn = new(DBConnection.GetConnectionString());
        conn.Open();
        using SqlCommand cmd = new(sql, conn);
        cmd.Parameters.AddWithValue("@readerID", readerID);
        cmd.Parameters.AddWithValue("@bookID", bookID);
        cmd.Parameters.AddWithValue("@status", BusinessConstants.STATUS_BORROWED);
        using SqlDataReader reader = cmd.ExecuteReader();
        return reader.Read() ? MapReader(reader) : null;
    }

    /// <summary>统计当前借出未还的图书总数（统计面板用）</summary>
    public int CountActiveBorrow()
    {
        const string sql = "SELECT COUNT(1) FROM T_Borrow WHERE status = @status";
        using SqlConnection conn = new(DBConnection.GetConnectionString());
        conn.Open();
        using SqlCommand cmd = new(sql, conn);
        cmd.Parameters.AddWithValue("@status", BusinessConstants.STATUS_BORROWED);
        return (int)cmd.ExecuteScalar();
    }

    /// <summary>统计当前逾期未还的图书总数（统计面板用）</summary>
    public int CountOverdue()
    {
        // GETDATE() 返回 SQL Server 当前时间，用于判断是否超过应还日期
        const string sql = "SELECT COUNT(1) FROM T_Borrow WHERE status = @status AND dueDate < GETDATE()";
        using SqlConnection conn = new(DBConnection.GetConnectionString());
        conn.Open();
        using SqlCommand cmd = new(sql, conn);
        cmd.Parameters.AddWithValue("@status", BusinessConstants.STATUS_BORROWED);
        return (int)cmd.ExecuteScalar();
    }

    /// <summary>SqlDataReader 映射为 BorrowInfo 实体</summary>
    private static BorrowInfo MapReader(SqlDataReader reader)
        => new()
        {
            BorrowID = reader.GetInt32(0),
            ReaderID = reader.GetString(1),
            BookID = reader.GetString(2),
            BorrowDate = reader.GetDateTime(3),
            DueDate = reader.GetDateTime(4),
            ReturnDate = reader.IsDBNull(5) ? null : reader.GetDateTime(5),
            Status = reader.GetString(6),
            ReaderName = reader.IsDBNull(7) ? null : reader.GetString(7),
            BookName = reader.IsDBNull(8) ? null : reader.GetString(8)
        };
}
