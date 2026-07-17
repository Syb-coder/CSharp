using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace LibrarySys.Forms;

public partial class FrmChangePassword
{
    private void InitializeComponent()
    {
        SuspendLayout();

        DoubleBuffered = true;
        Text = "修改密码";
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        ClientSize = new Size(450, 270);

        _txtOld = new TextBox();
        _txtOld.Location = new Point(120, 60);
        _txtOld.Size = new Size(200, 23);
        Controls.Add(_txtOld);

        _txtNew = new TextBox();
        _txtNew.Location = new Point(120, 100);
        _txtNew.Size = new Size(200, 23);
        Controls.Add(_txtNew);

        _txtConfirm = new TextBox();
        _txtConfirm.Location = new Point(120, 140);
        _txtConfirm.Size = new Size(200, 23);
        Controls.Add(_txtConfirm);

        ResumeLayout(false);
    }
}