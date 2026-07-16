using System;
using System.Windows.Forms;
using CampusStore.Common;
using CampusStore.Models;

namespace CampusStore.Forms
{
    /// <summary>
    /// 主窗体
    /// </summary>
    /// <remarks>
    /// 职责：展示功能菜单、根据角色控制权限、打开各子模块。
    /// 管理员拥有全部权限；店员禁用商品类别管理与用户管理。
    /// </remarks>
    public partial class FrmMain : Form
    {
        /// <summary>当前登录用户</summary>
        private readonly UserInfo _currentUser;

        /// <summary>是否正在执行退出登录流程（用于区分用户主动关闭窗口）</summary>
        private bool _isLoggingOut;

        // ===== 控件字段 =====
        private Label lblWelcome;
        private Button btnCategory;
        private Button btnProduct;
        private Button btnSupplier;
        private Button btnOrder;
        private Button btnUser;
        private Button btnLowStock;
        private Button btnChangePassword;
        private Button btnLogout;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="user">当前登录用户，用于权限控制与子窗体传参</param>
        public FrmMain(UserInfo user)
        {
            _currentUser = user ?? throw new ArgumentNullException(nameof(user));
            InitializeComponent();
            BindEvents();
            ApplyPermission();
            // 用户直接关闭主窗体（非退出登录）时，确保进程退出
            this.FormClosing += FrmMain_FormClosing;
        }

        /// <summary>
        /// 初始化所有控件属性与布局
        /// </summary>
        private void InitializeComponent()
        {
            UiHelper.SetupForm(this,
                $"校园易购信息管理系统 - [{_currentUser.LoginName}] ({_currentUser.Role})",
                700, 500);
            // 主窗体需屏幕居中（SetupForm 默认 CenterParent）
            this.StartPosition = FormStartPosition.CenterScreen;

            // 顶部欢迎信息
            lblWelcome = UiHelper.CreateLabel(
                $"欢迎您，{_currentUser.LoginName}（{_currentUser.Role}）", 20, 20);

            // 功能按钮区：2行3列，按钮 180x60，间距 20
            // 起始 X = (700 - 3*180 - 2*20) / 2 = 60
            btnCategory = UiHelper.CreatePrimaryButton("商品类别管理", 60, 80, 180, 60);
            btnProduct = UiHelper.CreatePrimaryButton("商品信息管理", 260, 80, 180, 60);
            btnSupplier = UiHelper.CreatePrimaryButton("供货商管理", 460, 80, 180, 60);

            btnOrder = UiHelper.CreatePrimaryButton("订单管理", 60, 160, 180, 60);
            btnUser = UiHelper.CreatePrimaryButton("用户管理", 260, 160, 180, 60);
            btnLowStock = UiHelper.CreatePrimaryButton("库存预警", 460, 160, 180, 60);

            // 底部按钮（居中：两按钮 120 宽，间距 10，总宽 250，起始 X = (700-250)/2 = 225）
            btnChangePassword = UiHelper.CreateSecondaryButton("修改密码", 225, 260, 120, 35);
            btnLogout = UiHelper.CreateSecondaryButton("退出登录", 355, 260, 120, 35);

            this.Controls.AddRange(new Control[]
            {
                lblWelcome,
                btnCategory, btnProduct, btnSupplier,
                btnOrder, btnUser, btnLowStock,
                btnChangePassword, btnLogout
            });
        }

        /// <summary>
        /// 绑定事件（在 InitializeComponent 之后调用）
        /// </summary>
        private void BindEvents()
        {
            btnCategory.Click += (s, e) => ShowDialog(new FrmCategory());
            btnProduct.Click += (s, e) => ShowDialog(new FrmProduct());
            btnSupplier.Click += (s, e) => ShowDialog(new FrmSupplier());
            btnOrder.Click += (s, e) => ShowDialog(new FrmOrder());
            btnUser.Click += (s, e) => ShowDialog(new FrmUser(_currentUser));
            btnLowStock.Click += (s, e) => ShowDialog(new FrmLowStock());
            btnChangePassword.Click += (s, e) => ShowDialog(new FrmChangePassword(_currentUser.UserID));
            btnLogout.Click += BtnLogout_Click;
        }

        /// <summary>
        /// 权限控制：店员禁用商品类别管理与用户管理
        /// </summary>
        private void ApplyPermission()
        {
            if (_currentUser.Role == "店员")
            {
                btnCategory.Enabled = false;
                btnUser.Enabled = false;
            }
        }

        /// <summary>
        /// 以对话框方式显示子窗体，关闭后自动释放资源
        /// </summary>
        /// <param name="childForm">子窗体实例</param>
        private void ShowDialog(Form childForm)
        {
            using (childForm)
            {
                childForm.ShowDialog(this);
            }
        }

        /// <summary>
        /// 退出登录：隐藏主窗体，显示登录窗体，关闭当前主窗体
        /// </summary>
        /// <remarks>
        /// 设置 _isLoggingOut 标志避免 FormClosing 事件中调用 Application.Exit()。
        /// </remarks>
        private void BtnLogout_Click(object sender, EventArgs e)
        {
            _isLoggingOut = true;
            this.Hide();
            FrmLogin loginForm = new FrmLogin();
            loginForm.Show();
            this.Close();
        }

        /// <summary>
        /// 窗体关闭事件：用户直接关闭主窗体时退出整个应用
        /// </summary>
        /// <remarks>
        /// 退出登录流程中 _isLoggingOut=true，跳过此逻辑，由新 FrmLogin 接管。
        /// </remarks>
        private void FrmMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!_isLoggingOut)
            {
                Application.Exit();
            }
        }
    }
}
