using LibrarySys.BLL;
using LibrarySys.Common;
using LibrarySys.Models;

namespace LibrarySys.Forms;

/// <summary>
/// 图书信息管理窗体
/// 固定坐标布局：查询区(顶部) → DataGridView(中间) → 编辑区(底部)
/// 极其保守的尺寸参数，DpiUnaware 模式下跨电脑零遮挡
/// </summary>
public class FrmBook : Form
{
    private readonly BookBiz _biz = new();
    private readonly BookTypeBiz _typeBiz = new();
    private readonly UserInfo _currentUser;
    private readonly bool _canEdit;

    private TextBox _txtBookId, _txtBookName, _txtAuthor, _txtPublisher, _txtISBN, _txtPrice, _txtCount;
    private ComboBox _cboType;
    private DateTimePicker _dtpPublishDate;
    private TextBox _txtQueryName, _txtQueryPub;
    private ComboBox _cboQueryType;
    private DataGridView _dgv;
    private bool _isLoading;

    public FrmBook()
    {
        _canEdit = true;
        InitializeUI();
        LoadTypeCombo();
        Load += (_, _) => LoadData();
    }

    public FrmBook(UserInfo currentUser)
    {
        _currentUser = currentUser;
        _canEdit = currentUser.UserPurview == BusinessConstants.ROLE_ADMIN;
        InitializeUI();
        LoadTypeCombo();
        Load += (_, _) => LoadData();
    }

    private void InitializeUI()
    {
        DoubleBuffered = true;
        Text = "图书信息管理";
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(1150, 700);
        MinimumSize = new Size(1100, 680);
        Font = UiHelper.DefaultFont;

        _txtBookId = UiHelper.CreateTextBox();
        _txtBookName = UiHelper.CreateTextBox();
        _cboType = UiHelper.CreateComboBox();
        _txtAuthor = UiHelper.CreateTextBox();
        _txtPublisher = UiHelper.CreateTextBox();
        _dtpPublishDate = UiHelper.CreateDateTimePicker();
        _txtISBN = UiHelper.CreateTextBox(180);
        _txtPrice = UiHelper.CreateTextBox(100);
        _txtCount = UiHelper.CreateTextBox(100);

        _txtQueryName = UiHelper.CreateTextBox();
        _cboQueryType = UiHelper.CreateComboBox();
        _txtQueryPub = UiHelper.CreateTextBox();

        // ===== 顶部查询区 =====
        // GroupBox 标题栏约 20px，内容区高度 = Height - 20
        // 按钮 y=72+32=104，需 Height ≥ 104+20=124
        GroupBox grpQuery = new()
        {
            Text = "查询条件",
            Dock = DockStyle.Top,
            Height = 125
        };
        UiHelper.LayoutFields(grpQuery.Controls, new List<UiHelper.FieldDef>
        {
            new("书籍名称：", _txtQueryName),
            new("类型：", _cboQueryType),
            new("出版社：", _txtQueryPub),
        }, pairsPerRow: 3, startX: 20, startY: 30);

        Button btnQuery = UiHelper.CreateButton("查询");
        Button btnShowAll = UiHelper.CreateButton("全部");
        btnQuery.Click += (_, _) => DoQuery();
        btnShowAll.Click += (_, _) => DoShowAll();
        UiHelper.LayoutButtons(grpQuery.Controls, new[] { btnQuery, btnShowAll }, 20, 72);

        // ===== DataGridView（手动定位，不依赖 Dock，避免嵌入窗体时布局错乱） =====
        _dgv = new DataGridView();
        UiHelper.StyleDataGridView(_dgv);
        _dgv.SelectionChanged += (_, _) => BindEditForm();

        // ===== 底部编辑区 =====
        // GroupBox 标题栏约 20px，内容区高度 = Height - 20
        // 按钮 y=140+32=172，需 Height ≥ 172+20=192
        GroupBox grpEdit = new()
        {
            Text = "图书信息",
            Dock = DockStyle.Bottom,
            Height = 195
        };
        UiHelper.LayoutFields(grpEdit.Controls, new List<UiHelper.FieldDef>
        {
            new("书籍编号：", _txtBookId),
            new("书籍名称：", _txtBookName),
            new("类型：", _cboType),
            new("作者：", _txtAuthor),
            new("出版社：", _txtPublisher),
            new("出版日期：", _dtpPublishDate),
            new("ISBN：", _txtISBN),
            new("价格：", _txtPrice),
            new("馆藏数量：", _txtCount),
        }, pairsPerRow: 3, startX: 20, startY: 30);

        Button btnAdd = UiHelper.CreateButton("新增"); btnAdd.Enabled = _canEdit;
        Button btnUpdate = UiHelper.CreateButton("修改"); btnUpdate.Enabled = _canEdit;
        Button btnDelete = UiHelper.CreateButton("删除"); btnDelete.Enabled = _canEdit;
        Button btnReset = UiHelper.CreateButton("重置");
        Button btnReturn = UiHelper.CreateButton("返回");
        btnAdd.Click += (_, _) => DoAdd();
        btnUpdate.Click += (_, _) => DoUpdate();
        btnDelete.Click += (_, _) => DoDelete();
        btnReset.Click += (_, _) => DoClear();
        btnReturn.Click += (_, _) => Close();
        UiHelper.LayoutButtonsWithReturn(grpEdit.Controls, new[] { btnAdd, btnUpdate, btnDelete, btnReset }, btnReturn, 20, 140, 1150);

        Controls.Add(grpEdit);
        Controls.Add(grpQuery);
        Controls.Add(_dgv);

        // 手动控制 DataGridView 位置，确保在 grpQuery 和 grpEdit 之间
        // 嵌入窗体时 WinForms Dock 布局可能异常，手动定位更可靠
        Layout += (_, _) =>
        {
            _dgv.Location = new Point(0, grpQuery.Bottom);
            _dgv.Size = new Size(ClientSize.Width, grpEdit.Top - grpQuery.Bottom);
        };
    }

