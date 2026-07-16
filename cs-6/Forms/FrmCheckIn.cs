using HotelSys.BLL;
using HotelSys.Common;
using HotelSys.Models;

namespace HotelSys.Forms;

/// <summary>
/// 入住管理窗体（PRD 4.2.8 / F-12 / F-13 / F-14 / F-15 / AC-13~AC-15）
/// 入住登记 + 续住 + 换房，核心业务窗体
/// 所有操作在 BLL 层事务内完成，保证房态与入住记录原子性
/// </summary>
public class FrmCheckIn : Form
{
    private readonly CheckInManager _mgr = new();
    private readonly CustomerManager _custMgr = new();
    private readonly RoomManager _roomMgr = new();
    private readonly UserInfo _currentUser;

    private GroupBox _queryBox, _editBox;
    private List<UiHelper.FieldDef> _editFields;
    private Button _btnRefresh;
    private DataGridView _dgv;
    // 入住登记字段
    private ComboBox _cboCustomer, _cboRoom, _cboNewRoom, _cboQueryStatus;
    private TextBox _txtDays, _txtDeposit, _txtExtraDays, _txtQueryRoom, _txtQueryName;
    private Button _btnCheckIn, _btnExtend, _btnChangeRoom, _btnQuery, _btnClear;
    private bool _isLoading;

    public FrmCheckIn(UserInfo currentUser) : this()
    {
        _currentUser = currentUser;
    }

    public FrmCheckIn()
    {
        DoubleBuffered = true;
        InitializeUI();
        Load += (_, _) => { LoadCombos(); LoadData(); };
    }

    private void InitializeUI()
    {
        Text = "入住管理";
        BackColor = ThemeColor.BgPage;
        Font = UiHelper.DefaultFont;

        _queryBox = UiHelper.CreateStyledGroupBox("查询");
        _queryBox.Dock = DockStyle.Top;
        BuildQueryArea(_queryBox);

        _editBox = UiHelper.CreateStyledGroupBox("入住登记 / 续住 / 换房");
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
        _btnClear.Click += (_, _) => ClearEdit();
        _btnCheckIn.Click += (_, _) => DoCheckIn();
        _btnExtend.Click += (_, _) => DoExtend();
        _btnChangeRoom.Click += (_, _) => DoChangeRoom();

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
        _txtQueryRoom = UiHelper.CreateTextBox(80);
        _txtQueryName = UiHelper.CreateTextBox(100);
        _cboQueryStatus = UiHelper.CreateComboBox(100);
        _btnQuery = UiHelper.CreatePrimaryButton("查询");
        _btnClear = UiHelper.CreateSecondaryButton("清空");

        var pairs = new List<UiHelper.QueryPair>
        {
            new("房号：", _txtQueryRoom),
            new("客户：", _txtQueryName),
            new("状态：", _cboQueryStatus),
        };
        FlowLayoutPanel flow = UiHelper.BuildQueryPanel(pairs, _btnQuery, _btnClear);
        flow.Dock = DockStyle.Fill;
        container.Controls.Add(flow);

        _cboQueryStatus.Items.Add("全部");
        _cboQueryStatus.Items.Add(BusinessConstants.CHECKIN_OCCUPIED);
        _cboQueryStatus.Items.Add(BusinessConstants.CHECKIN_CHECKEDOUT);
        _cboQueryStatus.SelectedIndex = 0;
    }

    private void BuildEditArea(GroupBox container)
    {
        _cboCustomer = UiHelper.CreateComboBox(180);
        _cboRoom = UiHelper.CreateComboBox(100);
        _txtDays = UiHelper.CreateTextBox(60);
        _txtDeposit = UiHelper.CreateTextBox(80);
        _txtExtraDays = UiHelper.CreateTextBox(60);
        _cboNewRoom = UiHelper.CreateComboBox(100);

        _editFields = new List<UiHelper.FieldDef>
        {
            new("客户：", _cboCustomer),
            new("入住房号：", _cboRoom),
            new("入住天数：", _txtDays),
            new("押金：", _txtDeposit),
            new("续住天数：", _txtExtraDays),
            new("换至房号：", _cboNewRoom),
        };

        _btnCheckIn = UiHelper.CreatePrimaryButton("入住登记");
        _btnExtend = UiHelper.CreatePrimaryButton("续住");
        _btnChangeRoom = UiHelper.CreatePrimaryButton("换房");
        _btnRefresh = UiHelper.CreateSecondaryButton("刷新");
        _btnRefresh.Click += (_, _) => LoadData();

        LayoutEditArea(container);
    }

