using System.Configuration;

namespace LibraryManagement.DAL;

/// <summary>
/// 数据库连接管理类，统一从 App.config 读取连接字符串
/// </summary>
public static class DBConnection
{
    /// <summary>缓存连接字符串，避免重复读取配置</summary>
    private static readonly string _connectionString;

    /// <summary>
    /// 静态构造函数，首次访问时从 App.config 读取连接字符串
    /// </summary>
    static DBConnection()
    {
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
