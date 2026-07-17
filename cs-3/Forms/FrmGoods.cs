using System.Data;
using CampusMart.BLL;
using CampusMart.Common;
using CampusMart.Models;

namespace CampusMart.Forms;

/// <summary>
/// 商品信息管理窗体：提供商品的多条件查询、增删改、Excel 导出功能
/// </summary>
/// <remarks>
/// 权限控制：操作员仅可查询和导出，增删改按钮禁用；管理员可全部操作。
/// Label 宽度使用 UiHelper 动态测量，多列采用链式布局（Label→控件→Label→控件）。
/// </remarks>
public class FrmGoods : Form
{
    private readonly UserInfo _currentUser;
    private readonly GoodsBiz _goodsBiz = new();
    private readonly CategoryBiz _categoryBiz = new();
    private readonly SupplierBiz _supplierBiz = new();

    private readonly TextBox _txtSearchName;
    private readonly ComboBox _cmbSearchCategory;
    private readonly ComboBox _cmbSearchSupplier;

    private readonly DataGridView _dgvGoods;

    private readonly TextBox _txtProductID;
    private readonly TextBox _txtProductName;
    private readonly ComboBox _cmbCategory;
    private readonly TextBox _txtUnitPrice;
    private readonly TextBox _txtOrigin;
    private readonly DateTimePicker _dtpProduceDate;
    private readonly TextBox _txtStockQty;
    private readonly ComboBox _cmbSupplier;

    private readonly Button _btnAdd;
    private readonly Button _btnUpdate;
    private readonly Button _btnDelete;
    private readonly Button _btnClear;
    private readonly Button _btnReturn;

    private const int CtrlHeight = 25;

    /// <summary>
    /// 构造商品信息管理窗体
    /// </summary>
    /// <param name="currentUser">当前登录用户，用于权限判断</param>
    public FrmGoods(UserInfo currentUser)
    {
        _currentUser = currentUser;

        Text = "商品信息管理";
        Size = new Size(600, 800);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        BackColor = Color.FromArgb(245, 247, 250);

        Font labelFont = new("Microsoft YaHei UI", 9F);

        const int ctrlGap = 8;

        // ===== 查询区 GroupBox（两行布局：第一行条件，第二行按钮）=====
        GroupBox grpSearch = new()
        {
            Text = "查询条件",
            Font = labelFont,
            Size = new Size(560, 85),
            Location = new Point(15, 5)
        };

        const int sMargin = 15;
        const int sRow1Y = 22;
        const int sRow2Y = 52;

        Label lblSearchName = UiHelper.CreateLabel("商品名：", sMargin, sRow1Y, labelFont);
        _txtSearchName = new TextBox
        {
            Font = labelFont,
            Size = new Size(120, CtrlHeight),
            Location = new Point(UiHelper.NextX(lblSearchName), sRow1Y - 3)
        };

        int sCol2X = _txtSearchName.Right + ctrlGap;
        Label lblSearchCategory = UiHelper.CreateLabel("类别：", sCol2X, sRow1Y, labelFont);
        _cmbSearchCategory = new ComboBox
        {
            Font = labelFont,
            Size = new Size(120, CtrlHeight),
            Location = new Point(UiHelper.NextX(lblSearchCategory), sRow1Y - 3),
            DropDownStyle = ComboBoxStyle.DropDownList
        };

        int sCol3X = _cmbSearchCategory.Right + ctrlGap;
        Label lblSearchSupplier = UiHelper.CreateLabel("供货商：", sCol3X, sRow1Y, labelFont);
        _cmbSearchSupplier = new ComboBox
        {
            Font = labelFont,
            Size = new Size(120, CtrlHeight),
            Location = new Point(UiHelper.NextX(lblSearchSupplier), sRow1Y - 3),
            DropDownStyle = ComboBoxStyle.DropDownList
        };

        Button btnSearch = UiHelper.CreateButton("查询", sMargin, sRow2Y - 5, labelFont);
        btnSearch.Click += BtnSearch_Click;
        Button btnExport = UiHelper.CreateButton("导出", btnSearch.Right + 10, sRow2Y - 5, labelFont);
        btnExport.Click += BtnExport_Click;

        grpSearch.Controls.AddRange(new Control[]
        {
            lblSearchName, _txtSearchName,
            lblSearchCategory, _cmbSearchCategory,
            lblSearchSupplier, _cmbSearchSupplier,
            btnSearch, btnExport
        });

        // ===== DataGridView =====
        _dgvGoods = new DataGridView
        {
            Font = labelFont,
            Size = new Size(560, 310),
            Location = new Point(15, 95),
            AutoGenerateColumns = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            RowHeadersVisible = false,
            BackgroundColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle
        };
        _dgvGoods.ColumnHeadersDefaultCellStyle.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold);
        _dgvGoods.Columns.AddRange(new DataGridViewColumn[]
        {
            new DataGridViewTextBoxColumn { Name = "ColProductID", HeaderText = "商品编号", DataPropertyName = "ProductID", Width = 60 },
            new DataGridViewTextBoxColumn { Name = "ColProductName", HeaderText = "商品名称", DataPropertyName = "ProductName", Width = 110 },
            new DataGridViewTextBoxColumn { Name = "ColCategoryName", HeaderText = "类别", DataPropertyName = "CategoryName", Width = 60 },
            new DataGridViewTextBoxColumn { Name = "ColUnitPrice", HeaderText = "单价", DataPropertyName = "UnitPrice", Width = 60 },
            new DataGridViewTextBoxColumn { Name = "ColOrigin", HeaderText = "产地", DataPropertyName = "Origin", Width = 70 },
            new DataGridViewTextBoxColumn { Name = "ColProduceDate", HeaderText = "生产日期", DataPropertyName = "ProduceDate", Width = 80 },
            new DataGridViewTextBoxColumn { Name = "ColStockQty", HeaderText = "库存", DataPropertyName = "StockQty", Width = 50 },
            new DataGridViewTextBoxColumn
            {
                Name = "ColSupplierName", HeaderText = "供货商", DataPropertyName = "SupplierName", Width = 60,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
            }
        });
        _dgvGoods.SelectionChanged += DgvGoods_SelectionChanged;

