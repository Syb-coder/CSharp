using System.Data;
using CampusStore.BLL;
using CampusStore.Common;
using CampusStore.Models;

namespace CampusStore.Forms;

/// <summary>
/// 订单管理窗体（含购物车功能）
/// </summary>
/// <remarks>
/// 左区：订单列表查询、查看明细、确认结款
/// 右区：订单明细展示 + 创建订单（购物车）
/// 所有 Label 使用 UiHelper.CreateLabel 创建（参见 cs-0/experience.md 经验一：避免文字被遮挡）。
/// </remarks>
public partial class FrmOrder : Form
{
    // ========== 业务对象 ==========
    private readonly OrderManager _orderManager = new();
    private readonly ProductManager _productManager = new();

    /// <summary>内存购物车列表</summary>
    private readonly List<OrderItemInfo> _cart = new();

    // ========== 左区控件：订单列表 ==========
    private Label lblReceiverName;
    private TextBox txtReceiverName;
    private Label lblStatus;
    private ComboBox cboStatus;
    private Button btnQueryOrders;
    private DataGridView dgvOrders;
    private Button btnViewDetail;
    private Button btnConfirm;

    // ========== 右区控件：订单详情 + 创建订单 ==========
    private GroupBox grpRightArea;

    // 订单明细
    private Label lblOrderDetail;
    private DataGridView dgvOrderDetail;

    // 创建订单
    private GroupBox grpCreateOrder;
    private Label lblProduct;
    private ComboBox cboProduct;
    private Label lblQuantity;
    private NumericUpDown nudQuantity;
    private Button btnAddToCart;
    private DataGridView dgvCart;
    private Button btnRemove;

    // 收货信息
    private Label lblReceiverNameOrder;
    private TextBox txtReceiverNameOrder;
    private Label lblPhone;
    private TextBox txtPhone;
    private Label lblAddress;
    private TextBox txtAddress;

    // 支付信息
    private Label lblPayMethod;
    private ComboBox cboPayMethod;
    private Label lblPayStatus;
    private ComboBox cboPayStatus;
    private Button btnSubmit;

    /// <summary>
    /// 构造函数
    /// </summary>
    public FrmOrder()
    {
        InitializeComponent();
        SetupGridColumns();
        LoadCombos();
        BindOrderGrid();
        BindEvents();
    }

