using System.Drawing;

namespace HotelSys.Common;

/// <summary>
/// 智慧酒店管理系统主题色常量类
/// 暖金色调（#C8953B），区别于 cs-5 的蓝色系，营造酒店行业奢华温暖感
/// 调色板参考高端酒店品牌 VI 规范
/// </summary>
public static class ThemeColor
{
    // ===== 主色系（暖金色） =====
    /// <summary>主色：按钮、选中态、强调元素</summary>
    public static readonly Color Primary = Color.FromArgb(200, 149, 59);      // #C8953B

    /// <summary>主色悬停态（略亮）</summary>
    public static readonly Color PrimaryHover = Color.FromArgb(220, 170, 80);  // #DCAA50

    /// <summary>主色按下态（更深）</summary>
    public static readonly Color PrimaryActive = Color.FromArgb(166, 123, 46); // #A67B2E

    /// <summary>主色浅色背景（卡片高亮、表头底色）</summary>
    public static readonly Color PrimaryLight = Color.FromArgb(255, 248, 231); // #FFF8E7

    // ===== 功能色 =====
    public static readonly Color Success = Color.FromArgb(76, 175, 80);   // #4CAF50
    public static readonly Color Warning = Color.FromArgb(255, 152, 0);   // #FF9800
    public static readonly Color Danger = Color.FromArgb(229, 57, 53);    // #E53935
    public static readonly Color Info = Color.FromArgb(126, 87, 194);     // #7E57C2

    // ===== 中性色 =====
    /// <summary>主要文字（标题、正文）</summary>
    public static readonly Color TextPrimary = Color.FromArgb(62, 39, 35);    // #3E2723 深棕

    /// <summary>次要文字（标签、说明）</summary>
    public static readonly Color TextSecondary = Color.FromArgb(141, 110, 99); // #8D6E63

    /// <summary>占位符/禁用态文字</summary>
    public static readonly Color TextPlaceholder = Color.FromArgb(188, 170, 164); // #BCAAA4

    /// <summary>边框默认色</summary>
    public static readonly Color Border = Color.FromArgb(215, 204, 190);  // #D7CCBE

    /// <summary>边框聚焦色（输入框获得焦点时）</summary>
    public static readonly Color BorderFocus = Color.FromArgb(200, 149, 59); // 同 Primary

    // ===== 背景色（暖白调） =====
    /// <summary>页面背景（窗体底色）</summary>
    public static readonly Color BgPage = Color.FromArgb(250, 247, 242);  // #FAF7F2 暖白

    /// <summary>卡片/容器背景（白色）</summary>
    public static readonly Color BgCard = Color.White;

    /// <summary>表格交替行底色</summary>
    public static readonly Color BgAltRow = Color.FromArgb(255, 251, 245); // #FFFBF5

    /// <summary>表头底色</summary>
    public static readonly Color BgHeader = Color.FromArgb(255, 248, 236); // #FFF8EC

    // ===== 深色侧栏专用（深棕木色） =====
    /// <summary>侧栏背景</summary>
    public static readonly Color SidebarBg = Color.FromArgb(62, 39, 35);    // #3E2723

    /// <summary>侧栏菜单默认文字色</summary>
    public static readonly Color SidebarText = Color.FromArgb(188, 170, 164); // #BCAAA4

    /// <summary>侧栏选中节点背景</summary>
    public static readonly Color SidebarActiveBg = Color.FromArgb(200, 149, 59); // 金

    /// <summary>侧栏选中节点文字色</summary>
    public static readonly Color SidebarActiveText = Color.White;

    /// <summary>侧栏分组节点文字色（"客房管理"等父节点）</summary>
    public static readonly Color SidebarGroupText = Color.FromArgb(161, 136, 127); // #A1887F

    // ===== 房态看板专用色（PRD 4.2.3） =====
    /// <summary>空闲房态色（绿色）</summary>
    public static readonly Color RoomFree = Color.LightGreen;

    /// <summary>在住房态色（红色）</summary>
    public static readonly Color RoomOccupied = Color.LightCoral;

    /// <summary>预留房态色（蓝色）</summary>
    public static readonly Color RoomReserved = Color.LightBlue;

    /// <summary>维护房态色（黄色）</summary>
    public static readonly Color RoomMaintenance = Color.LightYellow;

    // ===== 字体规范 =====
    public static readonly Font FontRegular = new("Microsoft YaHei UI", 9F);
    public static readonly Font FontMedium = new("Microsoft YaHei UI", 10F);
    public static readonly Font FontBold = new("Microsoft YaHei UI", 9F, FontStyle.Bold);
    public static readonly Font FontTitle = new("Microsoft YaHei UI", 14F, FontStyle.Bold);
    public static readonly Font FontHeader = new("Microsoft YaHei UI", 12F, FontStyle.Bold);
}