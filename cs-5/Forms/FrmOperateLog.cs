using LibrarySys.BLL;
using LibrarySys.Common;
using LibrarySys.Models;
using System.ComponentModel;

namespace LibrarySys.Forms;

/// <summary>
/// 操作日志查看窗体：支持时间范围、操作类型、用户名筛选
/// </summary>
public partial class FrmOperateLog : Form
{
    private readonly UserInfo _currentUser;
    private readonly LogBiz _biz = new();
    private DataGridView _dgv;
    private DateTimePicker _dtpFrom;
    private DateTimePicker _dtpTo;
    private ComboBox _cmbType;
    private TextBox _txtUserName;
    private Button _btnSearch;
    private Button _btnClear;

    /// <summary>无参构造，仅供 VS 设计器使用</summary>
    public FrmOperateLog()
    {
        InitializeComponent();
        BuildUI();
    }

    public FrmOperateLog(UserInfo currentUser)
    {
        _currentUser = currentUser;
        Text = "操作日志";
        InitializeComponent();
        BuildUI();
        Load += (_, _) => LoadData();
    }

    private void BuildUI()
    {
        // 设计器模式下跳过：设计器已在 InitializeComponent 中创建控件骨架
        if (LicenseManager.UsageMode == LicenseUsageMode.Designtime) return;
        // 运行时：清除 InitializeComponent 创建的骨架控件，重新完整构建
        Controls.Clear();

        DoubleBuffered = true;
        BackColor = Color.FromArgb(240, 242, 245);
        Font = UiHelper.DefaultFont;

        // ===== 查询区域 =====
        GroupBox gbQuery = new GroupBox
        {
            Text = "查询条件",
            Dock = DockStyle.Top,
            Height = 70,
            Font = new Font("Microsoft YaHei UI", 10F),
            Padding = new Padding(10, 20, 10, 5)
        };

        Label lblFrom = new Label { Text = "起始时间：", Location = new Point(15, 25), AutoSize = true };
        _dtpFrom = new DateTimePicker
        {
            Location = new Point(85, 22),
            Width = 130,
            Format = DateTimePickerFormat.Short,
            Value = DateTime.Today.AddDays(-7),
            Anchor = AnchorStyles.Left
        };

        Label lblTo = new Label { Text = "截止时间：", Location = new Point(225, 25), AutoSize = true };
        _dtpTo = new DateTimePicker
        {
            Location = new Point(295, 22),
            Width = 130,
            Format = DateTimePickerFormat.Short,
            Value = DateTime.Today.AddDays(1),
            Anchor = AnchorStyles.Left
        };

        Label lblType = new Label { Text = "操作类型：", Location = new Point(435, 25), AutoSize = true };
        _cmbType = new ComboBox
        {
            Location = new Point(505, 22),
            Width = 100,
            DropDownStyle = ComboBoxStyle.DropDownList,
            Anchor = AnchorStyles.Left
        };
        _cmbType.Items.AddRange(new[] { "全部", "登录", "新增", "修改", "删除", "借书", "还书", "预约", "取消预约", "缴费" });
        _cmbType.SelectedIndex = 0;

        Label lblUser = new Label { Text = "用户名：", Location = new Point(620, 25), AutoSize = true };
        _txtUserName = new TextBox { Location = new Point(680, 22), Width = 100, Anchor = AnchorStyles.Left };

        _btnSearch = new Button { Text = "查询", Location = new Point(795, 20), Width = 70 };
        _btnClear = new Button { Text = "清空", Location = new Point(875, 20), Width = 70 };

        _btnSearch.Click += (_, _) => LoadData();
        _btnClear.Click += (_, _) =>
        {
            _dtpFrom.Value = DateTime.Today.AddDays(-7);
            _dtpTo.Value = DateTime.Today.AddDays(1);
            _cmbType.SelectedIndex = 0;
            _txtUserName.Clear();
            LoadData();
        };

        gbQuery.Controls.AddRange(new Control[] { lblFrom, _dtpFrom, lblTo, _dtpTo, lblType, _cmbType, lblUser, _txtUserName, _btnSearch, _btnClear });

        // ===== DataGridView =====
        _dgv = new DataGridView { Dock = DockStyle.Fill };
        UiHelper.StyleDataGridView(_dgv);
        _dgv.AutoGenerateColumns = false;
        _dgv.Columns.AddRange(new DataGridViewColumn[]
        {
            new DataGridViewTextBoxColumn { HeaderText = "日志ID", Name = "LogID", Width = 60 },
            new DataGridViewTextBoxColumn { HeaderText = "操作者", Name = "UserName", Width = 80 },
            new DataGridViewTextBoxColumn { HeaderText = "操作时间", Name = "OperateTime", Width = 140 },
            new DataGridViewTextBoxColumn { HeaderText = "操作类型", Name = "OperateType", Width = 80 },
            new DataGridViewTextBoxColumn { HeaderText = "操作对象", Name = "OperateContent", Width = 150 },
            new DataGridViewTextBoxColumn { HeaderText = "详细说明", Name = "Detail", Width = 200 }
        });
        _dgv.EnableHeadersVisualStyles = false;
        _dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(240, 242, 245);
        _dgv.ColumnHeadersDefaultCellStyle.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold);
        _dgv.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 249, 250);

        Controls.Add(gbQuery);
        Controls.Add(_dgv);
    }

    /// <summary>加载日志数据</summary>
    private void LoadData()
    {
        try
        {
            string? operateType = _cmbType.SelectedIndex > 0 ? _cmbType.SelectedItem!.ToString() : null;
            string? userName = string.IsNullOrWhiteSpace(_txtUserName.Text) ? null : _txtUserName.Text.Trim();

            var list = _biz.Search(_dtpFrom.Value, _dtpTo.Value.AddDays(1), operateType, userName);
            _dgv.Rows.Clear();
            foreach (var item in list)
            {
                _dgv.Rows.Add(
                    item.LogID,
                    item.UserName,
                    item.OperateTime.ToString("yyyy-MM-dd HH:mm:ss"),
                    item.OperateType,
                    item.OperateContent,
                    item.Detail ?? ""
                );
            }

            // 更新标题显示条数
            Text = $"操作日志（共 {list.Count} 条）";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"加载日志失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}