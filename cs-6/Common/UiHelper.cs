using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace HotelSys.Common;

/// <summary>
/// UI 辅助工具类
/// 布局策略：响应式布局 + 固定坐标兼容，DpiUnaware 模式下彻底杜绝跨电脑遮挡/错位
/// 设计原则：所有尺寸都留足余量，宁大勿小，宁空勿挤
/// 视觉风格：现代扁平 + 暖金色（#C8953B），控件样式由 ThemeColor 统一管理
/// </summary>
public static class UiHelper
{
    public static readonly Font DefaultFont = ThemeColor.FontRegular;

    // 布局常量——极其保守，全部留足余量
    private const int LABEL_WIDTH = 100;      // Label 固定宽度 100px，足够放最长的中文标签
    private const int LABEL_HEIGHT = 28;      // Label 高度
    private const int CTRL_HEIGHT = 28;       // 输入框高度
    private const int ROW_HEIGHT = 40;        // 每行高度 40px，行间距充足
    private const int LABEL_GAP = 10;         // Label 与输入框间距 10px
    private const int COL_GAP = 30;           // 列与列之间额外间距
    private const int INPUT_WIDTH = 220;      // 输入框默认宽度
    private const int COL_WIDTH = LABEL_WIDTH + LABEL_GAP + INPUT_WIDTH + COL_GAP; // 每列总宽 = 100+10+220+30 = 360

    // 响应式布局阈值
    private const int MIN_COL_WIDTH = COL_WIDTH + 40; // 单列最小宽度含内边距 = 400
    private const int QUERY_ROW_HEIGHT = 36;   // 查询区每行高度
    private const int BTN_ROW_HEIGHT = 45;     // 按钮行高度

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
    /// 创建扁平化 TextBox，聚焦时背景变浅色
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

    /// <summary>兼容旧调用签名</summary>
    public static Button CreateButton(string text, int width = 85, int height = 35)
        => CreatePrimaryButton(text, width, height);

    /// <summary>主色按钮</summary>
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

    /// <summary>次要按钮</summary>
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

    /// <summary>危险按钮：红色</summary>
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

    #region ---- 响应式布局引擎 ----

    /// <summary>
    /// 根据容器可用宽度自动计算最佳列数（响应式）
    /// 单列最小宽度 = 400px，优先保持 2 列，窄屏降为 1 列，宽屏升为 3 列
    /// </summary>
    public static int CalcPairsPerRow(int containerWidth)
    {
        if (containerWidth >= MIN_COL_WIDTH * 3) return 3;
        if (containerWidth >= MIN_COL_WIDTH * 2) return 2;
        return 1;
    }

    /// <summary>
    /// 根据字段数和每行列数，计算编辑区 GroupBox 所需高度（含按钮行）
    /// </summary>
    public static int CalcEditHeight(int fieldCount, int pairsPerRow, int buttonRows = 1)
    {
        int fieldRows = (fieldCount + pairsPerRow - 1) / pairsPerRow; // 向上取整
        return 30 + fieldRows * ROW_HEIGHT + buttonRows * BTN_ROW_HEIGHT + 20;
    }

    /// <summary>
    /// 根据查询区控件数量计算查询区 GroupBox 所需高度
    /// </summary>
    public static int CalcQueryHeight(int controlCount, int pairsPerRow)
    {
        int rows = (controlCount + pairsPerRow - 1) / pairsPerRow;
        return 10 + rows * QUERY_ROW_HEIGHT + 10;
    }