        // ===== 输入区 GroupBox（竖向排列：每行一个字段，Label 右边缘对齐）=====
        GroupBox grpInput = new()
        {
            Text = "商品信息",
            Font = labelFont,
            Size = new Size(560, 305),
            Location = new Point(15, 410)
        };

        // Label 右边缘统一对齐于 X=110，输入控件统一从 X=120 开始
        const int labelRightX = 110;
        const int inputX = 120;
        const int inputWidth = 200;
        const int rowGap = 30;

        // 第1行：商品编号
        const int pRow1Y = 25;
        Label lblProductID = UiHelper.CreateLabelRightAligned("商品编号：", labelRightX, pRow1Y, labelFont);
        _txtProductID = new TextBox
        {
            Font = labelFont,
            Size = new Size(inputWidth, CtrlHeight),
            Location = new Point(inputX, pRow1Y - 3),
            ReadOnly = true,
            BackColor = Color.FromArgb(240, 240, 240)
        };

        // 第2行：商品名称
        const int pRow2Y = 25 + rowGap;
        Label lblProductName = UiHelper.CreateLabelRightAligned("商品名称：", labelRightX, pRow2Y, labelFont);
        _txtProductName = new TextBox
        {
            Font = labelFont,
            Size = new Size(inputWidth, CtrlHeight),
            Location = new Point(inputX, pRow2Y - 3)
        };

        // 第3行：类别
        const int pRow3Y = 25 + rowGap * 2;
        Label lblCategory = UiHelper.CreateLabelRightAligned("类别：", labelRightX, pRow3Y, labelFont);
        _cmbCategory = new ComboBox
        {
            Font = labelFont,
            Size = new Size(inputWidth, CtrlHeight),
            Location = new Point(inputX, pRow3Y - 3),
            DropDownStyle = ComboBoxStyle.DropDownList
        };

        // 第4行：供货商
        const int pRow4Y = 25 + rowGap * 3;
        Label lblSupplier = UiHelper.CreateLabelRightAligned("供货商：", labelRightX, pRow4Y, labelFont);
        _cmbSupplier = new ComboBox
        {
            Font = labelFont,
            Size = new Size(inputWidth, CtrlHeight),
            Location = new Point(inputX, pRow4Y - 3),
            DropDownStyle = ComboBoxStyle.DropDownList
        };

