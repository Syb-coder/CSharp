using HotelSys.BLL;
using HotelSys.Common;
using HotelSys.Models;

namespace HotelSys.Forms;

/// <summary>
/// 客户信息管理窗体（PRD 4.2.6 / F-07 / F-08 / AC-08 / AC-09）
/// 增删改查 + 多条件模糊查询 + 入住历史查看
/// 删除前由 BLL 校验在住记录，校验不通过抛 BusinessException
/// </summary>
public class FrmCustomer : Form
{
    private readonly CustomerManager _mgr = new();
    private readonly UserInfo _currentUser;

    private GroupBox _queryBox, _editBox;
    private List<UiHelper.FieldDef> _editFields;
    private Button _btnRefresh;
    private DataGridView _dgv;
    private TextBox _txtCustomerID, _txtName, _txtIdNumber, _txtPhone, _txtAddress;
    private TextBox _txtQueryName, _txtQueryId, _txtQueryPhone;
    private ComboBox _cboGender, _cboIdType;
    private Button _btnAdd, _btnUpdate, _btnDelete, _btnQuery, _btnClear, _btnHistory;
    private bool _isLoading;

    public FrmCustomer(UserInfo currentUser) : this()
    {
        _currentUser = currentUser;
    }

    public FrmCustomer()
    {
        DoubleBuffered = true;
        InitializeUI();
        Load += (_, _) => LoadData();
    }

    private void InitializeUI()
    {
        Text = "客户信息管理";
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
        _btnHistory.Click += (_, _) => ShowHistory();

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
        _txtQueryName = UiHelper.CreateTextBox(100);
        _txtQueryId = UiHelper.CreateTextBox(130);
        _txtQueryPhone = UiHelper.CreateTextBox(120);
        _btnQuery = UiHelper.CreatePrimaryButton("查询");
        _btnClear = UiHelper.CreateSecondaryButton("清空");

        var pairs = new List<UiHelper.QueryPair>
        {
            new UiHelper.QueryPair("姓名：", _txtQueryName),
            new UiHelper.QueryPair("证件号：", _txtQueryId),
            new UiHelper.QueryPair("手机号：", _txtQueryPhone),
        };
        FlowLayoutPanel flow = UiHelper.BuildQueryPanel(pairs, _btnQuery, _btnClear);
        flow.Dock = DockStyle.Fill;
        container.Controls.Add(flow);
    }

    private void BuildEditArea(GroupBox container)
    {
        _txtCustomerID = UiHelper.CreateTextBox(100);
        _txtCustomerID.Enabled = false;
        _txtName = UiHelper.CreateTextBox(150);
        _cboGender = UiHelper.CreateComboBox(100);
        _cboGender.Items.AddRange(BusinessConstants.GENDERS);
        _cboIdType = UiHelper.CreateComboBox(100);
        _cboIdType.Items.AddRange(BusinessConstants.ID_TYPES);
        _txtIdNumber = UiHelper.CreateTextBox(180);
        _txtPhone = UiHelper.CreateTextBox(150);
        _txtAddress = UiHelper.CreateTextBox(250);

        _editFields = new List<UiHelper.FieldDef>
        {
            new("编号：", _txtCustomerID),
            new("姓名：", _txtName),
            new("性别：", _cboGender),
            new("证件类型：", _cboIdType),
            new("证件号码：", _txtIdNumber),
            new("手机号：", _txtPhone),
            new("地址：", _txtAddress),
        };

        _btnAdd = UiHelper.CreatePrimaryButton("新增");
        _btnUpdate = UiHelper.CreatePrimaryButton("修改");
        _btnDelete = UiHelper.CreateDangerButton("删除");
        _btnHistory = UiHelper.CreateSecondaryButton("查看历史");
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
            new[] { _btnAdd, _btnUpdate, _btnDelete, _btnHistory },
            _btnRefresh, 20, btnY + 10, containerW);

