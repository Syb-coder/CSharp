using System.Data;
using Microsoft.Data.SqlClient;
using LibraryManagement.Models;

namespace LibraryManagement.DAL;

/// <summary>
/// 图书类别数据访问类，对应 tbl_BookCategory 表的 CRUD 操作
/// </summary>
public class BookCategoryDAL
{
    /// <summary>
    /// 查询全部图书类别
    /// </summary>
    /// <returns>类别列表</returns>
    public List<BookCategory> GetAllCategories()
    {
        const string sql = "SELECT categoryID, categoryName, borrowDays, finePerDay FROM tbl_BookCategory ORDER BY categoryID";
        List<BookCategory> list = new();
        using SqlConnection conn = new(DBConnection.GetConnectionString());
        using SqlCommand cmd = new(sql, conn);
        conn.Open();
        using SqlDataReader reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(MapReaderToCategory(reader));
        }
        return list;
    }

    /// <summary>
    /// 按类别名称关键字模糊查询
    /// </summary>
    /// <param name="keyword">查询关键字，为空则返回全部</param>
    /// <returns>类别列表</returns>
    public List<BookCategory> SearchCategories(string keyword)
    {
        if (string.IsNullOrWhiteSpace(keyword))
        {
            return GetAllCategories();
        }

        // 参数化查询：keyword 作为 SqlParameter 传入，SQL 引擎将其视为纯数据而非代码片段，从根本上杜绝 SQL 注入
        const string sql = "SELECT categoryID, categoryName, borrowDays, finePerDay FROM tbl_BookCategory WHERE categoryName LIKE @keyword ORDER BY categoryID";
        List<BookCategory> list = new();
        using SqlConnection conn = new(DBConnection.GetConnectionString());
        using SqlCommand cmd = new(sql, conn);
        // 模糊查询需要在参数值两侧拼接 % 通配符，而非在 SQL 中直接拼接，确保用户输入中的 % 和 _ 不会被当作通配符解释
        cmd.Parameters.Add(new SqlParameter("@keyword", SqlDbType.NVarChar, 20) { Value = $"%{keyword}%" });
        conn.Open();
        using SqlDataReader reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(MapReaderToCategory(reader));
        }
        return list;
    }

    /// <summary>
    /// 根据类别编号查询单个类别
    /// </summary>
    /// <param name="categoryID">类别编号</param>
    /// <returns>类别实体，未找到返回 null</returns>
    public BookCategory GetCategoryById(string categoryID)
    {
        const string sql = "SELECT categoryID, categoryName, borrowDays, finePerDay FROM tbl_BookCategory WHERE categoryID = @categoryID";
        using SqlConnection conn = new(DBConnection.GetConnectionString());
        using SqlCommand cmd = new(sql, conn);
        cmd.Parameters.Add(new SqlParameter("@categoryID", SqlDbType.NVarChar, 10) { Value = categoryID });
        conn.Open();
        using SqlDataReader reader = cmd.ExecuteReader();
        if (reader.Read())
        {
            return MapReaderToCategory(reader);
        }
        return null;
    }

    /// <summary>
    /// 新增图书类别
    /// </summary>
    /// <param name="category">类别实体</param>
    /// <returns>成功返回 true，编号已存在返回 false</returns>
    public bool InsertCategory(BookCategory category)
    {
        const string sql = "INSERT INTO tbl_BookCategory (categoryID, categoryName, borrowDays, finePerDay) VALUES (@categoryID, @categoryName, @borrowDays, @finePerDay)";
        using SqlConnection conn = new(DBConnection.GetConnectionString());
        using SqlCommand cmd = new(sql, conn);
        cmd.Parameters.Add(new SqlParameter("@categoryID", SqlDbType.NVarChar, 10) { Value = category.CategoryID });
        cmd.Parameters.Add(new SqlParameter("@categoryName", SqlDbType.NVarChar, 20) { Value = category.CategoryName });
        cmd.Parameters.Add(new SqlParameter("@borrowDays", SqlDbType.Int) { Value = category.BorrowDays });
        cmd.Parameters.Add(new SqlParameter("@finePerDay", SqlDbType.Decimal) { Value = category.FinePerDay, Precision = 10, Scale = 2 });
        conn.Open();
        try
        {
            return cmd.ExecuteNonQuery() > 0;
        }
        catch (SqlException ex) when (ex.Number == 2627) // 2627 = SQL Server 主键/唯一约束冲突错误码
        {
            // 主键冲突时返回 false 而非抛异常，让 BLL 层据此向用户提示"编号已存在"
            return false;
        }
    }

    /// <summary>
    /// 修改类别信息（编号不可修改）
    /// </summary>
    /// <param name="categoryID">类别编号</param>
    /// <param name="categoryName">新类别名称</param>
    /// <param name="borrowDays">新可借阅天数</param>
    /// <param name="finePerDay">新单日逾期罚款标准</param>
    /// <returns>成功返回 true</returns>
    public bool UpdateCategory(string categoryID, string categoryName, int borrowDays, decimal finePerDay)
    {
        const string sql = "UPDATE tbl_BookCategory SET categoryName = @categoryName, borrowDays = @borrowDays, finePerDay = @finePerDay WHERE categoryID = @categoryID";
        using SqlConnection conn = new(DBConnection.GetConnectionString());
        using SqlCommand cmd = new(sql, conn);
        cmd.Parameters.Add(new SqlParameter("@categoryName", SqlDbType.NVarChar, 20) { Value = categoryName });
        cmd.Parameters.Add(new SqlParameter("@borrowDays", SqlDbType.Int) { Value = borrowDays });
        cmd.Parameters.Add(new SqlParameter("@finePerDay", SqlDbType.Decimal) { Value = finePerDay, Precision = 10, Scale = 2 });
        cmd.Parameters.Add(new SqlParameter("@categoryID", SqlDbType.NVarChar, 10) { Value = categoryID });
        conn.Open();
        return cmd.ExecuteNonQuery() > 0;
    }

    /// <summary>
    /// 删除图书类别
    /// </summary>
    /// <param name="categoryID">类别编号</param>
    /// <returns>成功返回 true</returns>
    public bool DeleteCategory(string categoryID)
    {
        const string sql = "DELETE FROM tbl_BookCategory WHERE categoryID = @categoryID";
        using SqlConnection conn = new(DBConnection.GetConnectionString());
        using SqlCommand cmd = new(sql, conn);
        cmd.Parameters.Add(new SqlParameter("@categoryID", SqlDbType.NVarChar, 10) { Value = categoryID });
        conn.Open();
        return cmd.ExecuteNonQuery() > 0;
    }

    /// <summary>
    /// 统计指定类别下的图书数量
    /// </summary>
    /// <param name="categoryID">类别编号</param>
    /// <returns>图书数量</returns>
    public int CountBooksByCategory(string categoryID)
    {
        const string sql = "SELECT COUNT(1) FROM tbl_Book WHERE categoryID = @categoryID";
        using SqlConnection conn = new(DBConnection.GetConnectionString());
        using SqlCommand cmd = new(sql, conn);
        cmd.Parameters.Add(new SqlParameter("@categoryID", SqlDbType.NVarChar, 10) { Value = categoryID });
        conn.Open();
        return (int)cmd.ExecuteScalar();
    }

    /// <summary>
    /// 将 SqlDataReader 映射为 BookCategory 实体
    /// </summary>
    private static BookCategory MapReaderToCategory(SqlDataReader reader)
    {
        return new BookCategory
        {
            CategoryID = reader["categoryID"].ToString(),
            CategoryName = reader["categoryName"].ToString(),
            BorrowDays = Convert.ToInt32(reader["borrowDays"]),
            FinePerDay = Convert.ToDecimal(reader["finePerDay"])
        };
    }
}
