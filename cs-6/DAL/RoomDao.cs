using System.Data;
using Microsoft.Data.SqlClient;
using HotelSys.Models;
using HotelSys.Common;

namespace HotelSys.DAL;

/// <summary>
/// 客房信息数据访问类，继承泛型基类 BaseRepository&lt;RoomInfo&gt;
/// 查询时 JOIN T_RoomType 获取房型名称和单价
/// </summary>
public class RoomDao : BaseRepository<RoomInfo>
{
    /// <summary>查询全部客房（含房型名称和单价）</summary>
    public override List<RoomInfo> FindAll()
    {
        const string sql = @"SELECT r.roomNo, r.typeID, r.floor, r.bedCount, r.roomStatus, r.remark,
                                     rt.typeName, rt.price
                              FROM T_Room r
                              INNER JOIN T_RoomType rt ON r.typeID = rt.typeID
                              ORDER BY r.floor, r.roomNo";
        List<RoomInfo> list = new();
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

    /// <summary>按房号查询单条记录（含房型名称和单价）</summary>
    public override RoomInfo FindById(string id)
    {
        const string sql = @"SELECT r.roomNo, r.typeID, r.floor, r.bedCount, r.roomStatus, r.remark,
                                     rt.typeName, rt.price
                              FROM T_Room r
                              INNER JOIN T_RoomType rt ON r.typeID = rt.typeID
                              WHERE r.roomNo = @roomNo";
        using SqlConnection conn = new(DBConnection.GetConnectionString());
        conn.Open();
        using SqlCommand cmd = new(sql, conn);
        cmd.Parameters.AddWithValue("@roomNo", id);
        using SqlDataReader reader = cmd.ExecuteReader();
        return reader.Read() ? MapReader(reader) : null;
    }

    /// <summary>新增客房（默认状态为空闲）</summary>
    public override int Insert(RoomInfo entity)
    {
        const string sql = @"INSERT INTO T_Room (roomNo, typeID, floor, bedCount, roomStatus, remark)
                             VALUES (@roomNo, @typeID, @floor, @bedCount, @roomStatus, @remark)";
        using SqlConnection conn = new(DBConnection.GetConnectionString());
        conn.Open();
        using SqlCommand cmd = new(sql, conn);
        cmd.Parameters.AddWithValue("@roomNo", entity.RoomNo);
        cmd.Parameters.AddWithValue("@typeID", entity.TypeID);
        cmd.Parameters.AddWithValue("@floor", entity.Floor);
        cmd.Parameters.AddWithValue("@bedCount", entity.BedCount);
        // 新增客房默认状态为空闲（PRD 4.2.5）
        cmd.Parameters.AddWithValue("@roomStatus", entity.RoomStatus ?? RoomStatusConstants.FREE);
        cmd.Parameters.AddWithValue("@remark", (object)entity.Remark ?? DBNull.Value);
        return cmd.ExecuteNonQuery();
    }

    /// <summary>修改客房信息（不含房态，房态由业务驱动）</summary>
    public override int Update(RoomInfo entity)
    {
        const string sql = @"UPDATE T_Room SET typeID = @typeID, floor = @floor, bedCount = @bedCount, remark = @remark
                             WHERE roomNo = @roomNo";
        using SqlConnection conn = new(DBConnection.GetConnectionString());
        conn.Open();
        using SqlCommand cmd = new(sql, conn);
        cmd.Parameters.AddWithValue("@typeID", entity.TypeID);
        cmd.Parameters.AddWithValue("@floor", entity.Floor);
        cmd.Parameters.AddWithValue("@bedCount", entity.BedCount);
        cmd.Parameters.AddWithValue("@remark", (object)entity.Remark ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@roomNo", entity.RoomNo);
        return cmd.ExecuteNonQuery();
    }

    /// <summary>按房号删除</summary>
    public override int Delete(string id)
    {
        const string sql = "DELETE FROM T_Room WHERE roomNo = @roomNo";
        using SqlConnection conn = new(DBConnection.GetConnectionString());
        conn.Open();
        using SqlCommand cmd = new(sql, conn);
        cmd.Parameters.AddWithValue("@roomNo", id);
        return cmd.ExecuteNonQuery();
    }

    // ===== 以下为 Room 特有查询 =====

    /// <summary>按楼层查询客房</summary>
    public List<RoomInfo> GetByFloor(int floor)
    {
        const string sql = @"SELECT r.roomNo, r.typeID, r.floor, r.bedCount, r.roomStatus, r.remark,
                                     rt.typeName, rt.price
                              FROM T_Room r
                              INNER JOIN T_RoomType rt ON r.typeID = rt.typeID
                              WHERE r.floor = @floor
                              ORDER BY r.roomNo";
        List<RoomInfo> list = new();
        using SqlConnection conn = new(DBConnection.GetConnectionString());
        conn.Open();
        using SqlCommand cmd = new(sql, conn);
        cmd.Parameters.AddWithValue("@floor", floor);
        using SqlDataReader reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(MapReader(reader));
        }
        return list;
    }

    /// <summary>查询所有空闲房间（入住/换房时选择）</summary>
    public List<RoomInfo> GetFreeRooms()
    {
        const string sql = @"SELECT r.roomNo, r.typeID, r.floor, r.bedCount, r.roomStatus, r.remark,
                                     rt.typeName, rt.price
                              FROM T_Room r
                              INNER JOIN T_RoomType rt ON r.typeID = rt.typeID
                              WHERE r.roomStatus = @status
                              ORDER BY r.floor, r.roomNo";
        List<RoomInfo> list = new();
        using SqlConnection conn = new(DBConnection.GetConnectionString());
        conn.Open();
        using SqlCommand cmd = new(sql, conn);
        cmd.Parameters.AddWithValue("@status", RoomStatusConstants.FREE);
        using SqlDataReader reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(MapReader(reader));
        }
        return list;
    }

