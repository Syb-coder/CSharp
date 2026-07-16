using CampusShop.BLL;
using CampusShop.Models;

namespace CampusShop.Forms;

/// <summary>
/// 商品信息管理窗体：商品的查询、新增、修改、删除
/// 普通用户仅可查询；管理员可增删改查
/// </summary>
public class ProductForm : Form
{
    private readonly User _currentUser;
    private readonly ProductService _productService = new();
    private readonly CategoryService _categoryService = new();
    private readonly SupplierService _supplierService = new();

    // 查询区控件
    private readonly TextBox _txtSearchName;
    private readonly ComboBox _cboSearchCategory;
    private readonly ComboBox _cboSearchSupplier;
    private readonly Button _btnSearch;

    // 列表控件
    private readonly DataGridView _dgvProducts;

    // 输入区控件
    private readonly Panel _grpInput;
    private readonly TextBox _txtProductID;
    private readonly TextBox _txtProductName;
    private readonly TextBox _txtSpecification;
    private readonly TextBox _txtUnitPrice;
    private readonly TextBox _txtStockQuantity;
    private readonly ComboBox _cboCategory;
    private readonly ComboBox _cboSupplier;
    private readonly TextBox _txtOrigin;
    private readonly DateTimePicker _dtpProductionDate;

    // 操作按钮
    private readonly Button _btnAdd;
    private readonly Button _btnUpdate;
    private readonly Button _btnDelete;
    private readonly Button _btnClear;
    private readonly Button _btnBack;

    /// <summary>
    /// 构造商品信息管理窗体
    /// </summary>
    /// <param name="currentUser">当前登录用户，用于权限控制</param>
    public ProductForm(User currentUser)
    {
        _currentUser = currentUser;

        // 窗体基本属性
        Text = "商品信息管理";
        Size = new Size(900, 630);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        BackColor = Color.FromArgb(245, 247, 250);

        // ===== 查询区：Label 宽度=AutoSize实际值（5字+冒号=100px, 3字+冒号=65px, 4字+冒号=85px） =====
        Label lblSearchName = new()
        {
            Text = "商品名称：",
            Font = new Font("Microsoft YaHei UI", 9F),
            Location = new Point(15, 18),
            BackColor = Color.Transparent,
            AutoSize = false,
            Size = new Size(100, 20),
            TextAlign = ContentAlignment.MiddleLeft
        };
        _txtSearchName = new TextBox
        {
            Font = new Font("Microsoft YaHei UI", 9F),
            Location = new Point(120, 15),
            Size = new Size(130, 25)
        };

        Label lblSearchCategory = new()
        {
            Text = "类别：",
            Font = new Font("Microsoft YaHei UI", 9F),
            Location = new Point(260, 18),
            BackColor = Color.Transparent,
            AutoSize = false,
            Size = new Size(65, 20),
            TextAlign = ContentAlignment.MiddleLeft
        };
        _cboSearchCategory = new ComboBox
        {
            Font = new Font("Microsoft YaHei UI", 9F),
            Location = new Point(330, 15),
            Size = new Size(140, 25),
            DropDownStyle = ComboBoxStyle.DropDownList
        };

        Label lblSearchSupplier = new()
        {
            Text = "供货商：",
            Font = new Font("Microsoft YaHei UI", 9F),
            Location = new Point(480, 18),
            BackColor = Color.Transparent,
            AutoSize = false,
            Size = new Size(85, 20),
            TextAlign = ContentAlignment.MiddleLeft
        };
        _cboSearchSupplier = new ComboBox
        {
            Font = new Font("Microsoft YaHei UI", 9F),
            Location = new Point(570, 15),
            Size = new Size(160, 25),
            DropDownStyle = ComboBoxStyle.DropDownList
        };

        _btnSearch = CreateButton("查询", 740, 13);
        _btnSearch.Click += (s, e) => LoadData();

        // ===== 列表区：宽度适配 ClientSize 884（左 15 + 宽 854 + 右 15） =====
        _dgvProducts = new DataGridView
        {
            Location = new Point(15, 50),
            Size = new Size(854, 280),
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            ReadOnly = true,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None,
            BackgroundColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            RowHeadersVisible = false
        };
        _dgvProducts.SelectionChanged += DgvProducts_SelectionChanged;

        // ===== 输入区（Panel 宽度 854 适配窗体客户区，避免超出被裁切） =====
        _grpInput = new Panel
        {
            BackColor = Color.FromArgb(245, 247, 250),
            BorderStyle = BorderStyle.FixedSingle,
            Location = new Point(15, 335),
            Size = new Size(854, 200)
        };
        Label lblGrpTitle = new()
        {
            Text = "商品信息",
            Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold),
            ForeColor = Color.FromArgb(64, 158, 255),
            BackColor = Color.Transparent,
            Location = new Point(10, 5),
            AutoSize = true
        };

