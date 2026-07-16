using System.Data;
using CampusStore.Models;
using Microsoft.Data.SqlClient;

namespace CampusStore.DAL;

/// <summary>
/// 用户数据访问类：调用 sp_User_* 存储过程
/// </summary>
public class UserRepository : BaseRepository
{
    /// <summary>
    /// 验证用户登录
    /// </summary>
    /// <param name="loginName">登录名</param>
    /// <param name="passwordHash">密码哈希值</param>
    /// <param name="role">角色（管理员/店员）</param>
    /// <returns>匹配返回 UserInfo，否则返回 null</returns>
    public UserInfo Validate(string loginName, string passwordHash, string role)
    {
        return ExecuteSpList("sp_User_Validate", MapReader, new[]
        {
            MakeParam("@loginName", loginName),
            MakeParam("@password", passwordHash),
            MakeParam("@role", role)
        }).FirstOrDefault();
    }

    /// <summary>
    /// 添加用户
    /// </summary>
    /// <returns>0=用户名已存在，>0=新用户ID</returns>
    public int Add(string loginName, string passwordHash, string role)
    {
        object result = ExecuteSpScalar("sp_User_Add",
            MakeParam("@loginName", loginName),
            MakeParam("@password", passwordHash),
            MakeParam("@role", role));
        return Convert.ToInt32(result);
    }

    /// <summary>
    /// 修改用户（密码 + 角色）
    /// </summary>
    public int Update(int userID, string passwordHash, string role)
    {
        object result = ExecuteSpScalar("sp_User_Update",
            MakeParam("@userID", userID),
            MakeParam("@password", passwordHash),
            MakeParam("@role", role));
        return Convert.ToInt32(result);
    }

    /// <summary>
    /// 删除用户
    /// </summary>
    public int Delete(int userID)
    {
        object result = ExecuteSpScalar("sp_User_Delete", MakeParam("@userID", userID));
        return Convert.ToInt32(result);
    }

    /// <summary>
    /// 查询全部用户（直接读表，无复杂 JOIN）
    /// </summary>
    public List<UserInfo> GetAll()
    {
        DataTable dt = ExecuteViewDataTable("(SELECT userID, loginName, password, role FROM tbl_User) AS t");
        return dt.AsEnumerable().Select(MapRow).ToList();
    }

    /// <summary>
    /// 按用户编号查询
    /// </summary>
    public UserInfo GetByID(int userID)
    {
        DataTable dt = ExecuteViewDataTable("(SELECT userID, loginName, password, role FROM tbl_User) AS t",
            "userID = @userID", MakeParam("@userID", userID));
        return dt.AsEnumerable().Select(MapRow).FirstOrDefault();
    }

    /// <summary>
    /// DataReader 到实体映射
    /// </summary>
    private static UserInfo MapReader(SqlDataReader r) => new()
    {
        UserID = r.GetInt32(r.GetOrdinal("userID")),
        LoginName = r.GetString(r.GetOrdinal("loginName")),
        Role = r.GetString(r.GetOrdinal("role"))
    };

    /// <summary>
    /// DataRow 到实体映射
    /// </summary>
    private static UserInfo MapRow(DataRow row) => new()
    {
        UserID = row.Field<int>("userID"),
        LoginName = row.Field<string>("loginName") ?? string.Empty,
        Password = row.Field<string>("password") ?? string.Empty,
        Role = row.Field<string>("role") ?? string.Empty
    };
}
