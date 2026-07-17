using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace LibrarySys.Forms;

/// <summary>
/// FrmMain 的设计器分部类
/// VS 设计器通过 RoslynCodeDom 解析 InitializeComponent 渲染预览
/// 仅支持：new 控件赋给字段 + 简单属性赋值 + Controls.Add
/// 不支持：if/try-catch/lambda/静态方法调用
/// </summary>
public partial class FrmMain
{
    /// <summary>
    /// 设计器入口方法
    /// 设计器模式下创建控件骨架用于预览，运行时骨架会被 BuildUI() 清除并重建
    /// </summary>
    private void InitializeComponent()
    {
        SuspendLayout();

        DoubleBuffered = true;
        Text = "智慧图书馆管理系统";
        ClientSize = new Size(1200, 800);

        // ===== MenuStrip 骨架 =====
        _menuStrip = new MenuStrip();
        _menuStrip.Dock = DockStyle.Top;

        Controls.Add(_menuStrip);

        // ===== 左侧导航面板骨架 =====
        Panel leftPanel = new Panel();
        leftPanel.Dock = DockStyle.Left;
        leftPanel.Width = 200;

        _treeView = new TreeView();
        _treeView.Dock = DockStyle.Fill;
        leftPanel.Controls.Add(_treeView);

        Controls.Add(leftPanel);

        // ===== 右侧内容区域骨架 =====
        _rightPanel = new Panel();
        _rightPanel.Dock = DockStyle.Fill;

        Controls.Add(_rightPanel);

        ResumeLayout(false);
    }
}