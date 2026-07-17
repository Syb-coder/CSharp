using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace LibrarySys.Forms;

public partial class FrmFine
{
    private void InitializeComponent()
    {
        SuspendLayout();

        DoubleBuffered = true;
        Text = "罚款管理";
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(1150, 700);
        MinimumSize = new Size(1100, 680);

        _txtQueryReader = new TextBox();
        _txtQueryReader.Location = new Point(100, 30);
        _txtQueryReader.Size = new Size(200, 23);

        _cboQueryStatus = new ComboBox();
        _cboQueryStatus.Location = new Point(420, 30);
        _cboQueryStatus.Size = new Size(200, 23);

        _dgv = new DataGridView();
        _dgv.Dock = DockStyle.Fill;

        Controls.Add(_txtQueryReader);
        Controls.Add(_cboQueryStatus);
        Controls.Add(_dgv);

        ResumeLayout(false);
    }
}