    /// <summary>
    /// 初始化所有 UI 控件并加入窗体
    /// </summary>
    private void InitializeComponent()
    {
        UiHelper.SetupForm(this, "订单管理", 980, 700);

        // ============ 左区（X=15，宽 480）============
        // 查询区（Y=15）
        lblReceiverName = UiHelper.CreateLabel("收货人：", 15, 18);
        txtReceiverName = UiHelper.CreateTextBox(UiHelper.NextX(lblReceiverName), 15, 100);

        lblStatus = UiHelper.CreateLabel("状态：", txtReceiverName.Right + 10, 18);
        cboStatus = UiHelper.CreateComboBox(UiHelper.NextX(lblStatus), 15, 80);

        btnQueryOrders = UiHelper.CreatePrimaryButton("查询", cboStatus.Right + 10, 15);

        // 订单列表网格（Y=55，宽 480，高 400）
        dgvOrders = new DataGridView
        {
            Location = new Point(15, 55),
            Size = new Size(480, 400)
        };
        UiHelper.SetGridStyle(dgvOrders);

        // 底部按钮（Y=470）
        btnViewDetail = UiHelper.CreatePrimaryButton("查看明细", 15, 470, 90);
        btnConfirm = UiHelper.CreatePrimaryButton("确认结款", btnViewDetail.Right + 10, 470, 90);

        // ============ 右区（X=510，宽 450）============
        grpRightArea = new GroupBox
        {
            Text = "订单详情 / 创建订单",
            Location = new Point(510, 10),
            Size = new Size(450, 670)
        };

        // 右区上半：订单明细
        lblOrderDetail = UiHelper.CreateLabel("订单明细", 15, 25);
        dgvOrderDetail = new DataGridView
        {
            Location = new Point(15, 45),
            Size = new Size(420, 150)
        };
        UiHelper.SetGridStyle(dgvOrderDetail);

        // 右区下半：创建订单（内嵌 GroupBox）
        grpCreateOrder = new GroupBox
        {
            Text = "创建订单",
            Location = new Point(15, 210),
            Size = new Size(420, 440)
        };

        // 商品选择行（Y=25 相对于 grpCreateOrder）
        lblProduct = UiHelper.CreateLabel("商品：", 15, 25);
        cboProduct = UiHelper.CreateComboBox(UiHelper.NextX(lblProduct), 22, 150);

        lblQuantity = UiHelper.CreateLabel("数量：", cboProduct.Right + 10, 25);
        nudQuantity = new NumericUpDown
        {
            Location = new Point(UiHelper.NextX(lblQuantity), 22),
            Size = new Size(60, 25),
            Minimum = 1,
            Maximum = 999,
            Value = 1
        };

        btnAddToCart = UiHelper.CreatePrimaryButton("加入购物车", nudQuantity.Right + 10, 22, 90);

        // 购物车网格（Y=55）
        dgvCart = new DataGridView
        {
            Location = new Point(15, 55),
            Size = new Size(420, 120)
        };
        UiHelper.SetGridStyle(dgvCart);

        // 移除按钮（Y=185）
        btnRemove = UiHelper.CreateSecondaryButton("移除选中商品", 15, 185, 120);

        // 收货信息 - 第 1 行（Y=220）
        lblReceiverNameOrder = UiHelper.CreateLabel("收货人：", 15, 223);
        txtReceiverNameOrder = UiHelper.CreateTextBox(UiHelper.NextX(lblReceiverNameOrder), 220, 100);

        lblPhone = UiHelper.CreateLabel("手机号：", txtReceiverNameOrder.Right + 10, 223);
        txtPhone = UiHelper.CreateTextBox(UiHelper.NextX(lblPhone), 220, 120);

        // 收货信息 - 第 2 行（Y=255）
        lblAddress = UiHelper.CreateLabel("收货地址：", 15, 258);
        txtAddress = UiHelper.CreateTextBox(UiHelper.NextX(lblAddress), 255, 300);

        // 支付信息（Y=290）
        lblPayMethod = UiHelper.CreateLabel("支付方式：", 15, 293);
        cboPayMethod = UiHelper.CreateComboBox(UiHelper.NextX(lblPayMethod), 290, 100);

        lblPayStatus = UiHelper.CreateLabel("结款状态：", cboPayMethod.Right + 15, 293);
        cboPayStatus = UiHelper.CreateComboBox(UiHelper.NextX(lblPayStatus), 290, 100);

        // 提交订单按钮（Y=325）
        btnSubmit = UiHelper.CreatePrimaryButton("提交订单", 15, 325, 100);

        // ============ 组装控件层次 ============
        SuspendLayout();

        // 左区控件加入窗体
        Controls.AddRange(new Control[]
        {
            lblReceiverName, txtReceiverName, lblStatus, cboStatus,
            btnQueryOrders, dgvOrders, btnViewDetail, btnConfirm
        });

        // 右区内部控件加入 grpRightArea
        grpRightArea.Controls.AddRange(new Control[]
        {
            lblOrderDetail, dgvOrderDetail, grpCreateOrder
        });

        // 创建订单区控件加入 grpCreateOrder
        grpCreateOrder.Controls.AddRange(new Control[]
        {
            lblProduct, cboProduct, lblQuantity, nudQuantity, btnAddToCart,
            dgvCart, btnRemove,
            lblReceiverNameOrder, txtReceiverNameOrder, lblPhone, txtPhone,
            lblAddress, txtAddress,
            lblPayMethod, cboPayMethod, lblPayStatus, cboPayStatus,
            btnSubmit
        });

        // 右区 GroupBox 加入窗体
        Controls.Add(grpRightArea);

        ResumeLayout(false);
        PerformLayout();
    }

