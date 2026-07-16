using System.Data;
using Microsoft.Data.SqlClient;
using LibrarySys.Models;

namespace LibrarySys.DAL;

/// <summary>
/// 泛型数据访问基类，封装所有实体共用的 CRUD 操作
/// 子类只需实现抽象方法并添加特有查询，避免 CRUD 代码重复（cs-5 核心差异化设计）
/// </summary>
/// <typeparam name="T">实体类型</typeparam>
public abstract class BaseRepository<T> where T : class
{
    /// <summary>查询全部记录</summary>
    /// <returns>实体列表，无数据时返回空列表</returns>
    public abstract List<T> FindAll();

    /// <summary>按主键查询单条记录</summary>
    /// <param name="id">主键值（字符串形式）</param>
    /// <returns>实体对象，未找到时返回 null</returns>
    public abstract T FindById(string id);

    /// <summary>新增记录</summary>
    /// <param name="entity">实体对象</param>
    /// <returns>受影响行数（1=成功，0=失败）</returns>
    public abstract int Insert(T entity);

    /// <summary>更新记录</summary>
    /// <param name="entity">实体对象</param>
    /// <returns>受影响行数（1=成功，0=失败）</returns>
    public abstract int Update(T entity);

    /// <summary>按主键删除记录</summary>
    /// <param name="id">主键值（字符串形式）</param>
    /// <returns>受影响行数（1=成功，0=失败）</returns>
    public abstract int Delete(string id);

    // ===== 辅助方法（供子类使用） =====

    /// <summary>执行查询并返回 DataTable</summary>
    protected DataTable ExecuteQueryDataTable(string sql, CommandType commandType = CommandType.Text, params SqlParameter[] parameters)
    {
        using SqlConnection conn = new(DBConnection.GetConnectionString());
        conn.Open();
        using SqlCommand cmd = new(sql, conn) { CommandType = commandType };
        if (parameters != null)
        {
            foreach (var p in parameters) cmd.Parameters.Add(p);
        }
        using SqlDataAdapter adapter = new(cmd);
        DataTable dt = new();
        adapter.Fill(dt);
        return dt;
    }

    /// <summary>执行返回标量值的查询</summary>
    protected int ExecuteScalar(string sql, CommandType commandType = CommandType.Text, params SqlParameter[] parameters)
    {
        using SqlConnection conn = new(DBConnection.GetConnectionString());
        conn.Open();
        using SqlCommand cmd = new(sql, conn) { CommandType = commandType };
        if (parameters != null)
        {
            foreach (var p in parameters) cmd.Parameters.Add(p);
        }
        object result = cmd.ExecuteScalar();
        return result == DBNull.Value || result == null ? 0 : Convert.ToInt32(result);
    }

    /// <summary>执行非查询 SQL 语句</summary>
    protected int ExecuteNonQuery(string sql, CommandType commandType = CommandType.Text, params SqlParameter[] parameters)
    {
        using SqlConnection conn = new(DBConnection.GetConnectionString());
        conn.Open();
        using SqlCommand cmd = new(sql, conn) { CommandType = commandType };
        if (parameters != null)
        {
            foreach (var p in parameters) cmd.Parameters.Add(p);
        }
        return cmd.ExecuteNonQuery();
    }

    /// <summary>创建可空 DateTime 参数</summary>
    protected static SqlParameter DBNullParameter(string name, DateTime? value)
    {
        return new SqlParameter(name, (object?)value ?? DBNull.Value);
    }
}
