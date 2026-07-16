using HotelSys.BLL;
using HotelSys.Common;
using HotelSys.Models;

namespace HotelSys.Forms;

/// <summary>
/// 消费记账窗体（PRD 4.2.9 / F-17 / AC-16 / AC-17）
/// 为在住客人录入附加消费（餐饮/小商品/洗衣/其他）
/// 仅允许为"在住"状态的入住单录入消费
/// </summary>
public class FrmConsume : Form
{
    private readonly ConsumeManager _mgr = new();
    private readonly CheckInManager _checkInMgr = new();
    private readonly UserInfo _currentUser;

    private GroupBox _queryBox, _editBox;
    private List<UiHelper.FieldDef> _editFields;
    private Button _btnRefresh;
    private DataGridView _dgv;
    private TextBox _txtConsumeID, _txtAmount, _txtRemark, _txtQueryCheckIn;
    private ComboBox _cboCheckIn, _cboItem;
    private DateTimePicker _dtpConsumeTime;
    private DateTimePicker _dtpFrom, _dtpTo;
    private Button _btnAdd, _btnUpdate, _btnDelete, _btnQuery, _btnClear;
    private bool _isLoading;

    public FrmConsume(UserInfo currentUser) : this()
    {
        _currentUser = currentUser;
    }

    public FrmConsume()
    {
        DoubleBuffered = true;
        InitializeUI();
        Load += (_, _) => { LoadCheckIns(); LoadData(); };
    }

    private void InitializeUI()
    {
        Text = "消费记账";
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
        _txtQueryCheckIn = UiHelper.CreateTextBox(100);
        _dtpFrom = UiHelper.CreateDateTimePicker(130);
        _dtpTo = UiHelper.CreateDateTimePicker(130);
        _btnQuery = UiHelper.CreatePrimaryButton("查询");
        _btnClear = UiHelper.CreateSecondaryButton("清空");

        var pairs = new List<UiHelper.QueryPair>
        {
            new("入住单号：", _txtQueryCheckIn),
            new("从：", _dtpFrom),
            new("到：", _dtpTo),
        };

        FlowLayoutPanel flow = UiHelper.BuildQueryPanel(pairs, _btnQuery, _btnClear);
        flow.Dock = DockStyle.Fill;
        container.Controls.Add(flow);
    }

    private void BuildEditArea(GroupBox container)
    {
        _txtConsumeID = UiHelper.CreateTextBox(100);
        _txtConsumeID.Enabled = false;
        _cboCheckIn = UiHelper.CreateComboBox(200);
        _cboItem = UiHelper.CreateComboBox(120);
        _cboItem.Items.AddRange(BusinessConstants.CONSUME_ITEMS);
        _txtAmount = UiHelper.CreateTextBox(100);
        _dtpConsumeTime = UiHelper.CreateDateTimePicker(150);
        _txtRemark = UiHelper.CreateTextBox(250);

        _editFields = new List<UiHelper.FieldDef>
        {
            new("编号：", _txtConsumeID),
            new("入住单：", _cboCheckIn),
            new("消费项目：", _cboItem),
            new("金额：", _txtAmount),
            new("消费时间：", _dtpConsumeTime),
            new("备注：", _txtRemark),
        };

        _btnAdd = UiHelper.CreatePrimaryButton("新增");
        _btnUpdate = UiHelper.CreatePrimaryButton("修改");
        _btnDelete = UiHelper.CreateDangerButton("删除");
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
            new[] { _btnAdd, _btnUpdate, _btnDelete },
            _btnRefresh, 20, btnY + 10, containerW);