    /// <summary>
    /// 根据 GroupBox 内 FlowLayoutPanel 的实际内容计算查询区所需高度
    /// 彻底解决 AutoSize+Dock=Fill 冲突和硬编码控件数导致的控件遮盖问题
    /// </summary>
    /// <param name="box">包含 FlowLayoutPanel 的查询区 GroupBox</param>
    /// <returns>GroupBox 应有的高度（含标题和 padding）</returns>
    public static int CalcQueryHeightByGroupBox(GroupBox box)
    {
        if (box == null || box.Width <= 0) return 56;
        foreach (Control c in box.Controls)
        {
            if (c is FlowLayoutPanel flow)
            {
                // 获取 FlowLayoutPanel 在当前宽度下的首选高度（考虑自动换行）
                var prefSize = flow.GetPreferredSize(new Size(box.Width - 16, 0));
                return prefSize.Height + 24; // GroupBox 标题约 20px + 底部余量
            }
        }
        return 56; // 默认高度
    }

    public class FieldDef
    {
        public string LabelText;
        public Control Control;
        public FieldDef() { }
        public FieldDef(string label, Control control) { LabelText = label; Control = control; }
    }

    /// <summary>
    /// 响应式排列表单字段：根据容器宽度自动计算列数，杜绝控件遮盖
    /// </summary>
    /// <param name="container">容器控件集合</param>
    /// <param name="fields">字段列表</param>
    /// <param name="containerWidth">容器可用宽度（用于计算列数）</param>
    /// <param name="pairsPerRow">每行列数（0=自动计算）</param>
    /// <param name="startX">起始 X 坐标</param>
    /// <param name="startY">起始 Y 坐标</param>
    /// <returns>按钮区起始 Y 坐标</returns>
    public static int LayoutFields(
        Control.ControlCollection container,
        IList<FieldDef> fields, int containerWidth,
        int pairsPerRow = 0, int startX = 20, int startY = 30)
    {
        // 自动计算列数
        if (pairsPerRow <= 0)
            pairsPerRow = CalcPairsPerRow(containerWidth);

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

    #region ---- 查询区构建器（响应式，FlowLayoutPanel 自适应换行）----

    /// <summary>
    /// 查询区控件对（Label + 输入控件）
    /// </summary>
    public class QueryPair
    {
        public string LabelText;
        public Control Control;
        public QueryPair() { }
        public QueryPair(string label, Control control) { LabelText = label; Control = control; }
    }

    /// <summary>
    /// 构建响应式查询区：使用 FlowLayoutPanel 自动换行，彻底杜绝控件溢出遮盖
    /// 使用时将返回的 FlowLayoutPanel 添加到 GroupBox 中（Dock=Fill）
    /// </summary>
    /// <param name="pairs">Label + 控件对列表</param>
    /// <param name="buttons">查询区末尾的按钮</param>
    /// <returns>FlowLayoutPanel（已包含所有控件），调用方需自行添加到容器</returns>
    public static FlowLayoutPanel BuildQueryPanel(IList<QueryPair> pairs, params Button[] buttons)
    {
        // 注意：不设置 AutoSize=true，因为 AutoSize 与 Dock=Fill 冲突会导致
        // FlowLayoutPanel 高度超出 GroupBox 边界，遮盖下方 DataGridView
        var panel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            WrapContents = true,
            Padding = new Padding(16, 6, 16, 6),
            BackColor = ThemeColor.BgCard
        };

        foreach (var pair in pairs)
        {
            // 每个 Label+Control 对包装在一个小 Panel 中，保持水平排列
            Panel pairPanel = new()
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Margin = new Padding(3, 3, 8, 3),
                BackColor = ThemeColor.BgCard
            };

            Label lbl = new()
            {
                Text = pair.LabelText,
                Font = DefaultFont,
                AutoSize = true,
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = ThemeColor.TextPrimary,
                Location = new Point(0, 4)
            };

            pair.Control.Location = new Point(lbl.PreferredWidth + 4, 0);
            pair.Control.Margin = new Padding(0);

            pairPanel.Controls.Add(lbl);
            pairPanel.Controls.Add(pair.Control);
            // 让 pairPanel 宽度自适应
            pairPanel.Width = lbl.PreferredWidth + 4 + pair.Control.Width + 4;

            panel.Controls.Add(pairPanel);
        }

