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
    private readonly Button _btnSearch;
    private readonly Button _btnAdd;
    private readonly Button _btnUpdate;
    private readonly Button _btnDelete;
    private readonly Button _btnClear;
    private readonly Button _btnBack;
    private readonly GroupBox _grpInput;

    /// <summary>
    /// 构造图书类别管理窗体
    /// </summary>
    public BookCategoryForm()
    {
        Text = "图书类别管理";
        Size = new Size(700, 560);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        BackColor = Color.FromArgb(245, 247, 250);

        // 查询区域
        Label lblSearch = new()
        {
            Text = "类别名称：",
            Font = new Font("Microsoft YaHei UI", 9F),
            Location = new Point(15, 15),
            AutoSize = true
        };
        _txtSearchName = new TextBox
        {
            Font = new Font("Microsoft YaHei UI", 9F),
            Location = new Point(90, 12),
            Size = new Size(180, 25)
        };
        _btnSearch = CreateButton("查询", 285, 10);
        _btnSearch.Click += (s, e) => LoadData();

        // DataGridView 列表
        _dgvCategories = new DataGridView
        {
            Location = new Point(15, 45),
            Size = new Size(650, 250),
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            ReadOnly = true,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            BackgroundColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle
        };
        _dgvCategories.SelectionChanged += DgvCategories_SelectionChanged;

        // 输入区域
        _grpInput = new GroupBox
        {
            Text = "类别信息",
            Font = new Font("Microsoft YaHei UI", 9F),
            Location = new Point(15, 305),
            Size = new Size(650, 130)
        };

        Label lblID = new() { Text = "类别编号：", Location = new Point(20, 30), AutoSize = true, Font = new Font("Microsoft YaHei UI", 9F) };
        _txtCategoryID = new TextBox { Location = new Point(100, 27), Size = new Size(150, 25), Font = new Font("Microsoft YaHei UI", 9F) };

        Label lblName = new() { Text = "类别名称：", Location = new Point(280, 30), AutoSize = true, Font = new Font("Microsoft YaHei UI", 9F) };
        _txtCategoryName = new TextBox { Location = new Point(360, 27), Size = new Size(150, 25), Font = new Font("Microsoft YaHei UI", 9F) };

        _btnAdd = CreateButton("添加", 20, 70);
        _btnAdd.Click += BtnAdd_Click;
        _btnUpdate = CreateButton("修改", 110, 70);
        _btnUpdate.Click += BtnUpdate_Click;
        _btnDelete = CreateButton("删除", 200, 70);
        _btnDelete.Click += BtnDelete_Click;
        _btnClear = CreateButton("清空", 290, 70);
        _btnClear.Click += (s, e) => ClearInput();

        _grpInput.Controls.AddRange(new Control[] { lblID, _txtCategoryID, lblName, _txtCategoryName, _btnAdd, _btnUpdate, _btnDelete, _btnClear });

        // 返回按钮
        _btnBack = CreateButton("返回", 560, 450);
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
            // 编号为主键，修改时不可编辑
            _txtCategoryID.ReadOnly = true;
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
            _categoryService.AddCategory(_txtCategoryID.Text.Trim(), _txtCategoryName.Text.Trim());
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
            _categoryService.UpdateCategory(category.CategoryID, _txtCategoryName.Text.Trim());
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
        _txtCategoryID.ReadOnly = false;
        _txtCategoryID.BackColor = Color.White;
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
