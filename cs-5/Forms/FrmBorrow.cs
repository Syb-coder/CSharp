using LibrarySys.BLL;
using LibrarySys.Common;
using LibrarySys.Models;
using System.ComponentModel;

namespace LibrarySys.Forms;

/// <summary>
/// 借阅管理窗体（借书、还书、记录查询）
/// 固定坐标布局：查询区(顶部) → DataGridView(中间) → 操作区(底部)
/// </summary>
public partial class FrmBorrow : Form
{
    private readonly BorrowBiz _biz = new();
    private readonly UserInfo _currentUser;
    private readonly bool _canEdit;

    private DataGridView _dgv;
    private bool _isLoading;

    private TextBox _txtQueryReader, _txtQueryBook;
    private ComboBox _cboQueryStatus;

    private TextBox _txtReaderId, _txtBookId, _txtDueDate;
    private DateTimePicker _dtpBorrowDate;
    private Button _btnBorrow, _btnReturn;

    public FrmBorrow()
    {
        _canEdit = true;
        InitializeComponent();
        BuildUI();
        InitializeEvents();
        Load += (_, _) => LoadData();
    }

    public FrmBorrow(UserInfo currentUser)
    {
        _currentUser = currentUser;
        // 借书还书是所有用户的基础功能，不限制权限
        _canEdit = true;
        InitializeComponent();
        BuildUI();
        InitializeEvents();
        Load += (_, _) => LoadData();
    }

    private void BuildUI()
    {
        // 设计器模式下跳过：设计器已在 InitializeComponent 中创建控件骨架
        if (LicenseManager.UsageMode == LicenseUsageMode.Designtime) return;
        // 运行时：清除 InitializeComponent 创建的骨架控件，重新完整构建
        Controls.Clear();

        DoubleBuffered = true;
        Text = "借阅管理";
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(1150, 700);
        MinimumSize = new Size(1100, 680);
        Font = UiHelper.DefaultFont;

        // 创建控件
        _txtReaderId = UiHelper.CreateTextBox();
        _txtBookId = UiHelper.CreateTextBox();
        _dtpBorrowDate = UiHelper.CreateDateTimePicker();
        _dtpBorrowDate.Value = DateTime.Today;
        _txtDueDate = UiHelper.CreateTextBox();
        _txtDueDate.ReadOnly = true;
        _txtDueDate.BackColor = SystemColors.Control;

        _txtQueryReader = UiHelper.CreateTextBox();
        _txtQueryBook = UiHelper.CreateTextBox();
        _cboQueryStatus = UiHelper.CreateComboBox();
        _cboQueryStatus.Items.AddRange(new object[] { "", "借出", "已还" });
        _cboQueryStatus.SelectedIndex = 0;

        // ===== 顶部查询区（固定坐标） =====
        GroupBox grpQuery = new GroupBox()
        {
            Text = "查询条件",
            Location = new Point(0, 0),
            Size = new Size(1150, 125),
            Dock = DockStyle.Top
        };

        UiHelper.LayoutFields(grpQuery.Controls, new List<UiHelper.FieldDef>
        {
            new("读者编号：", _txtQueryReader),
            new("图书编号：", _txtQueryBook),
            new("借阅状态：", _cboQueryStatus),
        }, pairsPerRow: 3, startX: 20, startY: 30);

        Button btnQuery = UiHelper.CreateButton("查询");
        Button btnShowAll = UiHelper.CreateButton("全部");
        btnQuery.Click += OnQueryClick;
        btnShowAll.Click += OnShowAllClick;
        UiHelper.LayoutButtons(grpQuery.Controls, new[] { btnQuery, btnShowAll }, 20, 72);

        // ===== DataGridView（中间填充） =====
        _dgv = new DataGridView { Dock = DockStyle.Fill };
        UiHelper.StyleDataGridView(_dgv);

        // ===== 底部操作区（固定坐标） =====
        GroupBox grpEdit = new GroupBox()
        {
            Text = "借书操作",
            Dock = DockStyle.Bottom,
            Height = 195
        };

        UiHelper.LayoutFields(grpEdit.Controls, new List<UiHelper.FieldDef>
        {
            new("读者编号：", _txtReaderId),
            new("图书编号：", _txtBookId),
            new("借书日期：", _dtpBorrowDate),
            new("应还日期：", _txtDueDate),
        }, pairsPerRow: 3, startX: 20, startY: 30);

        _btnBorrow = UiHelper.CreateButton("借书");
        _btnBorrow.Enabled = _canEdit;
        _btnBorrow.Click += OnBorrowClick;
        _btnReturn = UiHelper.CreateButton("还书");
        // 默认禁用，选中借出记录后由 SelectionChanged 启用
        _btnReturn.Enabled = false;
        _btnReturn.Click += OnReturnClick;
        Button btnBack = UiHelper.CreateButton("返回");
        btnBack.Click += OnBackClick;
        UiHelper.LayoutButtonsWithReturn(
            grpEdit.Controls,
            new[] { _btnBorrow, _btnReturn },
            btnBack, 20, 140, 1150);

        // 添加顺序：先底部，再顶部，最后 Fill
        Controls.Add(grpEdit);
        Controls.Add(grpQuery);
        Controls.Add(_dgv);
    }

