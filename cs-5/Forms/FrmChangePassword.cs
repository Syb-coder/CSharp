using LibrarySys.BLL;
using LibrarySys.Common;
using LibrarySys.Models;
using System.ComponentModel;

namespace LibrarySys.Forms;

/// <summary>
/// 修改密码窗体：固定坐标布局，Label 测量后固定宽度，DPI 安全
/// </summary>
public partial class FrmChangePassword : Form
{
    private readonly UserBiz _biz = new();
    private readonly UserInfo _currentUser;

    private TextBox _txtOld, _txtNew, _txtConfirm;

    /// <summary>无参构造，仅供 VS 设计器使用</summary>
    public FrmChangePassword()
    {
        InitializeComponent();
        BuildUI();
    }

    public FrmChangePassword(UserInfo currentUser)
    {
        _currentUser = currentUser;
        InitializeComponent();
        BuildUI();
    }

    private void BuildUI()
    {
        // 设计器模式下跳过：设计器已在 InitializeComponent 中创建控件骨架
        if (LicenseManager.UsageMode == LicenseUsageMode.Designtime) return;
        // 运行时：清除 InitializeComponent 创建的骨架控件，重新完整构建
        Controls.Clear();

        DoubleBuffered = true;
        Text = "修改密码";
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        ClientSize = new Size(450, 270);
        Font = UiHelper.DefaultFont;

        // 标题
        Label lblTitle = new Label()
        {
            Text = $"当前用户：{_currentUser?.UserName ?? "（设计器预览）"}",
            AutoSize = false,
            TextAlign = ContentAlignment.MiddleLeft,
            Location = new Point(75, 15),
            Size = new Size(300, 25)
        };
        Controls.Add(lblTitle);

        // 表单字段
        _txtOld = UiHelper.CreateTextBox(200, isPassword: true);
        _txtNew = UiHelper.CreateTextBox(200, isPassword: true);
        _txtConfirm = UiHelper.CreateTextBox(200, isPassword: true);

        UiHelper.LayoutFields(Controls, new List<UiHelper.FieldDef>
        {
            new("旧密码：", _txtOld),
            new("新密码：", _txtNew),
            new("确认新密码：", _txtConfirm),
        }, pairsPerRow: 1, startX: 60, startY: 60);

        // 按钮
        Button btnSubmit = UiHelper.CreateButton("确认修改", 90);
        Button btnReturn = UiHelper.CreateButton("返回", 75);
        btnSubmit.Click += (_, _) => DoChange();
        btnReturn.Click += (_, _) => Close();
        btnSubmit.Location = new Point(125, 205);
        btnReturn.Location = new Point(245, 205);
        Controls.Add(btnSubmit);
        Controls.Add(btnReturn);

        AcceptButton = btnSubmit;
    }

    /// <summary>
    /// 执行密码修改操作
    /// </summary>
    private void DoChange()
    {
        try
        {
            _biz.ChangePassword(
                _currentUser.UserName,
                _txtOld.Text,
                _txtNew.Text,
                _txtConfirm.Text
            );
            UiHelper.Info("密码修改成功，下次登录请使用新密码");
            Close();
        }
        catch (BusinessException ex) { UiHelper.Error(ex.Message); }
        catch (Exception ex) { UiHelper.Error($"操作失败：{ex.Message}"); }
    }
}
