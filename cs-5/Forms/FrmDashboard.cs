using LibrarySys.BLL;
using LibrarySys.Common;
using LibrarySys.Models;

namespace LibrarySys.Forms;

/// <summary>
/// Dashboard 首页：6 个统计卡片 + 借阅热度排行 + 逾期提醒 + 预约提醒
/// 作为 MDI 子窗体嵌入 FrmMain 右侧面板
/// </summary>
public class FrmDashboard : Form
{
    private readonly Func<DashboardData> _getDashboardData;
    private readonly Func<IList<ReservationInfo>> _getWaitingReservations;
    private readonly Func<int> _getActiveReservationCount;
    private readonly Func<int> _getActiveBorrowCount;
    private TableLayoutPanel _cardPanel;
    private DataGridView _dgvRanking;
    private DataGridView _dgvOverdue;
    private DataGridView _dgvReservation;
    private Label _lblTitle;

    // 缓存边框画笔，避免 Paint 事件中反复创建
    private readonly Pen _cardBorderPen = new(ThemeColor.Border, 1);

    /// <summary>
    /// 构造 Dashboard 首页
    /// </summary>
    public FrmDashboard(
        Func<DashboardData> getDashboardData,
        Func<IList<ReservationInfo>> getWaitingReservations,
        Func<int> getActiveReservationCount,
        Func<int> getActiveBorrowCount)
    {
        _getDashboardData = getDashboardData;
        _getWaitingReservations = getWaitingReservations;
        _getActiveReservationCount = getActiveReservationCount;
        _getActiveBorrowCount = getActiveBorrowCount;
        DoubleBuffered = true;
        InitializeUI();
        Load += (_, _) => LoadData();
    }

