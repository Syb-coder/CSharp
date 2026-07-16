using HotelSys.BLL;
using HotelSys.Common;
using HotelSys.Models;

namespace HotelSys.Forms;

/// <summary>
/// 用户管理窗体（PRD 4.2.12 / F-21 / AC-21）
/// 用户增删改查，密码以 SHA-256 哈希存储
/// 修改时密码留空表示不修改密码，不能删除当前登录用户
/// </summary>
public class FrmUser : Form
{
    private readonly UserManager _mgr = new();
    private readonly UserInfo _currentUser;

    private DataGridView _dgv;
    private TextBox _txtUserName, _txtPassword, _txtConfirmPwd, _txtRealName, _txtQuery;
    private ComboBox _cboPurview;
    private Button _btnAdd, _btnUpdate, _btnDelete, _btnQuery, _btnClear;
    private string _originalUserName;  // 修改时记录原始用户名
    private bool _isLoading;

    public FrmUser(UserInfo currentUser) : this()
    {
        _currentUser = currentUser;
    }

    public FrmUser()
    {
        DoubleBuffered = true;
        InitializeUI();
        Load += (_, _) => LoadData();
    }

    private GroupBox _queryBox, _editBox;
    private List<UiHelper.FieldDef> _editFields;
    private Button _btnRefresh;

    private void InitializeUI()
    {
        Text = "用户管理";
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
        _txtQuery = UiHelper.CreateTextBox(180);
        _btnQuery = UiHelper.CreatePrimaryButton("查询");
        _btnClear = UiHelper.CreateSecondaryButton("清空");

        var pairs = new List<UiHelper.QueryPair>
        {
            new("用户名：", _txtQuery),
        };
        FlowLayoutPanel flow = UiHelper.BuildQueryPanel(pairs, _btnQuery, _btnClear);
        container.Controls.Add(flow);
    }

    private void BuildEditArea(GroupBox container)
    {
        _txtUserName = UiHelper.CreateTextBox(150);
        _txtPassword = UiHelper.CreateTextBox(150, isPassword: true);
        _txtConfirmPwd = UiHelper.CreateTextBox(150, isPassword: true);
        _cboPurview = UiHelper.CreateComboBox(100);
        _cboPurview.Items.Add(BusinessConstants.ROLE_ADMIN);
        _cboPurview.Items.Add(BusinessConstants.ROLE_USER);
        _txtRealName = UiHelper.CreateTextBox(150);

        _editFields = new List<UiHelper.FieldDef>
        {
            new("用户名：", _txtUserName),
            new("密码：", _txtPassword),
            new("确认密码：", _txtConfirmPwd),
            new("权限：", _cboPurview),
            new("真实姓名：", _txtRealName),
        };

        _btnAdd = UiHelper.CreatePrimaryButton("新增");
        _btnUpdate = UiHelper.CreatePrimaryButton("修改");
        _btnDelete = UiHelper.CreateDangerButton("删除");
        _btnRefresh = UiHelper.CreateSecondaryButton("刷新");
        _btnRefresh.Click += (_, _) => LoadData();

        LayoutEditArea(container);
    }

    /// <summary>按当前容器宽度重新布局编辑区（控件 + 按钮）</summary>
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
        _dgv.Columns.Add("UserName", "用户名");
        _dgv.Columns.Add("UserPurview", "权限");
        _dgv.Columns.Add("RealName", "真实姓名");
    }

    private void LoadData()
    {
        try
        {
            _isLoading = true;
            string keyword = _txtQuery?.Text.Trim();
            List<UserInfo> list = _mgr.GetAll();
            if (!string.IsNullOrEmpty(keyword))
                list = list.FindAll(u => (u.UserName ?? "").Contains(keyword));

            _dgv.Rows.Clear();
            foreach (var item in list)
            {
                _dgv.Rows.Add(item.UserName, item.UserPurview, item.RealName ?? "");
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

        // 记录原始用户名（用于 Update 的 originalUserName 参数）
        _originalUserName = _dgv.CurrentRow.Cells["UserName"].Value?.ToString();
        _txtUserName.Text = _originalUserName;
        // 修改时密码留空表示不修改，不回显密码
        _txtPassword.Text = "";
        _txtConfirmPwd.Text = "";

        string purview = _dgv.CurrentRow.Cells["UserPurview"].Value?.ToString();
        if (!string.IsNullOrEmpty(purview)) _cboPurview.SelectedItem = purview;
        _txtRealName.Text = _dgv.CurrentRow.Cells["RealName"].Value?.ToString();
    }

    private void ClearEdit()
    {
        _isLoading = true;
        _originalUserName = null;
        _txtUserName.Text = "";
        _txtUserName.Enabled = true;
        _txtPassword.Text = "";
        _txtConfirmPwd.Text = "";
        _cboPurview.SelectedIndex = _cboPurview.Items.Count > 0 ? 0 : -1;
        _txtRealName.Text = "";
        _isLoading = false;
    }

    private void DoAdd()
    {
        try
        {
            if (_txtPassword.Text != _txtConfirmPwd.Text)
            {
                UiHelper.Warning("两次输入的密码不一致");
                return;
            }
            UserInfo entity = new()
            {
                UserName = _txtUserName.Text.Trim(),
                UserPassword = _txtPassword.Text,
                UserPurview = _cboPurview.SelectedItem?.ToString(),
                RealName = _txtRealName.Text.Trim()
            };
            _mgr.Add(entity, _txtConfirmPwd.Text);
            UiHelper.Info("新增用户成功");
            LoadData();
        }
        catch (BusinessException ex) { UiHelper.Warning(ex.Message); }
        catch (Exception ex) { UiHelper.Error("新增失败：" + ex.Message); }
    }

    private void DoUpdate()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(_originalUserName))
            {
                UiHelper.Warning("请先选择要修改的记录");
                return;
            }
            if (_txtPassword.Text != _txtConfirmPwd.Text)
            {
                UiHelper.Warning("两次输入的密码不一致");
                return;
            }
            UserInfo entity = new()
            {
                UserName = _txtUserName.Text.Trim(),
                // 密码为空表示不修改，BLL 层会保留原密码
                UserPassword = _txtPassword.Text,
                UserPurview = _cboPurview.SelectedItem?.ToString(),
                RealName = _txtRealName.Text.Trim()
            };
            _mgr.Update(entity, _originalUserName);
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
            if (string.IsNullOrWhiteSpace(_originalUserName))
            {
                UiHelper.Warning("请先选择要删除的记录");
                return;
            }
            if (!UiHelper.Confirm($"确认删除用户 {_originalUserName} 吗？")) return;
            _mgr.Delete(_originalUserName);
            UiHelper.Info("删除成功");
            LoadData();
        }
        catch (BusinessException ex) { UiHelper.Warning(ex.Message); }
        catch (Exception ex) { UiHelper.Error("删除失败：" + ex.Message); }
    }
}
