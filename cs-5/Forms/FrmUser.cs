using LibrarySys.BLL;
using LibrarySys.Common;
using LibrarySys.Models;
using System.ComponentModel;

namespace LibrarySys.Forms;

/// <summary>
/// 用户管理窗体
/// 固定坐标布局：查询区(顶部) → DataGridView(中间) → 编辑区(底部)
/// </summary>
public partial class FrmUser : Form
{
    private readonly UserBiz _biz = new();
    private readonly UserInfo _currentUser;
    private readonly bool _canEdit;

    private DataGridView _dgv;
    private TextBox _txtUserName, _txtPassword;
    private ComboBox _cboPurview;
    private TextBox _txtQueryName;
    private ComboBox _cboQueryPurview;
    private bool _isLoading;

    public FrmUser()
    {
        // 无参构造：非管理员模式，所有编辑按钮不可用
        _canEdit = false;
        InitializeComponent();
        BuildUI();
        Load += (_, _) => LoadData();
    }

    public FrmUser(UserInfo currentUser)
    {
        _currentUser = currentUser;
        _canEdit = currentUser.UserPurview == BusinessConstants.ROLE_ADMIN;
        InitializeComponent();
        BuildUI();
        Load += (_, _) => LoadData();
    }

    private void BuildUI()
    {
        // 设计器模式下跳过：设计器已在 InitializeComponent 中创建控件骨架
        if (LicenseManager.UsageMode == LicenseUsageMode.Designtime) return;
        // 运行时：清除 InitializeComponent 创建的骨架控件，重新完整构建
        Controls.Clear();

        DoubleBuffered = true;
        Text = "用户管理";
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(1150, 700);
        MinimumSize = new Size(1100, 680);
        Font = UiHelper.DefaultFont;

        // 创建编辑区控件
        _txtUserName = UiHelper.CreateTextBox();
        _txtPassword = UiHelper.CreateTextBox(isPassword: true);
        _cboPurview = UiHelper.CreateComboBox();
        _cboPurview.Items.AddRange(new object[] { BusinessConstants.ROLE_ADMIN, BusinessConstants.ROLE_USER });

        // 创建查询区控件
        _txtQueryName = UiHelper.CreateTextBox();
        _cboQueryPurview = UiHelper.CreateComboBox();
        _cboQueryPurview.Items.AddRange(new object[] { "全部", BusinessConstants.ROLE_ADMIN, BusinessConstants.ROLE_USER });
        _cboQueryPurview.SelectedIndex = 0;

        // ===== 顶部查询区（固定坐标） =====
        GroupBox grpQuery = new GroupBox()
        {
            Text = "查询条件",
            Location = new Point(0, 0),
            Size = new Size(1150, 125),
            Dock = DockStyle.Top
        };

        UiHelper.LayoutFields(grpQuery.Controls, new List<UiHelper.FieldDef>
        {
            new("用户名：", _txtQueryName),
            new("身份：", _cboQueryPurview),
        }, pairsPerRow: 2, startX: 20, startY: 30);

        Button btnQuery = UiHelper.CreateButton("查询");
        Button btnShowAll = UiHelper.CreateButton("全部");
        btnQuery.Click += (_, _) => DoQuery();
        btnShowAll.Click += (_, _) => DoShowAll();
        UiHelper.LayoutButtons(grpQuery.Controls, new[] { btnQuery, btnShowAll }, 20, 72);

        // ===== DataGridView（中间填充） =====
        _dgv = new DataGridView { Dock = DockStyle.Fill };
        UiHelper.StyleDataGridView(_dgv);
        _dgv.SelectionChanged += (_, _) => BindEditForm();

        // ===== 底部编辑区（固定坐标） =====
        GroupBox grpEdit = new GroupBox()
        {
            Text = "用户信息",
            Dock = DockStyle.Bottom,
            Height = 195
        };

        UiHelper.LayoutFields(grpEdit.Controls, new List<UiHelper.FieldDef>
        {
            new("用户名：", _txtUserName),
            new("密码：", _txtPassword),
            new("身份：", _cboPurview),
        }, pairsPerRow: 3, startX: 20, startY: 30);

        Button btnAdd = UiHelper.CreateButton("新增"); btnAdd.Enabled = _canEdit;
        Button btnUpdate = UiHelper.CreateButton("修改"); btnUpdate.Enabled = _canEdit;
        Button btnDelete = UiHelper.CreateButton("删除"); btnDelete.Enabled = _canEdit;
        Button btnReset = UiHelper.CreateButton("重置");
        Button btnReturn = UiHelper.CreateButton("返回");
        btnAdd.Click += (_, _) => DoAdd();
        btnUpdate.Click += (_, _) => DoUpdate();
        btnDelete.Click += (_, _) => DoDelete();
        btnReset.Click += (_, _) => DoClear();
        btnReturn.Click += (_, _) => Close();
        UiHelper.LayoutButtonsWithReturn(
            grpEdit.Controls,
            new[] { btnAdd, btnUpdate, btnDelete, btnReset },
            btnReturn, 20, 140, 1150);

        // 添加顺序：先底部，再顶部，最后 Fill
        Controls.Add(grpEdit);
        Controls.Add(grpQuery);
        Controls.Add(_dgv);
    }

