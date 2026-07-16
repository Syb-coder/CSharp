using System.Drawing;
using System.Text;
using System.Windows.Forms;
using CampusStore.BLL;

namespace CampusStore.Common;

/// <summary>
/// UI 通用工具类
/// </summary>
/// <remarks>
/// 封装 WinForms 通用操作：
///   - Label 宽度测量后固定（参见 cs-0/experience.md 经验一）
///   - 统一异常处理（区分 BusinessException 业务异常与系统异常）
///   - DataGridView 样式设置
///   - 删除确认对话框
/// </remarks>
public static class UiHelper
{
    /// <summary>
    /// 创建固定宽度 Label，宽度根据文字内容自动测量
    /// </summary>
    /// <param name="text">显示文字</param>
    /// <param name="x">X 坐标</param>
    /// <param name="y">Y 坐标</param>
    /// <returns>配置好的 Label，宽度已固定为实际渲染值</returns>
    /// <remarks>
    /// 经验一：AutoSize=true 导致宽度不可控，与预设的 TextBox X 坐标重叠。
    /// 此方法先用 AutoSize 测量真实宽度，再固定，彻底解决遮盖问题。
    /// 注意：使用 TextRenderer.MeasureText 而非 Graphics.FromHwnd(IntPtr.Zero).MeasureString，
    /// 后者获取桌面 DC（固定 96 DPI），在 125%/150% 缩放下测量值偏小导致文字截断。
    /// </remarks>
    public static Label CreateLabel(string text, int x, int y)
    {
        Label lbl = new()
        {
            Text = text,
            Font = new Font("Microsoft YaHei UI", 9F),
            AutoSize = true,
            Location = new Point(x, y),
            TextAlign = ContentAlignment.MiddleLeft,
            BackColor = Color.Transparent
        };
        // 使用 TextRenderer 测量文字宽度，天然适配当前 DPI 缩放
        Size measured = TextRenderer.MeasureText(text, lbl.Font);
        lbl.AutoSize = false;
        lbl.Size = new Size(measured.Width + 2, 20); // +2px 保险余量
        return lbl;
    }

    /// <summary>
    /// 根据 Label 右边缘计算下一个输入控件的 X 坐标
    /// </summary>
    /// <param name="lbl">前一个 Label</param>
    /// <param name="gap">间距，默认 5px</param>
    /// <returns>输入控件的 X 坐标</returns>
    public static int NextX(Label lbl, int gap = 5) => lbl.Location.X + lbl.Width + gap;

    /// <summary>
    /// 统一异常处理：业务异常友好提示，系统异常显示完整信息
    /// </summary>
    /// <param name="ex">异常对象</param>
    /// <param name="owner">父窗体（可为 null）</param>
    public static void HandleException(Exception ex, IWin32Window owner = null)
    {
        if (ex is BusinessException bizEx)
        {
            MessageBox.Show(owner, bizEx.Message, "提示",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        else
        {
            // 系统异常：显示完整信息便于排查（开发阶段）
            // 生产环境应仅显示友好提示并记录日志
            string msg = $"系统错误：{ex.Message}";
            if (ex.InnerException != null)
                msg += $"\n\n内部异常：{ex.InnerException.Message}";
            MessageBox.Show(owner, msg, "系统错误",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    /// <summary>
    /// 设置 DataGridView 通用样式
    /// </summary>
    /// <param name="grid">待配置的 DataGridView</param>
    public static void SetGridStyle(DataGridView grid)
    {
        grid.AllowUserToAddRows = false;
        grid.AllowUserToDeleteRows = false;
        grid.AllowUserToResizeRows = false;
        grid.ReadOnly = true;
        grid.MultiSelect = false;
        grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        grid.BackgroundColor = Color.White;
        grid.BorderStyle = BorderStyle.None;
        grid.RowHeadersVisible = false;
        // 列标题样式
        grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(245, 246, 247);
        grid.ColumnHeadersDefaultCellStyle.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold);
        grid.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
        // 行样式
        grid.DefaultCellStyle.BackColor = Color.White;
        grid.DefaultCellStyle.ForeColor = Color.FromArgb(51, 51, 51);
        grid.DefaultCellStyle.Font = new Font("Microsoft YaHei UI", 9F);
        grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(232, 240, 254);
        grid.DefaultCellStyle.SelectionForeColor = Color.FromArgb(51, 51, 51);
        // 行高
        grid.RowTemplate.Height = 28;
        // 交替行颜色
        grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(250, 250, 252);
    }

    /// <summary>
    /// 删除确认对话框
    /// </summary>
    /// <param name="itemName">待删除项名称（用于提示）</param>
    /// <param name="owner">父窗体</param>
    /// <returns>用户选择 Yes 返回 true，否则 false</returns>
    public static bool ConfirmDelete(string itemName, IWin32Window owner = null)
    {
        return MessageBox.Show(owner,
            $"确定要删除 [{itemName}] 吗？此操作不可撤销。",
            "删除确认",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question) == DialogResult.Yes;
    }

    /// <summary>
    /// 设置窗体通用属性
    /// </summary>
    /// <param name="form">待配置的窗体</param>
    /// <param name="title">窗体标题</param>
    /// <param name="width">宽度</param>
    /// <param name="height">高度</param>
    public static void SetupForm(Form form, string title, int width, int height)
    {
        form.Text = title;
        form.Size = new Size(width, height);
        form.StartPosition = FormStartPosition.CenterParent;
        form.FormBorderStyle = FormBorderStyle.FixedDialog;
        form.MaximizeBox = false;
        form.MinimizeBox = false;
        form.Font = new Font("Microsoft YaHei UI", 9F);
        form.BackColor = Color.White;
    }

    /// <summary>
    /// 创建标准按钮（蓝色主按钮）
    /// </summary>
    public static Button CreatePrimaryButton(string text, int x, int y, int width = 80, int height = 30)
    {
        return new Button
        {
            Text = text,
            Location = new Point(x, y),
            Size = new Size(width, height),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(64, 132, 244),
            ForeColor = Color.White,
            Font = new Font("Microsoft YaHei UI", 9F),
            Cursor = Cursors.Hand
        };
    }

    /// <summary>
    /// 创建标准按钮（灰色次要按钮）
    /// </summary>
    public static Button CreateSecondaryButton(string text, int x, int y, int width = 80, int height = 30)
    {
        return new Button
        {
            Text = text,
            Location = new Point(x, y),
            Size = new Size(width, height),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(245, 246, 247),
            ForeColor = Color.FromArgb(51, 51, 51),
            Font = new Font("Microsoft YaHei UI", 9F),
            Cursor = Cursors.Hand
        };
    }

    /// <summary>
    /// 创建标准 TextBox
    /// </summary>
    public static TextBox CreateTextBox(int x, int y, int width = 150, bool isPassword = false)
    {
        return new TextBox
        {
            Location = new Point(x, y),
            Size = new Size(width, 25),
            Font = new Font("Microsoft YaHei UI", 9F),
            UseSystemPasswordChar = isPassword,
            BorderStyle = BorderStyle.FixedSingle
        };
    }

    /// <summary>
    /// 创建标准 ComboBox
    /// </summary>
    public static ComboBox CreateComboBox(int x, int y, int width = 150)
    {
        return new ComboBox
        {
            Location = new Point(x, y),
            Size = new Size(width, 25),
            Font = new Font("Microsoft YaHei UI", 9F),
            DropDownStyle = ComboBoxStyle.DropDownList,
            FlatStyle = FlatStyle.Flat
        };
    }
}
