using System.Drawing;

namespace LibrarySys.Common;

/// <summary>
/// 主题色常量类（现代扁平 + 主色蓝）
/// 统一管理全系统颜色，避免硬编码导致风格不一致
/// 调色板参考 Ant Design 规范，主色 #1890FF
/// </summary>
public static class ThemeColor
{
    // ===== 主色系 =====
    /// <summary>主色：按钮、选中态、强调元素</summary>
    public static readonly Color Primary = Color.FromArgb(24, 144, 255);

    /// <summary>主色悬停态（略深）</summary>
    public static readonly Color PrimaryHover = Color.FromArgb(64, 169, 255);

    /// <summary>主色按下态（更深）</summary>
    public static readonly Color PrimaryActive = Color.FromArgb(9, 109, 217);

    /// <summary>主色浅色背景（卡片高亮、表头底色）</summary>
    public static readonly Color PrimaryLight = Color.FromArgb(230, 247, 255);

    // ===== 功能色 =====
    public static readonly Color Success = Color.FromArgb(82, 196, 26);
    public static readonly Color Warning = Color.FromArgb(250, 173, 20);
    public static readonly Color Danger = Color.FromArgb(245, 34, 45);
    public static readonly Color Info = Color.FromArgb(114, 46, 209);

    // ===== 中性色 =====
    /// <summary>主要文字（标题、正文）</summary>
    public static readonly Color TextPrimary = Color.FromArgb(33, 37, 41);

    /// <summary>次要文字（标签、说明）</summary>
    public static readonly Color TextSecondary = Color.FromArgb(108, 117, 125);

    /// <summary>占位符/禁用态文字</summary>
    public static readonly Color TextPlaceholder = Color.FromArgb(173, 181, 189);

    /// <summary>边框默认色</summary>
    public static readonly Color Border = Color.FromArgb(217, 217, 217);

    /// <summary>边框聚焦色（输入框获得焦点时）</summary>
    public static readonly Color BorderFocus = Color.FromArgb(24, 144, 255);

    // ===== 背景色 =====
    /// <summary>页面背景（窗体底色）</summary>
    public static readonly Color BgPage = Color.FromArgb(240, 242, 245);

    /// <summary>卡片/容器背景（白色）</summary>
    public static readonly Color BgCard = Color.White;

    /// <summary>表格交替行底色</summary>
    public static readonly Color BgAltRow = Color.FromArgb(250, 250, 250);

    /// <summary>表头底色</summary>
    public static readonly Color BgHeader = Color.FromArgb(250, 250, 250);

    // ===== 深色侧栏专用 =====
    /// <summary>侧栏背景</summary>
    public static readonly Color SidebarBg = Color.FromArgb(50, 54, 68);

    /// <summary>侧栏菜单默认文字色</summary>
    public static readonly Color SidebarText = Color.FromArgb(180, 188, 204);

    /// <summary>侧栏选中节点背景</summary>
    public static readonly Color SidebarActiveBg = Color.FromArgb(24, 144, 255);

    /// <summary>侧栏选中节点文字色</summary>
    public static readonly Color SidebarActiveText = Color.White;

    /// <summary>侧栏分组节点文字色（"基础数据"等父节点）</summary>
    public static readonly Color SidebarGroupText = Color.FromArgb(140, 150, 170);

    // ===== 字体规范 =====
    public static readonly Font FontRegular = new("Microsoft YaHei UI", 9F);
    public static readonly Font FontMedium = new("Microsoft YaHei UI", 10F);
    public static readonly Font FontBold = new("Microsoft YaHei UI", 9F, FontStyle.Bold);
    public static readonly Font FontTitle = new("Microsoft YaHei UI", 14F, FontStyle.Bold);
    public static readonly Font FontHeader = new("Microsoft YaHei UI", 12F, FontStyle.Bold);
}
