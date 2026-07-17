using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace LibrarySys.Forms;

public partial class FrmUser
{
    private void InitializeComponent()
    {
        SuspendLayout();

        DoubleBuffered = true;
        Text = "用户管理";
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(1150, 700);
        MinimumSize = new Size(1100, 680);

        _txtQueryName = new TextBox();
        _txtQueryName.Location = new Point(100, 30);
        _txtQueryName.Size = new Size(200, 23);

        _cboQueryPurview = new ComboBox();
        _cboQueryPurview.Location = new Point(420, 30);
        _cboQueryPurview.Size = new Size(200, 23);

        _txtUserName = new TextBox();
        _txtUserName.Location = new Point(100, 70);
        _txtUserName.Size = new Size(200, 23);

        _txtPassword = new TextBox();
        _txtPassword.Location = new Point(420, 70);
        _txtPassword.Size = new Size(200, 23);

        _cboPurview = new ComboBox();
        _cboPurview.Location = new Point(740, 70);
        _cboPurview.Size = new Size(200, 23);

        _dgv = new DataGridView();
        _dgv.Dock = DockStyle.Fill;

        Controls.Add(_txtQueryName);
        Controls.Add(_cboQueryPurview);
        Controls.Add(_txtUserName);
        Controls.Add(_txtPassword);
        Controls.Add(_cboPurview);
        Controls.Add(_dgv);

        ResumeLayout(false);
    }
}