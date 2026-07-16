using Microsoft.Data.SqlClient;
using LibrarySys.Models;
using LibrarySys.Common;

namespace LibrarySys.DAL;

/// <summary>
/// 罚款信息数据访问类，继承泛型基类 BaseRepository&lt;FineInfo&gt;
/// </summary>
public class FineDao : BaseRepository<FineInfo>
{
    /// <summary>查询全部罚款记录（联表 T_Reader、T_Book 获取姓名和书名）</summary>
    public override List<FineInfo> FindAll()
    {
        const string sql = @"SELECT f.fineID, f.readerID, f.bookID, f.borrowID, f.overdueDays, f.fineAmount,
                                   f.fineStatus, f.createDate, f.payDate, r.readerName, b.bookName
                            FROM T_Fine f
                            LEFT JOIN T_Reader r ON f.readerID = r.readerID
                            LEFT JOIN T_Book b ON f.bookID = b.bookID
                            ORDER BY f.fineID DESC";
        List<FineInfo> list = new();
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

    /// <summary>按罚款编号查询单条记录（fineID 为自增整型，需将 string 转 int）</summary>
    public override FineInfo FindById(string id)
    {
        if (!int.TryParse(id, out int fineID))
            return null;
        return FindById(fineID);
    }

    /// <summary>按罚款编号查询（整型主键重载）</summary>
    public FineInfo FindById(int fineID)
    {
        const string sql = @"SELECT f.fineID, f.readerID, f.bookID, f.borrowID, f.overdueDays, f.fineAmount,
                                   f.fineStatus, f.createDate, f.payDate, r.readerName, b.bookName
                            FROM T_Fine f
                            LEFT JOIN T_Reader r ON f.readerID = r.readerID
                            LEFT JOIN T_Book b ON f.bookID = b.bookID
                            WHERE f.fineID = @fineID";
        using SqlConnection conn = new(DBConnection.GetConnectionString());
        conn.Open();
        using SqlCommand cmd = new(sql, conn);
        cmd.Parameters.AddWithValue("@fineID", fineID);
        using SqlDataReader reader = cmd.ExecuteReader();
        return reader.Read() ? MapReader(reader) : null;
    }

    /// <summary>新增罚款记录</summary>
    public override int Insert(FineInfo entity)
    {
        const string sql = @"INSERT INTO T_Fine (readerID, bookID, borrowID, overdueDays, fineAmount, fineStatus, createDate, payDate)
                             VALUES (@readerID, @bookID, @borrowID, @overdueDays, @fineAmount, @fineStatus, @createDate, @payDate)";
        using SqlConnection conn = new(DBConnection.GetConnectionString());
        conn.Open();
        using SqlCommand cmd = new(sql, conn);
        cmd.Parameters.AddWithValue("@readerID", entity.ReaderID);
        cmd.Parameters.AddWithValue("@bookID", entity.BookID);
        cmd.Parameters.AddWithValue("@borrowID", entity.BorrowID);
        cmd.Parameters.AddWithValue("@overdueDays", entity.OverdueDays);
        cmd.Parameters.AddWithValue("@fineAmount", entity.FineAmount);
        cmd.Parameters.AddWithValue("@fineStatus", entity.FineStatus);
        cmd.Parameters.AddWithValue("@createDate", entity.CreateDate);
        cmd.Parameters.AddWithValue("@payDate", (object)entity.PayDate ?? DBNull.Value);
        return cmd.ExecuteNonQuery();
    }

    /// <summary>更新罚款记录（缴费时更新 fineStatus 和 payDate）</summary>
    public override int Update(FineInfo entity)
    {
        const string sql = @"UPDATE T_Fine SET fineStatus = @fineStatus, payDate = @payDate
                             WHERE fineID = @fineID";
        using SqlConnection conn = new(DBConnection.GetConnectionString());
        conn.Open();
        using SqlCommand cmd = new(sql, conn);
        cmd.Parameters.AddWithValue("@fineStatus", entity.FineStatus);
        cmd.Parameters.AddWithValue("@payDate", (object)entity.PayDate ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@fineID", entity.FineID);
        return cmd.ExecuteNonQuery();
    }

    /// <summary>按罚款编号删除</summary>
    public override int Delete(string id)
    {
        if (!int.TryParse(id, out int fineID))
            return 0;
        const string sql = "DELETE FROM T_Fine WHERE fineID = @fineID";
        using SqlConnection conn = new(DBConnection.GetConnectionString());
        conn.Open();
        using SqlCommand cmd = new(sql, conn);
        cmd.Parameters.AddWithValue("@fineID", fineID);
        return cmd.ExecuteNonQuery();
    }

    // ===== 以下为 Fine 特有查询 =====

    /// <summary>按读者编号、罚款状态查询罚款记录</summary>
    public List<FineInfo> Search(string readerID, string fineStatus)
    {
        List<string> conditions = new();
        if (!string.IsNullOrWhiteSpace(readerID))
            conditions.Add("f.readerID LIKE '%' + @readerID + '%'");
        if (!string.IsNullOrWhiteSpace(fineStatus))
            conditions.Add("f.fineStatus = @fineStatus");

        string sql = @"SELECT f.fineID, f.readerID, f.bookID, f.borrowID, f.overdueDays, f.fineAmount,
                              f.fineStatus, f.createDate, f.payDate, r.readerName, b.bookName
                       FROM T_Fine f
                       LEFT JOIN T_Reader r ON f.readerID = r.readerID
                       LEFT JOIN T_Book b ON f.bookID = b.bookID";
        if (conditions.Count > 0)
            sql += " WHERE " + string.Join(" AND ", conditions);
        sql += " ORDER BY f.fineID DESC";

        List<FineInfo> list = new();
        using SqlConnection conn = new(DBConnection.GetConnectionString());
        conn.Open();
        using SqlCommand cmd = new(sql, conn);
        if (!string.IsNullOrWhiteSpace(readerID))
            cmd.Parameters.AddWithValue("@readerID", readerID);
        if (!string.IsNullOrWhiteSpace(fineStatus))
            cmd.Parameters.AddWithValue("@fineStatus", fineStatus);
        using SqlDataReader reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(MapReader(reader));
        }
        return list;
    }

    /// <summary>查询读者未缴清的罚款记录</summary>
    public List<FineInfo> FindUnpaidByReader(string readerID)
    {
        const string sql = @"SELECT f.fineID, f.readerID, f.bookID, f.borrowID, f.overdueDays, f.fineAmount,
                                   f.fineStatus, f.createDate, f.payDate, r.readerName, b.bookName
                            FROM T_Fine f
                            LEFT JOIN T_Reader r ON f.readerID = r.readerID
                            LEFT JOIN T_Book b ON f.bookID = b.bookID
                            WHERE f.readerID = @readerID AND f.fineStatus = @fineStatus
                            ORDER BY f.fineID DESC";
        List<FineInfo> list = new();
        using SqlConnection conn = new(DBConnection.GetConnectionString());
        conn.Open();
        using SqlCommand cmd = new(sql, conn);
        cmd.Parameters.AddWithValue("@readerID", readerID);
        cmd.Parameters.AddWithValue("@fineStatus", BusinessConstants.FINE_UNPAID);
        using SqlDataReader reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(MapReader(reader));
        }
        return list;
    }

