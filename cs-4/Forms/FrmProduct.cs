using System.Data;
using CampusStore.BLL;
using CampusStore.Common;
using CampusStore.Models;

namespace CampusStore.Forms;

/// <summary>
/// 商品信息管理窗体
/// </summary>
/// <remarks>
/// 提供商品的增删改查功能，支持按商品名称、类别、供货商组合条件检索。
/// 所有 Label 使用 UiHelper.CreateLabel 创建（参见 cs-0/experience.md 经验一：避免文字被遮挡）。
/// </remarks>
public partial class FrmProduct : Form
{
    // ========== 业务对象 ==========
    private readonly ProductManager _productManager = new();
    private readonly CategoryManager _categoryManager = new();
    private readonly SupplierManager _supplierManager = new();

    /// <summary>当前选中商品 ID（0 表示未选中，用于修改/删除）</summary>
    private int _selectedProductID;

    // ========== 查询区控件 ==========
    private Label lblProductName;
    private TextBox txtProductName;
    private Label lblCategory;
    private ComboBox cboCategory;
    private Label lblSupplier;
    private ComboBox cboSupplier;
    private Button btnQuery;
    private Button btnRefresh;

    // ========== 数据网格 ==========
    private DataGridView dgvProduct;

    // ========== 编辑区控件 - 第 1 行 ==========
    private Label lblEditName;
    private TextBox txtEditName;
    private Label lblEditCategory;
    private ComboBox cboEditCategory;
    private Label lblEditSupplier;
    private ComboBox cboEditSupplier;

    // ========== 编辑区控件 - 第 2 行 ==========
    private Label lblPrice;
    private TextBox txtPrice;
    private Label lblOrigin;
    private TextBox txtOrigin;
    private Label lblProduceDate;
    private DateTimePicker dtpProduceDate;
    private Label lblStock;
    private NumericUpDown nudStock;

    // ========== 操作按钮 ==========
    private Button btnAdd;
    private Button btnUpdate;
    private Button btnDelete;
    private Button btnClear;

    /// <summary>
    /// 构造函数
    /// </summary>
    public FrmProduct()
    {
        InitializeComponent();
        SetupGridColumns();
        LoadCombos();
        BindGrid();
        BindEvents();
    }

