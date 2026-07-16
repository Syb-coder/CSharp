using System.Data;
using CampusStore.BLL;
using CampusStore.Common;

namespace CampusStore.Forms;

/// <summary>
/// 库存预警窗体（只读面板）
/// </summary>
/// <remarks>
/// 查询并展示库存低于 20 件的商品列表，按库存量升序排列，供管理员及时补货参考。
/// 此窗体不支持修改操作。
/// 所有 Label 使用 UiHelper.CreateLabel 创建（参见 cs-0/experience.md 经验一：避免文字被遮挡）。
/// </remarks>
public partial class FrmLowStock : Form
{
    /// <summary>库存预警阈值（低于此值视为需要补货）</summary>
    private const int LOW_STOCK_THRESHOLD = 20;

    // ========== 业务对象 ==========
    private readonly ProductManager _productManager = new();

    // ========== UI 控件 ==========
    private Label lblTitle;
    private DataGridView dgvLowStock;
    private Button btnRefresh;
    private Button btnClose;

    /// <summary>
    /// 构造函数
    /// </summary>
    public FrmLowStock()
    {
        InitializeComponent();
        SetupGridColumns();
        BindGrid();
        BindEvents();
    }

    /// <summary>
    /// 初始化所有 UI 控件并加入窗体
    /// </summary>
    private void InitializeComponent()
    {
        UiHelper.SetupForm(this, "库存预警 - 低于 20 件商品", 800, 500);

        // 顶部提示标签（Y=15）
        lblTitle = UiHelper.CreateLabel("以下商品库存低于预警阈值（20 件），请及时补货：", 15, 18);

        // 数据网格（Y=50，宽 760，高 380）
        dgvLowStock = new DataGridView
        {
            Location = new Point(15, 50),
            Size = new Size(760, 380)
        };
        UiHelper.SetGridStyle(dgvLowStock);

        // 底部按钮（Y=450）
        btnRefresh = UiHelper.CreatePrimaryButton("刷新", 15, 450);
        btnClose = UiHelper.CreateSecondaryButton("关闭", btnRefresh.Right + 10, 450);

        // 添加所有控件到窗体
        SuspendLayout();
        Controls.AddRange(new Control[] { lblTitle, dgvLowStock, btnRefresh, btnClose });
        ResumeLayout(false);
        PerformLayout();
    }

    /// <summary>
    /// 配置数据网格列定义（手动定义列以使用中文标题）
    /// </summary>
    private void SetupGridColumns()
    {
        dgvLowStock.AutoGenerateColumns = false;
        dgvLowStock.Columns.Add(new DataGridViewTextBoxColumn { Name = "productID", DataPropertyName = "productID", HeaderText = "商品编号" });
        dgvLowStock.Columns.Add(new DataGridViewTextBoxColumn { Name = "displayNo", DataPropertyName = "displayNo", HeaderText = "可读编号" });
        dgvLowStock.Columns.Add(new DataGridViewTextBoxColumn { Name = "productName", DataPropertyName = "productName", HeaderText = "商品名称" });
        dgvLowStock.Columns.Add(new DataGridViewTextBoxColumn { Name = "stockQuantity", DataPropertyName = "stockQuantity", HeaderText = "当前库存" });
        dgvLowStock.Columns.Add(new DataGridViewTextBoxColumn { Name = "categoryName", DataPropertyName = "categoryName", HeaderText = "类别名称" });
        dgvLowStock.Columns.Add(new DataGridViewTextBoxColumn { Name = "supplierName", DataPropertyName = "supplierName", HeaderText = "供货商名称" });
        dgvLowStock.Columns.Add(new DataGridViewTextBoxColumn { Name = "origin", DataPropertyName = "origin", HeaderText = "产地" });
    }

    /// <summary>
    /// 绑定事件
    /// </summary>
    private void BindEvents()
    {
        btnRefresh.Click += BtnRefresh_Click;
        btnClose.Click += BtnClose_Click;
    }

    /// <summary>
    /// 加载库存预警数据
    /// </summary>
    /// <remarks>
    /// 通过 ProductManager.GetAll() 获取全部商品，然后筛选 stockQuantity &lt; 20 的行，
    /// 按库存量升序排列。
    /// </remarks>
    private void BindGrid()
    {
        DataTable allProducts = _productManager.GetAll();
        DataView dv = allProducts.DefaultView;
        dv.RowFilter = $"stockQuantity < {LOW_STOCK_THRESHOLD}";
        dv.Sort = "stockQuantity ASC";
        dgvLowStock.DataSource = dv.ToTable();
    }

    /// <summary>
    /// 刷新按钮：重新加载预警数据
    /// </summary>
    private void BtnRefresh_Click(object sender, EventArgs e)
    {
        try
        {
            BindGrid();
        }
        catch (Exception ex)
        {
            UiHelper.HandleException(ex, this);
        }
    }

    /// <summary>
    /// 关闭按钮
    /// </summary>
    private void BtnClose_Click(object sender, EventArgs e)
    {
        Close();
    }
}
