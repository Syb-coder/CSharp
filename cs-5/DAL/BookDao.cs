using Microsoft.Data.SqlClient;
using LibrarySys.Models;
using LibrarySys.Common;

namespace LibrarySys.DAL;

/// <summary>
/// 图书数据访问类，继承泛型基类 BaseRepository&lt;BookInfo&gt;
/// </summary>
public class BookDao : BaseRepository<BookInfo>
{
    /// <summary>查询全部图书（联表 T_BookType 获取类型名称）</summary>
    public override List<BookInfo> FindAll()
    {
        const string sql = @"SELECT b.bookID, b.bookName, b.author, b.publisher, b.publishDate, b.ISBN, b.price,
                                   b.typeID, b.totalCount, b.availableCount, t.typeName
                            FROM T_Book b
                            LEFT JOIN T_BookType t ON b.typeID = t.typeID
                            ORDER BY b.bookID";
        List<BookInfo> list = new();
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

    /// <summary>按图书编号查询单条记录</summary>
    public override BookInfo FindById(string id)
    {
        const string sql = @"SELECT b.bookID, b.bookName, b.author, b.publisher, b.publishDate, b.ISBN, b.price,
                                   b.typeID, b.totalCount, b.availableCount, t.typeName
                            FROM T_Book b
                            LEFT JOIN T_BookType t ON b.typeID = t.typeID
                            WHERE b.bookID = @bookID";
        using SqlConnection conn = new(DBConnection.GetConnectionString());
        conn.Open();
        using SqlCommand cmd = new(sql, conn);
        cmd.Parameters.AddWithValue("@bookID", id);
        using SqlDataReader reader = cmd.ExecuteReader();
        return reader.Read() ? MapReader(reader) : null;
    }

    /// <summary>新增图书（可借数量初始化等于馆藏数量）</summary>
    public override int Insert(BookInfo entity)
    {
        const string sql = @"INSERT INTO T_Book (bookID, bookName, author, publisher, publishDate, ISBN, price, typeID, totalCount, availableCount)
                             VALUES (@bookID, @bookName, @author, @publisher, @publishDate, @ISBN, @price, @typeID, @totalCount, @availableCount)";
        using SqlConnection conn = new(DBConnection.GetConnectionString());
        conn.Open();
        using SqlCommand cmd = new(sql, conn);
        AddParameters(cmd, entity);
        // 新书入库时可借数量 = 馆藏数量
        cmd.Parameters.AddWithValue("@availableCount", entity.TotalCount);
        return cmd.ExecuteNonQuery();
    }

    /// <summary>修改图书信息</summary>
    public override int Update(BookInfo entity)
    {
        const string sql = @"UPDATE T_Book SET bookName = @bookName, author = @author, publisher = @publisher,
                             publishDate = @publishDate, ISBN = @ISBN, price = @price, typeID = @typeID,
                             totalCount = @totalCount WHERE bookID = @bookID";
        using SqlConnection conn = new(DBConnection.GetConnectionString());
        conn.Open();
        using SqlCommand cmd = new(sql, conn);
        AddParameters(cmd, entity);
        return cmd.ExecuteNonQuery();
    }

    /// <summary>按图书编号删除</summary>
    public override int Delete(string id)
    {
        const string sql = "DELETE FROM T_Book WHERE bookID = @bookID";
        using SqlConnection conn = new(DBConnection.GetConnectionString());
        conn.Open();
        using SqlCommand cmd = new(sql, conn);
        cmd.Parameters.AddWithValue("@bookID", id);
        return cmd.ExecuteNonQuery();
    }

    // ===== 以下为 Book 特有查询 =====

    /// <summary>按书名、类型、出版社多条件组合查询</summary>
    public List<BookInfo> Search(string bookName, string typeID, string publisher)
    {
        List<string> conditions = new();
        if (!string.IsNullOrWhiteSpace(bookName))
            conditions.Add("b.bookName LIKE '%' + @bookName + '%'");
        if (!string.IsNullOrWhiteSpace(typeID))
            conditions.Add("b.typeID = @typeID");
        if (!string.IsNullOrWhiteSpace(publisher))
            conditions.Add("b.publisher LIKE '%' + @publisher + '%'");

        string sql = @"SELECT b.bookID, b.bookName, b.author, b.publisher, b.publishDate, b.ISBN, b.price,
                              b.typeID, b.totalCount, b.availableCount, t.typeName
                       FROM T_Book b
                       LEFT JOIN T_BookType t ON b.typeID = t.typeID";
        if (conditions.Count > 0)
            sql += " WHERE " + string.Join(" AND ", conditions);
        sql += " ORDER BY b.bookID";

        List<BookInfo> list = new();
        using SqlConnection conn = new(DBConnection.GetConnectionString());
        conn.Open();
        using SqlCommand cmd = new(sql, conn);
        if (!string.IsNullOrWhiteSpace(bookName))
            cmd.Parameters.AddWithValue("@bookName", bookName);
        if (!string.IsNullOrWhiteSpace(typeID))
            cmd.Parameters.AddWithValue("@typeID", typeID);
        if (!string.IsNullOrWhiteSpace(publisher))
            cmd.Parameters.AddWithValue("@publisher", publisher);
        using SqlDataReader reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(MapReader(reader));
        }
        return list;
    }

