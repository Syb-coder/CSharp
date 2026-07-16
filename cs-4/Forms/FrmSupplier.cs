using System.Data;
using CampusStore.BLL;
using CampusStore.Common;
using CampusStore.Models;

namespace CampusStore.Forms;

/// <summary>
/// 供货商管理窗体
/// </summary>
/// <remarks>
/// 提供供货商的增删改查功能，支持名称关键字查询和可空注册日期录入。
/// </remarks>
public partial class FrmSupplier : Form
{
    private readonly SupplierManager _supplierManager = new();

    /// <summary>当前选中的供货商编号（0 表示未选中）</summary>
    private int _selectedSupplierID = 0;

    // === 查询区控件 ===
    private Label _lblQueryName;
    private TextBox _txtQuery;
    private Button _btnQuery;
    private Button _btnRefresh;

    // === 列表区控件 ===
    private DataGridView _dgvSupplier;

    // === 编辑区控件（第一行）===
    private Label _lblName;
    private TextBox _txtName;
    private Label _lblLegal;
    private TextBox _txtLegal;
    private Label _lblRegister;
    private DateTimePicker _dtpRegister;

    // === 编辑区控件（第二行）===
    private Label _lblContact;
    private TextBox _txtContact;
    private Label _lblPhone;
    private TextBox _txtPhone;
    private Label _lblAddress;
    private TextBox _txtAddress;

    // === 操作按钮 ===
    private Button _btnAdd;
    private Button _btnUpdate;
    private Button _btnDelete;
    private Button _btnClear;

    /// <summary>
    /// 构造函数，初始化窗体并绑定事件
    /// </summary>
    public FrmSupplier()
    {
        InitializeComponent();
        BindEvents();
    }

    /// <summary>
    /// 初始化所有控件并添加到窗体
    /// </summary>
    private void InitializeComponent()
    {
        UiHelper.SetupForm(this, "供货商管理", 900, 600);

        // === 查询区（Y=15）===
        _lblQueryName = UiHelper.CreateLabel("供货商名称：", 15, 15);
        _txtQuery = UiHelper.CreateTextBox(UiHelper.NextX(_lblQueryName), 12, 200);
        _btnQuery = UiHelper.CreatePrimaryButton("查询", _txtQuery.Location.X + _txtQuery.Width + 10, 10, 75, 30);
        _btnRefresh = UiHelper.CreateSecondaryButton("刷新", _btnQuery.Location.X + _btnQuery.Width + 10, 10, 75, 30);

        // === 列表区（Y=55，宽 860，高 320）===
        _dgvSupplier = new DataGridView
        {
            Location = new Point(15, 55),
            Size = new Size(860, 320)
        };
        UiHelper.SetGridStyle(_dgvSupplier);
        SetupGridColumns();

        // === 编辑区第一行（Y=390）：名称、法人代表、注册日期 ===
        _lblName = UiHelper.CreateLabel("名称：", 15, 390);
        _txtName = UiHelper.CreateTextBox(UiHelper.NextX(_lblName), 387, 180);
        _lblLegal = UiHelper.CreateLabel("法人代表：", _txtName.Location.X + _txtName.Width + 15, 390);
        _txtLegal = UiHelper.CreateTextBox(UiHelper.NextX(_lblLegal), 387, 150);
        _lblRegister = UiHelper.CreateLabel("注册日期：", _txtLegal.Location.X + _txtLegal.Width + 15, 390);
        _dtpRegister = new DateTimePicker
        {
            Location = new Point(UiHelper.NextX(_lblRegister), 387),
            Width = 150,
            Format = DateTimePickerFormat.Short,
            ShowCheckBox = true,
            Checked = false
        };

        // === 编辑区第二行（Y=425）：联系人、电话、地址 ===
        _lblContact = UiHelper.CreateLabel("联系人：", 15, 425);
        _txtContact = UiHelper.CreateTextBox(UiHelper.NextX(_lblContact), 422, 150);
        _lblPhone = UiHelper.CreateLabel("电话：", _txtContact.Location.X + _txtContact.Width + 15, 425);
        _txtPhone = UiHelper.CreateTextBox(UiHelper.NextX(_lblPhone), 422, 150);
        _lblAddress = UiHelper.CreateLabel("地址：", _txtPhone.Location.X + _txtPhone.Width + 15, 425);
        _txtAddress = UiHelper.CreateTextBox(UiHelper.NextX(_lblAddress), 422, 300);

        // === 操作按钮区（Y=465，横排，间距 10）===
        _btnAdd = UiHelper.CreatePrimaryButton("添加", 15, 465, 80, 30);
        _btnUpdate = UiHelper.CreatePrimaryButton("修改", _btnAdd.Location.X + _btnAdd.Width + 10, 465, 80, 30);
        _btnDelete = UiHelper.CreateSecondaryButton("删除", _btnUpdate.Location.X + _btnUpdate.Width + 10, 465, 80, 30);
        _btnClear = UiHelper.CreateSecondaryButton("清空", _btnDelete.Location.X + _btnDelete.Width + 10, 465, 80, 30);

        // 添加所有控件到窗体
        Controls.AddRange(new Control[]
        {
            _lblQueryName, _txtQuery, _btnQuery, _btnRefresh,
            _dgvSupplier,
            _lblName, _txtName, _lblLegal, _txtLegal, _lblRegister, _dtpRegister,
            _lblContact, _txtContact, _lblPhone, _txtPhone, _lblAddress, _txtAddress,
            _btnAdd, _btnUpdate, _btnDelete, _btnClear
        });
    }

