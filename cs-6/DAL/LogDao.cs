using Microsoft.Data.SqlClient;
using HotelSys.Models;

namespace HotelSys.DAL;

/// <summary>
/// 操作日志数据访问类，继承泛型基类 BaseRepository&lt;OperateLogInfo&gt;
/// 日志仅支持插入和查询，不可修改、不可删除（PRD 4.2.13）
/// </summary>
public class LogDao : BaseRepository<OperateLogInfo>
{
    /// <summary>查询全部日志（按时间倒序）</summary>
    public override List<OperateLogInfo> FindAll()
    {
        const string sql = "SELECT logID, userName, operateTime, operateType, operateContent, detail FROM T_OperateLog ORDER BY operateTime DESC";
        List<OperateLogInfo> list = new();
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

    /// <summary>按日志编号查询单条记录</summary>
    public override OperateLogInfo FindById(string id)
    {
        const string sql = "SELECT logID, userName, operateTime, operateType, operateContent, detail FROM T_OperateLog WHERE logID = @logID";
        using SqlConnection conn = new(DBConnection.GetConnectionString());
        conn.Open();
        using SqlCommand cmd = new(sql, conn);
        cmd.Parameters.AddWithValue("@logID", int.Parse(id));
        using SqlDataReader reader = cmd.ExecuteReader();
        return reader.Read() ? MapReader(reader) : null;
    }

    /// <summary>新增日志记录，返回新生成的日志编号</summary>
    public override int Insert(OperateLogInfo entity)
    {
        const string sql = @"INSERT INTO T_OperateLog (userName, operateType, operateContent, detail)
                             OUTPUT INSERTED.logID
                             VALUES (@userName, @operateType, @operateContent, @detail)";
        using SqlConnection conn = new(DBConnection.GetConnectionString());
        conn.Open();
        using SqlCommand cmd = new(sql, conn);
        cmd.Parameters.AddWithValue("@userName", entity.UserName);
        cmd.Parameters.AddWithValue("@operateType", entity.OperateType);
        cmd.Parameters.AddWithValue("@operateContent", entity.OperateContent);
        cmd.Parameters.AddWithValue("@detail", (object)entity.Detail ?? DBNull.Value);
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    /// <summary>日志不可修改，抛出不支持异常</summary>
    public override int Update(OperateLogInfo entity)
        => throw new NotSupportedException("操作日志不可修改");

    /// <summary>日志不可删除，抛出不支持异常</summary>
    public override int Delete(string id)
        => throw new NotSupportedException("操作日志不可删除");

    // ===== 以下为 Log 特有查询 =====

    /// <summary>多条件查询日志（用户/操作类型/时间范围，PRD F-22）</summary>
    public List<OperateLogInfo> Search(string userName, string operateType, DateTime? from, DateTime? to)
    {
        const string sql = @"SELECT logID, userName, operateTime, operateType, operateContent, detail
                             FROM T_OperateLog
                             WHERE (@userName = '' OR userName = @userName)
                               AND (@operateType = '' OR operateType = @operateType)
                               AND (@from IS NULL OR operateTime >= @from)
                               AND (@to IS NULL OR operateTime < DATEADD(DAY, 1, @to))
                             ORDER BY operateTime DESC";
        List<OperateLogInfo> list = new();
        using SqlConnection conn = new(DBConnection.GetConnectionString());
        conn.Open();
        using SqlCommand cmd = new(sql, conn);
        cmd.Parameters.AddWithValue("@userName", userName ?? string.Empty);
        cmd.Parameters.AddWithValue("@operateType", operateType ?? string.Empty);
        cmd.Parameters.AddWithValue("@from", (object)from ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@to", (object)to ?? DBNull.Value);
        using SqlDataReader reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(MapReader(reader));
        }
        return list;
    }

    /// <summary>SqlDataReader 映射为 OperateLogInfo 实体</summary>
    private static OperateLogInfo MapReader(SqlDataReader reader)
        => new()
        {
            LogID = reader.GetInt32(0),
            UserName = reader.GetString(1),
            OperateTime = reader.GetDateTime(2),
            OperateType = reader.GetString(3),
            OperateContent = reader.GetString(4),
            Detail = reader.IsDBNull(5) ? null : reader.GetString(5)
        };
}