    private void LoadTypeCombo()
    {
        List<BookTypeInfo> types = _typeBiz.GetAll();
        _cboType.DataSource = new List<BookTypeInfo>(types);
        _cboType.DisplayMember = "TypeName";
        _cboType.ValueMember = "TypeID";

        List<BookTypeInfo> queryTypes = new() { new BookTypeInfo { TypeID = "", TypeName = "全部" } };
        queryTypes.AddRange(types);
        _cboQueryType.DataSource = queryTypes;
        _cboQueryType.DisplayMember = "TypeName";
        _cboQueryType.ValueMember = "TypeID";
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
        _dgv.Columns["BookID"].HeaderText = "书籍编号";
        _dgv.Columns["BookName"].HeaderText = "书籍名称";
        _dgv.Columns["Author"].HeaderText = "作者";
        _dgv.Columns["Publisher"].HeaderText = "出版社";
        _dgv.Columns["PublishDate"].HeaderText = "出版日期";
        _dgv.Columns["ISBN"].HeaderText = "ISBN";
        _dgv.Columns["Price"].HeaderText = "价格";
        _dgv.Columns["TypeName"].HeaderText = "类型";
        _dgv.Columns["TotalCount"].HeaderText = "馆藏数量";
        _dgv.Columns["AvailableCount"].HeaderText = "可借数量";
        _dgv.Columns["TypeID"].Visible = false;
    }

    private void DoQuery()
    {
        string typeId = _cboQueryType.SelectedValue?.ToString();
        _dgv.DataSource = _biz.Search(_txtQueryName.Text.Trim(), typeId, _txtQueryPub.Text.Trim());
        SetChineseHeaders();
    }

    private void DoShowAll()
    {
        _txtQueryName.Clear();
        _txtQueryPub.Clear();
        LoadData();
    }

    private void BindEditForm()
    {
        if (_isLoading) return;
        if (_dgv.CurrentRow?.DataBoundItem is not BookInfo item) return;
        _txtBookId.Text = item.BookID;
        _txtBookName.Text = item.BookName;
        _cboType.SelectedValue = item.TypeID;
        _txtAuthor.Text = item.Author;
        _txtPublisher.Text = item.Publisher;
        _dtpPublishDate.Value = item.PublishDate ?? DateTime.Today;
        _txtISBN.Text = item.ISBN;
        _txtPrice.Text = item.Price?.ToString() ?? "";
        _txtCount.Text = item.TotalCount.ToString();
        _txtBookId.ReadOnly = true;
    }

    private BookInfo BuildEntity()
    {
        return new BookInfo
        {
            BookID = _txtBookId.Text.Trim(),
            BookName = _txtBookName.Text.Trim(),
            TypeID = _cboType.SelectedValue?.ToString(),
            Author = _txtAuthor.Text.Trim(),
            Publisher = _txtPublisher.Text.Trim(),
            PublishDate = _dtpPublishDate.Value.Date,
            ISBN = _txtISBN.Text.Trim(),
            Price = decimal.TryParse(_txtPrice.Text.Trim(), out decimal p) ? p : 0,
            TotalCount = int.TryParse(_txtCount.Text.Trim(), out int c) ? c : 0
        };
    }

    private void DoAdd()
    {
        try
        {
            _biz.Add(BuildEntity());
            UiHelper.Info("新增成功");
            LoadData();
            DoClear();
        }
        catch (BusinessException ex) { UiHelper.Error(ex.Message); }
        catch (Exception ex) { UiHelper.Error($"操作失败：{ex.Message}"); }
    }

    private void DoUpdate()
    {
        try
        {
            _biz.Update(BuildEntity());
            UiHelper.Info("修改成功");
            LoadData();
        }
        catch (BusinessException ex) { UiHelper.Error(ex.Message); }
        catch (Exception ex) { UiHelper.Error($"操作失败：{ex.Message}"); }
    }

    private void DoDelete()
    {
        if (_dgv.CurrentRow?.DataBoundItem is not BookInfo item) return;
        if (!UiHelper.Confirm($"确认删除图书【{item.BookName}】吗？")) return;
        try
        {
            _biz.Delete(item.BookID);
            UiHelper.Info("删除成功");
            LoadData();
            DoClear();
        }
        catch (BusinessException ex) { UiHelper.Error(ex.Message); }
        catch (Exception ex) { UiHelper.Error($"操作失败：{ex.Message}"); }
    }

    private void DoClear()
    {
        _txtBookId.Clear();
        _txtBookName.Clear();
        _txtAuthor.Clear();
        _txtPublisher.Clear();
        _txtISBN.Clear();
        _txtPrice.Clear();
        _txtCount.Clear();
        _dtpPublishDate.Value = DateTime.Today;
        _txtBookId.ReadOnly = false;
        _dgv.ClearSelection();
    }
}