    /// <summary>
    /// 初始化所有 UI 控件并加入窗体
    /// </summary>
    private void InitializeComponent()
    {
        UiHelper.SetupForm(this, "商品信息管理", 950, 650);

        // ---- 查询区（Y=15）----
        lblProductName = UiHelper.CreateLabel("商品名称：", 15, 18);
        txtProductName = UiHelper.CreateTextBox(UiHelper.NextX(lblProductName), 15, 120);

        lblCategory = UiHelper.CreateLabel("类别：", txtProductName.Right + 15, 18);
        cboCategory = UiHelper.CreateComboBox(UiHelper.NextX(lblCategory), 15, 100);

        lblSupplier = UiHelper.CreateLabel("供货商：", cboCategory.Right + 15, 18);
        cboSupplier = UiHelper.CreateComboBox(UiHelper.NextX(lblSupplier), 15, 120);

        btnQuery = UiHelper.CreatePrimaryButton("查询", cboSupplier.Right + 15, 15);
        btnRefresh = UiHelper.CreateSecondaryButton("刷新", btnQuery.Right + 10, 15);

        // ---- 数据网格（Y=55，宽 910，高 280）----
        dgvProduct = new DataGridView
        {
            Location = new Point(15, 55),
            Size = new Size(910, 280)
        };
        UiHelper.SetGridStyle(dgvProduct);

        // ---- 编辑区第 1 行（Y=350）----
        lblEditName = UiHelper.CreateLabel("商品名称：", 15, 353);
        txtEditName = UiHelper.CreateTextBox(UiHelper.NextX(lblEditName), 350, 150);

        lblEditCategory = UiHelper.CreateLabel("类别：", txtEditName.Right + 15, 353);
        cboEditCategory = UiHelper.CreateComboBox(UiHelper.NextX(lblEditCategory), 350, 120);

        lblEditSupplier = UiHelper.CreateLabel("供货商：", cboEditCategory.Right + 15, 353);
        cboEditSupplier = UiHelper.CreateComboBox(UiHelper.NextX(lblEditSupplier), 350, 120);

        // ---- 编辑区第 2 行（Y=385）----
        lblPrice = UiHelper.CreateLabel("单价：", 15, 388);
        txtPrice = UiHelper.CreateTextBox(UiHelper.NextX(lblPrice), 385, 100);

        lblOrigin = UiHelper.CreateLabel("产地：", txtPrice.Right + 15, 388);
        txtOrigin = UiHelper.CreateTextBox(UiHelper.NextX(lblOrigin), 385, 120);

        lblProduceDate = UiHelper.CreateLabel("生产日期：", txtOrigin.Right + 15, 388);
        dtpProduceDate = new DateTimePicker
        {
            Location = new Point(UiHelper.NextX(lblProduceDate), 385),
            Size = new Size(150, 25),
            ShowCheckBox = true,
            Checked = false,
            Format = DateTimePickerFormat.Short
        };

        lblStock = UiHelper.CreateLabel("库存数量：", dtpProduceDate.Right + 15, 388);
        nudStock = new NumericUpDown
        {
            Location = new Point(UiHelper.NextX(lblStock), 385),
            Size = new Size(80, 25),
            Minimum = 0,
            Maximum = 999999,
            Value = 0
        };

        // ---- 操作按钮（Y=560）----
        btnAdd = UiHelper.CreatePrimaryButton("添加", 15, 560);
        btnUpdate = UiHelper.CreatePrimaryButton("修改", btnAdd.Right + 10, 560);
        btnDelete = UiHelper.CreatePrimaryButton("删除", btnUpdate.Right + 10, 560);
        btnClear = UiHelper.CreateSecondaryButton("清空", btnDelete.Right + 10, 560);

        // ---- 添加所有控件到窗体 ----
        SuspendLayout();
        Controls.AddRange(new Control[]
        {
            lblProductName, txtProductName, lblCategory, cboCategory,
            lblSupplier, cboSupplier, btnQuery, btnRefresh,
            dgvProduct,
            lblEditName, txtEditName, lblEditCategory, cboEditCategory,
            lblEditSupplier, cboEditSupplier,
            lblPrice, txtPrice, lblOrigin, txtOrigin,
            lblProduceDate, dtpProduceDate, lblStock, nudStock,
            btnAdd, btnUpdate, btnDelete, btnClear
        });
        ResumeLayout(false);
        PerformLayout();
    }

    /// <summary>
    /// 配置商品数据网格列（手动定义列以使用中文标题）
    /// </summary>
    private void SetupGridColumns()
    {
        dgvProduct.AutoGenerateColumns = false;
        dgvProduct.Columns.Add(new DataGridViewTextBoxColumn { Name = "productID", DataPropertyName = "productID", HeaderText = "编号" });
        dgvProduct.Columns.Add(new DataGridViewTextBoxColumn { Name = "displayNo", DataPropertyName = "displayNo", HeaderText = "可读编号" });
        dgvProduct.Columns.Add(new DataGridViewTextBoxColumn { Name = "productName", DataPropertyName = "productName", HeaderText = "商品名称" });
        dgvProduct.Columns.Add(new DataGridViewTextBoxColumn { Name = "categoryName", DataPropertyName = "categoryName", HeaderText = "类别" });
        dgvProduct.Columns.Add(new DataGridViewTextBoxColumn { Name = "unitPrice", DataPropertyName = "unitPrice", HeaderText = "单价", DefaultCellStyle = new DataGridViewCellStyle { Format = "N2" } });
        dgvProduct.Columns.Add(new DataGridViewTextBoxColumn { Name = "origin", DataPropertyName = "origin", HeaderText = "产地" });
        dgvProduct.Columns.Add(new DataGridViewTextBoxColumn { Name = "produceDate", DataPropertyName = "produceDate", HeaderText = "生产日期", DefaultCellStyle = new DataGridViewCellStyle { Format = "yyyy-MM-dd" } });
        dgvProduct.Columns.Add(new DataGridViewTextBoxColumn { Name = "stockQuantity", DataPropertyName = "stockQuantity", HeaderText = "库存" });
        dgvProduct.Columns.Add(new DataGridViewTextBoxColumn { Name = "supplierName", DataPropertyName = "supplierName", HeaderText = "供货商" });
    }