        // 第一行：商品编号(100)、商品名称(100)、规格(65) —— Label 宽度=AutoSize实际值
        Label lblProductID = new() { Text = "商品编号：", Font = new Font("Microsoft YaHei UI", 9F), Location = new Point(15, 33), BackColor = Color.Transparent, AutoSize = false, Size = new Size(100, 20), TextAlign = ContentAlignment.MiddleLeft };
        _txtProductID = new TextBox { Font = new Font("Microsoft YaHei UI", 9F), Location = new Point(120, 30), Size = new Size(120, 25) };

        Label lblProductName = new() { Text = "商品名称：", Font = new Font("Microsoft YaHei UI", 9F), Location = new Point(250, 33), BackColor = Color.Transparent, AutoSize = false, Size = new Size(100, 20), TextAlign = ContentAlignment.MiddleLeft };
        _txtProductName = new TextBox { Font = new Font("Microsoft YaHei UI", 9F), Location = new Point(355, 30), Size = new Size(150, 25) };

        Label lblSpec = new() { Text = "规格：", Font = new Font("Microsoft YaHei UI", 9F), Location = new Point(515, 33), BackColor = Color.Transparent, AutoSize = false, Size = new Size(65, 20), TextAlign = ContentAlignment.MiddleLeft };
        _txtSpecification = new TextBox { Font = new Font("Microsoft YaHei UI", 9F), Location = new Point(585, 30), Size = new Size(120, 25) };

        // 第二行：单价(65)、库存数量(100)、类别(65)、供货商(85) —— Label 宽度=AutoSize实际值
        Label lblPrice = new() { Text = "单价：", Font = new Font("Microsoft YaHei UI", 9F), Location = new Point(15, 71), BackColor = Color.Transparent, AutoSize = false, Size = new Size(65, 20), TextAlign = ContentAlignment.MiddleLeft };
        _txtUnitPrice = new TextBox { Font = new Font("Microsoft YaHei UI", 9F), Location = new Point(85, 68), Size = new Size(80, 25) };

        Label lblStock = new() { Text = "库存数量：", Font = new Font("Microsoft YaHei UI", 9F), Location = new Point(175, 71), BackColor = Color.Transparent, AutoSize = false, Size = new Size(100, 20), TextAlign = ContentAlignment.MiddleLeft };
        _txtStockQuantity = new TextBox { Font = new Font("Microsoft YaHei UI", 9F), Location = new Point(280, 68), Size = new Size(80, 25) };

        Label lblCategory = new() { Text = "类别：", Font = new Font("Microsoft YaHei UI", 9F), Location = new Point(370, 71), BackColor = Color.Transparent, AutoSize = false, Size = new Size(65, 20), TextAlign = ContentAlignment.MiddleLeft };
        _cboCategory = new ComboBox
        {
            Font = new Font("Microsoft YaHei UI", 9F),
            Location = new Point(440, 68),
            Size = new Size(140, 25),
            DropDownStyle = ComboBoxStyle.DropDownList
        };

        Label lblSupplier = new() { Text = "供货商：", Font = new Font("Microsoft YaHei UI", 9F), Location = new Point(590, 71), BackColor = Color.Transparent, AutoSize = false, Size = new Size(85, 20), TextAlign = ContentAlignment.MiddleLeft };
        _cboSupplier = new ComboBox
        {
            Font = new Font("Microsoft YaHei UI", 9F),
            Location = new Point(680, 68),
            Size = new Size(150, 25),
            DropDownStyle = ComboBoxStyle.DropDownList
        };

