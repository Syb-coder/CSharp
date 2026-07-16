using CampusShop.BLL;
using CampusShop.Models;

namespace CampusShop.Forms;

/// <summary>
/// 订单管理窗体：支持创建订单（购物车模式）、查看订单列表、确认支付
/// 普通用户可创建订单和查看；管理员可创建订单、查看和确认支付
/// </summary>
public class OrderForm : Form
{
    private readonly User _currentUser;
    private readonly OrderService _orderService = new();
    private readonly ProductService _productService = new();

    // ===== 查询区控件 =====
    private readonly TextBox _txtSearchReceiver;
    private readonly ComboBox _cboSearchStatus;
    private readonly Button _btnSearch;

    // ===== 订单列表区 =====
    private readonly DataGridView _dgvOrders;
    private readonly DataGridView _dgvOrderItems;

    // ===== 购物车区 =====
    private readonly Panel _grpCart;
    private readonly ComboBox _cboProduct;
    private readonly TextBox _txtQuantity;
    private readonly Button _btnAddToCart;
    private readonly DataGridView _dgvCart;
    private readonly Button _btnRemoveFromCart;

    // ===== 收货信息区 =====
    private readonly TextBox _txtReceiverName;
    private readonly TextBox _txtReceiverPhone;
    private readonly TextBox _txtReceiverAddress;
    private readonly ComboBox _cboPaymentMethod;
    private readonly ComboBox _cboPaymentStatus;

    // ===== 操作按钮 =====
    private readonly Button _btnCreateOrder;
    private readonly Button _btnConfirmPayment;
    private readonly Button _btnClearCart;
    private readonly Button _btnBack;

    // 购物车临时数据：商品编号 -> (商品名称, 单价, 数量)
    private readonly List<OrderItem> _cartItems = new();

    /// <summary>
    /// 构造订单管理窗体
    /// </summary>
    /// <param name="currentUser">当前登录用户</param>
    public OrderForm(User currentUser)
    {
        _currentUser = currentUser;

        Text = "订单管理";
        Size = new Size(1000, 720);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        BackColor = Color.FromArgb(245, 247, 250);

        // ===== 查询区 =====
        Label lblSearchReceiver = new()
        {
            Text = "收货人：",
            Font = new Font("Microsoft YaHei UI", 9F),
            Location = new Point(15, 18),
            BackColor = Color.Transparent,
            AutoSize = false,
            Size = new Size(85, 20),
            TextAlign = ContentAlignment.MiddleLeft
        };
        _txtSearchReceiver = new TextBox
        {
            Font = new Font("Microsoft YaHei UI", 9F),
            Location = new Point(105, 15),
            Size = new Size(120, 25)
        };

        Label lblSearchStatus = new()
        {
            Text = "支付状态：",
            Font = new Font("Microsoft YaHei UI", 9F),
            Location = new Point(235, 18),
            BackColor = Color.Transparent,
            AutoSize = false,
            Size = new Size(100, 20),
            TextAlign = ContentAlignment.MiddleLeft
        };
        _cboSearchStatus = new ComboBox
        {
            Font = new Font("Microsoft YaHei UI", 9F),
            Location = new Point(340, 15),
            Size = new Size(100, 25),
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        _cboSearchStatus.Items.AddRange(new object[] { "全部", "待支付", "已支付" });
        _cboSearchStatus.SelectedIndex = 0;

        _btnSearch = CreateButton("查询", 450, 13);
        _btnSearch.Click += (s, e) => LoadOrders();

        // ===== 订单列表区 =====
        Label lblOrderList = new()
        {
            Text = "订单列表",
            Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold),
            ForeColor = Color.FromArgb(64, 158, 255),
            Location = new Point(15, 45),
            AutoSize = true,
            BackColor = Color.Transparent
        };
        _dgvOrders = new DataGridView
        {
            Location = new Point(15, 70),
            Size = new Size(954, 180),
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            ReadOnly = true,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None,
            BackgroundColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            RowHeadersVisible = false
        };
        _dgvOrders.SelectionChanged += DgvOrders_SelectionChanged;

        // ===== 订单明细区 =====
        Label lblItems = new()
        {
            Text = "订单明细",
            Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold),
            ForeColor = Color.FromArgb(64, 158, 255),
            Location = new Point(15, 255),
            AutoSize = true,
            BackColor = Color.Transparent
        };
        _dgvOrderItems = new DataGridView
        {
            Location = new Point(15, 280),
            Size = new Size(954, 100),
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            ReadOnly = true,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None,
            BackgroundColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            RowHeadersVisible = false
        };