    /// <summary>
    /// 扣减可借数量（并发控制：WHERE availableCount > 0 防止超卖）
    /// 通过受影响行数判断是否扣减成功：1=成功，0=库存不足
    /// </summary>
    /// <param name="bookID">图书编号</param>
    /// <returns>受影响行数（1=成功，0=可借数量不足）</returns>
    public int DecreaseAvailableCount(string bookID)
    {
        // 乐观锁模式：WHERE 条件中包含 availableCount > 0，避免并发借书时超卖
        const string sql = "UPDATE T_Book SET availableCount = availableCount - 1 WHERE bookID = @bookID AND availableCount > 0";
        using SqlConnection conn = new(DBConnection.GetConnectionString());
        conn.Open();
        using SqlCommand cmd = new(sql, conn);
        cmd.Parameters.AddWithValue("@bookID", bookID);
        return cmd.ExecuteNonQuery();
    }

    /// <summary>恢复可借数量（还书时调用）</summary>
    public int IncreaseAvailableCount(string bookID)
    {
        const string sql = "UPDATE T_Book SET availableCount = availableCount + 1 WHERE bookID = @bookID";
        using SqlConnection conn = new(DBConnection.GetConnectionString());
        conn.Open();
        using SqlCommand cmd = new(sql, conn);
        cmd.Parameters.AddWithValue("@bookID", bookID);
        return cmd.ExecuteNonQuery();
    }

    /// <summary>检查图书是否有未归还的借阅记录（删除前校验）</summary>
    public bool HasActiveBorrow(string bookID)
    {
        const string sql = "SELECT COUNT(1) FROM T_Borrow WHERE bookID = @bookID AND status = @status";
        using SqlConnection conn = new(DBConnection.GetConnectionString());
        conn.Open();
        using SqlCommand cmd = new(sql, conn);
        cmd.Parameters.AddWithValue("@bookID", bookID);
        cmd.Parameters.AddWithValue("@status", BusinessConstants.STATUS_BORROWED);
        return (int)cmd.ExecuteScalar() > 0;
    }

    /// <summary>检查图书是否有未缴清的罚款（删除前校验）</summary>
    public bool HasUnpaidFine(string bookID)
    {
        const string sql = "SELECT COUNT(1) FROM T_Fine WHERE bookID = @bookID AND fineStatus = @fineStatus";
        using SqlConnection conn = new(DBConnection.GetConnectionString());
        conn.Open();
        using SqlCommand cmd = new(sql, conn);
        cmd.Parameters.AddWithValue("@bookID", bookID);
        cmd.Parameters.AddWithValue("@fineStatus", BusinessConstants.FINE_UNPAID);
        return (int)cmd.ExecuteScalar() > 0;
    }