        // 添加按钮
        foreach (var btn in buttons)
        {
            btn.Margin = new Padding(3, 3, 3, 3);
            panel.Controls.Add(btn);
        }

        return panel;
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

    /// <summary>
    /// 响应式按钮行：左侧按钮水平排列 + 右侧按钮靠右
    /// 使用 FlowLayoutPanel 防止容器过窄时按钮重叠
    /// </summary>
    public static FlowLayoutPanel BuildButtonRow(
        Button[] leftButtons, Button rightButton, int containerWidth)
    {
        var panel = new FlowLayoutPanel
        {
            Dock = DockStyle.None,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            WrapContents = false,
            BackColor = ThemeColor.BgCard,
            FlowDirection = FlowDirection.LeftToRight
        };

        // 左侧按钮
        foreach (var btn in leftButtons)
        {
            btn.Margin = new Padding(0, 0, 8, 0);
            panel.Controls.Add(btn);
        }

        // 右侧按钮：用空 Panel 撑开距离
        int leftTotalWidth = leftButtons.Sum(b => b.Width + 8);
        int spacerWidth = Math.Max(0, containerWidth - leftTotalWidth - rightButton.Width - 40);
        if (spacerWidth > 0)
        {
            Panel spacer = new()
            {
                Size = new Size(spacerWidth, 1),
                Margin = new Padding(0),
                BackColor = ThemeColor.BgCard
            };
            panel.Controls.Add(spacer);
        }

        rightButton.Margin = new Padding(0, 0, 0, 0);
        panel.Controls.Add(rightButton);

        return panel;
    }

    /// <summary>兼容旧接口（内部使用响应式布局）</summary>
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
    /// </summary>
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

        typeof(DataGridView).InvokeMember("DoubleBuffered",
            System.Reflection.BindingFlags.SetProperty | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic,
            null, dgv, new object[] { true });

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

        dgv.DefaultCellStyle.BackColor = ThemeColor.BgCard;
        dgv.DefaultCellStyle.ForeColor = ThemeColor.TextPrimary;
        dgv.DefaultCellStyle.SelectionBackColor = ThemeColor.PrimaryLight;
        dgv.DefaultCellStyle.SelectionForeColor = ThemeColor.TextPrimary;
        dgv.DefaultCellStyle.Padding = new Padding(8, 0, 0, 0);
        dgv.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;

        dgv.AlternatingRowsDefaultCellStyle.BackColor = ThemeColor.BgAltRow;
        dgv.AlternatingRowsDefaultCellStyle.SelectionBackColor = ThemeColor.PrimaryLight;
    }

    #endregion

    #region ---- GroupBox 统一样式 ----

    public static GroupBox CreateStyledGroupBox(string title)
    {
        return new GroupBox
        {
            Text = title,
            Font = ThemeColor.FontBold,
            ForeColor = ThemeColor.Primary,
            BackColor = ThemeColor.BgCard,
            Padding = new Padding(8, 8, 8, 8)
        };
    }

    #endregion

    #region ---- 消息框 ----

    public static bool Confirm(string message, string title = "确认")
        => MessageBox.Show(message, title, MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes;
    public static void Info(string message, string title = "提示")
        => MessageBox.Show(message, title, MessageBoxButtons.OK, MessageBoxIcon.Information);
    public static void Error(string message, string title = "错误")
        => MessageBox.Show(message, title, MessageBoxButtons.OK, MessageBoxIcon.Error);
    public static void Warning(string message, string title = "警告")
        => MessageBox.Show(message, title, MessageBoxButtons.OK, MessageBoxIcon.Warning);

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

    public static void EnableDoubleBuffered(Control control)
    {
        typeof(Control).InvokeMember("DoubleBuffered",
            System.Reflection.BindingFlags.SetProperty | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic,
            null, control, new object[] { true });
    }

    #endregion
}