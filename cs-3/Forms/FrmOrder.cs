using System.Data;
using CampusMart.BLL;
using CampusMart.Common;
using CampusMart.Models;

namespace CampusMart.Forms;

/// <summary>
/// 订单管理窗体：提供订单查询、订单明细查看、新建订单（购物车式）、确认支付、Excel 导出功能
/// </summary>
/// <remarks>
/// 布局分四区：顶部查询区、订单列表区、订单明细区、新建订单区（含购物车明细）。
/// 新建订单采用购物车模式：选择商品 + 数量 → 加入购物车 → 确认下单。
/// Label 宽度使用 UiHelper 动态测量，多列采用链式布局。
/// </remarks>
public class FrmOrder : Form
{
    private readonly UserInfo _currentUser;
    private readonly OrderBiz _orderBiz = new();
    private readonly GoodsBiz _goodsBiz = new();

    private readonly List<OrderItemInfo> _cartItems = new();

    private readonly TextBox _txtSearchOrderNo;
    private readonly TextBox _txtSearchReceiver;
    private readonly ComboBox _cmbSearchPaymentStatus;

    private readonly DataGridView _dgvOrders;
    private readonly DataGridView _dgvOrderItems;

    private readonly TextBox _txtReceiverName;
    private readonly TextBox _txtReceiverPhone;
    private readonly TextBox _txtReceiverAddress;
    private readonly ComboBox _cmbPaymentMethod;
    private readonly ComboBox _cmbProduct;
    private readonly TextBox _txtQuantity;
    private readonly DataGridView _dgvCart;
    private readonly Label _lblTotalAmount;

    private const int CtrlHeight = 25;

