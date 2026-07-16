using LibrarySys.BLL;
using LibrarySys.Common;
using LibrarySys.Models;

namespace LibrarySys.Forms;

/// <summary>
/// 登录窗体：卡片式布局 + 主题色块 + 扁平输入框
/// 视觉风格：浅灰背景 + 白色卡片 + 主色顶部色条
/// </summary>
public class FrmLogin : Form
{
    private readonly UserBiz _userBiz = new();

    private TextBox _txtUserName;
    private TextBox _txtPassword;
    private ComboBox _cmbPurview; // 身份选择下拉框

    // 缓存 GDI 对象，避免 Paint 事件中反复创建
    private readonly SolidBrush _cardPrimaryBrush = new(ThemeColor.Primary);
    private readonly Pen _cardBorderPen = new(ThemeColor.Border, 1);

    public UserInfo CurrentUser { get; private set; }

    public FrmLogin()
    {
        DoubleBuffered = true;
        InitializeUI();
    }

    private void InitializeUI()
    {
        Text = "图书馆信息管理系统 - 登录";
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        ClientSize = new Size(480, 530);
        Font = UiHelper.DefaultFont;
        BackColor = ThemeColor.BgPage;

        // ===== 中央卡片 =====
        Panel card = new()
        {
            Size = new Size(360, 430),
            Location = new Point(60, 50),
            BackColor = ThemeColor.BgCard
        };
        // 卡片绘制：顶部主色条 + 浅灰描边
        card.Paint += (_, e) =>
        {
            // 顶部主色条（复用缓存的画刷和画笔）
            e.Graphics.FillRectangle(_cardPrimaryBrush, 0, 0, card.Width, 6);
            e.Graphics.DrawRectangle(_cardBorderPen, 0, 0, card.Width - 1, card.Height - 1);
        };
        Controls.Add(card);

        // ===== 卡片内：标题区域 =====
        Label lblTitle = new()
        {
            Text = "智慧图书馆",
            Font = new Font("Microsoft YaHei UI", 20F, FontStyle.Bold),
            ForeColor = ThemeColor.TextPrimary,
            Location = new Point(0, 40),
            Size = new Size(360, 36),
            TextAlign = ContentAlignment.MiddleCenter
        };
        card.Controls.Add(lblTitle);

        Label lblSubtitle = new()
        {
            Text = "Library Management System",
            Font = new Font("Microsoft YaHei UI", 9F),
            ForeColor = ThemeColor.TextSecondary,
            Location = new Point(0, 78),
            Size = new Size(360, 22),
            TextAlign = ContentAlignment.MiddleCenter
        };
        card.Controls.Add(lblSubtitle);

        // 分隔线（细色条）
        Panel divider = new()
        {
            BackColor = Color.FromArgb(238, 238, 238),
            Location = new Point(40, 115),
            Size = new Size(280, 1)
        };
        card.Controls.Add(divider);

        // ===== 表单字段区域 =====
        _txtUserName = UiHelper.CreateTextBox(280);
        _txtPassword = UiHelper.CreateTextBox(280, isPassword: true);

        // 用户名
        Label lblUser = new()
        {
            Text = "用户名",
            Font = ThemeColor.FontRegular,
            ForeColor = ThemeColor.TextSecondary,
            Location = new Point(40, 140),
            Size = new Size(280, 20)
        };
        card.Controls.Add(lblUser);
        _txtUserName.Location = new Point(40, 165);
        card.Controls.Add(_txtUserName);

        // 密码
        Label lblPwd = new()
        {
            Text = "密码",
            Font = ThemeColor.FontRegular,
            ForeColor = ThemeColor.TextSecondary,
            Location = new Point(40, 200),
            Size = new Size(280, 20)
        };
        card.Controls.Add(lblPwd);
        _txtPassword.Location = new Point(40, 225);
        card.Controls.Add(_txtPassword);

        // 身份选择
        Label lblPurview = new()
        {
            Text = "身份",
            Font = ThemeColor.FontRegular,
            ForeColor = ThemeColor.TextSecondary,
            Location = new Point(40, 260),
            Size = new Size(280, 20)
        };
        card.Controls.Add(lblPurview);
        _cmbPurview = UiHelper.CreateComboBox(280);
        _cmbPurview.Location = new Point(40, 285);
        _cmbPurview.Items.AddRange(new object[] { "管理员", "普通用户" });
        _cmbPurview.SelectedIndex = 0; // 默认选中管理员
        card.Controls.Add(_cmbPurview);

        // ===== 按钮 =====
        Button btnLogin = UiHelper.CreatePrimaryButton("登 录", 280, 40);
        btnLogin.Location = new Point(40, 340);
        btnLogin.Click += BtnLogin_Click;
        card.Controls.Add(btnLogin);

        Button btnExit = UiHelper.CreateSecondaryButton("退 出", 280, 36);
        btnExit.Location = new Point(40, 385);
        btnExit.Click += (_, _) => Application.Exit();
        card.Controls.Add(btnExit);

        // ===== 底部版权 =====
        Label lblCopyright = new()
        {
            Text = "© 2026 智慧图书馆管理系统  课程设计作品",
            Font = new Font("Microsoft YaHei UI", 8F),
            ForeColor = ThemeColor.TextPlaceholder,
            Location = new Point(0, 495),
            Size = new Size(480, 20),
            TextAlign = ContentAlignment.MiddleCenter
        };
        Controls.Add(lblCopyright);

        AcceptButton = btnLogin;
    }

    private void BtnLogin_Click(object sender, EventArgs e)
    {
        string userName = _txtUserName.Text.Trim();
        string password = _txtPassword.Text;
        string purview = _cmbPurview.SelectedItem?.ToString() ?? "";

        if (string.IsNullOrWhiteSpace(userName)) { UiHelper.Error("请输入用户名"); _txtUserName.Focus(); return; }
        if (string.IsNullOrWhiteSpace(password)) { UiHelper.Error("请输入密码"); _txtPassword.Focus(); return; }
        if (string.IsNullOrWhiteSpace(purview)) { UiHelper.Error("请选择身份"); _cmbPurview.Focus(); return; }

        try
        {
            // 验证用户名、密码和身份三者匹配
            UserInfo user = _userBiz.Login(userName, password, purview);
            if (user == null)
            {
                UiHelper.Error("用户名、密码或身份不正确");
                _txtPassword.Clear();
                _txtPassword.Focus();
                return;
            }
            CurrentUser = user;
            Hide();
            using FrmMain mainForm = new(user);
            mainForm.ShowDialog();
            Application.Exit();
        }
        catch (Exception ex) { UiHelper.Error($"登录失败：{ex.Message}"); }
    }
}
