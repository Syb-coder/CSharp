using CampusStore.Forms;

namespace CampusStore;

/// <summary>
/// 应用程序入口类
/// </summary>
internal static class Program
{
    /// <summary>
    /// 应用程序主入口点
    /// </summary>
    // STAThread 指示主线程以单线程单元（Single-Threaded Apartment）模式运行
    // WinForms 的 UI 控件（如剪贴板、文件对话框、OLE 拖放）依赖 COM 组件，要求调用线程为 STA
    // 若缺少此特性，部分系统对话框会抛出 InvalidOperationException
    [STAThread]
    static void Main()
    {
        // 初始化应用程序配置（高 DPI 支持、默认字体等），.NET 6+ 的自动生成方法
        ApplicationConfiguration.Initialize();
        // 从 FrmLogin 启动：系统需先验证用户身份再决定加载哪些功能模块
        // 登录成功后由 FrmLogin 自行打开主窗体并关闭自身，保证未授权用户无法绕过登录
        Application.Run(new FrmLogin());
    }
}
