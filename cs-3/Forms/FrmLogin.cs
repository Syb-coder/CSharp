using CampusMart.BLL;
using CampusMart.Common;
using CampusMart.Models;

namespace CampusMart.Forms;

/// <summary>
/// 登录窗体：用户输入用户名、密码并选择角色，验证通过后进入主窗体
/// </summary>
/// <remarks>
/// Label 宽度使用 UiHelper 动态测量，杜绝硬编码像素在不同 DPI/字体环境下导致文字截断。
/// 三个标签右对齐，输入框左对齐于统一 X 坐标，保证视觉整齐。
/// </remarks>
public class FrmLogin : Form
{
    private readonly TextBox _txtLoginName;
    private readonly TextBox _txtPassword;
    private readonly ComboBox _cmbRole;
    private readonly Button _btnLogin;
    private readonly Button _btnExit;
    private readonly Label _lblTitle;

    private readonly UserBiz _userBiz = new();

    /// <summary>
    /// 构造登录窗体，初始化界面控件
    /// </summary>
    public FrmLogin()
    {
        // 窗体基本属性
        Text = "校园易购信息管理系统 - 登录";
        Size = new Size(450, 340);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        BackColor = Color.FromArgb(245, 247, 250);

        Font titleFont = new("Microsoft YaHei UI", 16F, FontStyle.Bold);
        Font labelFont = new("Microsoft YaHei UI", 10F);
        Font inputFont = new("Microsoft YaHei UI", 10F);
        Font btnFont = new("Microsoft YaHei UI", 10F);

        // 标题标签：动态测量宽度，居中显示
        _lblTitle = new Label
        {
            Text = "校园易购信息管理系统",
            Font = titleFont,
            ForeColor = Color.FromArgb(51, 51, 51),
            TextAlign = ContentAlignment.MiddleCenter,
            AutoSize = false
        };
        int titleWidth = TextRenderer.MeasureText(_lblTitle.Text, titleFont).Width + 10;
        _lblTitle.Size = new Size(titleWidth, 40);
        _lblTitle.Location = new Point((ClientSize.Width - titleWidth) / 2, 25);

        // 输入区统一 X 坐标（标签右边缘对齐于此 - 5）
        const int inputX = 170;
        const int labelRightX = inputX - 8;

        // 用户名标签（右对齐）
        Label lblLoginName = UiHelper.CreateLabelRightAligned("用户名：", labelRightX, 95, labelFont);
        _txtLoginName = new TextBox
        {
            Font = inputFont,
            Size = new Size(200, 25),
            Location = new Point(inputX, 93)
        };

        // 密码标签（右对齐）
        Label lblPassword = UiHelper.CreateLabelRightAligned("密码：", labelRightX, 135, labelFont);
        _txtPassword = new TextBox
        {
            Font = inputFont,
            Size = new Size(200, 25),
            Location = new Point(inputX, 133),
            UseSystemPasswordChar = true
        };

        // 角色标签（右对齐）
        Label lblRole = UiHelper.CreateLabelRightAligned("角色：", labelRightX, 175, labelFont);
        _cmbRole = new ComboBox
        {
            Font = inputFont,
            Size = new Size(200, 25),
            Location = new Point(inputX, 173),
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        _cmbRole.Items.AddRange(new object[] { RoleConstants.ADMIN, RoleConstants.OPERATOR });

        // 按钮居中排列于输入框下方
        const int btnY = 225;
        const int btnWidth = 95;
        const int btnGap = 15;
        int btnTotalWidth = btnWidth * 2 + btnGap;
        int btnStartX = inputX + (200 - btnTotalWidth) / 2;

        _btnLogin = UiHelper.CreateButton("登录", btnStartX, btnY, btnFont, btnWidth, 35);
        _btnLogin.Click += BtnLogin_Click;

        _btnExit = UiHelper.CreateButton("退出", btnStartX + btnWidth + btnGap, btnY, btnFont, btnWidth, 35);
        _btnExit.BackColor = Color.FromArgb(200, 200, 200);
        _btnExit.Click += BtnExit_Click;

        // 添加控件到窗体
        Controls.AddRange(new Control[]
        {
            _lblTitle, lblLoginName, _txtLoginName,
            lblPassword, _txtPassword,
            lblRole, _cmbRole,
            _btnLogin, _btnExit
        });

        // 登录窗体是应用入口，关闭它意味着用户退出系统
        FormClosed += (s, e) => Application.Exit();
    }

    /// <summary>
    /// 登录按钮点击事件：校验输入并验证用户身份
    /// </summary>
    private void BtnLogin_Click(object sender, EventArgs e)
    {
        try
        {
            string loginName = _txtLoginName.Text.Trim();
            string password = _txtPassword.Text;
            string role = _cmbRole.SelectedItem?.ToString();

            UserInfo user = _userBiz.Login(loginName, password, role);

            Hide();
            FrmMain mainForm = new(user);
            mainForm.FormClosed += (s, e) => Close();
            mainForm.Show();
        }
        catch (BusinessException ex)
        {
            MessageBox.Show(ex.Message, "登录失败", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _txtPassword.Clear();
            _txtPassword.Focus();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"系统错误：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
