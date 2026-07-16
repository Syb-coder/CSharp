using HotelSys.BLL;
using HotelSys.Common;
using HotelSys.Models;

namespace HotelSys.Forms;

/// <summary>
/// 退房结算窗体（PRD 4.2.10 / F-18 / F-19 / AC-18 / AC-19，核心业务）
/// 参考值自动预填 + 所有金额字段可手动修改 + 应收应退实时计算
/// 最终保存到数据库的数字以操作员手动填写/确认为准
/// </summary>
public class FrmCheckOut : Form
{
    private readonly CheckInManager _checkInMgr = new();
    private readonly CheckOutManager _checkOutMgr = new();
    private readonly UserInfo _currentUser;

    private GroupBox _queryBox, _editBox;
    private List<UiHelper.FieldDef> _editFields;
    private DataGridView _dgv;
    private TextBox _txtQueryRoom, _txtQueryName;
    private Button _btnQuery, _btnConfirm, _btnClear;

    // 只读信息字段
    private Label _lblRoomNo, _lblCustomer, _lblCheckInTime, _lblDeposit, _lblBalance;
    // 可编辑金额字段
    private TextBox _txtActualDays, _txtRoomCharge, _txtOtherCharge, _txtConsumeAmount, _txtTotalAmount;

    private CheckInInfo _currentCheckIn;  // 当前选中的入住单
    private bool _isLoading;

    public FrmCheckOut(UserInfo currentUser) : this()
    {
        _currentUser = currentUser;
    }

    public FrmCheckOut()
    {
        DoubleBuffered = true;
        InitializeUI();
        Load += (_, _) => LoadData();
    }

    private void InitializeUI()
    {
        Text = "退房结算";
        BackColor = ThemeColor.BgPage;
        Font = UiHelper.DefaultFont;

        _queryBox = UiHelper.CreateStyledGroupBox("查询在住记录");
        _queryBox.Dock = DockStyle.Top;
        BuildQueryArea(_queryBox);

        _editBox = UiHelper.CreateStyledGroupBox("结算退房（参考值已预填，所有金额可手动修改）");
        _editBox.Dock = DockStyle.Bottom;
        BuildEditArea(_editBox);

        _dgv = new DataGridView { Dock = DockStyle.Fill };
        UiHelper.StyleDataGridView(_dgv);
        BuildGridColumns();

        Controls.Add(_dgv);
        Controls.Add(_editBox);
        Controls.Add(_queryBox);

        _dgv.SelectionChanged += (_, _) => OnRowSelected();
        _btnQuery.Click += (_, _) => LoadData();
        _btnClear.Click += (_, _) => ClearSettlement();
        _btnConfirm.Click += (_, _) => DoCheckOut();

        // 住宿费/其他费用/附加消费变动时自动重算总金额，总金额变动时重算应收应退
        _txtRoomCharge.TextChanged += (_, _) => RecalculateTotal();
        _txtOtherCharge.TextChanged += (_, _) => RecalculateTotal();
        _txtConsumeAmount.TextChanged += (_, _) => RecalculateTotal();
        _txtTotalAmount.TextChanged += (_, _) => UpdateBalance();

        Resize += (_, _) => AdjustLayoutHeights();
        Shown += (_, _) => AdjustLayoutHeights();
    }

    private void AdjustLayoutHeights()
    {
        _queryBox.Height = UiHelper.CalcQueryHeightByGroupBox(_queryBox);
        LayoutEditArea(_editBox);
    }

    private void BuildQueryArea(GroupBox container)
    {
        _txtQueryRoom = UiHelper.CreateTextBox(100);
        _txtQueryName = UiHelper.CreateTextBox(120);
        _btnQuery = UiHelper.CreatePrimaryButton("查询");

        var pairs = new List<UiHelper.QueryPair>
        {
            new("房号：", _txtQueryRoom),
            new("客户：", _txtQueryName),
        };
        FlowLayoutPanel flow = UiHelper.BuildQueryPanel(pairs, _btnQuery);
        container.Controls.Add(flow);
    }