    private void InitializeUI()
    {
        Text = "首页";
        BackColor = ThemeColor.BgPage;
        Font = UiHelper.DefaultFont;
        AutoScroll = true;

        // 标题
        _lblTitle = new Label
        {
            Text = "智慧图书馆管理系统 - 数据概览",
            Font = ThemeColor.FontTitle,
            Dock = DockStyle.Top,
            Height = 50,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(20, 0, 0, 0),
            ForeColor = ThemeColor.TextPrimary
        };

        // 6 个统计卡片面板（2行3列）
        _cardPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 180,
            ColumnCount = 3,
            RowCount = 2,
            Padding = new Padding(16, 8, 16, 8),
            BackColor = ThemeColor.BgPage
        };
        for (int i = 0; i < 3; i++)
            _cardPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
        for (int i = 0; i < 2; i++)
            _cardPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));

        // 卡片颜色统一为蓝色主调，但保留差异化强调色
        var cardConfigs = new (string Title, string Value, Color Color, string Icon)[]
        {
            ("馆藏图书", "0", ThemeColor.Primary, "\U0001F4DA"),
            ("注册读者", "0", Color.FromArgb(19, 194, 194), "\U0001F465"),
            ("当前借出", "0", ThemeColor.Warning, "\U0001F4D6"),
            ("逾期未还", "0", ThemeColor.Danger, "\u26A0"),
            ("未缴罚款", "0", ThemeColor.TextSecondary, "\U0001F4B0"),
            ("活跃预约", "0", ThemeColor.Info, "\U0001F4CB")
        };

        for (int i = 0; i < 6; i++)
        {
            var card = CreateCard(cardConfigs[i].Title, cardConfigs[i].Color, cardConfigs[i].Icon);
            card.Tag = (i, cardConfigs[i].Value);
            _cardPanel.Controls.Add(card, i % 3, i / 3);
        }

        // 借阅热度排行表格
        Label lblRanking = new Label
        {
            Text = "借阅热度排行 Top10",
            Font = ThemeColor.FontHeader,
            Dock = DockStyle.Top,
            Height = 35,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(20, 0, 0, 0),
            ForeColor = ThemeColor.TextPrimary
        };

        _dgvRanking = CreateDataGridView(new[] { "书名", "借阅次数" }, new[] { 300, 100 });
        _dgvRanking.Dock = DockStyle.Top;
        _dgvRanking.Height = 220;

        // 逾期提醒表格
        Label lblOverdue = new Label
        {
            Text = "逾期未还提醒",
            Font = ThemeColor.FontHeader,
            Dock = DockStyle.Top,
            Height = 35,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(20, 0, 0, 0),
            ForeColor = ThemeColor.Danger
        };

        _dgvOverdue = CreateDataGridView(new[] { "读者姓名", "书名", "应还日期", "逾期天数" }, new[] { 120, 200, 100, 80 });
        _dgvOverdue.Dock = DockStyle.Top;
        _dgvOverdue.Height = 180;

        // 预约提醒表格
        Label lblReservation = new Label
        {
            Text = "待取书预约提醒",
            Font = ThemeColor.FontHeader,
            Dock = DockStyle.Top,
            Height = 35,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(20, 0, 0, 0),
            ForeColor = ThemeColor.Info
        };

        _dgvReservation = CreateDataGridView(new[] { "读者姓名", "书名", "通知日期", "状态" }, new[] { 120, 200, 100, 80 });
        _dgvReservation.Dock = DockStyle.Top;
        _dgvReservation.Height = 180;

        // 导入布局（Dock=Top 规则：后添加的控件靠顶布局，所以从底到顶顺序添加）
        Controls.Add(_dgvReservation);
        Controls.Add(lblReservation);
        Controls.Add(_dgvOverdue);
        Controls.Add(lblOverdue);
        Controls.Add(_dgvRanking);
        Controls.Add(lblRanking);
        Controls.Add(_cardPanel);
        Controls.Add(_lblTitle);
    }

    /// <summary>创建单个统计卡片</summary>
    private Panel CreateCard(string title, Color accentColor, string icon)
    {
        Panel card = new Panel
        {
            Margin = new Padding(8),
            BackColor = ThemeColor.BgCard,
            BorderStyle = BorderStyle.None,
            Dock = DockStyle.Fill
        };

        // 卡片绘制：顶部 4px 强调色条 + 浅灰描边
        card.Paint += (_, e) =>
        {
            // 顶部强调色条（颜色各异，不能缓存）
            using var topBrush = new SolidBrush(accentColor);
            e.Graphics.FillRectangle(topBrush, 0, 0, card.Width, 4);
            // 浅灰描边（复用缓存的画笔）
            e.Graphics.DrawRectangle(_cardBorderPen, 0, 0, card.Width - 1, card.Height - 1);
        };

        // 左侧色条（Dock=Left，随卡片自动拉伸高度）
        Panel colorBar = new Panel
        {
            Width = 4,
            Dock = DockStyle.Left,
            BackColor = accentColor
        };

        // 内容区 Panel（用于设置内边距）
        Panel contentPanel = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(12, 12, 12, 12)
        };

        // 标题
        Label lblTitle = new Label
        {
            Text = title,
            Font = ThemeColor.FontMedium,
            Location = new Point(0, 0),
            AutoSize = true,
            ForeColor = ThemeColor.TextSecondary
        };

        // 数值
        Label lblValue = new Label
        {
            Text = "加载中...",
            Font = new Font("Microsoft YaHei UI", 24F, FontStyle.Bold),
            Location = new Point(0, 26),
            AutoSize = true,
            ForeColor = accentColor,
            Name = "lblValue"
        };

        contentPanel.Controls.Add(lblValue);
        contentPanel.Controls.Add(lblTitle);
        card.Controls.Add(contentPanel);
        card.Controls.Add(colorBar);
        return card;
    }

    /// <summary>创建统一风格的 DataGridView（复用 UiHelper.StyleDataGridView）</summary>
    private DataGridView CreateDataGridView(string[] headers, int[] widths)
    {
        var dgv = new DataGridView
        {
            Margin = new Padding(16, 0, 16, 8)
        };

        for (int i = 0; i < headers.Length; i++)
        {
            dgv.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = headers[i],
                Name = $"col{i}",
                Width = widths[i]
            });
        }

        // 复用全局统一风格
        UiHelper.StyleDataGridView(dgv);
        return dgv;
    }

    /// <summary>加载数据</summary>
    public void LoadData()
    {
        try
        {
            var data = _getDashboardData();

            // 更新 6 个统计卡片
            var values = new string[]
            {
                data.TotalBooks.ToString(),
                data.TotalReaders.ToString(),
                data.ActiveBorrows.ToString(),
                data.OverdueCount.ToString(),
                data.UnpaidFines.ToString(),
                data.ActiveReservations.ToString()
            };

            for (int i = 0; i < 6; i++)
            {
                var card = (Panel)_cardPanel.Controls[i];
                var lbl = card.Controls.Find("lblValue", true).FirstOrDefault();
                if (lbl != null)
                    lbl.Text = values[i];
            }

            // 借阅热度排行
            _dgvRanking.Rows.Clear();
            if (data.Ranking.Count == 0)
            {
                _dgvRanking.Rows.Add("暂无数据", "");
            }
            else
            {
                foreach (var item in data.Ranking)
                    _dgvRanking.Rows.Add(item.BookName, item.BorrowCount);
            }

            // 逾期提醒
            _dgvOverdue.Rows.Clear();
            if (data.OverdueReaders.Count == 0)
            {
                _dgvOverdue.Rows.Add("无逾期记录", "", "", "");
            }
            else
            {
                foreach (var item in data.OverdueReaders)
                {
                    int overdueDays = (DateTime.Today - item.DueDate).Days;
                    _dgvOverdue.Rows.Add(item.ReaderName, item.BookName,
                        item.DueDate.ToString("yyyy-MM-dd"), $"{overdueDays}天");
                }
            }

            // 预约提醒
            _dgvReservation.Rows.Clear();
            var waitingRes = _getWaitingReservations();
            if (waitingRes.Count == 0)
            {
                _dgvReservation.Rows.Add("无待取书预约", "", "", "");
            }
            else
            {
                foreach (var res in waitingRes)
                    _dgvReservation.Rows.Add(res.ReaderName, res.BookName,
                        res.NotifyDate?.ToString("yyyy-MM-dd") ?? "-", res.Status);
            }
        }
        catch
        {
            // 数据加载失败不崩溃，仅显示错误
            foreach (Control card in _cardPanel.Controls)
            {
                var lbl = card.Controls.Find("lblValue", true).FirstOrDefault();
                if (lbl != null) lbl.Text = "错误";
            }
        }
    }
}