    /// <summary>检查借阅记录是否已生成罚款（同一条借阅最多一条罚款）</summary>
    public bool ExistsByBorrowID(int borrowID)
    {
        const string sql = "SELECT COUNT(1) FROM T_Fine WHERE borrowID = @borrowID";
        using SqlConnection conn = new(DBConnection.GetConnectionString());
        conn.Open();
        using SqlCommand cmd = new(sql, conn);
        cmd.Parameters.AddWithValue("@borrowID", borrowID);
        return (int)cmd.ExecuteScalar() > 0;
    }

    /// <summary>SqlDataReader 映射为 FineInfo 实体</summary>
    private static FineInfo MapReader(SqlDataReader reader)
        => new()
        {
            FineID = reader.GetInt32(0),
            ReaderID = reader.GetString(1),
            BookID = reader.GetString(2),
            BorrowID = reader.GetInt32(3),
            OverdueDays = reader.GetInt32(4),
            FineAmount = reader.GetDecimal(5),
            FineStatus = reader.GetString(6),
            CreateDate = reader.GetDateTime(7),
            PayDate = reader.IsDBNull(8) ? null : reader.GetDateTime(8),
            ReaderName = reader.IsDBNull(9) ? null : reader.GetString(9),
            BookName = reader.IsDBNull(10) ? null : reader.GetString(10)
        };
}