    private void BuildEditArea(GroupBox container)
    {
        // 只读信息标签
        _lblRoomNo = CreateInfoLabel();
        _lblCustomer = CreateInfoLabel();
        _lblCheckInTime = CreateInfoLabel();
        _lblDeposit = CreateInfoLabel();

        // 可编辑金额输入框
        _txtActualDays = UiHelper.CreateTextBox(80);
        _txtRoomCharge = UiHelper.CreateTextBox(100);
        _txtOtherCharge = UiHelper.CreateTextBox(100);
        _txtConsumeAmount = UiHelper.CreateTextBox(100);
        _txtTotalAmount = UiHelper.CreateTextBox(100);

        _lblBalance = new Label
        {
            Size = new Size(150, 28),
            Font = ThemeColor.FontBold,
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = ThemeColor.Danger,
            BorderStyle = BorderStyle.FixedSingle
        };

        _editFields = new List<UiHelper.FieldDef>
        {
            new("房号：", _lblRoomNo),
            new("客户：", _lblCustomer),
            new("入住时间：", _lblCheckInTime),
            new("押金：", _lblDeposit),
            new("实际天数：", _txtActualDays),
            new("住宿费：", _txtRoomCharge),
            new("其他费用：", _txtOtherCharge),
            new("附加消费：", _txtConsumeAmount),
            new("总金额：", _txtTotalAmount),
            new("应收/应退：", _lblBalance),
        };

        _btnConfirm = UiHelper.CreatePrimaryButton("确认结账", 120, 38);
        _btnClear = UiHelper.CreateSecondaryButton("清空", 85, 38);

        LayoutEditArea(container);
    }

    /// <summary>按当前容器宽度重新布局编辑区（控件 + 按钮）</summary>
    private void LayoutEditArea(GroupBox container)
    {
        container.Controls.Clear();
        int containerW = container.Width - 16;
        int btnY = UiHelper.LayoutFields(container.Controls, _editFields, containerW, 0, 20, 30);

        // 确认结账靠左，清空按钮靠右（使用 LayoutButtonsWithReturn 统一布局）
        UiHelper.LayoutButtonsWithReturn(
            container.Controls,
            new[] { _btnConfirm },
            _btnClear, 20, btnY + 10, containerW);

        container.Height = UiHelper.CalcEditHeight(_editFields.Count, UiHelper.CalcPairsPerRow(containerW));
    }

    /// <summary>创建只读信息标签</summary>
    private Label CreateInfoLabel()
    {
        return new Label
        {
            Size = new Size(180, 28),
            Font = UiHelper.DefaultFont,
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = ThemeColor.TextSecondary,
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = ThemeColor.BgAltRow
        };
    }

    private void BuildGridColumns()
    {
        _dgv.Columns.Add("CheckInID", "入住单号");
        _dgv.Columns.Add("RoomNo", "房号");
        _dgv.Columns.Add("CustomerName", "客户");
        _dgv.Columns.Add("TypeName", "房型");
        _dgv.Columns.Add("Price", "单价");
        _dgv.Columns.Add("CheckInTime", "入住时间");
        _dgv.Columns.Add("ExpectCheckOut", "预计退房");
        _dgv.Columns.Add("Deposit", "押金");
    }

    private void LoadData()
    {
        try
        {
            _isLoading = true;
            string roomNo = _txtQueryRoom?.Text.Trim();
            string customerName = _txtQueryName?.Text.Trim();

            // 只显示在住记录（退房仅针对在住）
            List<CheckInInfo> list = _checkInMgr.Search(roomNo, customerName, null, null, BusinessConstants.CHECKIN_OCCUPIED);
            _dgv.Rows.Clear();
            foreach (var item in list)
            {
                _dgv.Rows.Add(item.CheckInID, item.RoomNo, item.CustomerName ?? "",
                    item.TypeName ?? "", item.Price, item.CheckInTime.ToString("yyyy-MM-dd HH:mm"),
                    item.ExpectCheckOut.ToString("yyyy-MM-dd"), item.Deposit);
            }
            ClearSettlement();
        }
        catch (Exception ex)
        {
            UiHelper.Error("查询失败：" + ex.Message);
        }
        finally
        {
            _isLoading = false;
        }
    }

    /// <summary>选中在住记录时，自动计算参考值并预填到各输入框（PRD 4.2.10）</summary>
    private void OnRowSelected()
    {
        if (_isLoading) return;
        if (_dgv.CurrentRow == null || _dgv.CurrentRow.Index < 0) return;

        try
        {
            int checkInID = int.Parse(_dgv.CurrentRow.Cells["CheckInID"].Value?.ToString() ?? "0");

            // 先清空旧引用，避免异常后残留旧值导致后续操作误用
            _currentCheckIn = null;
            ClearSettlement();

            // 调用 BLL 计算参考值（实际天数、住宿费、消费、总费用）
            _currentCheckIn = _checkOutMgr.CalculateReference(checkInID);

            // 预填只读信息
            _lblRoomNo.Text = _currentCheckIn.RoomNo;
            _lblCustomer.Text = _currentCheckIn.CustomerName ?? "";
            _lblCheckInTime.Text = _currentCheckIn.CheckInTime.ToString("yyyy-MM-dd HH:mm");
            _lblDeposit.Text = _currentCheckIn.Deposit.ToString("F2");

            // 预填可编辑金额（参考值，操作员可修改）
            _txtActualDays.Text = _currentCheckIn.ActualDays?.ToString() ?? "1";
            _txtRoomCharge.Text = _currentCheckIn.RoomCharge?.ToString("F2") ?? "0";
            _txtOtherCharge.Text = _currentCheckIn.OtherCharge?.ToString("F2") ?? "0";
            _txtConsumeAmount.Text = _currentCheckIn.ConsumeAmount?.ToString("F2") ?? "0";
            _txtTotalAmount.Text = _currentCheckIn.TotalAmount?.ToString("F2") ?? "0";

            UpdateBalance();
        }
        catch (BusinessException ex) { UiHelper.Warning(ex.Message); }
        catch (Exception ex) { UiHelper.Error("加载结算信息失败：" + ex.Message); }
    }