    /// <summary>
    /// 配置所有 DataGridView 的列定义
    /// </summary>
    private void SetupGridColumns()
    {
        // 订单列表（DataTable 绑定，列名 camelCase）
        dgvOrders.AutoGenerateColumns = false;
        dgvOrders.Columns.Add(new DataGridViewTextBoxColumn { Name = "orderID", DataPropertyName = "orderID", HeaderText = "订单编号" });
        dgvOrders.Columns.Add(new DataGridViewTextBoxColumn { Name = "orderDate", DataPropertyName = "orderDate", HeaderText = "下单日期", DefaultCellStyle = new DataGridViewCellStyle { Format = "yyyy-MM-dd HH:mm" } });
        dgvOrders.Columns.Add(new DataGridViewTextBoxColumn { Name = "paymentMethod", DataPropertyName = "paymentMethod", HeaderText = "支付方式" });
        dgvOrders.Columns.Add(new DataGridViewTextBoxColumn { Name = "paymentTime", DataPropertyName = "paymentTime", HeaderText = "结款时间", DefaultCellStyle = new DataGridViewCellStyle { Format = "yyyy-MM-dd HH:mm" } });
        dgvOrders.Columns.Add(new DataGridViewTextBoxColumn { Name = "paymentStatus", DataPropertyName = "paymentStatus", HeaderText = "状态" });
        dgvOrders.Columns.Add(new DataGridViewTextBoxColumn { Name = "receiverName", DataPropertyName = "receiverName", HeaderText = "收货人" });
        dgvOrders.Columns.Add(new DataGridViewTextBoxColumn { Name = "receiverPhone", DataPropertyName = "receiverPhone", HeaderText = "手机号" });
        dgvOrders.Columns.Add(new DataGridViewTextBoxColumn { Name = "totalAmount", DataPropertyName = "totalAmount", HeaderText = "总金额", DefaultCellStyle = new DataGridViewCellStyle { Format = "N2" } });

        // 订单明细（List<OrderItemInfo> 绑定，属性名 PascalCase）
        dgvOrderDetail.AutoGenerateColumns = false;
        dgvOrderDetail.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "ProductName", HeaderText = "商品名称" });
        dgvOrderDetail.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "UnitPrice", HeaderText = "单价", DefaultCellStyle = new DataGridViewCellStyle { Format = "N2" } });
        dgvOrderDetail.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Quantity", HeaderText = "数量" });
        dgvOrderDetail.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Amount", HeaderText = "小计", DefaultCellStyle = new DataGridViewCellStyle { Format = "N2" } });

        // 购物车（List<OrderItemInfo> 绑定）
        dgvCart.AutoGenerateColumns = false;
        dgvCart.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "ProductID", HeaderText = "商品编号" });
        dgvCart.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "ProductName", HeaderText = "商品名称" });
        dgvCart.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "UnitPrice", HeaderText = "单价", DefaultCellStyle = new DataGridViewCellStyle { Format = "N2" } });
        dgvCart.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Quantity", HeaderText = "数量" });
        dgvCart.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Amount", HeaderText = "小计", DefaultCellStyle = new DataGridViewCellStyle { Format = "N2" } });
    }

    /// <summary>
    /// 绑定事件
    /// </summary>
    private void BindEvents()
    {
        btnQueryOrders.Click += BtnQueryOrders_Click;
        btnViewDetail.Click += BtnViewDetail_Click;
        btnConfirm.Click += BtnConfirm_Click;
        btnAddToCart.Click += BtnAddToCart_Click;
        btnRemove.Click += BtnRemove_Click;
        btnSubmit.Click += BtnSubmit_Click;
    }

    // ========== 数据加载 ==========

    /// <summary>
    /// 加载下拉框数据
    /// </summary>
    private void LoadCombos()
    {
        // 查询区状态下拉框
        cboStatus.Items.AddRange(new object[] { "全部", "未结款", "已结款" });
        cboStatus.SelectedIndex = 0;

        // 商品下拉框（从 ProductManager.GetAllList 加载）
        List<ProductInfo> products = _productManager.GetAllList();
        cboProduct.DisplayMember = "ProductName";
        cboProduct.ValueMember = "ProductID";
        cboProduct.DataSource = products;

        // 支付方式
        cboPayMethod.Items.AddRange(new object[] { "现金", "微信", "支付宝" });
        cboPayMethod.SelectedIndex = 0;

        // 结款状态
        cboPayStatus.Items.AddRange(new object[] { "未结款", "已结款" });
        cboPayStatus.SelectedIndex = 0;
    }

    /// <summary>
    /// 绑定订单列表网格
    /// </summary>
    private void BindOrderGrid()
    {
        dgvOrders.DataSource = _orderManager.GetAll();
    }

    // ========== 左区事件处理 ==========

    /// <summary>
    /// 查询订单：按收货人 + 状态检索
    /// </summary>
    private void BtnQueryOrders_Click(object sender, EventArgs e)
    {
        try
        {
            string name = txtReceiverName.Text.Trim();
            // "全部"传空字符串，BLL 会跳过该条件
            string status = cboStatus.SelectedIndex == 0 ? "" : cboStatus.SelectedItem.ToString();
            dgvOrders.DataSource = _orderManager.Search(name, status);
        }
        catch (Exception ex)
        {
            UiHelper.HandleException(ex, this);
        }
    }

    /// <summary>
    /// 查看明细：加载选中订单的明细列表
    /// </summary>
    private void BtnViewDetail_Click(object sender, EventArgs e)
    {
        try
        {
            int orderID = GetSelectedOrderID();
            if (orderID <= 0) return;
            List<OrderItemInfo> items = _orderManager.GetItems(orderID);
            dgvOrderDetail.DataSource = items;
        }
        catch (Exception ex)
        {
            UiHelper.HandleException(ex, this);
        }
    }

    /// <summary>
    /// 确认结款
    /// </summary>
    private void BtnConfirm_Click(object sender, EventArgs e)
    {
        try
        {
            int orderID = GetSelectedOrderID();
            if (orderID <= 0) return;
            _orderManager.Confirm(orderID);
            BindOrderGrid();
            MessageBox.Show("确认结款成功", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            UiHelper.HandleException(ex, this);
        }
    }

    // ========== 右区事件处理：创建订单 ==========

    /// <summary>
    /// 加入购物车
    /// </summary>
    private void BtnAddToCart_Click(object sender, EventArgs e)
    {
        try
        {
            ProductInfo selected = cboProduct.SelectedItem as ProductInfo;
            if (selected == null)
                throw new BusinessException("请选择商品");

            int quantity = (int)nudQuantity.Value;

            // 若购物车已存在该商品，累加数量
            OrderItemInfo existing = _cart.FirstOrDefault(x => x.ProductID == selected.ProductID);
            if (existing != null)
            {
                existing.Quantity += quantity;
                existing.Amount = existing.UnitPrice * existing.Quantity;
            }
            else
            {
                _cart.Add(new OrderItemInfo
                {
                    ProductID = selected.ProductID,
                    ProductName = selected.ProductName,
                    UnitPrice = selected.UnitPrice,
                    Quantity = quantity,
                    Amount = selected.UnitPrice * quantity
                });
            }

            RefreshCart();
        }
        catch (Exception ex)
        {
            UiHelper.HandleException(ex, this);
        }
    }

    /// <summary>
    /// 移除购物车中选中的商品
    /// </summary>
    private void BtnRemove_Click(object sender, EventArgs e)
    {
        try
        {
            if (dgvCart.CurrentRow == null) return;
            OrderItemInfo selectedItem = dgvCart.CurrentRow.DataBoundItem as OrderItemInfo;
            if (selectedItem == null) return;
            _cart.Remove(selectedItem);
            RefreshCart();
        }
        catch (Exception ex)
        {
            UiHelper.HandleException(ex, this);
        }
    }

    /// <summary>
    /// 提交订单
    /// </summary>
    private void BtnSubmit_Click(object sender, EventArgs e)
    {
        try
        {
            if (_cart.Count == 0)
                throw new BusinessException("购物车为空，请先添加商品");

            OrderInfo order = new()
            {
                PaymentMethod = cboPayMethod.SelectedItem.ToString(),
                PaymentStatus = cboPayStatus.SelectedItem.ToString(),
                ReceiverName = txtReceiverNameOrder.Text.Trim(),
                ReceiverPhone = txtPhone.Text.Trim(),
                ReceiverAddress = txtAddress.Text.Trim(),
                Items = new List<OrderItemInfo>(_cart)
            };

            _orderManager.CreateOrder(order);

            // 清空购物车和表单
            _cart.Clear();
            RefreshCart();
            txtReceiverNameOrder.Clear();
            txtPhone.Clear();
            txtAddress.Clear();
            cboPayMethod.SelectedIndex = 0;
            cboPayStatus.SelectedIndex = 0;
            nudQuantity.Value = 1;

            // 刷新订单列表
            BindOrderGrid();

            MessageBox.Show("订单创建成功", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            UiHelper.HandleException(ex, this);
        }
    }

    // ========== 辅助方法 ==========

    /// <summary>
    /// 刷新购物车网格绑定
    /// </summary>
    private void RefreshCart()
    {
        dgvCart.DataSource = null;
        dgvCart.DataSource = _cart;
    }

    /// <summary>
    /// 获取当前选中订单的 ID
    /// </summary>
    /// <returns>订单 ID，未选中返回 0</returns>
    private int GetSelectedOrderID()
    {
        if (dgvOrders.CurrentRow == null)
        {
            MessageBox.Show("请先选择订单", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return 0;
        }
        DataRowView drv = dgvOrders.CurrentRow.DataBoundItem as DataRowView;
        if (drv == null) return 0;
        return Convert.ToInt32(drv["orderID"]);
    }
}
