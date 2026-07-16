using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace LibrarySys.Common;

/// <summary>
/// UI 辅助工具类
/// 布局策略：固定坐标 + 极其保守的尺寸参数，DpiUnaware 模式下彻底杜绝跨电脑遮挡/错位
/// 设计原则：所有尺寸都留足余量，宁大勿小，宁空勿挤
/// 视觉风格：现代扁平 + 主色蓝（#1890FF），控件样式由 ThemeColor 统一管理
/// </summary>
public static class UiHelper
{
    public static readonly Font DefaultFont = ThemeColor.FontRegular;

    // 布局常量——极其保守，全部留足余量
    private const int LABEL_WIDTH = 100;      // Label 固定宽度 100px，足够放最长的中文标签
    private const int LABEL_HEIGHT = 28;      // Label 高度加大
    private const int CTRL_HEIGHT = 28;       // 输入框高度加大
    private const int ROW_HEIGHT = 40;        // 每行高度 40px，行间距充足
    private const int LABEL_GAP = 10;         // Label 与输入框间距 10px
    private const int COL_GAP = 30;           // 列与列之间额外间距
    private const int INPUT_WIDTH = 220;      // 输入框宽度加大到 220px
    private const int COL_WIDTH = LABEL_WIDTH + LABEL_GAP + INPUT_WIDTH + COL_GAP; // 每列总宽 = 100+10+220+30 = 360

    #region ---- 基础控件 ----

    public static Label CreateLabel(string text, int x, int y)
    {
        return new Label
        {
            Text = text,
            Font = DefaultFont,
            Location = new Point(x, y),
            Size = new Size(LABEL_WIDTH, LABEL_HEIGHT),
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = ThemeColor.TextPrimary
        };
    }

    /// <summary>
    /// 创建扁平化 TextBox
    /// 通过 Enter/Leave 事件实现聚焦边框变色（背景色微调）
    /// </summary>
    public static TextBox CreateTextBox(int width = INPUT_WIDTH, bool isPassword = false)
    {
        var txt = new TextBox
        {
            Size = new Size(width, CTRL_HEIGHT),
            Font = DefaultFont,
            UseSystemPasswordChar = isPassword,
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = ThemeColor.BgCard,
            ForeColor = ThemeColor.TextPrimary
        };
        // 聚焦时背景变浅蓝，离开时还原
        txt.Enter += (_, _) => txt.BackColor = ThemeColor.PrimaryLight;
        txt.Leave += (_, _) => txt.BackColor = ThemeColor.BgCard;
        return txt;
    }

    /// <summary>创建扁平化 ComboBox</summary>
    public static ComboBox CreateComboBox(int width = INPUT_WIDTH, bool dropDownList = true)
    {
        return new ComboBox
        {
            Size = new Size(width, CTRL_HEIGHT),
            Font = DefaultFont,
            DropDownStyle = dropDownList ? ComboBoxStyle.DropDownList : ComboBoxStyle.DropDown,
            FlatStyle = FlatStyle.Flat,
            BackColor = ThemeColor.BgCard,
            ForeColor = ThemeColor.TextPrimary
        };
    }

    public static DateTimePicker CreateDateTimePicker(int width = INPUT_WIDTH) => new()
    {
        Size = new Size(width, CTRL_HEIGHT),
        Font = DefaultFont,
        Format = DateTimePickerFormat.Short
    };

    /// <summary>
    /// 创建扁平化主按钮（主色蓝 + 白字）
    /// 兼容旧调用签名，默认即为主色按钮
    /// </summary>
    public static Button CreateButton(string text, int width = 85, int height = 35)
        => CreatePrimaryButton(text, width, height);

    /// <summary>主色按钮：用于主要操作（新增、查询、登录等）</summary>
    public static Button CreatePrimaryButton(string text, int width = 85, int height = 35)
    {
        var btn = new Button
        {
            Text = text,
            Size = new Size(width, height),
            Font = DefaultFont,
            FlatStyle = FlatStyle.Flat,
            BackColor = ThemeColor.Primary,
            ForeColor = Color.White,
            Cursor = Cursors.Hand
        };
        btn.FlatAppearance.BorderColor = ThemeColor.Primary;
        btn.FlatAppearance.MouseDownBackColor = ThemeColor.PrimaryActive;
        btn.FlatAppearance.MouseOverBackColor = ThemeColor.PrimaryHover;
        return btn;
    }