    /// <summary>检查图书是否有活跃预约（排队中/待取书），删除前需先取消</summary>
    public bool HasActiveReservation(string bookID)
    {
        const string sql = @"SELECT COUNT(1) FROM T_Reservation
                             WHERE bookID = @bookID AND status IN (N'排队中', N'待取书')";
        using SqlConnection conn = new(DBConnection.GetConnectionString());
        conn.Open();
        using SqlCommand cmd = new(sql, conn);
        cmd.Parameters.AddWithValue("@bookID", bookID);
        return (int)cmd.ExecuteScalar() > 0;
    }

    /// <summary>
    /// 级联删除图书及其所有关联历史记录（在事务中执行）
    /// 删除顺序：T_Fine → T_Reservation → T_Borrow → T_Book
    /// </summary>
    /// <param name="bookID">图书编号</param>
    public void DeleteCascade(string bookID)
    {
        using SqlConnection conn = new(DBConnection.GetConnectionString());
        conn.Open();
        using SqlTransaction tran = conn.BeginTransaction();
        try
        {
            const string sqlFine = "DELETE FROM T_Fine WHERE bookID = @bookID";
            using (SqlCommand cmd = new(sqlFine, conn, tran))
            {
                cmd.Parameters.AddWithValue("@bookID", bookID);
                cmd.ExecuteNonQuery();
            }

            const string sqlReserve = "DELETE FROM T_Reservation WHERE bookID = @bookID";
            using (SqlCommand cmd = new(sqlReserve, conn, tran))
            {
                cmd.Parameters.AddWithValue("@bookID", bookID);
                cmd.ExecuteNonQuery();
            }

            const string sqlBorrow = "DELETE FROM T_Borrow WHERE bookID = @bookID";
            using (SqlCommand cmd = new(sqlBorrow, conn, tran))
            {
                cmd.Parameters.AddWithValue("@bookID", bookID);
                cmd.ExecuteNonQuery();
            }

            const string sqlBook = "DELETE FROM T_Book WHERE bookID = @bookID";
            using (SqlCommand cmd = new(sqlBook, conn, tran))
            {
                cmd.Parameters.AddWithValue("@bookID", bookID);
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

    /// <summary>添加图书实体参数到 SqlCommand（Insert/Update 共用）</summary>
    private static void AddParameters(SqlCommand cmd, BookInfo entity)
    {
        cmd.Parameters.AddWithValue("@bookID", entity.BookID);
        cmd.Parameters.AddWithValue("@bookName", entity.BookName);
        cmd.Parameters.AddWithValue("@author", (object)entity.Author ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@publisher", (object)entity.Publisher ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@publishDate", (object)entity.PublishDate ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@ISBN", (object)entity.ISBN ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@price", (object)entity.Price ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@typeID", entity.TypeID);
        cmd.Parameters.AddWithValue("@totalCount", entity.TotalCount);
    }

    /// <summary>SqlDataReader 映射为 BookInfo 实体</summary>
    private static BookInfo MapReader(SqlDataReader reader)
        => new()
        {
            BookID = reader.GetString(0),
            BookName = reader.GetString(1),
            Author = reader.IsDBNull(2) ? null : reader.GetString(2),
            Publisher = reader.IsDBNull(3) ? null : reader.GetString(3),
            PublishDate = reader.IsDBNull(4) ? null : reader.GetDateTime(4),
            ISBN = reader.IsDBNull(5) ? null : reader.GetString(5),
            Price = reader.IsDBNull(6) ? null : reader.GetDecimal(6),
            TypeID = reader.GetString(7),
            TotalCount = reader.GetInt32(8),
            AvailableCount = reader.GetInt32(9),
            TypeName = reader.IsDBNull(10) ? null : reader.GetString(10)
        };
}
