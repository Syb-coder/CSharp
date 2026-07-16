using System.Data;
using CampusMart.Models;
using Microsoft.Data.SqlClient;

namespace CampusMart.DAL;

/// <summary>
/// 用户数据访问类，对应 tbl_User 表的 CRUD 操作
/// </summary>
/// <remarks>
/// 与 cs-2 的 UserDAL 差异化命名（UserDao），并采用 SqlDataAdapter + DataTable 模式。
/// </remarks>
public class UserDao : BaseDao
{
    /// <summary>
    /// 根据用户名查询用户信息
    /// </summary>
    /// <param name="loginName">登录用户名</param>
    /// <returns>用户实体，未找到返回 null</returns>
    public UserInfo GetUserByLoginName(string loginName)
    {
        const string sql = "SELECT userID, loginName, password, realName, role FROM tbl_User WHERE loginName = @loginName";
        SqlParameter p = new("@loginName", SqlDbType.NVarChar, 20) { Value = loginName };
        DataTable table = ExecuteDataTable(sql, p);
        if (table.Rows.Count == 0) return null;
        return MapRowToUser(table.Rows[0]);
    }

    /// <summary>
    /// 查询全部用户列表
    /// </summary>
    /// <returns>用户列表</returns>
    public List<UserInfo> GetAllUsers()
    {
        const string sql = "SELECT userID, loginName, password, realName, role FROM tbl_User ORDER BY userID";
        DataTable table = ExecuteDataTable(sql);
        List<UserInfo> list = new(table.Rows.Count);
        foreach (DataRow row in table.Rows)
        {
            list.Add(MapRowToUser(row));
        }
        return list;
    }

    /// <summary>
    /// 新增用户
    /// </summary>
    /// <param name="user">用户实体</param>
    /// <returns>成功返回 true，用户名已存在返回 false</returns>
    public bool InsertUser(UserInfo user)
    {
        const string sql = "INSERT INTO tbl_User (loginName, password, realName, role) VALUES (@loginName, @password, @realName, @role)";
        SqlParameter[] ps =
        {
            new("@loginName", SqlDbType.NVarChar, 20) { Value = user.LoginName },
            new("@password", SqlDbType.NVarChar, 64) { Value = user.Password },
            new("@realName", SqlDbType.NVarChar, 20) { Value = (object)user.RealName ?? DBNull.Value },
            new("@role", SqlDbType.NVarChar, 10) { Value = user.Role }
        };
        try
        {
            return ExecuteNonQuery(sql, ps) > 0;
        }
        catch (SqlException ex) when (ex.Number == 2627 || ex.Number == 2601) // 唯一约束冲突
        {
            return false;
        }
    }

    /// <summary>
    /// 修改用户密码和角色
    /// </summary>
    /// <param name="user">用户实体</param>
    /// <returns>成功返回 true</returns>
    public bool UpdateUser(UserInfo user)
    {
        const string sql = "UPDATE tbl_User SET password = @password, realName = @realName, role = @role WHERE userID = @userID";
        SqlParameter[] ps =
        {
            new("@password", SqlDbType.NVarChar, 64) { Value = user.Password },
            new("@realName", SqlDbType.NVarChar, 20) { Value = (object)user.RealName ?? DBNull.Value },
            new("@role", SqlDbType.NVarChar, 10) { Value = user.Role },
            new("@userID", SqlDbType.Int) { Value = user.UserID }
        };
        return ExecuteNonQuery(sql, ps) > 0;
    }

    /// <summary>
    /// 修改用户密码
    /// </summary>
    /// <param name="userID">用户编号</param>
    /// <param name="newPasswordHash">新密码哈希值</param>
    /// <returns>成功返回 true</returns>
    public bool UpdatePassword(int userID, string newPasswordHash)
    {
        const string sql = "UPDATE tbl_User SET password = @password WHERE userID = @userID";
        SqlParameter[] ps =
        {
            new("@password", SqlDbType.NVarChar, 64) { Value = newPasswordHash },
            new("@userID", SqlDbType.Int) { Value = userID }
        };
        return ExecuteNonQuery(sql, ps) > 0;
    }

    /// <summary>
    /// 删除用户
    /// </summary>
    /// <param name="userID">用户编号</param>
    /// <returns>成功返回 true</returns>
    public bool DeleteUser(int userID)
    {
        const string sql = "DELETE FROM tbl_User WHERE userID = @userID";
        SqlParameter p = new("@userID", SqlDbType.Int) { Value = userID };
        return ExecuteNonQuery(sql, p) > 0;
    }

    /// <summary>
    /// 将 DataRow 映射为 UserInfo 实体
    /// </summary>
    private static UserInfo MapRowToUser(DataRow row)
    {
        return new UserInfo
        {
            UserID = Convert.ToInt32(row["userID"]),
            LoginName = row["loginName"].ToString(),
            Password = row["password"].ToString(),
            RealName = row["realName"] == DBNull.Value ? null : row["realName"].ToString(),
            Role = row["role"].ToString()
        };
    }
}
