using CampusMart.BLL;
using CampusMart.Common;
using CampusMart.Models;

namespace CampusMart.Forms;

/// <summary>
/// 主窗体：系统导航中枢，采用 MenuStrip 菜单栏 + ToolStrip 工具栏布局
/// </summary>
/// <remarks>
/// 与 cs-2 差异化：cs-2 使用 Button 垂直排列导航，cs-3 使用 MenuStrip + ToolStrip。
/// 权限控制通过菜单项 Enabled + 可见性实现（cs-2 通过按钮 Enabled + 背景色）。
/// </remarks>
public class FrmMain : Form
{
    private readonly UserInfo _currentUser;
    private readonly MenuStrip _menuStrip;
    private readonly ToolStrip _toolStrip;
    private readonly StatusStrip _statusStrip;
    private readonly ToolStripStatusLabel _statusLabel;

    // 菜单项引用（用于权限控制）
    private readonly ToolStripMenuItem _miCategory;
    private readonly ToolStripMenuItem _miGoods;
    private readonly ToolStripMenuItem _miSupplier;
    private readonly ToolStripMenuItem _miOrder;
    private readonly ToolStripMenuItem _miUser;
    private readonly ToolStripMenuItem _miChangePassword;

    /// <summary>
    /// 构造主窗体
    /// </summary>
    /// <param name="currentUser">当前登录用户</param>
    public FrmMain(UserInfo currentUser)
    {
        _currentUser = currentUser;

        // 窗体基本属性
        Text = "校园易购信息管理系统";
        Size = new Size(900, 600);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.FromArgb(245, 247, 250);

        // ===== MenuStrip 菜单栏 =====
        _menuStrip = new MenuStrip
        {
            BackColor = Color.FromArgb(64, 158, 255),
            ForeColor = Color.White,
            Font = new Font("Microsoft YaHei UI", 10F)
        };

        // 功能菜单（顶级）
        ToolStripMenuItem miFunction = new("功能管理");
        _miCategory = new ToolStripMenuItem("商品类别管理", null, (s, e) => OpenChildForm(new FrmCategory(_currentUser)));
        _miGoods = new ToolStripMenuItem("商品信息管理", null, (s, e) => OpenChildForm(new FrmGoods(_currentUser)));
        _miSupplier = new ToolStripMenuItem("供货商管理", null, (s, e) => OpenChildForm(new FrmSupplier(_currentUser)));
        _miOrder = new ToolStripMenuItem("订单管理", null, (s, e) => OpenChildForm(new FrmOrder(_currentUser)));
        miFunction.DropDownItems.AddRange(new ToolStripItem[] { _miCategory, _miGoods, _miSupplier, _miOrder });

        // 系统菜单
        ToolStripMenuItem miSystem = new("系统管理");
        _miUser = new ToolStripMenuItem("用户管理", null, (s, e) => OpenChildForm(new FrmUser(_currentUser)));
        _miChangePassword = new ToolStripMenuItem("修改密码", null, (s, e) => OpenChildForm(new FrmChangePassword(_currentUser)));
        ToolStripMenuItem miExit = new("退出系统", null, (s, e) =>
        {
            if (MessageBox.Show("确定要退出系统吗？", "确认", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                Close();
            }
        });
        miSystem.DropDownItems.AddRange(new ToolStripItem[] { _miUser, _miChangePassword, new ToolStripSeparator(), miExit });

        _menuStrip.Items.AddRange(new ToolStripItem[] { miFunction, miSystem });
        MainMenuStrip = _menuStrip;

        // ===== ToolStrip 工具栏（快捷操作按钮）=====
        _toolStrip = new ToolStrip
        {
            BackColor = Color.FromArgb(250, 250, 250),
            Font = new Font("Microsoft YaHei UI", 9F),
            GripStyle = ToolStripGripStyle.Hidden
        };
        _toolStrip.Items.AddRange(new ToolStripItem[]
        {
            new ToolStripButton("类别管理", null, (s, e) => OpenChildForm(new FrmCategory(_currentUser))),
            new ToolStripButton("商品管理", null, (s, e) => OpenChildForm(new FrmGoods(_currentUser))),
            new ToolStripButton("供货商", null, (s, e) => OpenChildForm(new FrmSupplier(_currentUser))),
            new ToolStripButton("订单管理", null, (s, e) => OpenChildForm(new FrmOrder(_currentUser))),
            new ToolStripSeparator(),
            new ToolStripButton("用户管理", null, (s, e) => OpenChildForm(new FrmUser(_currentUser))),
            new ToolStripButton("修改密码", null, (s, e) => OpenChildForm(new FrmChangePassword(_currentUser)))
        });

        // ===== StatusStrip 状态栏（显示当前用户信息）=====
        _statusStrip = new StatusStrip
        {
            BackColor = Color.FromArgb(64, 158, 255),
            ForeColor = Color.White
        };
        _statusLabel = new ToolStripStatusLabel
        {
            Text = $"当前用户：{_currentUser.LoginName}（{_currentUser.Role}）  |  校园易购信息管理系统 v1.0",
            Font = new Font("Microsoft YaHei UI", 9F)
        };
        _statusStrip.Items.Add(_statusLabel);

        // 添加控件
        Controls.AddRange(new Control[] { _menuStrip, _toolStrip, _statusStrip });

        // 根据权限设置菜单项可用性
        ApplyPermission();
    }

    /// <summary>
    /// 根据登录用户角色启用/禁用菜单项
    /// 管理员：全部菜单可用
    /// 操作员：禁用类别管理、供货商管理、用户管理
    /// </summary>
    private void ApplyPermission()
    {
        bool isAdmin = _currentUser.Role == RoleConstants.ADMIN;

        // 操作员不可操作：类别管理、供货商管理、用户管理
        _miCategory.Enabled = isAdmin;
        _miSupplier.Enabled = isAdmin;
        _miUser.Enabled = isAdmin;

        // 同步禁用工具栏对应按钮（索引：0=类别, 2=供货商, 5=用户）
        _toolStrip.Items[0].Enabled = isAdmin;
        _toolStrip.Items[2].Enabled = isAdmin;
        _toolStrip.Items[5].Enabled = isAdmin;
    }

    /// <summary>
    /// 打开子窗体（单例模式，同一时间只显示一个子窗体）
    /// </summary>
    /// <param name="childForm">子窗体实例</param>
    private void OpenChildForm(Form childForm)
    {
        try
        {
            childForm.StartPosition = FormStartPosition.CenterScreen;
            childForm.FormClosed += (s, e) => Show();
            childForm.Show();
            Hide();
        }
        catch (Exception ex)
        {
            Show();
            MessageBox.Show($"打开窗体失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
