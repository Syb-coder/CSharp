using System.Data;
using Microsoft.Data.SqlClient;
using HotelSys.Models;
using HotelSys.Common;

namespace HotelSys.DAL;

/// <summary>
/// 入住记录数据访问类，继承泛型基类 BaseRepository&lt;CheckInInfo&gt;
/// 查询时 JOIN T_Customer、T_Room、T_RoomType 获取关联显示字段和房型单价（用于费用计算）
/// 提供事务版本方法供 BLL 层 CheckInManager 管理入住/退房/换房事务
/// </summary>
public class CheckInDao : BaseRepository<CheckInInfo>
{
    private const string SelectFields = @"ci.checkInID, ci.customerID, ci.roomNo, ci.checkInTime, ci.expectCheckOut,
       ci.checkOutTime, ci.actualDays, ci.deposit, ci.roomCharge, ci.otherCharge,
       ci.consumeAmount, ci.totalAmount, ci.status, ci.remark,
       c.customerName, rt.typeName, rt.price, c.phone";

    private const string JoinFrom = @"FROM T_CheckIn ci
       INNER JOIN T_Customer c ON ci.customerID = c.customerID
       INNER JOIN T_Room r ON ci.roomNo = r.roomNo
       INNER JOIN T_RoomType rt ON r.typeID = rt.typeID";

    /// <summary>查询全部入住记录（按入住时间倒序）</summary>
    public override List<CheckInInfo> FindAll()
    {
        string sql = $"SELECT {SelectFields} {JoinFrom} ORDER BY ci.checkInTime DESC";
        return ExecuteList(sql);
    }

    /// <summary>按入住单号查询单条记录</summary>
    public override CheckInInfo FindById(string id)
    {
        string sql = $"SELECT {SelectFields} {JoinFrom} WHERE ci.checkInID = @checkInID";
        using SqlConnection conn = new(DBConnection.GetConnectionString());
        conn.Open();
        using SqlCommand cmd = new(sql, conn);
        cmd.Parameters.AddWithValue("@checkInID", int.Parse(id));
        using SqlDataReader reader = cmd.ExecuteReader();
        return reader.Read() ? MapReader(reader) : null;
    }

    /// <summary>按入住单号查询（int 版本）</summary>
    public CheckInInfo GetById(int checkInID)
    {
        string sql = $"SELECT {SelectFields} {JoinFrom} WHERE ci.checkInID = @checkInID";
        using SqlConnection conn = new(DBConnection.GetConnectionString());
        conn.Open();
        using SqlCommand cmd = new(sql, conn);
        cmd.Parameters.AddWithValue("@checkInID", checkInID);
        using SqlDataReader reader = cmd.ExecuteReader();
        return reader.Read() ? MapReader(reader) : null;
    }