    /// <summary>次要按钮：白底灰边框，用于返回、重置、取消等</summary>
    public static Button CreateSecondaryButton(string text, int width = 85, int height = 35)
    {
        var btn = new Button
        {
            Text = text,
            Size = new Size(width, height),
            Font = DefaultFont,
            FlatStyle = FlatStyle.Flat,
            BackColor = ThemeColor.BgCard,
            ForeColor = ThemeColor.TextPrimary,
            Cursor = Cursors.Hand
        };
        btn.FlatAppearance.BorderColor = ThemeColor.Border;
        btn.FlatAppearance.MouseDownBackColor = Color.FromArgb(242, 244, 247);
        btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(245, 248, 252);
        return btn;
    }

    /// <summary>危险按钮：红色，用于删除操作</summary>
    public static Button CreateDangerButton(string text, int width = 85, int height = 35)
    {
        var btn = new Button
        {
            Text = text,
            Size = new Size(width, height),
            Font = DefaultFont,
            FlatStyle = FlatStyle.Flat,
            BackColor = ThemeColor.BgCard,
            ForeColor = ThemeColor.Danger,
            Cursor = Cursors.Hand
        };
        btn.FlatAppearance.BorderColor = ThemeColor.Danger;
        btn.FlatAppearance.MouseDownBackColor = Color.FromArgb(255, 241, 240);
        btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(255, 235, 234);
        return btn;
    }

    #endregion

    #region ---- 表单布局 ----

    public class FieldDef
    {
        public string LabelText;
        public Control Control;
        public FieldDef() { }
        public FieldDef(string label, Control control) { LabelText = label; Control = control; }
    }

    /// <summary>
    /// 在指定容器中按固定坐标排列表单字段
    /// </summary>
    public static int LayoutFields(
        Control.ControlCollection container,
        IList<FieldDef> fields, int pairsPerRow,
        int startX = 20, int startY = 30)
    {
        int col = 0, row = 0;
        foreach (var f in fields)
        {
            int x = startX + col * COL_WIDTH;
            int y = startY + row * ROW_HEIGHT;

            Label lbl = CreateLabel(f.LabelText, x, y);
            container.Add(lbl);

            f.Control.Location = new Point(x + LABEL_WIDTH + LABEL_GAP, y);
            container.Add(f.Control);

            col++;
            if (col >= pairsPerRow) { col = 0; row++; }
        }

        int totalRows = row + (col > 0 ? 1 : 0);
        return startY + totalRows * ROW_HEIGHT;
    }

    #endregion

    #region ---- 按钮行 ----

    public static int LayoutButtons(
        Control.ControlCollection container,
        Button[] buttons, int x, int y)
    {
        int curX = x;
        foreach (var btn in buttons)
        {
            btn.Location = new Point(curX, y);
            container.Add(btn);
            curX += btn.Width + 10;
        }
        return y + 45;
    }

    public static int LayoutButtonsWithReturn(
        Control.ControlCollection container,
        Button[] leftButtons, Button returnBtn,
        int startX, int y, int containerWidth)
    {
        int curX = startX;
        foreach (var btn in leftButtons)
        {
            btn.Location = new Point(curX, y);
            container.Add(btn);
            curX += btn.Width + 10;
        }

        returnBtn.Location = new Point(containerWidth - returnBtn.Width - 20, y);
        returnBtn.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        container.Add(returnBtn);

        return y + 45;
    }

    #endregion

    #region ---- DataGridView 统一样式 ----

