using System.Configuration;
using System.Data;
using Microsoft.Data.SqlClient;

namespace CampusStore.DAL;

/// <summary>
/// 数据访问层基类：封装存储过程调用与通用数据库操作
/// </summary>
/// <remarks>
/// 与 cs-2/cs-3 差异化：
///   - cs-2 内联参数化 SQL，BLL 层管理事务
///   - cs-3 内联参数化 SQL，BaseDao 封装 SqlDataAdapter
///   - cs-4 所有 CRUD 通过存储过程调用（CommandType = StoredProcedure），
///     DAL 层不拼接任何 SQL 语句，业务规则下沉至数据库端（触发器+存储过程）
/// </remarks>
public abstract class BaseRepository
{
    /// <summary>数据库连接字符串（从 App.config 读取，子类只读访问）</summary>
    protected static readonly string ConnectionString;

    /// <summary>
    /// 静态构造函数，首次访问时从 App.config 读取连接字符串
    /// </summary>
    static BaseRepository()
    {
        // CLR 保证静态构造函数只执行一次且线程安全，天然实现延迟初始化
        // 读取失败立即抛异常（Fail-Fast），避免后续 Repository 层每次操作才发现配置缺失
        ConnectionString = ConfigurationManager.ConnectionStrings["CampusStore"]?.ConnectionString
            ?? throw new InvalidOperationException("App.config 中未找到 CampusStore 连接字符串配置");
    }

    /// <summary>
    /// 执行存储过程查询，返回 DataTable 结果集
    /// </summary>
    /// <param name="spName">存储过程名称</param>
    /// <param name="parameters">参数列表</param>
    /// <returns>查询结果集</returns>
    protected static DataTable ExecuteSpDataTable(string spName, params SqlParameter[] parameters)
    {
        DataTable table = new();
        using SqlDataAdapter adapter = new(spName, ConnectionString)
        {
            SelectCommand = { CommandType = CommandType.StoredProcedure }
        };
        if (parameters != null && parameters.Length > 0)
        {
            adapter.SelectCommand.Parameters.AddRange(parameters);
        }
        // SqlDataAdapter 内部自动管理连接的打开与关闭
        adapter.Fill(table);
        return table;
    }

    /// <summary>
    /// 执行存储过程非查询操作（INSERT/UPDATE/DELETE），返回受影响行数
    /// </summary>
    /// <param name="spName">存储过程名称</param>
    /// <param name="parameters">参数列表</param>
    /// <returns>受影响行数</returns>
    protected static int ExecuteSpNonQuery(string spName, params SqlParameter[] parameters)
    {
        using SqlConnection conn = new(ConnectionString);
        using SqlCommand cmd = new(spName, conn) { CommandType = CommandType.StoredProcedure };
        if (parameters != null && parameters.Length > 0)
        {
            cmd.Parameters.AddRange(parameters);
        }
        conn.Open();
        return cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// 执行存储过程，返回结果集首行首列的值
    /// </summary>
    /// <param name="spName">存储过程名称</param>
    /// <param name="parameters">参数列表</param>
    /// <returns>首行首列值，无结果返回 null</returns>
    protected static object ExecuteSpScalar(string spName, params SqlParameter[] parameters)
    {
        using SqlConnection conn = new(ConnectionString);
        using SqlCommand cmd = new(spName, conn) { CommandType = CommandType.StoredProcedure };
        if (parameters != null && parameters.Length > 0)
        {
            cmd.Parameters.AddRange(parameters);
        }
        conn.Open();
        return cmd.ExecuteScalar();
    }

    /// <summary>
    /// 执行存储过程并读取结果集，通过回调映射为实体列表
    /// </summary>
    /// <typeparam name="T">实体类型</typeparam>
    /// <param name="spName">存储过程名称</param>
    /// <param name="mapper">DataReader 到实体的映射函数</param>
    /// <param name="parameters">参数列表</param>
    /// <returns>实体列表</returns>
    /// <remarks>使用 DataReader 而非 DataTable，减少大结果集的内存占用</remarks>
    protected static List<T> ExecuteSpList<T>(string spName, Func<SqlDataReader, T> mapper, params SqlParameter[] parameters)
    {
        List<T> list = new();
        using SqlConnection conn = new(ConnectionString);
        using SqlCommand cmd = new(spName, conn) { CommandType = CommandType.StoredProcedure };
        if (parameters != null && parameters.Length > 0)
        {
            cmd.Parameters.AddRange(parameters);
        }
        conn.Open();
        using SqlDataReader reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(mapper(reader));
        }
        return list;
    }

    /// <summary>
    /// 执行视图查询（SELECT * FROM view WHERE ...），返回 DataTable
    /// </summary>
    /// <param name="viewName">视图名称</param>
    /// <param name="whereClause">WHERE 子句（不含 WHERE 关键字），可为空</param>
    /// <param name="parameters">参数列表</param>
    /// <returns>查询结果集</returns>
    /// <remarks>
    /// 视图查询使用 Text 命令类型，但 WHERE 子句仍采用参数化，防止 SQL 注入。
    /// 此方法仅用于读取视图，不涉及写操作。
    /// </remarks>
    protected static DataTable ExecuteViewDataTable(string viewName, string whereClause = null, params SqlParameter[] parameters)
    {
        DataTable table = new();
        string sql = string.IsNullOrWhiteSpace(whereClause)
            ? $"SELECT * FROM {viewName}"
            : $"SELECT * FROM {viewName} WHERE {whereClause}";
        using SqlDataAdapter adapter = new(sql, ConnectionString);
        if (parameters != null && parameters.Length > 0)
        {
            adapter.SelectCommand.Parameters.AddRange(parameters);
        }
        adapter.Fill(table);
        return table;
    }

    /// <summary>
    /// 创建参数对象，简化子类调用
    /// </summary>
    /// <param name="name">参数名（含 @ 前缀）</param>
    /// <param name="value">参数值</param>
    /// <returns>SqlParameter 实例</returns>
    protected static SqlParameter MakeParam(string name, object value)
    {
        // DBNull.Value 显式传递，避免 SqlParameter 默认行为将 null 当作字符串处理
        return new SqlParameter(name, value ?? DBNull.Value);
    }

    /// <summary>
    /// 创建新的数据库连接，供需要显式管理事务的子类使用
    /// </summary>
    /// <returns>未打开的 SqlConnection 实例</returns>
    protected static SqlConnection CreateConnection()
    {
        return new SqlConnection(ConnectionString);
    }
}
