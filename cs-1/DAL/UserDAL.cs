using System.Data;
using Microsoft.Data.SqlClient;
using LibraryManagement.Models;

namespace LibraryManagement.DAL;

/// <summary>
/// 用户数据访问类，对应 tbl_User 表的 CRUD 操作
/// </summary>
public class UserDAL
{
    /// <summary>
    /// 根据用户名查询用户信息
    /// </summary>
    /// <param name="userName">用户名</param>
    /// <returns>用户实体，未找到返回 null</returns>
    public User GetUserByName(string userName)
    {
        const string sql = "SELECT userName, userPassword, userPurview FROM tbl_User WHERE userName = @userName";
        using SqlConnection conn = new(DBConnection.GetConnectionString());
        using SqlCommand cmd = new(sql, conn);
        cmd.Parameters.Add(new SqlParameter("@userName", SqlDbType.NVarChar, 16) { Value = userName });
        conn.Open();
        using SqlDataReader reader = cmd.ExecuteReader();
        if (reader.Read())
        {
            return MapReaderToUser(reader);
        }
        return null;
    }

    /// <summary>
    /// 查询全部用户列表
    /// </summary>
    /// <returns>用户列表</returns>
    public List<User> GetAllUsers()
    {
        const string sql = "SELECT userName, userPassword, userPurview FROM tbl_User ORDER BY userName";
        List<User> list = new();
        using SqlConnection conn = new(DBConnection.GetConnectionString());
        using SqlCommand cmd = new(sql, conn);
        conn.Open();
        using SqlDataReader reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(MapReaderToUser(reader));
        }
        return list;
    }

    /// <summary>
    /// 新增用户
    /// </summary>
    /// <param name="user">用户实体</param>
    /// <returns>成功返回 true，用户名已存在返回 false</returns>
    public bool InsertUser(User user)
    {
        const string sql = "INSERT INTO tbl_User (userName, userPassword, userPurview) VALUES (@userName, @userPassword, @userPurview)";
        using SqlConnection conn = new(DBConnection.GetConnectionString());
        using SqlCommand cmd = new(sql, conn);
        cmd.Parameters.Add(new SqlParameter("@userName", SqlDbType.NVarChar, 16) { Value = user.UserName });
        cmd.Parameters.Add(new SqlParameter("@userPassword", SqlDbType.NVarChar, 64) { Value = user.UserPassword });
        cmd.Parameters.Add(new SqlParameter("@userPurview", SqlDbType.NVarChar, 16) { Value = user.UserPurview });
        conn.Open();
        try
        {
            return cmd.ExecuteNonQuery() > 0;
        }
        catch (SqlException ex) when (ex.Number == 2627) // 主键冲突
        {
            return false;
        }
    }

    /// <summary>
    /// 修改用户密码和权限
    /// </summary>
    /// <param name="user">用户实体</param>
    /// <returns>成功返回 true</returns>
    public bool UpdateUser(User user)
    {
        const string sql = "UPDATE tbl_User SET userPassword = @userPassword, userPurview = @userPurview WHERE userName = @userName";
        using SqlConnection conn = new(DBConnection.GetConnectionString());
        using SqlCommand cmd = new(sql, conn);
        cmd.Parameters.Add(new SqlParameter("@userPassword", SqlDbType.NVarChar, 64) { Value = user.UserPassword });
        cmd.Parameters.Add(new SqlParameter("@userPurview", SqlDbType.NVarChar, 16) { Value = user.UserPurview });
        cmd.Parameters.Add(new SqlParameter("@userName", SqlDbType.NVarChar, 16) { Value = user.UserName });
        conn.Open();
        return cmd.ExecuteNonQuery() > 0;
    }

    /// <summary>
    /// 修改用户密码
    /// </summary>
    /// <param name="userName">用户名</param>
    /// <param name="newPasswordHash">新密码哈希值</param>
    /// <returns>成功返回 true</returns>
    public bool UpdatePassword(string userName, string newPasswordHash)
    {
        const string sql = "UPDATE tbl_User SET userPassword = @userPassword WHERE userName = @userName";
        using SqlConnection conn = new(DBConnection.GetConnectionString());
        using SqlCommand cmd = new(sql, conn);
        cmd.Parameters.Add(new SqlParameter("@userPassword", SqlDbType.NVarChar, 64) { Value = newPasswordHash });
        cmd.Parameters.Add(new SqlParameter("@userName", SqlDbType.NVarChar, 16) { Value = userName });
        conn.Open();
        return cmd.ExecuteNonQuery() > 0;
    }

    /// <summary>
    /// 删除用户
    /// </summary>
    /// <param name="userName">用户名</param>
    /// <returns>成功返回 true</returns>
    public bool DeleteUser(string userName)
    {
        const string sql = "DELETE FROM tbl_User WHERE userName = @userName";
        using SqlConnection conn = new(DBConnection.GetConnectionString());
        using SqlCommand cmd = new(sql, conn);
        cmd.Parameters.Add(new SqlParameter("@userName", SqlDbType.NVarChar, 16) { Value = userName });
        conn.Open();
        return cmd.ExecuteNonQuery() > 0;
    }

    /// <summary>
    /// 将 SqlDataReader 映射为 User 实体
    /// </summary>
    private static User MapReaderToUser(SqlDataReader reader)
    {
        return new User
        {
            UserName = reader["userName"].ToString(),
            UserPassword = reader["userPassword"].ToString(),
            UserPurview = reader["userPurview"].ToString()
        };
    }
}
