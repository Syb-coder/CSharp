using HotelSys.DAL;
using HotelSys.Forms;

namespace HotelSys;

/// <summary>
/// 应用程序入口类
/// </summary>
internal static class Program
{
    /// <summary>
    /// 应用程序主入口点
    /// </summary>
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();

        // 程序启动时自动检测数据库完整性，缺失则建库/建表/补默认用户
        // 保证部署到新电脑时无需手动执行 SQL 脚本也能登录（沿用 cs-5 零配置部署方案）
        if (!DBInitializer.EnsureDatabaseReady(out string dbError))
        {
            MessageBox.Show(
                $"数据库初始化失败，请确认 SQL Server 服务已启动。\n\n错误详情：{dbError}\n\n" +
                "可尝试双击运行 install.bat 进行一键修复。",
                "数据库连接失败",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            return;
        }

        Application.Run(new FrmLogin());
    }
}
