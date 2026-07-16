using LibraryManagement.BLL;
using LibraryManagement.Models;

namespace LibraryManagement.Forms;

/// <summary>
/// 修改密码窗体：当前登录用户修改自身密码
/// </summary>
public class ChangePasswordForm : Form
{
    private readonly UserService _userService = new();

    private readonly TextBox _txtOldPassword;
    private readonly TextBox _txtNewPassword;
    private readonly TextBox _txtConfirmPassword;
    private readonly Button _btnConfirm;
    private readonly Button _btnBack;

    /// <summary>当前登录用户</summary>
    private readonly User _currentUser;

    /// <summary>
    /// 构造修改密码窗体
    /// </summary>
    /// <param name="currentUser">当前登录用户</param>
    public ChangePasswordForm(User currentUser)
    {
        _currentUser = currentUser;

        // 窗体基本属性
        Text = "修改密码";
        Size = new Size(400, 300);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        BackColor = Color.FromArgb(245, 247, 250);

        // 旧密码
        Label lblOld = new()
        {
            Text = "旧密码：",
            Font = new Font("Microsoft YaHei UI", 10F),
            Size = new Size(100, 25),
            Location = new Point(40, 30),
            TextAlign = ContentAlignment.MiddleLeft
        };
        _txtOldPassword = new TextBox
        {
            Font = new Font("Microsoft YaHei UI", 10F),
            Size = new Size(200, 25),
            Location = new Point(140, 28),
            // 密码掩码显示，防止旁人窥屏获取明文
            UseSystemPasswordChar = true
        };

        // 新密码
        Label lblNew = new()
        {
            Text = "新密码：",
            Font = new Font("Microsoft YaHei UI", 10F),
            Size = new Size(100, 25),
            Location = new Point(40, 75),
            TextAlign = ContentAlignment.MiddleLeft
        };
        _txtNewPassword = new TextBox
        {
            Font = new Font("Microsoft YaHei UI", 10F),
            Size = new Size(200, 25),
            Location = new Point(140, 73),
            UseSystemPasswordChar = true
        };

        // 确认新密码
        Label lblConfirm = new()
        {
            Text = "确认新密码：",
            Font = new Font("Microsoft YaHei UI", 10F),
            Size = new Size(100, 25),
            Location = new Point(40, 120),
            TextAlign = ContentAlignment.MiddleLeft
        };
        _txtConfirmPassword = new TextBox
        {
            Font = new Font("Microsoft YaHei UI", 10F),
            Size = new Size(200, 25),
            Location = new Point(140, 118),
            UseSystemPasswordChar = true
        };

        // 按钮
        _btnConfirm = CreateButton("确认修改", 100, 175);
        _btnConfirm.Click += BtnConfirm_Click;
        _btnBack = CreateButton("返回", 210, 175);
        _btnBack.Click += (s, e) => Close();

        Controls.AddRange(new Control[] { lblOld, _txtOldPassword, lblNew, _txtNewPassword, lblConfirm, _txtConfirmPassword, _btnConfirm, _btnBack });
    }

    /// <summary>
    /// 确认修改按钮点击事件：校验并提交密码修改
    /// </summary>
    private void BtnConfirm_Click(object sender, EventArgs e)
    {
        try
        {
            // 密码不做 Trim，因为密码中可能合法包含首尾空格
            _userService.ChangePassword(
                _currentUser.UserName,
                _txtOldPassword.Text,
                _txtNewPassword.Text,
                _txtConfirmPassword.Text);

            MessageBox.Show("密码修改成功", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            // 修改成功后关闭窗体，返回主窗体继续操作
            Close();
        }
        // BusinessException 是业务层校验失败（如旧密码错误、两次密码不一致），属于用户可纠正的错误
        catch (BusinessException ex)
        {
            MessageBox.Show(ex.Message, "修改失败", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        // Exception 捕获系统级错误（如数据库连接失败），与业务错误区分提示
        catch (Exception ex)
        {
            MessageBox.Show($"修改失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    /// <summary>
    /// 创建统一风格的按钮（蓝色背景、白色文字、扁平样式）
    /// </summary>
    private static Button CreateButton(string text, int x, int y)
    {
        return new Button
        {
            Text = text,
            Font = new Font("Microsoft YaHei UI", 10F),
            Size = new Size(90, 32),
            Location = new Point(x, y),
            BackColor = Color.FromArgb(64, 158, 255),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            TextAlign = ContentAlignment.MiddleCenter
        };
    }
}
