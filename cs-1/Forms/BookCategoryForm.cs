using LibraryManagement.BLL;
using LibraryManagement.Models;

namespace LibraryManagement.Forms;

/// <summary>
/// 图书类别管理窗体：类别的增删改查
/// </summary>
public class BookCategoryForm : Form
{
    private readonly BookCategoryService _categoryService = new();

    private readonly DataGridView _dgvCategories;
    private readonly TextBox _txtSearchName;
    private readonly TextBox _txtCategoryID;
    private readonly TextBox _txtCategoryName;
    private readonly TextBox _txtBorrowDays;
    private readonly TextBox _txtFinePerDay;
    private readonly Button _btnSearch;
    private readonly Button _btnAdd;
    private readonly Button _btnUpdate;
    private readonly Button _btnDelete;
    private readonly Button _btnClear;
    private readonly Button _btnBack;
    private readonly Panel _grpInput;

    /// <summary>
    /// 构造图书类别管理窗体
    /// </summary>
    public BookCategoryForm()
    {
        Text = "图书类别管理";
        Size = new Size(700, 600);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        BackColor = Color.FromArgb(245, 247, 250);

        // 查询区域：Label 宽度=100（"类别名称："实际需要100px），TextBox X=Label右边缘+5
        // AutoSize=false + 固定Size：禁用自动尺寸，防止Label宽度不足导致文字被右侧控件遮挡（参见cs-0经验文档）
        Label lblSearch = new()
        {
            Text = "类别名称：",
            Font = new Font("Microsoft YaHei UI", 9F),
            Location = new Point(15, 15),
            AutoSize = false,
            Size = new Size(100, 20),
            TextAlign = ContentAlignment.MiddleLeft,
            BackColor = Color.Transparent
        };
        _txtSearchName = new TextBox
        {
            Font = new Font("Microsoft YaHei UI", 9F),
            Location = new Point(120, 12),
            Size = new Size(170, 25)
        };
        _btnSearch = CreateButton("查询", 300, 10);
        _btnSearch.Click += (s, e) => LoadData();

        // DataGridView 列表
        _dgvCategories = new DataGridView
        {
            Location = new Point(15, 45),
            Size = new Size(650, 250),
            // 禁止用户直接在网格中增删行，所有数据操作必须通过下方输入区按钮完成
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            ReadOnly = true,
            // 整行选中模式，配合 SelectionChanged 事件实现点击行即回填输入框
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            BackgroundColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            RowHeadersVisible = false
        };
        _dgvCategories.SelectionChanged += DgvCategories_SelectionChanged;

        // 输入区域（Panel 无圆角边框遮盖，子控件精确对齐）
        // 使用 Panel 替代 GroupBox：GroupBox 的圆角边框会遮盖子控件边缘，Panel 的直角边框更利于精确布局
        _grpInput = new Panel
        {
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = Color.FromArgb(245, 247, 250),
            Location = new Point(15, 305),
            Size = new Size(650, 165)
        };
        // 标题 Label 替代 GroupBox 的 Text 属性
        Label lblGrpTitle = new()
        {
            Text = "类别信息",
            Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold),
            ForeColor = Color.FromArgb(64, 158, 255),
            Location = new Point(10, 5),
            AutoSize = true,
            BackColor = Color.Transparent
        };

        // 第一行：类别编号 + 类别名称
        Label lblID = new() { Text = "类别编号：", Location = new Point(15, 30), AutoSize = false, Size = new Size(100, 20), TextAlign = ContentAlignment.MiddleLeft, Font = new Font("Microsoft YaHei UI", 9F), BackColor = Color.Transparent };
        _txtCategoryID = new TextBox { Location = new Point(120, 27), Size = new Size(140, 25), Font = new Font("Microsoft YaHei UI", 9F) };

        Label lblName = new() { Text = "类别名称：", Location = new Point(275, 30), AutoSize = false, Size = new Size(100, 20), TextAlign = ContentAlignment.MiddleLeft, Font = new Font("Microsoft YaHei UI", 9F), BackColor = Color.Transparent };
        _txtCategoryName = new TextBox { Location = new Point(380, 27), Size = new Size(140, 25), Font = new Font("Microsoft YaHei UI", 9F) };

