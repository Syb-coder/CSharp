using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace LibrarySys.Forms;

/// <summary>
/// FrmStatistic 的设计器分部类
/// VS 设计器通过 RoslynCodeDom 解析 InitializeComponent 渲染预览
/// 仅支持：new 控件赋给字段 + 简单属性赋值 + Controls.Add
/// 不支持：if/try-catch/lambda/静态方法调用
/// </summary>
public partial class FrmStatistic
{
    /// <summary>
    /// 设计器入口方法
    /// 设计器模式下创建控件骨架用于预览，运行时骨架会被 BuildUI() 清除并重建
    /// </summary>
    private void InitializeComponent()
    {
        SuspendLayout();

        DoubleBuffered = true;
        Text = "统计面板";
        ClientSize = new Size(1000, 680);

        // ===== 统计卡片骨架（4 个 Label） =====
        TableLayoutPanel cardsPanel = new TableLayoutPanel();
        cardsPanel.Dock = DockStyle.Top;
        cardsPanel.Height = 170;
        cardsPanel.ColumnCount = 2;
        cardsPanel.RowCount = 2;

        _lblTotalBooks = new Label();
        _lblTotalBooks.Text = "馆藏图书总量";
        _lblTotalBooks.Dock = DockStyle.Fill;
        _lblTotalBooks.BorderStyle = BorderStyle.FixedSingle;
        cardsPanel.Controls.Add(_lblTotalBooks, 0, 0);

        _lblTotalReaders = new Label();
        _lblTotalReaders.Text = "注册读者总数";
        _lblTotalReaders.Dock = DockStyle.Fill;
        _lblTotalReaders.BorderStyle = BorderStyle.FixedSingle;
        cardsPanel.Controls.Add(_lblTotalReaders, 1, 0);

        _lblActiveBorrow = new Label();
        _lblActiveBorrow.Text = "当前借出图书数";
        _lblActiveBorrow.Dock = DockStyle.Fill;
        _lblActiveBorrow.BorderStyle = BorderStyle.FixedSingle;
        cardsPanel.Controls.Add(_lblActiveBorrow, 0, 1);

        _lblOverdue = new Label();
        _lblOverdue.Text = "当前逾期图书数";
        _lblOverdue.Dock = DockStyle.Fill;
        _lblOverdue.BorderStyle = BorderStyle.FixedSingle;
        cardsPanel.Controls.Add(_lblOverdue, 1, 1);

        Controls.Add(cardsPanel);

        // ===== 借阅热度排行骨架 =====
        Panel rankingPanel = new Panel();
        rankingPanel.Dock = DockStyle.Top;
        rankingPanel.Height = 220;

        _dgvRanking = new DataGridView();
        _dgvRanking.Dock = DockStyle.Fill;
        rankingPanel.Controls.Add(_dgvRanking);

        Controls.Add(rankingPanel);

        // ===== 逾期读者列表骨架 =====
        Panel overduePanel = new Panel();
        overduePanel.Dock = DockStyle.Fill;

        _dgvOverdue = new DataGridView();
        _dgvOverdue.Dock = DockStyle.Fill;
        overduePanel.Controls.Add(_dgvOverdue);

        Controls.Add(overduePanel);

        ResumeLayout(false);
    }
}