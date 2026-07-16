using LibraryManagement.BLL;
using LibraryManagement.Models;

namespace LibraryManagement.Forms;

/// <summary>
/// 登录窗体：用户输入用户名、密码并选择身份，验证通过后进入主窗体
/// </summary>
public class LoginForm : Form
{
    private readonly TextBox _txtUserName;
    private readonly TextBox _txtPassword;
    private readonly ComboBox _cmbRole;
    private readonly Button _btnLogin;
    private readonly Button _btnExit;
    private readonly Label _lblTitle;
    private readonly Label _lblUserName;
    private readonly Label _lblPassword;
    private readonly Label _lblRole;

    private readonly UserService _userService = new();

    /// <summary>
    /// 构造登录窗体，初始化界面控件
    /// </summary>
    public LoginForm()
    {
        // 窗体基本属性
        Text = "图书馆信息管理系统 - 登录";
        Size = new Size(420, 340);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        BackColor = Color.FromArgb(245, 247, 250);

        // 标题标签
        _lblTitle = new Label
        {
            Text = "图书馆信息管理系统",
            Font = new Font("Microsoft YaHei UI", 16F, FontStyle.Bold),
            ForeColor = Color.FromArgb(51, 51, 51),
            TextAlign = ContentAlignment.MiddleCenter,
            Size = new Size(380, 40),
            Location = new Point(15, 25)
        };

        // 用户名标签
        _lblUserName = new Label
        {
            Text = "用户名：",
            Font = new Font("Microsoft YaHei UI", 10F),
            Size = new Size(80, 25),
            Location = new Point(60, 95),
            TextAlign = ContentAlignment.MiddleLeft
        };

        // 用户名输入框
        _txtUserName = new TextBox
        {
            Font = new Font("Microsoft YaHei UI", 10F),
            Size = new Size(200, 25),
            Location = new Point(145, 93)
        };

        // 密码标签
        _lblPassword = new Label
        {
            Text = "密码：",
            Font = new Font("Microsoft YaHei UI", 10F),
            Size = new Size(80, 25),
            Location = new Point(60, 135),
            TextAlign = ContentAlignment.MiddleLeft
        };

        // 密码输入框（掩码显示）
        _txtPassword = new TextBox
        {
            Font = new Font("Microsoft YaHei UI", 10F),
            Size = new Size(200, 25),
            Location = new Point(145, 133),
            // 使用系统密码字符掩码，防止旁人窥屏获取明文密码
            UseSystemPasswordChar = true
        };

        // 身份标签
        _lblRole = new Label
        {
            Text = "身份：",
            Font = new Font("Microsoft YaHei UI", 10F),
            Size = new Size(80, 25),
            Location = new Point(60, 175),
            TextAlign = ContentAlignment.MiddleLeft
        };

        // 身份下拉框
        _cmbRole = new ComboBox
        {
            Font = new Font("Microsoft YaHei UI", 10F),
            Size = new Size(200, 25),
            Location = new Point(145, 173),
            // DropDownList 禁止手动输入，限定只能选择预设身份，避免非法角色值
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        _cmbRole.Items.AddRange(new object[] { "管理员", "普通用户" });

        // 登录按钮
        _btnLogin = new Button
        {
            Text = "登录",
            Font = new Font("Microsoft YaHei UI", 10F),
            Size = new Size(95, 35),
            Location = new Point(145, 225),
            BackColor = Color.FromArgb(64, 158, 255),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        _btnLogin.FlatAppearance.BorderSize = 0;
        _btnLogin.Click += BtnLogin_Click;

        // 退出按钮
        _btnExit = new Button
        {
            Text = "退出",
            Font = new Font("Microsoft YaHei UI", 10F),
            Size = new Size(95, 35),
            Location = new Point(250, 225),
            BackColor = Color.FromArgb(200, 200, 200),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        _btnExit.FlatAppearance.BorderSize = 0;
        _btnExit.Click += BtnExit_Click;

        // 添加控件到窗体
        Controls.AddRange(new Control[]
        {
            _lblTitle, _lblUserName, _txtUserName,
            _lblPassword, _txtPassword,
            _lblRole, _cmbRole,
            _btnLogin, _btnExit
        });

        // 登录窗体是应用入口，关闭它意味着用户退出系统，故直接终止进程
        FormClosed += (s, e) => Application.Exit();
    }

    /// <summary>
    /// 登录按钮点击事件：校验输入并验证用户身份
    /// </summary>
    private void BtnLogin_Click(object sender, EventArgs e)
    {
        try
        {
            string userName = _txtUserName.Text.Trim();
            // 密码不做 Trim，因为密码中可能合法包含首尾空格
            string password = _txtPassword.Text;
            string role = _cmbRole.SelectedItem?.ToString();

            User user = _userService.Login(userName, password, role);

            // 验证通过，隐藏登录窗体（而非关闭），保留实例以便主窗体关闭后再处理
            Hide();
            MainForm mainForm = new(user);
            // 主窗体关闭时根据关闭方式决定后续行为：
            // - 退出登录（IsLogout=true）：重新显示登录界面，支持切换账号
            // - 窗体关闭按钮（IsLogout=false）：关闭登录窗体，触发 Application.Exit() 退出应用
            mainForm.FormClosed += (s, e) =>
            {
                if (mainForm.IsLogout)
                {
                    // 退出登录：重新显示登录界面并清空输入，等待重新登录
                    Show();
                    _txtPassword.Clear();
                    _txtUserName.SelectAll();
                    _txtUserName.Focus();
                }
                else
                {
                    // 退出应用：关闭登录窗体，触发 FormClosed → Application.Exit()
                    Close();
                }
            };
            mainForm.Show();
        }
        // BusinessException 是业务层校验失败（如密码错误、角色不匹配），属于用户可纠正的错误
        catch (BusinessException ex)
        {
            MessageBox.Show(ex.Message, "登录失败", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _txtPassword.Clear();
            _txtPassword.Focus();
        }
        // Exception 捕获系统级错误（如数据库连接失败），与业务错误区分提示
        catch (Exception ex)
        {
            MessageBox.Show($"数据库连接失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            _txtPassword.Clear();
            _txtPassword.Focus();
        }
    }

    /// <summary>
    /// 退出按钮点击事件：关闭应用
    /// </summary>
    private void BtnExit_Click(object sender, EventArgs e)
    {
        Application.Exit();
    }
}
