using CampusShop.BLL;
using CampusShop.Models;

namespace CampusShop.Forms;

/// <summary>
/// 商品类别管理窗体：类别的增删改查，支持树状层级结构展示
/// 普通用户仅可查询；管理员可增删改查
/// </summary>
public class CategoryForm : Form
{
    private readonly User _currentUser;
    private readonly CategoryService _categoryService = new();

    // 查询区控件
    private readonly TextBox _txtSearchName;
    private readonly Button _btnSearch;

    // 列表区控件：TreeView 展示树状层级
    private readonly TreeView _tvCategories;

    // 输入区控件
    private readonly Panel _grpInput;
    private readonly TextBox _txtCategoryID;
    private readonly TextBox _txtCategoryName;
    private readonly ComboBox _cboParentCategory;
    private readonly TextBox _txtCategoryDesc;
    private readonly DateTimePicker _dtpAddTime;
    private readonly Button _btnAdd;
    private readonly Button _btnUpdate;
    private readonly Button _btnDelete;
    private readonly Button _btnClear;
    private readonly Button _btnBack;

    // 类别数据缓存，供父类别下拉框和 TreeView 共用
    private List<Category> _allCategories = new();

    /// <summary>
    /// 构造商品类别管理窗体
    /// </summary>
    /// <param name="currentUser">当前登录用户，用于权限控制</param>
    public CategoryForm(User currentUser)
    {
        _currentUser = currentUser;

        // 窗体基本属性
        Text = "商品类别管理";
        Size = new Size(800, 560);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        BackColor = Color.FromArgb(245, 247, 250);

        // ===== 查询区：Label 宽度=100（"类别名称："5字+冒号需100px） =====
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

        // ===== 列表区：TreeView 展示树状层级结构 =====
        Label lblTreeTitle = new()
        {
            Text = "类别层级",
            Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold),
            ForeColor = Color.FromArgb(64, 158, 255),
            Location = new Point(15, 45),
            AutoSize = true,
            BackColor = Color.Transparent
        };
        _tvCategories = new TreeView
        {
            Font = new Font("Microsoft YaHei UI", 9F),
            Location = new Point(15, 70),
            Size = new Size(360, 385),
            BorderStyle = BorderStyle.FixedSingle,
            ShowPlusMinus = true,
            ShowLines = true,
            FullRowSelect = true
        };
        _tvCategories.AfterSelect += TvCategories_AfterSelect;