        container.Height = UiHelper.CalcEditHeight(_editFields.Count, UiHelper.CalcPairsPerRow(containerW));
    }

    private void BuildGridColumns()
    {
        _dgv.Columns.Add("ConsumeID", "编号");
        _dgv.Columns.Add("CheckInID", "入住单号");
        _dgv.Columns.Add("RoomNo", "房号");
        _dgv.Columns.Add("CustomerName", "客户");
        _dgv.Columns.Add("ItemName", "消费项目");
        _dgv.Columns.Add("Amount", "金额");
        _dgv.Columns.Add("ConsumeTime", "消费时间");
        _dgv.Columns.Add("Remark", "备注");
    }

    /// <summary>加载在住入住单到下拉框</summary>
    private void LoadCheckIns()
    {
        var occupiedList = _checkInMgr.GetOccupiedList();
        _cboCheckIn.Items.Clear();
        foreach (var item in occupiedList)
        {
            _cboCheckIn.Items.Add(item);
        }
        // 设置显示格式
        _cboCheckIn.DisplayMember = "CheckInID";
    }

    private void LoadData()
    {
        try
        {
            _isLoading = true;

            int? checkInID = int.TryParse(_txtQueryCheckIn?.Text.Trim(), out int c) ? c : null;
            DateTime from = _dtpFrom?.Value.Date ?? DateTime.Today.AddMonths(-1);
            DateTime to = _dtpTo?.Value.Date.AddDays(1) ?? DateTime.Today.AddDays(1);

            List<ConsumeInfo> list;
            if (checkInID.HasValue)
            {
                list = _mgr.GetByCheckIn(checkInID.Value);
            }
            else
            {
                list = _mgr.GetByDateRange(from, to);
            }

            _dgv.Rows.Clear();
            foreach (var item in list)
            {
                _dgv.Rows.Add(item.ConsumeID, item.CheckInID, item.RoomNo ?? "",
                    item.CustomerName ?? "", item.ItemName, item.Amount,
                    item.ConsumeTime.ToString("yyyy-MM-dd HH:mm"), item.Remark ?? "");
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

        _txtConsumeID.Text = _dgv.CurrentRow.Cells["ConsumeID"].Value?.ToString();

        // 匹配入住单下拉框
        int checkInID = int.TryParse(_dgv.CurrentRow.Cells["CheckInID"].Value?.ToString(), out int c) ? c : 0;
        for (int i = 0; i < _cboCheckIn.Items.Count; i++)
        {
            if (_cboCheckIn.Items[i] is CheckInInfo ci && ci.CheckInID == checkInID)
            {
                _cboCheckIn.SelectedIndex = i;
                break;
            }
        }

        string itemName = _dgv.CurrentRow.Cells["ItemName"].Value?.ToString();
        if (!string.IsNullOrEmpty(itemName)) _cboItem.SelectedItem = itemName;
        _txtAmount.Text = _dgv.CurrentRow.Cells["Amount"].Value?.ToString();

        if (DateTime.TryParse(_dgv.CurrentRow.Cells["ConsumeTime"].Value?.ToString(), out DateTime t))
            _dtpConsumeTime.Value = t;
        _txtRemark.Text = _dgv.CurrentRow.Cells["Remark"].Value?.ToString();
    }

    private void ClearEdit()
    {
        _isLoading = true;
        _txtConsumeID.Text = "";
        _cboCheckIn.SelectedIndex = _cboCheckIn.Items.Count > 0 ? 0 : -1;
        _cboItem.SelectedIndex = _cboItem.Items.Count > 0 ? 0 : -1;
        _txtAmount.Text = "";
        _dtpConsumeTime.Value = DateTime.Now;
        _txtRemark.Text = "";
        _isLoading = false;
    }

    private ConsumeInfo BuildEntityFromInput()
    {
        int checkInID = _cboCheckIn.SelectedItem is CheckInInfo ci ? ci.CheckInID : 0;
        return new ConsumeInfo
        {
            ConsumeID = int.TryParse(_txtConsumeID.Text.Trim(), out int id) ? id : 0,
            CheckInID = checkInID,
            ItemName = _cboItem.SelectedItem?.ToString() ?? _cboItem.Text,
            Amount = decimal.TryParse(_txtAmount.Text.Trim(), out decimal a) ? a : 0,
            ConsumeTime = _dtpConsumeTime.Value,
            Remark = _txtRemark.Text.Trim()
        };
    }

    private void DoAdd()
    {
        try
        {
            _mgr.Add(BuildEntityFromInput());
            UiHelper.Info("消费录入成功");
            LoadData();
        }
        catch (BusinessException ex) { UiHelper.Warning(ex.Message); }
        catch (Exception ex) { UiHelper.Error("新增失败：" + ex.Message); }
    }

    private void DoUpdate()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(_txtConsumeID.Text))
            {
                UiHelper.Warning("请先选择要修改的记录");
                return;
            }
            _mgr.Update(BuildEntityFromInput());
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
            if (string.IsNullOrWhiteSpace(_txtConsumeID.Text))
            {
                UiHelper.Warning("请先选择要删除的记录");
                return;
            }
            if (!UiHelper.Confirm("确认删除该消费记录吗？")) return;
            _mgr.Delete(int.Parse(_txtConsumeID.Text.Trim()));
            UiHelper.Info("删除成功");
            LoadData();
        }
        catch (BusinessException ex) { UiHelper.Warning(ex.Message); }
        catch (Exception ex) { UiHelper.Error("删除失败：" + ex.Message); }
    }
}
