using CampusStore.BLL;
using CampusStore.Common;
using CampusStore.Models;

namespace CampusStore.Forms;

/// <summary>
/// 用户管理窗体
/// </summary>
/// <remarks>
/// 提供系统用户的增删改查功能。构造时传入当前登录用户，
/// 删除操作会校验是否为自身账号（不允许删除当前登录用户）。
/// </remarks>
public partial class FrmUser : Form
{
    private readonly UserManager _userManager = new();

    /// <summary>当前登录用户（用于删除时校验不可删除自身）</summary>
    private readonly UserInfo _currentUser;

    /// <summary>当前选中的用户编号（0 表示未选中）</summary>
    private int _selectedUserID = 0;

    // === 列表区控件 ===
    private DataGridView _dgvUser;

    // === 编辑区控件（第一行）===
    private Label _lblLoginName;
    private TextBox _txtLoginName;
    private Label _lblPassword;
    private TextBox _txtPassword;
    private Label _lblConfirm;
    private TextBox _txtConfirm;

    // === 编辑区控件（第二行）===
    private Label _lblRole;
    private ComboBox _cboRole;

    // === 操作按钮 ===
    private Button _btnAdd;
    private Button _btnUpdate;
    private Button _btnDelete;
    private Button _btnClear;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="currentUser">当前登录用户，删除时校验不可删除自身</param>
    public FrmUser(UserInfo currentUser)
    {
        _currentUser = currentUser;
        InitializeComponent();
        BindEvents();
    }

    /// <summary>
    /// 初始化所有控件并添加到窗体
    /// </summary>
    private void InitializeComponent()
    {
        UiHelper.SetupForm(this, "用户管理", 700, 500);

        // === 列表区（Y=15，宽 660，高 350）===
        _dgvUser = new DataGridView
        {
            Location = new Point(15, 15),
            Size = new Size(660, 350)
        };
        UiHelper.SetGridStyle(_dgvUser);
        SetupGridColumns();

        // === 编辑区第一行（Y=375）：登录名、密码、确认密码 ===
        _lblLoginName = UiHelper.CreateLabel("登录名：", 15, 375);
        _txtLoginName = UiHelper.CreateTextBox(UiHelper.NextX(_lblLoginName), 372, 120);
        _lblPassword = UiHelper.CreateLabel("密码：", _txtLoginName.Location.X + _txtLoginName.Width + 10, 375);
        _txtPassword = UiHelper.CreateTextBox(UiHelper.NextX(_lblPassword), 372, 100, isPassword: true);
        _lblConfirm = UiHelper.CreateLabel("确认密码：", _txtPassword.Location.X + _txtPassword.Width + 10, 375);
        _txtConfirm = UiHelper.CreateTextBox(UiHelper.NextX(_lblConfirm), 372, 100, isPassword: true);

        // === 编辑区第二行（Y=410）：权限 ===
        _lblRole = UiHelper.CreateLabel("权限：", 15, 410);
        _cboRole = UiHelper.CreateComboBox(UiHelper.NextX(_lblRole), 407, 120);
        _cboRole.Items.AddRange(new object[] { "管理员", "店员" });

        // === 操作按钮区（Y=440，横排，间距 10）===
        _btnAdd = UiHelper.CreatePrimaryButton("添加", 15, 440, 75, 30);
        _btnUpdate = UiHelper.CreatePrimaryButton("修改", _btnAdd.Location.X + _btnAdd.Width + 10, 440, 75, 30);
        _btnDelete = UiHelper.CreateSecondaryButton("删除", _btnUpdate.Location.X + _btnUpdate.Width + 10, 440, 75, 30);
        _btnClear = UiHelper.CreateSecondaryButton("清空", _btnDelete.Location.X + _btnDelete.Width + 10, 440, 75, 30);

        // 添加所有控件到窗体
        Controls.AddRange(new Control[]
        {
            _dgvUser,
            _lblLoginName, _txtLoginName, _lblPassword, _txtPassword, _lblConfirm, _txtConfirm,
            _lblRole, _cboRole,
            _btnAdd, _btnUpdate, _btnDelete, _btnClear
        });
    }

    /// <summary>
    /// 配置 DataGridView 列（列标题中文化，不显示密码列）
    /// </summary>
    private void SetupGridColumns()
    {
        _dgvUser.AutoGenerateColumns = false;
        _dgvUser.Columns.AddRange(new DataGridViewColumn[]
        {
            new DataGridViewTextBoxColumn { DataPropertyName = "UserID", HeaderText = "用户编号", Name = "colUserID" },
            new DataGridViewTextBoxColumn { DataPropertyName = "LoginName", HeaderText = "登录名", Name = "colLoginName" },
            new DataGridViewTextBoxColumn { DataPropertyName = "Role", HeaderText = "角色", Name = "colRole" }
        });
    }

