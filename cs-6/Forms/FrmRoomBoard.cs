using HotelSys.BLL;
using HotelSys.Common;
using HotelSys.Models;

namespace HotelSys.Forms;

/// <summary>
/// 房态看板窗体（PRD 4.2.3，本项目核心差异化特色）
/// 顶部4个统计卡片 + 按楼层分组的房间方块网格
/// 颜色编码：绿(空闲)/红(在住)/蓝(预留)/黄(维护)
/// 点击房间方块弹出快捷操作菜单
/// </summary>
public class FrmRoomBoard : Form
{
    private readonly StatisticsManager _statMgr = new();
    private readonly RoomManager _roomMgr = new();
    private readonly ReservationManager _reserveMgr = new();
    private readonly Action<string> _navigateTo;

    // 统计卡片数值标签引用（用于刷新时更新）
    private Label[] _cardValues;
    // 房间方块容器（用于刷新时重建）
    private Panel _roomArea;

    public FrmRoomBoard(Action<string> navigateTo = null)
    {
        _navigateTo = navigateTo;
        DoubleBuffered = true;
        InitializeUI();
        Load += (_, _) => LoadData();
    }

    private void InitializeUI()
    {
        Text = "房态看板";
        BackColor = ThemeColor.BgPage;
        Font = UiHelper.DefaultFont;

        BuildStatisticsArea();
        BuildRoomArea();
    }

