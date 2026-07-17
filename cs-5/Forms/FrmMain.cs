using LibrarySys.BLL;
using LibrarySys.Common;
using LibrarySys.Models;
using System.ComponentModel;

namespace LibrarySys.Forms;

/// <summary>
/// 主窗体：左侧 TreeView 导航 + 右侧 Panel 嵌入子窗体
/// 非 MDI 模式，子窗体以 TopLevel=false 嵌入右侧 Panel
/// </summary>
public partial class FrmMain : Form
{
    private readonly UserInfo _currentUser;
    private TreeView _treeView;
    private Panel _rightPanel;
    private MenuStrip _menuStrip;

    /// <summary>普通用户被禁用的节点 Tag 集合</summary>
    private readonly HashSet<string> _disabledNodes = new();

    // 缓存 GDI 对象，避免 Paint/DrawNode 事件中反复创建
    private readonly SolidBrush _logoPrimaryBrush = new(ThemeColor.Primary);
    private readonly Font _logoFont = new("Microsoft YaHei UI", 13F, FontStyle.Bold);
    private readonly SolidBrush _logoTextBrush = new(Color.White);
    private readonly SolidBrush _selNodeBrush = new(ThemeColor.Primary);

    /// <summary>当前登录用户信息</summary>
    public UserInfo CurrentUser => _currentUser;

    /// <summary>无参构造，仅供 VS 设计器使用</summary>
    public FrmMain()
    {
        InitializeComponent();
        BuildUI();
    }

    public FrmMain(UserInfo currentUser)
    {
        _currentUser = currentUser;
        // 使用 WinForms 原生双缓冲，避免 WS_EX_COMPOSITED 导致的控件渲染异常和卡顿
        SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        DoubleBuffered = true;
        InitializeComponent();
        BuildUI();
        try { new ReservationBiz().CleanExpired(); } catch { }
        try { LogBiz.Log(_currentUser.UserName, BusinessConstants.LOG_LOGIN, "登录系统", $"用户 {_currentUser.UserName} 登录系统"); } catch { }
        ShowDashboard();
    }