    /// <summary>
    /// 应用统一扁平化 DataGridView 样式
    /// 调用时机：构造完成后、绑定数据前
    /// </summary>
    /// <param name="dgv">待美化的 DataGridView</param>
    public static void StyleDataGridView(DataGridView dgv)
    {
        dgv.AllowUserToAddRows = false;
        dgv.AllowUserToDeleteRows = false;
        dgv.AllowUserToResizeRows = false;
        dgv.ReadOnly = true;
        dgv.RowHeadersVisible = false;
        dgv.BackgroundColor = ThemeColor.BgCard;
        dgv.BorderStyle = BorderStyle.None;
        dgv.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
        dgv.GridColor = Color.FromArgb(233, 233, 233);
        dgv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        dgv.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        dgv.MultiSelect = false;
        dgv.Font = ThemeColor.FontRegular;
        dgv.RowTemplate.Height = 32;

        // 启用双缓冲，消除滚动/选中时的闪烁卡顿
        typeof(DataGridView).InvokeMember("DoubleBuffered",
            System.Reflection.BindingFlags.SetProperty | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic,
            null, dgv, new object[] { true });

        // 表头样式
        dgv.EnableHeadersVisualStyles = false;
        dgv.ColumnHeadersDefaultCellStyle.BackColor = ThemeColor.BgHeader;
        dgv.ColumnHeadersDefaultCellStyle.ForeColor = ThemeColor.TextPrimary;
        dgv.ColumnHeadersDefaultCellStyle.Font = ThemeColor.FontBold;
        dgv.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
        dgv.ColumnHeadersDefaultCellStyle.Padding = new Padding(8, 0, 0, 0);
        dgv.ColumnHeadersDefaultCellStyle.SelectionBackColor = ThemeColor.BgHeader;
        dgv.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
        dgv.ColumnHeadersHeight = 36;
        dgv.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;

        // 单元格默认样式
        dgv.DefaultCellStyle.BackColor = ThemeColor.BgCard;
        dgv.DefaultCellStyle.ForeColor = ThemeColor.TextPrimary;
        dgv.DefaultCellStyle.SelectionBackColor = ThemeColor.PrimaryLight;
        dgv.DefaultCellStyle.SelectionForeColor = ThemeColor.TextPrimary;
        dgv.DefaultCellStyle.Padding = new Padding(8, 0, 0, 0);
        dgv.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;

        // 交替行
        dgv.AlternatingRowsDefaultCellStyle.BackColor = ThemeColor.BgAltRow;
        dgv.AlternatingRowsDefaultCellStyle.SelectionBackColor = ThemeColor.PrimaryLight;
    }

    #endregion

    #region ---- GroupBox 统一样式 ----

    /// <summary>
    /// 应用统一 GroupBox 样式（标题加粗主色 + 浅色边框）
    /// 通过重绘实现扁平化外观
    /// </summary>
    public static GroupBox CreateStyledGroupBox(string title)
    {
        var gb = new GroupBox
        {
            Text = title,
            Font = ThemeColor.FontBold,
            ForeColor = ThemeColor.Primary,
            BackColor = ThemeColor.BgCard,
            Padding = new Padding(8, 8, 8, 8)
        };
        return gb;
    }

    #endregion

    #region ---- 消息框 ----

    public static bool Confirm(string message, string title = "确认")
        => MessageBox.Show(message, title, MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes;
    public static void Info(string message, string title = "提示")
        => MessageBox.Show(message, title, MessageBoxButtons.OK, MessageBoxIcon.Information);
    public static void Error(string message, string title = "错误")
        => MessageBox.Show(message, title, MessageBoxButtons.OK, MessageBoxIcon.Error);

    #endregion

    #region ---- ComboBox ----

    public static void BindCombo<T>(ComboBox combo, List<T> dataSource,
        string displayMember, string valueMember, string placeholder = null)
    {
        combo.DisplayMember = displayMember;
        combo.ValueMember = valueMember;
        combo.DataSource = dataSource;
        if (placeholder != null) combo.SelectedIndex = -1;
    }

    #endregion

    #region ---- 双缓冲 ----

    /// <summary>
    /// 通过反射启用控件的 DoubleBuffered 属性（protected），消除滚动/重绘时的闪烁
    /// </summary>
    public static void EnableDoubleBuffered(Control control)
    {
        typeof(Control).InvokeMember("DoubleBuffered",
            System.Reflection.BindingFlags.SetProperty | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic,
            null, control, new object[] { true });
    }

    #endregion
}
