using System.Data;
using Microsoft.Data.SqlClient;
using LibraryManagement.Models;

namespace LibraryManagement.DAL;

/// <summary>
/// 借阅数据访问类，对应 tbl_Borrow 表的 CRUD 操作
/// </summary>
public class BorrowDAL
{
    /// <summary>
    /// 查询全部借阅记录（含读者姓名、书名联表查询）
    /// </summary>
    /// <returns>借阅记录列表</returns>
    public List<BorrowRecord> GetAllBorrows()
    {
        // 双 LEFT JOIN 联表读者和图书表：即使读者或图书被物理删除，借阅记录仍能展示（关联字段为 NULL）
        // 借阅记录是历史档案，不应因关联实体删除而丢失
        const string sql = @"
            SELECT br.borrowID, br.readerID, br.bookID, br.borrowDate, br.dueDate,
                   br.returnDate, br.status,
                   r.readerName, b.bookName
            FROM tbl_Borrow br
            LEFT JOIN tbl_Reader r ON br.readerID = r.readerID
            LEFT JOIN tbl_Book b ON br.bookID = b.bookID
            ORDER BY br.borrowID DESC"; // DESC 按 borrowID 倒序，最新记录排在最前
        List<BorrowRecord> list = new();
        using SqlConnection conn = new(DBConnection.GetConnectionString());
        using SqlCommand cmd = new(sql, conn);
        conn.Open();
        using SqlDataReader reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(MapReaderToBorrow(reader));
        }
        return list;
    }

    /// <summary>
    /// 多条件查询借阅记录
    /// </summary>
    /// <param name="readerID">读者编号（可空）</param>
    /// <param name="bookID">图书编号（可空）</param>
    /// <param name="status">状态：借出/已还（可空表示不限）</param>
    /// <param name="startDate">借出日期起始（可空）</param>
    /// <param name="endDate">借出日期截止（可空）</param>
    /// <returns>借阅记录列表</returns>
    public List<BorrowRecord> SearchBorrows(string readerID, string bookID, string status, DateTime? startDate, DateTime? endDate)
    {
        const string sql = @"
            SELECT br.borrowID, br.readerID, br.bookID, br.borrowDate, br.dueDate,
                   br.returnDate, br.status,
                   r.readerName, b.bookName
            FROM tbl_Borrow br
            LEFT JOIN tbl_Reader r ON br.readerID = r.readerID
            LEFT JOIN tbl_Book b ON br.bookID = b.bookID
            WHERE (@readerID IS NULL OR br.readerID = @readerID)
              AND (@bookID IS NULL OR br.bookID = @bookID)
              AND (@status IS NULL OR br.status = @status)
              AND (@startDate IS NULL OR br.borrowDate >= @startDate)
              AND (@endDate IS NULL OR br.borrowDate <= @endDate)
            ORDER BY br.borrowID DESC";
        List<BorrowRecord> list = new();
        using SqlConnection conn = new(DBConnection.GetConnectionString());
        using SqlCommand cmd = new(sql, conn);
        cmd.Parameters.Add(new SqlParameter("@readerID", SqlDbType.NVarChar, 20)
        {
            Value = string.IsNullOrWhiteSpace(readerID) ? DBNull.Value : readerID
        });
        cmd.Parameters.Add(new SqlParameter("@bookID", SqlDbType.NVarChar, 20)
        {
            Value = string.IsNullOrWhiteSpace(bookID) ? DBNull.Value : bookID
        });
        cmd.Parameters.Add(new SqlParameter("@status", SqlDbType.NVarChar, 4)
        {
            Value = string.IsNullOrWhiteSpace(status) ? DBNull.Value : status
        });
        cmd.Parameters.Add(new SqlParameter("@startDate", SqlDbType.Date)
        {
            Value = startDate.HasValue ? startDate.Value : DBNull.Value
        });
        cmd.Parameters.Add(new SqlParameter("@endDate", SqlDbType.Date)
        {
            Value = endDate.HasValue ? endDate.Value : DBNull.Value
        });
        conn.Open();
        using SqlDataReader reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(MapReaderToBorrow(reader));
        }
        return list;
    }

    /// <summary>
    /// 按借阅编号查询单条借阅记录（含读者姓名、书名联表查询）
    /// </summary>
    /// <param name="borrowID">借阅编号</param>
    /// <returns>借阅记录实体，不存在返回 null</returns>
    public BorrowRecord GetBorrowById(int borrowID)
    {
        const string sql = @"
            SELECT br.borrowID, br.readerID, br.bookID, br.borrowDate, br.dueDate,
                   br.returnDate, br.status,
                   r.readerName, b.bookName
            FROM tbl_Borrow br
            LEFT JOIN tbl_Reader r ON br.readerID = r.readerID
            LEFT JOIN tbl_Book b ON br.bookID = b.bookID
            WHERE br.borrowID = @borrowID";
        using SqlConnection conn = new(DBConnection.GetConnectionString());
        using SqlCommand cmd = new(sql, conn);
        cmd.Parameters.Add(new SqlParameter("@borrowID", SqlDbType.Int) { Value = borrowID });
        conn.Open();
        using SqlDataReader reader = cmd.ExecuteReader();
        return reader.Read() ? MapReaderToBorrow(reader) : null;
    }

    /// <summary>
    /// 统计读者当前借出未还的图书数量
    /// </summary>
    /// <param name="readerID">读者编号</param>
    /// <returns>借出未还数量</returns>
    public int CountBorrowingByReader(string readerID)
    {
        const string sql = "SELECT COUNT(1) FROM tbl_Borrow WHERE readerID = @readerID AND status = N'借出'";
        using SqlConnection conn = new(DBConnection.GetConnectionString());
        using SqlCommand cmd = new(sql, conn);
        cmd.Parameters.Add(new SqlParameter("@readerID", SqlDbType.NVarChar, 20) { Value = readerID });
        conn.Open();
        return (int)cmd.ExecuteScalar();
    }

    /// <summary>
    /// 检查读者是否已借阅指定图书且未归还
    /// </summary>
    /// <param name="readerID">读者编号</param>
    /// <param name="bookID">图书编号</param>
    /// <returns>存在未归还记录返回 true</returns>
    public bool ExistsUnreturnedBorrow(string readerID, string bookID)
    {
        const string sql = "SELECT COUNT(1) FROM tbl_Borrow WHERE readerID = @readerID AND bookID = @bookID AND status = N'借出'";
        using SqlConnection conn = new(DBConnection.GetConnectionString());
        using SqlCommand cmd = new(sql, conn);
        cmd.Parameters.Add(new SqlParameter("@readerID", SqlDbType.NVarChar, 20) { Value = readerID });
        cmd.Parameters.Add(new SqlParameter("@bookID", SqlDbType.NVarChar, 20) { Value = bookID });
        conn.Open();
        return (int)cmd.ExecuteScalar() > 0;
    }

    /// <summary>
    /// 查询读者是否有超期未还的图书
    /// </summary>
    /// <param name="readerID">读者编号</param>
    /// <returns>有超期未还返回 true</returns>
    public bool HasOverdueBorrow(string readerID)
    {
        // 使用 GETDATE() 获取数据库服务器时间而非应用服务器时间，避免客户端时钟不准导致判断偏差
        // 超期判断逻辑：状态为"借出"且应还日期已过当前时间
        const string sql = "SELECT COUNT(1) FROM tbl_Borrow WHERE readerID = @readerID AND status = N'借出' AND dueDate < GETDATE()";
        using SqlConnection conn = new(DBConnection.GetConnectionString());
        using SqlCommand cmd = new(sql, conn);
        cmd.Parameters.Add(new SqlParameter("@readerID", SqlDbType.NVarChar, 20) { Value = readerID });
        conn.Open();
        return (int)cmd.ExecuteScalar() > 0;
    }

    /// <summary>
    /// 在指定事务中插入借阅记录
    /// </summary>
    /// <param name="conn">已打开的连接</param>
    /// <param name="transaction">所属事务</param>
    /// <param name="record">借阅记录实体</param>
    /// <returns>成功返回 true</returns>
    public bool InsertBorrow(SqlConnection conn, SqlTransaction transaction, BorrowRecord record)
    {
        // 事务方法设计：conn 和 transaction 由 BLL 层创建并传入，此方法不管理连接生命周期
        // 这样 BLL 层可以将"插入借阅记录"和"扣减库存"组合在同一事务中，保证原子性
        const string sql = @"
            INSERT INTO tbl_Borrow (readerID, bookID, borrowDate, dueDate, returnDate, status)
            VALUES (@readerID, @bookID, @borrowDate, @dueDate, @returnDate, @status)";
        using SqlCommand cmd = new(sql, conn, transaction);
        cmd.Parameters.Add(new SqlParameter("@readerID", SqlDbType.NVarChar, 20) { Value = record.ReaderID });
        cmd.Parameters.Add(new SqlParameter("@bookID", SqlDbType.NVarChar, 20) { Value = record.BookID });
        cmd.Parameters.Add(new SqlParameter("@borrowDate", SqlDbType.Date) { Value = record.BorrowDate });
        cmd.Parameters.Add(new SqlParameter("@dueDate", SqlDbType.Date) { Value = record.DueDate });
        cmd.Parameters.Add(new SqlParameter("@returnDate", SqlDbType.Date)
        {
            Value = record.ReturnDate.HasValue ? record.ReturnDate.Value : DBNull.Value
        });
        cmd.Parameters.Add(new SqlParameter("@status", SqlDbType.NVarChar, 4) { Value = record.Status });
        return cmd.ExecuteNonQuery() > 0;
    }

    /// <summary>
    /// 在指定事务中更新归还信息
    /// </summary>
    /// <param name="conn">已打开的连接</param>
    /// <param name="transaction">所属事务</param>
    /// <param name="borrowID">借阅编号</param>
    /// <param name="returnDate">归还日期</param>
    /// <returns>成功返回 true</returns>
    public bool UpdateReturn(SqlConnection conn, SqlTransaction transaction, int borrowID, DateTime returnDate)
    {
        // 还书日期由 BLL 层传入（DateTime.Today）而非使用 GETDATE()，
        // 因为 BLL 层需要用同一个日期计算逾期天数，保证判断一致性
        const string sql = "UPDATE tbl_Borrow SET returnDate = @returnDate, status = N'已还' WHERE borrowID = @borrowID";
        using SqlCommand cmd = new(sql, conn, transaction);
        cmd.Parameters.Add(new SqlParameter("@returnDate", SqlDbType.Date) { Value = returnDate });
        cmd.Parameters.Add(new SqlParameter("@borrowID", SqlDbType.Int) { Value = borrowID });
        return cmd.ExecuteNonQuery() > 0;
    }

    /// <summary>
    /// 将 SqlDataReader 映射为 BorrowRecord 实体
    /// </summary>
    private static BorrowRecord MapReaderToBorrow(SqlDataReader reader)
    {
        return new BorrowRecord
        {
            BorrowID = Convert.ToInt32(reader["borrowID"]),
            ReaderID = reader["readerID"].ToString(),
            BookID = reader["bookID"].ToString(),
            BorrowDate = (DateTime)reader["borrowDate"],
            DueDate = (DateTime)reader["dueDate"],
            ReturnDate = reader["returnDate"] == DBNull.Value ? null : (DateTime?)reader["returnDate"],
            Status = reader["status"].ToString(),
            ReaderName = reader["readerName"] == DBNull.Value ? null : reader["readerName"].ToString(),
            BookName = reader["bookName"] == DBNull.Value ? null : reader["bookName"].ToString()
        };
    }
}