    /// <summary>构建顶部统计卡片区域</summary>
    private void BuildStatisticsArea()
    {
        Panel statPanel = new()
        {
            Dock = DockStyle.Top,
            Height = 110,
            BackColor = ThemeColor.BgPage,
            Padding = new Padding(16, 10, 16, 10)
        };

        // 4个统计卡片配置：标题、强调色
        var cardConfigs = new (string Title, Color Color)[]
        {
            ("今日入住", ThemeColor.Primary),
            ("今日退房", ThemeColor.Success),
            ("当前在住", ThemeColor.Warning),
            ("今日营收", ThemeColor.Info)
        };

        _cardValues = new Label[4];
        TableLayoutPanel cardLayout = new()
        {
            Dock = DockStyle.Fill,
            ColumnCount = 5,  // 4卡片+1刷新按钮列
            RowCount = 1
        };
        for (int i = 0; i < 4; i++)
            cardLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 23F));
        cardLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 8F));

        for (int i = 0; i < 4; i++)
        {
            Panel card = CreateStatCard(cardConfigs[i].Title, cardConfigs[i].Color, out Label lblValue);
            _cardValues[i] = lblValue;
            cardLayout.Controls.Add(card, i, 0);
        }

        // 刷新按钮
        Button btnRefresh = UiHelper.CreateSecondaryButton("刷新", 60, 80);
        btnRefresh.Click += (_, _) => LoadData();
        cardLayout.Controls.Add(btnRefresh, 4, 0);

        statPanel.Controls.Add(cardLayout);
        Controls.Add(statPanel);
    }

    /// <summary>创建单个统计卡片（Paint自绘顶部色条+描边）</summary>
    private Panel CreateStatCard(string title, Color accentColor, out Label lblValue)
    {
        Panel card = new()
        {
            Margin = new Padding(4),
            BackColor = ThemeColor.BgCard,
            Dock = DockStyle.Fill
        };
        card.Paint += (_, e) =>
        {
            using var topBrush = new SolidBrush(accentColor);
            using var borderPen = new Pen(ThemeColor.Border, 1);
            e.Graphics.FillRectangle(topBrush, 0, 0, card.Width, 4);
            e.Graphics.DrawRectangle(borderPen, 0, 0, card.Width - 1, card.Height - 1);
        };

        Label lblTitle = new()
        {
            Text = title,
            Font = ThemeColor.FontRegular,
            ForeColor = ThemeColor.TextSecondary,
            Location = new Point(12, 14),
            AutoSize = true
        };

        lblValue = new Label()
        {
            Text = "0",
            Font = new Font("Microsoft YaHei UI", 22F, FontStyle.Bold),
            ForeColor = accentColor,
            Location = new Point(12, 36),
            AutoSize = true
        };

        card.Controls.Add(lblValue);
        card.Controls.Add(lblTitle);
        return card;
    }

    /// <summary>构建房间区域（按楼层分组的 FlowLayoutPanel）</summary>
    private void BuildRoomArea()
    {
        _roomArea = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = ThemeColor.BgPage,
            AutoScroll = true,
            Padding = new Padding(16, 8, 16, 16)
        };
        Controls.Add(_roomArea);
        // statPanel(Dock=Top) 已在 BuildStatisticsArea 中先添加，Dock 布局引擎按 Z-order 正确处理
    }

    /// <summary>加载统计数据和房间方块</summary>
    public void LoadData()
    {
        try
        {
            // 更新统计卡片
            StatisticsInfo stat = _statMgr.GetTodayOverview();
            _cardValues[0].Text = stat.TodayCheckInCount.ToString();
            _cardValues[1].Text = stat.TodayCheckOutCount.ToString();
            _cardValues[2].Text = stat.OccupiedCount.ToString();
            _cardValues[3].Text = $"¥{stat.TodayRevenue:F2}";

            // 重建房间方块
            BuildRoomBlocks();
        }
        catch (Exception ex)
        {
            UiHelper.Error($"加载房态数据失败：{ex.Message}");
        }
    }

    /// <summary>按楼层分组构建房间方块</summary>
    private void BuildRoomBlocks()
    {
        _roomArea.Controls.Clear();

        List<RoomInfo> allRooms = _roomMgr.GetAll();
        if (allRooms.Count == 0)
        {
            Label empty = new()
            {
                Text = "暂无客房数据，请先在客房管理中添加客房",
                Font = ThemeColor.FontMedium,
                ForeColor = ThemeColor.TextSecondary,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill
            };
            _roomArea.Controls.Add(empty);
            return;
        }

        // 按楼层分组
        var floorGroups = allRooms.GroupBy(r => r.Floor).OrderBy(g => g.Key);

        // 容器宽度保护：首次加载时 ClientSize 可能为 0，使用父容器宽度兜底
        int areaWidth = _roomArea.ClientSize.Width > 0
            ? _roomArea.ClientSize.Width
            : _roomArea.Width;
        int grpWidth = Math.Max(400, areaWidth - 40);

        foreach (var group in floorGroups)
        {
            // 该楼层的房间列表（按房号排序）
            var roomsInFloor = group.OrderBy(r => r.RoomNo).ToList();

            // 每个房间方块 70×50 + Margin(4*2)，实际占位约 78×58
            const int BLOCK_W = 78;
            const int BLOCK_H = 58;
            int availableWidth = grpWidth - 24;  // 减去 GroupBox 内边距和边框
            int blocksPerRow = Math.Max(1, availableWidth / BLOCK_W);
            int rowsNeeded = (roomsInFloor.Count + blocksPerRow - 1) / blocksPerRow;
            // GroupBox 高度 = 标题区(24) + 内边距(16) + 行数 × 行高 + 底部余量(8)
            int grpHeight = 24 + 16 + rowsNeeded * BLOCK_H + 8;

            GroupBox grpFloor = new()
            {
                Text = $"{group.Key}楼",
                Font = ThemeColor.FontBold,
                ForeColor = ThemeColor.TextPrimary,
                BackColor = ThemeColor.BgCard,
                Width = grpWidth,
                Height = grpHeight,
                Margin = new Padding(0, 0, 0, 8),
                Padding = new Padding(8, 8, 8, 8)
            };

            FlowLayoutPanel flow = new()
            {
                Dock = DockStyle.Fill,
                BackColor = ThemeColor.BgCard,
                WrapContents = true,
                FlowDirection = FlowDirection.LeftToRight,
                AutoScroll = false
            };

            foreach (RoomInfo room in roomsInFloor)
            {
                Panel block = CreateRoomBlock(room);
                flow.Controls.Add(block);
            }

            grpFloor.Controls.Add(flow);
            _roomArea.Controls.Add(grpFloor);
        }
    }

    /// <summary>创建单个房间方块（70×50，颜色编码房态）</summary>
    private Panel CreateRoomBlock(RoomInfo room)
    {
        Color blockColor = GetRoomColor(room.RoomStatus);

        Panel block = new()
        {
            Size = new Size(70, 50),
            BackColor = blockColor,
            Margin = new Padding(4),
            Cursor = Cursors.Hand,
            Tag = room
        };

        Label lblNo = new()
        {
            Text = room.RoomNo,
            Font = ThemeColor.FontBold,
            ForeColor = ThemeColor.TextPrimary,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter
        };

        Label lblType = new()
        {
            Text = room.TypeName,
            Font = new Font("Microsoft YaHei UI", 7F),
            ForeColor = ThemeColor.TextSecondary,
            Dock = DockStyle.Bottom,
            TextAlign = ContentAlignment.MiddleCenter,
            Height = 14
        };

        block.Controls.Add(lblNo);
        block.Controls.Add(lblType);
        block.Click += (_, _) => ShowRoomMenu(room);
        lblNo.Click += (_, _) => ShowRoomMenu(room);

        return block;
    }

    /// <summary>根据房态获取颜色编码（PRD 4.2.3）</summary>
    private static Color GetRoomColor(string status)
    {
        return status switch
        {
            RoomStatusConstants.FREE => ThemeColor.RoomFree,
            RoomStatusConstants.OCCUPIED => ThemeColor.RoomOccupied,
            RoomStatusConstants.RESERVED => ThemeColor.RoomReserved,
            RoomStatusConstants.MAINTENANCE => ThemeColor.RoomMaintenance,
            _ => Color.LightGray
        };
    }

    /// <summary>点击房间方块弹出快捷操作菜单（PRD 4.2.3）</summary>
    private void ShowRoomMenu(RoomInfo room)
    {
        ContextMenuStrip menu = new();

        switch (room.RoomStatus)
        {
            case RoomStatusConstants.FREE:
                menu.Items.Add("办理入住", null, (_, _) => NavigateTo("CheckIn"));
                menu.Items.Add("设为维护", null, (_, _) => DoSetMaintenance(room.RoomNo));
                break;
            case RoomStatusConstants.OCCUPIED:
                menu.Items.Add("办理退房", null, (_, _) => NavigateTo("CheckOut"));
                menu.Items.Add("录入消费", null, (_, _) => NavigateTo("Consume"));
                menu.Items.Add("续住", null, (_, _) => NavigateTo("CheckIn"));
                menu.Items.Add("换房", null, (_, _) => NavigateTo("CheckIn"));
                menu.Items.Add("查看详情", null, (_, _) => ShowRoomDetail(room));
                break;
            case RoomStatusConstants.RESERVED:
                menu.Items.Add("预订转入住", null, (_, _) => NavigateTo("CheckIn"));
                menu.Items.Add("取消预订", null, (_, _) => DoCancelReservation(room.RoomNo));
                break;
            case RoomStatusConstants.MAINTENANCE:
                menu.Items.Add("恢复空闲", null, (_, _) => DoRestoreFree(room.RoomNo));
                break;
        }

        menu.Show(Cursor.Position);
    }

    /// <summary>导航到指定模块（通过 FrmMain 委托回调）</summary>
    private void NavigateTo(string formKey)
    {
        _navigateTo?.Invoke(formKey);
    }

    /// <summary>设为维护</summary>
    private void DoSetMaintenance(string roomNo)
    {
        if (!UiHelper.Confirm($"确认将房号 {roomNo} 设为维护状态吗？")) return;
        try
        {
            _roomMgr.UpdateStatus(roomNo, RoomStatusConstants.MAINTENANCE);
            UiHelper.Info("已设为维护");
            LoadData();
        }
        catch (BusinessException ex) { UiHelper.Error(ex.Message); }
        catch (Exception ex) { UiHelper.Error($"操作失败：{ex.Message}"); }
    }

    /// <summary>恢复空闲</summary>
    private void DoRestoreFree(string roomNo)
    {
        if (!UiHelper.Confirm($"确认将房号 {roomNo} 恢复为空闲状态吗？")) return;
        try
        {
            _roomMgr.UpdateStatus(roomNo, RoomStatusConstants.FREE);
            UiHelper.Info("已恢复空闲");
            LoadData();
        }
        catch (BusinessException ex) { UiHelper.Error(ex.Message); }
        catch (Exception ex) { UiHelper.Error($"操作失败：{ex.Message}"); }
    }

    /// <summary>取消预订（查找该房间的待入住预订并取消）</summary>
    private void DoCancelReservation(string roomNo)
    {
        try
        {
            List<ReservationInfo> reservations = _reserveMgr.Search(null, BusinessConstants.RESERVE_WAITING, null, null);
            ReservationInfo target = reservations.FirstOrDefault(r => r.RoomNo == roomNo);
            if (target == null)
            {
                UiHelper.Warning("未找到该房间的待入住预订");
                return;
            }
            if (!UiHelper.Confirm($"确认取消房号 {roomNo} 的预订吗？")) return;
            _reserveMgr.Cancel(target.ReserveID);
            UiHelper.Info("预订已取消");
            LoadData();
        }
        catch (BusinessException ex) { UiHelper.Error(ex.Message); }
        catch (Exception ex) { UiHelper.Error($"操作失败：{ex.Message}"); }
    }

    /// <summary>显示房间详情</summary>
    private void ShowRoomDetail(RoomInfo room)
    {
        string info = $"房号：{room.RoomNo}\n" +
                      $"房型：{room.TypeName}\n" +
                      $"楼层：{room.Floor}楼\n" +
                      $"床位数：{room.BedCount}\n" +
                      $"房态：{room.RoomStatus}\n" +
                      $"单价：¥{room.Price:F2}/天";
        if (!string.IsNullOrEmpty(room.Remark))
            info += $"\n备注：{room.Remark}";
        UiHelper.Info(info, "房间详情");
    }
}