    /// <summary>新增入住记录，返回新生成的入住单号</summary>
    public override int Insert(CheckInInfo entity)
    {
        const string sql = @"INSERT INTO T_CheckIn (customerID, roomNo, checkInTime, expectCheckOut, deposit, status, remark)
                             OUTPUT INSERTED.checkInID
                             VALUES (@customerID, @roomNo, @checkInTime, @expectCheckOut, @deposit, @status, @remark)";
        using SqlConnection conn = new(DBConnection.GetConnectionString());
        conn.Open();
        using SqlCommand cmd = new(sql, conn);
        cmd.Parameters.AddWithValue("@customerID", entity.CustomerID);
        cmd.Parameters.AddWithValue("@roomNo", entity.RoomNo);
        cmd.Parameters.AddWithValue("@checkInTime", entity.CheckInTime);
        cmd.Parameters.AddWithValue("@expectCheckOut", entity.ExpectCheckOut);
        cmd.Parameters.AddWithValue("@deposit", entity.Deposit);
        cmd.Parameters.AddWithValue("@status", entity.Status ?? BusinessConstants.CHECKIN_OCCUPIED);
        cmd.Parameters.AddWithValue("@remark", (object)entity.Remark ?? DBNull.Value);
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    /// <summary>
    /// 新增入住记录（事务版本，供 BLL 层在事务中调用）
    /// </summary>
    public int InsertWithTransaction(CheckInInfo entity, SqlConnection conn, SqlTransaction trans)
    {
        const string sql = @"INSERT INTO T_CheckIn (customerID, roomNo, checkInTime, expectCheckOut, deposit, status, remark)
                             OUTPUT INSERTED.checkInID
                             VALUES (@customerID, @roomNo, @checkInTime, @expectCheckOut, @deposit, @status, @remark)";
        using SqlCommand cmd = new(sql, conn, trans);
        cmd.Parameters.AddWithValue("@customerID", entity.CustomerID);
        cmd.Parameters.AddWithValue("@roomNo", entity.RoomNo);
        cmd.Parameters.AddWithValue("@checkInTime", entity.CheckInTime);
        cmd.Parameters.AddWithValue("@expectCheckOut", entity.ExpectCheckOut);
        cmd.Parameters.AddWithValue("@deposit", entity.Deposit);
        cmd.Parameters.AddWithValue("@status", entity.Status ?? BusinessConstants.CHECKIN_OCCUPIED);
        cmd.Parameters.AddWithValue("@remark", (object)entity.Remark ?? DBNull.Value);
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    /// <summary>修改入住记录（结账时更新所有费用字段和状态）</summary>
    public override int Update(CheckInInfo entity)
    {
        const string sql = @"UPDATE T_CheckIn SET checkOutTime = @checkOutTime, actualDays = @actualDays,
                             roomCharge = @roomCharge, otherCharge = @otherCharge, consumeAmount = @consumeAmount,
                             totalAmount = @totalAmount, status = @status, remark = @remark,
                             expectCheckOut = @expectCheckOut, roomNo = @roomNo
                             WHERE checkInID = @checkInID";
        using SqlConnection conn = new(DBConnection.GetConnectionString());
        conn.Open();
        using SqlCommand cmd = new(sql, conn);
        AddUpdateParameters(cmd, entity);
        return cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// 修改入住记录（事务版本，供 BLL 层在事务中调用）
    /// </summary>
    public int UpdateWithTransaction(CheckInInfo entity, SqlConnection conn, SqlTransaction trans)
    {
        const string sql = @"UPDATE T_CheckIn SET checkOutTime = @checkOutTime, actualDays = @actualDays,
                             roomCharge = @roomCharge, otherCharge = @otherCharge, consumeAmount = @consumeAmount,
                             totalAmount = @totalAmount, status = @status, remark = @remark,
                             expectCheckOut = @expectCheckOut, roomNo = @roomNo
                             WHERE checkInID = @checkInID";
        using SqlCommand cmd = new(sql, conn, trans);
        AddUpdateParameters(cmd, entity);
        return cmd.ExecuteNonQuery();
    }

    /// <summary>按入住单号删除</summary>
    public override int Delete(string id)
    {
        const string sql = "DELETE FROM T_CheckIn WHERE checkInID = @checkInID";
        using SqlConnection conn = new(DBConnection.GetConnectionString());
        conn.Open();
        using SqlCommand cmd = new(sql, conn);
        cmd.Parameters.AddWithValue("@checkInID", int.Parse(id));
        return cmd.ExecuteNonQuery();
    }

    // ===== 以下为 CheckIn 特有查询 =====

    /// <summary>查询所有在住记录（PRD F-13）</summary>
    public List<CheckInInfo> GetOccupiedList()
    {
        string sql = $"SELECT {SelectFields} {JoinFrom} WHERE ci.status = @status ORDER BY ci.checkInTime DESC";
        return ExecuteList(sql, new SqlParameter("@status", BusinessConstants.CHECKIN_OCCUPIED));
    }

    /// <summary>查询所有已结账历史记录</summary>
    public List<CheckInInfo> GetHistoryList()
    {
        string sql = $"SELECT {SelectFields} {JoinFrom} WHERE ci.status = @status ORDER BY ci.checkOutTime DESC";
        return ExecuteList(sql, new SqlParameter("@status", BusinessConstants.CHECKIN_CHECKEDOUT));
    }

    /// <summary>多条件查询入住记录（房号/客户姓名/日期范围/状态，PRD F-13）</summary>
    public List<CheckInInfo> Search(string roomNo, string customerName, DateTime? from, DateTime? to, string status)
    {
        string sql = $@"SELECT {SelectFields} {JoinFrom}
                        WHERE (@roomNo = '' OR ci.roomNo = @roomNo)
                          AND (@customerName = '' OR c.customerName LIKE '%' + @customerName + '%')
                          AND (@from IS NULL OR ci.checkInTime >= @from)
                          AND (@to IS NULL OR ci.checkInTime < DATEADD(DAY, 1, @to))
                          AND (@status = '' OR ci.status = @status)
                        ORDER BY ci.checkInTime DESC";
        List<CheckInInfo> list = new();
        using SqlConnection conn = new(DBConnection.GetConnectionString());
        conn.Open();
        using SqlCommand cmd = new(sql, conn);
        cmd.Parameters.AddWithValue("@roomNo", roomNo ?? string.Empty);
        cmd.Parameters.AddWithValue("@customerName", customerName ?? string.Empty);
        cmd.Parameters.AddWithValue("@from", (object)from ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@to", (object)to ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@status", status ?? string.Empty);
        using SqlDataReader reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(MapReader(reader));
        }
        return list;
    }

    /// <summary>按客户编号查询入住历史（PRD F-08）</summary>
    public List<CheckInInfo> GetByCustomer(int customerID)
    {
        string sql = $"SELECT {SelectFields} {JoinFrom} WHERE ci.customerID = @customerID ORDER BY ci.checkInTime DESC";
        return ExecuteList(sql, new SqlParameter("@customerID", customerID));
    }

    /// <summary>按房号查询在住记录（换房/退房时用）</summary>
    public CheckInInfo GetOccupiedByRoom(string roomNo)
    {
        string sql = $"SELECT {SelectFields} {JoinFrom} WHERE ci.roomNo = @roomNo AND ci.status = @status";
        using SqlConnection conn = new(DBConnection.GetConnectionString());
        conn.Open();
        using SqlCommand cmd = new(sql, conn);
        cmd.Parameters.AddWithValue("@roomNo", roomNo);
        cmd.Parameters.AddWithValue("@status", BusinessConstants.CHECKIN_OCCUPIED);
        using SqlDataReader reader = cmd.ExecuteReader();
        return reader.Read() ? MapReader(reader) : null;
    }

    // ===== 统计查询（PRD 4.2.3 / 4.2.11） =====

    /// <summary>今日入住数（PRD 4.2.3 统计卡片1）</summary>
    public int GetTodayCheckInCount()
    {
        const string sql = "SELECT COUNT(*) FROM T_CheckIn WHERE CONVERT(DATE, checkInTime) = CONVERT(DATE, GETDATE()) AND status != @cancelled";
        return ExecuteScalar(sql, CommandType.Text, new SqlParameter("@cancelled", BusinessConstants.CHECKIN_CANCELLED));
    }

    /// <summary>今日退房数（PRD 4.2.3 统计卡片2）</summary>
    public int GetTodayCheckOutCount()
    {
        const string sql = "SELECT COUNT(*) FROM T_CheckIn WHERE CONVERT(DATE, checkOutTime) = CONVERT(DATE, GETDATE()) AND status = @checkedout";
        return ExecuteScalar(sql, CommandType.Text, new SqlParameter("@checkedout", BusinessConstants.CHECKIN_CHECKEDOUT));
    }

    /// <summary>当前在住数（PRD 4.2.3 统计卡片3）</summary>
    public int GetOccupiedCount()
    {
        const string sql = "SELECT COUNT(*) FROM T_CheckIn WHERE status = @occupied";
        return ExecuteScalar(sql, CommandType.Text, new SqlParameter("@occupied", BusinessConstants.CHECKIN_OCCUPIED));
    }

    /// <summary>今日营收（PRD 4.2.3 统计卡片4）</summary>
    public decimal GetTodayRevenue()
    {
        const string sql = "SELECT ISNULL(SUM(totalAmount), 0) FROM T_CheckIn WHERE CONVERT(DATE, checkOutTime) = CONVERT(DATE, GETDATE()) AND status = @checkedout";
        return ExecuteScalarDecimal(sql, CommandType.Text, new SqlParameter("@checkedout", BusinessConstants.CHECKIN_CHECKEDOUT));
    }

    /// <summary>按日期范围查询营收（PRD 4.2.11）</summary>
    public decimal GetRevenueByDateRange(DateTime from, DateTime to)
    {
        const string sql = "SELECT ISNULL(SUM(totalAmount), 0) FROM T_CheckIn WHERE checkOutTime >= @from AND checkOutTime < DATEADD(DAY, 1, @to) AND status = @checkedout";
        return ExecuteScalarDecimal(sql, CommandType.Text,
            new SqlParameter("@from", from),
            new SqlParameter("@to", to),
            new SqlParameter("@checkedout", BusinessConstants.CHECKIN_CHECKEDOUT));
    }

    /// <summary>按日期范围查询结账记录数（用于平均客单价计算）</summary>
    public int GetCheckOutCountByDateRange(DateTime from, DateTime to)
    {
        const string sql = "SELECT COUNT(*) FROM T_CheckIn WHERE checkOutTime >= @from AND checkOutTime < DATEADD(DAY, 1, @to) AND status = @checkedout";
        return ExecuteScalar(sql, CommandType.Text,
            new SqlParameter("@from", from),
            new SqlParameter("@to", to),
            new SqlParameter("@checkedout", BusinessConstants.CHECKIN_CHECKEDOUT));
    }

    /// <summary>房型入住排行 Top5（PRD 4.2.11）</summary>
    public List<KeyValuePair<string, int>> GetRoomTypeRanking()
    {
        const string sql = @"SELECT TOP 5 rt.typeName, COUNT(*) AS cnt
                             FROM T_CheckIn ci
                             INNER JOIN T_Room r ON ci.roomNo = r.roomNo
                             INNER JOIN T_RoomType rt ON r.typeID = rt.typeID
                             WHERE ci.status = @checkedout
                             GROUP BY rt.typeName
                             ORDER BY cnt DESC";
        List<KeyValuePair<string, int>> list = new();
        using SqlConnection conn = new(DBConnection.GetConnectionString());
        conn.Open();
        using SqlCommand cmd = new(sql, conn);
        cmd.Parameters.AddWithValue("@checkedout", BusinessConstants.CHECKIN_CHECKEDOUT);
        using SqlDataReader reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(new KeyValuePair<string, int>(reader.GetString(0), reader.GetInt32(1)));
        }
        return list;
    }

    // ===== 私有辅助方法 =====

    /// <summary>执行查询并返回列表（带参数）</summary>
    private List<CheckInInfo> ExecuteList(string sql, params SqlParameter[] parameters)
    {
        List<CheckInInfo> list = new();
        using SqlConnection conn = new(DBConnection.GetConnectionString());
        conn.Open();
        using SqlCommand cmd = new(sql, conn);
        if (parameters != null)
        {
            foreach (var p in parameters) cmd.Parameters.Add(p);
        }
        using SqlDataReader reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(MapReader(reader));
        }
        return list;
    }