        // ===== 输入区 =====
        _grpInput = new Panel
        {
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = Color.FromArgb(245, 247, 250),
            Location = new Point(390, 70),
            Size = new Size(379, 385)
        };
        Label lblGrpTitle = new()
        {
            Text = "类别信息",
            Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold),
            ForeColor = Color.FromArgb(64, 158, 255),
            Location = new Point(10, 5),
            AutoSize = true,
            BackColor = Color.Transparent
        };

        Label lblID = new() { Text = "类别编号：", Font = new Font("Microsoft YaHei UI", 9F), Location = new Point(15, 35), BackColor = Color.Transparent, AutoSize = false, Size = new Size(100, 20), TextAlign = ContentAlignment.MiddleLeft };
        _txtCategoryID = new TextBox { Font = new Font("Microsoft YaHei UI", 9F), Location = new Point(120, 32), Size = new Size(230, 25) };

        Label lblName = new() { Text = "类别名称：", Font = new Font("Microsoft YaHei UI", 9F), Location = new Point(15, 75), BackColor = Color.Transparent, AutoSize = false, Size = new Size(100, 20), TextAlign = ContentAlignment.MiddleLeft };
        _txtCategoryName = new TextBox { Font = new Font("Microsoft YaHei UI", 9F), Location = new Point(120, 72), Size = new Size(230, 25) };

        Label lblParent = new() { Text = "父类别：", Font = new Font("Microsoft YaHei UI", 9F), Location = new Point(15, 115), BackColor = Color.Transparent, AutoSize = false, Size = new Size(100, 20), TextAlign = ContentAlignment.MiddleLeft };
        _cboParentCategory = new ComboBox
        {
            Font = new Font("Microsoft YaHei UI", 9F),
            Location = new Point(120, 112),
            Size = new Size(230, 25),
            DropDownStyle = ComboBoxStyle.DropDownList
        };

        Label lblDesc = new() { Text = "类别描述：", Font = new Font("Microsoft YaHei UI", 9F), Location = new Point(15, 155), BackColor = Color.Transparent, AutoSize = false, Size = new Size(100, 20), TextAlign = ContentAlignment.MiddleLeft };
        _txtCategoryDesc = new TextBox { Font = new Font("Microsoft YaHei UI", 9F), Location = new Point(120, 152), Size = new Size(230, 25) };

        Label lblAddTime = new() { Text = "添加时间：", Font = new Font("Microsoft YaHei UI", 9F), Location = new Point(15, 195), BackColor = Color.Transparent, AutoSize = false, Size = new Size(100, 20), TextAlign = ContentAlignment.MiddleLeft };
        // DateTimePicker 无 ReadOnly 属性，用 Enabled=false 实现只读不可编辑
        _dtpAddTime = new DateTimePicker
        {
            Font = new Font("Microsoft YaHei UI", 9F),
            Location = new Point(120, 192),
            Size = new Size(230, 25),
            Format = DateTimePickerFormat.Custom,
            CustomFormat = "yyyy-MM-dd HH:mm:ss",
            Enabled = false
        };

        _btnAdd = CreateButton("添加", 15, 240);
        _btnAdd.Click += BtnAdd_Click;
        _btnUpdate = CreateButton("修改", 105, 240);
        _btnUpdate.Click += BtnUpdate_Click;
        _btnDelete = CreateButton("删除", 195, 240);
        _btnDelete.Click += BtnDelete_Click;
        _btnClear = CreateButton("清空", 285, 240);
        _btnClear.Click += (s, e) => ClearInput();

        _grpInput.Controls.AddRange(new Control[]
        {
            lblGrpTitle,
            lblID, _txtCategoryID,
            lblName, _txtCategoryName,
            lblParent, _cboParentCategory,
            lblDesc, _txtCategoryDesc,
            lblAddTime, _dtpAddTime,
            _btnAdd, _btnUpdate, _btnDelete, _btnClear
        });

        _btnBack = CreateButton("返回", 690, 480);
        _btnBack.Click += (s, e) => Close();

        Controls.AddRange(new Control[]
        {
            lblSearch, _txtSearchName, _btnSearch,
            lblTreeTitle, _tvCategories,
            _grpInput, _btnBack
        });

        Load += (s, e) => LoadData();
        ApplyPermission();
    }

    /// <summary>
    /// 加载类别数据到 TreeView 和父类别下拉框
    /// </summary>
    private void LoadData()
    {
        try
        {
            string keyword = _txtSearchName.Text.Trim();
            _allCategories = string.IsNullOrEmpty(keyword)
                ? _categoryService.GetAllCategories()
                : _categoryService.SearchByName(keyword);

            BuildTreeView(_allCategories);
            BuildParentComboBox();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"加载数据失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    /// <summary>
    /// 构建 TreeView 树状结构
    /// </summary>
    private void BuildTreeView(List<Category> categories)
    {
        _tvCategories.BeginUpdate();
        _tvCategories.Nodes.Clear();

        var rootNodes = categories.Where(c => string.IsNullOrEmpty(c.ParentCategoryID)).ToList();
        foreach (var root in rootNodes)
        {
            TreeNode node = CreateCategoryNode(root, categories);
            _tvCategories.Nodes.Add(node);
        }

        if (_tvCategories.Nodes.Count == 0)
        {
            _tvCategories.Nodes.Add("（暂无类别数据）");
        }

        _tvCategories.ExpandAll();
        _tvCategories.EndUpdate();
    }

    /// <summary>
    /// 递归创建类别节点及其子节点
    /// </summary>
    private TreeNode CreateCategoryNode(Category category, List<Category> allCategories)
    {
        TreeNode node = new($"{category.CategoryID} - {category.CategoryName}")
        {
            Tag = category
        };

        var children = allCategories.Where(c => c.ParentCategoryID == category.CategoryID).ToList();
        foreach (var child in children)
        {
            node.Nodes.Add(CreateCategoryNode(child, allCategories));
        }

        return node;
    }

    /// <summary>
    /// 构建父类别下拉框
    /// </summary>
    private void BuildParentComboBox()
    {
        List<Category> source = new()
        {
            new Category { CategoryID = "", CategoryName = "（根类别）" }
        };
        source.AddRange(_allCategories);
        _cboParentCategory.DisplayMember = nameof(Category.CategoryName);
        _cboParentCategory.ValueMember = nameof(Category.CategoryID);
        _cboParentCategory.DataSource = source;
    }

    /// <summary>
    /// TreeView 选中节点变化时回填输入框
    /// </summary>
    private void TvCategories_AfterSelect(object sender, TreeViewEventArgs e)
    {
        if (e.Node.Tag is Category category)
        {
            _txtCategoryID.Text = category.CategoryID;
            _txtCategoryName.Text = category.CategoryName;
            if (!string.IsNullOrEmpty(category.ParentCategoryID))
            {
                _cboParentCategory.SelectedValue = category.ParentCategoryID;
            }
            else
            {
                _cboParentCategory.SelectedIndex = 0;
            }
            _txtCategoryDesc.Text = category.CategoryDesc;
            // AddTime 可空，有值时回填到 DateTimePicker
            if (category.AddTime.HasValue)
            {
                _dtpAddTime.Value = category.AddTime.Value;
            }
            _txtCategoryID.ReadOnly = true;
        }
    }

    /// <summary>
    /// 添加按钮点击事件
    /// </summary>
    private void BtnAdd_Click(object sender, EventArgs e)
    {
        try
        {
            string categoryID = _txtCategoryID.Text.Trim();
            string categoryName = _txtCategoryName.Text.Trim();
            string parentCategoryID = _cboParentCategory.SelectedValue?.ToString() ?? "";

            string categoryDesc = _txtCategoryDesc.Text.Trim();
            _categoryService.AddCategory(categoryID, categoryName, parentCategoryID, categoryDesc);
            MessageBox.Show("添加成功", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            ClearInput();
            LoadData();
        }
        catch (BusinessException ex)
        {
            MessageBox.Show(ex.Message, "校验失败", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"添加失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    /// <summary>
    /// 修改按钮点击事件
    /// </summary>
    private void BtnUpdate_Click(object sender, EventArgs e)
    {
        try
        {
            string categoryID = _txtCategoryID.Text.Trim();
            string categoryName = _txtCategoryName.Text.Trim();
            string parentCategoryID = _cboParentCategory.SelectedValue?.ToString() ?? "";

            string categoryDesc = _txtCategoryDesc.Text.Trim();
            _categoryService.UpdateCategory(categoryID, categoryName, parentCategoryID, categoryDesc);
            MessageBox.Show("修改成功", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            LoadData();
        }
        catch (BusinessException ex)
        {
            MessageBox.Show(ex.Message, "校验失败", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"修改失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    /// <summary>
    /// 删除按钮点击事件
    /// </summary>
    private void BtnDelete_Click(object sender, EventArgs e)
    {
        string categoryID = _txtCategoryID.Text.Trim();
        if (string.IsNullOrEmpty(categoryID))
        {
            MessageBox.Show("请先选择要删除的类别", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        // 二次确认防止误删除
        if (MessageBox.Show($"确定要删除类别「{_txtCategoryName.Text}」吗？", "确认删除",
            MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
        {
            return;
        }

        try
        {
            _categoryService.DeleteCategory(categoryID);
            MessageBox.Show("删除成功", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            ClearInput();
            LoadData();
        }
        catch (BusinessException ex)
        {
            MessageBox.Show(ex.Message, "校验失败", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"删除失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    /// <summary>
    /// 清空输入框，恢复新增模式
    /// </summary>
    private void ClearInput()
    {
        _txtCategoryID.Clear();
        _txtCategoryName.Clear();
        _cboParentCategory.SelectedIndex = 0;
        _txtCategoryDesc.Clear();
        _dtpAddTime.Value = DateTime.Now;
        _txtCategoryID.ReadOnly = false;
        _txtCategoryID.Focus();
    }

    /// <summary>
    /// 根据权限启用/禁用操作按钮
    /// </summary>
    private void ApplyPermission()
    {
        bool isAdmin = _currentUser.UserPurview == BusinessConstants.ROLE_ADMIN;
        _btnAdd.Enabled = isAdmin;
        _btnUpdate.Enabled = isAdmin;
        _btnDelete.Enabled = isAdmin;

        if (!isAdmin)
        {
            _btnAdd.BackColor = Color.FromArgb(200, 200, 200);
            _btnUpdate.BackColor = Color.FromArgb(200, 200, 200);
            _btnDelete.BackColor = Color.FromArgb(200, 200, 200);
        }
    }

    /// <summary>
    /// 创建统一风格的操作按钮
    /// </summary>
    private static Button CreateButton(string text, int x, int y)
    {
        return new Button
        {
            Text = text,
            Font = new Font("Microsoft YaHei UI", 9F),
            Size = new Size(80, 28),
            Location = new Point(x, y),
            BackColor = Color.FromArgb(64, 158, 255),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            TextAlign = ContentAlignment.MiddleCenter
        };
    }
}
