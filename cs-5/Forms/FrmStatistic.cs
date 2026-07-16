using LibrarySys.BLL;
using LibrarySys.Common;
using LibrarySys.Models;

namespace LibrarySys.Forms;

/// <summary>
/// 借阅统计面板窗体
/// 布局：顶部四个统计卡片（Dock=Top）+ 中部借阅热度 Top10（Dock=Top）+ 底部逾期读者列表（Dock=Fill）+ 按钮行（Dock=Bottom）
/// 全 Dock 布局，零硬编码 Y 坐标，DPI 安全
/// </summary>
public class FrmStatistic : Form
{
    private readonly StatisticBiz _biz = new();

    private Label _lblTotalBooks, _lblTotalReaders, _lblActiveBorrow, _lblOverdue;
    private DataGridView _dgvRanking;
    private DataGridView _dgvOverdue;

    public FrmStatistic()
    {
        InitializeUI();
        Load += (_, _) => LoadData();
    }

    private void InitializeUI()
    {
        DoubleBuffered = true;
        Text = "统计面板";
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(1000, 680);
        MinimumSize = new Size(800, 550);
        Font = UiHelper.DefaultFont;
        FormBorderStyle = FormBorderStyle.Sizable;
        MaximizeBox = true;
        MinimizeBox = true;

        // =====================================================================
        // 布局顺序（从底到顶）：按钮行(Bottom) → 逾期列表(Fill) → 排行(Top) → 卡片(Top)
        // =====================================================================

        // 1. 按钮行（最底部）
        FlowLayoutPanel btnFlow = new()
        {
            Dock = DockStyle.Bottom, Height = 45,
            FlowDirection = FlowDirection.LeftToRight, WrapContents = false,
            Padding = new Padding(10, 5, 10, 5)
        };
        Button btnRefresh = UiHelper.CreateButton("刷新");
        Button btnReturn = UiHelper.CreateButton("返回");
        Panel spacer = new() { Width = 9999, Height = 1 };
        btnRefresh.Click += (_, _) => LoadData();
        btnReturn.Click += (_, _) => Close();
        btnFlow.Controls.Add(btnRefresh);
        btnFlow.Controls.Add(spacer);
        btnFlow.Controls.Add(btnReturn);
        btnFlow.Resize += (s, e) =>
            spacer.Width = Math.Max(0, btnFlow.ClientSize.Width - btnRefresh.Width - btnReturn.Width - 60);
        Controls.Add(btnFlow);

        // 2. 逾期读者列表（Fill 填充剩余空间）
        Panel overduePanel = new() { Dock = DockStyle.Fill, Padding = new Padding(10, 5, 10, 5) };
        Label lblOverdue = new() { Text = "逾期读者列表", AutoSize = true, Dock = DockStyle.Top, Font = new Font(UiHelper.DefaultFont.FontFamily, 10F, FontStyle.Bold), Padding = new Padding(0, 3, 0, 5) };
        _dgvOverdue = new DataGridView
        {
            Dock = DockStyle.Fill,
            AllowUserToAddRows = false, ReadOnly = true,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false
        };
        UiHelper.StyleDataGridView(_dgvOverdue);
        // 保留 AllCells 自适应宽度（StyleDataGridView 默认设为 Fill）
        _dgvOverdue.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;
        overduePanel.Controls.Add(lblOverdue);
        overduePanel.Controls.Add(_dgvOverdue);
        Controls.Add(overduePanel);

        // 3. 借阅热度排行 Top10（Top，固定高度）
        Panel rankingPanel = new() { Dock = DockStyle.Top, Height = 220, Padding = new Padding(10, 5, 10, 5) };
        Label lblRanking = new() { Text = "借阅热度排行 Top10", AutoSize = true, Dock = DockStyle.Top, Font = new Font(UiHelper.DefaultFont.FontFamily, 10F, FontStyle.Bold), Padding = new Padding(0, 3, 0, 5) };
        _dgvRanking = new DataGridView
        {
            Dock = DockStyle.Fill,
            AllowUserToAddRows = false, ReadOnly = true,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false
        };
        UiHelper.StyleDataGridView(_dgvRanking);
        rankingPanel.Controls.Add(lblRanking);
        rankingPanel.Controls.Add(_dgvRanking);
        Controls.Add(rankingPanel);

        // 4. 顶部统计卡片（Top，2×2 网格）
        TableLayoutPanel cardsPanel = new()
        {
            Dock = DockStyle.Top, Height = 170,
            ColumnCount = 2, RowCount = 2,
            Padding = new Padding(10, 10, 10, 5)
        };
        cardsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        cardsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        cardsPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
        cardsPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));

        _lblTotalBooks = CreateStatCard("馆藏图书总量");
        _lblTotalReaders = CreateStatCard("注册读者总数");
        _lblActiveBorrow = CreateStatCard("当前借出图书数");
        _lblOverdue = CreateStatCard("当前逾期图书数");

        cardsPanel.Controls.Add(_lblTotalBooks, 0, 0);
        cardsPanel.Controls.Add(_lblTotalReaders, 1, 0);
        cardsPanel.Controls.Add(_lblActiveBorrow, 0, 1);
        cardsPanel.Controls.Add(_lblOverdue, 1, 1);
        Controls.Add(cardsPanel);
    }

    /// <summary>创建统计卡片（Dock=Fill 以填满 TableLayoutPanel 单元格）</summary>
    private static Label CreateStatCard(string title)
    {
        return new Label
        {
            Dock = DockStyle.Fill,
            Text = $"  {title}\n  -",
            Font = new Font("Microsoft YaHei UI", 11F),
            TextAlign = ContentAlignment.MiddleLeft,
            BackColor = Color.FromArgb(245, 247, 250),
            BorderStyle = BorderStyle.FixedSingle,
            Margin = new Padding(5)
        };
    }

    /// <summary>加载统计数据</summary>
    private void LoadData()
    {
        try
        {
            _lblTotalBooks.Text = $"  馆藏图书总量\n  {_biz.GetTotalBooks()}";
            _lblTotalReaders.Text = $"  注册读者总数\n  {_biz.GetTotalReaders()}";
            _lblActiveBorrow.Text = $"  当前借出图书数\n  {_biz.GetActiveBorrowCount()}";
            _lblOverdue.Text = $"  当前逾期图书数\n  {_biz.GetOverdueCount()}";

            _dgvRanking.DataSource = _biz.GetBorrowRankingTop10();
            _dgvOverdue.DataSource = _biz.GetOverdueReaders();
        }
        catch (BusinessException ex) { UiHelper.Error(ex.Message); }
        catch (Exception ex) { UiHelper.Error($"加载统计数据失败：{ex.Message}"); }
    }
}
