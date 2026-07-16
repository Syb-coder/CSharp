using HotelSys.BLL;
using HotelSys.Common;
using HotelSys.Models;

namespace HotelSys.Forms;

/// <summary>
/// 主窗体（PRD 4.2.2）
/// 左侧 TreeView 导航 + 右侧 Panel 嵌入子窗体 + 顶部 MenuStrip 菜单栏
/// 权限控制：前台操作员禁用"用户管理"和"操作日志"节点
/// </summary>
public class FrmMain : Form
{
    private readonly UserInfo _currentUser;
    private TreeView _treeView;
    private Panel _rightPanel;
    private MenuStrip _menuStrip;

    /// <summary>前台操作员被禁用的节点 Tag 集合</summary>
    private readonly HashSet<string> _disabledNodes = new();

    // 缓存 GDI 对象，避免 Paint 事件中反复创建
    private readonly SolidBrush _logoPrimaryBrush = new(ThemeColor.Primary);
    private readonly Font _logoFont = new("Microsoft YaHei UI", 13F, FontStyle.Bold);
    private readonly SolidBrush _logoTextBrush = new(Color.White);
    private readonly SolidBrush _selNodeBrush = new(ThemeColor.SidebarActiveBg);

    public FrmMain(UserInfo currentUser)
    {
        _currentUser = currentUser;
        // 使用 WinForms 原生双缓冲，避免 WS_EX_COMPOSITED 导致的控件渲染异常和卡顿（cs-5 验证）
        SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        DoubleBuffered = true;
        InitializeUI();
        ApplyPermissionControl();

        // 启动时自动过期超期预订（PRD AC-11.1），吞异常避免启动失败
        try { new ReservationManager().AutoExpire(); } catch { }

        // 记录登录日志
        try
        {
            LogManager.Record(_currentUser.UserName, BusinessConstants.LOG_LOGIN,
                $"用户 {_currentUser.UserName} 登录系统", $"身份:{_currentUser.UserPurview}");
        }
        catch { }

        // 默认显示房态看板首页
        ShowRoomBoard();
    }

    private void InitializeUI()
    {
        Text = $"智慧酒店管理系统 - 当前用户：{_currentUser.UserName}（{_currentUser.UserPurview}）";
        StartPosition = FormStartPosition.CenterScreen;
        WindowState = FormWindowState.Maximized;
        MinimumSize = new Size(1024, 600);
        Font = UiHelper.DefaultFont;
        BackColor = ThemeColor.BgPage;

        BuildMenuStrip();
        BuildRightPanel();

        // 控件添加顺序：Fill 先，Left 次，Top 最后（WinForms Dock Z-order 规则）
        Controls.Add(_rightPanel);
        Controls.Add(BuildLeftPanel());
        Controls.Add(_menuStrip);
    }

    /// <summary>构建顶部菜单栏</summary>
    private void BuildMenuStrip()
    {
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
        ToolStripMenuItem miLogout = new("退出登录", null, (_, _) => Close());
        miSystem.DropDownItems.Add(miChangePwd);
        miSystem.DropDownItems.Add(new ToolStripSeparator());
        miSystem.DropDownItems.Add(miLogout);
        _menuStrip.Items.Add(miSystem);
    }

    /// <summary>构建左侧导航面板（Logo + TreeView）</summary>
    private Panel BuildLeftPanel()
    {
        Panel leftPanel = new()
        {
            Dock = DockStyle.Left,
            Width = 200,
            BackColor = ThemeColor.SidebarBg
        };

        // 顶部 Logo 区
        Panel logoPanel = new()
        {
            Dock = DockStyle.Top,
            Height = 60,
            BackColor = ThemeColor.SidebarBg
        };
        logoPanel.Paint += (_, e) =>
        {
            e.Graphics.FillRectangle(_logoPrimaryBrush, 0, 0, 6, logoPanel.Height);
            TextRenderer.DrawText(e.Graphics, "酒店管理", _logoFont,
                new Point(20, 15), Color.White);
        };

        // 导航树
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
        // 反射启用 TreeView 双缓冲，消除展开/折叠时的闪烁
        UiHelper.EnableDoubleBuffered(_treeView);

        BuildNavigationNodes();

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
            // 分组节点和禁用节点不可选中
            if (e.Node?.Tag is string tag && (tag == "Group" || _disabledNodes.Contains(tag)))
                e.Cancel = true;
        };

