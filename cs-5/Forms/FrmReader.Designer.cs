using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace LibrarySys.Forms;

public partial class FrmReader
{
    private void InitializeComponent()
    {
        SuspendLayout();

        DoubleBuffered = true;
        Text = "读者信息管理";
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(1150, 700);
        MinimumSize = new Size(1100, 680);

        _txtQueryID = new TextBox();
        _txtQueryID.Location = new Point(100, 30);
        _txtQueryID.Size = new Size(200, 23);

        _txtQueryName = new TextBox();
        _txtQueryName.Location = new Point(420, 30);
        _txtQueryName.Size = new Size(200, 23);

        _txtReaderID = new TextBox();
        _txtReaderID.Location = new Point(100, 70);
        _txtReaderID.Size = new Size(200, 23);

        _txtReaderName = new TextBox();
        _txtReaderName.Location = new Point(420, 70);
        _txtReaderName.Size = new Size(200, 23);

        _cboSex = new ComboBox();
        _cboSex.Location = new Point(740, 70);
        _cboSex.Size = new Size(200, 23);

        _txtPhone = new TextBox();
        _txtPhone.Location = new Point(100, 110);
        _txtPhone.Size = new Size(200, 23);

        _txtDepartment = new TextBox();
        _txtDepartment.Location = new Point(420, 110);
        _txtDepartment.Size = new Size(200, 23);

        _dtpRegisterDate = new DateTimePicker();
        _dtpRegisterDate.Location = new Point(740, 110);
        _dtpRegisterDate.Size = new Size(200, 23);

        _dgv = new DataGridView();
        _dgv.Dock = DockStyle.Fill;

        Controls.Add(_txtQueryID);
        Controls.Add(_txtQueryName);
        Controls.Add(_txtReaderID);
        Controls.Add(_txtReaderName);
        Controls.Add(_cboSex);
        Controls.Add(_txtPhone);
        Controls.Add(_txtDepartment);
        Controls.Add(_dtpRegisterDate);
        Controls.Add(_dgv);

        ResumeLayout(false);
    }
}