using HotelSys.BLL;
using HotelSys.Common;
using HotelSys.Models;

namespace HotelSys.Forms;

/// <summary>
/// 预订管理窗体（PRD 4.2.7 / F-09 / F-10 / F-11 / AC-10 / AC-11 / AC-12）
/// 预订登记、取消预订、预订转入住
/// 简化方案：预订即预留指定房间，预订成功后房态变为"预留"
/// </summary>
public class FrmReservation : Form
{
    private readonly ReservationManager _mgr = new();
    private readonly CustomerManager _custMgr = new();
    private readonly RoomManager _roomMgr = new();
    private readonly UserInfo _currentUser;

    private GroupBox _queryBox, _editBox;
    private List<UiHelper.FieldDef> _editFields;
    private Button _btnRefresh;
    private DataGridView _dgv;
    private ComboBox _cboCustomer, _cboRoom, _cboQueryStatus;
    private DateTimePicker _dtpExpectCheckIn, _dtpQueryFrom, _dtpQueryTo;
    private TextBox _txtExpectDays, _txtContactPhone, _txtRemark, _txtDeposit, _txtQueryName;
    private Button _btnReserve, _btnCancel, _btnConvert, _btnQuery, _btnClear;
    private bool _isLoading;

    public FrmReservation(UserInfo currentUser) : this()
    {
        _currentUser = currentUser;
    }

    public FrmReservation()
    {
        DoubleBuffered = true;
        InitializeUI();
        Load += (_, _) => { LoadCombos(); LoadData(); };
    }

    private void InitializeUI()
    {
        Text = "预订管理";
        BackColor = ThemeColor.BgPage;
        Font = UiHelper.DefaultFont;

        // 创建查询区控件（供 BuildQueryPanel 使用）
        _txtQueryName = UiHelper.CreateTextBox(100);
        _cboQueryStatus = UiHelper.CreateComboBox(100);
        _dtpQueryFrom = UiHelper.CreateDateTimePicker(130);
        _dtpQueryTo = UiHelper.CreateDateTimePicker(130);
        _btnQuery = UiHelper.CreatePrimaryButton("查询");
        _btnClear = UiHelper.CreateSecondaryButton("清空");

        _queryBox = UiHelper.CreateStyledGroupBox("查询");
        _queryBox.Dock = DockStyle.Top;
        BuildQueryArea(_queryBox);

        _editBox = UiHelper.CreateStyledGroupBox("预订登记 / 操作");
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
        _btnReserve.Click += (_, _) => DoReserve();
        _btnCancel.Click += (_, _) => DoCancel();
        _btnConvert.Click += (_, _) => DoConvert();

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
        var pairs = new List<UiHelper.QueryPair>
        {
            new("客户姓名：", _txtQueryName),
            new("状态：", _cboQueryStatus),
            new("从：", _dtpQueryFrom),
            new("到：", _dtpQueryTo),
        };
        FlowLayoutPanel flow = UiHelper.BuildQueryPanel(pairs, _btnQuery, _btnClear);
        flow.Dock = DockStyle.Fill;
        container.Controls.Add(flow);
    }

    private void BuildEditArea(GroupBox container)
    {
        _cboCustomer = UiHelper.CreateComboBox(200);
        _cboRoom = UiHelper.CreateComboBox(120);
        _dtpExpectCheckIn = UiHelper.CreateDateTimePicker(150);
        _txtExpectDays = UiHelper.CreateTextBox(60);
        _txtContactPhone = UiHelper.CreateTextBox(150);
        _txtRemark = UiHelper.CreateTextBox(200);
        _txtDeposit = UiHelper.CreateTextBox(100);

        _editFields = new List<UiHelper.FieldDef>
        {
            new("客户：", _cboCustomer),
            new("房号：", _cboRoom),
            new("预计入住：", _dtpExpectCheckIn),
            new("预计天数：", _txtExpectDays),
            new("联系电话：", _txtContactPhone),
            new("备注：", _txtRemark),
            new("押金(转入住)：", _txtDeposit),
        };

        _btnReserve = UiHelper.CreatePrimaryButton("预订登记");
        _btnCancel = UiHelper.CreateDangerButton("取消预订");
        _btnConvert = UiHelper.CreatePrimaryButton("转入住");
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
            new[] { _btnReserve, _btnCancel, _btnConvert },
            _btnRefresh, 20, btnY + 10, containerW);

