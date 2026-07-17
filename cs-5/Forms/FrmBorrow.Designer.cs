using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace LibrarySys.Forms;

public partial class FrmBorrow
{
    private void InitializeComponent()
    {
        SuspendLayout();

        DoubleBuffered = true;
        Text = "借阅管理";
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(1150, 700);
        MinimumSize = new Size(1100, 680);

        // ===== 查询区骨架 =====
        GroupBox grpQuery = new GroupBox();
        grpQuery.Text = "查询条件";
        grpQuery.Dock = DockStyle.Top;
        grpQuery.Height = 125;

        _txtQueryReader = new TextBox();
        _txtQueryReader.Location = new Point(100, 30);
        _txtQueryReader.Size = new Size(200, 23);
        grpQuery.Controls.Add(_txtQueryReader);

        _txtQueryBook = new TextBox();
        _txtQueryBook.Location = new Point(420, 30);
        _txtQueryBook.Size = new Size(200, 23);
        grpQuery.Controls.Add(_txtQueryBook);

        _cboQueryStatus = new ComboBox();
        _cboQueryStatus.Location = new Point(740, 30);
        _cboQueryStatus.Size = new Size(200, 23);
        grpQuery.Controls.Add(_cboQueryStatus);

        Controls.Add(grpQuery);

        // ===== DataGridView 骨架 =====
        _dgv = new DataGridView();
        _dgv.Dock = DockStyle.Fill;
        Controls.Add(_dgv);

        // ===== 操作区骨架 =====
        GroupBox grpEdit = new GroupBox();
        grpEdit.Text = "借书操作";
        grpEdit.Dock = DockStyle.Bottom;
        grpEdit.Height = 195;

        _txtReaderId = new TextBox();
        _txtReaderId.Location = new Point(100, 30);
        _txtReaderId.Size = new Size(200, 23);
        grpEdit.Controls.Add(_txtReaderId);

        _txtBookId = new TextBox();
        _txtBookId.Location = new Point(420, 30);
        _txtBookId.Size = new Size(200, 23);
        grpEdit.Controls.Add(_txtBookId);

        _dtpBorrowDate = new DateTimePicker();
        _dtpBorrowDate.Location = new Point(740, 30);
        _dtpBorrowDate.Size = new Size(200, 23);
        grpEdit.Controls.Add(_dtpBorrowDate);

        _txtDueDate = new TextBox();
        _txtDueDate.Location = new Point(100, 70);
        _txtDueDate.Size = new Size(200, 23);
        grpEdit.Controls.Add(_txtDueDate);

        Controls.Add(grpEdit);

        ResumeLayout(false);
    }
}