using System.Data;
using CampusStore.BLL;
using CampusStore.Common;

namespace CampusStore.Forms;

/// <summary>
/// 商品类别管理窗体
/// </summary>
/// <remarks>
/// 提供商品类别的增删改查功能，包括关键字查询、列表选择编辑和排序号管理。
/// </remarks>
public partial class FrmCategory : Form
{
    private readonly CategoryManager _categoryManager = new();

    /// <summary>当前选中的类别编号（0 表示未选中）</summary>
    private int _selectedCategoryID = 0;

    // === 查询区控件 ===
    private Label _lblQueryName;
    private TextBox _txtQuery;
    private Button _btnQuery;
    private Button _btnRefresh;

    // === 列表区控件 ===
    private DataGridView _dgvCategory;

    // === 编辑区控件 ===
    private Label _lblName;
    private TextBox _txtName;
    private Label _lblDesc;
    private TextBox _txtDesc;
    private Label _lblSort;
    private NumericUpDown _numSort;

    // === 操作按钮 ===
    private Button _btnAdd;
    private Button _btnUpdate;
    private Button _btnDelete;
    private Button _btnClear;

    /// <summary>
    /// 构造函数，初始化窗体并绑定事件
    /// </summary>
    public FrmCategory()
    {
        InitializeComponent();
        BindEvents();
    }

    /// <summary>
    /// 初始化所有控件并添加到窗体
    /// </summary>
    private void InitializeComponent()
    {
        UiHelper.SetupForm(this, "商品类别管理", 800, 520);

        // === 查询区（Y=15）===
        _lblQueryName = UiHelper.CreateLabel("类别名称：", 15, 15);
        _txtQuery = UiHelper.CreateTextBox(UiHelper.NextX(_lblQueryName), 12, 200);
        _btnQuery = UiHelper.CreatePrimaryButton("查询", _txtQuery.Location.X + _txtQuery.Width + 10, 10, 75, 30);
        _btnRefresh = UiHelper.CreateSecondaryButton("刷新", _btnQuery.Location.X + _btnQuery.Width + 10, 10, 75, 30);

        // === 列表区（Y=55，宽 760，高 280）===
        _dgvCategory = new DataGridView
        {
            Location = new Point(15, 55),
            Size = new Size(760, 280)
        };
        UiHelper.SetGridStyle(_dgvCategory);
        SetupGridColumns();

        // === 编辑区（Y=350）===
        _lblName = UiHelper.CreateLabel("类别名称：", 15, 350);
        _txtName = UiHelper.CreateTextBox(UiHelper.NextX(_lblName), 347, 200);
        _lblDesc = UiHelper.CreateLabel("类别描述：", _txtName.Location.X + _txtName.Width + 15, 350);
        _txtDesc = UiHelper.CreateTextBox(UiHelper.NextX(_lblDesc), 347, 300);

        // === 编辑区第二行（Y=385）===
        _lblSort = UiHelper.CreateLabel("排序号：", 15, 385);
        _numSort = new NumericUpDown
        {
            Location = new Point(UiHelper.NextX(_lblSort), 382),
            Width = 100,
            Minimum = 0,
            Maximum = 9999,
            Value = 0
        };

        // === 操作按钮区（Y=420，横排，间距 10）===
        _btnAdd = UiHelper.CreatePrimaryButton("添加", 15, 420, 80, 30);
        _btnUpdate = UiHelper.CreatePrimaryButton("修改", _btnAdd.Location.X + _btnAdd.Width + 10, 420, 80, 30);
        _btnDelete = UiHelper.CreateSecondaryButton("删除", _btnUpdate.Location.X + _btnUpdate.Width + 10, 420, 80, 30);
        _btnClear = UiHelper.CreateSecondaryButton("清空", _btnDelete.Location.X + _btnDelete.Width + 10, 420, 80, 30);

        // 添加所有控件到窗体
        Controls.AddRange(new Control[]
        {
            _lblQueryName, _txtQuery, _btnQuery, _btnRefresh,
            _dgvCategory,
            _lblName, _txtName, _lblDesc, _txtDesc, _lblSort, _numSort,
            _btnAdd, _btnUpdate, _btnDelete, _btnClear
        });
    }

    /// <summary>
    /// 配置 DataGridView 列（列标题中文化）
    /// </summary>
    private void SetupGridColumns()
    {
        _dgvCategory.AutoGenerateColumns = false;
        _dgvCategory.Columns.AddRange(new DataGridViewColumn[]
        {
            new DataGridViewTextBoxColumn { DataPropertyName = "categoryID", HeaderText = "类别编号", Name = "colCategoryID" },
            new DataGridViewTextBoxColumn { DataPropertyName = "categoryName", HeaderText = "类别名称", Name = "colCategoryName" },
            new DataGridViewTextBoxColumn { DataPropertyName = "categoryDesc", HeaderText = "类别描述", Name = "colCategoryDesc" },
            new DataGridViewTextBoxColumn
            {
                DataPropertyName = "addTime",
                HeaderText = "添加时间",
                Name = "colAddTime",
                DefaultCellStyle = new DataGridViewCellStyle { Format = "yyyy-MM-dd HH:mm" }
            },
            new DataGridViewTextBoxColumn { DataPropertyName = "sortOrder", HeaderText = "排序号", Name = "colSortOrder" },
            new DataGridViewTextBoxColumn { DataPropertyName = "productCount", HeaderText = "商品数量", Name = "colProductCount" }
        });
    }