    /// <summary>
    /// 注册事件处理（在 InitializeComponent 之后调用，避免设计器解析 lambda 表达式失败）
    /// </summary>
    private void InitializeEvents()
    {
        UpdateDueDate();
        _dtpBorrowDate.ValueChanged += OnBorrowDateChanged;
        _dgv.SelectionChanged += OnDgvSelectionChanged;
    }

    private void OnBorrowDateChanged(object? sender, EventArgs e) => UpdateDueDate();
    private void OnQueryClick(object? sender, EventArgs e) => DoQuery();
    private void OnShowAllClick(object? sender, EventArgs e) => DoShowAll();
    private void OnBorrowClick(object? sender, EventArgs e) => DoBorrow();
    private void OnReturnClick(object? sender, EventArgs e) => DoReturnBook();
    private void OnBackClick(object? sender, EventArgs e) => Close();

    private void OnDgvSelectionChanged(object? sender, EventArgs e)
    {
        if (_isLoading) return;
        if (_dgv.CurrentRow?.DataBoundItem is BorrowInfo item)
            _btnReturn.Enabled = item.Status == BusinessConstants.STATUS_BORROWED;
        else
            _btnReturn.Enabled = false;
    }

    /// <summary>
    /// 应还日期 = 借书日期 + 默认借阅期限（60天），借书日期变更时自动重算
    /// </summary>
    private void UpdateDueDate()
    {
        _txtDueDate.Text = _dtpBorrowDate.Value.AddDays(BusinessConstants.DEFAULT_BORROW_DAYS).ToString("yyyy-MM-dd");
    }

    private void LoadData()
    {
        _isLoading = true;
        // 先置空 DataSource 强制清空，再绑定新数据，确保状态列等数据完整刷新
        _dgv.DataSource = null;
        _dgv.DataSource = _biz.GetAll();
        SetChineseHeaders();
        _isLoading = false;
    }

    private void SetChineseHeaders()
    {
        if (_dgv.Columns.Count == 0) return;
        _dgv.Columns["BorrowID"].HeaderText = "借阅编号";
        _dgv.Columns["ReaderID"].HeaderText = "读者编号";
        _dgv.Columns["ReaderName"].HeaderText = "读者姓名";
        _dgv.Columns["BookID"].HeaderText = "图书编号";
        _dgv.Columns["BookName"].HeaderText = "图书名称";
        _dgv.Columns["BorrowDate"].HeaderText = "借书日期";
        _dgv.Columns["DueDate"].HeaderText = "应还日期";
        _dgv.Columns["ReturnDate"].HeaderText = "归还日期";
        _dgv.Columns["Status"].HeaderText = "状态";
    }

    private void DoQuery()
    {
        string status = _cboQueryStatus.SelectedItem?.ToString();
        _dgv.DataSource = _biz.Search(
            _txtQueryReader.Text.Trim(),
            _txtQueryBook.Text.Trim(),
            string.IsNullOrEmpty(status) ? null : status,
            null, null);
        SetChineseHeaders();
    }

    private void DoShowAll()
    {
        _txtQueryReader.Clear();
        _txtQueryBook.Clear();
        _cboQueryStatus.SelectedIndex = 0;
        LoadData();
    }

    private void DoBorrow()
    {
        string readerId = _txtReaderId.Text.Trim();
        string bookId = _txtBookId.Text.Trim();
        if (string.IsNullOrEmpty(readerId) || string.IsNullOrEmpty(bookId))
        {
            UiHelper.Error("请输入读者编号和图书编号");
            return;
        }
        try
        {
            _biz.Borrow(readerId, bookId, _dtpBorrowDate.Value.Date);
            UiHelper.Info("借书成功");
            LoadData();
            ClearBorrowForm();
        }
        catch (BusinessException ex) { UiHelper.Error(ex.Message); }
        catch (Exception ex) { UiHelper.Error($"操作失败：{ex.Message}"); }
    }

    private void DoReturnBook()
    {
        if (_dgv.CurrentRow?.DataBoundItem is not BorrowInfo item)
        {
            UiHelper.Error("请先在列表中选中一条借出记录");
            return;
        }
        if (item.Status != BusinessConstants.STATUS_BORROWED)
        {
            UiHelper.Error("该记录已归还");
            return;
        }
        if (!UiHelper.Confirm($"确认归还图书【{item.BookName}】吗？")) return;
        try
        {
            _biz.Return(item.BorrowID);
            UiHelper.Info("还书成功");
            LoadData();
        }
        catch (BusinessException ex) { UiHelper.Error(ex.Message); }
        catch (Exception ex) { UiHelper.Error($"操作失败：{ex.Message}"); }
    }

    private void ClearBorrowForm()
    {
        _txtReaderId.Clear();
        _txtBookId.Clear();
        _dtpBorrowDate.Value = DateTime.Today;
        _txtReaderId.Focus();
    }
}