    /// <summary>
    /// 配置 DataGridView 列（列标题中文化）
    /// </summary>
    private void SetupGridColumns()
    {
        _dgvSupplier.AutoGenerateColumns = false;
        _dgvSupplier.Columns.AddRange(new DataGridViewColumn[]
        {
            new DataGridViewTextBoxColumn { DataPropertyName = "supplierID", HeaderText = "供货商编号", Name = "colSupplierID" },
            new DataGridViewTextBoxColumn { DataPropertyName = "supplierName", HeaderText = "名称", Name = "colSupplierName" },
            new DataGridViewTextBoxColumn { DataPropertyName = "legalPerson", HeaderText = "法人代表", Name = "colLegalPerson" },
            new DataGridViewTextBoxColumn
            {
                DataPropertyName = "registerDate",
                HeaderText = "注册日期",
                Name = "colRegisterDate",
                DefaultCellStyle = new DataGridViewCellStyle { Format = "yyyy-MM-dd" }
            },
            new DataGridViewTextBoxColumn { DataPropertyName = "contactPerson", HeaderText = "联系人", Name = "colContactPerson" },
            new DataGridViewTextBoxColumn { DataPropertyName = "phone", HeaderText = "电话", Name = "colPhone" },
            new DataGridViewTextBoxColumn { DataPropertyName = "address", HeaderText = "地址", Name = "colAddress" }
        });
    }

    /// <summary>
    /// 绑定控件事件
    /// </summary>
    private void BindEvents()
    {
        Load += FrmSupplier_Load;
        _btnQuery.Click += BtnQuery_Click;
        _btnRefresh.Click += BtnRefresh_Click;
        _dgvSupplier.SelectionChanged += DgvSupplier_SelectionChanged;
        _btnAdd.Click += BtnAdd_Click;
        _btnUpdate.Click += BtnUpdate_Click;
        _btnDelete.Click += BtnDelete_Click;
        _btnClear.Click += BtnClear_Click;
    }

    /// <summary>
    /// 窗体加载时绑定全部数据
    /// </summary>
    private void FrmSupplier_Load(object sender, EventArgs e)
    {
        LoadData();
    }

    /// <summary>
    /// 加载全部供货商数据到列表
    /// </summary>
    private void LoadData()
    {
        try
        {
            _dgvSupplier.DataSource = _supplierManager.GetAll();
        }
        catch (Exception ex)
        {
            UiHelper.HandleException(ex, this);
        }
    }

    /// <summary>
    /// 按供货商名称关键字查询
    /// </summary>
    private void BtnQuery_Click(object sender, EventArgs e)
    {
        try
        {
            string keyword = _txtQuery.Text.Trim();
            // 关键字为空时查询全部，避免无意义检索
            _dgvSupplier.DataSource = string.IsNullOrEmpty(keyword)
                ? _supplierManager.GetAll()
                : _supplierManager.Search(keyword);
        }
        catch (Exception ex)
        {
            UiHelper.HandleException(ex, this);
        }
    }

    /// <summary>
    /// 刷新列表并清空查询条件
    /// </summary>
    private void BtnRefresh_Click(object sender, EventArgs e)
    {
        _txtQuery.Text = "";
        LoadData();
    }