        // 第二行：可借阅天数 + 单日罚款标准
        Label lblDays = new() { Text = "可借天数：", Location = new Point(15, 65), AutoSize = false, Size = new Size(100, 20), TextAlign = ContentAlignment.MiddleLeft, Font = new Font("Microsoft YaHei UI", 9F), BackColor = Color.Transparent };
        _txtBorrowDays = new TextBox { Location = new Point(120, 62), Size = new Size(140, 25), Font = new Font("Microsoft YaHei UI", 9F) };

        Label lblFine = new() { Text = "罚款标准(元/天)：", Location = new Point(275, 65), AutoSize = false, Size = new Size(130, 20), TextAlign = ContentAlignment.MiddleLeft, Font = new Font("Microsoft YaHei UI", 9F), BackColor = Color.Transparent };
        _txtFinePerDay = new TextBox { Location = new Point(410, 62), Size = new Size(110, 25), Font = new Font("Microsoft YaHei UI", 9F) };

        // 按钮行
        _btnAdd = CreateButton("添加", 15, 105);
        _btnAdd.Click += BtnAdd_Click;
        _btnUpdate = CreateButton("修改", 105, 105);
        _btnUpdate.Click += BtnUpdate_Click;
        _btnDelete = CreateButton("删除", 195, 105);
        _btnDelete.Click += BtnDelete_Click;
        _btnClear = CreateButton("清空", 285, 105);
        _btnClear.Click += (s, e) => ClearInput();

        _grpInput.Controls.AddRange(new Control[] { lblGrpTitle, lblID, _txtCategoryID, lblName, _txtCategoryName, lblDays, _txtBorrowDays, lblFine, _txtFinePerDay, _btnAdd, _btnUpdate, _btnDelete, _btnClear });

        // 返回按钮（Y坐标随面板高度增加下移）
        _btnBack = CreateButton("返回", 560, 480);
        _btnBack.Click += (s, e) => Close();

        Controls.AddRange(new Control[] { lblSearch, _txtSearchName, _btnSearch, _dgvCategories, _grpInput, _btnBack });

