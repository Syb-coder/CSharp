using CampusShop.BLL;
using CampusShop.Models;

namespace CampusShop.Forms;

/// <summary>
/// 用户管理窗体：用户的增删改查及权限分配
/// 仅管理员可访问此窗体
/// </summary>
public class UserForm : Form
{
    private readonly UserService _userService = new();
    private readonly DataGridView _dgvUsers;
    private readonly TextBox _txtUserName;
    private readonly TextBox _txtPassword;
    private readonly TextBox _txtConfirmPassword;
    private readonly ComboBox _cmbRole;
    private readonly Panel _grpInput;
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
            BorderStyle = BorderStyle.FixedSingle,
            RowHeadersVisible = false
        };
        _dgvUsers.SelectionChanged += DgvUsers_SelectionChanged;

        // 输入区域（Panel 无圆角边框遮盖）
        _grpInput = new Panel
        {
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = Color.FromArgb(245, 247, 250),
            Location = new Point(15, 275),
            Size = new Size(650, 150)
        };
        Label lblGrpTitle = new()
        {
            Text = "用户信息",
            Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold),
            ForeColor = Color.FromArgb(64, 158, 255),
            Location = new Point(10, 5),
            AutoSize = true,
            BackColor = Color.Transparent
        };

        // Label 宽度=AutoSize实际值：3字+冒号=65px, 4字+冒号=100px
        Label lblUserName = new() { Text = "用户名：", Location = new Point(15, 30), AutoSize = false, Size = new Size(65, 20), TextAlign = ContentAlignment.MiddleLeft, Font = new Font("Microsoft YaHei UI", 9F), BackColor = Color.Transparent };
        _txtUserName = new TextBox { Location = new Point(85, 27), Size = new Size(150, 25), Font = new Font("Microsoft YaHei UI", 9F) };

        Label lblPassword = new() { Text = "密码：", Location = new Point(250, 30), AutoSize = false, Size = new Size(65, 20), TextAlign = ContentAlignment.MiddleLeft, Font = new Font("Microsoft YaHei UI", 9F), BackColor = Color.Transparent };
        _txtPassword = new TextBox { Location = new Point(320, 27), Size = new Size(150, 25), Font = new Font("Microsoft YaHei UI", 9F), UseSystemPasswordChar = true };

        Label lblConfirm = new() { Text = "确认密码：", Location = new Point(15, 65), AutoSize = false, Size = new Size(100, 20), TextAlign = ContentAlignment.MiddleLeft, Font = new Font("Microsoft YaHei UI", 9F), BackColor = Color.Transparent };
        _txtConfirmPassword = new TextBox { Location = new Point(120, 62), Size = new Size(150, 25), Font = new Font("Microsoft YaHei UI", 9F), UseSystemPasswordChar = true };

        Label lblRole = new() { Text = "权限：", Location = new Point(285, 65), AutoSize = false, Size = new Size(65, 20), TextAlign = ContentAlignment.MiddleLeft, Font = new Font("Microsoft YaHei UI", 9F), BackColor = Color.Transparent };
        _cmbRole = new ComboBox
        {
            Location = new Point(355, 62),
            Size = new Size(150, 25),
            Font = new Font("Microsoft YaHei UI", 9F),
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        _cmbRole.Items.AddRange(new object[] { BusinessConstants.ROLE_ADMIN, BusinessConstants.ROLE_USER });

        _btnAdd = CreateButton("添加", 15, 100);
        _btnAdd.Click += BtnAdd_Click;
        _btnUpdate = CreateButton("修改", 105, 100);
        _btnUpdate.Click += BtnUpdate_Click;
        _btnDelete = CreateButton("删除", 195, 100);
        _btnDelete.Click += BtnDelete_Click;
        _btnClear = CreateButton("清空", 285, 100);
        _btnClear.Click += (s, e) => ClearInput();

        _grpInput.Controls.AddRange(new Control[] { lblGrpTitle, lblUserName, _txtUserName, lblPassword, _txtPassword, lblConfirm, _txtConfirmPassword, lblRole, _cmbRole, _btnAdd, _btnUpdate, _btnDelete, _btnClear });

        // 返回按钮（Y=430 适配 ClientSize 464，避免超出底部）
        _btnBack = CreateButton("返回", 560, 430);
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
            // 隐藏密码列，避免哈希值显示在界面上
            _dgvUsers.Columns[nameof(User.UserPassword)].Visible = false;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"加载数据失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    /// <summary>
    /// 列表选中行变化时回填输入框
    /// </summary>
    private void DgvUsers_SelectionChanged(object sender, EventArgs e)
    {
        if (_dgvUsers.CurrentRow?.DataBoundItem is not User user)
        {
            return;
        }

        _txtUserName.Text = user.UserName;
        // 密码框留空：修改时用户需重新输入新密码
        _txtPassword.Clear();
        _txtConfirmPassword.Clear();
        _cmbRole.SelectedItem = user.UserPurview;

        // 用户名为主键，修改时不可编辑
        _txtUserName.ReadOnly = true;
    }

    /// <summary>
    /// 添加按钮点击事件
    /// </summary>
    private void BtnAdd_Click(object sender, EventArgs e)
    {
        try
        {
            _userService.AddUser(
                _txtUserName.Text.Trim(),
                _txtPassword.Text,
                _txtConfirmPassword.Text,
                _cmbRole.SelectedItem?.ToString() ?? "");
            MessageBox.Show("添加成功", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            ClearInput();
            LoadData();
        }
        catch (BusinessException ex)
        {
            MessageBox.Show(ex.Message, "校验失败", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"添加失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    /// <summary>
    /// 修改按钮点击事件
    /// 修改时密码留空表示不修改密码，仅修改权限
    /// </summary>
    private void BtnUpdate_Click(object sender, EventArgs e)
    {
        if (_dgvUsers.CurrentRow == null)
        {
            MessageBox.Show("请先选择要修改的用户", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        try
        {
            // 密码留空表示不修改密码：UpdateUser 内部会保留原密码哈希
            _userService.UpdateUser(
                _txtUserName.Text.Trim(),
                _txtPassword.Text,
                _cmbRole.SelectedItem?.ToString() ?? "");
            MessageBox.Show("修改成功", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            LoadData();
        }
        catch (BusinessException ex)
        {
            MessageBox.Show(ex.Message, "校验失败", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"修改失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    /// <summary>
    /// 删除按钮点击事件
    /// </summary>
    private void BtnDelete_Click(object sender, EventArgs e)
    {
        if (_dgvUsers.CurrentRow == null)
        {
            MessageBox.Show("请先选择要删除的用户", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        string userName = _txtUserName.Text.Trim();
        if (string.IsNullOrEmpty(userName))
        {
            MessageBox.Show("用户名为空，无法删除", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        // 二次确认：删除用户是不可逆操作
        DialogResult result = MessageBox.Show(
            $"确认删除用户 [{userName}] 吗？",
            "确认删除",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);
        if (result != DialogResult.Yes)
        {
            return;
        }

        try
        {
            _userService.DeleteUser(userName, _currentUser.UserName);
            MessageBox.Show("删除成功", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            ClearInput();
            LoadData();
        }
        catch (BusinessException ex)
        {
            MessageBox.Show(ex.Message, "校验失败", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"删除失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    /// <summary>
    /// 清空输入框，恢复新增模式
    /// </summary>
    private void ClearInput()
    {
        _txtUserName.Clear();
        _txtPassword.Clear();
        _txtConfirmPassword.Clear();
        _cmbRole.SelectedIndex = -1;
        _txtUserName.ReadOnly = false;
        _txtUserName.Focus();
    }

    /// <summary>
    /// 创建统一风格的操作按钮
    /// </summary>
    private static Button CreateButton(string text, int x, int y)
    {
        return new Button
        {
            Text = text,
            Font = new Font("Microsoft YaHei UI", 9F),
            Size = new Size(80, 28),
            Location = new Point(x, y),
            BackColor = Color.FromArgb(64, 158, 255),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            TextAlign = ContentAlignment.MiddleCenter
        };
    }
}