    /// <summary>按房态查询客房（房态看板用）</summary>
    public List<RoomInfo> GetByStatus(string status)
    {
        const string sql = @"SELECT r.roomNo, r.typeID, r.floor, r.bedCount, r.roomStatus, r.remark,
                                     rt.typeName, rt.price
                              FROM T_Room r
                              INNER JOIN T_RoomType rt ON r.typeID = rt.typeID
                              WHERE r.roomStatus = @status
                              ORDER BY r.floor, r.roomNo";
        List<RoomInfo> list = new();
        using SqlConnection conn = new(DBConnection.GetConnectionString());
        conn.Open();
        using SqlCommand cmd = new(sql, conn);
        cmd.Parameters.AddWithValue("@status", status);
        using SqlDataReader reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(MapReader(reader));
        }
        return list;
    }

    /// <summary>
    /// 更新房间状态（房态状态机驱动，PRD 4.2.5）
    /// 仅在业务操作（入住/退房/预订/维护）时调用
    /// </summary>
    /// <param name="roomNo">房号</param>
    /// <param name="newStatus">新状态</param>
    /// <returns>受影响行数</returns>
    public int UpdateStatus(string roomNo, string newStatus)
    {
        const string sql = "UPDATE T_Room SET roomStatus = @status WHERE roomNo = @roomNo";
        using SqlConnection conn = new(DBConnection.GetConnectionString());
        conn.Open();
        using SqlCommand cmd = new(sql, conn);
        cmd.Parameters.AddWithValue("@status", newStatus);
        cmd.Parameters.AddWithValue("@roomNo", roomNo);
        return cmd.ExecuteNonQuery();
    }

    /// <summary>统计该房间的在住/预留记录数（删除前校验用）</summary>
    public int CountActiveByRoom(string roomNo)
    {
        // 分别查询在住记录数和预留记录数后求和
        int checkInCount = ExecuteScalar(
            "SELECT COUNT(*) FROM T_CheckIn WHERE roomNo = @roomNo AND status = @occupied",
            CommandType.Text,
            new SqlParameter("@roomNo", roomNo),
            new SqlParameter("@occupied", BusinessConstants.CHECKIN_OCCUPIED));
        int reserveCount = ExecuteScalar(
            "SELECT COUNT(*) FROM T_Reservation WHERE roomNo = @roomNo AND status = @waiting",
            CommandType.Text,
            new SqlParameter("@roomNo", roomNo),
            new SqlParameter("@waiting", BusinessConstants.RESERVE_WAITING));
        return checkInCount + reserveCount;
    }

    /// <summary>SqlDataReader 映射为 RoomInfo 实体</summary>
    private static RoomInfo MapReader(SqlDataReader reader)
        => new()
        {
            RoomNo = reader.GetString(0),
            TypeID = reader.GetString(1),
            Floor = reader.GetInt32(2),
            BedCount = reader.GetInt32(3),
            RoomStatus = reader.GetString(4),
            Remark = reader.IsDBNull(5) ? null : reader.GetString(5),
            TypeName = reader.GetString(6),
            Price = reader.GetDecimal(7)
        };
}
