using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace LibrarySys.Forms;

/// <summary>
/// FrmBook 的设计器分部类
/// VS 设计器通过 RoslynCodeDom 解析 InitializeComponent 渲染预览
/// 仅支持：new 控件赋给字段 + 简单属性赋值 + Controls.Add
/// 不支持：if/try-catch/lambda/静态方法调用
/// </summary>
public partial class FrmBook
{
    /// <summary>
    /// 设计器入口方法
    /// 设计器模式下创建控件骨架用于预览，运行时骨架会被 BuildUI() 清除并重建
    /// </summary>
    private void InitializeComponent()
    {
        SuspendLayout();

        DoubleBuffered = true;
        Text = "图书信息管理";
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(1150, 700);
        MinimumSize = new Size(1100, 680);

        // ===== 查询区骨架 =====
        GroupBox grpQuery = new GroupBox();
        grpQuery.Text = "查询条件";
        grpQuery.Dock = DockStyle.Top;
        grpQuery.Height = 125;

        _txtQueryName = new TextBox();
        _txtQueryName.Location = new Point(100, 30);
        _txtQueryName.Size = new Size(200, 23);
        grpQuery.Controls.Add(_txtQueryName);

        _cboQueryType = new ComboBox();
        _cboQueryType.Location = new Point(420, 30);
        _cboQueryType.Size = new Size(200, 23);
        grpQuery.Controls.Add(_cboQueryType);

        _txtQueryPub = new TextBox();
        _txtQueryPub.Location = new Point(740, 30);
        _txtQueryPub.Size = new Size(200, 23);
        grpQuery.Controls.Add(_txtQueryPub);

        Controls.Add(grpQuery);

        // ===== DataGridView 骨架 =====
        _dgv = new DataGridView();
        _dgv.Dock = DockStyle.Fill;
        Controls.Add(_dgv);

        // ===== 编辑区骨架 =====
        GroupBox grpEdit = new GroupBox();
        grpEdit.Text = "图书信息";
        grpEdit.Dock = DockStyle.Bottom;
        grpEdit.Height = 195;

        _txtBookId = new TextBox();
        _txtBookId.Location = new Point(100, 30);
        _txtBookId.Size = new Size(200, 23);
        grpEdit.Controls.Add(_txtBookId);

        _txtBookName = new TextBox();
        _txtBookName.Location = new Point(420, 30);
        _txtBookName.Size = new Size(200, 23);
        grpEdit.Controls.Add(_txtBookName);

        _cboType = new ComboBox();
        _cboType.Location = new Point(740, 30);
        _cboType.Size = new Size(200, 23);
        grpEdit.Controls.Add(_cboType);

        _txtAuthor = new TextBox();
        _txtAuthor.Location = new Point(100, 70);
        _txtAuthor.Size = new Size(200, 23);
        grpEdit.Controls.Add(_txtAuthor);

        _txtPublisher = new TextBox();
        _txtPublisher.Location = new Point(420, 70);
        _txtPublisher.Size = new Size(200, 23);
        grpEdit.Controls.Add(_txtPublisher);

        _dtpPublishDate = new DateTimePicker();
        _dtpPublishDate.Location = new Point(740, 70);
        _dtpPublishDate.Size = new Size(200, 23);
        grpEdit.Controls.Add(_dtpPublishDate);

        _txtISBN = new TextBox();
        _txtISBN.Location = new Point(100, 110);
        _txtISBN.Size = new Size(180, 23);
        grpEdit.Controls.Add(_txtISBN);

        _txtPrice = new TextBox();
        _txtPrice.Location = new Point(420, 110);
        _txtPrice.Size = new Size(100, 23);
        grpEdit.Controls.Add(_txtPrice);

        _txtCount = new TextBox();
        _txtCount.Location = new Point(740, 110);
        _txtCount.Size = new Size(100, 23);
        grpEdit.Controls.Add(_txtCount);

        Controls.Add(grpEdit);

        ResumeLayout(false);
    }
}