        // 第5行：单价
        const int pRow5Y = 25 + rowGap * 4;
        Label lblUnitPrice = UiHelper.CreateLabelRightAligned("单价：", labelRightX, pRow5Y, labelFont);
        _txtUnitPrice = new TextBox
        {
            Font = labelFont,
            Size = new Size(inputWidth, CtrlHeight),
            Location = new Point(inputX, pRow5Y - 3)
        };

        // 第6行：库存
        const int pRow6Y = 25 + rowGap * 5;
        Label lblStockQty = UiHelper.CreateLabelRightAligned("库存：", labelRightX, pRow6Y, labelFont);
        _txtStockQty = new TextBox
        {
            Font = labelFont,
            Size = new Size(inputWidth, CtrlHeight),
            Location = new Point(inputX, pRow6Y - 3)
        };

        // 第7行：产地
        const int pRow7Y = 25 + rowGap * 6;
        Label lblOrigin = UiHelper.CreateLabelRightAligned("产地：", labelRightX, pRow7Y, labelFont);
        _txtOrigin = new TextBox
        {
            Font = labelFont,
            Size = new Size(inputWidth, CtrlHeight),
            Location = new Point(inputX, pRow7Y - 3)
        };

        // 第8行：生产日期
        const int pRow8Y = 25 + rowGap * 7;
        Label lblProduceDate = UiHelper.CreateLabelRightAligned("生产日期：", labelRightX, pRow8Y, labelFont);
        _dtpProduceDate = new DateTimePicker
        {
            Font = labelFont,
            Size = new Size(inputWidth, CtrlHeight),
            Location = new Point(inputX, pRow8Y - 3),
            Format = DateTimePickerFormat.Short
        };

        grpInput.Controls.AddRange(new Control[]
        {
            lblProductID, _txtProductID,
            lblProductName, _txtProductName,
            lblCategory, _cmbCategory,
            lblSupplier, _cmbSupplier,
            lblUnitPrice, _txtUnitPrice,
            lblStockQty, _txtStockQty,
            lblOrigin, _txtOrigin,
            lblProduceDate, _dtpProduceDate
        });

        // ===== 操作按钮区 =====
        const int btnY = 725;
        _btnAdd = UiHelper.CreateButton("添加", 15, btnY, labelFont);
        _btnAdd.Click += BtnAdd_Click;
        _btnUpdate = UiHelper.CreateButton("修改", _btnAdd.Right + 10, btnY, labelFont);
        _btnUpdate.Click += BtnUpdate_Click;
        _btnDelete = UiHelper.CreateButton("删除", _btnUpdate.Right + 10, btnY, labelFont);
        _btnDelete.Click += BtnDelete_Click;
        _btnClear = UiHelper.CreateButton("清空", _btnDelete.Right + 10, btnY, labelFont);
        _btnClear.Click += BtnClear_Click;
        _btnReturn = UiHelper.CreateButton("返回", 480, btnY, labelFont);
        _btnReturn.BackColor = Color.FromArgb(200, 200, 200);
        _btnReturn.Click += BtnReturn_Click;

        Controls.AddRange(new Control[]
        {
            grpSearch,
            _dgvGoods,
            grpInput,
            _btnAdd, _btnUpdate, _btnDelete, _btnClear, _btnReturn
        });

        ApplyPermission();

