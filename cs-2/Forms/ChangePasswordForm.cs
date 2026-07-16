using CampusShop.BLL;
using CampusShop.Models;

namespace CampusShop.Forms;

/// <summary>
/// 修改密码窗体：当前登录用户修改自身密码
/// </summary>
public class ChangePasswordForm : Form
{
    private readonly TextBox _txtOldPassword;
    private readonly TextBox _txtNewPassword;
    private readonly TextBox _txtConfirmPassword;
    private readonly Button _btnConfirm;
    private readonly Button _btnBack;

    /// <summary>当前登录用户</summary>
    private readonly User _currentUser;
    private readonly UserService _userService = new();

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

        // 旧密码（Label 宽度=100：4字+冒号）
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

        // 确认新密码（Label 宽度=100：5字+冒号实际需120px，这里统一用100靠右截断不美观，故用120）
        Label lblConfirm = new()
        {
            Text = "确认新密码：",
            Font = new Font("Microsoft YaHei UI", 10F),
            Size = new Size(120, 25),
            Location = new Point(20, 120),
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
    /// 确认修改按钮点击事件
    /// 调用 UserService.ChangePassword 执行密码修改（SHA-256 哈希加密）
    /// </summary>
    private void BtnConfirm_Click(object sender, EventArgs e)
    {
        string oldPwd = _txtOldPassword.Text;
        string newPwd = _txtNewPassword.Text;
        string confirmPwd = _txtConfirmPassword.Text;

        try
        {
            _userService.ChangePassword(_currentUser.UserName, oldPwd, newPwd, confirmPwd);
            MessageBox.Show("密码修改成功", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            _txtOldPassword.Clear();
            _txtNewPassword.Clear();
            _txtConfirmPassword.Clear();
        }
        catch (BusinessException ex)
        {
            MessageBox.Show(ex.Message, "校验失败", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"密码修改失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    /// <summary>
    /// 创建统一风格的操作按钮
    /// </summary>
    private static Button CreateButton(string text, int x, int y)
    {
        return new Button
        {
            Text = text,
            Font = new Font("Microsoft YaHei UI", 10F),
            Size = new Size(95, 32),
            Location = new Point(x, y),
            BackColor = Color.FromArgb(64, 158, 255),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            TextAlign = ContentAlignment.MiddleCenter
        };
    }
}
