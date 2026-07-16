using System.Configuration;

namespace LibraryManagement.DAL;

/// <summary>
/// 数据库连接管理类，统一从 App.config 读取连接字符串
/// </summary>
public static class DBConnection
{
    // readonly + static 保证连接字符串在首次初始化后不可变，多线程环境下无需加锁即可安全访问
    private static readonly string _connectionString;

    /// <summary>
    /// 静态构造函数，首次访问时从 App.config 读取连接字符串
    /// </summary>
    static DBConnection()
    {
        // CLR 保证静态构造函数只执行一次且线程安全，天然实现了延迟初始化
        // 读取失败立即抛异常（Fail-Fast），避免后续 DAL 层每次操作才发现配置缺失
        _connectionString = ConfigurationManager.ConnectionStrings["LibraryDB"]?.ConnectionString
            ?? throw new InvalidOperationException("App.config 中未找到 LibraryDB 连接字符串配置");
    }

    /// <summary>
    /// 获取数据库连接字符串
    /// </summary>
    /// <returns>连接字符串</returns>
    public static string GetConnectionString()
    {
        return _connectionString;
    }
}