    /// <summary>
    /// 绑定控件事件
    /// </summary>
    private void BindEvents()
    {
        Load += FrmCategory_Load;
        _btnQuery.Click += BtnQuery_Click;
        _btnRefresh.Click += BtnRefresh_Click;
        _dgvCategory.SelectionChanged += DgvCategory_SelectionChanged;
        _btnAdd.Click += BtnAdd_Click;
        _btnUpdate.Click += BtnUpdate_Click;
        _btnDelete.Click += BtnDelete_Click;
        _btnClear.Click += BtnClear_Click;
    }

    /// <summary>
    /// 窗体加载时绑定全部数据
    /// </summary>
    private void FrmCategory_Load(object sender, EventArgs e)
    {
        LoadData();
    }

    /// <summary>
    /// 加载全部类别数据到列表
    /// </summary>
    private void LoadData()
    {
        try
        {
            _dgvCategory.DataSource = _categoryManager.GetAll();
        }
        catch (Exception ex)
        {
            UiHelper.HandleException(ex, this);
        }
    }

    /// <summary>
    /// 按类别名称关键字查询
    /// </summary>
    private void BtnQuery_Click(object sender, EventArgs e)
    {
        try
        {
            string keyword = _txtQuery.Text.Trim();
            // 关键字为空时查询全部，避免无意义检索
            _dgvCategory.DataSource = string.IsNullOrEmpty(keyword)
                ? _categoryManager.GetAll()
                : _categoryManager.Search(keyword);
        }
        catch (Exception ex)
        {
            UiHelper.HandleException(ex, this);
        }
    }

    /// <summary>
    /// 刷新列表并清空查询条件
    /// </summary>
    private void BtnRefresh_Click(object sender, EventArgs e)
    {
        _txtQuery.Text = "";
        LoadData();
    }

    /// <summary>
    /// 选中行时将数据填入编辑区
    /// </summary>
    private void DgvCategory_SelectionChanged(object sender, EventArgs e)
    {
        // 列顺序：0=categoryID, 1=categoryName, 2=categoryDesc, 3=addTime, 4=sortOrder, 5=productCount
        if (_dgvCategory.SelectedRows.Count == 0)
        {
            _selectedCategoryID = 0;
            return;
        }
        DataGridViewRow row = _dgvCategory.SelectedRows[0];
        object idValue = row.Cells[0].Value;
        if (idValue == null || idValue == DBNull.Value)
        {
            _selectedCategoryID = 0;
            return;
        }
        _selectedCategoryID = Convert.ToInt32(idValue);
        _txtName.Text = row.Cells[1].Value?.ToString() ?? "";
        _txtDesc.Text = row.Cells[2].Value?.ToString() ?? "";
        object sortValue = row.Cells[4].Value;
        if (sortValue != null && sortValue != DBNull.Value)
            _numSort.Value = Convert.ToInt32(sortValue);
    }

    /// <summary>
    /// 添加类别
    /// </summary>
    private void BtnAdd_Click(object sender, EventArgs e)
    {
        try
        {
            _categoryManager.Add(_txtName.Text.Trim(), _txtDesc.Text.Trim(), (int)_numSort.Value);
            LoadData();
            ClearInputs();
            MessageBox.Show("添加成功", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            UiHelper.HandleException(ex, this);
        }
    }

    /// <summary>
    /// 修改类别（需先选中行）
    /// </summary>
    private void BtnUpdate_Click(object sender, EventArgs e)
    {
        try
        {
            if (_selectedCategoryID <= 0)
                throw new BusinessException("请先选择要修改的类别");
            _categoryManager.Update(_selectedCategoryID, _txtName.Text.Trim(), _txtDesc.Text.Trim(), (int)_numSort.Value);
            LoadData();
            ClearInputs();
            MessageBox.Show("修改成功", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            UiHelper.HandleException(ex, this);
        }
    }

    /// <summary>
    /// 删除类别（需先选中行并确认）
    /// </summary>
    private void BtnDelete_Click(object sender, EventArgs e)
    {
        try
        {
            if (_selectedCategoryID <= 0)
                throw new BusinessException("请先选择要删除的类别");
            if (!UiHelper.ConfirmDelete(_txtName.Text, this))
                return;
            _categoryManager.Delete(_selectedCategoryID);
            LoadData();
            ClearInputs();
            MessageBox.Show("删除成功", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            UiHelper.HandleException(ex, this);
        }
    }

    /// <summary>
    /// 清空编辑区
    /// </summary>
    private void BtnClear_Click(object sender, EventArgs e)
    {
        ClearInputs();
    }

    /// <summary>
    /// 清空所有输入控件并重置选中状态
    /// </summary>
    private void ClearInputs()
    {
        _txtName.Text = "";
        _txtDesc.Text = "";
        _numSort.Value = 0;
        _selectedCategoryID = 0;
        _dgvCategory.ClearSelection();
    }
}
