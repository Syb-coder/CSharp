using System.Data;
using CampusShop.Models;
using Microsoft.Data.SqlClient;

namespace CampusShop.DAL;

/// <summary>
/// 商品类别数据访问类，对应 tbl_Category 表的 CRUD 操作
/// </summary>
public class CategoryDAL
{
    /// <summary>
    /// 查询全部商品类别（含父类别名称）
    /// </summary>
    /// <returns>类别列表</returns>
    public List<Category> GetAllCategories()
    {
        // LEFT JOIN 自连接：根类别无父类别，LEFT JOIN 保证根类别也能被查询出来
        const string sql = @"
            SELECT c.categoryID, c.categoryName, c.parentCategoryID,
                   c.categoryDesc, c.addTime,
                   p.categoryName AS parentCategoryName
            FROM tbl_Category c
            LEFT JOIN tbl_Category p ON c.parentCategoryID = p.categoryID
            ORDER BY c.categoryID";
        List<Category> list = new();
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
    /// 按类别名称关键字模糊检索
    /// </summary>
    /// <param name="keyword">名称关键字</param>
    /// <returns>类别列表</returns>
    public List<Category> SearchByName(string keyword)
    {
        const string sql = @"
            SELECT c.categoryID, c.categoryName, c.parentCategoryID,
                   c.categoryDesc, c.addTime,
                   p.categoryName AS parentCategoryName
            FROM tbl_Category c
            LEFT JOIN tbl_Category p ON c.parentCategoryID = p.categoryID
            WHERE c.categoryName LIKE '%' + @keyword + '%'
            ORDER BY c.categoryID";
        List<Category> list = new();
        using SqlConnection conn = new(DBConnection.GetConnectionString());
        using SqlCommand cmd = new(sql, conn);
        cmd.Parameters.Add(new SqlParameter("@keyword", SqlDbType.NVarChar, 20) { Value = keyword });
        conn.Open();
        using SqlDataReader reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(MapReaderToCategory(reader));
        }
        return list;
    }

