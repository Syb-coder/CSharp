using Microsoft.Data.SqlClient;
using HotelSys.Models;

namespace HotelSys.DAL;

/// <summary>
/// 客户信息数据访问类，继承泛型基类 BaseRepository&lt;CustomerInfo&gt;
/// </summary>
public class CustomerDao : BaseRepository<CustomerInfo>
{
    /// <summary>查询全部客户</summary>
    public override List<CustomerInfo> FindAll()
    {
        const string sql = "SELECT customerID, customerName, gender, idType, idNumber, phone, address, createTime FROM T_Customer ORDER BY customerID DESC";
        List<CustomerInfo> list = new();
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

    /// <summary>按客户编号查询单条记录</summary>
    public override CustomerInfo FindById(string id)
    {
        const string sql = "SELECT customerID, customerName, gender, idType, idNumber, phone, address, createTime FROM T_Customer WHERE customerID = @customerID";
        using SqlConnection conn = new(DBConnection.GetConnectionString());
        conn.Open();
        using SqlCommand cmd = new(sql, conn);
        cmd.Parameters.AddWithValue("@customerID", int.Parse(id));
        using SqlDataReader reader = cmd.ExecuteReader();
        return reader.Read() ? MapReader(reader) : null;
    }

    /// <summary>按客户编号查询（int 版本）</summary>
    public CustomerInfo GetById(int customerID)
    {
        const string sql = "SELECT customerID, customerName, gender, idType, idNumber, phone, address, createTime FROM T_Customer WHERE customerID = @customerID";
        using SqlConnection conn = new(DBConnection.GetConnectionString());
        conn.Open();
        using SqlCommand cmd = new(sql, conn);
        cmd.Parameters.AddWithValue("@customerID", customerID);
        using SqlDataReader reader = cmd.ExecuteReader();
        return reader.Read() ? MapReader(reader) : null;
    }

    /// <summary>新增客户，返回新生成的客户编号</summary>
    public override int Insert(CustomerInfo entity)
    {
        const string sql = @"INSERT INTO T_Customer (customerName, gender, idType, idNumber, phone, address)
                             OUTPUT INSERTED.customerID
                             VALUES (@customerName, @gender, @idType, @idNumber, @phone, @address)";
        using SqlConnection conn = new(DBConnection.GetConnectionString());
        conn.Open();
        using SqlCommand cmd = new(sql, conn);
        cmd.Parameters.AddWithValue("@customerName", entity.CustomerName);
        cmd.Parameters.AddWithValue("@gender", (object)entity.Gender ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@idType", entity.IdType);
        cmd.Parameters.AddWithValue("@idNumber", entity.IdNumber);
        cmd.Parameters.AddWithValue("@phone", (object)entity.Phone ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@address", (object)entity.Address ?? DBNull.Value);
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    /// <summary>修改客户信息</summary>
    public override int Update(CustomerInfo entity)
    {
        const string sql = @"UPDATE T_Customer SET customerName = @customerName, gender = @gender, idType = @idType,
                             idNumber = @idNumber, phone = @phone, address = @address WHERE customerID = @customerID";
        using SqlConnection conn = new(DBConnection.GetConnectionString());
        conn.Open();
        using SqlCommand cmd = new(sql, conn);
        cmd.Parameters.AddWithValue("@customerName", entity.CustomerName);
        cmd.Parameters.AddWithValue("@gender", (object)entity.Gender ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@idType", entity.IdType);
        cmd.Parameters.AddWithValue("@idNumber", entity.IdNumber);
        cmd.Parameters.AddWithValue("@phone", (object)entity.Phone ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@address", (object)entity.Address ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@customerID", entity.CustomerID);
        return cmd.ExecuteNonQuery();
    }

    /// <summary>按客户编号删除</summary>
    public override int Delete(string id)
    {
        const string sql = "DELETE FROM T_Customer WHERE customerID = @customerID";
        using SqlConnection conn = new(DBConnection.GetConnectionString());
        conn.Open();
        using SqlCommand cmd = new(sql, conn);
        cmd.Parameters.AddWithValue("@customerID", int.Parse(id));
        return cmd.ExecuteNonQuery();
    }

    // ===== 以下为 Customer 特有查询 =====

    /// <summary>多条件模糊查询（姓名/证件号/手机号，PRD F-07）</summary>
    public List<CustomerInfo> Search(string name, string idNumber, string phone)
    {
        const string sql = @"SELECT customerID, customerName, gender, idType, idNumber, phone, address, createTime
                             FROM T_Customer
                             WHERE (@name = '' OR customerName LIKE '%' + @name + '%')
                               AND (@idNumber = '' OR idNumber LIKE '%' + @idNumber + '%')
                               AND (@phone = '' OR phone LIKE '%' + @phone + '%')
                             ORDER BY customerID DESC";
        List<CustomerInfo> list = new();
        using SqlConnection conn = new(DBConnection.GetConnectionString());
        conn.Open();
        using SqlCommand cmd = new(sql, conn);
        cmd.Parameters.AddWithValue("@name", name ?? string.Empty);
        cmd.Parameters.AddWithValue("@idNumber", idNumber ?? string.Empty);
        cmd.Parameters.AddWithValue("@phone", phone ?? string.Empty);
        using SqlDataReader reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(MapReader(reader));
        }
        return list;
    }

    /// <summary>统计该客户的在住记录数（删除前校验用）</summary>
    public int CountOccupiedByCustomer(int customerID)
    {
        const string sql = "SELECT COUNT(*) FROM T_CheckIn WHERE customerID = @customerID AND status = @status";
        using SqlConnection conn = new(DBConnection.GetConnectionString());
        conn.Open();
        using SqlCommand cmd = new(sql, conn);
        cmd.Parameters.AddWithValue("@customerID", customerID);
        cmd.Parameters.AddWithValue("@status", HotelSys.Common.BusinessConstants.CHECKIN_OCCUPIED);
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    /// <summary>SqlDataReader 映射为 CustomerInfo 实体</summary>
    private static CustomerInfo MapReader(SqlDataReader reader)
        => new()
        {
            CustomerID = reader.GetInt32(0),
            CustomerName = reader.GetString(1),
            Gender = reader.IsDBNull(2) ? null : reader.GetString(2),
            IdType = reader.GetString(3),
            IdNumber = reader.GetString(4),
            Phone = reader.IsDBNull(5) ? null : reader.GetString(5),
            Address = reader.IsDBNull(6) ? null : reader.GetString(6),
            CreateTime = reader.GetDateTime(7)
        };
}
