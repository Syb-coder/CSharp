using HotelSys.BLL;
using HotelSys.Common;
using HotelSys.Models;

namespace HotelSys.Forms;

/// <summary>
/// 客房管理窗体（PRD 4.2.5 / F-02 / F-03 / AC-06 / AC-07）
/// 增删改查 + 房态维护（空闲↔维护）
/// 房态由业务驱动自动流转，此处仅提供空闲↔维护手动切换
/// </summary>
public class FrmRoom : Form
{
    private readonly RoomManager _mgr = new();
    private readonly RoomTypeManager _typeMgr = new();
    private readonly UserInfo _currentUser;

    private GroupBox _queryBox, _editBox;
    private List<UiHelper.FieldDef> _editFields;
    private Button _btnRefresh;
    private DataGridView _dgv;
    private TextBox _txtRoomNo, _txtFloor, _txtBedCount, _txtRemark, _txtQueryRoom, _txtQueryFloor;
    private ComboBox _cboType, _cboQueryStatus;
    private Label _lblStatus;
    private Button _btnAdd, _btnUpdate, _btnDelete, _btnQuery, _btnToggleStatus, _btnClear;
    private bool _isLoading;

    public FrmRoom(UserInfo currentUser) : this()
    {
        _currentUser = currentUser;
    }

    public FrmRoom()
    {
        DoubleBuffered = true;
        InitializeUI();
        Load += (_, _) => { LoadTypes(); LoadQueryStatusCombo(); LoadData(); };
    }

    private void InitializeUI()
    {
        Text = "客房管理";
        BackColor = ThemeColor.BgPage;
        Font = UiHelper.DefaultFont;

        _queryBox = UiHelper.CreateStyledGroupBox("查询");
        _queryBox.Dock = DockStyle.Top;
        BuildQueryArea(_queryBox);

        _editBox = UiHelper.CreateStyledGroupBox("编辑");
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
        _btnAdd.Click += (_, _) => DoAdd();
        _btnUpdate.Click += (_, _) => DoUpdate();
        _btnDelete.Click += (_, _) => DoDelete();
        _btnToggleStatus.Click += (_, _) => DoToggleStatus();

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
        _txtQueryFloor = UiHelper.CreateTextBox(80);
        _cboQueryStatus = UiHelper.CreateComboBox(100);

        _btnQuery = UiHelper.CreatePrimaryButton("查询");
        _btnClear = UiHelper.CreateSecondaryButton("清空");

        var pairs = new List<UiHelper.QueryPair>
        {
            new("房号：", _txtQueryRoom),
            new("楼层：", _txtQueryFloor),
            new("状态：", _cboQueryStatus),
        };

        FlowLayoutPanel flow = UiHelper.BuildQueryPanel(pairs, _btnQuery, _btnClear);
        flow.Dock = DockStyle.Fill;
        container.Controls.Add(flow);
    }

    private void BuildEditArea(GroupBox container)
    {
        _txtRoomNo = UiHelper.CreateTextBox(120);
        _cboType = UiHelper.CreateComboBox(180);
        _txtFloor = UiHelper.CreateTextBox(80);
        _txtBedCount = UiHelper.CreateTextBox(80);
        _txtRemark = UiHelper.CreateTextBox(250);
        _lblStatus = new Label
        {
            Size = new Size(120, 28),
            Font = ThemeColor.FontBold,
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = ThemeColor.Success,
            BorderStyle = BorderStyle.FixedSingle
        };

        _editFields = new List<UiHelper.FieldDef>
        {
            new("房号：", _txtRoomNo),
            new("房型：", _cboType),
            new("楼层：", _txtFloor),
            new("床位数：", _txtBedCount),
            new("当前状态：", _lblStatus),
            new("备注：", _txtRemark),
        };

        _btnAdd = UiHelper.CreatePrimaryButton("新增");
        _btnUpdate = UiHelper.CreatePrimaryButton("修改");
        _btnDelete = UiHelper.CreateDangerButton("删除");
        _btnToggleStatus = UiHelper.CreateSecondaryButton("设为维护");
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
            new[] { _btnAdd, _btnUpdate, _btnDelete, _btnToggleStatus },
            _btnRefresh, 20, btnY + 10, containerW);

