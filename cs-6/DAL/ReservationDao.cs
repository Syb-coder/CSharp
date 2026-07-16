using Microsoft.Data.SqlClient;
using HotelSys.Models;
using HotelSys.Common;

namespace HotelSys.DAL;

/// <summary>
/// 预订记录数据访问类，继承泛型基类 BaseRepository&lt;ReservationInfo&gt;
/// 查询时 JOIN T_Customer、T_Room、T_RoomType 获取关联显示字段
/// </summary>
public class ReservationDao : BaseRepository<ReservationInfo>
{
    /// <summary>查询全部预订记录（含关联字段）</summary>
    public override List<ReservationInfo> FindAll()
    {
        const string sql = @"SELECT rv.reserveID, rv.customerID, rv.roomNo, rv.expectCheckIn, rv.expectDays,
                                     rv.contactPhone, rv.reserveTime, rv.status, rv.remark,
                                     c.customerName, rt.typeName, r.floor
                              FROM T_Reservation rv
                              INNER JOIN T_Customer c ON rv.customerID = c.customerID
                              INNER JOIN T_Room r ON rv.roomNo = r.roomNo
                              INNER JOIN T_RoomType rt ON r.typeID = rt.typeID
                              ORDER BY rv.reserveTime DESC";
        List<ReservationInfo> list = new();
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

    /// <summary>按预订编号查询单条记录</summary>
    public override ReservationInfo FindById(string id)
    {
        const string sql = @"SELECT rv.reserveID, rv.customerID, rv.roomNo, rv.expectCheckIn, rv.expectDays,
                                     rv.contactPhone, rv.reserveTime, rv.status, rv.remark,
                                     c.customerName, rt.typeName, r.floor
                              FROM T_Reservation rv
                              INNER JOIN T_Customer c ON rv.customerID = c.customerID
                              INNER JOIN T_Room r ON rv.roomNo = r.roomNo
                              INNER JOIN T_RoomType rt ON r.typeID = rt.typeID
                              WHERE rv.reserveID = @reserveID";
        using SqlConnection conn = new(DBConnection.GetConnectionString());
        conn.Open();
        using SqlCommand cmd = new(sql, conn);
        cmd.Parameters.AddWithValue("@reserveID", int.Parse(id));
        using SqlDataReader reader = cmd.ExecuteReader();
        return reader.Read() ? MapReader(reader) : null;
    }

    /// <summary>新增预订记录，返回新生成的预订编号</summary>
    public override int Insert(ReservationInfo entity)
    {
        const string sql = @"INSERT INTO T_Reservation (customerID, roomNo, expectCheckIn, expectDays, contactPhone, status, remark)
                             OUTPUT INSERTED.reserveID
                             VALUES (@customerID, @roomNo, @expectCheckIn, @expectDays, @contactPhone, @status, @remark)";
        using SqlConnection conn = new(DBConnection.GetConnectionString());
        conn.Open();
        using SqlCommand cmd = new(sql, conn);
        cmd.Parameters.AddWithValue("@customerID", entity.CustomerID);
        cmd.Parameters.AddWithValue("@roomNo", entity.RoomNo);
        cmd.Parameters.AddWithValue("@expectCheckIn", entity.ExpectCheckIn);
        cmd.Parameters.AddWithValue("@expectDays", entity.ExpectDays);
        cmd.Parameters.AddWithValue("@contactPhone", entity.ContactPhone);
        cmd.Parameters.AddWithValue("@status", entity.Status ?? BusinessConstants.RESERVE_WAITING);
        cmd.Parameters.AddWithValue("@remark", (object)entity.Remark ?? DBNull.Value);
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    /// <summary>修改预订记录</summary>
    public override int Update(ReservationInfo entity)
    {
        const string sql = @"UPDATE T_Reservation SET customerID = @customerID, roomNo = @roomNo, expectCheckIn = @expectCheckIn,
                             expectDays = @expectDays, contactPhone = @contactPhone, status = @status, remark = @remark
                             WHERE reserveID = @reserveID";
        using SqlConnection conn = new(DBConnection.GetConnectionString());
        conn.Open();
        using SqlCommand cmd = new(sql, conn);
        cmd.Parameters.AddWithValue("@customerID", entity.CustomerID);
        cmd.Parameters.AddWithValue("@roomNo", entity.RoomNo);
        cmd.Parameters.AddWithValue("@expectCheckIn", entity.ExpectCheckIn);
        cmd.Parameters.AddWithValue("@expectDays", entity.ExpectDays);
        cmd.Parameters.AddWithValue("@contactPhone", entity.ContactPhone);
        cmd.Parameters.AddWithValue("@status", entity.Status);
        cmd.Parameters.AddWithValue("@remark", (object)entity.Remark ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@reserveID", entity.ReserveID);
        return cmd.ExecuteNonQuery();
    }

    /// <summary>按预订编号删除</summary>
    public override int Delete(string id)
    {
        const string sql = "DELETE FROM T_Reservation WHERE reserveID = @reserveID";
        using SqlConnection conn = new(DBConnection.GetConnectionString());
        conn.Open();
        using SqlCommand cmd = new(sql, conn);
        cmd.Parameters.AddWithValue("@reserveID", int.Parse(id));
        return cmd.ExecuteNonQuery();
    }

    // ===== 以下为 Reservation 特有查询 =====

    /// <summary>按预订编号查询（int 版本）</summary>
    public ReservationInfo GetById(int reserveID) => FindById(reserveID.ToString());

    /// <summary>更新预订状态</summary>
    public int UpdateStatus(int reserveID, string status)
    {
        const string sql = "UPDATE T_Reservation SET status = @status WHERE reserveID = @reserveID";
        using SqlConnection conn = new(DBConnection.GetConnectionString());
        conn.Open();
        using SqlCommand cmd = new(sql, conn);
        cmd.Parameters.AddWithValue("@status", status);
        cmd.Parameters.AddWithValue("@reserveID", reserveID);
        return cmd.ExecuteNonQuery();
    }

    /// <summary>多条件查询预订记录（客户姓名/状态/日期范围）</summary>
    public List<ReservationInfo> Search(string customerName, string status, DateTime? fromDate, DateTime? toDate)
    {
        const string sql = @"SELECT rv.reserveID, rv.customerID, rv.roomNo, rv.expectCheckIn, rv.expectDays,
                                     rv.contactPhone, rv.reserveTime, rv.status, rv.remark,
                                     c.customerName, rt.typeName, r.floor
                              FROM T_Reservation rv
                              INNER JOIN T_Customer c ON rv.customerID = c.customerID
                              INNER JOIN T_Room r ON rv.roomNo = r.roomNo
                              INNER JOIN T_RoomType rt ON r.typeID = rt.typeID
                              WHERE (@customerName = '' OR c.customerName LIKE '%' + @customerName + '%')
                                AND (@status = '' OR rv.status = @status)
                                AND (@fromDate IS NULL OR rv.expectCheckIn >= @fromDate)
                                AND (@toDate IS NULL OR rv.expectCheckIn <= @toDate)
                              ORDER BY rv.reserveTime DESC";
        List<ReservationInfo> list = new();
        using SqlConnection conn = new(DBConnection.GetConnectionString());
        conn.Open();
        using SqlCommand cmd = new(sql, conn);
        cmd.Parameters.AddWithValue("@customerName", customerName ?? string.Empty);
        cmd.Parameters.AddWithValue("@status", status ?? string.Empty);
        cmd.Parameters.AddWithValue("@fromDate", (object)fromDate ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@toDate", (object)toDate ?? DBNull.Value);
        using SqlDataReader reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(MapReader(reader));
        }
        return list;
    }

    /// <summary>
    /// 检查同一房间在同一时间段是否已有有效预订（时间重叠校验，PRD F-09）
    /// </summary>
    /// <param name="roomNo">房号</param>
    /// <param name="expectCheckIn">预计入住日期</param>
    /// <param name="expectDays">预计入住天数</param>
    /// <param name="excludeReserveID">排除的预订编号（修改时用），0表示不排除</param>
    /// <returns>有重叠返回 true，否则 false</returns>
    public bool HasTimeConflict(string roomNo, DateTime expectCheckIn, int expectDays, int excludeReserveID = 0)
    {
        // 时间重叠判断：新预订的 [expectCheckIn, expectCheckIn+expectDays) 与已有预订的 [expectCheckIn, expectCheckIn+expectDays) 有交集
        DateTime newEnd = expectCheckIn.AddDays(expectDays);
        const string sql = @"SELECT COUNT(*) FROM T_Reservation
                              WHERE roomNo = @roomNo AND status = @waiting
                                AND (@excludeID = 0 OR reserveID != @excludeID)
                                AND expectCheckIn < @newEnd
                                AND DATEADD(DAY, expectDays, expectCheckIn) > @newStart";
        using SqlConnection conn = new(DBConnection.GetConnectionString());
        conn.Open();
        using SqlCommand cmd = new(sql, conn);
        cmd.Parameters.AddWithValue("@roomNo", roomNo);
        cmd.Parameters.AddWithValue("@waiting", BusinessConstants.RESERVE_WAITING);
        cmd.Parameters.AddWithValue("@excludeID", excludeReserveID);
        cmd.Parameters.AddWithValue("@newEnd", newEnd);
        cmd.Parameters.AddWithValue("@newStart", expectCheckIn);
        return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
    }

    /// <summary>查询所有超期待入住预订（自动过期用，PRD F-11/AC-11.1）</summary>
    public List<ReservationInfo> GetExpiredReservations()
    {
        const string sql = @"SELECT rv.reserveID, rv.customerID, rv.roomNo, rv.expectCheckIn, rv.expectDays,
                                     rv.contactPhone, rv.reserveTime, rv.status, rv.remark,
                                     c.customerName, rt.typeName, r.floor
                              FROM T_Reservation rv
                              INNER JOIN T_Customer c ON rv.customerID = c.customerID
                              INNER JOIN T_Room r ON rv.roomNo = r.roomNo
                              INNER JOIN T_RoomType rt ON r.typeID = rt.typeID
                              WHERE rv.status = @waiting AND rv.expectCheckIn < DATEADD(DAY, -1, GETDATE())";
        List<ReservationInfo> list = new();
        using SqlConnection conn = new(DBConnection.GetConnectionString());
        conn.Open();
        using SqlCommand cmd = new(sql, conn);
        cmd.Parameters.AddWithValue("@waiting", BusinessConstants.RESERVE_WAITING);
        using SqlDataReader reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(MapReader(reader));
        }
        return list;
    }

    /// <summary>SqlDataReader 映射为 ReservationInfo 实体</summary>
    private static ReservationInfo MapReader(SqlDataReader reader)
        => new()
        {
            ReserveID = reader.GetInt32(0),
            CustomerID = reader.GetInt32(1),
            RoomNo = reader.GetString(2),
            ExpectCheckIn = reader.GetDateTime(3),
            ExpectDays = reader.GetInt32(4),
            ContactPhone = reader.GetString(5),
            ReserveTime = reader.GetDateTime(6),
            Status = reader.GetString(7),
            Remark = reader.IsDBNull(8) ? null : reader.GetString(8),
            CustomerName = reader.GetString(9),
            TypeName = reader.GetString(10),
            Floor = reader.GetInt32(11)
        };
}
