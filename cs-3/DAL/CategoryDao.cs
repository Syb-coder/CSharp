using System.Data;
using CampusMart.Models;
using Microsoft.Data.SqlClient;

namespace CampusMart.DAL;

/// <summary>
/// 商品类别数据访问类，对应 tbl_Category 表的 CRUD 操作（扁平结构）
/// </summary>
/// <remarks>
/// 与 cs-2 的 CategoryDAL 差异化命名（CategoryDao）。本系统类别为扁平结构，无父子层级查询。
/// </remarks>
public class CategoryDao : BaseDao
{
    /// <summary>
    /// 查询全部商品类别
    /// </summary>
    /// <returns>类别列表</returns>
    public List<CategoryInfo> GetAllCategories()
    {
        const string sql = "SELECT categoryID, categoryName, categoryDesc, addTime FROM tbl_Category ORDER BY categoryID";
        DataTable table = ExecuteDataTable(sql);
        List<CategoryInfo> list = new(table.Rows.Count);
        foreach (DataRow row in table.Rows)
        {
            list.Add(MapRowToCategory(row));
        }
        return list;
    }

    /// <summary>
    /// 按类别名称关键字模糊检索
    /// </summary>
    /// <param name="keyword">类别名称关键字</param>
    /// <returns>匹配的类别列表</returns>
    public List<CategoryInfo> SearchByName(string keyword)
    {
        const string sql = "SELECT categoryID, categoryName, categoryDesc, addTime FROM tbl_Category WHERE categoryName LIKE @keyword ORDER BY categoryID";
        SqlParameter p = new("@keyword", SqlDbType.NVarChar, 20) { Value = $"%{keyword}%" };
        DataTable table = ExecuteDataTable(sql, p);
        List<CategoryInfo> list = new(table.Rows.Count);
        foreach (DataRow row in table.Rows)
        {
            list.Add(MapRowToCategory(row));
        }
        return list;
    }

    /// <summary>
    /// 按类别编号查询
    /// </summary>
    /// <param name="categoryID">类别编号</param>
    /// <returns>类别实体，未找到返回 null</returns>
    public CategoryInfo GetCategoryByID(int categoryID)
    {
        const string sql = "SELECT categoryID, categoryName, categoryDesc, addTime FROM tbl_Category WHERE categoryID = @categoryID";
        SqlParameter p = new("@categoryID", SqlDbType.Int) { Value = categoryID };
        DataTable table = ExecuteDataTable(sql, p);
        if (table.Rows.Count == 0) return null;
        return MapRowToCategory(table.Rows[0]);
    }

    /// <summary>
    /// 新增类别（addTime 由数据库默认值 GETDATE() 自动填充）
    /// </summary>
    /// <param name="category">类别实体</param>
    /// <returns>成功返回 true</returns>
    public bool InsertCategory(CategoryInfo category)
    {
        const string sql = "INSERT INTO tbl_Category (categoryName, categoryDesc) VALUES (@categoryName, @categoryDesc)";
        SqlParameter[] ps =
        {
            new("@categoryName", SqlDbType.NVarChar, 20) { Value = category.CategoryName },
            new("@categoryDesc", SqlDbType.NVarChar, 100) { Value = (object)category.CategoryDesc ?? DBNull.Value }
        };
        return ExecuteNonQuery(sql, ps) > 0;
    }

    /// <summary>
    /// 修改类别信息（类别编号为主键，不允许修改）
    /// </summary>
    /// <param name="category">类别实体</param>
    /// <returns>成功返回 true</returns>
    public bool UpdateCategory(CategoryInfo category)
    {
        const string sql = "UPDATE tbl_Category SET categoryName = @categoryName, categoryDesc = @categoryDesc WHERE categoryID = @categoryID";
        SqlParameter[] ps =
        {
            new("@categoryName", SqlDbType.NVarChar, 20) { Value = category.CategoryName },
            new("@categoryDesc", SqlDbType.NVarChar, 100) { Value = (object)category.CategoryDesc ?? DBNull.Value },
            new("@categoryID", SqlDbType.Int) { Value = category.CategoryID }
        };
        return ExecuteNonQuery(sql, ps) > 0;
    }

    /// <summary>
    /// 检查指定类别下是否存在关联商品
    /// </summary>
    /// <param name="categoryID">类别编号</param>
    /// <returns>存在关联商品返回 true</returns>
    public bool HasRelatedProducts(int categoryID)
    {
        const string sql = "SELECT COUNT(1) FROM tbl_Product WHERE categoryID = @categoryID";
        SqlParameter p = new("@categoryID", SqlDbType.Int) { Value = categoryID };
        object result = ExecuteScalar(sql, p);
        return Convert.ToInt32(result) > 0;
    }

    /// <summary>
    /// 删除类别
    /// </summary>
    /// <param name="categoryID">类别编号</param>
    /// <returns>成功返回 true</returns>
    public bool DeleteCategory(int categoryID)
    {
        const string sql = "DELETE FROM tbl_Category WHERE categoryID = @categoryID";
        SqlParameter p = new("@categoryID", SqlDbType.Int) { Value = categoryID };
        return ExecuteNonQuery(sql, p) > 0;
    }

    /// <summary>
    /// 将 DataRow 映射为 CategoryInfo 实体
    /// </summary>
    private static CategoryInfo MapRowToCategory(DataRow row)
    {
        return new CategoryInfo
        {
            CategoryID = Convert.ToInt32(row["categoryID"]),
            CategoryName = row["categoryName"].ToString(),
            CategoryDesc = row["categoryDesc"] == DBNull.Value ? null : row["categoryDesc"].ToString(),
            AddTime = Convert.ToDateTime(row["addTime"])
        };
    }
}