    private void BuildUI()
    {
        // 设计器模式下跳过：设计器已在 InitializeComponent 中创建控件骨架
        if (LicenseManager.UsageMode == LicenseUsageMode.Designtime) return;
        // 运行时：清除 InitializeComponent 创建的骨架控件，重新完整构建
        Controls.Clear();

        Text = $"智慧图书馆管理系统 - 当前用户：{_currentUser?.UserName ?? "（设计器预览）"}（{_currentUser?.UserPurview ?? ""}）";
        StartPosition = FormStartPosition.CenterScreen;
        WindowState = FormWindowState.Maximized;
        MinimumSize = new Size(1024, 600);
        Font = UiHelper.DefaultFont;
        BackColor = ThemeColor.BgPage;

        // ===== 顶部 MenuStrip =====
        _menuStrip = new MenuStrip
        {
            Font = ThemeColor.FontMedium,
            BackColor = ThemeColor.BgCard,
            ForeColor = ThemeColor.TextPrimary,
            Renderer = new FlatMenuRenderer(),
            Dock = DockStyle.Top
        };
        ToolStripMenuItem miSystem = new("系统(&S)");
        ToolStripMenuItem miChangePwd = new("修改密码", null, (_, _) => OpenChild("ChangePassword"))
            { Tag = "ChangePassword" };
        ToolStripMenuItem miLogout = new("退出登录", null, (_, _) => { Close(); });
        miSystem.DropDownItems.Add(miChangePwd);
        miSystem.DropDownItems.Add(new ToolStripSeparator());
        miSystem.DropDownItems.Add(miLogout);
        _menuStrip.Items.Add(miSystem);

        // ===== 左侧 TreeView 导航面板 =====
        Panel leftPanel = new Panel
        {
            Dock = DockStyle.Left,
            Width = 200,
            BackColor = ThemeColor.SidebarBg
        };

        Panel logoPanel = new Panel()
        {
            Dock = DockStyle.Top,
            Height = 60,
            BackColor = ThemeColor.SidebarBg
        };
        logoPanel.Paint += (_, e) =>
        {
            e.Graphics.FillRectangle(_logoPrimaryBrush, 0, 0, logoPanel.Width, 3);
            e.Graphics.DrawString("📚  智慧图书馆", _logoFont, _logoTextBrush, 16, 22);
        };

        _treeView = new TreeView
        {
            Dock = DockStyle.Fill,
            BackColor = ThemeColor.SidebarBg,
            ForeColor = ThemeColor.SidebarText,
            Font = ThemeColor.FontMedium,
            BorderStyle = BorderStyle.None,
            ItemHeight = 36,
            Indent = 20,
            ShowLines = false,
            ShowPlusMinus = false,
            HideSelection = false,
            DrawMode = TreeViewDrawMode.OwnerDrawText,
            HotTracking = false
        };
        // 启用 TreeView 双缓冲（通过反射设置 protected DoubleBuffered 属性）
        typeof(TreeView).InvokeMember("DoubleBuffered",
            System.Reflection.BindingFlags.SetProperty | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic,
            null, _treeView, new object[] { true });

        TreeNode rootHome = new("首页") { Tag = "Dashboard", ForeColor = ThemeColor.SidebarText };
        TreeNode rootBase = new("基础数据") { Tag = "Group", ForeColor = ThemeColor.SidebarGroupText };
        rootBase.Nodes.Add(new TreeNode("图书类型管理") { Tag = "BookType", ForeColor = ThemeColor.SidebarText });
        rootBase.Nodes.Add(new TreeNode("图书信息管理") { Tag = "Book", ForeColor = ThemeColor.SidebarText });
        rootBase.Nodes.Add(new TreeNode("读者管理") { Tag = "Reader", ForeColor = ThemeColor.SidebarText });
        TreeNode rootCirc = new("流通管理") { Tag = "Group", ForeColor = ThemeColor.SidebarGroupText };
        rootCirc.Nodes.Add(new TreeNode("借还书操作") { Tag = "Borrow", ForeColor = ThemeColor.SidebarText });
        rootCirc.Nodes.Add(new TreeNode("借阅记录查询") { Tag = "BorrowRecord", ForeColor = ThemeColor.SidebarText });
        rootCirc.Nodes.Add(new TreeNode("预约管理") { Tag = "Reservation", ForeColor = ThemeColor.SidebarText });
        TreeNode rootFine = new("罚款管理") { Tag = "Group", ForeColor = ThemeColor.SidebarGroupText };
        rootFine.Nodes.Add(new TreeNode("罚款管理") { Tag = "Fine", ForeColor = ThemeColor.SidebarText });
        TreeNode rootSys = new("系统管理") { Tag = "Group", ForeColor = ThemeColor.SidebarGroupText };
        rootSys.Nodes.Add(new TreeNode("用户账号管理") { Tag = "User", ForeColor = ThemeColor.SidebarText });
        rootSys.Nodes.Add(new TreeNode("操作日志") { Tag = "OperateLog", ForeColor = ThemeColor.SidebarText });
        TreeNode rootStat = new("统计分析") { Tag = "Group", ForeColor = ThemeColor.SidebarGroupText };
        rootStat.Nodes.Add(new TreeNode("统计面板") { Tag = "Statistic", ForeColor = ThemeColor.SidebarText });
        _treeView.Nodes.AddRange(new[] { rootHome, rootBase, rootCirc, rootFine, rootSys, rootStat });
        _treeView.ExpandAll();

        // 根据权限禁用 TreeView 节点（普通用户：图书类型、用户管理、操作日志不可用）
        ApplyPermissionControl();

        _treeView.DrawNode += (_, e) =>
        {
            bool isGroup = e.Node.Tag is string gt && gt == "Group";
            bool isSelected = e.Node == _treeView.SelectedNode;

            if (!isGroup && isSelected)
            {
                e.Graphics.FillRectangle(_selNodeBrush, e.Bounds);
                TextRenderer.DrawText(e.Graphics, e.Node.Text, ThemeColor.FontMedium,
                    new Point(e.Bounds.X + 12, e.Bounds.Y + 8), ThemeColor.SidebarActiveText);
            }
            else
            {
                TextRenderer.DrawText(e.Graphics, e.Node.Text, ThemeColor.FontMedium,
                    new Point(e.Bounds.X + (isGroup ? 8 : 12), e.Bounds.Y + 8), e.Node.ForeColor);
            }
        };

        _treeView.BeforeSelect += (_, e) =>
        {
            if (e.Node?.Tag is string tag && tag == "Group")
                e.Cancel = true;
            // 普通用户不能选择被禁用的节点（图书类型、用户管理、操作日志）
            if (e.Node?.Tag is string nodeTag && _disabledNodes.Contains(nodeTag))
                e.Cancel = true;
        };
        _treeView.AfterSelect += (_, e) =>
        {
            if (e.Node?.Tag is string tag && tag != "Group")
                OpenChild(tag);
        };

        leftPanel.Controls.Add(_treeView);
        leftPanel.Controls.Add(logoPanel);

        // ===== 右侧内容区域 =====
        _rightPanel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = ThemeColor.BgPage
        };

