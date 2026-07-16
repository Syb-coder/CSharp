using LibrarySys.BLL;
using LibrarySys.Common;
using LibrarySys.Models;

namespace LibrarySys.Forms;

/// <summary>
/// 罚款管理窗体（查询罚款记录、登记缴费）
/// 固定坐标布局：查询区(顶部) → DataGridView(中间) → 操作区(底部，仅按钮)
/// </summary>
public class FrmFine : Form
{
    private readonly FineBiz _biz = new();
    private readonly UserInfo _currentUser;
    private readonly bool _canEdit;

    private TextBox _txtQueryReader;
    private ComboBox _cboQueryStatus;
    private Button _btnPay;
    private DataGridView _dgv;
    private bool _isLoading;

    public FrmFine()
    {
        _canEdit = true;
        InitializeUI();
        Load += (_, _) => LoadData();
    }

    public FrmFine(UserInfo currentUser)
    {
        _currentUser = currentUser;
        _canEdit = currentUser.UserPurview == BusinessConstants.ROLE_ADMIN;
        InitializeUI();
        Load += (_, _) => LoadData();
    }

    private void InitializeUI()
    {
        DoubleBuffered = true;
        Text = "罚款管理";
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(1150, 700);
        MinimumSize = new Size(1100, 680);
        Font = UiHelper.DefaultFont;

        _txtQueryReader = UiHelper.CreateTextBox();
        _cboQueryStatus = UiHelper.CreateComboBox();
        // 首项为空字符串表示不按状态过滤
        _cboQueryStatus.Items.AddRange(new object[] { "", BusinessConstants.FINE_UNPAID, BusinessConstants.FINE_PAID });

        // ===== 顶部查询区（固定坐标） =====
        GroupBox grpQuery = new()
        {
            Text = "查询条件",
            Location = new Point(0, 0),
            Size = new Size(1150, 125),
            Dock = DockStyle.Top
        };

        UiHelper.LayoutFields(grpQuery.Controls, new List<UiHelper.FieldDef>
        {
            new("读者编号：", _txtQueryReader),
            new("缴费状态：", _cboQueryStatus),
        }, pairsPerRow: 2, startX: 20, startY: 30);

        Button btnQuery = UiHelper.CreateButton("查询");
        Button btnShowAll = UiHelper.CreateButton("全部");
        btnQuery.Click += (_, _) => DoQuery();
        btnShowAll.Click += (_, _) => DoShowAll();
        UiHelper.LayoutButtons(grpQuery.Controls, new[] { btnQuery, btnShowAll }, 20, 72);

        // ===== DataGridView（中间填充） =====
        _dgv = new DataGridView { Dock = DockStyle.Fill };
        UiHelper.StyleDataGridView(_dgv);
        _dgv.SelectionChanged += (_, _) => SelectionChanged();

        // ===== 底部操作区（仅按钮，无表单） =====
        GroupBox grpAction = new()
        {
            Text = "操作",
            Dock = DockStyle.Bottom,
            Height = 90
        };

        _btnPay = UiHelper.CreateButton("缴费", 90);
        _btnPay.Enabled = _canEdit;
        _btnPay.Click += (_, _) => DoPay();

        Button btnReturn = UiHelper.CreateButton("返回");
        btnReturn.Click += (_, _) => Close();
        UiHelper.LayoutButtonsWithReturn(
            grpAction.Controls,
            new[] { _btnPay },
            btnReturn, 20, 30, 1150);

        // 添加顺序：先底部，再顶部，最后 Fill
        Controls.Add(grpAction);
        Controls.Add(grpQuery);
        Controls.Add(_dgv);
    }

    /// <summary>
    /// 选中行变化时控制缴费按钮可用性：
    /// 非管理员禁用；已缴罚款禁用；未缴可用
    /// </summary>
    private void SelectionChanged()
    {
        if (_isLoading) return;
        if (!_canEdit)
        {
            _btnPay.Enabled = false;
            return;
        }
        if (_dgv.CurrentRow?.DataBoundItem is FineInfo item)
        {
            _btnPay.Enabled = item.FineStatus != BusinessConstants.FINE_PAID;
        }
        else
        {
            _btnPay.Enabled = false;
        }
    }

    private void LoadData()
    {
        _isLoading = true;
        _dgv.DataSource = _biz.GetAll();
        SetChineseHeaders();
        _isLoading = false;
    }

    private void SetChineseHeaders()
    {
        if (_dgv.Columns.Count == 0) return;
        _dgv.Columns["FineID"].HeaderText = "罚款编号";
        _dgv.Columns["ReaderID"].HeaderText = "读者编号";
        _dgv.Columns["ReaderName"].HeaderText = "读者姓名";
        _dgv.Columns["BookID"].HeaderText = "书籍编号";
        _dgv.Columns["BookName"].HeaderText = "书籍名称";
        _dgv.Columns["BorrowID"].HeaderText = "借阅编号";
        _dgv.Columns["OverdueDays"].HeaderText = "逾期天数";
        _dgv.Columns["FineAmount"].HeaderText = "罚款金额（元）";
        _dgv.Columns["FineStatus"].HeaderText = "缴费状态";
        _dgv.Columns["CreateDate"].HeaderText = "生成日期";
        _dgv.Columns["PayDate"].HeaderText = "缴费日期";
    }

    private void DoShowAll()
    {
        _txtQueryReader.Clear();
        if (_cboQueryStatus.Items.Count > 0) _cboQueryStatus.SelectedIndex = 0;
        LoadData();
    }

    private void DoQuery()
    {
        string status = _cboQueryStatus.SelectedItem?.ToString();
        _dgv.DataSource = _biz.Search(_txtQueryReader.Text.Trim(), string.IsNullOrEmpty(status) ? null : status);
        SetChineseHeaders();
    }

    private void DoPay()
    {
        if (_dgv.CurrentRow?.DataBoundItem is not FineInfo item)
        {
            UiHelper.Error("请先在列表中选中一条罚款记录");
            return;
        }
        if (item.FineStatus == BusinessConstants.FINE_PAID)
        {
            UiHelper.Error("该罚款已缴清");
            return;
        }
        if (!UiHelper.Confirm($"确认登记缴费吗？\n读者：{item.ReaderName}\n图书：{item.BookName}\n罚款金额：{item.FineAmount:F2} 元")) return;
        try
        {
            _biz.Pay(item.FineID);
            UiHelper.Info("缴费登记成功");
            LoadData();
        }
        catch (BusinessException ex) { UiHelper.Error(ex.Message); }
        catch (Exception ex) { UiHelper.Error($"操作失败：{ex.Message}"); }
    }
}