        container.Height = UiHelper.CalcEditHeight(_editFields.Count, UiHelper.CalcPairsPerRow(containerW));
    }

    private void BuildGridColumns()
    {
        _dgv.Columns.Add("RoomNo", "房号");
        _dgv.Columns.Add("TypeName", "房型");
        _dgv.Columns.Add("Floor", "楼层");
        _dgv.Columns.Add("BedCount", "床位数");
        _dgv.Columns.Add("RoomStatus", "状态");
        _dgv.Columns.Add("Remark", "备注");
    }

    /// <summary>加载房型下拉框</summary>
    private void LoadTypes()
    {
        var types = _typeMgr.GetAll();
        UiHelper.BindCombo(_cboType, types, "TypeName", "TypeID");
    }

    /// <summary>加载查询状态下拉框</summary>
    private void LoadQueryStatusCombo()
    {
        _cboQueryStatus.Items.Clear();
        _cboQueryStatus.Items.Add("全部");
        foreach (var s in RoomStatusConstants.ALL)
            _cboQueryStatus.Items.Add(s);
        _cboQueryStatus.SelectedIndex = 0;
    }

    private void LoadData()
    {
        try
        {
            _isLoading = true;

            string roomNo = _txtQueryRoom?.Text.Trim();
            int? floor = int.TryParse(_txtQueryFloor?.Text.Trim(), out int f) ? f : null;
            string status = _cboQueryStatus.SelectedIndex > 0 ? _cboQueryStatus.SelectedItem.ToString() : null;

            List<RoomInfo> list = _mgr.GetAll();
            if (!string.IsNullOrEmpty(roomNo))
                list = list.FindAll(r => (r.RoomNo ?? "").Contains(roomNo));
            if (floor.HasValue)
                list = list.FindAll(r => r.Floor == floor.Value);
            if (!string.IsNullOrEmpty(status))
                list = list.FindAll(r => r.RoomStatus == status);

            _dgv.Rows.Clear();
            foreach (var item in list)
            {
                _dgv.Rows.Add(item.RoomNo, item.TypeName ?? "", item.Floor, item.BedCount,
                    item.RoomStatus, item.Remark ?? "");
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

        string roomNo = _dgv.CurrentRow.Cells["RoomNo"].Value?.ToString();
        _txtRoomNo.Text = roomNo;
        _txtRoomNo.Enabled = false;

        // 在下拉框中匹配房型
        string typeName = _dgv.CurrentRow.Cells["TypeName"].Value?.ToString();
        for (int i = 0; i < _cboType.Items.Count; i++)
        {
            if (_cboType.Items[i] is RoomTypeInfo rt && rt.TypeName == typeName)
            {
                _cboType.SelectedIndex = i;
                break;
            }
        }

        _txtFloor.Text = _dgv.CurrentRow.Cells["Floor"].Value?.ToString();
        _txtBedCount.Text = _dgv.CurrentRow.Cells["BedCount"].Value?.ToString();
        _txtRemark.Text = _dgv.CurrentRow.Cells["Remark"].Value?.ToString();

        string status = _dgv.CurrentRow.Cells["RoomStatus"].Value?.ToString();
        _lblStatus.Text = status;
        UpdateStatusButton(status);
    }

    /// <summary>根据当前房态更新维护切换按钮文字</summary>
    private void UpdateStatusButton(string status)
    {
        if (status == RoomStatusConstants.FREE)
        {
            _btnToggleStatus.Text = "设为维护";
            _btnToggleStatus.Enabled = true;
        }
        else if (status == RoomStatusConstants.MAINTENANCE)
        {
            _btnToggleStatus.Text = "恢复空闲";
            _btnToggleStatus.Enabled = true;
        }
        else
        {
            _btnToggleStatus.Text = "房态由业务驱动";
            _btnToggleStatus.Enabled = false;
        }
    }

    private void ClearEdit()
    {
        _isLoading = true;
        try
        {
            _txtRoomNo.Text = "";
            _txtRoomNo.Enabled = true;
            _cboType.SelectedIndex = _cboType.Items.Count > 0 ? 0 : -1;
            _txtFloor.Text = "";
            _txtBedCount.Text = "";
            _txtRemark.Text = "";
            _lblStatus.Text = "新增默认空闲";
            _lblStatus.ForeColor = ThemeColor.Success;
            _btnToggleStatus.Text = "设为维护";
            _btnToggleStatus.Enabled = false;
        }
        finally
        {
            _isLoading = false;
        }
    }

    private RoomInfo BuildEntityFromInput()
    {
        string typeId = _cboType.SelectedItem is RoomTypeInfo rt ? rt.TypeID : "";
        return new RoomInfo
        {
            RoomNo = _txtRoomNo.Text.Trim(),
            TypeID = typeId,
            Floor = int.TryParse(_txtFloor.Text.Trim(), out int f) ? f : 0,
            BedCount = int.TryParse(_txtBedCount.Text.Trim(), out int b) ? b : 0,
            RoomStatus = RoomStatusConstants.FREE,
            Remark = _txtRemark.Text.Trim()
        };
    }

    private void DoAdd()
    {
        try
        {
            _mgr.Add(BuildEntityFromInput());
            UiHelper.Info("新增成功，默认状态为空闲");
            LoadData();
        }
        catch (BusinessException ex) { UiHelper.Warning(ex.Message); }
        catch (Exception ex) { UiHelper.Error("新增失败：" + ex.Message); }
    }

    private void DoUpdate()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(_txtRoomNo.Text))
            {
                UiHelper.Warning("请先选择要修改的记录");
                return;
            }
            // 修改时保留原房态（房态由业务驱动，不在此处修改）
            RoomInfo entity = BuildEntityFromInput();
            string currentStatus = _lblStatus.Text;
            if (RoomStatusConstants.ALL.Contains(currentStatus))
                entity.RoomStatus = currentStatus;
            _mgr.Update(entity);
            UiHelper.Info("修改成功");
            LoadData();
        }
        catch (BusinessException ex) { UiHelper.Warning(ex.Message); }
        catch (Exception ex) { UiHelper.Error("修改失败：" + ex.Message); }
    }

    private void DoDelete()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(_txtRoomNo.Text))
            {
                UiHelper.Warning("请先选择要删除的记录");
                return;
            }
            if (!UiHelper.Confirm($"确认删除客房 {_txtRoomNo.Text} 吗？")) return;
            _mgr.Delete(_txtRoomNo.Text.Trim());
            UiHelper.Info("删除成功");
            LoadData();
        }
        catch (BusinessException ex) { UiHelper.Warning(ex.Message); }
        catch (Exception ex) { UiHelper.Error("删除失败：" + ex.Message); }
    }

    /// <summary>房态维护：空闲↔维护手动切换（PRD AC-07）</summary>
    private void DoToggleStatus()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(_txtRoomNo.Text))
            {
                UiHelper.Warning("请先选择要操作的房间");
                return;
            }
            string currentStatus = _lblStatus.Text;
            string newStatus = currentStatus == RoomStatusConstants.FREE
                ? RoomStatusConstants.MAINTENANCE
                : RoomStatusConstants.FREE;

            if (!UiHelper.Confirm($"确认将房号 {_txtRoomNo.Text} 设为 {newStatus} 吗？")) return;
            _mgr.UpdateStatus(_txtRoomNo.Text.Trim(), newStatus);
            UiHelper.Info($"房态已变更为 {newStatus}");
            LoadData();
        }
        catch (BusinessException ex) { UiHelper.Warning(ex.Message); }
        catch (Exception ex) { UiHelper.Error("操作失败：" + ex.Message); }
    }
}
