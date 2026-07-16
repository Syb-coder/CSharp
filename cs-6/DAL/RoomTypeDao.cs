using Microsoft.Data.SqlClient;
using HotelSys.Models;

namespace HotelSys.DAL;

/// <summary>
/// 客房类型数据访问类，继承泛型基类 BaseRepository&lt;RoomTypeInfo&gt;
/// </summary>
public class RoomTypeDao : BaseRepository<RoomTypeInfo>
{
    /// <summary>查询全部客房类型</summary>
    public override List<RoomTypeInfo> FindAll()
    {
        const string sql = "SELECT typeID, typeName, price, bedCount, description FROM T_RoomType ORDER BY typeID";
        List<RoomTypeInfo> list = new();
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

    /// <summary>按类型编号查询单条记录</summary>
    public override RoomTypeInfo FindById(string id)
    {
        const string sql = "SELECT typeID, typeName, price, bedCount, description FROM T_RoomType WHERE typeID = @typeID";
        using SqlConnection conn = new(DBConnection.GetConnectionString());
        conn.Open();
        using SqlCommand cmd = new(sql, conn);
        cmd.Parameters.AddWithValue("@typeID", id);
        using SqlDataReader reader = cmd.ExecuteReader();
        return reader.Read() ? MapReader(reader) : null;
    }

    /// <summary>按类型名称查询（用于唯一性校验）</summary>
    public RoomTypeInfo FindByName(string typeName)
    {
        const string sql = "SELECT typeID, typeName, price, bedCount, description FROM T_RoomType WHERE typeName = @typeName";
        using SqlConnection conn = new(DBConnection.GetConnectionString());
        conn.Open();
        using SqlCommand cmd = new(sql, conn);
        cmd.Parameters.AddWithValue("@typeName", typeName);
        using SqlDataReader reader = cmd.ExecuteReader();
        return reader.Read() ? MapReader(reader) : null;
    }

    /// <summary>新增客房类型</summary>
    public override int Insert(RoomTypeInfo entity)
    {
        const string sql = "INSERT INTO T_RoomType (typeID, typeName, price, bedCount, description) VALUES (@typeID, @typeName, @price, @bedCount, @description)";
        using SqlConnection conn = new(DBConnection.GetConnectionString());
        conn.Open();
        using SqlCommand cmd = new(sql, conn);
        cmd.Parameters.AddWithValue("@typeID", entity.TypeID);
        cmd.Parameters.AddWithValue("@typeName", entity.TypeName);
        cmd.Parameters.AddWithValue("@price", entity.Price);
        cmd.Parameters.AddWithValue("@bedCount", entity.BedCount);
        cmd.Parameters.AddWithValue("@description", (object)entity.Description ?? DBNull.Value);
        return cmd.ExecuteNonQuery();
    }

    /// <summary>修改客房类型</summary>
    public override int Update(RoomTypeInfo entity)
    {
        const string sql = "UPDATE T_RoomType SET typeName = @typeName, price = @price, bedCount = @bedCount, description = @description WHERE typeID = @typeID";
        using SqlConnection conn = new(DBConnection.GetConnectionString());
        conn.Open();
        using SqlCommand cmd = new(sql, conn);
        cmd.Parameters.AddWithValue("@typeName", entity.TypeName);
        cmd.Parameters.AddWithValue("@price", entity.Price);
        cmd.Parameters.AddWithValue("@bedCount", entity.BedCount);
        cmd.Parameters.AddWithValue("@description", (object)entity.Description ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@typeID", entity.TypeID);
        return cmd.ExecuteNonQuery();
    }

    /// <summary>按类型编号删除</summary>
    public override int Delete(string id)
    {
        const string sql = "DELETE FROM T_RoomType WHERE typeID = @typeID";
        using SqlConnection conn = new(DBConnection.GetConnectionString());
        conn.Open();
        using SqlCommand cmd = new(sql, conn);
        cmd.Parameters.AddWithValue("@typeID", id);
        return cmd.ExecuteNonQuery();
    }

    /// <summary>统计关联客房数量（删除前校验用）</summary>
    public int CountRoomsByType(string typeID)
    {
        const string sql = "SELECT COUNT(*) FROM T_Room WHERE typeID = @typeID";
        using SqlConnection conn = new(DBConnection.GetConnectionString());
        conn.Open();
        using SqlCommand cmd = new(sql, conn);
        cmd.Parameters.AddWithValue("@typeID", typeID);
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    /// <summary>SqlDataReader 映射为 RoomTypeInfo 实体</summary>
    private static RoomTypeInfo MapReader(SqlDataReader reader)
        => new()
        {
            TypeID = reader.GetString(0),
            TypeName = reader.GetString(1),
            Price = reader.GetDecimal(2),
            BedCount = reader.GetInt32(3),
            Description = reader.IsDBNull(4) ? null : reader.GetString(4)
        };
}
