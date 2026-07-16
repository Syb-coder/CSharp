using Microsoft.Data.SqlClient;
using LibrarySys.Models;
using LibrarySys.Common;

namespace LibrarySys.DAL;

/// <summary>
/// 读者数据访问类，继承泛型基类 BaseRepository<ReaderInfo>
/// </summary>
public class ReaderDao : BaseRepository<ReaderInfo>
{
    /// <summary>查询全部读者</summary>
    public override List<ReaderInfo> FindAll()
    {
        const string sql = @"SELECT readerID, readerName, readerSex, phone, department, registerDate
                             FROM T_Reader ORDER BY readerID";
        List<ReaderInfo> list = new();
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

    /// <summary>按读者编号查询单条记录</summary>
    public override ReaderInfo FindById(string id)
    {
        const string sql = @"SELECT readerID, readerName, readerSex, phone, department, registerDate
                             FROM T_Reader WHERE readerID = @readerID";
        using SqlConnection conn = new(DBConnection.GetConnectionString());
        conn.Open();
        using SqlCommand cmd = new(sql, conn);
        cmd.Parameters.AddWithValue("@readerID", id);
        using SqlDataReader reader = cmd.ExecuteReader();
        return reader.Read() ? MapReader(reader) : null;
    }

    /// <summary>新增读者</summary>
    public override int Insert(ReaderInfo entity)
    {
        const string sql = @"INSERT INTO T_Reader (readerID, readerName, readerSex, phone, department, registerDate)
                             VALUES (@readerID, @readerName, @readerSex, @phone, @department, @registerDate)";
        using SqlConnection conn = new(DBConnection.GetConnectionString());
        conn.Open();
        using SqlCommand cmd = new(sql, conn);
        cmd.Parameters.AddWithValue("@readerID", entity.ReaderID);
        cmd.Parameters.AddWithValue("@readerName", entity.ReaderName);
        cmd.Parameters.AddWithValue("@readerSex", entity.ReaderSex);
        cmd.Parameters.AddWithValue("@phone", (object)entity.Phone ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@department", (object)entity.Department ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@registerDate", (object)entity.RegisterDate ?? DBNull.Value);
        return cmd.ExecuteNonQuery();
    }

    /// <summary>修改读者信息</summary>
    public override int Update(ReaderInfo entity)
    {
        const string sql = @"UPDATE T_Reader SET readerName = @readerName, readerSex = @readerSex,
                             phone = @phone, department = @department, registerDate = @registerDate
                             WHERE readerID = @readerID";
        using SqlConnection conn = new(DBConnection.GetConnectionString());
        conn.Open();
        using SqlCommand cmd = new(sql, conn);
        cmd.Parameters.AddWithValue("@readerName", entity.ReaderName);
        cmd.Parameters.AddWithValue("@readerSex", entity.ReaderSex);
        cmd.Parameters.AddWithValue("@phone", (object)entity.Phone ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@department", (object)entity.Department ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@registerDate", (object)entity.RegisterDate ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@readerID", entity.ReaderID);
        return cmd.ExecuteNonQuery();
    }

    /// <summary>按读者编号删除（仅删除读者本身，调用前需确保无外键引用）</summary>
    public override int Delete(string id)
    {
        const string sql = "DELETE FROM T_Reader WHERE readerID = @readerID";
        using SqlConnection conn = new(DBConnection.GetConnectionString());
        conn.Open();
        using SqlCommand cmd = new(sql, conn);
        cmd.Parameters.AddWithValue("@readerID", id);
        return cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// 级联删除读者及其所有关联历史记录（在事务中执行）
    /// 删除顺序：T_Fine → T_Reservation → T_Borrow → T_Reader（按外键依赖反向删除）
    /// </summary>
    /// <param name="readerID">读者编号</param>
    public void DeleteCascade(string readerID)
    {
        using SqlConnection conn = new(DBConnection.GetConnectionString());
        conn.Open();
        using SqlTransaction tran = conn.BeginTransaction();
        try
        {
            // 1. 删除该读者的所有罚款记录（T_Fine 外键依赖 T_Borrow）
            const string sqlFine = "DELETE FROM T_Fine WHERE readerID = @readerID";
            using (SqlCommand cmd = new(sqlFine, conn, tran))
            {
                cmd.Parameters.AddWithValue("@readerID", readerID);
                cmd.ExecuteNonQuery();
            }

            // 2. 删除该读者的所有预约记录
            const string sqlReserve = "DELETE FROM T_Reservation WHERE readerID = @readerID";
            using (SqlCommand cmd = new(sqlReserve, conn, tran))
            {
                cmd.Parameters.AddWithValue("@readerID", readerID);
                cmd.ExecuteNonQuery();
            }

            // 3. 删除该读者的所有借阅记录
            const string sqlBorrow = "DELETE FROM T_Borrow WHERE readerID = @readerID";
            using (SqlCommand cmd = new(sqlBorrow, conn, tran))
            {
                cmd.Parameters.AddWithValue("@readerID", readerID);
                cmd.ExecuteNonQuery();
            }

            // 4. 最后删除读者本身
            const string sqlReader = "DELETE FROM T_Reader WHERE readerID = @readerID";
            using (SqlCommand cmd = new(sqlReader, conn, tran))
            {
                cmd.Parameters.AddWithValue("@readerID", readerID);
                cmd.ExecuteNonQuery();
            }

            tran.Commit();
        }
        catch
        {
            tran.Rollback();
            throw;
        }
    }

    // ===== 以下为 Reader 特有查询 =====

    /// <summary>按编号、姓名多条件组合查询（条件为空时忽略该条件）</summary>
    public List<ReaderInfo> Search(string readerID, string readerName)
    {
        List<string> conditions = new();
        if (!string.IsNullOrWhiteSpace(readerID))
            conditions.Add("readerID LIKE '%' + @readerID + '%'");
        if (!string.IsNullOrWhiteSpace(readerName))
            conditions.Add("readerName LIKE '%' + @readerName + '%'");

        string sql = @"SELECT readerID, readerName, readerSex, phone, department, registerDate
                       FROM T_Reader";
        if (conditions.Count > 0)
            sql += " WHERE " + string.Join(" AND ", conditions);
        sql += " ORDER BY readerID";

        List<ReaderInfo> list = new();
        using SqlConnection conn = new(DBConnection.GetConnectionString());
        conn.Open();
        using SqlCommand cmd = new(sql, conn);
        if (!string.IsNullOrWhiteSpace(readerID))
            cmd.Parameters.AddWithValue("@readerID", readerID);
        if (!string.IsNullOrWhiteSpace(readerName))
            cmd.Parameters.AddWithValue("@readerName", readerName);
        using SqlDataReader reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(MapReader(reader));
        }
        return list;
    }

    /// <summary>检查读者是否有未归还的借阅记录（删除前校验）</summary>
    public bool HasActiveBorrow(string readerID)
    {
        const string sql = "SELECT COUNT(1) FROM T_Borrow WHERE readerID = @readerID AND status = @status";
        using SqlConnection conn = new(DBConnection.GetConnectionString());
        conn.Open();
        using SqlCommand cmd = new(sql, conn);
        cmd.Parameters.AddWithValue("@readerID", readerID);
        cmd.Parameters.AddWithValue("@status", BusinessConstants.STATUS_BORROWED);
        return (int)cmd.ExecuteScalar() > 0;
    }

    /// <summary>统计读者当前借出未还的图书数量（借阅上限校验）</summary>
    public int CountActiveBorrow(string readerID)
    {
        const string sql = "SELECT COUNT(1) FROM T_Borrow WHERE readerID = @readerID AND status = @status";
        using SqlConnection conn = new(DBConnection.GetConnectionString());
        conn.Open();
        using SqlCommand cmd = new(sql, conn);
        cmd.Parameters.AddWithValue("@readerID", readerID);
        cmd.Parameters.AddWithValue("@status", BusinessConstants.STATUS_BORROWED);
        return (int)cmd.ExecuteScalar();
    }

    /// <summary>检查读者是否有未缴清罚款（删除前校验 / 借书前校验）</summary>
    public bool HasUnpaidFine(string readerID)
    {
        const string sql = "SELECT COUNT(1) FROM T_Fine WHERE readerID = @readerID AND fineStatus = @fineStatus";
        using SqlConnection conn = new(DBConnection.GetConnectionString());
        conn.Open();
        using SqlCommand cmd = new(sql, conn);
        cmd.Parameters.AddWithValue("@readerID", readerID);
        cmd.Parameters.AddWithValue("@fineStatus", BusinessConstants.FINE_UNPAID);
        return (int)cmd.ExecuteScalar() > 0;
    }

    /// <summary>检查读者是否有逾期未还的图书（借书前校验，PRD AC-10b）</summary>
    public bool HasOverdueBorrow(string readerID)
    {
        const string sql = @"SELECT COUNT(1) FROM T_Borrow
                             WHERE readerID = @readerID AND status = @status
                               AND dueDate < CAST(GETDATE() AS DATE)";
        using SqlConnection conn = new(DBConnection.GetConnectionString());
        conn.Open();
        using SqlCommand cmd = new(sql, conn);
        cmd.Parameters.AddWithValue("@readerID", readerID);
        cmd.Parameters.AddWithValue("@status", BusinessConstants.STATUS_BORROWED);
        return (int)cmd.ExecuteScalar() > 0;
    }

    /// <summary>获取读者逾期未还的图书列表（含书名，用于友好提示）</summary>
    public List<string> GetOverdueBookNames(string readerID)
    {
        const string sql = @"SELECT b.bookName
                             FROM T_Borrow br
                             JOIN T_Book b ON br.bookID = b.bookID
                             WHERE br.readerID = @readerID AND br.status = @status
                               AND br.dueDate < CAST(GETDATE() AS DATE)
                             ORDER BY br.dueDate";
        List<string> books = new();
        using SqlConnection conn = new(DBConnection.GetConnectionString());
        conn.Open();
        using SqlCommand cmd = new(sql, conn);
        cmd.Parameters.AddWithValue("@readerID", readerID);
        cmd.Parameters.AddWithValue("@status", BusinessConstants.STATUS_BORROWED);
        using SqlDataReader reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            books.Add(reader.GetString(0));
        }
        return books;
    }

    /// <summary>检查读者是否有活跃预约（排队中/待取书），删除前需先取消</summary>
    public bool HasActiveReservation(string readerID)
    {
        const string sql = @"SELECT COUNT(1) FROM T_Reservation
                             WHERE readerID = @readerID AND status IN (N'排队中', N'待取书')";
        using SqlConnection conn = new(DBConnection.GetConnectionString());
        conn.Open();
        using SqlCommand cmd = new(sql, conn);
        cmd.Parameters.AddWithValue("@readerID", readerID);
        return (int)cmd.ExecuteScalar() > 0;
    }

    /// <summary>SqlDataReader 映射为 ReaderInfo 实体</summary>
    private static ReaderInfo MapReader(SqlDataReader reader)
        => new()
        {
            ReaderID = reader.GetString(0),
            ReaderName = reader.GetString(1),
            ReaderSex = reader.IsDBNull(2) ? null : reader.GetString(2),
            Phone = reader.IsDBNull(3) ? null : reader.GetString(3),
            Department = reader.IsDBNull(4) ? null : reader.GetString(4),
            RegisterDate = reader.IsDBNull(5) ? null : reader.GetDateTime(5)
        };
}
