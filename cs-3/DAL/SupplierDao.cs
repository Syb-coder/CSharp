using System.Data;
using CampusMart.Models;
using Microsoft.Data.SqlClient;

namespace CampusMart.DAL;

/// <summary>
/// 供货商数据访问类，对应 tbl_Supplier 表的 CRUD 操作
/// </summary>
/// <remarks>
/// 与 cs-2 的 SupplierDAL 差异化命名（SupplierDao）。
/// </remarks>
public class SupplierDao : BaseDao
{
    /// <summary>
    /// 查询全部供货商
    /// </summary>
    /// <returns>供货商列表</returns>
    public List<SupplierInfo> GetAllSuppliers()
    {
        const string sql = "SELECT supplierID, supplierName, legalPerson, registerDate, contactPerson, phone, address FROM tbl_Supplier ORDER BY supplierID";
        DataTable table = ExecuteDataTable(sql);
        List<SupplierInfo> list = new(table.Rows.Count);
        foreach (DataRow row in table.Rows)
        {
            list.Add(MapRowToSupplier(row));
        }
        return list;
    }

    /// <summary>
    /// 按供货商名称或法人代表关键字检索
    /// </summary>
    /// <param name="keyword">名称或法人关键字</param>
    /// <returns>匹配的供货商列表</returns>
    public List<SupplierInfo> SearchByNameOrLegalPerson(string keyword)
    {
        const string sql = @"SELECT supplierID, supplierName, legalPerson, registerDate, contactPerson, phone, address
                             FROM tbl_Supplier
                             WHERE supplierName LIKE @keyword OR legalPerson LIKE @keyword2
                             ORDER BY supplierID";
        SqlParameter[] ps =
        {
            new("@keyword", SqlDbType.NVarChar, 50) { Value = $"%{keyword}%" },
            new("@keyword2", SqlDbType.NVarChar, 20) { Value = $"%{keyword}%" }
        };
        DataTable table = ExecuteDataTable(sql, ps);
        List<SupplierInfo> list = new(table.Rows.Count);
        foreach (DataRow row in table.Rows)
        {
            list.Add(MapRowToSupplier(row));
        }
        return list;
    }

    /// <summary>
    /// 新增供货商
    /// </summary>
    /// <param name="supplier">供货商实体</param>
    /// <returns>成功返回 true</returns>
    public bool InsertSupplier(SupplierInfo supplier)
    {
        const string sql = @"INSERT INTO tbl_Supplier (supplierName, legalPerson, registerDate, contactPerson, phone, address)
                             VALUES (@supplierName, @legalPerson, @registerDate, @contactPerson, @phone, @address)";
        SqlParameter[] ps =
        {
            new("@supplierName", SqlDbType.NVarChar, 50) { Value = supplier.SupplierName },
            new("@legalPerson", SqlDbType.NVarChar, 20) { Value = (object)supplier.LegalPerson ?? DBNull.Value },
            new("@registerDate", SqlDbType.Date) { Value = (object)supplier.RegisterDate ?? DBNull.Value },
            new("@contactPerson", SqlDbType.NVarChar, 20) { Value = (object)supplier.ContactPerson ?? DBNull.Value },
            new("@phone", SqlDbType.NVarChar, 15) { Value = (object)supplier.Phone ?? DBNull.Value },
            new("@address", SqlDbType.NVarChar, 100) { Value = (object)supplier.Address ?? DBNull.Value }
        };
        return ExecuteNonQuery(sql, ps) > 0;
    }

    /// <summary>
    /// 修改供货商信息
    /// </summary>
    /// <param name="supplier">供货商实体</param>
    /// <returns>成功返回 true</returns>
    public bool UpdateSupplier(SupplierInfo supplier)
    {
        const string sql = @"UPDATE tbl_Supplier
                             SET supplierName = @supplierName, legalPerson = @legalPerson, registerDate = @registerDate,
                                 contactPerson = @contactPerson, phone = @phone, address = @address
                             WHERE supplierID = @supplierID";
        SqlParameter[] ps =
        {
            new("@supplierName", SqlDbType.NVarChar, 50) { Value = supplier.SupplierName },
            new("@legalPerson", SqlDbType.NVarChar, 20) { Value = (object)supplier.LegalPerson ?? DBNull.Value },
            new("@registerDate", SqlDbType.Date) { Value = (object)supplier.RegisterDate ?? DBNull.Value },
            new("@contactPerson", SqlDbType.NVarChar, 20) { Value = (object)supplier.ContactPerson ?? DBNull.Value },
            new("@phone", SqlDbType.NVarChar, 15) { Value = (object)supplier.Phone ?? DBNull.Value },
            new("@address", SqlDbType.NVarChar, 100) { Value = (object)supplier.Address ?? DBNull.Value },
            new("@supplierID", SqlDbType.Int) { Value = supplier.SupplierID }
        };
        return ExecuteNonQuery(sql, ps) > 0;
    }

    /// <summary>
    /// 检查指定供货商下是否存在关联商品
    /// </summary>
    /// <param name="supplierID">供货商编号</param>
    /// <returns>存在关联商品返回 true</returns>
    public bool HasRelatedProducts(int supplierID)
    {
        const string sql = "SELECT COUNT(1) FROM tbl_Product WHERE supplierID = @supplierID";
        SqlParameter p = new("@supplierID", SqlDbType.Int) { Value = supplierID };
        object result = ExecuteScalar(sql, p);
        return Convert.ToInt32(result) > 0;
    }

    /// <summary>
    /// 删除供货商
    /// </summary>
    /// <param name="supplierID">供货商编号</param>
    /// <returns>成功返回 true</returns>
    public bool DeleteSupplier(int supplierID)
    {
        const string sql = "DELETE FROM tbl_Supplier WHERE supplierID = @supplierID";
        SqlParameter p = new("@supplierID", SqlDbType.Int) { Value = supplierID };
        return ExecuteNonQuery(sql, p) > 0;
    }

    /// <summary>
    /// 将 DataRow 映射为 SupplierInfo 实体
    /// </summary>
    private static SupplierInfo MapRowToSupplier(DataRow row)
    {
        return new SupplierInfo
        {
            SupplierID = Convert.ToInt32(row["supplierID"]),
            SupplierName = row["supplierName"].ToString(),
            LegalPerson = row["legalPerson"] == DBNull.Value ? null : row["legalPerson"].ToString(),
            RegisterDate = row["registerDate"] == DBNull.Value ? null : Convert.ToDateTime(row["registerDate"]),
            ContactPerson = row["contactPerson"] == DBNull.Value ? null : row["contactPerson"].ToString(),
            Phone = row["phone"] == DBNull.Value ? null : row["phone"].ToString(),
            Address = row["address"] == DBNull.Value ? null : row["address"].ToString()
        };
    }
}
