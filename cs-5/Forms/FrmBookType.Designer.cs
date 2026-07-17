using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace LibrarySys.Forms;

public partial class FrmBookType
{
    private void InitializeComponent()
    {
        SuspendLayout();

        DoubleBuffered = true;
        Text = "图书类型管理";
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(1150, 700);
        MinimumSize = new Size(1100, 680);

        // ===== 编辑区骨架 =====
        GroupBox grpEdit = new GroupBox();
        grpEdit.Text = "类型信息";
        grpEdit.Dock = DockStyle.Bottom;
        grpEdit.Height = 195;

        _txtTypeId = new TextBox();
        _txtTypeId.Location = new Point(100, 30);
        _txtTypeId.Size = new Size(200, 23);
        grpEdit.Controls.Add(_txtTypeId);

        _txtTypeName = new TextBox();
        _txtTypeName.Location = new Point(420, 30);
        _txtTypeName.Size = new Size(200, 23);
        grpEdit.Controls.Add(_txtTypeName);

        Controls.Add(grpEdit);

        // ===== DataGridView 骨架 =====
        _dgv = new DataGridView();
        _dgv.Dock = DockStyle.Fill;
        Controls.Add(_dgv);

        ResumeLayout(false);
    }
}