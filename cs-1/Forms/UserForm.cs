using LibraryManagement.BLL;
using LibraryManagement.Models;

namespace LibraryManagement.Forms;

/// <summary>
/// 用户管理窗体：用户的增删改查及权限分配
/// </summary>
public class UserForm : Form
{
    private readonly UserService _userService = new();

    private readonly DataGridView _dgvUsers;
    private readonly TextBox _txtUserName;
    private readonly TextBox _txtPassword;
    private readonly TextBox _txtConfirmPassword;
    private readonly ComboBox _cmbRole;
    private readonly GroupBox _grpInput;
    private readonly Button _btnAdd;
    private readonly Button _btnUpdate;
    private readonly Button _btnDelete;
    private readonly Button _btnClear;
    private readonly Button _btnBack;

    /// <summary>当前登录用户</summary>
    private readonly User _currentUser;

    /// <summary>
    /// 构造用户管理窗体
    /// </summary>
    /// <param name="currentUser">当前登录用户，用于删除时校验</param>
    public UserForm(User currentUser)
    {
        _currentUser = currentUser;

        // 窗体基本属性
        Text = "用户管理";
        Size = new Size(700, 520);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        BackColor = Color.FromArgb(245, 247, 250);

        // DataGridView 用户列表
        _dgvUsers = new DataGridView
        {
            Location = new Point(15, 15),
            Size = new Size(650, 250),
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            ReadOnly = true,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            BackgroundColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle
        };
        _dgvUsers.SelectionChanged += DgvUsers_SelectionChanged;

        // 输入区域
        _grpInput = new GroupBox
        {
            Text = "用户信息",
            Font = new Font("Microsoft YaHei UI", 9F),
            Location = new Point(15, 275),
            Size = new Size(650, 150)
        };

        Label lblUserName = new() { Text = "用户名：", Location = new Point(20, 30), AutoSize = true, Font = new Font("Microsoft YaHei UI", 9F) };
        _txtUserName = new TextBox { Location = new Point(90, 27), Size = new Size(150, 25), Font = new Font("Microsoft YaHei UI", 9F) };

        Label lblPassword = new() { Text = "密码：", Location = new Point(260, 30), AutoSize = true, Font = new Font("Microsoft YaHei UI", 9F) };
        _txtPassword = new TextBox { Location = new Point(320, 27), Size = new Size(150, 25), Font = new Font("Microsoft YaHei UI", 9F), UseSystemPasswordChar = true };

        Label lblConfirm = new() { Text = "确认密码：", Location = new Point(20, 65), AutoSize = true, Font = new Font("Microsoft YaHei UI", 9F) };
        _txtConfirmPassword = new TextBox { Location = new Point(90, 62), Size = new Size(150, 25), Font = new Font("Microsoft YaHei UI", 9F), UseSystemPasswordChar = true };

        Label lblRole = new() { Text = "权限：", Location = new Point(260, 65), AutoSize = true, Font = new Font("Microsoft YaHei UI", 9F) };
        _cmbRole = new ComboBox
        {
            Location = new Point(320, 62),
            Size = new Size(150, 25),
            Font = new Font("Microsoft YaHei UI", 9F),
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        _cmbRole.Items.AddRange(new object[] { BusinessConstants.ROLE_ADMIN, BusinessConstants.ROLE_USER });

        _btnAdd = CreateButton("添加", 20, 100);
        _btnAdd.Click += BtnAdd_Click;
        _btnUpdate = CreateButton("修改", 110, 100);
        _btnUpdate.Click += BtnUpdate_Click;
        _btnDelete = CreateButton("删除", 200, 100);
        _btnDelete.Click += BtnDelete_Click;
        _btnClear = CreateButton("清空", 290, 100);
        _btnClear.Click += (s, e) => ClearInput();

        _grpInput.Controls.AddRange(new Control[] { lblUserName, _txtUserName, lblPassword, _txtPassword, lblConfirm, _txtConfirmPassword, lblRole, _cmbRole, _btnAdd, _btnUpdate, _btnDelete, _btnClear });

        // 返回按钮
        _btnBack = CreateButton("返回", 560, 440);
        _btnBack.Click += (s, e) => Close();

        Controls.AddRange(new Control[] { _dgvUsers, _grpInput, _btnBack });

        // 窗体加载时读取用户列表
        Load += (s, e) => LoadData();
    }

    /// <summary>
    /// 加载用户列表到 DataGridView
    /// </summary>
    private void LoadData()
    {
        try
        {
            List<User> users = _userService.GetAllUsers();
            _dgvUsers.DataSource = users;
            // 设置列标题
            _dgvUsers.Columns[nameof(User.UserName)].HeaderText = "用户名";
            _dgvUsers.Columns[nameof(User.UserPurview)].HeaderText = "权限";
            // 隐藏密码列，避免暴露哈希值
            _dgvUsers.Columns[nameof(User.UserPassword)].Visible = false;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"加载数据失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    /// <summary>
    /// 列表选中行变化时回填输入框
    /// 选中后用户名只读，密码与确认密码清空（修改时留空表示不改密码）
    /// </summary>
    private void DgvUsers_SelectionChanged(object sender, EventArgs e)
    {
        if (_dgvUsers.CurrentRow?.DataBoundItem is User user)
        {
            _txtUserName.Text = user.UserName;
            // 主键不可编辑
            _txtUserName.ReadOnly = true;
            _txtUserName.BackColor = Color.FromArgb(240, 240, 240);
            // 密码框清空，留空表示不修改密码
            _txtPassword.Clear();
            _txtConfirmPassword.Clear();
            _cmbRole.SelectedItem = user.UserPurview;
        }
    }

    /// <summary>
    /// 添加按钮点击事件
    /// </summary>
    private void BtnAdd_Click(object sender, EventArgs e)
    {
        // 处于选中态时用户名只读，需先清空才能添加新用户
        if (_txtUserName.ReadOnly)
        {
            MessageBox.Show("请先点击清空按钮再添加新用户", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        try
        {
            string role = _cmbRole.SelectedItem?.ToString();
            _userService.AddUser(_txtUserName.Text.Trim(), _txtPassword.Text, _txtConfirmPassword.Text, role);
            MessageBox.Show("添加成功", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            ClearInput();
            LoadData();
        }
        catch (BusinessException ex)
        {
            MessageBox.Show(ex.Message, "操作失败", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"操作失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    /// <summary>
    /// 修改按钮点击事件
    /// </summary>
    private void BtnUpdate_Click(object sender, EventArgs e)
    {
        if (_dgvUsers.CurrentRow?.DataBoundItem is not User user)
        {
            MessageBox.Show("请先选择要修改的用户", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        string password = _txtPassword.Text;
        string confirmPassword = _txtConfirmPassword.Text;
        // BLL 的 UpdateUser 不校验确认密码，需在 UI 层校验一致性
        if (!string.IsNullOrEmpty(password) && password != confirmPassword)
        {
            MessageBox.Show("两次输入的密码不一致", "操作失败", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        try
        {
            string role = _cmbRole.SelectedItem?.ToString();
            _userService.UpdateUser(user.UserName, password, role);
            MessageBox.Show("修改成功", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            LoadData();
        }
        catch (BusinessException ex)
        {
            MessageBox.Show(ex.Message, "操作失败", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"操作失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    /// <summary>
    /// 删除按钮点击事件
    /// </summary>
    private void BtnDelete_Click(object sender, EventArgs e)
    {
        if (_dgvUsers.CurrentRow?.DataBoundItem is not User user)
        {
            MessageBox.Show("请先选择要删除的用户", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        if (MessageBox.Show($"确定要删除用户《{user.UserName}》吗？", "确认删除", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
        {
            return;
        }

        try
        {
            // 删除当前登录用户由 BLL 抛 BusinessException 处理
            _userService.DeleteUser(user.UserName, _currentUser.UserName);
            MessageBox.Show("删除成功", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            ClearInput();
            LoadData();
        }
        catch (BusinessException ex)
        {
            MessageBox.Show(ex.Message, "操作失败", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"操作失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    /// <summary>
    /// 清空输入框并重置用户名可编辑状态
    /// </summary>
    private void ClearInput()
    {
        _txtUserName.Clear();
        _txtPassword.Clear();
        _txtConfirmPassword.Clear();
        _cmbRole.SelectedIndex = -1;
        _txtUserName.ReadOnly = false;
        _txtUserName.BackColor = Color.White;
        if (_dgvUsers.CurrentRow != null)
        {
            _dgvUsers.ClearSelection();
        }
    }

    /// <summary>
    /// 创建统一风格的按钮（蓝色背景、白色文字、扁平样式）
    /// </summary>
    private static Button CreateButton(string text, int x, int y)
    {
        return new Button
        {
            Text = text,
            Font = new Font("Microsoft YaHei UI", 9F),
            Size = new Size(80, 30),
            Location = new Point(x, y),
            BackColor = Color.FromArgb(64, 158, 255),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            TextAlign = ContentAlignment.MiddleCenter
        };
    }
}