    /// <summary>
    /// 根据类别编号查询
    /// </summary>
    /// <param name="categoryID">类别编号</param>
    /// <returns>类别实体，未找到返回 null</returns>
    public Category GetCategoryByID(string categoryID)
    {
        const string sql = "SELECT categoryID, categoryName, parentCategoryID, categoryDesc, addTime FROM tbl_Category WHERE categoryID = @categoryID";
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
    /// 新增类别
    /// </summary>
    /// <param name="category">类别实体</param>
    /// <returns>成功返回 true，编号已存在返回 false</returns>
    public bool InsertCategory(Category category)
    {
        const string sql = @"INSERT INTO tbl_Category
            (categoryID, categoryName, parentCategoryID, categoryDesc, addTime)
            VALUES (@categoryID, @categoryName, @parentCategoryID, @categoryDesc, @addTime)";
        using SqlConnection conn = new(DBConnection.GetConnectionString());
        using SqlCommand cmd = new(sql, conn);
        cmd.Parameters.Add(new SqlParameter("@categoryID", SqlDbType.NVarChar, 10) { Value = category.CategoryID });
        cmd.Parameters.Add(new SqlParameter("@categoryName", SqlDbType.NVarChar, 20) { Value = category.CategoryName });
        // parentCategoryID 为空时传 DBNull，对应数据库 NULL
        cmd.Parameters.Add(new SqlParameter("@parentCategoryID", SqlDbType.NVarChar, 10) { Value = (object)category.ParentCategoryID ?? DBNull.Value });
        cmd.Parameters.Add(new SqlParameter("@categoryDesc", SqlDbType.NVarChar, 100) { Value = (object)category.CategoryDesc ?? DBNull.Value });
        // addTime 为空时由数据库 DEFAULT GETDATE() 自动填充
        cmd.Parameters.Add(new SqlParameter("@addTime", SqlDbType.DateTime) { Value = (object)category.AddTime ?? DateTime.Now });
        conn.Open();
        try
        {
            return cmd.ExecuteNonQuery() > 0;
        }
        catch (SqlException ex) when (ex.Number == 2627) // 主键冲突
        {
            return false;
        }
    }

    /// <summary>
    /// 修改类别名称、父类别和描述（编号不可改）
    /// </summary>
    /// <param name="category">类别实体</param>
    /// <returns>成功返回 true</returns>
    public bool UpdateCategory(Category category)
    {
        const string sql = @"UPDATE tbl_Category SET
            categoryName = @categoryName,
            parentCategoryID = @parentCategoryID,
            categoryDesc = @categoryDesc
            WHERE categoryID = @categoryID";
        using SqlConnection conn = new(DBConnection.GetConnectionString());
        using SqlCommand cmd = new(sql, conn);
        cmd.Parameters.Add(new SqlParameter("@categoryName", SqlDbType.NVarChar, 20) { Value = category.CategoryName });
        cmd.Parameters.Add(new SqlParameter("@parentCategoryID", SqlDbType.NVarChar, 10) { Value = (object)category.ParentCategoryID ?? DBNull.Value });
        cmd.Parameters.Add(new SqlParameter("@categoryDesc", SqlDbType.NVarChar, 100) { Value = (object)category.CategoryDesc ?? DBNull.Value });
        cmd.Parameters.Add(new SqlParameter("@categoryID", SqlDbType.NVarChar, 10) { Value = category.CategoryID });
        conn.Open();
        return cmd.ExecuteNonQuery() > 0;
    }

    /// <summary>
    /// 删除类别
    /// </summary>
    /// <param name="categoryID">类别编号</param>
    /// <returns>成功返回 true</returns>
    public bool DeleteCategory(string categoryID)
    {
        const string sql = "DELETE FROM tbl_Category WHERE categoryID = @categoryID";
        using SqlConnection conn = new(DBConnection.GetConnectionString());
        using SqlCommand cmd = new(sql, conn);
        cmd.Parameters.Add(new SqlParameter("@categoryID", SqlDbType.NVarChar, 10) { Value = categoryID });
        conn.Open();
        return cmd.ExecuteNonQuery() > 0;
    }

    /// <summary>
    /// 检查指定类别下是否有关联商品
    /// </summary>
    /// <param name="categoryID">类别编号</param>
    /// <returns>存在关联商品返回 true</returns>
    public bool HasProducts(string categoryID)
    {
        const string sql = "SELECT COUNT(1) FROM tbl_Product WHERE categoryID = @categoryID";
        using SqlConnection conn = new(DBConnection.GetConnectionString());
        using SqlCommand cmd = new(sql, conn);
        cmd.Parameters.Add(new SqlParameter("@categoryID", SqlDbType.NVarChar, 10) { Value = categoryID });
        conn.Open();
        return (int)cmd.ExecuteScalar() > 0;
    }

    /// <summary>
    /// 将 SqlDataReader 映射为 Category 实体
    /// </summary>
    private static Category MapReaderToCategory(SqlDataReader reader)
    {
        return new Category
        {
            CategoryID = reader["categoryID"].ToString(),
            CategoryName = reader["categoryName"].ToString(),
            ParentCategoryID = reader["parentCategoryID"] == DBNull.Value ? null : reader["parentCategoryID"].ToString(),
            CategoryDesc = HasColumn(reader, "categoryDesc") && reader["categoryDesc"] != DBNull.Value
                ? reader["categoryDesc"].ToString() : null,
            AddTime = HasColumn(reader, "addTime") && reader["addTime"] != DBNull.Value
                ? Convert.ToDateTime(reader["addTime"]) : null,
            // parentCategoryName 来自 LEFT JOIN，根类别时为 DBNull
            ParentCategoryName = HasColumn(reader, "parentCategoryName") && reader["parentCategoryName"] != DBNull.Value
                ? reader["parentCategoryName"].ToString() : null
        };
    }

    /// <summary>
    /// 判断 SqlDataReader 当前结果集中是否存在指定列
    /// </summary>
    /// <param name="reader">数据读取器</param>
    /// <param name="columnName">列名</param>
    /// <returns>存在返回 true</returns>
    private static bool HasColumn(SqlDataReader reader, string columnName)
    {
        for (int i = 0; i < reader.FieldCount; i++)
        {
            if (reader.GetName(i) == columnName)
            {
                return true;
            }
        }
        return false;
    }
}