        // 添加顺序：MenuStrip(Top) → leftPanel(Left) → _rightPanel(Fill)
        // WinForms Dock 布局：后添加的同方向 Dock 控件会占据更靠近边缘的位置
        // MenuStrip 必须最先加入 Controls（Z-order 最低），这样它 Dock=Top 时在最顶部
        Controls.Add(_rightPanel);
        Controls.Add(leftPanel);
        Controls.Add(_menuStrip);
    }

    /// <summary>
    /// 根据当前用户权限禁用 TreeView 节点
    /// 普通用户：图书类型管理、用户账号管理、操作日志 不可用
    /// </summary>
    private void ApplyPermissionControl()
    {
        // 设计器无参构造时 _currentUser 为 null，跳过权限控制
        if (_currentUser == null) return;
        if (_currentUser.UserPurview == BusinessConstants.ROLE_ADMIN)
            return;

        // 普通用户禁用的节点 Tag
        string[] disabledTags = { "BookType", "User", "OperateLog" };
        Color disabledColor = Color.FromArgb(180, 180, 180); // 灰色表示禁用

        foreach (var tag in disabledTags)
        {
            _disabledNodes.Add(tag);
        }

        // 递归遍历所有节点，将禁用节点设为灰色
        foreach (TreeNode node in _treeView.Nodes)
        {
            ApplyDisabledStyle(node);
        }
    }

    /// <summary>递归设置禁用节点的样式</summary>
    private void ApplyDisabledStyle(TreeNode node)
    {
        if (node.Tag is string tag && _disabledNodes.Contains(tag))
        {
            node.ForeColor = Color.FromArgb(180, 180, 180);
        }
        foreach (TreeNode child in node.Nodes)
        {
            ApplyDisabledStyle(child);
        }
    }

    private void OpenChild(string formKey)
    {
        try
        {
            CloseCurrentChild();

            Form child = formKey switch
            {
                "Dashboard" => new FrmDashboard(
                    () => new StatisticBiz().GetDashboardData(),
                    () => new ReservationBiz().Search(null, null, BusinessConstants.RESERVE_WAITING),
                    () => new ReservationBiz().CountActive(),
                    () => new BorrowBiz().Search(null, null, BusinessConstants.STATUS_BORROWED, null, null).Count),
                "BookType" => new FrmBookType(_currentUser),
                "Book" => new FrmBook(_currentUser),
                "Reader" => new FrmReader(_currentUser),
                "Borrow" => new FrmBorrow(_currentUser),
                "BorrowRecord" => new FrmBorrow(_currentUser),
                "Reservation" => new FrmReservation(_currentUser),
                "Fine" => new FrmFine(_currentUser),
                "User" => new FrmUser(_currentUser),
                "OperateLog" => new FrmOperateLog(_currentUser),
                "Statistic" => new FrmStatistic(),
                "ChangePassword" => new FrmChangePassword(_currentUser),
                _ => null
            };

            if (child == null) return;

            child.TopLevel = false;
            child.FormBorderStyle = FormBorderStyle.None;
            child.Dock = DockStyle.Fill;
            _rightPanel.Controls.Add(child);
            child.Show();

            // 修复嵌入窗体（TopLevel=false）中 DataGridView 鼠标滚轮失效问题
            // 原因：嵌入窗体的滚轮消息只发给焦点控件，不会转发到 DataGridView
            HookMouseWheelForGrid(child);
        }
        catch (Exception ex)
        {
            string msg = $"打开窗体失败：{ex.Message}";
            if (ex.InnerException != null)
                msg += $"\n\n内部异常：{ex.InnerException.Message}";
            MessageBox.Show(msg, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ShowDashboard()
    {
        _treeView.SelectedNode = _treeView.Nodes[0];
    }

    private void CloseCurrentChild()
    {
        if (_rightPanel.Controls.Count > 0)
        {
            var ctrl = _rightPanel.Controls[0];
            _rightPanel.Controls.Clear();
            if (ctrl is Form frm)
            {
                frm.Close();
                frm.Dispose();
            }
        }
    }

    /// <summary>
    /// 递归挂载 MouseWheel 事件，修复嵌入窗体中 DataGridView 滚轮失效问题
    /// </summary>
    /// <param name="container">起始控件</param>
    private static void HookMouseWheelForGrid(Control container)
    {
        // DataGridView 自身已有滚轮处理，跳过避免双重滚动
        if (container is not DataGridView)
        {
            container.MouseWheel += ForwardWheelToGrid;
        }
        foreach (Control c in container.Controls)
        {
            HookMouseWheelForGrid(c);
        }
    }

    /// <summary>
    /// 将滚轮事件转发给子窗体中的 DataGridView
    /// </summary>
    private static void ForwardWheelToGrid(object sender, MouseEventArgs e)
    {
        // DataGridView 自身的滚轮由其内置处理，跳过
        if (sender is DataGridView) return;
        if (sender is not Control ctrl) return;

        Form form = ctrl.FindForm();
        if (form == null) return;

        var dgv = FindDataGridView(form);
        if (dgv == null || dgv.RowCount == 0) return;

        // 按系统设置滚动行数滚动
        int lines = e.Delta > 0
            ? -SystemInformation.MouseWheelScrollLines
            : SystemInformation.MouseWheelScrollLines;
        int newIndex = Math.Max(0, Math.Min(
            dgv.FirstDisplayedScrollingRowIndex + lines,
            dgv.RowCount - 1));

        if (newIndex != dgv.FirstDisplayedScrollingRowIndex)
        {
            dgv.FirstDisplayedScrollingRowIndex = newIndex;
        }
    }

    /// <summary>
    /// 递归查找第一个 DataGridView 控件
    /// </summary>
    /// <param name="parent">父控件</param>
    /// <returns>DataGridView 或 null</returns>
    private static DataGridView FindDataGridView(Control parent)
    {
        foreach (Control c in parent.Controls)
        {
            if (c is DataGridView dgv) return dgv;
            var found = FindDataGridView(c);
            if (found != null) return found;
        }
        return null;
    }
}

internal class FlatMenuRenderer : ToolStripProfessionalRenderer
{
    private readonly SolidBrush _primaryBrush = new(ThemeColor.Primary);
    private readonly SolidBrush _bgCardBrush = new(ThemeColor.BgCard);

    public FlatMenuRenderer() : base(new FlatMenuColorTable()) { }

    protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
    {
        var item = e.Item;
        Rectangle rc = new(Point.Empty, item.Size);
        if (item.Selected || item.Pressed)
            e.Graphics.FillRectangle(_primaryBrush, rc);
        else
            e.Graphics.FillRectangle(_bgCardBrush, rc);
    }

    protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
    {
        if (e.Item.Selected || e.Item.Pressed)
            e.TextColor = Color.White;
        else
            e.TextColor = ThemeColor.TextPrimary;
        base.OnRenderItemText(e);
    }
}

internal class FlatMenuColorTable : ProfessionalColorTable
{
    public override Color MenuBorder => ThemeColor.Border;
    public override Color MenuItemBorder => ThemeColor.Primary;
    public override Color MenuItemSelected => ThemeColor.Primary;
    public override Color MenuItemSelectedGradientBegin => ThemeColor.Primary;
    public override Color MenuItemSelectedGradientEnd => ThemeColor.Primary;
    public override Color MenuItemPressedGradientBegin => ThemeColor.PrimaryActive;
    public override Color MenuItemPressedGradientEnd => ThemeColor.PrimaryActive;
    public override Color MenuStripGradientBegin => ThemeColor.BgCard;
    public override Color MenuStripGradientEnd => ThemeColor.BgCard;
    public override Color ToolStripDropDownBackground => ThemeColor.BgCard;
    public override Color ImageMarginGradientBegin => ThemeColor.BgCard;
    public override Color ImageMarginGradientMiddle => ThemeColor.BgCard;
    public override Color ImageMarginGradientEnd => ThemeColor.BgCard;
    // 去掉下拉菜单左侧的边距渐变块
    public override Color ImageMarginRevealedGradientBegin => ThemeColor.BgCard;
    public override Color ImageMarginRevealedGradientMiddle => ThemeColor.BgCard;
    public override Color ImageMarginRevealedGradientEnd => ThemeColor.BgCard;
}