    /// <summary>
    /// 绑定控件事件
    /// </summary>
    private void BindEvents()
    {
        Load += FrmUser_Load;
        _dgvUser.SelectionChanged += DgvUser_SelectionChanged;
        _btnAdd.Click += BtnAdd_Click;
        _btnUpdate.Click += BtnUpdate_Click;
        _btnDelete.Click += BtnDelete_Click;
        _btnClear.Click += BtnClear_Click;
    }

    /// <summary>
    /// 窗体加载时绑定全部数据
    /// </summary>
    private void FrmUser_Load(object sender, EventArgs e)
    {
        LoadData();
    }

    /// <summary>
    /// 加载全部用户数据到列表
    /// </summary>
    private void LoadData()
    {
        try
        {
            _dgvUser.DataSource = _userManager.GetAll();
        }
        catch (Exception ex)
        {
            UiHelper.HandleException(ex, this);
        }
    }

    /// <summary>
    /// 选中行时填充登录名和权限（不填充密码，修改时需重新输入）
    /// </summary>
    private void DgvUser_SelectionChanged(object sender, EventArgs e)
    {
        // 列顺序：0=UserID, 1=LoginName, 2=Role
        if (_dgvUser.SelectedRows.Count == 0)
        {
            _selectedUserID = 0;
            return;
        }
        DataGridViewRow row = _dgvUser.SelectedRows[0];
        object idValue = row.Cells[0].Value;
        if (idValue == null || idValue == DBNull.Value)
        {
            _selectedUserID = 0;
            return;
        }
        _selectedUserID = Convert.ToInt32(idValue);
        _txtLoginName.Text = row.Cells[1].Value?.ToString() ?? "";
        // 通过 FindStringExact 匹配角色下拉项
        string role = row.Cells[2].Value?.ToString() ?? "";
        int roleIndex = _cboRole.FindStringExact(role);
        if (roleIndex >= 0)
            _cboRole.SelectedIndex = roleIndex;
        // 密码不回填，修改时需重新输入
        _txtPassword.Text = "";
        _txtConfirm.Text = "";
    }

    /// <summary>
    /// 添加用户
    /// </summary>
    private void BtnAdd_Click(object sender, EventArgs e)
    {
        try
        {
            string role = _cboRole.SelectedItem?.ToString() ?? "";
            _userManager.AddUser(_txtLoginName.Text.Trim(), _txtPassword.Text, _txtConfirm.Text, role);
            LoadData();
            ClearInputs();
            MessageBox.Show("添加成功", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            UiHelper.HandleException(ex, this);
        }
    }

    /// <summary>
    /// 修改用户（需先选中行，密码需重新输入）
    /// </summary>
    private void BtnUpdate_Click(object sender, EventArgs e)
    {
        try
        {
            if (_selectedUserID <= 0)
                throw new BusinessException("请先选择要修改的用户");
            // BLL 的 UpdateUser 不校验确认密码，UI 层补充校验
            if (_txtPassword.Text != _txtConfirm.Text)
                throw new BusinessException("两次输入的密码不一致");
            string role = _cboRole.SelectedItem?.ToString() ?? "";
            _userManager.UpdateUser(_selectedUserID, _txtPassword.Text, role);
            LoadData();
            ClearInputs();
            MessageBox.Show("修改成功", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            UiHelper.HandleException(ex, this);
        }
    }

    /// <summary>
    /// 删除用户（需先选中行并确认，不可删除当前登录用户）
    /// </summary>
    private void BtnDelete_Click(object sender, EventArgs e)
    {
        try
        {
            if (_selectedUserID <= 0)
                throw new BusinessException("请先选择要删除的用户");
            if (!UiHelper.ConfirmDelete(_txtLoginName.Text, this))
                return;
            // 传入当前登录用户 ID，BLL 会校验是否为自身账号
            _userManager.DeleteUser(_selectedUserID, _currentUser.UserID);
            LoadData();
            ClearInputs();
            MessageBox.Show("删除成功", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            UiHelper.HandleException(ex, this);
        }
    }

    /// <summary>
    /// 清空编辑区
    /// </summary>
    private void BtnClear_Click(object sender, EventArgs e)
    {
        ClearInputs();
    }

    /// <summary>
    /// 清空所有输入控件并重置选中状态
    /// </summary>
    private void ClearInputs()
    {
        _txtLoginName.Text = "";
        _txtPassword.Text = "";
        _txtConfirm.Text = "";
        _cboRole.SelectedIndex = -1;
        _selectedUserID = 0;
        _dgvUser.ClearSelection();
    }
}
