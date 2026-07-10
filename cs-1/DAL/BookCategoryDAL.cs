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
        const string sql = "SELECT categoryID, categoryName FROM tbl_BookCategory ORDER BY categoryID";
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

        const string sql = "SELECT categoryID, categoryName FROM tbl_BookCategory WHERE categoryName LIKE @keyword ORDER BY categoryID";
        List<BookCategory> list = new();
        using SqlConnection conn = new(DBConnection.GetConnectionString());
        using SqlCommand cmd = new(sql, conn);
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
        const string sql = "SELECT categoryID, categoryName FROM tbl_BookCategory WHERE categoryID = @categoryID";
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
        const string sql = "INSERT INTO tbl_BookCategory (categoryID, categoryName) VALUES (@categoryID, @categoryName)";
        using SqlConnection conn = new(DBConnection.GetConnectionString());
        using SqlCommand cmd = new(sql, conn);
        cmd.Parameters.Add(new SqlParameter("@categoryID", SqlDbType.NVarChar, 10) { Value = category.CategoryID });
        cmd.Parameters.Add(new SqlParameter("@categoryName", SqlDbType.NVarChar, 20) { Value = category.CategoryName });
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
    /// 修改类别名称（编号不可修改）
    /// </summary>
    /// <param name="categoryID">类别编号</param>
    /// <param name="categoryName">新类别名称</param>
    /// <returns>成功返回 true</returns>
    public bool UpdateCategory(string categoryID, string categoryName)
    {
        const string sql = "UPDATE tbl_BookCategory SET categoryName = @categoryName WHERE categoryID = @categoryID";
        using SqlConnection conn = new(DBConnection.GetConnectionString());
        using SqlCommand cmd = new(sql, conn);
        cmd.Parameters.Add(new SqlParameter("@categoryName", SqlDbType.NVarChar, 20) { Value = categoryName });
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
            CategoryName = reader["categoryName"].ToString()
        };
    }
}
