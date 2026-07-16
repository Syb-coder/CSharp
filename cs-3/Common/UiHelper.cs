namespace CampusMart.Common;

/// <summary>
/// WinForms UI 布局辅助类，遵循"测量→布局"原则（来源 cs-0/experience.md 经验一）。
/// 所有 Label 使用动态测量宽度，杜绝硬编码像素值导致的 DPI/字体环境下文字截断。
/// </summary>
public static class UiHelper
{
    /// <summary>标签与输入控件之间的水平间距</summary>
    private const int ControlGap = 5;

    /// <summary>标签高度（与 TextBox/ComboBox 默认高度匹配）</summary>
    private const int LabelHeight = 25;

    /// <summary>默认字体（微软雅黑 9pt，与 ApplicationDefaultFont 一致）</summary>
    private static readonly Font DefaultFont = new("Microsoft YaHei UI", 9F);

    /// <summary>
    /// 创建固定宽度 Label，宽度根据文字内容和字体动态测量，确保文字在任何 DPI/字体环境下完整显示。
    /// 标签左对齐于指定 X 坐标，右边缘根据实际渲染宽度自动确定。
    /// </summary>
    /// <param name="text">显示文字（含冒号）</param>
    /// <param name="x">X 坐标（左边缘）</param>
    /// <param name="y">Y 坐标</param>
    /// <param name="font">字体，为 null 时使用默认 9pt 微软雅黑</param>
    /// <param name="height">高度，默认 25px</param>
    /// <param name="extraPadding">额外右侧余量像素，默认 2px 防止 DPI 取整误差</param>
    /// <returns>配置好的 Label 实例</returns>
    public static Label CreateLabel(string text, int x, int y, Font font = null, int height = LabelHeight, int extraPadding = 4)
    {
        Font f = font ?? DefaultFont;
        int measuredWidth = TextRenderer.MeasureText(text, f).Width;
        return new Label
        {
            Text = text,
            Font = f,
            AutoSize = false,
            Size = new Size(measuredWidth + extraPadding, height),
            Location = new Point(x, y),
            TextAlign = ContentAlignment.MiddleLeft,
            BackColor = Color.Transparent
        };
    }

    /// <summary>
    /// 创建右对齐 Label：标签的右边缘固定在 rightX 位置，标签向左延伸。
    /// 适用于"标签右边缘统一、输入框左对齐"的表单布局，保证视觉整齐。
    /// </summary>
    /// <param name="text">显示文字（含冒号）</param>
    /// <param name="rightX">标签右边缘的 X 坐标</param>
    /// <param name="y">Y 坐标</param>
    /// <param name="font">字体，为 null 时使用默认 9pt 微软雅黑</param>
    /// <param name="height">高度，默认 25px</param>
    /// <param name="extraPadding">额外右侧余量像素</param>
    /// <returns>配置好的 Label 实例</returns>
    public static Label CreateLabelRightAligned(string text, int rightX, int y, Font font = null, int height = LabelHeight, int extraPadding = 4)
    {
        Font f = font ?? DefaultFont;
        int measuredWidth = TextRenderer.MeasureText(text, f).Width;
        int width = measuredWidth + extraPadding;
        return new Label
        {
            Text = text,
            Font = f,
            AutoSize = false,
            Size = new Size(width, height),
            Location = new Point(rightX - width, y),
            TextAlign = ContentAlignment.MiddleRight,
            BackColor = Color.Transparent
        };
    }

    /// <summary>
    /// 根据 Label 右边缘 + 默认间距计算下一个输入控件的 X 坐标。
    /// </summary>
    /// <param name="lbl">前一个 Label 控件</param>
    /// <param name="gap">额外间距，默认 5px</param>
    /// <returns>输入控件的 X 坐标</returns>
    public static int NextX(Label lbl, int gap = ControlGap)
    {
        return lbl.Location.X + lbl.Width + gap;
    }

    /// <summary>
    /// 创建统一风格的操作按钮（蓝色主按钮风格，调用方可覆盖 BackColor 实现次要按钮灰色风格）。
    /// </summary>
    /// <param name="text">按钮文字</param>
    /// <param name="x">X 坐标</param>
    /// <param name="y">Y 坐标</param>
    /// <param name="font">字体，为 null 时使用默认 9pt 微软雅黑</param>
    /// <param name="width">按钮宽度，默认 80px</param>
    /// <param name="height">按钮高度，默认 30px</param>
    /// <returns>配置好的 Button 实例</returns>
    public static Button CreateButton(string text, int x, int y, Font font = null, int width = 80, int height = 30)
    {
        return new Button
        {
            Text = text,
            Font = font ?? DefaultFont,
            Size = new Size(width, height),
            Location = new Point(x, y),
            BackColor = Color.FromArgb(64, 158, 255),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            TextAlign = ContentAlignment.MiddleCenter
        };
    }
}