        LoadSearchCategories();
        LoadSearchSuppliers();
        LoadInputCategories();
        LoadInputSuppliers();
        LoadGoods();
    }

    private void ApplyPermission()
    {
        bool isAdmin = _currentUser.Role == RoleConstants.ADMIN;
        _btnAdd.Enabled = isAdmin;
        _btnUpdate.Enabled = isAdmin;
        _btnDelete.Enabled = isAdmin;
    }

    private void LoadSearchCategories()
    {
        _cmbSearchCategory.Items.Clear();
        _cmbSearchCategory.Items.Add(new CategoryInfo { CategoryID = 0, CategoryName = "全部" });
        foreach (CategoryInfo c in _categoryBiz.GetAllCategories())
        {
            _cmbSearchCategory.Items.Add(c);
        }
        _cmbSearchCategory.DisplayMember = "CategoryName";
        _cmbSearchCategory.ValueMember = "CategoryID";
        _cmbSearchCategory.SelectedIndex = 0;
    }

    private void LoadSearchSuppliers()
    {
        _cmbSearchSupplier.Items.Clear();
        _cmbSearchSupplier.Items.Add(new SupplierInfo { SupplierID = 0, SupplierName = "全部" });
        foreach (SupplierInfo s in _supplierBiz.GetAllSuppliers())
        {
            _cmbSearchSupplier.Items.Add(s);
        }
        _cmbSearchSupplier.DisplayMember = "SupplierName";
        _cmbSearchSupplier.ValueMember = "SupplierID";
        _cmbSearchSupplier.SelectedIndex = 0;
    }

    private void LoadInputCategories()
    {
        _cmbCategory.Items.Clear();
        foreach (CategoryInfo c in _categoryBiz.GetAllCategories())
        {
            _cmbCategory.Items.Add(c);
        }
        _cmbCategory.DisplayMember = "CategoryName";
        _cmbCategory.ValueMember = "CategoryID";
    }

    private void LoadInputSuppliers()
    {
        _cmbSupplier.Items.Clear();
        foreach (SupplierInfo s in _supplierBiz.GetAllSuppliers())
        {
            _cmbSupplier.Items.Add(s);
        }
        _cmbSupplier.DisplayMember = "SupplierName";
        _cmbSupplier.ValueMember = "SupplierID";
    }

    private void LoadGoods()
    {
        _dgvGoods.DataSource = _goodsBiz.GetAllGoods();
    }

    private void BtnSearch_Click(object sender, EventArgs e)
    {
        try
        {
            string productName = _txtSearchName.Text.Trim();
            int categoryID = GetSelectedCategoryID(_cmbSearchCategory);
            int supplierID = GetSelectedSupplierID(_cmbSearchSupplier);
            _dgvGoods.DataSource = _goodsBiz.SearchGoods(productName, categoryID, supplierID);
        }
        catch (BusinessException ex)
        {
            MessageBox.Show(ex.Message, "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"系统错误：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void BtnExport_Click(object sender, EventArgs e)
    {
        try
        {
            if (_dgvGoods.Rows.Count == 0)
            {
                MessageBox.Show("没有数据可导出", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using SaveFileDialog sfd = new();
            sfd.Filter = "Excel 文件|*.xlsx";
            sfd.FileName = ExcelUtil.BuildFileName("商品清单");
            if (sfd.ShowDialog() != DialogResult.OK)
            {
                return;
            }

            DataTable table = BuildGoodsDataTable((List<GoodsInfo>)_dgvGoods.DataSource);
            ExcelUtil.ExportDataTable(table, sfd.FileName);
            MessageBox.Show("导出成功！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"导出失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private static DataTable BuildGoodsDataTable(List<GoodsInfo> goods)
    {
        DataTable table = new();
        table.Columns.Add("商品编号", typeof(int));
        table.Columns.Add("商品名称", typeof(string));
        table.Columns.Add("类别", typeof(string));
        table.Columns.Add("单价", typeof(decimal));
        table.Columns.Add("产地", typeof(string));
        table.Columns.Add("生产日期", typeof(string));
        table.Columns.Add("库存", typeof(int));
        table.Columns.Add("供货商", typeof(string));

        foreach (GoodsInfo g in goods)
        {
            table.Rows.Add(
                g.ProductID,
                g.ProductName ?? string.Empty,
                g.CategoryName ?? string.Empty,
                g.UnitPrice,
                g.Origin ?? string.Empty,
                g.ProduceDate?.ToString("yyyy-MM-dd") ?? string.Empty,
                g.StockQty,
                g.SupplierName ?? string.Empty
            );
        }
        return table;
    }

    private void DgvGoods_SelectionChanged(object sender, EventArgs e)
    {
        if (_dgvGoods.CurrentRow == null || _dgvGoods.CurrentRow.DataBoundItem == null)
        {
            return;
        }

        GoodsInfo goods = (GoodsInfo)_dgvGoods.CurrentRow.DataBoundItem;
        _txtProductID.Text = goods.ProductID.ToString();
        _txtProductName.Text = goods.ProductName;
        SelectComboItem<CategoryInfo>(_cmbCategory, goods.CategoryID, c => c.CategoryID);
        _txtUnitPrice.Text = goods.UnitPrice.ToString("0.00");
        _txtOrigin.Text = goods.Origin;
        _dtpProduceDate.Value = goods.ProduceDate ?? DateTime.Now;
        _txtStockQty.Text = goods.StockQty.ToString();
        SelectComboItem<SupplierInfo>(_cmbSupplier, goods.SupplierID, s => s.SupplierID);
    }

    private void BtnAdd_Click(object sender, EventArgs e)
    {
        try
        {
            GoodsInfo goods = BuildGoodsFromInput();
            _goodsBiz.AddGoods(goods);
            MessageBox.Show("添加成功", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            LoadGoods();
            ClearInput();
        }
        catch (BusinessException ex)
        {
            MessageBox.Show(ex.Message, "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"系统错误：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void BtnUpdate_Click(object sender, EventArgs e)
    {
        try
        {
            if (string.IsNullOrEmpty(_txtProductID.Text))
            {
                MessageBox.Show("请先选择要修改的商品", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            GoodsInfo goods = BuildGoodsFromInput();
            _goodsBiz.UpdateGoods(goods);
            MessageBox.Show("修改成功", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            LoadGoods();
        }
        catch (BusinessException ex)
        {
            MessageBox.Show(ex.Message, "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"系统错误：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void BtnDelete_Click(object sender, EventArgs e)
    {
        try
        {
            if (string.IsNullOrEmpty(_txtProductID.Text))
            {
                MessageBox.Show("请先选择要删除的商品", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (MessageBox.Show("确定要删除该商品吗？", "确认", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            {
                return;
            }

            int productID = int.Parse(_txtProductID.Text.Trim());
            _goodsBiz.DeleteGoods(productID);
            MessageBox.Show("删除成功", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            LoadGoods();
            ClearInput();
        }
        catch (BusinessException ex)
        {
            MessageBox.Show(ex.Message, "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"系统错误：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void BtnClear_Click(object sender, EventArgs e)
    {
        ClearInput();
    }

    private void BtnReturn_Click(object sender, EventArgs e)
    {
        Close();
    }

    private GoodsInfo BuildGoodsFromInput()
    {
        if (!decimal.TryParse(_txtUnitPrice.Text.Trim(), out decimal unitPrice))
        {
            throw new BusinessException("单价必须是有效的数字");
        }
        if (!int.TryParse(_txtStockQty.Text.Trim(), out int stockQty))
        {
            throw new BusinessException("库存数量必须是有效的整数");
        }

        int.TryParse(_txtProductID.Text.Trim(), out int productID);
        CategoryInfo selectedCategory = _cmbCategory.SelectedItem as CategoryInfo;
        SupplierInfo selectedSupplier = _cmbSupplier.SelectedItem as SupplierInfo;

        return new GoodsInfo
        {
            ProductID = productID,
            ProductName = _txtProductName.Text.Trim(),
            CategoryID = selectedCategory?.CategoryID ?? 0,
            UnitPrice = unitPrice,
            Origin = _txtOrigin.Text.Trim(),
            ProduceDate = _dtpProduceDate.Value,
            StockQty = stockQty,
            SupplierID = selectedSupplier?.SupplierID ?? 0
        };
    }

    private void ClearInput()
    {
        _txtProductID.Clear();
        _txtProductName.Clear();
        _cmbCategory.SelectedIndex = -1;
        _txtUnitPrice.Clear();
        _txtOrigin.Clear();
        _dtpProduceDate.Value = DateTime.Now;
        _txtStockQty.Clear();
        _cmbSupplier.SelectedIndex = -1;
    }

    private static int GetSelectedCategoryID(ComboBox cmb)
    {
        return cmb.SelectedItem is CategoryInfo c ? c.CategoryID : 0;
    }

    private static int GetSelectedSupplierID(ComboBox cmb)
    {
        return cmb.SelectedItem is SupplierInfo s ? s.SupplierID : 0;
    }

    private static void SelectComboItem<T>(ComboBox cmb, int id, Func<T, int> idSelector)
    {
        for (int i = 0; i < cmb.Items.Count; i++)
        {
            if (cmb.Items[i] is T item && idSelector(item) == id)
            {
                cmb.SelectedIndex = i;
                return;
            }
        }
        cmb.SelectedIndex = -1;
    }
}
