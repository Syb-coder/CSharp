using Microsoft.Data.SqlClient;
using LibrarySys.Models;

namespace LibrarySys.DAL;

/// <summary>
/// 系统用户数据访问类，继承泛型基类 BaseRepository&lt;UserInfo&gt;
/// </summary>
public class UserDao : BaseRepository<UserInfo>
{
    /// <summary>查询全部用户</summary>
    public override List<UserInfo> FindAll()
    {
        const string sql = "SELECT userName, userPassword, userPurview FROM T_User ORDER BY userName";
        List<UserInfo> list = new();
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

    /// <summary>按用户名查询单条记录</summary>
    public override UserInfo FindById(string id)
    {
        const string sql = "SELECT userName, userPassword, userPurview FROM T_User WHERE userName = @userName";
        using SqlConnection conn = new(DBConnection.GetConnectionString());
        conn.Open();
        using SqlCommand cmd = new(sql, conn);
        cmd.Parameters.AddWithValue("@userName", id);
        using SqlDataReader reader = cmd.ExecuteReader();
        return reader.Read() ? MapReader(reader) : null;
    }

    /// <summary>新增用户</summary>
    public override int Insert(UserInfo entity)
    {
        const string sql = "INSERT INTO T_User (userName, userPassword, userPurview) VALUES (@userName, @userPassword, @userPurview)";
        using SqlConnection conn = new(DBConnection.GetConnectionString());
        conn.Open();
        using SqlCommand cmd = new(sql, conn);
        cmd.Parameters.AddWithValue("@userName", entity.UserName);
        cmd.Parameters.AddWithValue("@userPassword", entity.UserPassword);
        cmd.Parameters.AddWithValue("@userPurview", entity.UserPurview);
        return cmd.ExecuteNonQuery();
    }

    /// <summary>修改用户密码和权限</summary>
    public override int Update(UserInfo entity)
    {
        const string sql = "UPDATE T_User SET userPassword = @userPassword, userPurview = @userPurview WHERE userName = @userName";
        using SqlConnection conn = new(DBConnection.GetConnectionString());
        conn.Open();
        using SqlCommand cmd = new(sql, conn);
        cmd.Parameters.AddWithValue("@userPassword", entity.UserPassword);
        cmd.Parameters.AddWithValue("@userPurview", entity.UserPurview);
        cmd.Parameters.AddWithValue("@userName", entity.UserName);
        return cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// 部分更新用户信息（仅更新提供的字段，支持修改用户名）
    /// </summary>
    /// <param name="entity">新用户信息</param>
    /// <param name="originalUserName">原始用户名（用于 WHERE 条件定位记录）</param>
    /// <returns>受影响行数</returns>
    public int UpdatePartial(UserInfo entity, string originalUserName)
    {
        var setClauses = new List<string>();
        var parameters = new List<SqlParameter>();

        // 密码不为空时才更新密码字段
        if (!string.IsNullOrWhiteSpace(entity.UserPassword))
        {
            setClauses.Add("userPassword = @userPassword");
            parameters.Add(new SqlParameter("@userPassword", entity.UserPassword));
        }
        // 权限不为空时才更新权限字段
        if (!string.IsNullOrWhiteSpace(entity.UserPurview))
        {
            setClauses.Add("userPurview = @userPurview");
            parameters.Add(new SqlParameter("@userPurview", entity.UserPurview));
        }
        // 用户名变更时才更新用户名字段
        if (!string.IsNullOrWhiteSpace(entity.UserName) && entity.UserName != originalUserName)
        {
            setClauses.Add("userName = @newUserName");
            parameters.Add(new SqlParameter("@newUserName", entity.UserName));
        }

        // 无字段需要更新时直接返回
        if (setClauses.Count == 0) return 0;

        string sql = $"UPDATE T_User SET {string.Join(", ", setClauses)} WHERE userName = @originalUserName";
        parameters.Add(new SqlParameter("@originalUserName", originalUserName));

        using SqlConnection conn = new(DBConnection.GetConnectionString());
        conn.Open();
        using SqlCommand cmd = new(sql, conn);
        foreach (var p in parameters) cmd.Parameters.Add(p);
        return cmd.ExecuteNonQuery();
    }

    /// <summary>按用户名删除</summary>
    public override int Delete(string id)
    {
        const string sql = "DELETE FROM T_User WHERE userName = @userName";
        using SqlConnection conn = new(DBConnection.GetConnectionString());
        conn.Open();
        using SqlCommand cmd = new(sql, conn);
        cmd.Parameters.AddWithValue("@userName", id);
        return cmd.ExecuteNonQuery();
    }

    // ===== 以下为 User 特有查询 =====

    /// <summary>
    /// 登录验证：校验用户名、密码和身份三者匹配
    /// </summary>
    /// <param name="userName">用户名</param>
    /// <param name="passwordHash">密码 MD5 哈希值（大写十六进制）</param>
    /// <param name="purview">用户选择的身份（管理员 / 普通用户）</param>
    /// <returns>验证通过返回 UserInfo（含数据库中的真实角色），否则 null</returns>
    public UserInfo ValidateUser(string userName, string passwordHash, string purview)
    {
        const string sql = @"SELECT userName, userPassword, userPurview FROM T_User
                              WHERE userName = @userName AND userPassword = @userPassword AND userPurview = @userPurview";
        using SqlConnection conn = new(DBConnection.GetConnectionString());
        conn.Open();
        using SqlCommand cmd = new(sql, conn);
        cmd.Parameters.AddWithValue("@userName", userName);
        cmd.Parameters.AddWithValue("@userPassword", passwordHash);
        cmd.Parameters.AddWithValue("@userPurview", purview);
        using SqlDataReader reader = cmd.ExecuteReader();
        return reader.Read() ? MapReader(reader) : null;
    }

    /// <summary>SqlDataReader 映射为 UserInfo 实体</summary>
    private static UserInfo MapReader(SqlDataReader reader)
        => new()
        {
            UserName = reader.GetString(0),
            UserPassword = reader.GetString(1),
            UserPurview = reader.GetString(2)
        };
}
