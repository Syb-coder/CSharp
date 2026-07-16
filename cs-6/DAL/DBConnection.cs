using System.Configuration;

namespace HotelSys.DAL;

/// <summary>
/// 数据库连接管理类，统一管理连接字符串
/// </summary>
public static class DBConnection
{
    private static string _connectionString;
    private static readonly object _lock = new();

    static DBConnection()
    {
        _connectionString = ConfigurationManager.ConnectionStrings["HotelDB"]?.ConnectionString
            ?? throw new InvalidOperationException("App.config 中未找到 HotelDB 连接字符串配置");
    }

    /// <summary>
    /// 获取数据库连接字符串
    /// </summary>
    public static string GetConnectionString() => _connectionString;

    /// <summary>
    /// 更新连接字符串（由 DBInitializer 在启动时自动探测实例后调用）
    /// </summary>
    /// <param name="connStr">探测到的正确连接字符串</param>
    internal static void SetConnectionString(string connStr)
    {
        lock (_lock)
        {
            _connectionString = connStr;
        }
    }
}