        _treeView.AfterSelect += (_, e) =>
        {
            if (e.Node?.Tag is string tag && tag != "Group")
                OpenChild(tag);
        };

        // 添加顺序：TreeView(Fill) 先，logoPanel(Top) 后
        leftPanel.Controls.Add(_treeView);
        leftPanel.Controls.Add(logoPanel);

        return leftPanel;
    }

    /// <summary>构建右侧内容面板</summary>
    private void BuildRightPanel()
    {
        _rightPanel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = ThemeColor.BgPage
        };
    }

    /// <summary>构建导航树节点（PRD 4.2.2 菜单结构）</summary>
    private void BuildNavigationNodes()
    {
        TreeNode rootHome = new("首页") { Tag = "RoomBoard", ForeColor = ThemeColor.SidebarText };

        TreeNode rootRoom = new("客房管理") { Tag = "Group", ForeColor = ThemeColor.SidebarGroupText };
        rootRoom.Nodes.Add(new TreeNode("房型管理") { Tag = "RoomType", ForeColor = ThemeColor.SidebarText });
        rootRoom.Nodes.Add(new TreeNode("客房管理") { Tag = "Room", ForeColor = ThemeColor.SidebarText });

        TreeNode rootCustomer = new("客户管理") { Tag = "Customer", ForeColor = ThemeColor.SidebarText };

        TreeNode rootBiz = new("业务管理") { Tag = "Group", ForeColor = ThemeColor.SidebarGroupText };
        rootBiz.Nodes.Add(new TreeNode("预订管理") { Tag = "Reservation", ForeColor = ThemeColor.SidebarText });
        rootBiz.Nodes.Add(new TreeNode("入住登记") { Tag = "CheckIn", ForeColor = ThemeColor.SidebarText });
        rootBiz.Nodes.Add(new TreeNode("消费记账") { Tag = "Consume", ForeColor = ThemeColor.SidebarText });
        rootBiz.Nodes.Add(new TreeNode("退房结算") { Tag = "CheckOut", ForeColor = ThemeColor.SidebarText });

        TreeNode rootStat = new("统计分析") { Tag = "Statistics", ForeColor = ThemeColor.SidebarText };

        TreeNode rootSys = new("系统管理") { Tag = "Group", ForeColor = ThemeColor.SidebarGroupText };
        rootSys.Nodes.Add(new TreeNode("用户管理") { Tag = "User", ForeColor = ThemeColor.SidebarText });
        rootSys.Nodes.Add(new TreeNode("修改密码") { Tag = "ChangePassword", ForeColor = ThemeColor.SidebarText });
        rootSys.Nodes.Add(new TreeNode("操作日志") { Tag = "OperateLog", ForeColor = ThemeColor.SidebarText });

        _treeView.Nodes.AddRange(new[] { rootHome, rootRoom, rootCustomer, rootBiz, rootStat, rootSys });
        _treeView.ExpandAll();
    }

    /// <summary>权限控制：前台操作员禁用用户管理和操作日志节点</summary>
    private void ApplyPermissionControl()
    {
        if (_currentUser.UserPurview == BusinessConstants.ROLE_ADMIN)
            return;

        // 前台禁用：用户管理、操作日志（PRD 4.2.2 权限矩阵）
        string[] disabledTags = { "User", "OperateLog" };
        foreach (var tag in disabledTags)
            _disabledNodes.Add(tag);

        foreach (TreeNode node in _treeView.Nodes)
            ApplyDisabledStyle(node);
    }

    /// <summary>递归设置禁用节点的灰色文字</summary>
    private void ApplyDisabledStyle(TreeNode node)
    {
        if (node.Tag is string tag && _disabledNodes.Contains(tag))
            node.ForeColor = Color.FromArgb(180, 180, 180);
        foreach (TreeNode child in node.Nodes)
            ApplyDisabledStyle(child);
    }

    /// <summary>默认显示房态看板首页</summary>
    private void ShowRoomBoard()
    {
        OpenChild("RoomBoard");
    }

    /// <summary>根据 Tag 路由打开对应子窗体并嵌入右侧 Panel</summary>
    private void OpenChild(string formKey)
    {
        try
        {
            CloseCurrentChild();

            Form child = formKey switch
            {
                "RoomBoard" => new FrmRoomBoard(),
                "RoomType" => new FrmRoomType(_currentUser),
                "Room" => new FrmRoom(_currentUser),
                "Customer" => new FrmCustomer(_currentUser),
                "Reservation" => new FrmReservation(_currentUser),
                "CheckIn" => new FrmCheckIn(_currentUser),
                "Consume" => new FrmConsume(_currentUser),
                "CheckOut" => new FrmCheckOut(_currentUser),
                "Statistics" => new FrmStatistics(),
                "User" => new FrmUser(_currentUser),
                "ChangePassword" => new FrmChangePassword(_currentUser),
                "OperateLog" => new FrmOperateLog(_currentUser),
                _ => null
            };

            if (child == null) return;

            // 嵌入子窗体：非顶级 + 无边框 + 填充
            child.TopLevel = false;
            child.FormBorderStyle = FormBorderStyle.None;
            child.Dock = DockStyle.Fill;
            _rightPanel.Controls.Add(child);
            child.Show();

            // 修复嵌入窗体滚轮失效问题
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

    /// <summary>关闭并释放当前嵌入的子窗体</summary>
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

    /// <summary>递归挂载滚轮转发，修复嵌入窗体 DataGridView 滚轮失效</summary>
    private static void HookMouseWheelForGrid(Control container)
    {
        if (container is not DataGridView)
            container.MouseWheel += ForwardWheelToGrid;
        foreach (Control c in container.Controls)
            HookMouseWheelForGrid(c);
    }

    /// <summary>将滚轮事件转发到窗体内的 DataGridView</summary>
    private static void ForwardWheelToGrid(object sender, MouseEventArgs e)
    {
        if (sender is DataGridView) return;
        if (sender is not Control ctrl) return;

        Form form = ctrl.FindForm();
        if (form == null) return;

        var dgv = FindDataGridView(form);
        if (dgv == null || dgv.RowCount == 0) return;

        int lines = e.Delta > 0
            ? -SystemInformation.MouseWheelScrollLines
            : SystemInformation.MouseWheelScrollLines;
        int newIndex = Math.Max(0, Math.Min(
            dgv.FirstDisplayedScrollingRowIndex + lines,
            dgv.RowCount - 1));

        if (newIndex != dgv.FirstDisplayedScrollingRowIndex)
            dgv.FirstDisplayedScrollingRowIndex = newIndex;
    }

    /// <summary>在控件树中查找第一个 DataGridView</summary>
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

/// <summary>
/// 扁平化菜单渲染器（沿用 cs-5 方案）
/// 统一 MenuStrip 为扁平主色风格，消除系统默认的 3D 渐变
/// </summary>
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
        e.TextColor = (e.Item.Selected || e.Item.Pressed) ? Color.White : ThemeColor.TextPrimary;
        base.OnRenderItemText(e);
    }
}

/// <summary>扁平化菜单颜色表</summary>
internal class FlatMenuColorTable : ProfessionalColorTable
{
    public override Color MenuBorder => ThemeColor.Border;
    public override Color MenuItemBorder => ThemeColor.Primary;
    public override Color MenuItemSelected => ThemeColor.Primary;
    public override Color MenuItemSelectedGradientBegin => ThemeColor.Primary;
    public override Color MenuItemSelectedGradientEnd => ThemeColor.Primary;
    public override Color MenuItemPressedGradientBegin => ThemeColor.Primary;
    public override Color MenuItemPressedGradientEnd => ThemeColor.Primary;
    public override Color MenuStripGradientBegin => ThemeColor.BgCard;
    public override Color MenuStripGradientEnd => ThemeColor.BgCard;
    public override Color ToolStripBorder => ThemeColor.Border;
    public override Color ToolStripContentPanelGradientBegin => ThemeColor.BgCard;
    public override Color ToolStripContentPanelGradientEnd => ThemeColor.BgCard;
}
