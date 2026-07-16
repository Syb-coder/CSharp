using System.Data;
using CampusShop.Models;
using Microsoft.Data.SqlClient;

namespace CampusShop.DAL;

/// <summary>
/// 供货商数据访问类，对应 tbl_Supplier 表的 CRUD 操作
/// </summary>
public class SupplierDAL
{
    /// <summary>
    /// 查询全部供货商
    /// </summary>
    /// <returns>供货商列表</returns>
    public List<Supplier> GetAllSuppliers()
    {
        const string sql = @"SELECT supplierID, supplierName, contactPerson, phone, address,
                             legalPerson, registerDate FROM tbl_Supplier ORDER BY supplierID";
        List<Supplier> list = new();
        using SqlConnection conn = new(DBConnection.GetConnectionString());
        using SqlCommand cmd = new(sql, conn);
        conn.Open();
        using SqlDataReader reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(MapReaderToSupplier(reader));
        }
        return list;
    }

    /// <summary>
    /// 按编号或名称检索供货商
    /// </summary>
    /// <param name="idKeyword">编号关键字</param>
    /// <param name="nameKeyword">名称关键字</param>
    /// <returns>供货商列表</returns>
    public List<Supplier> SearchSuppliers(string idKeyword, string nameKeyword)
    {
        // 动态拼接 WHERE 条件：仅当关键字非空时添加对应过滤条件
        List<string> conditions = new();
        if (!string.IsNullOrEmpty(idKeyword))
        {
            conditions.Add("supplierID LIKE '%' + @idKeyword + '%'");
        }
        if (!string.IsNullOrEmpty(nameKeyword))
        {
            conditions.Add("supplierName LIKE '%' + @nameKeyword + '%'");
        }

        string whereClause = conditions.Count > 0 ? "WHERE " + string.Join(" AND ", conditions) : "";
        string sql = $@"SELECT supplierID, supplierName, contactPerson, phone, address,
                             legalPerson, registerDate FROM tbl_Supplier {whereClause} ORDER BY supplierID";

        List<Supplier> list = new();
        using SqlConnection conn = new(DBConnection.GetConnectionString());
        using SqlCommand cmd = new(sql, conn);
        if (!string.IsNullOrEmpty(idKeyword))
        {
            cmd.Parameters.Add(new SqlParameter("@idKeyword", SqlDbType.NVarChar, 20) { Value = idKeyword });
        }
        if (!string.IsNullOrEmpty(nameKeyword))
        {
            cmd.Parameters.Add(new SqlParameter("@nameKeyword", SqlDbType.NVarChar, 50) { Value = nameKeyword });
        }
        conn.Open();
        using SqlDataReader reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(MapReaderToSupplier(reader));
        }
        return list;
    }

    /// <summary>
    /// 根据供货商编号查询
    /// </summary>
    /// <param name="supplierID">供货商编号</param>
    /// <returns>供货商实体，未找到返回 null</returns>
    public Supplier GetSupplierByID(string supplierID)
    {
        const string sql = @"SELECT supplierID, supplierName, contactPerson, phone, address,
                             legalPerson, registerDate FROM tbl_Supplier WHERE supplierID = @supplierID";
        using SqlConnection conn = new(DBConnection.GetConnectionString());
        using SqlCommand cmd = new(sql, conn);
        cmd.Parameters.Add(new SqlParameter("@supplierID", SqlDbType.NVarChar, 20) { Value = supplierID });
        conn.Open();
        using SqlDataReader reader = cmd.ExecuteReader();
        if (reader.Read())
        {
            return MapReaderToSupplier(reader);
        }
        return null;
    }

    /// <summary>
    /// 新增供货商
    /// </summary>
    /// <param name="supplier">供货商实体</param>
    /// <returns>成功返回 true，编号已存在返回 false</returns>
    public bool InsertSupplier(Supplier supplier)
    {
        const string sql = @"INSERT INTO tbl_Supplier
            (supplierID, supplierName, contactPerson, phone, address, legalPerson, registerDate)
            VALUES (@supplierID, @supplierName, @contactPerson, @phone, @address, @legalPerson, @registerDate)";
        using SqlConnection conn = new(DBConnection.GetConnectionString());
        using SqlCommand cmd = new(sql, conn);
        cmd.Parameters.Add(new SqlParameter("@supplierID", SqlDbType.NVarChar, 20) { Value = supplier.SupplierID });
        cmd.Parameters.Add(new SqlParameter("@supplierName", SqlDbType.NVarChar, 50) { Value = supplier.SupplierName });
        cmd.Parameters.Add(new SqlParameter("@contactPerson", SqlDbType.NVarChar, 20) { Value = (object)supplier.ContactPerson ?? DBNull.Value });
        cmd.Parameters.Add(new SqlParameter("@phone", SqlDbType.NVarChar, 15) { Value = (object)supplier.Phone ?? DBNull.Value });
        cmd.Parameters.Add(new SqlParameter("@address", SqlDbType.NVarChar, 100) { Value = (object)supplier.Address ?? DBNull.Value });
        cmd.Parameters.Add(new SqlParameter("@legalPerson", SqlDbType.NVarChar, 20) { Value = (object)supplier.LegalPerson ?? DBNull.Value });
        cmd.Parameters.Add(new SqlParameter("@registerDate", SqlDbType.Date) { Value = (object)supplier.RegisterDate ?? DBNull.Value });
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
    /// 修改供货商信息（编号不可改）
    /// </summary>
    /// <param name="supplier">供货商实体</param>
    /// <returns>成功返回 true</returns>
    public bool UpdateSupplier(Supplier supplier)
    {
        const string sql = @"UPDATE tbl_Supplier SET
            supplierName = @supplierName, contactPerson = @contactPerson, phone = @phone,
            address = @address, legalPerson = @legalPerson, registerDate = @registerDate
            WHERE supplierID = @supplierID";
        using SqlConnection conn = new(DBConnection.GetConnectionString());
        using SqlCommand cmd = new(sql, conn);
        cmd.Parameters.Add(new SqlParameter("@supplierName", SqlDbType.NVarChar, 50) { Value = supplier.SupplierName });
        cmd.Parameters.Add(new SqlParameter("@contactPerson", SqlDbType.NVarChar, 20) { Value = (object)supplier.ContactPerson ?? DBNull.Value });
        cmd.Parameters.Add(new SqlParameter("@phone", SqlDbType.NVarChar, 15) { Value = (object)supplier.Phone ?? DBNull.Value });
        cmd.Parameters.Add(new SqlParameter("@address", SqlDbType.NVarChar, 100) { Value = (object)supplier.Address ?? DBNull.Value });
        cmd.Parameters.Add(new SqlParameter("@legalPerson", SqlDbType.NVarChar, 20) { Value = (object)supplier.LegalPerson ?? DBNull.Value });
        cmd.Parameters.Add(new SqlParameter("@registerDate", SqlDbType.Date) { Value = (object)supplier.RegisterDate ?? DBNull.Value });
        cmd.Parameters.Add(new SqlParameter("@supplierID", SqlDbType.NVarChar, 20) { Value = supplier.SupplierID });
        conn.Open();
        return cmd.ExecuteNonQuery() > 0;
    }

    /// <summary>
    /// 删除供货商
    /// </summary>
    /// <param name="supplierID">供货商编号</param>
    /// <returns>成功返回 true</returns>
    public bool DeleteSupplier(string supplierID)
    {
        const string sql = "DELETE FROM tbl_Supplier WHERE supplierID = @supplierID";
        using SqlConnection conn = new(DBConnection.GetConnectionString());
        using SqlCommand cmd = new(sql, conn);
        cmd.Parameters.Add(new SqlParameter("@supplierID", SqlDbType.NVarChar, 20) { Value = supplierID });
        conn.Open();
        return cmd.ExecuteNonQuery() > 0;
    }

    /// <summary>
    /// 检查指定供货商下是否有关联商品
    /// </summary>
    /// <param name="supplierID">供货商编号</param>
    /// <returns>存在关联商品返回 true</returns>
    public bool HasProducts(string supplierID)
    {
        const string sql = "SELECT COUNT(1) FROM tbl_Product WHERE supplierID = @supplierID";
        using SqlConnection conn = new(DBConnection.GetConnectionString());
        using SqlCommand cmd = new(sql, conn);
        cmd.Parameters.Add(new SqlParameter("@supplierID", SqlDbType.NVarChar, 20) { Value = supplierID });
        conn.Open();
        return (int)cmd.ExecuteScalar() > 0;
    }

    /// <summary>
    /// 将 SqlDataReader 映射为 Supplier 实体
    /// </summary>
    private static Supplier MapReaderToSupplier(SqlDataReader reader)
    {
        return new Supplier
        {
            SupplierID = reader["supplierID"].ToString(),
            SupplierName = reader["supplierName"].ToString(),
            ContactPerson = reader["contactPerson"] == DBNull.Value ? null : reader["contactPerson"].ToString(),
            Phone = reader["phone"] == DBNull.Value ? null : reader["phone"].ToString(),
            Address = reader["address"] == DBNull.Value ? null : reader["address"].ToString(),
            LegalPerson = HasColumn(reader, "legalPerson") && reader["legalPerson"] != DBNull.Value
                ? reader["legalPerson"].ToString() : null,
            RegisterDate = HasColumn(reader, "registerDate") && reader["registerDate"] != DBNull.Value
                ? Convert.ToDateTime(reader["registerDate"]) : null
        };
    }

    /// <summary>
    /// 判断 SqlDataReader 当前结果集中是否存在指定列
    /// </summary>
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