    /// <summary>
    /// 选中行时将数据填入编辑区
    /// </summary>
    private void DgvSupplier_SelectionChanged(object sender, EventArgs e)
    {
        // 列顺序：0=supplierID, 1=supplierName, 2=legalPerson, 3=registerDate, 4=contactPerson, 5=phone, 6=address
        if (_dgvSupplier.SelectedRows.Count == 0)
        {
            _selectedSupplierID = 0;
            return;
        }
        DataGridViewRow row = _dgvSupplier.SelectedRows[0];
        object idValue = row.Cells[0].Value;
        if (idValue == null || idValue == DBNull.Value)
        {
            _selectedSupplierID = 0;
            return;
        }
        _selectedSupplierID = Convert.ToInt32(idValue);
        _txtName.Text = row.Cells[1].Value?.ToString() ?? "";
        _txtLegal.Text = row.Cells[2].Value?.ToString() ?? "";
        // 注册日期可空：DBNull 时取消勾选
        object dateValue = row.Cells[3].Value;
        if (dateValue != null && dateValue != DBNull.Value)
        {
            _dtpRegister.Checked = true;
            _dtpRegister.Value = Convert.ToDateTime(dateValue);
        }
        else
        {
            _dtpRegister.Checked = false;
        }
        _txtContact.Text = row.Cells[4].Value?.ToString() ?? "";
        _txtPhone.Text = row.Cells[5].Value?.ToString() ?? "";
        _txtAddress.Text = row.Cells[6].Value?.ToString() ?? "";
    }

    /// <summary>
    /// 添加供货商
    /// </summary>
    private void BtnAdd_Click(object sender, EventArgs e)
    {
        try
        {
            SupplierInfo info = BuildSupplierInfo();
            _supplierManager.Add(info);
            LoadData();
            ClearInputs();
            MessageBox.Show("添加成功", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            UiHelper.HandleException(ex, this);
        }
    }

    /// <summary>
    /// 修改供货商（需先选中行）
    /// </summary>
    private void BtnUpdate_Click(object sender, EventArgs e)
    {
        try
        {
            if (_selectedSupplierID <= 0)
                throw new BusinessException("请先选择要修改的供货商");
            SupplierInfo info = BuildSupplierInfo();
            _supplierManager.Update(info);
            LoadData();
            ClearInputs();
            MessageBox.Show("修改成功", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            UiHelper.HandleException(ex, this);
        }
    }

    /// <summary>
    /// 删除供货商（需先选中行并确认）
    /// </summary>
    private void BtnDelete_Click(object sender, EventArgs e)
    {
        try
        {
            if (_selectedSupplierID <= 0)
                throw new BusinessException("请先选择要删除的供货商");
            if (!UiHelper.ConfirmDelete(_txtName.Text, this))
                return;
            _supplierManager.Delete(_selectedSupplierID);
            LoadData();
            ClearInputs();
            MessageBox.Show("删除成功", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            UiHelper.HandleException(ex, this);
        }
    }

    /// <summary>
    /// 清空编辑区
    /// </summary>
    private void BtnClear_Click(object sender, EventArgs e)
    {
        ClearInputs();
    }

    /// <summary>
    /// 清空所有输入控件并重置选中状态
    /// </summary>
    private void ClearInputs()
    {
        _txtName.Text = "";
        _txtLegal.Text = "";
        _dtpRegister.Checked = false;
        _txtContact.Text = "";
        _txtPhone.Text = "";
        _txtAddress.Text = "";
        _selectedSupplierID = 0;
        _dgvSupplier.ClearSelection();
    }

    /// <summary>
    /// 从编辑区控件构造 SupplierInfo 对象
    /// </summary>
    /// <returns>填充好的供货商实体对象</returns>
    private SupplierInfo BuildSupplierInfo()
    {
        return new SupplierInfo
        {
            SupplierID = _selectedSupplierID,
            SupplierName = _txtName.Text.Trim(),
            LegalPerson = _txtLegal.Text.Trim(),
            // ShowCheckBox=true 时通过 Checked 判断是否取值
            RegisterDate = _dtpRegister.Checked ? _dtpRegister.Value : null,
            ContactPerson = _txtContact.Text.Trim(),
            Phone = _txtPhone.Text.Trim(),
            Address = _txtAddress.Text.Trim()
        };
    }
}
