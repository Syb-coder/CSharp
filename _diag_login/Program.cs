using System.Drawing;
using System.Windows.Forms;

// 诊断：按 FrmLogin.cs 的代码原样重建布局，渲染为图片查看实际效果
var form = new Form();
form.Text = "校园易购信息管理系统 - 登录";
form.Size = new Size(420, 340);
form.StartPosition = FormStartPosition.CenterScreen;
form.FormBorderStyle = FormBorderStyle.FixedSingle;
form.MaximizeBox = false;
form.BackColor = Color.FromArgb(245, 247, 250);
form.Font = new Font("Microsoft YaHei UI", 9F); // 模拟 ApplicationDefaultFont

// 标题标签
var lblTitle = new Label
{
    Text = "校园易购信息管理系统",
    Font = new Font("Microsoft YaHei UI", 16F, FontStyle.Bold),
    ForeColor = Color.FromArgb(51, 51, 51),
    TextAlign = ContentAlignment.MiddleCenter,
    Size = new Size(380, 40),
    Location = new Point(15, 25)
};

// 用户名标签
var lblLoginName = new Label
{
    Text = "用户名：",
    Font = new Font("Microsoft YaHei UI", 10F),
    AutoSize = false,
    Size = new Size(75, 25),
    Location = new Point(60, 95),
    TextAlign = ContentAlignment.MiddleLeft
};

var txtLoginName = new TextBox
{
    Font = new Font("Microsoft YaHei UI", 10F),
    Size = new Size(200, 25),
    Location = new Point(140, 93)
};

// 密码标签
var lblPassword = new Label
{
    Text = "密码：",
    Font = new Font("Microsoft YaHei UI", 10F),
    AutoSize = false,
    Size = new Size(65, 25),
    Location = new Point(70, 135),
    TextAlign = ContentAlignment.MiddleLeft
};

var txtPassword = new TextBox
{
    Font = new Font("Microsoft YaHei UI", 10F),
    Size = new Size(200, 25),
    Location = new Point(140, 133),
    UseSystemPasswordChar = true
};

// 角色标签
var lblRole = new Label
{
    Text = "角色：",
    Font = new Font("Microsoft YaHei UI", 10F),
    AutoSize = false,
    Size = new Size(65, 25),
    Location = new Point(70, 175),
    TextAlign = ContentAlignment.MiddleLeft
};

var cmbRole = new ComboBox
{
    Font = new Font("Microsoft YaHei UI", 10F),
    Size = new Size(200, 25),
    Location = new Point(140, 173),
    DropDownStyle = ComboBoxStyle.DropDownList
};
cmbRole.Items.AddRange(new object[] { "管理员", "操作员" });
cmbRole.SelectedIndex = 0;

// 登录按钮
var btnLogin = new Button
{
    Text = "登录",
    Font = new Font("Microsoft YaHei UI", 10F),
    Size = new Size(95, 35),
    Location = new Point(140, 225),
    BackColor = Color.FromArgb(64, 158, 255),
    ForeColor = Color.White,
    FlatStyle = FlatStyle.Flat
};
btnLogin.FlatAppearance.BorderSize = 0;

// 退出按钮
var btnExit = new Button
{
    Text = "退出",
    Font = new Font("Microsoft YaHei UI", 10F),
    Size = new Size(95, 35),
    Location = new Point(245, 225),
    BackColor = Color.FromArgb(200, 200, 200),
    ForeColor = Color.White,
    FlatStyle = FlatStyle.Flat
};
btnExit.FlatAppearance.BorderSize = 0;

form.Controls.AddRange(new Control[]
{
    lblTitle, lblLoginName, txtLoginName,
    lblPassword, txtPassword,
    lblRole, cmbRole,
    btnLogin, btnExit
});

// 强制创建句柄
var h = form.Handle;

Console.WriteLine($"窗体 Size={form.Size} ClientSize={form.ClientSize}");
Console.WriteLine($"AutoScaleMode={form.AutoScaleMode}");
Console.WriteLine();

// 输出所有控件信息
foreach (Control c in form.Controls)
{
    string text = c.Text.Length > 15 ? c.Text.Substring(0, 15) : c.Text;
    Console.WriteLine($"  [{c.GetType().Name,-9}] '{text}'  Location={c.Location}  Size={c.Size}  Right={c.Right}  Bottom={c.Bottom}");
}
Console.WriteLine();

// 检查是否有控件超出 ClientSize
Console.WriteLine("=== 边界检查 ===");
foreach (Control c in form.Controls)
{
    if (c.Right > form.ClientSize.Width)
        Console.WriteLine($"  [超出右边] {c.Text} Right={c.Right} > ClientWidth={form.ClientSize.Width}");
    if (c.Bottom > form.ClientSize.Height)
        Console.WriteLine($"  [超出底部] {c.Text} Bottom={c.Bottom} > ClientHeight={form.ClientSize.Height}");
}

// 渲染窗体客户区到 Bitmap
int w = form.ClientSize.Width;
int ht = form.ClientSize.Height;
using var bmp = new Bitmap(w, ht);
form.DrawToBitmap(bmp, new Rectangle(0, 0, w, ht));
bmp.Save(@"c:\000\code\CSharp\_diag_login\login_preview.png");
Console.WriteLine($"\n已保存窗体截图: _diag_login/login_preview.png ({w}x{ht})");