    /// <summary>
    /// 构造订单管理窗体
    /// </summary>
    /// <param name="currentUser">当前登录用户，创建订单时记录 UserID</param>
    public FrmOrder(UserInfo currentUser)
    {
        _currentUser = currentUser;

        Text = "订单管理";
        Size = new Size(1000, 700);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        BackColor = Color.FromArgb(245, 247, 250);

        Font labelFont = new("Microsoft YaHei UI", 9F);
        const int ctrlGap = 8;

        // ===== 查询区 GroupBox =====
        GroupBox grpSearch = new()
        {
            Text = "查询条件",
            Font = labelFont,
            Size = new Size(964, 55),
            Location = new Point(10, 5)
        };

        const int sMargin = 15;
        const int sRowY = 22;

        Label lblSearchOrderNo = UiHelper.CreateLabel("订单号：", sMargin, sRowY, labelFont);
        _txtSearchOrderNo = new TextBox
        {
            Font = labelFont,
            Size = new Size(140, CtrlHeight),
            Location = new Point(UiHelper.NextX(lblSearchOrderNo), sRowY - 3)
        };

        int sCol2X = _txtSearchOrderNo.Right + ctrlGap;
        Label lblSearchReceiver = UiHelper.CreateLabel("收货人：", sCol2X, sRowY, labelFont);
        _txtSearchReceiver = new TextBox
        {
            Font = labelFont,
            Size = new Size(140, CtrlHeight),
            Location = new Point(UiHelper.NextX(lblSearchReceiver), sRowY - 3)
        };

        int sCol3X = _txtSearchReceiver.Right + ctrlGap;
        Label lblSearchStatus = UiHelper.CreateLabel("支付状态：", sCol3X, sRowY, labelFont);
        _cmbSearchPaymentStatus = new ComboBox
        {
            Font = labelFont,
            Size = new Size(120, CtrlHeight),
            Location = new Point(UiHelper.NextX(lblSearchStatus), sRowY - 3),
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        _cmbSearchPaymentStatus.Items.AddRange(new object[] { "全部", PaymentConstants.UNPAID, PaymentConstants.PAID });
        _cmbSearchPaymentStatus.SelectedIndex = 0;

        Button btnSearch = UiHelper.CreateButton("查询", _cmbSearchPaymentStatus.Right + ctrlGap, sRowY - 5, labelFont);
        btnSearch.Click += BtnSearch_Click;
        Button btnExport = UiHelper.CreateButton("导出", btnSearch.Right + 10, sRowY - 5, labelFont);
        btnExport.Click += BtnExport_Click;

        grpSearch.Controls.AddRange(new Control[]
        {
            lblSearchOrderNo, _txtSearchOrderNo,
            lblSearchReceiver, _txtSearchReceiver,
            lblSearchStatus, _cmbSearchPaymentStatus,
            btnSearch, btnExport
        });

        // ===== 订单列表 DataGridView =====
        _dgvOrders = new DataGridView
        {
            Font = labelFont,
            Size = new Size(964, 180),
            Location = new Point(10, 65),
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
        _dgvOrders.ColumnHeadersDefaultCellStyle.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold);
        _dgvOrders.Columns.AddRange(new DataGridViewColumn[]
        {
            new DataGridViewTextBoxColumn { Name = "ColOrderID", HeaderText = "订单编号", DataPropertyName = "OrderID", Width = 70 },
            new DataGridViewTextBoxColumn { Name = "ColOrderNo", HeaderText = "订单号", DataPropertyName = "OrderNo", Width = 130 },
            new DataGridViewTextBoxColumn { Name = "ColReceiverName", HeaderText = "收货人", DataPropertyName = "ReceiverName", Width = 80 },
            new DataGridViewTextBoxColumn { Name = "ColReceiverPhone", HeaderText = "手机号", DataPropertyName = "ReceiverPhone", Width = 110 },
            new DataGridViewTextBoxColumn { Name = "ColPaymentMethod", HeaderText = "支付方式", DataPropertyName = "PaymentMethod", Width = 80 },
            new DataGridViewTextBoxColumn { Name = "ColPaymentStatus", HeaderText = "支付状态", DataPropertyName = "PaymentStatus", Width = 80 },
            new DataGridViewTextBoxColumn { Name = "ColTotalAmount", HeaderText = "总金额", DataPropertyName = "TotalAmount", Width = 90 },
            new DataGridViewTextBoxColumn { Name = "ColOrderDate", HeaderText = "下单日期", DataPropertyName = "OrderDate", Width = 140 },
            new DataGridViewTextBoxColumn
            {
                Name = "ColOperatorName", HeaderText = "操作员", DataPropertyName = "OperatorName", Width = 100,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
            }
        });
        _dgvOrders.SelectionChanged += DgvOrders_SelectionChanged;

        // ===== 操作按钮行 =====
        Button btnConfirmPayment = UiHelper.CreateButton("确认支付", 20, 250, labelFont);
        btnConfirmPayment.Click += BtnConfirmPayment_Click;
        Button btnNewOrder = UiHelper.CreateButton("新建订单", btnConfirmPayment.Right + 10, 250, labelFont);
        btnNewOrder.Click += BtnNewOrder_Click;
        Button btnReturn = UiHelper.CreateButton("返回", 870, 250, labelFont);
        btnReturn.BackColor = Color.FromArgb(200, 200, 200);
        btnReturn.Click += BtnReturn_Click;

        // ===== 订单明细区 =====
        Label lblOrderItems = UiHelper.CreateLabel("订单明细：", 10, 290, new Font("Microsoft YaHei UI", 9F, FontStyle.Bold));

        _dgvOrderItems = new DataGridView
        {
            Font = labelFont,
            Size = new Size(964, 95),
            Location = new Point(10, 312),
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
        _dgvOrderItems.ColumnHeadersDefaultCellStyle.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold);
        _dgvOrderItems.Columns.AddRange(new DataGridViewColumn[]
        {
            new DataGridViewTextBoxColumn { Name = "ColItemProductID", HeaderText = "商品编号", DataPropertyName = "ProductID", Width = 80 },
            new DataGridViewTextBoxColumn { Name = "ColItemProductName", HeaderText = "商品名称", DataPropertyName = "ProductName", Width = 250 },
            new DataGridViewTextBoxColumn { Name = "ColItemUnitPrice", HeaderText = "单价", DataPropertyName = "UnitPrice", Width = 100 },
            new DataGridViewTextBoxColumn { Name = "ColItemQuantity", HeaderText = "数量", DataPropertyName = "Quantity", Width = 80 },
            new DataGridViewTextBoxColumn
            {
                Name = "ColItemSubtotal", HeaderText = "小计", DataPropertyName = "Subtotal", Width = 120,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
            }
        });

        // ===== 新建订单 GroupBox =====
        GroupBox grpNewOrder = new()
        {
            Text = "新建订单",
            Font = labelFont,
            Size = new Size(964, 250),
            Location = new Point(10, 415)
        };

        const int pMargin = 20;
        const int pGap = ctrlGap;

        // 第1行：收货人、手机号、支付方式
        const int pRow1Y = 27;
        Label lblReceiverName = UiHelper.CreateLabel("收货人：", pMargin, pRow1Y, labelFont);
        _txtReceiverName = new TextBox
        {
            Font = labelFont,
            Size = new Size(140, CtrlHeight),
            Location = new Point(UiHelper.NextX(lblReceiverName), pRow1Y - 3)
        };

        int p1Col2X = _txtReceiverName.Right + pGap;
        Label lblReceiverPhone = UiHelper.CreateLabel("手机号：", p1Col2X, pRow1Y, labelFont);
        _txtReceiverPhone = new TextBox
        {
            Font = labelFont,
            Size = new Size(140, CtrlHeight),
            Location = new Point(UiHelper.NextX(lblReceiverPhone), pRow1Y - 3)
        };

        int p1Col3X = _txtReceiverPhone.Right + pGap;
        Label lblPaymentMethod = UiHelper.CreateLabel("支付方式：", p1Col3X, pRow1Y, labelFont);
        _cmbPaymentMethod = new ComboBox
        {
            Font = labelFont,
            Size = new Size(120, CtrlHeight),
            Location = new Point(UiHelper.NextX(lblPaymentMethod), pRow1Y - 3),
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        _cmbPaymentMethod.Items.AddRange(new object[]
        {
            PaymentMethodConstants.CASH, PaymentMethodConstants.WECHAT, PaymentMethodConstants.ALIPAY
        });
        _cmbPaymentMethod.SelectedIndex = 0;

        // 第2行：收货地址
        const int pRow2Y = 62;
        Label lblReceiverAddress = UiHelper.CreateLabel("收货地址：", pMargin, pRow2Y, labelFont);
        _txtReceiverAddress = new TextBox
        {
            Font = labelFont,
            Size = new Size(400, CtrlHeight),
            Location = new Point(UiHelper.NextX(lblReceiverAddress), pRow2Y - 3)
        };

        // 第3行：商品、数量、添加、移除
        const int pRow3Y = 97;
        Label lblProduct = UiHelper.CreateLabel("商品：", pMargin, pRow3Y, labelFont);
        _cmbProduct = new ComboBox
        {
            Font = labelFont,
            Size = new Size(220, CtrlHeight),
            Location = new Point(UiHelper.NextX(lblProduct), pRow3Y - 3),
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        _cmbProduct.DisplayMember = "ProductName";
        _cmbProduct.ValueMember = "ProductID";

        int p3Col2X = _cmbProduct.Right + pGap;
        Label lblQuantity = UiHelper.CreateLabel("数量：", p3Col2X, pRow3Y, labelFont);
        _txtQuantity = new TextBox
        {
            Font = labelFont,
            Size = new Size(80, CtrlHeight),
            Location = new Point(UiHelper.NextX(lblQuantity), pRow3Y - 3)
        };

        Button btnAddToOrder = UiHelper.CreateButton("添加", _txtQuantity.Right + pGap, pRow3Y - 5, labelFont);
        btnAddToOrder.Click += BtnAddToOrder_Click;
        Button btnRemoveFromCart = UiHelper.CreateButton("移除", btnAddToOrder.Right + 10, pRow3Y - 5, labelFont);
        btnRemoveFromCart.Click += BtnRemoveFromCart_Click;

        // 第4行：购物车明细 DataGridView
        _dgvCart = new DataGridView
        {
            Font = labelFont,
            Size = new Size(924, 75),
            Location = new Point(pMargin, 125),
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
        _dgvCart.ColumnHeadersDefaultCellStyle.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold);
        _dgvCart.Columns.AddRange(new DataGridViewColumn[]
        {
            new DataGridViewTextBoxColumn { Name = "ColCartProductID", HeaderText = "商品编号", DataPropertyName = "ProductID", Width = 80 },
            new DataGridViewTextBoxColumn { Name = "ColCartProductName", HeaderText = "商品名称", DataPropertyName = "ProductName", Width = 300 },
            new DataGridViewTextBoxColumn { Name = "ColCartUnitPrice", HeaderText = "单价", DataPropertyName = "UnitPrice", Width = 100 },
            new DataGridViewTextBoxColumn { Name = "ColCartQuantity", HeaderText = "数量", DataPropertyName = "Quantity", Width = 80 },
            new DataGridViewTextBoxColumn
            {
                Name = "ColCartSubtotal", HeaderText = "小计", DataPropertyName = "Subtotal", Width = 120,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
            }
        });

        // 第5行：总金额、确认下单、取消
        const int pRow5Y = 212;
        Label lblTotal = UiHelper.CreateLabel("总金额：", 500, pRow5Y, labelFont);
        _lblTotalAmount = new Label
        {
            Text = "0.00",
            Font = new Font("Microsoft YaHei UI", 10F, FontStyle.Bold),
            ForeColor = Color.FromArgb(233, 30, 99),
            AutoSize = true,
            Location = new Point(UiHelper.NextX(lblTotal), pRow5Y),
            TextAlign = ContentAlignment.MiddleLeft
        };

        Button btnConfirmOrder = UiHelper.CreateButton("确认下单", 740, pRow5Y - 5, labelFont);
        btnConfirmOrder.Click += BtnConfirmOrder_Click;
        Button btnCancelOrder = UiHelper.CreateButton("取消", 830, pRow5Y - 5, labelFont);
        btnCancelOrder.BackColor = Color.FromArgb(200, 200, 200);
        btnCancelOrder.Click += BtnCancelOrder_Click;

        grpNewOrder.Controls.AddRange(new Control[]
        {
            lblReceiverName, _txtReceiverName,
            lblReceiverPhone, _txtReceiverPhone,
            lblPaymentMethod, _cmbPaymentMethod,
            lblReceiverAddress, _txtReceiverAddress,
            lblProduct, _cmbProduct,
            lblQuantity, _txtQuantity,
            btnAddToOrder, btnRemoveFromCart,
            _dgvCart,
            lblTotal, _lblTotalAmount,
            btnConfirmOrder, btnCancelOrder
        });

        Controls.AddRange(new Control[]
        {
            grpSearch,
            _dgvOrders,
            btnConfirmPayment, btnNewOrder, btnReturn,
            lblOrderItems,
            _dgvOrderItems,
            grpNewOrder
        });

        LoadProducts();
        LoadOrders();
    }

    private void LoadProducts()
    {
        _cmbProduct.Items.Clear();
        foreach (GoodsInfo g in _goodsBiz.GetAllGoods())
        {
            _cmbProduct.Items.Add(g);
        }
        if (_cmbProduct.Items.Count > 0)
        {
            _cmbProduct.SelectedIndex = 0;
        }
    }

    private void LoadOrders()
    {
        _dgvOrders.DataSource = _orderBiz.GetAllOrders();
    }

    private void BtnSearch_Click(object sender, EventArgs e)
    {
        try
        {
            string orderNo = _txtSearchOrderNo.Text.Trim();
            string receiverName = _txtSearchReceiver.Text.Trim();
            string paymentStatus = _cmbSearchPaymentStatus.SelectedItem?.ToString();
            if (paymentStatus == "全部")
            {
                paymentStatus = string.Empty;
            }
            _dgvOrders.DataSource = _orderBiz.SearchOrders(orderNo, receiverName, paymentStatus, null, null);
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
            if (_dgvOrders.Rows.Count == 0)
            {
                MessageBox.Show("没有数据可导出", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using SaveFileDialog sfd = new();
            sfd.Filter = "Excel 文件|*.xlsx";
            sfd.FileName = ExcelUtil.BuildFileName("订单列表");
            if (sfd.ShowDialog() != DialogResult.OK)
            {
                return;
            }

            DataTable table = BuildOrderDataTable((List<OrderInfo>)_dgvOrders.DataSource);
            ExcelUtil.ExportDataTable(table, sfd.FileName);
            MessageBox.Show("导出成功！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"导出失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private static DataTable BuildOrderDataTable(List<OrderInfo> orders)
    {
        DataTable table = new();
        table.Columns.Add("订单编号", typeof(int));
        table.Columns.Add("订单号", typeof(string));
        table.Columns.Add("收货人", typeof(string));
        table.Columns.Add("手机号", typeof(string));
        table.Columns.Add("支付方式", typeof(string));
        table.Columns.Add("支付状态", typeof(string));
        table.Columns.Add("总金额", typeof(decimal));
        table.Columns.Add("下单日期", typeof(string));
        table.Columns.Add("操作员", typeof(string));

        foreach (OrderInfo o in orders)
        {
            table.Rows.Add(
                o.OrderID,
                o.OrderNo ?? string.Empty,
                o.ReceiverName ?? string.Empty,
                o.ReceiverPhone ?? string.Empty,
                o.PaymentMethod ?? string.Empty,
                o.PaymentStatus ?? string.Empty,
                o.TotalAmount,
                o.OrderDate.ToString("yyyy-MM-dd HH:mm"),
                o.OperatorName ?? string.Empty
            );
        }
        return table;
    }

    private void DgvOrders_SelectionChanged(object sender, EventArgs e)
    {
        if (_dgvOrders.CurrentRow == null || _dgvOrders.CurrentRow.DataBoundItem == null)
        {
            _dgvOrderItems.DataSource = null;
            return;
        }

        OrderInfo order = (OrderInfo)_dgvOrders.CurrentRow.DataBoundItem;
        _dgvOrderItems.DataSource = _orderBiz.GetOrderItems(order.OrderID);
    }

    private void BtnConfirmPayment_Click(object sender, EventArgs e)
    {
        try
        {
            if (_dgvOrders.CurrentRow == null || _dgvOrders.CurrentRow.DataBoundItem == null)
            {
                MessageBox.Show("请先选择订单", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            OrderInfo order = (OrderInfo)_dgvOrders.CurrentRow.DataBoundItem;
            if (order.PaymentStatus == PaymentConstants.PAID)
            {
                MessageBox.Show("该订单已支付，无需重复操作", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (MessageBox.Show("确认该订单已收款？", "确认", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            {
                return;
            }

            _orderBiz.ConfirmPayment(order.OrderID);
            MessageBox.Show("支付确认成功", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            LoadOrders();
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

    private void BtnNewOrder_Click(object sender, EventArgs e)
    {
        ClearNewOrderPanel();
        _txtReceiverName.Focus();
    }

    private void BtnReturn_Click(object sender, EventArgs e)
    {
        Close();
    }

    private void BtnAddToOrder_Click(object sender, EventArgs e)
    {
        try
        {
            GoodsInfo selectedGoods = _cmbProduct.SelectedItem as GoodsInfo;
            if (selectedGoods == null)
            {
                MessageBox.Show("请选择商品", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (!int.TryParse(_txtQuantity.Text.Trim(), out int quantity) || quantity <= 0)
            {
                MessageBox.Show("请输入有效的数量（正整数）", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            OrderItemInfo existing = _cartItems.FirstOrDefault(i => i.ProductID == selectedGoods.ProductID);
            if (existing != null)
            {
                existing.Quantity += quantity;
                existing.Subtotal = existing.UnitPrice * existing.Quantity;
            }
            else
            {
                _cartItems.Add(new OrderItemInfo
                {
                    ProductID = selectedGoods.ProductID,
                    ProductName = selectedGoods.ProductName,
                    UnitPrice = selectedGoods.UnitPrice,
                    Quantity = quantity,
                    Subtotal = selectedGoods.UnitPrice * quantity
                });
            }

            RefreshCartGrid();
            UpdateTotalAmount();
            _txtQuantity.Clear();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"系统错误：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void BtnRemoveFromCart_Click(object sender, EventArgs e)
    {
        if (_dgvCart.CurrentRow == null || _dgvCart.CurrentRow.DataBoundItem == null)
        {
            MessageBox.Show("请先在购物车中选择要移除的商品", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        OrderItemInfo item = (OrderItemInfo)_dgvCart.CurrentRow.DataBoundItem;
        _cartItems.Remove(item);
        RefreshCartGrid();
        UpdateTotalAmount();
    }

    private void BtnConfirmOrder_Click(object sender, EventArgs e)
    {
        try
        {
            OrderInfo order = new()
            {
                ReceiverName = _txtReceiverName.Text.Trim(),
                ReceiverPhone = _txtReceiverPhone.Text.Trim(),
                ReceiverAddress = _txtReceiverAddress.Text.Trim(),
                PaymentMethod = _cmbPaymentMethod.SelectedItem?.ToString(),
                UserID = _currentUser.UserID
            };

            _orderBiz.CreateOrder(order, new List<OrderItemInfo>(_cartItems));
            MessageBox.Show("下单成功！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            ClearNewOrderPanel();
            LoadOrders();
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

    private void BtnCancelOrder_Click(object sender, EventArgs e)
    {
        ClearNewOrderPanel();
    }

    private void RefreshCartGrid()
    {
        _dgvCart.DataSource = null;
        _dgvCart.DataSource = _cartItems;
    }

    private void UpdateTotalAmount()
    {
        decimal total = _cartItems.Sum(i => i.Subtotal);
        _lblTotalAmount.Text = total.ToString("0.00");
    }

    private void ClearNewOrderPanel()
    {
        _txtReceiverName.Clear();
        _txtReceiverPhone.Clear();
        _txtReceiverAddress.Clear();
        _cmbPaymentMethod.SelectedIndex = 0;
        _txtQuantity.Clear();
        _cartItems.Clear();
        RefreshCartGrid();
        UpdateTotalAmount();
    }
}