        // ===== 购物车区 =====
        _grpCart = new Panel
        {
            BackColor = Color.FromArgb(245, 247, 250),
            BorderStyle = BorderStyle.FixedSingle,
            Location = new Point(15, 390),
            Size = new Size(954, 240)
        };
        Label lblCartTitle = new()
        {
            Text = "创建新订单",
            Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold),
            ForeColor = Color.FromArgb(64, 158, 255),
            Location = new Point(10, 5),
            AutoSize = true,
            BackColor = Color.Transparent
        };

        // 商品选择行
        Label lblProduct = new() { Text = "商品：", Font = new Font("Microsoft YaHei UI", 9F), Location = new Point(15, 33), BackColor = Color.Transparent, AutoSize = false, Size = new Size(65, 20), TextAlign = ContentAlignment.MiddleLeft };
        _cboProduct = new ComboBox
        {
            Font = new Font("Microsoft YaHei UI", 9F),
            Location = new Point(85, 30),
            Size = new Size(250, 25),
            DropDownStyle = ComboBoxStyle.DropDownList
        };

        Label lblQty = new() { Text = "数量：", Font = new Font("Microsoft YaHei UI", 9F), Location = new Point(345, 33), BackColor = Color.Transparent, AutoSize = false, Size = new Size(65, 20), TextAlign = ContentAlignment.MiddleLeft };
        _txtQuantity = new TextBox { Font = new Font("Microsoft YaHei UI", 9F), Location = new Point(415, 30), Size = new Size(60, 25) };

        _btnAddToCart = CreateButton("加入购物车", 485, 28);
        _btnAddToCart.Size = new Size(100, 28);
        _btnAddToCart.Click += BtnAddToCart_Click;

        _btnRemoveFromCart = CreateButton("移除选中", 595, 28);
        _btnRemoveFromCart.Size = new Size(90, 28);
        _btnRemoveFromCart.Click += BtnRemoveFromCart_Click;

        // 购物车列表
        _dgvCart = new DataGridView
        {
            Location = new Point(15, 65),
            Size = new Size(924, 60),
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            ReadOnly = true,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None,
            BackgroundColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            RowHeadersVisible = false
        };

        // 收货信息行
        Label lblReceiver = new() { Text = "收货人：", Font = new Font("Microsoft YaHei UI", 9F), Location = new Point(15, 135), BackColor = Color.Transparent, AutoSize = false, Size = new Size(85, 20), TextAlign = ContentAlignment.MiddleLeft };
        _txtReceiverName = new TextBox { Font = new Font("Microsoft YaHei UI", 9F), Location = new Point(105, 132), Size = new Size(100, 25) };

        Label lblPhone = new() { Text = "手机号：", Font = new Font("Microsoft YaHei UI", 9F), Location = new Point(215, 135), BackColor = Color.Transparent, AutoSize = false, Size = new Size(85, 20), TextAlign = ContentAlignment.MiddleLeft };
        _txtReceiverPhone = new TextBox { Font = new Font("Microsoft YaHei UI", 9F), Location = new Point(305, 132), Size = new Size(120, 25) };

        Label lblAddr = new() { Text = "收货地址：", Font = new Font("Microsoft YaHei UI", 9F), Location = new Point(435, 135), BackColor = Color.Transparent, AutoSize = false, Size = new Size(100, 20), TextAlign = ContentAlignment.MiddleLeft };
        _txtReceiverAddress = new TextBox { Font = new Font("Microsoft YaHei UI", 9F), Location = new Point(540, 132), Size = new Size(200, 25) };

        Label lblPayMethod = new() { Text = "支付方式：", Font = new Font("Microsoft YaHei UI", 9F), Location = new Point(15, 170), BackColor = Color.Transparent, AutoSize = false, Size = new Size(100, 20), TextAlign = ContentAlignment.MiddleLeft };
        _cboPaymentMethod = new ComboBox
        {
            Font = new Font("Microsoft YaHei UI", 9F),
            Location = new Point(120, 167),
            Size = new Size(100, 25),
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        _cboPaymentMethod.Items.AddRange(new object[] { "现金", "微信", "支付宝" });
        _cboPaymentMethod.SelectedIndex = 0;

        Label lblPayStatus = new() { Text = "支付状态：", Font = new Font("Microsoft YaHei UI", 9F), Location = new Point(230, 170), BackColor = Color.Transparent, AutoSize = false, Size = new Size(100, 20), TextAlign = ContentAlignment.MiddleLeft };
        _cboPaymentStatus = new ComboBox
        {
            Font = new Font("Microsoft YaHei UI", 9F),
            Location = new Point(335, 167),
            Size = new Size(100, 25),
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        _cboPaymentStatus.Items.AddRange(new object[] { "待支付", "已支付" });
        _cboPaymentStatus.SelectedIndex = 0;

        // 操作按钮
        _btnCreateOrder = CreateButton("提交订单", 450, 167);
        _btnCreateOrder.Size = new Size(100, 28);
        _btnCreateOrder.Click += BtnCreateOrder_Click;

        _btnConfirmPayment = CreateButton("确认支付", 560, 167);
        _btnConfirmPayment.Size = new Size(100, 28);
        _btnConfirmPayment.Click += BtnConfirmPayment_Click;

        _btnClearCart = CreateButton("清空购物车", 670, 167);
        _btnClearCart.Size = new Size(110, 28);
        _btnClearCart.Click += (s, e) => ClearCart();

        _grpCart.Controls.AddRange(new Control[]
        {
            lblCartTitle,
            lblProduct, _cboProduct,
            lblQty, _txtQuantity,
            _btnAddToCart, _btnRemoveFromCart,
            _dgvCart,
            lblReceiver, _txtReceiverName,
            lblPhone, _txtReceiverPhone,
            lblAddr, _txtReceiverAddress,
            lblPayMethod, _cboPaymentMethod,
            lblPayStatus, _cboPaymentStatus,
            _btnCreateOrder, _btnConfirmPayment, _btnClearCart
        });

        // 返回按钮
        _btnBack = CreateButton("返回", 890, 640);
        _btnBack.Click += (s, e) => Close();

        Controls.AddRange(new Control[]
        {
            lblSearchReceiver, _txtSearchReceiver,
            lblSearchStatus, _cboSearchStatus,
            _btnSearch,
            lblOrderList, _dgvOrders,
            lblItems, _dgvOrderItems,
            _grpCart,
            _btnBack
        });

        Load += (s, e) =>
        {
            LoadProducts();
            LoadOrders();
        };
        ApplyPermission();
    }

    /// <summary>
    /// 加载商品列表到购物车下拉框
    /// </summary>
    private void LoadProducts()
    {
        try
        {
            List<Product> products = _productService.GetAllProducts();
            _cboProduct.DisplayMember = nameof(Product.ProductName);
            _cboProduct.ValueMember = nameof(Product.ProductID);
            _cboProduct.DataSource = products;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"加载商品列表失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    /// <summary>
    /// 加载订单列表
    /// </summary>
    private void LoadOrders()
    {
        try
        {
            string receiver = _txtSearchReceiver.Text.Trim();
            string status = _cboSearchStatus.SelectedItem?.ToString() ?? "全部";
            if (status == "全部")
            {
                status = "";
            }

            List<Order> orders = _orderService.SearchOrders(receiver, status, null, null);
            _dgvOrders.DataSource = orders;
            SetOrderColumnHeaders();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"加载订单失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    /// <summary>
    /// 设置订单列表的列标题和宽度
    /// </summary>
    private void SetOrderColumnHeaders()
    {
        // 隐藏明细列表列（Items 属性不直接在 DGV 中展示）
        if (_dgvOrders.Columns.Contains(nameof(Order.Items)))
        {
            _dgvOrders.Columns[nameof(Order.Items)].Visible = false;
        }

        SetOrderColumn(nameof(Order.OrderID), "订单编号", 80);
        SetOrderColumn(nameof(Order.OrderDate), "下单日期", 100);
        SetOrderColumn(nameof(Order.PaymentMethod), "支付方式", 80);
        SetOrderColumn(nameof(Order.PaymentTime), "支付时间", 140);
        SetOrderColumn(nameof(Order.PaymentStatus), "支付状态", 80);
        SetOrderColumn(nameof(Order.ReceiverName), "收货人", 80);
        SetOrderColumn(nameof(Order.ReceiverPhone), "手机号", 120);
        SetOrderColumn(nameof(Order.ReceiverAddress), "收货地址", 200);
        SetOrderColumn(nameof(Order.TotalAmount), "总金额", 80);

        if (_dgvOrders.Columns.Contains(nameof(Order.TotalAmount)))
        {
            _dgvOrders.Columns[nameof(Order.TotalAmount)].DefaultCellStyle.Format = "0.00";
        }
        if (_dgvOrders.Columns.Contains(nameof(Order.OrderDate)))
        {
            _dgvOrders.Columns[nameof(Order.OrderDate)].DefaultCellStyle.Format = "yyyy-MM-dd";
        }
        if (_dgvOrders.Columns.Contains(nameof(Order.PaymentTime)))
        {
            _dgvOrders.Columns[nameof(Order.PaymentTime)].DefaultCellStyle.Format = "yyyy-MM-dd HH:mm";
        }
    }

    private void SetOrderColumn(string propName, string headerText, int width)
    {
        if (!_dgvOrders.Columns.Contains(propName))
        {
            return;
        }
        _dgvOrders.Columns[propName].HeaderText = headerText;
        _dgvOrders.Columns[propName].Width = width;
    }

    /// <summary>
    /// 订单列表选中行变化时加载明细
    /// </summary>
    private void DgvOrders_SelectionChanged(object sender, EventArgs e)
    {
        if (_dgvOrders.CurrentRow?.DataBoundItem is not Order order)
        {
            return;
        }
        _dgvOrderItems.DataSource = order.Items;
        SetOrderItemColumnHeaders();
    }

    /// <summary>
    /// 设置订单明细列表的列标题
    /// </summary>
    private void SetOrderItemColumnHeaders()
    {
        SetItemColumn(nameof(OrderItem.ItemID), "明细编号", 80);
        SetItemColumn(nameof(OrderItem.OrderID), "订单编号", 80);
        SetItemColumn(nameof(OrderItem.ProductID), "商品编号", 100);
        SetItemColumn(nameof(OrderItem.ProductName), "商品名称", 250);
        SetItemColumn(nameof(OrderItem.UnitPrice), "单价", 100);
        SetItemColumn(nameof(OrderItem.Quantity), "数量", 80);
        SetItemColumn(nameof(OrderItem.TotalPrice), "总价", 100);

        if (_dgvOrderItems.Columns.Contains(nameof(OrderItem.UnitPrice)))
        {
            _dgvOrderItems.Columns[nameof(OrderItem.UnitPrice)].DefaultCellStyle.Format = "0.00";
        }
        if (_dgvOrderItems.Columns.Contains(nameof(OrderItem.TotalPrice)))
        {
            _dgvOrderItems.Columns[nameof(OrderItem.TotalPrice)].DefaultCellStyle.Format = "0.00";
        }
    }

    private void SetItemColumn(string propName, string headerText, int width)
    {
        if (!_dgvOrderItems.Columns.Contains(propName))
        {
            return;
        }
        _dgvOrderItems.Columns[propName].HeaderText = headerText;
        _dgvOrderItems.Columns[propName].Width = width;
    }

    /// <summary>
    /// 加入购物车
    /// </summary>
    private void BtnAddToCart_Click(object sender, EventArgs e)
    {
        if (_cboProduct.SelectedValue == null)
        {
            MessageBox.Show("请选择商品", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        if (!int.TryParse(_txtQuantity.Text.Trim(), out int quantity) || quantity <= 0)
        {
            MessageBox.Show("数量必须为正整数", "校验失败", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        string productID = _cboProduct.SelectedValue.ToString()!;
        Product product = _productService.GetProductByID(productID);
        if (product == null)
        {
            MessageBox.Show("商品不存在", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }
        if (quantity > product.StockQuantity)
        {
            MessageBox.Show($"库存不足，当前库存仅剩 {product.StockQuantity} 件", "校验失败", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        // 如果购物车中已有同款商品，累加数量
        OrderItem existing = _cartItems.FirstOrDefault(i => i.ProductID == productID);
        if (existing != null)
        {
            int newQty = existing.Quantity + quantity;
            if (newQty > product.StockQuantity)
            {
                MessageBox.Show($"购物车中已有 {existing.Quantity} 件，加上 {quantity} 件将超过库存 {product.StockQuantity} 件", "校验失败", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            existing.Quantity = newQty;
            existing.TotalPrice = newQty * existing.UnitPrice;
        }
        else
        {
            _cartItems.Add(new OrderItem
            {
                ProductID = productID,
                ProductName = product.ProductName,
                UnitPrice = product.UnitPrice,
                Quantity = quantity,
                TotalPrice = quantity * product.UnitPrice
            });
        }

        RefreshCart();
        _txtQuantity.Clear();
        _cboProduct.Focus();
    }

    /// <summary>
    /// 从购物车移除选中商品
    /// </summary>
    private void BtnRemoveFromCart_Click(object sender, EventArgs e)
    {
        if (_dgvCart.CurrentRow?.DataBoundItem is not OrderItem item)
        {
            MessageBox.Show("请先在购物车中选择要移除的商品", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        _cartItems.Remove(item);
        RefreshCart();
    }

    /// <summary>
    /// 刷新购物车列表显示
    /// </summary>
    private void RefreshCart()
    {
        _dgvCart.DataSource = null;
        _dgvCart.DataSource = _cartItems;
        // 设置列标题
        if (_dgvCart.Columns.Contains(nameof(OrderItem.ProductID)))
        {
            _dgvCart.Columns[nameof(OrderItem.ProductID)].HeaderText = "商品编号";
            _dgvCart.Columns[nameof(OrderItem.ProductID)].Width = 120;
        }
        if (_dgvCart.Columns.Contains(nameof(OrderItem.ProductName)))
        {
            _dgvCart.Columns[nameof(OrderItem.ProductName)].HeaderText = "商品名称";
            _dgvCart.Columns[nameof(OrderItem.ProductName)].Width = 300;
        }
        if (_dgvCart.Columns.Contains(nameof(OrderItem.UnitPrice)))
        {
            _dgvCart.Columns[nameof(OrderItem.UnitPrice)].HeaderText = "单价";
            _dgvCart.Columns[nameof(OrderItem.UnitPrice)].Width = 100;
            _dgvCart.Columns[nameof(OrderItem.UnitPrice)].DefaultCellStyle.Format = "0.00";
        }
        if (_dgvCart.Columns.Contains(nameof(OrderItem.Quantity)))
        {
            _dgvCart.Columns[nameof(OrderItem.Quantity)].HeaderText = "数量";
            _dgvCart.Columns[nameof(OrderItem.Quantity)].Width = 80;
        }
        if (_dgvCart.Columns.Contains(nameof(OrderItem.TotalPrice)))
        {
            _dgvCart.Columns[nameof(OrderItem.TotalPrice)].HeaderText = "总价";
            _dgvCart.Columns[nameof(OrderItem.TotalPrice)].Width = 100;
            _dgvCart.Columns[nameof(OrderItem.TotalPrice)].DefaultCellStyle.Format = "0.00";
        }
        // 隐藏不需要的列
        if (_dgvCart.Columns.Contains(nameof(OrderItem.ItemID)))
        {
            _dgvCart.Columns[nameof(OrderItem.ItemID)].Visible = false;
        }
        if (_dgvCart.Columns.Contains(nameof(OrderItem.OrderID)))
        {
            _dgvCart.Columns[nameof(OrderItem.OrderID)].Visible = false;
        }
    }

    /// <summary>
    /// 提交订单
    /// </summary>
    private void BtnCreateOrder_Click(object sender, EventArgs e)
    {
        if (_cartItems.Count == 0)
        {
            MessageBox.Show("购物车为空，请先添加商品", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        try
        {
            Order order = new()
            {
                ReceiverName = _txtReceiverName.Text.Trim(),
                ReceiverPhone = _txtReceiverPhone.Text.Trim(),
                ReceiverAddress = _txtReceiverAddress.Text.Trim(),
                PaymentMethod = _cboPaymentMethod.SelectedItem?.ToString() ?? "",
                PaymentStatus = _cboPaymentStatus.SelectedItem?.ToString() ?? "待支付",
                Items = _cartItems.Select(i => new OrderItem
                {
                    ProductID = i.ProductID,
                    Quantity = i.Quantity
                }).ToList()
            };

            _orderService.CreateOrder(order);
            MessageBox.Show($"订单创建成功！订单编号：{order.OrderID}", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
            ClearCart();
            LoadOrders();
        }
        catch (BusinessException ex)
        {
            MessageBox.Show(ex.Message, "校验失败", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"创建订单失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    /// <summary>
    /// 确认支付：将选中订单的状态更新为"已支付"
    /// </summary>
    private void BtnConfirmPayment_Click(object sender, EventArgs e)
    {
        if (_dgvOrders.CurrentRow?.DataBoundItem is not Order order)
        {
            MessageBox.Show("请先选择要确认支付的订单", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        try
        {
            _orderService.ConfirmPayment(order.OrderID);
            MessageBox.Show("支付确认成功", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            LoadOrders();
        }
        catch (BusinessException ex)
        {
            MessageBox.Show(ex.Message, "校验失败", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"确认支付失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    /// <summary>
    /// 清空购物车和收货信息
    /// </summary>
    private void ClearCart()
    {
        _cartItems.Clear();
        RefreshCart();
        _txtReceiverName.Clear();
        _txtReceiverPhone.Clear();
        _txtReceiverAddress.Clear();
        _txtQuantity.Clear();
        _cboPaymentMethod.SelectedIndex = 0;
        _cboPaymentStatus.SelectedIndex = 0;
        if (_cboProduct.Items.Count > 0)
        {
            _cboProduct.SelectedIndex = 0;
        }
    }

    /// <summary>
    /// 根据权限启用/禁用操作按钮
    /// </summary>
    private void ApplyPermission()
    {
        bool isAdmin = _currentUser.UserPurview == BusinessConstants.ROLE_ADMIN;
        // 确认支付仅管理员可用
        _btnConfirmPayment.Enabled = isAdmin;
        if (!isAdmin)
        {
            _btnConfirmPayment.BackColor = Color.FromArgb(200, 200, 200);
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