    /// <summary>
    /// 自动重算总金额 = 住宿费 + 其他费用 + 附加消费
    /// 仅在操作员修改三个子项时触发，总金额手动修改时不反向覆盖
    /// </summary>
    private void RecalculateTotal()
    {
        if (_isLoading) return;
        if (_currentCheckIn == null) return;

        decimal roomCharge = decimal.TryParse(_txtRoomCharge.Text.Trim(), out decimal rc) ? rc : 0;
        decimal otherCharge = decimal.TryParse(_txtOtherCharge.Text.Trim(), out decimal oc) ? oc : 0;
        decimal consumeAmount = decimal.TryParse(_txtConsumeAmount.Text.Trim(), out decimal ca) ? ca : 0;
        // 临时标记加载中，避免 UpdateBalance 被重复触发
        _isLoading = true;
        _txtTotalAmount.Text = (roomCharge + otherCharge + consumeAmount).ToString("F2");
        _isLoading = false;
        UpdateBalance();
    }

    /// <summary>实时计算应收应退 = 总金额 - 押金（PRD 4.2.10）</summary>
    private void UpdateBalance()
    {
        if (_currentCheckIn == null) return;
        if (!decimal.TryParse(_txtTotalAmount.Text.Trim(), out decimal total)) total = 0;
        decimal balance = _checkOutMgr.CalculateBalance(total, _currentCheckIn.Deposit);
        if (balance >= 0)
            _lblBalance.Text = $"应收 {balance:F2} 元";
        else
            _lblBalance.Text = $"应退 {-balance:F2} 元";
    }

    private void ClearSettlement()
    {
        _isLoading = true;
        _currentCheckIn = null;
        _lblRoomNo.Text = "";
        _lblCustomer.Text = "";
        _lblCheckInTime.Text = "";
        _lblDeposit.Text = "";
        _txtActualDays.Text = "";
        _txtRoomCharge.Text = "";
        _txtOtherCharge.Text = "";
        _txtConsumeAmount.Text = "";
        _txtTotalAmount.Text = "";
        _lblBalance.Text = "";
        _isLoading = false;
    }

    /// <summary>确认结账（PRD 4.2.10 / AC-19）</summary>
    private void DoCheckOut()
    {
        try
        {
            if (_currentCheckIn == null)
            {
                UiHelper.Warning("请先选择要退房的在住记录");
                return;
            }
            // 构建包含操作员手动填写费用字段的实体
            CheckInInfo info = new()
            {
                CheckInID = _currentCheckIn.CheckInID,
                ActualDays = int.TryParse(_txtActualDays.Text.Trim(), out int d) ? d : 1,
                RoomCharge = decimal.TryParse(_txtRoomCharge.Text.Trim(), out decimal rc) ? rc : 0,
                OtherCharge = decimal.TryParse(_txtOtherCharge.Text.Trim(), out decimal oc) ? oc : 0,
                ConsumeAmount = decimal.TryParse(_txtConsumeAmount.Text.Trim(), out decimal ca) ? ca : 0,
                TotalAmount = decimal.TryParse(_txtTotalAmount.Text.Trim(), out decimal ta) ? ta : 0,
            };

            // 计算应收应退用于确认提示
            decimal balance = _checkOutMgr.CalculateBalance(info.TotalAmount ?? 0, _currentCheckIn.Deposit);
            string balanceDesc = balance >= 0 ? $"应收 {balance:F2} 元" : $"应退 {-balance:F2} 元";

            if (!UiHelper.Confirm($"确认结账吗？\n总金额：{info.TotalAmount:F2}\n押金：{_currentCheckIn.Deposit:F2}\n{balanceDesc}"))
                return;

            _checkInMgr.CheckOut(info);
            UiHelper.Info($"退房结账成功！{balanceDesc}");
            LoadData();
        }
        catch (BusinessException ex) { UiHelper.Warning(ex.Message); }
        catch (Exception ex) { UiHelper.Error("结账失败：" + ex.Message); }
    }
}
