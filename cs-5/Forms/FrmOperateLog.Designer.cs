using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace LibrarySys.Forms;

public partial class FrmOperateLog
{
    private void InitializeComponent()
    {
        SuspendLayout();

        DoubleBuffered = true;
        Text = "操作日志";
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(1150, 700);
        MinimumSize = new Size(1100, 680);

        _dtpFrom = new DateTimePicker();
        _dtpFrom.Location = new Point(85, 22);
        _dtpFrom.Size = new Size(130, 23);

        _dtpTo = new DateTimePicker();
        _dtpTo.Location = new Point(295, 22);
        _dtpTo.Size = new Size(130, 23);

        _cmbType = new ComboBox();
        _cmbType.Location = new Point(505, 22);
        _cmbType.Size = new Size(100, 23);

        _txtUserName = new TextBox();
        _txtUserName.Location = new Point(680, 22);
        _txtUserName.Size = new Size(100, 23);

        _btnSearch = new Button();
        _btnSearch.Text = "查询";
        _btnSearch.Location = new Point(795, 20);
        _btnSearch.Size = new Size(70, 23);

        _btnClear = new Button();
        _btnClear.Text = "清空";
        _btnClear.Location = new Point(875, 20);
        _btnClear.Size = new Size(70, 23);

        _dgv = new DataGridView();
        _dgv.Dock = DockStyle.Fill;

        Controls.Add(_dtpFrom);
        Controls.Add(_dtpTo);
        Controls.Add(_cmbType);
        Controls.Add(_txtUserName);
        Controls.Add(_btnSearch);
        Controls.Add(_btnClear);
        Controls.Add(_dgv);

        ResumeLayout(false);
    }
}