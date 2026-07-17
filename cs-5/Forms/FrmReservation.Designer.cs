using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace LibrarySys.Forms;

/// <summary>
/// FrmReservation 的设计器分部类
/// VS 设计器通过 RoslynCodeDom 解析 InitializeComponent 渲染预览
/// 仅支持：new 控件赋给字段 + 简单属性赋值 + Controls.Add
/// 不支持：if/try-catch/lambda/静态方法调用
/// </summary>
public partial class FrmReservation
{
    /// <summary>
    /// 设计器入口方法
    /// 设计器模式下创建控件骨架用于预览，运行时骨架会被 BuildUI() 清除并重建
    /// </summary>
    private void InitializeComponent()
    {
        SuspendLayout();

        DoubleBuffered = true;
        Text = "预约管理";
        ClientSize = new Size(1000, 600);

        // ===== 查询区骨架 =====
        GroupBox grpQuery = new GroupBox();
        grpQuery.Text = "查询条件";
        grpQuery.Dock = DockStyle.Top;
        grpQuery.Height = 70;

        _txtReaderID = new TextBox();
        _txtReaderID.Location = new Point(85, 22);
        _txtReaderID.Size = new Size(120, 23);
        grpQuery.Controls.Add(_txtReaderID);

        _txtBookID = new TextBox();
        _txtBookID.Location = new Point(290, 22);
        _txtBookID.Size = new Size(120, 23);
        grpQuery.Controls.Add(_txtBookID);

        _cmbStatus = new ComboBox();
        _cmbStatus.Location = new Point(465, 22);
        _cmbStatus.Size = new Size(100, 23);
        grpQuery.Controls.Add(_cmbStatus);

        _btnSearch = new Button();
        _btnSearch.Text = "查询";
        _btnSearch.Location = new Point(580, 20);
        _btnSearch.Size = new Size(70, 25);
        grpQuery.Controls.Add(_btnSearch);

        _btnRefresh = new Button();
        _btnRefresh.Text = "刷新";
        _btnRefresh.Location = new Point(660, 20);
        _btnRefresh.Size = new Size(70, 25);
        grpQuery.Controls.Add(_btnRefresh);

        Controls.Add(grpQuery);

        // ===== DataGridView 骨架 =====
        _dgv = new DataGridView();
        _dgv.Dock = DockStyle.Fill;
        Controls.Add(_dgv);

        // ===== 底部按钮骨架 =====
        Panel bottomPanel = new Panel();
        bottomPanel.Dock = DockStyle.Bottom;
        bottomPanel.Height = 50;

        _btnReserve = new Button();
        _btnReserve.Text = "预约登记";
        _btnReserve.Location = new Point(10, 10);
        _btnReserve.Size = new Size(90, 30);
        bottomPanel.Controls.Add(_btnReserve);

        _btnNotify = new Button();
        _btnNotify.Text = "通知取书";
        _btnNotify.Location = new Point(110, 10);
        _btnNotify.Size = new Size(90, 30);
        bottomPanel.Controls.Add(_btnNotify);

        _btnCancel = new Button();
        _btnCancel.Text = "取消预约";
        _btnCancel.Location = new Point(210, 10);
        _btnCancel.Size = new Size(90, 30);
        bottomPanel.Controls.Add(_btnCancel);

        Controls.Add(bottomPanel);

        ResumeLayout(false);
    }
}