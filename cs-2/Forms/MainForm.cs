using CampusShop.BLL;
using CampusShop.Models;

namespace CampusShop.Forms;

/// <summary>
/// 主窗体：系统导航中枢，根据登录用户权限动态启用/禁用功能按钮
/// </summary>
public class MainForm : Form
{
    private readonly User _currentUser;
    private readonly Button _btnCategory;
    private readonly Button _btnProduct;
    private readonly Button _btnSupplier;
    private readonly Button _btnOrder;
    private readonly Button _btnUser;
    private readonly Button _btnChangePassword;
    private readonly Button _btnExit;
    private readonly Label _lblUserInfo;

    /// <summary>
    /// 构造主窗体
    /// </summary>
    /// <param name="currentUser">当前登录用户</param>
    public MainForm(User currentUser)
    {
        _currentUser = currentUser;

        // 窗体基本属性
        Text = "校园易购信息管理系统";
        Size = new Size(600, 600);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        BackColor = Color.FromArgb(245, 247, 250);

        // 用户信息标签（显示在标题栏下方）
        _lblUserInfo = new Label
        {
            Text = $"当前用户：{_currentUser.UserName}（{_currentUser.UserPurview}）",
            Font = new Font("Microsoft YaHei UI", 11F, FontStyle.Bold),
            ForeColor = Color.FromArgb(51, 51, 51),
            TextAlign = ContentAlignment.MiddleCenter,
            Size = new Size(560, 35),
            Location = new Point(15, 20)
        };

        // 功能按钮统一样式
        int btnWidth = 250;
        int btnHeight = 52;
        int startX = (600 - btnWidth) / 2 - 8;
        int startY = 70;
        int gap = 10;

        // 商品类别管理按钮
        _btnCategory = CreateMenuButton("商品类别管理", startX, startY, btnWidth, btnHeight);
        _btnCategory.Click += (s, e) => OpenChildForm(new CategoryForm(_currentUser));

        // 商品管理按钮
        _btnProduct = CreateMenuButton("商品管理", startX, startY + (btnHeight + gap) * 1, btnWidth, btnHeight);
        // 传入当前用户对象，子窗体内部根据权限控制增删改按钮的可用性
        _btnProduct.Click += (s, e) => OpenChildForm(new ProductForm(_currentUser));

        // 供货商管理按钮
        _btnSupplier = CreateMenuButton("供货商管理", startX, startY + (btnHeight + gap) * 2, btnWidth, btnHeight);
        _btnSupplier.Click += (s, e) => OpenChildForm(new SupplierForm(_currentUser));

        // 订单管理按钮
        _btnOrder = CreateMenuButton("订单管理", startX, startY + (btnHeight + gap) * 3, btnWidth, btnHeight);
        _btnOrder.Click += (s, e) => OpenChildForm(new OrderForm(_currentUser));

        // 用户管理按钮（仅管理员可用）
        _btnUser = CreateMenuButton("用户管理", startX, startY + (btnHeight + gap) * 4, btnWidth, btnHeight);
        _btnUser.Click += (s, e) => OpenChildForm(new UserForm(_currentUser));

        // 修改密码按钮
        _btnChangePassword = CreateMenuButton("修改密码", startX, startY + (btnHeight + gap) * 5, btnWidth, btnHeight);
        _btnChangePassword.Click += (s, e) => OpenChildForm(new ChangePasswordForm(_currentUser));

        // 退出系统按钮
        _btnExit = CreateMenuButton("退出系统", startX, startY + (btnHeight + gap) * 6, btnWidth, btnHeight);
        _btnExit.BackColor = Color.FromArgb(245, 108, 108);
        _btnExit.Click += (s, e) =>
        {
            // 弹出确认对话框防止误点击导致数据未保存即退出
            if (MessageBox.Show("确定要退出系统吗？", "确认", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                Close();
            }
        };

        // 添加控件
        Controls.AddRange(new Control[]
        {
            _lblUserInfo,
            _btnCategory, _btnProduct, _btnSupplier,
            _btnOrder, _btnUser, _btnChangePassword, _btnExit
        });

        // 根据权限设置按钮可用性
        ApplyPermission();
    }

    /// <summary>
    /// 创建统一风格的菜单按钮
    /// </summary>
    private static Button CreateMenuButton(string text, int x, int y, int width, int height)
    {
        return new Button
        {
            Text = text,
            Font = new Font("Microsoft YaHei UI", 11F),
            Size = new Size(width, height),
            Location = new Point(x, y),
            BackColor = Color.FromArgb(64, 158, 255),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            TextAlign = ContentAlignment.MiddleCenter
        };
    }

    /// <summary>
    /// 根据登录用户权限启用/禁用功能按钮
    /// 管理员：全部按钮可用
    /// 普通用户：仅查询类按钮可用（商品、供货商、售卖查询）
    /// </summary>
    private void ApplyPermission()
    {
        bool isAdmin = _currentUser.UserPurview == BusinessConstants.ROLE_ADMIN;

        // 商品类别管理和用户管理属于管理类功能，普通用户无权访问
        _btnCategory.Enabled = isAdmin;
        _btnUser.Enabled = isAdmin;

        // 禁用同时改变背景色为灰色，给用户明确的视觉反馈
        if (!isAdmin)
        {
            _btnCategory.BackColor = Color.FromArgb(200, 200, 200);
            _btnUser.BackColor = Color.FromArgb(200, 200, 200);
        }
    }

    /// <summary>
    /// 打开子窗体并隐藏主窗体
    /// </summary>
    /// <param name="childForm">子窗体实例</param>
    private void OpenChildForm(Form childForm)
    {
        try
        {
            // 隐藏主窗体而非关闭，实现单窗体切换模式，避免多窗体叠加造成界面混乱
            Hide();
            childForm.StartPosition = FormStartPosition.CenterScreen;
            // 子窗体关闭时恢复显示主窗体，形成"主菜单 → 子功能 → 返回主菜单"的导航闭环
            childForm.FormClosed += (s, e) => Show();
            childForm.Show();
        }
        catch (Exception ex)
        {
            // 异常时恢复主窗体显示，避免用户卡在无界面的状态
            Show();
            MessageBox.Show($"打开窗体失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