        Load += (s, e) => LoadData();
    }

    /// <summary>
    /// 加载类别数据到 DataGridView
    /// </summary>
    private void LoadData()
    {
        try
        {
            string keyword = _txtSearchName.Text.Trim();
            List<BookCategory> categories = _categoryService.SearchCategories(keyword);
            _dgvCategories.DataSource = categories;
            _dgvCategories.Columns[nameof(BookCategory.CategoryID)].HeaderText = "类别编号";
            _dgvCategories.Columns[nameof(BookCategory.CategoryName)].HeaderText = "类别名称";
            _dgvCategories.Columns[nameof(BookCategory.BorrowDays)].HeaderText = "可借天数";
            _dgvCategories.Columns[nameof(BookCategory.FinePerDay)].HeaderText = "日罚款(元)";
            // 新字段列宽调小，仅展示数值
            _dgvCategories.Columns[nameof(BookCategory.BorrowDays)].Width = 80;
            _dgvCategories.Columns[nameof(BookCategory.FinePerDay)].Width = 90;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"加载数据失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    /// <summary>
    /// 列表选中行变化时回填输入框
    /// </summary>
    private void DgvCategories_SelectionChanged(object sender, EventArgs e)
    {
        if (_dgvCategories.CurrentRow?.DataBoundItem is BookCategory category)
        {
            _txtCategoryID.Text = category.CategoryID;
            _txtCategoryName.Text = category.CategoryName;
            _txtBorrowDays.Text = category.BorrowDays.ToString();
            _txtFinePerDay.Text = category.FinePerDay.ToString("F2");
            // 编号为主键，选中回填后设为只读，防止用户修改主键导致更新指向错误记录
            _txtCategoryID.ReadOnly = true;
            // 灰色背景从视觉上提示用户该字段不可编辑
            _txtCategoryID.BackColor = Color.FromArgb(240, 240, 240);
        }
    }

    /// <summary>
    /// 添加按钮点击事件
    /// </summary>
    private void BtnAdd_Click(object sender, EventArgs e)
    {
        try
        {
            // 解析借阅天数和罚款标准，格式错误时给出明确提示
            if (!int.TryParse(_txtBorrowDays.Text.Trim(), out int borrowDays) || borrowDays <= 0)
            {
                MessageBox.Show("请输入有效的可借阅天数（正整数）", "输入错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (!decimal.TryParse(_txtFinePerDay.Text.Trim(), out decimal finePerDay) || finePerDay <= 0)
            {
                MessageBox.Show("请输入有效的单日罚款标准（正数）", "输入错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _categoryService.AddCategory(_txtCategoryID.Text.Trim(), _txtCategoryName.Text.Trim(), borrowDays, finePerDay);
            MessageBox.Show("添加成功", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            ClearInput();
            LoadData();
        }
        catch (BusinessException ex)
        {
            MessageBox.Show(ex.Message, "操作失败", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"操作失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    /// <summary>
    /// 修改按钮点击事件
    /// </summary>
    private void BtnUpdate_Click(object sender, EventArgs e)
    {
        if (_dgvCategories.CurrentRow?.DataBoundItem is not BookCategory category)
        {
            MessageBox.Show("请先选择要修改的类别", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        try
        {
            // 解析借阅天数和罚款标准，格式错误时给出明确提示
            if (!int.TryParse(_txtBorrowDays.Text.Trim(), out int borrowDays) || borrowDays <= 0)
            {
                MessageBox.Show("请输入有效的可借阅天数（正整数）", "输入错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (!decimal.TryParse(_txtFinePerDay.Text.Trim(), out decimal finePerDay) || finePerDay <= 0)
            {
                MessageBox.Show("请输入有效的单日罚款标准（正数）", "输入错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _categoryService.UpdateCategory(category.CategoryID, _txtCategoryName.Text.Trim(), borrowDays, finePerDay);
            MessageBox.Show("修改成功", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            LoadData();
        }
        catch (BusinessException ex)
        {
            MessageBox.Show(ex.Message, "操作失败", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"操作失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    /// <summary>
    /// 删除按钮点击事件
    /// </summary>
    private void BtnDelete_Click(object sender, EventArgs e)
    {
        if (_dgvCategories.CurrentRow?.DataBoundItem is not BookCategory category)
        {
            MessageBox.Show("请先选择要删除的类别", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        if (MessageBox.Show($"确定要删除类别《{category.CategoryName}》吗？", "确认删除", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
        {
            return;
        }

        try
        {
            _categoryService.DeleteCategory(category.CategoryID);
            MessageBox.Show("删除成功", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            ClearInput();
            LoadData();
        }
        catch (BusinessException ex)
        {
            MessageBox.Show(ex.Message, "操作失败", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"操作失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    /// <summary>
    /// 清空输入框并重置编号可编辑状态
    /// </summary>
    private void ClearInput()
    {
        _txtCategoryID.Clear();
        _txtCategoryName.Clear();
        _txtBorrowDays.Clear();
        _txtFinePerDay.Clear();
        _txtCategoryID.ReadOnly = false;
        _txtCategoryID.BackColor = Color.White;
        // 清除列表选中状态，避免选中行与已清空的输入框内容不一致造成误解
        if (_dgvCategories.CurrentRow != null)
        {
            _dgvCategories.ClearSelection();
        }
    }

    /// <summary>
    /// 创建统一风格的按钮
    /// </summary>
    private static Button CreateButton(string text, int x, int y)
    {
        return new Button
        {
            Text = text,
            Font = new Font("Microsoft YaHei UI", 9F),
            Size = new Size(80, 30),
            Location = new Point(x, y),
            BackColor = Color.FromArgb(64, 158, 255),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            TextAlign = ContentAlignment.MiddleCenter
        };
    }
}