    /// <summary>
    /// 绑定事件
    /// </summary>
    private void BindEvents()
    {
        btnQuery.Click += BtnQuery_Click;
        btnRefresh.Click += BtnRefresh_Click;
        dgvProduct.SelectionChanged += DgvProduct_SelectionChanged;
        btnAdd.Click += BtnAdd_Click;
        btnUpdate.Click += BtnUpdate_Click;
        btnDelete.Click += BtnDelete_Click;
        btnClear.Click += BtnClear_Click;
    }

    // ========== 数据加载 ==========

    /// <summary>
    /// 加载下拉框数据
    /// </summary>
    private void LoadCombos()
    {
        // 查询区类别下拉框（含"全部"选项）
        DataTable catDt = _categoryManager.GetAll();
        DataRow allCatRow = catDt.NewRow();
        allCatRow["categoryID"] = 0;
        allCatRow["categoryName"] = "全部";
        catDt.Rows.InsertAt(allCatRow, 0);
        cboCategory.DisplayMember = "categoryName";
        cboCategory.ValueMember = "categoryID";
        cboCategory.DataSource = catDt;
        cboCategory.SelectedIndex = 0;

        // 查询区供货商下拉框（含"全部"选项）
        List<SupplierInfo> suppliers = _supplierManager.GetAllList();
        suppliers.Insert(0, new SupplierInfo { SupplierID = 0, SupplierName = "全部" });
        cboSupplier.DisplayMember = "SupplierName";
        cboSupplier.ValueMember = "SupplierID";
        cboSupplier.DataSource = suppliers;
        cboSupplier.SelectedIndex = 0;

        // 编辑区类别下拉框（不含"全部"）
        DataTable catDtEdit = _categoryManager.GetAll();
        cboEditCategory.DisplayMember = "categoryName";
        cboEditCategory.ValueMember = "categoryID";
        cboEditCategory.DataSource = catDtEdit;

        // 编辑区供货商下拉框（不含"全部"）
        List<SupplierInfo> suppliersEdit = _supplierManager.GetAllList();
        cboEditSupplier.DisplayMember = "SupplierName";
        cboEditSupplier.ValueMember = "SupplierID";
        cboEditSupplier.DataSource = suppliersEdit;
    }

    /// <summary>
    /// 绑定商品数据网格
    /// </summary>
    private void BindGrid()
    {
        dgvProduct.DataSource = _productManager.GetAll();
    }

    // ========== 事件处理 ==========

    /// <summary>
    /// 查询按钮：按条件检索商品
    /// </summary>
    private void BtnQuery_Click(object sender, EventArgs e)
    {
        try
        {
            string name = txtProductName.Text.Trim();
            int? catID = GetComboNullableInt(cboCategory);
            int? supID = GetComboNullableInt(cboSupplier);
            dgvProduct.DataSource = _productManager.Search(name, catID, supID);
        }
        catch (Exception ex)
        {
            UiHelper.HandleException(ex, this);
        }
    }

    /// <summary>
    /// 刷新按钮：重新加载全部商品
    /// </summary>
    private void BtnRefresh_Click(object sender, EventArgs e)
    {
        try
        {
            txtProductName.Clear();
            cboCategory.SelectedIndex = 0;
            cboSupplier.SelectedIndex = 0;
            BindGrid();
        }
        catch (Exception ex)
        {
            UiHelper.HandleException(ex, this);
        }
    }