    private void LayoutEditArea(GroupBox container)
    {
        container.Controls.Clear();
        int containerW = container.Width - 16;
        int btnY = UiHelper.LayoutFields(container.Controls, _editFields, containerW, 0, 20, 30);

        UiHelper.LayoutButtonsWithReturn(
            container.Controls,
            new[] { _btnCheckIn, _btnExtend, _btnChangeRoom },
            _btnRefresh, 20, btnY + 10, containerW);

        container.Height = UiHelper.CalcEditHeight(_editFields.Count, UiHelper.CalcPairsPerRow(containerW));
    }

    private void BuildGridColumns()
    {
        _dgv.Columns.Add("CheckInID", "入住单号");
        _dgv.Columns.Add("RoomNo", "房号");
        _dgv.Columns.Add("CustomerName", "客户");
        _dgv.Columns.Add("TypeName", "房型");
        _dgv.Columns.Add("CheckInTime", "入住时间");
        _dgv.Columns.Add("ExpectCheckOut", "预计退房");
        _dgv.Columns.Add("Deposit", "押金");
        _dgv.Columns.Add("Status", "状态");
    }

    /// <summary>加载客户和空闲房间下拉框</summary>
    private void LoadCombos()
    {
        var customers = _custMgr.GetAll();
        _cboCustomer.Items.Clear();
        foreach (var c in customers)
            _cboCustomer.Items.Add(c);
        _cboCustomer.DisplayMember = "CustomerName";

        var freeRooms = _roomMgr.GetFreeRooms();
        _cboRoom.Items.Clear();
        _cboNewRoom.Items.Clear();
        foreach (var r in freeRooms)
        {
            _cboRoom.Items.Add(r);
            _cboNewRoom.Items.Add(r);
        }
        _cboRoom.DisplayMember = "RoomNo";
        _cboNewRoom.DisplayMember = "RoomNo";
    }