    private void LoadData()
    {
        _isLoading = true;
        _dgv.DataSource = _biz.GetAll();
        SetChineseHeaders();
        _isLoading = false;
    }

    private void SetChineseHeaders()
    {
        if (_dgv.Columns.Count == 0) return;
        _dgv.Columns["UserName"].HeaderText = "用户名";
        _dgv.Columns["UserPurview"].HeaderText = "身份";
        // 密码列隐藏，避免泄露哈希
        if (_dgv.Columns.Contains("UserPassword"))
            _dgv.Columns["UserPassword"].Visible = false;
    }

    private void DoQuery()
    {
        string name = _txtQueryName.Text.Trim();
        string purview = _cboQueryPurview.SelectedItem?.ToString() ?? "";
        List<UserInfo> all = _biz.GetAll();
        List<UserInfo> filtered = all;

        if (!string.IsNullOrEmpty(name))
        {
            filtered = filtered.FindAll(u => u.UserName.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0);
        }
        if (purview != "全部" && !string.IsNullOrEmpty(purview))
        {
            filtered = filtered.FindAll(u => u.UserPurview == purview);
        }

        _dgv.DataSource = filtered;
        SetChineseHeaders();
    }

    private void DoShowAll()
    {
        _txtQueryName.Clear();
        _cboQueryPurview.SelectedIndex = 0;
        LoadData();
    }

    private void BindEditForm()
    {
        if (_isLoading) return;
        if (_dgv.CurrentRow?.DataBoundItem is not UserInfo item) return;
        _txtUserName.Text = item.UserName;
        _cboPurview.SelectedItem = item.UserPurview;
        // 密码不回显，留空表示不修改密码
        _txtPassword.Clear();
    }

    private UserInfo BuildEntity()
    {
        return new UserInfo
        {
            UserName = _txtUserName.Text.Trim(),
            UserPassword = _txtPassword.Text,
            UserPurview = _cboPurview.SelectedItem?.ToString()
        };
    }

    private void DoAdd()
    {
        try
        {
            UserInfo entity = BuildEntity();
            _biz.Add(entity, _txtPassword.Text);
            UiHelper.Info("新增成功");
            LoadData();
            DoClear();
        }
        catch (BusinessException ex) { UiHelper.Error(ex.Message); }
        catch (Exception ex) { UiHelper.Error($"操作失败：{ex.Message}"); }
    }

    private void DoUpdate()
    {
        if (_dgv.CurrentRow?.DataBoundItem is not UserInfo originalItem) return;
        try
        {
            _biz.Update(BuildEntity(), originalItem.UserName);
            UiHelper.Info("修改成功");
            LoadData();
        }
        catch (BusinessException ex) { UiHelper.Error(ex.Message); }
        catch (Exception ex) { UiHelper.Error($"操作失败：{ex.Message}"); }
    }

    private void DoDelete()
    {
        if (_dgv.CurrentRow?.DataBoundItem is not UserInfo item) return;
        // 防止删除当前登录账号导致会话失效
        if (_currentUser != null && item.UserName == _currentUser.UserName)
        {
            UiHelper.Error("不能删除当前登录的用户账号");
            return;
        }
        if (!UiHelper.Confirm($"确认删除用户【{item.UserName}】吗？")) return;
        try
        {
            string currentName = _currentUser?.UserName ?? "";
            _biz.Delete(item.UserName, currentName);
            UiHelper.Info("删除成功");
            LoadData();
            DoClear();
        }
        catch (BusinessException ex) { UiHelper.Error(ex.Message); }
        catch (Exception ex) { UiHelper.Error($"操作失败：{ex.Message}"); }
    }

    private void DoClear()
    {
        _txtUserName.Clear();
        _txtPassword.Clear();
        _cboPurview.SelectedIndex = -1;
        _txtUserName.ReadOnly = false;
        _dgv.ClearSelection();
    }
}
