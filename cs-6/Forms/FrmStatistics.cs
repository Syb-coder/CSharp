using HotelSys.BLL;
using HotelSys.Common;
using HotelSys.Models;

namespace HotelSys.Forms;

/// <summary>
/// 营业统计窗体（PRD 4.2.11 / AC-20）
/// 今日营业概览卡片 + 入住率 + 房型入住排行 + 日期范围营收
/// 使用 DataGridView 展示数据，不引入第三方图表控件
/// </summary>
public class FrmStatistics : Form
{
    private readonly StatisticsManager _mgr = new();

    private Label[] _cardValues;
    private Label _lblOccupancy, _lblRevenue, _lblCheckOutCount, _lblAvgPrice;
    private DateTimePicker _dtpFrom, _dtpTo;
    private Button _btnQuery;
    private DataGridView _dgvRanking;

    public FrmStatistics()
    {
        DoubleBuffered = true;
        InitializeUI();
        Load += (_, _) => LoadData();
    }

    private void InitializeUI()
    {
        Text = "营业统计";
        BackColor = ThemeColor.BgPage;
        Font = UiHelper.DefaultFont;

        // 使用 TableLayoutPanel 实现响应式布局，营收区自动高度
        TableLayoutPanel mainLayout = new()
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3
        };
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 110F));     // 卡片区
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));           // 营收区（自动高度）
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));      // 排行表格

        // 顶部统计卡片区
        Panel cardPanel = new()
        {
            Dock = DockStyle.Fill,
            BackColor = ThemeColor.BgPage,
            Padding = new Padding(16, 10, 16, 10)
        };
        BuildCardArea(cardPanel);

        // 中间营收查询区（AutoSize=true，高度由内容决定）
        Panel revenuePanel = new()
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            BackColor = ThemeColor.BgCard
        };
        BuildRevenueArea(revenuePanel);

        // 底部房型排行表格
        _dgvRanking = new DataGridView { Dock = DockStyle.Fill };
        UiHelper.StyleDataGridView(_dgvRanking);
        _dgvRanking.Columns.Add("Rank", "排名");
        _dgvRanking.Columns.Add("TypeName", "房型");
        _dgvRanking.Columns.Add("Count", "入住次数");

        mainLayout.Controls.Add(cardPanel, 0, 0);
        mainLayout.Controls.Add(revenuePanel, 0, 1);
        mainLayout.Controls.Add(_dgvRanking, 0, 2);
        Controls.Add(mainLayout);

        _btnQuery.Click += (_, _) => LoadRevenueData();
    }

    /// <summary>构建顶部4个统计卡片</summary>
    private void BuildCardArea(Panel container)
    {
        var cardConfigs = new (string Title, Color Color)[]
        {
            ("今日入住", ThemeColor.Primary),
            ("今日退房", ThemeColor.Success),
            ("当前在住", ThemeColor.Warning),
            ("今日营收", ThemeColor.Info)
        };

        _cardValues = new Label[4];
        TableLayoutPanel layout = new()
        {
            Dock = DockStyle.Fill,
            ColumnCount = 4,
            RowCount = 1
        };
        for (int i = 0; i < 4; i++)
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));

        for (int i = 0; i < 4; i++)
        {
            Panel card = CreateStatCard(cardConfigs[i].Title, cardConfigs[i].Color, out Label lblValue);
            _cardValues[i] = lblValue;
            layout.Controls.Add(card, i, 0);
        }
        container.Controls.Add(layout);
    }

    /// <summary>创建单个统计卡片（自绘顶部色条）</summary>
    private Panel CreateStatCard(string title, Color accentColor, out Label lblValue)
    {
        Panel card = new()
        {
            Margin = new Padding(4, 0, 4, 0),
            BackColor = ThemeColor.BgCard,
            BorderStyle = BorderStyle.FixedSingle
        };
        card.Paint += (_, e) =>
        {
            // 顶部色条
            using SolidBrush b = new(accentColor);
            e.Graphics.FillRectangle(b, 0, 0, card.Width, 4);
        };

        Label lblTitle = new()
        {
            Text = title,
            Font = UiHelper.DefaultFont,
            ForeColor = ThemeColor.TextSecondary,
            Location = new Point(12, 14),
            AutoSize = true
        };
        lblValue = new Label
        {
            Font = ThemeColor.FontTitle,
            ForeColor = ThemeColor.TextPrimary,
            Location = new Point(12, 40),
            AutoSize = true,
            Text = "0"
        };
        card.Controls.Add(lblTitle);
        card.Controls.Add(lblValue);
        return card;
    }

    /// <summary>构建营收查询区（响应式 FlowLayoutPanel，两层堆叠）</summary>
    private void BuildRevenueArea(Panel container)
    {
        container.AutoSize = true;
        container.AutoSizeMode = AutoSizeMode.GrowAndShrink;

        // --- 查询控件行（Dock=Top，不换行） ---
        FlowLayoutPanel queryFlow = new()
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            WrapContents = false,
            BackColor = ThemeColor.BgCard,
            Padding = new Padding(16, 6, 16, 0),
            Dock = DockStyle.Top
        };

        var lblFrom = UiHelper.CreateLabel("从：", 0, 0);
        _dtpFrom = UiHelper.CreateDateTimePicker(130);
        _dtpFrom.Value = DateTime.Today.AddDays(-30);

        var lblTo = UiHelper.CreateLabel("到：", 0, 0);
        _dtpTo = UiHelper.CreateDateTimePicker(130);
        _dtpTo.Value = DateTime.Today;

        _btnQuery = UiHelper.CreatePrimaryButton("查询营收");

        queryFlow.Controls.AddRange(new Control[] { lblFrom, _dtpFrom, lblTo, _dtpTo, _btnQuery });

        // --- 营收结果显示区（Dock=Top，自动换行） ---
        FlowLayoutPanel resultFlow = new()
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            WrapContents = true,
            BackColor = ThemeColor.BgCard,
            Padding = new Padding(16, 4, 16, 6),
            Dock = DockStyle.Top
        };

        _lblRevenue = new Label
        {
            AutoSize = true,
            MinimumSize = new Size(80, 28),
            Font = ThemeColor.FontBold,
            ForeColor = ThemeColor.Danger,
            TextAlign = ContentAlignment.MiddleLeft,
            Text = "0.00"
        };
        _lblCheckOutCount = new Label
        {
            AutoSize = true,
            MinimumSize = new Size(40, 28),
            Font = ThemeColor.FontBold,
            ForeColor = ThemeColor.Primary,
            TextAlign = ContentAlignment.MiddleLeft,
            Text = "0"
        };
        _lblAvgPrice = new Label
        {
            AutoSize = true,
            MinimumSize = new Size(60, 28),
            Font = ThemeColor.FontBold,
            ForeColor = ThemeColor.Info,
            TextAlign = ContentAlignment.MiddleLeft,
            Text = "0.00"
        };
        _lblOccupancy = new Label
        {
            AutoSize = true,
            MinimumSize = new Size(60, 28),
            Font = ThemeColor.FontBold,
            ForeColor = ThemeColor.Success,
            TextAlign = ContentAlignment.MiddleLeft,
            Text = "0%"
        };

        resultFlow.Controls.Add(MakeResultPair("区间营收：", _lblRevenue));
        resultFlow.Controls.Add(MakeResultPair("结账数：", _lblCheckOutCount));
        resultFlow.Controls.Add(MakeResultPair("客单价：", _lblAvgPrice));
        resultFlow.Controls.Add(MakeResultPair("当前入住率：", _lblOccupancy));

        // Dock=Top 添加顺序：先添加的在下，后添加的在上
        // 期望视觉顺序：queryFlow(上) → resultFlow(下)
        container.Controls.Add(resultFlow);
        container.Controls.Add(queryFlow);
    }

    /// <summary>创建标题+数值标签对，用于 FlowLayoutPanel 响应式布局</summary>
    private static Panel MakeResultPair(string title, Label valueLabel)
    {
        FlowLayoutPanel pair = new()
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Margin = new Padding(0, 2, 16, 2),
            BackColor = ThemeColor.BgCard,
            WrapContents = false
        };

        Label lblTitle = new()
        {
            Text = title,
            Font = UiHelper.DefaultFont,
            AutoSize = true,
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = ThemeColor.TextPrimary,
            Margin = new Padding(0, 4, 4, 0)
        };

        valueLabel.Margin = new Padding(0, 0, 0, 0);

        pair.Controls.Add(lblTitle);
        pair.Controls.Add(valueLabel);

        return pair;
    }

    /// <summary>加载今日概览 + 入住率 + 房型排行</summary>
    private void LoadData()
    {
        try
        {
            // 今日营业概览
            var overview = _mgr.GetTodayOverview();
            _cardValues[0].Text = overview.TodayCheckInCount.ToString();
            _cardValues[1].Text = overview.TodayCheckOutCount.ToString();
            _cardValues[2].Text = overview.OccupiedCount.ToString();
            _cardValues[3].Text = overview.TodayRevenue.ToString("F2");

            // 入住率
            _lblOccupancy.Text = _mgr.GetOccupancyRate().ToString("F2") + "%";

            // 房型入住排行
            var ranking = _mgr.GetRoomTypeRanking();
            _dgvRanking.Rows.Clear();
            int rank = 1;
            foreach (var item in ranking)
            {
                _dgvRanking.Rows.Add(rank, item.Key, item.Value);
                rank++;
            }

            // 加载默认日期范围营收
            LoadRevenueData();
        }
        catch (Exception ex)
        {
            UiHelper.Error("加载统计数据失败：" + ex.Message);
        }
    }

    /// <summary>加载日期范围营收数据</summary>
    private void LoadRevenueData()
    {
        try
        {
            DateTime from = _dtpFrom.Value.Date;
            DateTime to = _dtpTo.Value.Date.AddDays(1);

            decimal revenue = _mgr.GetRevenueByDateRange(from, to);
            int checkOutCount = _mgr.GetCheckOutCountByDateRange(from, to);
            decimal avgPrice = checkOutCount > 0 ? revenue / checkOutCount : 0;

            _lblRevenue.Text = revenue.ToString("F2");
            _lblCheckOutCount.Text = checkOutCount.ToString();
            _lblAvgPrice.Text = avgPrice.ToString("F2");
        }
        catch (Exception ex)
        {
            UiHelper.Error("查询营收失败：" + ex.Message);
        }
    }
}