    /// <summary>
    /// 网格选中行变化：填充编辑区控件
    /// </summary>
    private void DgvProduct_SelectionChanged(object sender, EventArgs e)
    {
        if (dgvProduct.CurrentRow == null) return;

        DataRowView drv = dgvProduct.CurrentRow.DataBoundItem as DataRowView;
        if (drv == null) return;

        _selectedProductID = Convert.ToInt32(drv["productID"]);
        txtEditName.Text = drv["productName"].ToString();
        cboEditCategory.SelectedValue = Convert.ToInt32(drv["categoryID"]);
        cboEditSupplier.SelectedValue = Convert.ToInt32(drv["supplierID"]);
        txtPrice.Text = Convert.ToDecimal(drv["unitPrice"]).ToString("F2");
        txtOrigin.Text = drv["origin"].ToString();

        // 处理可空日期
        dtpProduceDate.Checked = drv["produceDate"] != DBNull.Value;
        if (dtpProduceDate.Checked)
            dtpProduceDate.Value = Convert.ToDateTime(drv["produceDate"]);

        int stock = Convert.ToInt32(drv["stockQuantity"]);
        nudStock.Value = stock >= nudStock.Minimum && stock <= nudStock.Maximum ? stock : 0;
    }

    /// <summary>
    /// 添加按钮
    /// </summary>
    private void BtnAdd_Click(object sender, EventArgs e)
    {
        try
        {
            ProductInfo info = BuildProductFromEditArea();
            _productManager.Add(info);
            BindGrid();
            ClearEditArea();
            MessageBox.Show("添加成功", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            UiHelper.HandleException(ex, this);
        }
    }

    /// <summary>
    /// 修改按钮
    /// </summary>
    private void BtnUpdate_Click(object sender, EventArgs e)
    {
        try
        {
            ProductInfo info = BuildProductFromEditArea();
            info.ProductID = _selectedProductID;
            _productManager.Update(info);
            BindGrid();
            MessageBox.Show("修改成功", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            UiHelper.HandleException(ex, this);
        }
    }

    /// <summary>
    /// 删除按钮
    /// </summary>
    private void BtnDelete_Click(object sender, EventArgs e)
    {
        try
        {
            if (_selectedProductID <= 0)
                throw new BusinessException("请先选择要删除的商品");
            if (!UiHelper.ConfirmDelete(txtEditName.Text, this))
                return;
            _productManager.Delete(_selectedProductID);
            BindGrid();
            ClearEditArea();
            MessageBox.Show("删除成功", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            UiHelper.HandleException(ex, this);
        }
    }

    /// <summary>
    /// 清空按钮：重置编辑区并取消选中
    /// </summary>
    private void BtnClear_Click(object sender, EventArgs e)
    {
        ClearEditArea();
    }

    // ========== 辅助方法 ==========

    /// <summary>
    /// 从编辑区控件构建 ProductInfo 对象
    /// </summary>
    /// <returns>填充好的 ProductInfo 实例</returns>
    private ProductInfo BuildProductFromEditArea()
    {
        if (!decimal.TryParse(txtPrice.Text.Trim(), out decimal price))
            throw new BusinessException("请输入有效的单价");

        return new ProductInfo
        {
            ProductID = 0,
            ProductName = txtEditName.Text.Trim(),
            CategoryID = Convert.ToInt32(cboEditCategory.SelectedValue),
            SupplierID = Convert.ToInt32(cboEditSupplier.SelectedValue),
            UnitPrice = price,
            Origin = txtOrigin.Text.Trim(),
            ProduceDate = dtpProduceDate.Checked ? dtpProduceDate.Value : null,
            StockQuantity = (int)nudStock.Value
        };
    }

    /// <summary>
    /// 清空编辑区控件并重置选中状态
    /// </summary>
    private void ClearEditArea()
    {
        _selectedProductID = 0;
        txtEditName.Clear();
        if (cboEditCategory.Items.Count > 0) cboEditCategory.SelectedIndex = 0;
        if (cboEditSupplier.Items.Count > 0) cboEditSupplier.SelectedIndex = 0;
        txtPrice.Clear();
        txtOrigin.Clear();
        dtpProduceDate.Checked = false;
        nudStock.Value = 0;
        dgvProduct.ClearSelection();
    }

    /// <summary>
    /// 读取 ComboBox 的可空整数值（值为 0 或 null 时返回 null）
    /// </summary>
    /// <param name="cbo">查询区 ComboBox</param>
    /// <returns>可空整数值</returns>
    private static int? GetComboNullableInt(ComboBox cbo)
    {
        if (cbo.SelectedValue == null) return null;
        int value = Convert.ToInt32(cbo.SelectedValue);
        return value > 0 ? value : null;
    }
}
