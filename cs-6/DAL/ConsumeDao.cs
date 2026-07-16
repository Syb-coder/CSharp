using Microsoft.Data.SqlClient;
using HotelSys.Models;

namespace HotelSys.DAL;

/// <summary>
/// 消费记录数据访问类，继承泛型基类 BaseRepository&lt;ConsumeInfo&gt;
/// 查询时 JOIN T_CheckIn、T_Customer、T_Room 获取关联显示字段
/// </summary>
public class ConsumeDao : BaseRepository<ConsumeInfo>
{
    /// <summary>查询全部消费记录（含关联字段）</summary>
    public override List<ConsumeInfo> FindAll()
    {
        const string sql = @"SELECT cs.consumeID, cs.checkInID, cs.itemName, cs.amount, cs.consumeTime, cs.remark,
                                     r.roomNo, c.customerName
                              FROM T_Consume cs
                              INNER JOIN T_CheckIn ci ON cs.checkInID = ci.checkInID
                              INNER JOIN T_Room r ON ci.roomNo = r.roomNo
                              INNER JOIN T_Customer c ON ci.customerID = c.customerID
                              ORDER BY cs.consumeTime DESC";
        List<ConsumeInfo> list = new();
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

    /// <summary>按消费编号查询单条记录</summary>
    public override ConsumeInfo FindById(string id)
    {
        const string sql = @"SELECT cs.consumeID, cs.checkInID, cs.itemName, cs.amount, cs.consumeTime, cs.remark,
                                     r.roomNo, c.customerName
                              FROM T_Consume cs
                              INNER JOIN T_CheckIn ci ON cs.checkInID = ci.checkInID
                              INNER JOIN T_Room r ON ci.roomNo = r.roomNo
                              INNER JOIN T_Customer c ON ci.customerID = c.customerID
                              WHERE cs.consumeID = @consumeID";
        using SqlConnection conn = new(DBConnection.GetConnectionString());
        conn.Open();
        using SqlCommand cmd = new(sql, conn);
        cmd.Parameters.AddWithValue("@consumeID", int.Parse(id));
        using SqlDataReader reader = cmd.ExecuteReader();
        return reader.Read() ? MapReader(reader) : null;
    }

    /// <summary>新增消费记录，返回新生成的消费编号</summary>
    public override int Insert(ConsumeInfo entity)
    {
        const string sql = @"INSERT INTO T_Consume (checkInID, itemName, amount, consumeTime, remark)
                             OUTPUT INSERTED.consumeID
                             VALUES (@checkInID, @itemName, @amount, @consumeTime, @remark)";
        using SqlConnection conn = new(DBConnection.GetConnectionString());
        conn.Open();
        using SqlCommand cmd = new(sql, conn);
        cmd.Parameters.AddWithValue("@checkInID", entity.CheckInID);
        cmd.Parameters.AddWithValue("@itemName", entity.ItemName);
        cmd.Parameters.AddWithValue("@amount", entity.Amount);
        cmd.Parameters.AddWithValue("@consumeTime", entity.ConsumeTime);
        cmd.Parameters.AddWithValue("@remark", (object)entity.Remark ?? DBNull.Value);
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    /// <summary>修改消费记录</summary>
    public override int Update(ConsumeInfo entity)
    {
        const string sql = @"UPDATE T_Consume SET itemName = @itemName, amount = @amount, consumeTime = @consumeTime, remark = @remark
                             WHERE consumeID = @consumeID";
        using SqlConnection conn = new(DBConnection.GetConnectionString());
        conn.Open();
        using SqlCommand cmd = new(sql, conn);
        cmd.Parameters.AddWithValue("@itemName", entity.ItemName);
        cmd.Parameters.AddWithValue("@amount", entity.Amount);
        cmd.Parameters.AddWithValue("@consumeTime", entity.ConsumeTime);
        cmd.Parameters.AddWithValue("@remark", (object)entity.Remark ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@consumeID", entity.ConsumeID);
        return cmd.ExecuteNonQuery();
    }

    /// <summary>按消费编号删除</summary>
    public override int Delete(string id)
    {
        const string sql = "DELETE FROM T_Consume WHERE consumeID = @consumeID";
        using SqlConnection conn = new(DBConnection.GetConnectionString());
        conn.Open();
        using SqlCommand cmd = new(sql, conn);
        cmd.Parameters.AddWithValue("@consumeID", int.Parse(id));
        return cmd.ExecuteNonQuery();
    }

    // ===== 以下为 Consume 特有查询 =====

    /// <summary>按入住单查询消费明细（PRD F-17）</summary>
    public List<ConsumeInfo> GetByCheckIn(int checkInID)
    {
        const string sql = @"SELECT cs.consumeID, cs.checkInID, cs.itemName, cs.amount, cs.consumeTime, cs.remark,
                                     r.roomNo, c.customerName
                              FROM T_Consume cs
                              INNER JOIN T_CheckIn ci ON cs.checkInID = ci.checkInID
                              INNER JOIN T_Room r ON ci.roomNo = r.roomNo
                              INNER JOIN T_Customer c ON ci.customerID = c.customerID
                              WHERE cs.checkInID = @checkInID
                              ORDER BY cs.consumeTime DESC";
        List<ConsumeInfo> list = new();
        using SqlConnection conn = new(DBConnection.GetConnectionString());
        conn.Open();
        using SqlCommand cmd = new(sql, conn);
        cmd.Parameters.AddWithValue("@checkInID", checkInID);
        using SqlDataReader reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(MapReader(reader));
        }
        return list;
    }

    /// <summary>按日期范围查询消费明细</summary>
    public List<ConsumeInfo> GetByDateRange(DateTime from, DateTime to)
    {
        const string sql = @"SELECT cs.consumeID, cs.checkInID, cs.itemName, cs.amount, cs.consumeTime, cs.remark,
                                     r.roomNo, c.customerName
                              FROM T_Consume cs
                              INNER JOIN T_CheckIn ci ON cs.checkInID = ci.checkInID
                              INNER JOIN T_Room r ON ci.roomNo = r.roomNo
                              INNER JOIN T_Customer c ON ci.customerID = c.customerID
                              WHERE cs.consumeTime >= @from AND cs.consumeTime < DATEADD(DAY, 1, @to)
                              ORDER BY cs.consumeTime DESC";
        List<ConsumeInfo> list = new();
        using SqlConnection conn = new(DBConnection.GetConnectionString());
        conn.Open();
        using SqlCommand cmd = new(sql, conn);
        cmd.Parameters.AddWithValue("@from", from);
        cmd.Parameters.AddWithValue("@to", to);
        using SqlDataReader reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(MapReader(reader));
        }
        return list;
    }

    /// <summary>汇总指定入住单的消费总额（结算退房时预填参考值，PRD F-18）</summary>
    public decimal GetTotalByCheckIn(int checkInID)
    {
        const string sql = "SELECT ISNULL(SUM(amount), 0) FROM T_Consume WHERE checkInID = @checkInID";
        using SqlConnection conn = new(DBConnection.GetConnectionString());
        conn.Open();
        using SqlCommand cmd = new(sql, conn);
        cmd.Parameters.AddWithValue("@checkInID", checkInID);
        object result = cmd.ExecuteScalar();
        return result == DBNull.Value || result == null ? 0m : Convert.ToDecimal(result);
    }

    /// <summary>SqlDataReader 映射为 ConsumeInfo 实体</summary>
    private static ConsumeInfo MapReader(SqlDataReader reader)
        => new()
        {
            ConsumeID = reader.GetInt32(0),
            CheckInID = reader.GetInt32(1),
            ItemName = reader.GetString(2),
            Amount = reader.GetDecimal(3),
            ConsumeTime = reader.GetDateTime(4),
            Remark = reader.IsDBNull(5) ? null : reader.GetString(5),
            RoomNo = reader.GetString(6),
            CustomerName = reader.GetString(7)
        };
}
