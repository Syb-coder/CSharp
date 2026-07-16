using System.Data;
using Microsoft.Data.SqlClient;
using LibraryManagement.Models;

namespace LibraryManagement.DAL;

/// <summary>
/// 图书数据访问类，对应 tbl_Book 表的 CRUD 操作
/// </summary>
public class BookDAL
{
    /// <summary>
    /// 查询全部图书（含类别名称联表查询）
    /// </summary>
    /// <returns>图书列表</returns>
    public List<Book> GetAllBooks()
    {
        // 使用 LEFT JOIN 而非 INNER JOIN：若 tbl_Book 中存在 categoryID 未在 tbl_BookCategory 中登记的脏数据，
        // LEFT JOIN 仍能返回这些图书记录，categoryName 为 NULL，避免数据"消失"
        const string sql = @"
            SELECT b.bookID, b.bookName, b.author, b.publisher, b.publishDate,
                   b.ISBN, b.price, b.categoryID, b.totalCount, b.availableCount,
                   c.categoryName
            FROM tbl_Book b
            LEFT JOIN tbl_BookCategory c ON b.categoryID = c.categoryID
            ORDER BY b.bookID";
        List<Book> list = new();
        using SqlConnection conn = new(DBConnection.GetConnectionString());
        using SqlCommand cmd = new(sql, conn);
        conn.Open();
        using SqlDataReader reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(MapReaderToBook(reader));
        }
        return list;
    }

    /// <summary>
    /// 多条件组合查询图书
    /// </summary>
    /// <param name="bookName">书名关键字（可空）</param>
    /// <param name="author">作者关键字（可空）</param>
    /// <param name="categoryID">类别编号（可空，空表示不限类别）</param>
    /// <param name="isbn">ISBN 号（可空）</param>
    /// <returns>图书列表</returns>
    public List<Book> SearchBooks(string bookName, string author, string categoryID, string isbn)
    {
        // 多条件查询采用 "@param IS NULL OR ..." 模式：
        // 当参数为 NULL 时条件恒真（不参与过滤），参数非 NULL 时才按值过滤。
        // 这种模式避免了在 C# 中动态拼接 SQL 字符串，既防注入又简化代码，单条 SQL 即覆盖所有条件组合
        const string sql = @"
            SELECT b.bookID, b.bookName, b.author, b.publisher, b.publishDate,
                   b.ISBN, b.price, b.categoryID, b.totalCount, b.availableCount,
                   c.categoryName
            FROM tbl_Book b
            LEFT JOIN tbl_BookCategory c ON b.categoryID = c.categoryID
            WHERE (@bookName IS NULL OR b.bookName LIKE @bookName)
              AND (@author IS NULL OR b.author LIKE @author)
              AND (@categoryID IS NULL OR b.categoryID = @categoryID)
              AND (@isbn IS NULL OR b.ISBN = @isbn)
            ORDER BY b.bookID";
        List<Book> list = new();
        using SqlConnection conn = new(DBConnection.GetConnectionString());
        using SqlCommand cmd = new(sql, conn);
        // 空白字符串转为 DBNull.Value，配合 SQL 中的 IS NULL 判断跳过该条件
        cmd.Parameters.Add(new SqlParameter("@bookName", SqlDbType.NVarChar, 100)
        {
            Value = string.IsNullOrWhiteSpace(bookName) ? DBNull.Value : $"%{bookName}%"
        });
        cmd.Parameters.Add(new SqlParameter("@author", SqlDbType.NVarChar, 50)
        {
            Value = string.IsNullOrWhiteSpace(author) ? DBNull.Value : $"%{author}%"
        });
        // categoryID 和 isbn 为精确匹配（不加 % 通配符），因为编号是确定性查询
        cmd.Parameters.Add(new SqlParameter("@categoryID", SqlDbType.NVarChar, 10)
        {
            Value = string.IsNullOrWhiteSpace(categoryID) ? DBNull.Value : categoryID
        });
        cmd.Parameters.Add(new SqlParameter("@isbn", SqlDbType.NVarChar, 13)
        {
            Value = string.IsNullOrWhiteSpace(isbn) ? DBNull.Value : isbn
        });
        conn.Open();
        using SqlDataReader reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(MapReaderToBook(reader));
        }
        return list;
    }

    /// <summary>
    /// 根据图书编号查询单本图书
    /// </summary>
    /// <param name="bookID">图书编号</param>
    /// <returns>图书实体，未找到返回 null</returns>
    public Book GetBookById(string bookID)
    {
        const string sql = @"
            SELECT b.bookID, b.bookName, b.author, b.publisher, b.publishDate,
                   b.ISBN, b.price, b.categoryID, b.totalCount, b.availableCount,
                   c.categoryName
            FROM tbl_Book b
            LEFT JOIN tbl_BookCategory c ON b.categoryID = c.categoryID
            WHERE b.bookID = @bookID";
        using SqlConnection conn = new(DBConnection.GetConnectionString());
        using SqlCommand cmd = new(sql, conn);
        SafeAddParam(cmd, "@bookID", SqlDbType.NVarChar, 20, bookID);
        conn.Open();
        using SqlDataReader reader = cmd.ExecuteReader();
        if (reader.Read())
        {
            return MapReaderToBook(reader);
        }
        return null;
    }

    /// <summary>
    /// 新增图书
    /// </summary>
    /// <param name="book">图书实体</param>
    /// <returns>成功返回 true，编号已存在返回 false</returns>
    public bool InsertBook(Book book)
    {
        const string sql = @"
            INSERT INTO tbl_Book (bookID, bookName, author, publisher, publishDate, ISBN, price, categoryID, totalCount, availableCount)
            VALUES (@bookID, @bookName, @author, @publisher, @publishDate, @ISBN, @price, @categoryID, @totalCount, @availableCount)";
        using SqlConnection conn = new(DBConnection.GetConnectionString());
        using SqlCommand cmd = new(sql, conn);
        AddBookParameters(cmd, book);
        conn.Open();
        try
        {
            return cmd.ExecuteNonQuery() > 0;
        }
        catch (SqlException ex) when (ex.Number == 2627) // 主键冲突：bookID 已存在
        {
            return false;
        }
    }

    /// <summary>
    /// 修改图书信息
    /// </summary>
    /// <param name="book">图书实体</param>
    /// <returns>成功返回 true</returns>
    public bool UpdateBook(Book book)
    {
        const string sql = @"
            UPDATE tbl_Book SET
                bookName = @bookName, author = @author, publisher = @publisher,
                publishDate = @publishDate, ISBN = @ISBN, price = @price,
                categoryID = @categoryID, totalCount = @totalCount, availableCount = @availableCount
            WHERE bookID = @bookID";
        using SqlConnection conn = new(DBConnection.GetConnectionString());
        using SqlCommand cmd = new(sql, conn);
        AddBookParameters(cmd, book);
        conn.Open();
        return cmd.ExecuteNonQuery() > 0;
    }

    /// <summary>
    /// 删除图书
    /// </summary>
    /// <param name="bookID">图书编号</param>
    /// <returns>成功返回 true</returns>
    public bool DeleteBook(string bookID)
    {
        const string sql = "DELETE FROM tbl_Book WHERE bookID = @bookID";
        using SqlConnection conn = new(DBConnection.GetConnectionString());
        using SqlCommand cmd = new(sql, conn);
        SafeAddParam(cmd, "@bookID", SqlDbType.NVarChar, 20, bookID);
        conn.Open();
        return cmd.ExecuteNonQuery() > 0;
    }

    /// <summary>
    /// 统计指定图书的未归还借阅数量
    /// </summary>
    /// <param name="bookID">图书编号</param>
    /// <returns>未归还数量</returns>
    public int CountUnreturnedBorrows(string bookID)
    {
        const string sql = "SELECT COUNT(1) FROM tbl_Borrow WHERE bookID = @bookID AND status = N'借出'";
        using SqlConnection conn = new(DBConnection.GetConnectionString());
        using SqlCommand cmd = new(sql, conn);
        SafeAddParam(cmd, "@bookID", SqlDbType.NVarChar, 20, bookID);
        conn.Open();
        return (int)cmd.ExecuteScalar();
    }

    /// <summary>
    /// 在指定事务中扣减图书可借数量（原子操作，受 availableCount > 0 条件保护）
    /// </summary>
    /// <param name="conn">已打开的连接</param>
    /// <param name="transaction">所属事务</param>
    /// <param name="bookID">图书编号</param>
    /// <returns>扣减成功返回 true（受影响行数=1），可借数量不足返回 false</returns>
    public bool DecreaseAvailableCount(SqlConnection conn, SqlTransaction transaction, string bookID)
    {
        // WHERE 条件中加 availableCount > 0 是并发安全的关键：
        // 即使两个线程同时执行此 UPDATE，数据库的行级锁会串行化执行，
        // 第一个线程扣减后 availableCount 变为 0，第二个线程因条件不满足返回 0 行，从而感知并发冲突
        const string sql = "UPDATE tbl_Book SET availableCount = availableCount - 1 WHERE bookID = @bookID AND availableCount > 0";
        // conn 和 transaction 由 BLL 层传入，此方法不自行创建连接，保证与借阅记录插入在同一事务中提交或回滚
        using SqlCommand cmd = new(sql, conn, transaction);
        SafeAddParam(cmd, "@bookID", SqlDbType.NVarChar, 20, bookID);
        return cmd.ExecuteNonQuery() > 0;
    }

    /// <summary>
    /// 在指定事务中恢复图书可借数量
    /// </summary>
    /// <param name="conn">已打开的连接</param>
    /// <param name="transaction">所属事务</param>
    /// <param name="bookID">图书编号</param>
    /// <returns>恢复成功返回 true</returns>
    public bool IncreaseAvailableCount(SqlConnection conn, SqlTransaction transaction, string bookID)
    {
        // 还书恢复数量无需 availableCount > 上限 保护：因为原始借出时已经扣减过，恢复不会超过 totalCount
        const string sql = "UPDATE tbl_Book SET availableCount = availableCount + 1 WHERE bookID = @bookID";
        using SqlCommand cmd = new(sql, conn, transaction);
        SafeAddParam(cmd, "@bookID", SqlDbType.NVarChar, 20, bookID);
        return cmd.ExecuteNonQuery() > 0;
    }

    /// <summary>
    /// 安全添加参数：若同名参数已存在则更新值，否则新增
    /// 这是比 Clear() 更可靠的防御性写法，避免某些库版本中 Clear() 不彻底的问题
    /// </summary>
    private static void SafeAddParam(SqlCommand cmd, string name, SqlDbType type, int size, object value)
    {
        if (cmd.Parameters.Contains(name))
        {
            cmd.Parameters[name].Value = value ?? DBNull.Value;
        }
        else
        {
            cmd.Parameters.Add(new SqlParameter(name, type, size) { Value = value ?? DBNull.Value });
        }
    }

    /// <summary>
    /// 添加图书参数到 SqlCommand
    /// </summary>
    private static void AddBookParameters(SqlCommand cmd, Book book)
    {
        SafeAddParam(cmd, "@bookID", SqlDbType.NVarChar, 20, book.BookID);
        SafeAddParam(cmd, "@bookName", SqlDbType.NVarChar, 100, book.BookName);
        // nullable 字段（作者、出版社等非必填项）的空值必须转为 DBNull.Value，
        // 因为 SQL Server 不接受 C# 的 null 作为参数值，传入 null 会报异常
        SafeAddParam(cmd, "@author", SqlDbType.NVarChar, 50,
            string.IsNullOrEmpty(book.Author) ? DBNull.Value : book.Author);
        SafeAddParam(cmd, "@publisher", SqlDbType.NVarChar, 50,
            string.IsNullOrEmpty(book.Publisher) ? DBNull.Value : book.Publisher);
        SafeAddParam(cmd, "@publishDate", SqlDbType.Date, 0,
            book.PublishDate.HasValue ? book.PublishDate.Value : DBNull.Value);
        SafeAddParam(cmd, "@ISBN", SqlDbType.NVarChar, 13,
            string.IsNullOrEmpty(book.ISBN) ? DBNull.Value : book.ISBN);
        SafeAddParam(cmd, "@price", SqlDbType.Decimal, 0,
            book.Price.HasValue ? book.Price.Value : DBNull.Value);
        SafeAddParam(cmd, "@categoryID", SqlDbType.NVarChar, 10, book.CategoryID);
        SafeAddParam(cmd, "@totalCount", SqlDbType.Int, 0, book.TotalCount);
        SafeAddParam(cmd, "@availableCount", SqlDbType.Int, 0, book.AvailableCount);
    }

    /// <summary>
    /// 将 SqlDataReader 映射为 Book 实体
    /// </summary>
    private static Book MapReaderToBook(SqlDataReader reader)
    {
        return new Book
        {
            BookID = reader["bookID"].ToString(),
            BookName = reader["bookName"].ToString(),
            Author = reader["author"] == DBNull.Value ? null : reader["author"].ToString(),
            Publisher = reader["publisher"] == DBNull.Value ? null : reader["publisher"].ToString(),
            PublishDate = reader["publishDate"] == DBNull.Value ? null : (DateTime?)reader["publishDate"],
            ISBN = reader["ISBN"] == DBNull.Value ? null : reader["ISBN"].ToString(),
            Price = reader["price"] == DBNull.Value ? null : (decimal?)reader["price"],
            CategoryID = reader["categoryID"].ToString(),
            TotalCount = Convert.ToInt32(reader["totalCount"]),
            AvailableCount = Convert.ToInt32(reader["availableCount"]),
            CategoryName = reader["categoryName"] == DBNull.Value ? null : reader["categoryName"].ToString()
        };
    }
}