        container.Height = UiHelper.CalcEditHeight(_editFields.Count, UiHelper.CalcPairsPerRow(containerW));
    }

    private void BuildGridColumns()
    {
        _dgv.Columns.Add("ReserveID", "预订编号");
        _dgv.Columns.Add("CustomerName", "客户");
        _dgv.Columns.Add("RoomNo", "房号");
        _dgv.Columns.Add("TypeName", "房型");
        _dgv.Columns.Add("ExpectCheckIn", "预计入住");
        _dgv.Columns.Add("ExpectDays", "天数");
        _dgv.Columns.Add("ContactPhone", "联系电话");
        _dgv.Columns.Add("Status", "状态");
        _dgv.Columns.Add("ReserveTime", "预订时间");
    }

    /// <summary>加载客户和空闲房间下拉框，同时初始化查询状态下拉框</summary>
    private void LoadCombos()
    {
        // 初始化查询状态下拉框（仅首次，避免重复调用时重置用户选择）
        if (_cboQueryStatus.Items.Count == 0)
        {
            _cboQueryStatus.Items.Add("全部");
            _cboQueryStatus.Items.Add(BusinessConstants.RESERVE_WAITING);
            _cboQueryStatus.Items.Add(BusinessConstants.RESERVE_CHECKEDIN);
            _cboQueryStatus.Items.Add(BusinessConstants.RESERVE_CANCELLED);
            _cboQueryStatus.Items.Add(BusinessConstants.RESERVE_EXPIRED);
            _cboQueryStatus.SelectedIndex = 0;
        }

        var customers = _custMgr.GetAll();
        _cboCustomer.Items.Clear();
        foreach (var c in customers)
            _cboCustomer.Items.Add(c);
        _cboCustomer.DisplayMember = "CustomerName";

        var freeRooms = _roomMgr.GetFreeRooms();
        _cboRoom.Items.Clear();
        foreach (var r in freeRooms)
            _cboRoom.Items.Add(r);
        _cboRoom.DisplayMember = "RoomNo";
    }

    private void LoadData()
    {
        try
        {
            _isLoading = true;
            string customerName = _txtQueryName?.Text.Trim();
            string status = _cboQueryStatus?.SelectedIndex > 0 ? _cboQueryStatus.SelectedItem.ToString() : null;
            DateTime? from = _dtpQueryFrom?.Value.Date;
            DateTime? to = _dtpQueryTo?.Value.Date.AddDays(1);

            List<ReservationInfo> list = _mgr.Search(customerName, status, from, to);
            _dgv.Rows.Clear();
            foreach (var item in list)
            {
                _dgv.Rows.Add(item.ReserveID, item.CustomerName ?? "", item.RoomNo,
                    item.TypeName ?? "", item.ExpectCheckIn.ToString("yyyy-MM-dd"),
                    item.ExpectDays, item.ContactPhone, item.Status,
                    item.ReserveTime.ToString("yyyy-MM-dd HH:mm"));
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
        // 选中行时仅显示信息，不自动填充编辑区（编辑区用于新增预订）
        // 取消/转入住操作基于选中行的 ReserveID
    }

    private void ClearEdit()
    {
        _isLoading = true;
        _cboCustomer.SelectedIndex = _cboCustomer.Items.Count > 0 ? 0 : -1;
        _cboRoom.SelectedIndex = _cboRoom.Items.Count > 0 ? 0 : -1;
        _dtpExpectCheckIn.Value = DateTime.Today;
        _txtExpectDays.Text = "1";
        _txtContactPhone.Text = "";
        _txtRemark.Text = "";
        _txtDeposit.Text = "0";
        _isLoading = false;
    }

    /// <summary>获取选中行的预订编号</summary>
    private int GetSelectedReserveID()
    {
        if (_dgv.CurrentRow == null || _dgv.CurrentRow.Index < 0)
        {
            UiHelper.Warning("请先选择一条预订记录");
            return -1;
        }
        if (int.TryParse(_dgv.CurrentRow.Cells["ReserveID"].Value?.ToString(), out int id))
            return id;
        UiHelper.Warning("无法获取预订编号");
        return -1;
    }

    /// <summary>预订登记（PRD F-09 / AC-10）</summary>
    private void DoReserve()
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
            if (!int.TryParse(_txtExpectDays.Text.Trim(), out int days) || days < 1)
            {
                UiHelper.Warning("预计天数必须为正整数");
                return;
            }

            ReservationInfo info = new()
            {
                CustomerID = customer.CustomerID,
                RoomNo = room.RoomNo,
                ExpectCheckIn = _dtpExpectCheckIn.Value.Date,
                ExpectDays = days,
                ContactPhone = _txtContactPhone.Text.Trim(),
                ReserveTime = DateTime.Now,
                Remark = _txtRemark.Text.Trim()
            };
            _mgr.Reserve(info);
            UiHelper.Info($"预订成功！房号 {room.RoomNo} 已预留");
            LoadCombos();  // 刷新空闲房间列表
            LoadData();
        }
        catch (BusinessException ex) { UiHelper.Warning(ex.Message); }
        catch (Exception ex) { UiHelper.Error("预订失败：" + ex.Message); }
    }

    /// <summary>取消预订（PRD F-10 / AC-11）</summary>
    private void DoCancel()
    {
        try
        {
            int reserveID = GetSelectedReserveID();
            if (reserveID < 0) return;

            string status = _dgv.CurrentRow.Cells["Status"].Value?.ToString();
            if (status != BusinessConstants.RESERVE_WAITING)
            {
                UiHelper.Warning($"仅待入住的预订可取消，当前状态：{status}");
                return;
            }
            if (!UiHelper.Confirm("确认取消该预订吗？取消后房间恢复空闲")) return;
            _mgr.Cancel(reserveID);
            UiHelper.Info("取消成功，房间已恢复空闲");
            LoadCombos();
            LoadData();
        }
        catch (BusinessException ex) { UiHelper.Warning(ex.Message); }
        catch (Exception ex) { UiHelper.Error("取消失败：" + ex.Message); }
    }

    /// <summary>预订转入住（PRD F-11 / AC-12）</summary>
    private void DoConvert()
    {
        try
        {
            int reserveID = GetSelectedReserveID();
            if (reserveID < 0) return;

            string status = _dgv.CurrentRow.Cells["Status"].Value?.ToString();
            if (status != BusinessConstants.RESERVE_WAITING)
            {
                UiHelper.Warning($"仅待入住的预订可转入住，当前状态：{status}");
                return;
            }
            if (!decimal.TryParse(_txtDeposit.Text.Trim(), out decimal deposit) || deposit < 0)
            {
                UiHelper.Warning("押金必须为非负数");
                return;
            }
            if (!UiHelper.Confirm($"确认转入住并收取押金 {deposit:F2} 元吗？")) return;

            CheckInInfo checkIn = _mgr.ConvertToCheckIn(reserveID, deposit);
            UiHelper.Info($"转入住成功！新入住单号：{checkIn.CheckInID}");
            LoadCombos();
            LoadData();
        }
        catch (BusinessException ex) { UiHelper.Warning(ex.Message); }
        catch (Exception ex) { UiHelper.Error("转入住失败：" + ex.Message); }
    }
}
