using HotelSys.BLL;
using HotelSys.Common;
using HotelSys.Models;

namespace HotelSys.Forms;

/// <summary>
/// 修改密码窗体（PRD 4.2.12 / F-22 / AC-22）
/// 旧密码验证通过后才可修改新密码
/// </summary>
public class FrmChangePassword : Form
{
    private readonly UserManager _mgr = new();
    private readonly UserInfo _currentUser;

    private TextBox _txtOldPwd, _txtNewPwd, _txtConfirmPwd;
    private Button _btnConfirm, _btnCancel;
    private Panel _mainPanel;
    private List<UiHelper.FieldDef> _editFields;

    public FrmChangePassword(UserInfo currentUser) : this()
    {
        _currentUser = currentUser;
    }

    public FrmChangePassword()
    {
        DoubleBuffered = true;
        InitializeUI();
    }

    private void InitializeUI()
    {
        Text = "修改密码";
        BackColor = ThemeColor.BgPage;
        Font = UiHelper.DefaultFont;
        StartPosition = FormStartPosition.CenterParent;

        _mainPanel = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(40, 30, 40, 30),
            BackColor = ThemeColor.BgCard
        };

        Label lblTitle = new()
        {
            Text = "修改密码",
            Font = ThemeColor.FontTitle,
            ForeColor = ThemeColor.TextPrimary,
            Location = new Point(0, 0),
            Size = new Size(300, 40),
            TextAlign = ContentAlignment.MiddleLeft
        };

        _txtOldPwd = UiHelper.CreateTextBox(250, isPassword: true);
        _txtNewPwd = UiHelper.CreateTextBox(250, isPassword: true);
        _txtConfirmPwd = UiHelper.CreateTextBox(250, isPassword: true);

        _editFields = new List<UiHelper.FieldDef>
        {
            new("旧密码：", _txtOldPwd),
            new("新密码：", _txtNewPwd),
            new("确认密码：", _txtConfirmPwd),
        };

        _btnConfirm = UiHelper.CreatePrimaryButton("确认修改", 120, 38);
        _btnConfirm.Click += (_, _) => DoChange();

        _btnCancel = UiHelper.CreateSecondaryButton("清空", 85, 38);
        _btnCancel.Click += (_, _) => ClearInput();

        _mainPanel.Controls.Add(lblTitle);
        Controls.Add(_mainPanel);

        LayoutEditArea();

        Resize += (_, _) => LayoutEditArea();
        Shown += (_, _) => LayoutEditArea();
    }

    /// <summary>按当前容器宽度重新布局编辑区</summary>
    private void LayoutEditArea()
    {
        // 只清理动态添加的控件，保留标题标签
        _mainPanel.Controls.Clear();
        _mainPanel.Controls.Add(new Label
        {
            Text = "修改密码",
            Font = ThemeColor.FontTitle,
            ForeColor = ThemeColor.TextPrimary,
            Location = new Point(0, 0),
            Size = new Size(300, 40),
            TextAlign = ContentAlignment.MiddleLeft
        });

        int containerW = _mainPanel.Width - 80;
        int btnY = UiHelper.LayoutFields(_mainPanel.Controls, _editFields, containerW, 0, 20, 60);

        UiHelper.LayoutButtons(_mainPanel.Controls,
            new[] { _btnConfirm, _btnCancel }, 20, btnY + 20);
    }

    private void ClearInput()
    {
        _txtOldPwd.Text = "";
        _txtNewPwd.Text = "";
        _txtConfirmPwd.Text = "";
    }

    private void DoChange()
    {
        try
        {
            if (_txtNewPwd.Text != _txtConfirmPwd.Text)
            {
                UiHelper.Warning("两次输入的新密码不一致");
                return;
            }
            string userName = _currentUser?.UserName ?? CurrentUser.UserName;
            _mgr.ChangePassword(userName, _txtOldPwd.Text, _txtNewPwd.Text);
            UiHelper.Info("密码修改成功");
            ClearInput();
        }
        catch (BusinessException ex) { UiHelper.Warning(ex.Message); }
        catch (Exception ex) { UiHelper.Error("密码修改失败：" + ex.Message); }
    }
}
