using System;
using System.Drawing;
using System.Windows.Forms;
using CampusStore.BLL;
using CampusStore.Common;
using CampusStore.Models;

namespace CampusStore.Forms
{
    /// <summary>
    /// 登录窗体
    /// </summary>
    /// <remarks>
    /// 职责：验证用户身份，成功后进入主窗体。
    /// 登录成功时隐藏本窗体（不关闭），由主窗体接管交互；
    /// 用户直接关闭本窗体时调用 Application.Exit() 退出应用。
    /// </remarks>
    public partial class FrmLogin : Form
    {
        // ===== 控件字段 =====
        private Label lblTitle;
        private Label lblUserName;
        private Label lblPassword;
        private Label lblRole;
        private TextBox txtUserName;
        private TextBox txtPassword;
        private ComboBox cmbRole;
        private Button btnLogin;
        private Button btnExit;

        /// <summary>
        /// 登录是否成功标志
        /// </summary>
        /// <remarks>
        /// FormClosed 事件据此判断：未成功登录而关闭窗体时退出整个应用，
        /// 避免出现"登录窗体已关但进程未退"的僵死状态。
        /// </remarks>
        private bool _loginSucceeded = false;

        /// <summary>
        /// 构造函数：初始化控件并绑定事件
        /// </summary>
        public FrmLogin()
        {
            InitializeComponent();
            BindEvents();
        }

        /// <summary>
        /// 初始化所有控件属性与布局
        /// </summary>
        private void InitializeComponent()
        {
            UiHelper.SetupForm(this, "校园易购信息管理系统 - 登录", 450, 320);
            // 登录窗体需屏幕居中（SetupForm 默认 CenterParent）
            this.StartPosition = FormStartPosition.CenterScreen;

            // 标题标签：加粗 14pt，横跨窗体宽度并居中显示
            // 说明：标题需特殊字体，无法复用 UiHelper.CreateLabel（其按 9pt 测量宽度），
            //       此处采用 AutoSize=false + 全宽 + TextAlign=MiddleCenter，杜绝文字被遮挡。
            lblTitle = new Label
            {
                Text = "校园易购信息管理系统",
                Font = new Font("Microsoft YaHei UI", 14F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                AutoSize = false,
                Size = new Size(this.ClientSize.Width, 35),
                Location = new Point(0, 30),
                BackColor = Color.Transparent
            };

            // 用户名行（Y=90，Label 起始 X=60）
            lblUserName = UiHelper.CreateLabel("用户名:", 60, 90);
            txtUserName = UiHelper.CreateTextBox(UiHelper.NextX(lblUserName), 90);

            // 密码行（Y=130）
            lblPassword = UiHelper.CreateLabel("密码:", 60, 130);
            txtPassword = UiHelper.CreateTextBox(UiHelper.NextX(lblPassword), 130, isPassword: true);

            // 身份行（Y=170）
            lblRole = UiHelper.CreateLabel("身份:", 60, 170);
            cmbRole = UiHelper.CreateComboBox(UiHelper.NextX(lblRole), 170);
            cmbRole.Items.AddRange(new object[] { "管理员", "店员" });
            cmbRole.SelectedIndex = 0;

            // 按钮行（Y=215）
            btnLogin = UiHelper.CreatePrimaryButton("登录", 150, 215, 100, 30);
            btnExit = UiHelper.CreateSecondaryButton("退出", 260, 215, 100, 30);

            this.Controls.AddRange(new Control[]
            {
                lblTitle,
                lblUserName, txtUserName,
                lblPassword, txtPassword,
                lblRole, cmbRole,
                btnLogin, btnExit
            });
        }

        /// <summary>
        /// 绑定事件（在 InitializeComponent 之后调用）
        /// </summary>
        private void BindEvents()
        {
            btnLogin.Click += BtnLogin_Click;
            btnExit.Click += BtnExit_Click;
            this.FormClosed += FrmLogin_FormClosed;
        }

        /// <summary>
        /// 登录按钮点击：读取输入并调用 BLL 验证身份
        /// </summary>
        private void BtnLogin_Click(object sender, EventArgs e)
        {
            try
            {
                string loginName = txtUserName.Text.Trim();
                string password = txtPassword.Text;
                string role = cmbRole.SelectedItem?.ToString();

                // 卫语句：基础校验提前返回
                if (string.IsNullOrWhiteSpace(loginName) || string.IsNullOrWhiteSpace(password))
                {
                    MessageBox.Show("请输入用户名和密码", "提示",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtPassword.Focus();
                    return;
                }

                if (string.IsNullOrEmpty(role))
                {
                    MessageBox.Show("请选择身份", "提示",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // 调用业务层登录（密码明文传入，BLL 内部 SHA-256 哈希）
                UserInfo user = new UserManager().Login(loginName, password, role);
                if (user == null)
                {
                    // 登录失败：提示并清空密码框
                    MessageBox.Show("用户名或密码错误，或身份不匹配", "登录失败",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtPassword.Clear();
                    txtPassword.Focus();
                    return;
                }

                // 登录成功：置标志、隐藏登录窗体、显示主窗体
                _loginSucceeded = true;
                this.Hide();
                FrmMain mainForm = new FrmMain(user);
                mainForm.Show();
            }
            catch (Exception ex)
            {
                UiHelper.HandleException(ex, this);
            }
        }

        /// <summary>
        /// 退出按钮：直接退出应用
        /// </summary>
        private void BtnExit_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        /// <summary>
        /// 窗体关闭事件：未成功登录而关闭时退出整个应用
        /// </summary>
        /// <remarks>
        /// 覆盖用户点击右上角 X 关闭窗体的场景，确保进程不残留。
        /// 登录成功后本窗体仅 Hide 不 Close，不会触发此事件。
        /// </remarks>
        private void FrmLogin_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (!_loginSucceeded)
            {
                Application.Exit();
            }
        }
    }
}
