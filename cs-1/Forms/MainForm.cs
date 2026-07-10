using LibraryManagement.BLL;
using LibraryManagement.Models;

namespace LibraryManagement.Forms;

/// <summary>
/// 主窗体：系统导航中枢，根据登录用户权限动态启用/禁用功能按钮
/// </summary>
public class MainForm : Form
{
    private readonly User _currentUser;
    private readonly Button _btnBookCategory;
    private readonly Button _btnBook;
    private readonly Button _btnReader;
    private readonly Button _btnBorrow;
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
        Text = "图书馆信息管理系统";
        Size = new Size(600, 520);
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
        int btnHeight = 55;
        int startX = (600 - btnWidth) / 2 - 8;
        int startY = 75;
        int gap = 12;

        // 图书类别管理按钮
        _btnBookCategory = CreateMenuButton("图书类别管理", startX, startY, btnWidth, btnHeight);
        _btnBookCategory.Click += (s, e) => OpenChildForm(new BookCategoryForm());

        // 图书管理按钮
        _btnBook = CreateMenuButton("图书管理", startX, startY + (btnHeight + gap) * 1, btnWidth, btnHeight);
        _btnBook.Click += (s, e) => OpenChildForm(new BookForm(_currentUser));

        // 读者管理按钮
        _btnReader = CreateMenuButton("读者管理", startX, startY + (btnHeight + gap) * 2, btnWidth, btnHeight);
        _btnReader.Click += (s, e) => OpenChildForm(new ReaderForm(_currentUser));

        // 图书借阅按钮
        _btnBorrow = CreateMenuButton("图书借阅", startX, startY + (btnHeight + gap) * 3, btnWidth, btnHeight);
        _btnBorrow.Click += (s, e) => OpenChildForm(new BorrowForm(_currentUser));

        // 用户管理按钮（仅管理员可见）
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
            if (MessageBox.Show("确定要退出系统吗？", "确认", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                Close();
            }
        };

        // 添加控件
        Controls.AddRange(new Control[]
        {
            _lblUserInfo,
            _btnBookCategory, _btnBook, _btnReader,
            _btnBorrow, _btnUser, _btnChangePassword, _btnExit
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
    /// 普通用户：仅查询类按钮可用（图书、读者、借阅查询）
    /// </summary>
    private void ApplyPermission()
    {
        bool isAdmin = _currentUser.UserPurview == BusinessConstants.ROLE_ADMIN;

        // 普通用户禁用管理类按钮
        _btnBookCategory.Enabled = isAdmin;
        _btnUser.Enabled = isAdmin;

        // 普通用户管理类按钮变灰
        if (!isAdmin)
        {
            _btnBookCategory.BackColor = Color.FromArgb(200, 200, 200);
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
            Hide();
            childForm.StartPosition = FormStartPosition.CenterScreen;
            childForm.FormClosed += (s, e) => Show();
            childForm.Show();
        }
        catch (Exception ex)
        {
            Show();
            MessageBox.Show($"打开窗体失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
