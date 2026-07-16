using System.Configuration;
using System.Data;
using Microsoft.Data.SqlClient;

namespace CampusMart.DAL;

/// <summary>
/// 数据访问层基类，封装数据库连接与通用操作
/// </summary>
/// <remarks>
/// 与 cs-2 差异化：cs-2 使用静态类 DBConnection 仅提供连接字符串，各 DAL 自行管理连接；
/// cs-3 采用实例基类 BaseDao，封装 SqlDataAdapter + DataTable 模式，子类无需管理连接生命周期。
/// </remarks>
public abstract class BaseDao
{
    /// <summary>数据库连接字符串（从 App.config 读取，子类只读访问）</summary>
    protected static readonly string ConnectionString;

    /// <summary>
    /// 静态构造函数，首次访问时从 App.config 读取连接字符串
    /// </summary>
    static BaseDao()
    {
        // CLR 保证静态构造函数只执行一次且线程安全，天然实现延迟初始化
        // 读取失败立即抛异常（Fail-Fast），避免后续 DAO 层每次操作才发现配置缺失
        ConnectionString = ConfigurationManager.ConnectionStrings["CampusMart"]?.ConnectionString
            ?? throw new InvalidOperationException("App.config 中未找到 CampusMart 连接字符串配置");
    }

    /// <summary>
    /// 执行查询，返回 DataTable 结果集
    /// </summary>
    /// <param name="sql">SQL 查询语句</param>
    /// <param name="parameters">参数化查询参数</param>
    /// <returns>查询结果集</returns>
    /// <remarks>使用 SqlDataAdapter + DataTable 模式，与 cs-2 的 SqlDataReader 差异化</remarks>
    protected static DataTable ExecuteDataTable(string sql, params SqlParameter[] parameters)
    {
        DataTable table = new();
        // SqlDataAdapter 内部自动管理连接的打开与关闭，无需显式 conn.Open()
        using SqlDataAdapter adapter = new(sql, ConnectionString);
        if (parameters != null && parameters.Length > 0)
        {
            adapter.SelectCommand.Parameters.AddRange(parameters);
        }
        adapter.Fill(table);
        return table;
    }

    /// <summary>
    /// 执行非查询语句（INSERT/UPDATE/DELETE），返回受影响行数
    /// </summary>
    /// <param name="sql">SQL 语句</param>
    /// <param name="parameters">参数化查询参数</param>
    /// <returns>受影响行数</returns>
    protected static int ExecuteNonQuery(string sql, params SqlParameter[] parameters)
    {
        using SqlConnection conn = new(ConnectionString);
        using SqlCommand cmd = new(sql, conn);
        if (parameters != null && parameters.Length > 0)
        {
            cmd.Parameters.AddRange(parameters);
        }
        conn.Open();
        return cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// 执行查询，返回结果集首行首列的值
    /// </summary>
    /// <param name="sql">SQL 查询语句</param>
    /// <param name="parameters">参数化查询参数</param>
    /// <returns>首行首列值，无结果返回 null</returns>
    protected static object ExecuteScalar(string sql, params SqlParameter[] parameters)
    {
        using SqlConnection conn = new(ConnectionString);
        using SqlCommand cmd = new(sql, conn);
        if (parameters != null && parameters.Length > 0)
        {
            cmd.Parameters.AddRange(parameters);
        }
        conn.Open();
        return cmd.ExecuteScalar();
    }

    /// <summary>
    /// 创建新的数据库连接，供需要显式管理事务的子类使用
    /// </summary>
    /// <returns>未打开的 SqlConnection 实例</returns>
    /// <remarks>OrderDao 创建订单时需要显式管理事务，通过此方法获取独立连接</remarks>
    protected static SqlConnection CreateConnection()
    {
        return new SqlConnection(ConnectionString);
    }
}
