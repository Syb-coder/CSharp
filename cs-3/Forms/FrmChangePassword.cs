using CampusMart.BLL;
using CampusMart.Common;
using CampusMart.Models;

namespace CampusMart.Forms;

/// <summary>
/// 修改密码窗体
/// </summary>
/// <remarks>
/// Label 宽度使用 UiHelper 动态测量，杜绝硬编码像素导致文字截断。
/// 三个密码输入框均使用 UseSystemPasswordChar 掩码显示，防止旁人窥屏。
/// 三个标签右对齐，输入框左对齐于统一 X 坐标，保证视觉整齐。
/// </remarks>
public class FrmChangePassword : Form
{
    private readonly UserInfo _currentUser;
    private readonly UserBiz _userBiz = new();

    private readonly TextBox _txtOldPassword;
    private readonly TextBox _txtNewPassword;
    private readonly TextBox _txtConfirmPassword;
    private readonly Button _btnConfirm;
    private readonly Button _btnReturn;

    /// <summary>
    /// 构造修改密码窗体
    /// </summary>
    /// <param name="currentUser">当前登录用户</param>
    public FrmChangePassword(UserInfo currentUser)
    {
        _currentUser = currentUser;

        Text = "修改密码";
        Size = new Size(430, 280);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        BackColor = Color.FromArgb(245, 247, 250);

        Font labelFont = new("Microsoft YaHei UI", 9F);

        // 输入框统一 X 坐标（标签右边缘对齐于此 - 8）
        const int inputX = 170;
        const int labelRightX = inputX - 8;

        Label lblOld = UiHelper.CreateLabelRightAligned("旧密码：", labelRightX, 40, labelFont);
        _txtOldPassword = new TextBox
        {
            Font = labelFont,
            Size = new Size(200, 25),
            Location = new Point(inputX, 37),
            UseSystemPasswordChar = true
        };

        Label lblNew = UiHelper.CreateLabelRightAligned("新密码：", labelRightX, 80, labelFont);
        _txtNewPassword = new TextBox
        {
            Font = labelFont,
            Size = new Size(200, 25),
            Location = new Point(inputX, 77),
            UseSystemPasswordChar = true
        };

        Label lblConfirm = UiHelper.CreateLabelRightAligned("确认新密码：", labelRightX, 120, labelFont);
        _txtConfirmPassword = new TextBox
        {
            Font = labelFont,
            Size = new Size(200, 25),
            Location = new Point(inputX, 117),
            UseSystemPasswordChar = true
        };

        const int btnY = 170;
        const int btnWidth = 80;
        const int btnGap = 15;
        int btnTotalWidth = btnWidth * 2 + btnGap;
        int btnStartX = inputX + (200 - btnTotalWidth) / 2;

        _btnConfirm = UiHelper.CreateButton("确认修改", btnStartX, btnY, labelFont, btnWidth, 30);
        _btnReturn = UiHelper.CreateButton("返回", btnStartX + btnWidth + btnGap, btnY, labelFont, btnWidth, 30);
        _btnReturn.BackColor = Color.FromArgb(200, 200, 200);

        _btnConfirm.Click += BtnConfirm_Click;
        _btnReturn.Click += BtnReturn_Click;

        Controls.AddRange(new Control[]
        {
            lblOld, _txtOldPassword,
            lblNew, _txtNewPassword,
            lblConfirm, _txtConfirmPassword,
            _btnConfirm, _btnReturn
        });
    }

    /// <summary>
    /// 确认修改按钮点击事件：调用业务层修改密码
    /// </summary>
    private void BtnConfirm_Click(object sender, EventArgs e)
    {
        try
        {
            string oldPwd = _txtOldPassword.Text;
            string newPwd = _txtNewPassword.Text;
            string confirmPwd = _txtConfirmPassword.Text;

            _userBiz.ChangePassword(_currentUser.UserID, oldPwd, newPwd, confirmPwd);

            MessageBox.Show("密码修改成功", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            Close();
        }
        catch (BusinessException ex)
        {
            MessageBox.Show(ex.Message, "修改失败", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"系统错误：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    /// <summary>
    /// 返回按钮点击事件：关闭窗体
    /// </summary>
    private void BtnReturn_Click(object sender, EventArgs e)
    {
        Close();
    }
}
