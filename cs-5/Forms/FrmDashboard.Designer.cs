using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace LibrarySys.Forms;

/// <summary>
/// FrmDashboard 的设计器分部类
/// VS 设计器通过 RoslynCodeDom 解析 InitializeComponent 渲染预览
/// 仅支持：new 控件赋给字段 + 简单属性赋值 + Controls.Add
/// 不支持：if/try-catch/lambda/静态方法调用
/// </summary>
public partial class FrmDashboard
{
    /// <summary>
    /// 设计器入口方法
    /// 设计器模式下创建控件骨架用于预览，运行时骨架会被 BuildUI() 清除并重建
    /// </summary>
    private void InitializeComponent()
    {
        SuspendLayout();

        DoubleBuffered = true;
        Text = "首页";
        ClientSize = new Size(1000, 800);

        // ===== 标题骨架 =====
        _lblTitle = new Label();
        _lblTitle.Text = "智慧图书馆管理系统 - 数据概览";
        _lblTitle.Dock = DockStyle.Top;
        _lblTitle.Height = 50;

        Controls.Add(_lblTitle);

        // ===== 统计卡片面板骨架 =====
        _cardPanel = new TableLayoutPanel();
        _cardPanel.Dock = DockStyle.Top;
        _cardPanel.Height = 180;
        _cardPanel.ColumnCount = 3;
        _cardPanel.RowCount = 2;

        Controls.Add(_cardPanel);

        // ===== 借阅热度排行骨架 =====
        _dgvRanking = new DataGridView();
        _dgvRanking.Dock = DockStyle.Top;
        _dgvRanking.Height = 220;

        Controls.Add(_dgvRanking);

        // ===== 逾期提醒骨架 =====
        _dgvOverdue = new DataGridView();
        _dgvOverdue.Dock = DockStyle.Top;
        _dgvOverdue.Height = 180;

        Controls.Add(_dgvOverdue);

        // ===== 预约提醒骨架 =====
        _dgvReservation = new DataGridView();
        _dgvReservation.Dock = DockStyle.Top;
        _dgvReservation.Height = 180;

        Controls.Add(_dgvReservation);

        ResumeLayout(false);
    }
}