        container.Height = UiHelper.CalcEditHeight(_editFields.Count, UiHelper.CalcPairsPerRow(containerW));
    }

    private void BuildGridColumns()
    {
        _dgv.Columns.Add("CustomerID", "编号");
        _dgv.Columns.Add("CustomerName", "姓名");
        _dgv.Columns.Add("Gender", "性别");
        _dgv.Columns.Add("IdType", "证件类型");
        _dgv.Columns.Add("IdNumber", "证件号码");
        _dgv.Columns.Add("Phone", "手机号");
        _dgv.Columns.Add("Address", "地址");
        _dgv.Columns.Add("CreateTime", "登记时间");
    }

    private void LoadData()
    {
        try
        {
            _isLoading = true;
            string name = _txtQueryName?.Text.Trim();
            string id = _txtQueryId?.Text.Trim();
            string phone = _txtQueryPhone?.Text.Trim();

            List<CustomerInfo> list = _mgr.Search(name, id, phone);
            _dgv.Rows.Clear();
            foreach (var item in list)
            {
                _dgv.Rows.Add(item.CustomerID, item.CustomerName, item.Gender ?? "",
                    item.IdType ?? "", item.IdNumber ?? "", item.Phone ?? "",
                    item.Address ?? "", item.CreateTime.ToString("yyyy-MM-dd HH:mm"));
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

        _txtCustomerID.Text = _dgv.CurrentRow.Cells["CustomerID"].Value?.ToString();
        _txtName.Text = _dgv.CurrentRow.Cells["CustomerName"].Value?.ToString();
        _txtIdNumber.Text = _dgv.CurrentRow.Cells["IdNumber"].Value?.ToString();
        _txtPhone.Text = _dgv.CurrentRow.Cells["Phone"].Value?.ToString();
        _txtAddress.Text = _dgv.CurrentRow.Cells["Address"].Value?.ToString();

        // 下拉框匹配
        string gender = _dgv.CurrentRow.Cells["Gender"].Value?.ToString();
        if (!string.IsNullOrEmpty(gender)) _cboGender.SelectedItem = gender;
        string idType = _dgv.CurrentRow.Cells["IdType"].Value?.ToString();
        if (!string.IsNullOrEmpty(idType)) _cboIdType.SelectedItem = idType;
    }

    private void ClearEdit()
    {
        _isLoading = true;
        _txtCustomerID.Text = "";
        _txtName.Text = "";
        _cboGender.SelectedIndex = _cboGender.Items.Count > 0 ? 0 : -1;
        _cboIdType.SelectedIndex = _cboIdType.Items.Count > 0 ? 0 : -1;
        _txtIdNumber.Text = "";
        _txtPhone.Text = "";
        _txtAddress.Text = "";
        _isLoading = false;
    }

    private CustomerInfo BuildEntityFromInput()
    {
        int id = int.TryParse(_txtCustomerID.Text.Trim(), out int i) ? i : 0;
        return new CustomerInfo
        {
            CustomerID = id,
            CustomerName = _txtName.Text.Trim(),
            Gender = _cboGender.SelectedItem?.ToString(),
            IdType = _cboIdType.SelectedItem?.ToString(),
            IdNumber = _txtIdNumber.Text.Trim(),
            Phone = _txtPhone.Text.Trim(),
            Address = _txtAddress.Text.Trim()
        };
    }

    private void DoAdd()
    {
        try
        {
            int newId = _mgr.Add(BuildEntityFromInput());
            UiHelper.Info($"新增成功，客户编号：{newId}");
            LoadData();
        }
        catch (BusinessException ex) { UiHelper.Warning(ex.Message); }
        catch (Exception ex) { UiHelper.Error("新增失败：" + ex.Message); }
    }

    private void DoUpdate()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(_txtCustomerID.Text))
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
            if (string.IsNullOrWhiteSpace(_txtCustomerID.Text))
            {
                UiHelper.Warning("请先选择要删除的记录");
                return;
            }
            if (!UiHelper.Confirm($"确认删除客户 {_txtName.Text} 吗？")) return;
            _mgr.Delete(int.Parse(_txtCustomerID.Text.Trim()));
            UiHelper.Info("删除成功");
            LoadData();
        }
        catch (BusinessException ex) { UiHelper.Warning(ex.Message); }
        catch (Exception ex) { UiHelper.Error("删除失败：" + ex.Message); }
    }

    /// <summary>查看客户入住历史（PRD F-08 / AC-09）</summary>
    private void ShowHistory()
    {
        if (string.IsNullOrWhiteSpace(_txtCustomerID.Text))
        {
            UiHelper.Warning("请先选择客户");
            return;
        }
        try
        {
            int customerID = int.Parse(_txtCustomerID.Text.Trim());
            var history = _mgr.GetHistory(customerID);
            ShowHistoryDialog(_txtName.Text, history);
        }
        catch (Exception ex)
        {
            UiHelper.Error("查询历史失败：" + ex.Message);
        }
    }

    /// <summary>弹出入住历史对话框</summary>
    private void ShowHistoryDialog(string customerName, List<CheckInInfo> history)
    {
        using Form dlg = new()
        {
            Text = $"{customerName} 的入住历史",
            StartPosition = FormStartPosition.CenterParent,
            Size = new Size(720, 400)
        };
        DataGridView dgv = new() { Dock = DockStyle.Fill };
        UiHelper.StyleDataGridView(dgv);
        dgv.Columns.Add("CheckInID", "入住单号");
        dgv.Columns.Add("RoomNo", "房号");
        dgv.Columns.Add("TypeName", "房型");
        dgv.Columns.Add("CheckInTime", "入住时间");
        dgv.Columns.Add("ExpectCheckOut", "预计退房");
        dgv.Columns.Add("Deposit", "押金");
        dgv.Columns.Add("Status", "状态");

        foreach (var item in history)
        {
            dgv.Rows.Add(item.CheckInID, item.RoomNo, item.TypeName ?? "",
                item.CheckInTime.ToString("yyyy-MM-dd HH:mm"),
                item.ExpectCheckOut.ToString("yyyy-MM-dd"),
                item.Deposit, item.Status);
        }
        dlg.Controls.Add(dgv);
        dlg.ShowDialog(this);
    }
}
