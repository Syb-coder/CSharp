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
        cmd.Parameters.Add(new SqlParameter("@bookName", SqlDbType.NVarChar, 100)
        {
            Value = string.IsNullOrWhiteSpace(bookName) ? DBNull.Value : $"%{bookName}%"
        });
        cmd.Parameters.Add(new SqlParameter("@author", SqlDbType.NVarChar, 50)
        {
            Value = string.IsNullOrWhiteSpace(author) ? DBNull.Value : $"%{author}%"
        });
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
        cmd.Parameters.Add(new SqlParameter("@bookID", SqlDbType.NVarChar, 20) { Value = bookID });
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
        catch (SqlException ex) when (ex.Number == 2627)
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
        cmd.Parameters.Add(new SqlParameter("@bookID", SqlDbType.NVarChar, 20) { Value = bookID });
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
        cmd.Parameters.Add(new SqlParameter("@bookID", SqlDbType.NVarChar, 20) { Value = bookID });
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
        const string sql = "UPDATE tbl_Book SET availableCount = availableCount - 1 WHERE bookID = @bookID AND availableCount > 0";
        using SqlCommand cmd = new(sql, conn, transaction);
        cmd.Parameters.Add(new SqlParameter("@bookID", SqlDbType.NVarChar, 20) { Value = bookID });
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
        const string sql = "UPDATE tbl_Book SET availableCount = availableCount + 1 WHERE bookID = @bookID";
        using SqlCommand cmd = new(sql, conn, transaction);
        cmd.Parameters.Add(new SqlParameter("@bookID", SqlDbType.NVarChar, 20) { Value = bookID });
        return cmd.ExecuteNonQuery() > 0;
    }

    /// <summary>
    /// 添加图书参数到 SqlCommand
    /// </summary>
    private static void AddBookParameters(SqlCommand cmd, Book book)
    {
        cmd.Parameters.Add(new SqlParameter("@bookID", SqlDbType.NVarChar, 20) { Value = book.BookID });
        cmd.Parameters.Add(new SqlParameter("@bookName", SqlDbType.NVarChar, 100) { Value = book.BookName });
        cmd.Parameters.Add(new SqlParameter("@author", SqlDbType.NVarChar, 50)
        {
            Value = string.IsNullOrEmpty(book.Author) ? DBNull.Value : book.Author
        });
        cmd.Parameters.Add(new SqlParameter("@publisher", SqlDbType.NVarChar, 50)
        {
            Value = string.IsNullOrEmpty(book.Publisher) ? DBNull.Value : book.Publisher
        });
        cmd.Parameters.Add(new SqlParameter("@publishDate", SqlDbType.Date)
        {
            Value = book.PublishDate.HasValue ? book.PublishDate.Value : DBNull.Value
        });
        cmd.Parameters.Add(new SqlParameter("@ISBN", SqlDbType.NVarChar, 13)
        {
            Value = string.IsNullOrEmpty(book.ISBN) ? DBNull.Value : book.ISBN
        });
        cmd.Parameters.Add(new SqlParameter("@price", SqlDbType.Decimal)
        {
            Value = book.Price.HasValue ? book.Price.Value : DBNull.Value
        });
        cmd.Parameters.Add(new SqlParameter("@categoryID", SqlDbType.NVarChar, 10) { Value = book.CategoryID });
        cmd.Parameters.Add(new SqlParameter("@totalCount", SqlDbType.Int) { Value = book.TotalCount });
        cmd.Parameters.Add(new SqlParameter("@availableCount", SqlDbType.Int) { Value = book.AvailableCount });
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
