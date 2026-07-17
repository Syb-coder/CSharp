using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace LibrarySys.Forms;

public partial class FrmLogin
{
    private void InitializeComponent()
    {
        SuspendLayout();

        Text = "图书馆信息管理系统 - 登录";
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        ClientSize = new Size(480, 530);

        _txtUserName = new TextBox();
        _txtUserName.Location = new Point(100, 215);
        _txtUserName.Size = new Size(280, 23);
        Controls.Add(_txtUserName);

        _txtPassword = new TextBox();
        _txtPassword.Location = new Point(100, 275);
        _txtPassword.Size = new Size(280, 23);
        Controls.Add(_txtPassword);

        _cmbPurview = new ComboBox();
        _cmbPurview.Location = new Point(100, 335);
        _cmbPurview.Size = new Size(280, 23);
        Controls.Add(_cmbPurview);

        ResumeLayout(false);
    }
}