        // 第三行：产地(65)、生产日期(100) —— Label 宽度=AutoSize实际值
        Label lblOrigin = new() { Text = "产地：", Font = new Font("Microsoft YaHei UI", 9F), Location = new Point(15, 110), BackColor = Color.Transparent, AutoSize = false, Size = new Size(65, 20), TextAlign = ContentAlignment.MiddleLeft };
        _txtOrigin = new TextBox { Font = new Font("Microsoft YaHei UI", 9F), Location = new Point(85, 108), Size = new Size(150, 25) };

        Label lblProductionDate = new() { Text = "生产日期：", Font = new Font("Microsoft YaHei UI", 9F), Location = new Point(250, 110), BackColor = Color.Transparent, AutoSize = false, Size = new Size(100, 20), TextAlign = ContentAlignment.MiddleLeft };
        _dtpProductionDate = new DateTimePicker { Font = new Font("Microsoft YaHei UI", 9F), Location = new Point(355, 108), Size = new Size(150, 25), Format = DateTimePickerFormat.Short };

        // 第四行：操作按钮（从第三行下移至第四行）
        _btnAdd = CreateButton("添加", 15, 145);
        _btnAdd.Click += BtnAdd_Click;
        _btnUpdate = CreateButton("修改", 105, 145);
        _btnUpdate.Click += BtnUpdate_Click;
        _btnDelete = CreateButton("删除", 195, 145);
        _btnDelete.Click += BtnDelete_Click;
        _btnClear = CreateButton("清空", 285, 145);
        _btnClear.Click += (s, e) => ClearInput();

        _grpInput.Controls.AddRange(new Control[]
        {
            lblGrpTitle,
            lblProductID, _txtProductID,
            lblProductName, _txtProductName,
            lblSpec, _txtSpecification,
            lblPrice, _txtUnitPrice,
            lblStock, _txtStockQuantity,
            lblCategory, _cboCategory,
            lblSupplier, _cboSupplier,
            lblOrigin, _txtOrigin,
            lblProductionDate, _dtpProductionDate,
            _btnAdd, _btnUpdate, _btnDelete, _btnClear
        });

        // 返回按钮（Y=540 适配窗体高度增加后的客户区，避免超出底部）
        _btnBack = CreateButton("返回", 790, 540);
        _btnBack.Click += (s, e) => Close();

        Controls.AddRange(new Control[]
        {
            lblSearchName, _txtSearchName,
            lblSearchCategory, _cboSearchCategory,
            lblSearchSupplier, _cboSearchSupplier,
            _btnSearch,
            _dgvProducts,
            _grpInput,
            _btnBack
        });

        // 窗体加载时先加载下拉框数据，再加载商品列表
        Load += (s, e) =>
        {
            LoadCategories();
            LoadSuppliers();
            LoadData();
        };

