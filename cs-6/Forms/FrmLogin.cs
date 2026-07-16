using HotelSys.BLL;
using HotelSys.Common;
using HotelSys.Models;

namespace HotelSys.Forms;

/// <summary>
/// 登录窗体（PRD 4.2.1）
/// 卡片式布局，垂直排列（标签在上、输入框在下），支持用户名、密码、身份选择
/// 区别于 cs-5：暖金色酒店主题 + 身份选择下拉框
/// </summary>
public class FrmLogin : Form
{
    private readonly UserManager _userMgr = new();

    private TextBox _txtUserName;
    private TextBox _txtPassword;
    private ComboBox _cboPurview;

    // 缓存 GDI 对象，避免 Paint 事件中反复创建导致句柄泄漏
    private readonly SolidBrush _cardAccentBrush = new(ThemeColor.Primary);
    private readonly Pen _cardBorderPen = new(ThemeColor.Border, 1);

    /// <summary>登录成功后的用户信息</summary>
    public UserInfo CurrentUser { get; private set; }

    public FrmLogin()
    {
        InitializeUI();
    }

    private void InitializeUI()
    {
        Text = "智慧酒店管理系统 - 登录";
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        ClientSize = new Size(520, 530);
        Font = UiHelper.DefaultFont;
        BackColor = ThemeColor.BgPage;

        // 卡片容器
        Panel card = new()
        {
            Size = new Size(400, 460),
            Location = new Point(60, 35),
            BackColor = ThemeColor.BgCard
        };
        card.Paint += (_, e) =>
        {
            // 顶部 6px 主色条（暖金色）
            e.Graphics.FillRectangle(_cardAccentBrush, 0, 0, card.Width, 6);
            e.Graphics.DrawRectangle(_cardBorderPen, 0, 0, card.Width - 1, card.Height - 1);
        };

        // 标题
        Label lblTitle = new()
        {
            Text = "智慧酒店管理系统",
            Font = new Font("Microsoft YaHei UI", 20F, FontStyle.Bold),
            Location = new Point(0, 35),
            Size = new Size(400, 36),
            TextAlign = ContentAlignment.MiddleCenter,
            ForeColor = ThemeColor.TextPrimary
        };

        // 副标题
        Label lblSubtitle = new()
        {
            Text = "Smart Hotel Management System",
            Font = ThemeColor.FontRegular,
            Location = new Point(0, 75),
            Size = new Size(400, 20),
            TextAlign = ContentAlignment.MiddleCenter,
            ForeColor = ThemeColor.TextSecondary
        };

        // 分隔线
        Panel separator = new()
        {
            Location = new Point(60, 105),
            Size = new Size(280, 1),
            BackColor = Color.FromArgb(238, 238, 238)
        };

        // 布局常量：卡片内左右边距 60px，输入区宽度 280px
        const int left = 60;
        const int inputW = 280;

        // ===== 用户名 =====
        Label lblUser = new()
        {
            Text = "用户名",
            Location = new Point(left, 125),
            Size = new Size(inputW, 22),
            Font = ThemeColor.FontRegular,
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = ThemeColor.TextSecondary
        };
        _txtUserName = UiHelper.CreateTextBox(inputW);
        _txtUserName.Location = new Point(left, 150);

        // ===== 密码 =====
        Label lblPwd = new()
        {
            Text = "密码",
            Location = new Point(left, 195),
            Size = new Size(inputW, 22),
            Font = ThemeColor.FontRegular,
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = ThemeColor.TextSecondary
        };
        _txtPassword = UiHelper.CreateTextBox(inputW, isPassword: true);
        _txtPassword.Location = new Point(left, 220);

        // ===== 身份选择 =====
        Label lblPurview = new()
        {
            Text = "身份",
            Location = new Point(left, 265),
            Size = new Size(inputW, 22),
            Font = ThemeColor.FontRegular,
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = ThemeColor.TextSecondary
        };
        _cboPurview = UiHelper.CreateComboBox(inputW);
        _cboPurview.Location = new Point(left, 290);
        _cboPurview.Items.AddRange(new object[] { BusinessConstants.ROLE_ADMIN, BusinessConstants.ROLE_USER });
        _cboPurview.SelectedIndex = 0;

        // ===== 按钮 =====
        Button btnLogin = UiHelper.CreatePrimaryButton("登 录", inputW, 40);
        btnLogin.Location = new Point(left, 345);
        btnLogin.Font = new Font("Microsoft YaHei UI", 11F, FontStyle.Bold);
        btnLogin.Click += BtnLogin_Click;

        Button btnExit = UiHelper.CreateSecondaryButton("退 出", inputW, 36);
        btnExit.Location = new Point(left, 395);
        btnExit.Click += (_, _) => Application.Exit();

        card.Controls.AddRange(new Control[] { lblTitle, lblSubtitle, separator,
            lblUser, _txtUserName, lblPwd, _txtPassword, lblPurview, _cboPurview,
            btnLogin, btnExit });
        Controls.Add(card);

        AcceptButton = btnLogin;
    }

    /// <summary>
    /// 登录验证：用户名+密码+身份三者必须匹配
    /// </summary>
    private void BtnLogin_Click(object sender, EventArgs e)
    {
        string userName = _txtUserName.Text.Trim();
        string password = _txtPassword.Text.Trim();
        string purview = _cboPurview.SelectedItem?.ToString();

        if (string.IsNullOrWhiteSpace(userName))
        {
            UiHelper.Error("请输入用户名");
            _txtUserName.Focus();
            return;
        }
        if (string.IsNullOrWhiteSpace(password))
        {
            UiHelper.Error("请输入密码");
            _txtPassword.Focus();
            return;
        }
        if (string.IsNullOrWhiteSpace(purview))
        {
            UiHelper.Error("请选择身份");
            return;
        }

        try
        {
            UserInfo user = _userMgr.Login(userName, password, purview);
            if (user == null)
            {
                UiHelper.Error("用户名、密码或身份不正确");
                _txtPassword.Clear();
                _txtPassword.Focus();
                return;
            }

            // 注意：CurrentUser 是 HotelSys.Common 静态类，需完全限定名避免与本窗体属性冲突
            HotelSys.Common.CurrentUser.SetUser(user);

            // 登录成功后自动过期超期预订（PRD AC-11.1）
            try { new ReservationManager().AutoExpire(); } catch { }

            LogManager.Record(user.UserName, BusinessConstants.LOG_LOGIN,
                $"用户 {user.UserName} 登录系统", $"身份:{user.UserPurview}");

            Hide();
            using FrmMain mainForm = new(user);
            mainForm.ShowDialog();
            Application.Exit();
        }
        catch (Exception ex)
        {
            UiHelper.Error($"登录失败：{ex.Message}");
        }
    }
}