    private void LoadData()
    {
        try
        {
            _isLoading = true;
            string roomNo = _txtQueryRoom?.Text.Trim();
            string customerName = _txtQueryName?.Text.Trim();
            string status = _cboQueryStatus?.SelectedIndex > 0 ? _cboQueryStatus.SelectedItem.ToString() : null;

            List<CheckInInfo> list = _mgr.Search(roomNo, customerName, null, null, status);
            _dgv.Rows.Clear();
            foreach (var item in list)
            {
                _dgv.Rows.Add(item.CheckInID, item.RoomNo, item.CustomerName ?? "",
                    item.TypeName ?? "", item.CheckInTime.ToString("yyyy-MM-dd HH:mm"),
                    item.ExpectCheckOut.ToString("yyyy-MM-dd"), item.Deposit, item.Status);
            }
            ClearEdit();
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

    private void OnRowSelected()
    {
        if (_isLoading) return;
        if (_dgv.CurrentRow == null || _dgv.CurrentRow.Index < 0) return;
        // 选中行用于续住/换房操作，基于选中行的 CheckInID
    }

    private void ClearEdit()
    {
        _isLoading = true;
        _cboCustomer.SelectedIndex = _cboCustomer.Items.Count > 0 ? 0 : -1;
        _cboRoom.SelectedIndex = _cboRoom.Items.Count > 0 ? 0 : -1;
        _cboNewRoom.SelectedIndex = _cboNewRoom.Items.Count > 0 ? 0 : -1;
        _txtDays.Text = "1";
        _txtDeposit.Text = "0";
        _txtExtraDays.Text = "1";
        _isLoading = false;
    }

    /// <summary>获取选中行的入住单号</summary>
    private int GetSelectedCheckInID()
    {
        if (_dgv.CurrentRow == null || _dgv.CurrentRow.Index < 0)
        {
            UiHelper.Warning("请先选择一条入住记录");
            return -1;
        }
        if (int.TryParse(_dgv.CurrentRow.Cells["CheckInID"].Value?.ToString(), out int id))
            return id;
        UiHelper.Warning("无法获取入住单号");
        return -1;
    }

    /// <summary>入住登记（PRD F-12 / AC-13）</summary>
    private void DoCheckIn()
    {
        try
        {
            if (_cboCustomer.SelectedItem is not CustomerInfo customer)
            {
                UiHelper.Warning("请选择客户");
                return;
            }
            if (_cboRoom.SelectedItem is not RoomInfo room)
            {
                UiHelper.Warning("请选择房间");
                return;
            }
            if (!int.TryParse(_txtDays.Text.Trim(), out int days) || days < 1)
            {
                UiHelper.Warning("入住天数必须为正整数");
                return;
            }
            if (!decimal.TryParse(_txtDeposit.Text.Trim(), out decimal deposit) || deposit < 0)
            {
                UiHelper.Warning("押金必须为非负数");
                return;
            }

            CheckInInfo checkIn = _mgr.CheckIn(room.RoomNo, customer.CustomerID, days, deposit);
            UiHelper.Info($"入住成功！入住单号：{checkIn.CheckInID}，预计退房：{checkIn.ExpectCheckOut:yyyy-MM-dd}");
            LoadCombos();  // 刷新空闲房间列表
            LoadData();
        }
        catch (BusinessException ex) { UiHelper.Warning(ex.Message); }
        catch (Exception ex) { UiHelper.Error("入住失败：" + ex.Message); }
    }

    /// <summary>续住办理（PRD F-14 / AC-14）</summary>
    private void DoExtend()
    {
        try
        {
            int checkInID = GetSelectedCheckInID();
            if (checkInID < 0) return;

            string status = _dgv.CurrentRow.Cells["Status"].Value?.ToString();
            if (status != BusinessConstants.CHECKIN_OCCUPIED)
            {
                UiHelper.Warning($"仅在住记录可续住，当前状态：{status}");
                return;
            }
            if (!int.TryParse(_txtExtraDays.Text.Trim(), out int extraDays) || extraDays < 1)
            {
                UiHelper.Warning("续住天数必须为正整数");
                return;
            }
            if (!UiHelper.Confirm($"确认为入住单 {checkInID} 续住 {extraDays} 天吗？")) return;
            _mgr.ExtendStay(checkInID, extraDays);
            UiHelper.Info($"续住成功，已延长 {extraDays} 天");
            LoadData();
        }
        catch (BusinessException ex) { UiHelper.Warning(ex.Message); }
        catch (Exception ex) { UiHelper.Error("续住失败：" + ex.Message); }
    }

    /// <summary>换房办理（PRD F-15 / AC-15）</summary>
    private void DoChangeRoom()
    {
        try
        {
            int checkInID = GetSelectedCheckInID();
            if (checkInID < 0) return;

            string status = _dgv.CurrentRow.Cells["Status"].Value?.ToString();
            if (status != BusinessConstants.CHECKIN_OCCUPIED)
            {
                UiHelper.Warning($"仅在住记录可换房，当前状态：{status}");
                return;
            }
            if (_cboNewRoom.SelectedItem is not RoomInfo newRoom)
            {
                UiHelper.Warning("请选择新房间");
                return;
            }
            string currentRoom = _dgv.CurrentRow.Cells["RoomNo"].Value?.ToString();
            if (currentRoom == newRoom.RoomNo)
            {
                UiHelper.Warning("新房间与原房间相同");
                return;
            }
            if (!UiHelper.Confirm($"确认将入住单 {checkInID} 从 {currentRoom} 换至 {newRoom.RoomNo} 吗？")) return;
            _mgr.ChangeRoom(checkInID, newRoom.RoomNo);
            UiHelper.Info("换房成功");
            LoadCombos();
            LoadData();
        }
        catch (BusinessException ex) { UiHelper.Warning(ex.Message); }
        catch (Exception ex) { UiHelper.Error("换房失败：" + ex.Message); }
    }
}