        // 根据权限启用/禁用操作按钮
        ApplyPermission();
    }

    /// <summary>
    /// 加载商品类别到查询与输入下拉框
    /// </summary>
    private void LoadCategories()
    {
        List<Category> categories = _categoryService.GetAllCategories();

        // 查询下拉框：首项"全部"，空编号表示不按类别过滤
        List<Category> searchSource = new()
        {
            new Category { CategoryID = "", CategoryName = "全部" }
        };
        searchSource.AddRange(categories);
        _cboSearchCategory.DisplayMember = nameof(Category.CategoryName);
        _cboSearchCategory.ValueMember = nameof(Category.CategoryID);
        _cboSearchCategory.DataSource = searchSource;

        // 输入下拉框：首项"请选择"，空编号触发业务校验
        // 使用独立列表实例，避免与查询下拉框共享 BindingContext 导致选中联动
        List<Category> inputSource = new()
        {
            new Category { CategoryID = "", CategoryName = "请选择" }
        };
        inputSource.AddRange(categories);
        _cboCategory.DisplayMember = nameof(Category.CategoryName);
        _cboCategory.ValueMember = nameof(Category.CategoryID);
        _cboCategory.DataSource = inputSource;
    }

    /// <summary>
    /// 加载供货商到查询与输入下拉框
    /// </summary>
    private void LoadSuppliers()
    {
        List<Supplier> suppliers = _supplierService.GetAllSuppliers();

        // 查询下拉框：首项"全部"
        List<Supplier> searchSource = new()
        {
            new Supplier { SupplierID = "", SupplierName = "全部" }
        };
        searchSource.AddRange(suppliers);
        _cboSearchSupplier.DisplayMember = nameof(Supplier.SupplierName);
        _cboSearchSupplier.ValueMember = nameof(Supplier.SupplierID);
        _cboSearchSupplier.DataSource = searchSource;

        // 输入下拉框：首项"请选择"
        List<Supplier> inputSource = new()
        {
            new Supplier { SupplierID = "", SupplierName = "请选择" }
        };
        inputSource.AddRange(suppliers);
        _cboSupplier.DisplayMember = nameof(Supplier.SupplierName);
        _cboSupplier.ValueMember = nameof(Supplier.SupplierID);
        _cboSupplier.DataSource = inputSource;
    }

    /// <summary>
    /// 按查询条件加载商品列表
    /// </summary>
    private void LoadData()
    {
        try
        {
            string name = _txtSearchName.Text.Trim();
            string categoryID = _cboSearchCategory.SelectedValue?.ToString() ?? "";
            string supplierID = _cboSearchSupplier.SelectedValue?.ToString() ?? "";

            List<Product> products = _productService.SearchProducts(name, categoryID, supplierID);
            _dgvProducts.DataSource = products;
            SetColumnHeaders();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"加载数据失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    /// <summary>
    /// 设置 DataGridView 列标题、格式与显示顺序
    /// </summary>
    private void SetColumnHeaders()
    {
        // 隐藏外键列，仅展示名称列
        SetColumn(nameof(Product.CategoryID), null, 0);
        SetColumn(nameof(Product.SupplierID), null, 0);

        // 新增产地、生产日期两列后重新分配列宽，总宽适配 DataGridView 854
        SetColumn(nameof(Product.ProductID), "商品编号", 90);
        SetColumn(nameof(Product.ProductName), "商品名称", 130);
        SetColumn(nameof(Product.Specification), "规格", 80);
        SetColumn(nameof(Product.UnitPrice), "单价", 70);
        SetColumn(nameof(Product.StockQuantity), "库存数量", 80);
        SetColumn(nameof(Product.CategoryName), "类别", 80);
        SetColumn(nameof(Product.SupplierName), "供货商", 124);
        SetColumn(nameof(Product.Origin), "产地", 100);
        SetColumn(nameof(Product.ProductionDate), "生产日期", 100);

        // 单价格式化，避免显示多余小数位
        if (_dgvProducts.Columns.Contains(nameof(Product.UnitPrice)))
        {
            _dgvProducts.Columns[nameof(Product.UnitPrice)].DefaultCellStyle.Format = "0.00";
        }
    }

    /// <summary>
    /// 设置单列标题、宽度或隐藏列
    /// </summary>
    private void SetColumn(string propName, string headerText, int width)
    {
        if (!_dgvProducts.Columns.Contains(propName))
        {
            return;
        }
        if (headerText == null)
        {
            _dgvProducts.Columns[propName].Visible = false;
        }
        else
        {
            _dgvProducts.Columns[propName].HeaderText = headerText;
            _dgvProducts.Columns[propName].Width = width;
        }
    }

    /// <summary>
    /// 列表选中行变化时回填输入框
    /// </summary>
    private void DgvProducts_SelectionChanged(object sender, EventArgs e)
    {
        // 绑定切换瞬间 DataBoundItem 可能为 null，需做类型守卫避免空引用
        if (_dgvProducts.CurrentRow?.DataBoundItem is not Product product)
        {
            return;
        }

        _txtProductID.Text = product.ProductID;
        _txtProductName.Text = product.ProductName;
        _txtSpecification.Text = product.Specification;
        _txtUnitPrice.Text = product.UnitPrice.ToString("0.00");
        _txtStockQuantity.Text = product.StockQuantity.ToString();

        // 下拉框选中对应项
        if (!string.IsNullOrEmpty(product.CategoryID))
        {
            _cboCategory.SelectedValue = product.CategoryID;
        }
        else
        {
            _cboCategory.SelectedIndex = 0;
        }

        if (!string.IsNullOrEmpty(product.SupplierID))
        {
            _cboSupplier.SelectedValue = product.SupplierID;
        }
        else
        {
            _cboSupplier.SelectedIndex = 0;
        }

        // 产地直接回填文本框，空值用空字符串覆盖避免残留旧数据
        _txtOrigin.Text = product.Origin ?? string.Empty;

        // 生产日期有值则回填，无值则重置为今天（DateTimePicker 不能赋 null）
        _dtpProductionDate.Value = product.ProductionDate ?? DateTime.Today;

        // 商品编号为主键，修改时不可编辑
        _txtProductID.ReadOnly = true;
    }

    /// <summary>
    /// 添加按钮点击事件
    /// </summary>
    private void BtnAdd_Click(object sender, EventArgs e)
    {
        // 数值字段预解析：提前拦截格式错误，避免 BusinessException 与格式异常混在一起难以定位
        if (!decimal.TryParse(_txtUnitPrice.Text.Trim(), out decimal unitPrice))
        {
            MessageBox.Show("单价必须为有效的数字", "校验失败", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        if (!int.TryParse(_txtStockQuantity.Text.Trim(), out int stockQuantity))
        {
            MessageBox.Show("库存数量必须为有效的整数", "校验失败", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        try
        {
            _productService.AddProduct(
                _txtProductID.Text.Trim(),
                _txtProductName.Text.Trim(),
                _txtSpecification.Text.Trim(),
                unitPrice,
                stockQuantity,
                _cboCategory.SelectedValue?.ToString() ?? "",
                _cboSupplier.SelectedValue?.ToString() ?? "",
                _txtOrigin.Text.Trim(),
                _dtpProductionDate.Value);
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
        // 修改操作必须基于选中行，无选中时直接拦截，避免误操作空数据
        if (_dgvProducts.CurrentRow == null)
        {
            MessageBox.Show("请先选择要修改的商品", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        if (!decimal.TryParse(_txtUnitPrice.Text.Trim(), out decimal unitPrice))
        {
            MessageBox.Show("单价必须为有效的数字", "校验失败", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        if (!int.TryParse(_txtStockQuantity.Text.Trim(), out int stockQuantity))
        {
            MessageBox.Show("库存数量必须为有效的整数", "校验失败", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        try
        {
            _productService.UpdateProduct(
                _txtProductID.Text.Trim(),
                _txtProductName.Text.Trim(),
                _txtSpecification.Text.Trim(),
                unitPrice,
                stockQuantity,
                _cboCategory.SelectedValue?.ToString() ?? "",
                _cboSupplier.SelectedValue?.ToString() ?? "",
                _txtOrigin.Text.Trim(),
                _dtpProductionDate.Value);
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
        if (_dgvProducts.CurrentRow == null)
        {
            MessageBox.Show("请先选择要删除的商品", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        // 二次确认：删除是不可逆操作，存在售卖记录时虽被业务层拦截，但仍需让用户明确意图
        string productID = _txtProductID.Text.Trim();
        if (string.IsNullOrEmpty(productID))
        {
            MessageBox.Show("商品编号为空，无法删除", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        DialogResult result = MessageBox.Show(
            $"确认删除商品 [{productID}] 吗？",
            "确认删除",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);
        if (result != DialogResult.Yes)
        {
            return;
        }

        try
        {
            _productService.DeleteProduct(productID);
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
        _txtProductID.Clear();
        _txtProductName.Clear();
        _txtSpecification.Clear();
        _txtUnitPrice.Clear();
        _txtStockQuantity.Clear();
        _cboCategory.SelectedIndex = 0;
        _cboSupplier.SelectedIndex = 0;
        _txtOrigin.Clear();
        _dtpProductionDate.Value = DateTime.Today;
        _txtProductID.ReadOnly = false;
        _txtProductID.Focus();
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