    /// <summary>为 Update 命令添加参数（复用，避免重复代码）</summary>
    private static void AddUpdateParameters(SqlCommand cmd, CheckInInfo entity)
    {
        cmd.Parameters.AddWithValue("@checkOutTime", (object)entity.CheckOutTime ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@actualDays", (object)entity.ActualDays ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@roomCharge", (object)entity.RoomCharge ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@otherCharge", (object)entity.OtherCharge ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@consumeAmount", (object)entity.ConsumeAmount ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@totalAmount", (object)entity.TotalAmount ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@status", entity.Status);
        cmd.Parameters.AddWithValue("@remark", (object)entity.Remark ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@expectCheckOut", entity.ExpectCheckOut);
        cmd.Parameters.AddWithValue("@roomNo", entity.RoomNo);
        cmd.Parameters.AddWithValue("@checkInID", entity.CheckInID);
    }

    /// <summary>SqlDataReader 映射为 CheckInInfo 实体</summary>
    private static CheckInInfo MapReader(SqlDataReader reader)
        => new()
        {
            CheckInID = reader.GetInt32(0),
            CustomerID = reader.GetInt32(1),
            RoomNo = reader.GetString(2),
            CheckInTime = reader.GetDateTime(3),
            ExpectCheckOut = reader.GetDateTime(4),
            CheckOutTime = reader.IsDBNull(5) ? null : reader.GetDateTime(5),
            ActualDays = reader.IsDBNull(6) ? null : reader.GetInt32(6),
            Deposit = reader.GetDecimal(7),
            RoomCharge = reader.IsDBNull(8) ? null : reader.GetDecimal(8),
            OtherCharge = reader.IsDBNull(9) ? null : reader.GetDecimal(9),
            ConsumeAmount = reader.IsDBNull(10) ? null : reader.GetDecimal(10),
            TotalAmount = reader.IsDBNull(11) ? null : reader.GetDecimal(11),
            Status = reader.GetString(12),
            Remark = reader.IsDBNull(13) ? null : reader.GetString(13),
            CustomerName = reader.GetString(14),
            TypeName = reader.GetString(15),
            Price = reader.GetDecimal(16),
            Phone = reader.IsDBNull(17) ? null : reader.GetString(17)
        };
}
