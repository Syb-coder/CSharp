using CampusShop.BLL;
using CampusShop.Models;

namespace CampusShop.Forms;

/// <summary>
/// 供货商管理窗体：供货商的查询、新增、修改、删除
/// 普通用户仅可查询；管理员可增删改查
/// </summary>
public class SupplierForm : Form
{
    private readonly User _currentUser;
    private readonly SupplierService _supplierService = new();

    // 查询区控件
    private readonly TextBox _txtSearchID;
    private readonly TextBox _txtSearchName;
    private readonly Button _btnSearch;

    // 列表控件
    private readonly DataGridView _dgvSuppliers;

    // 输入区控件
    private readonly Panel _grpInput;
    private readonly TextBox _txtSupplierID;
    private readonly TextBox _txtSupplierName;
    private readonly TextBox _txtContactPerson;
    private readonly TextBox _txtPhone;
    private readonly TextBox _txtAddress;
    private readonly TextBox _txtLegalPerson;
    private readonly DateTimePicker _dtpRegisterDate;

    // 操作按钮
    private readonly Button _btnAdd;
    private readonly Button _btnUpdate;
    private readonly Button _btnDelete;
    private readonly Button _btnClear;
    private readonly Button _btnBack;

    /// <summary>
    /// 构造供货商管理窗体
    /// </summary>
    /// <param name="currentUser">当前登录用户，用于权限控制</param>
    public SupplierForm(User currentUser)
    {
        _currentUser = currentUser;

        // 窗体基本属性
        Text = "供货商管理";
        Size = new Size(900, 630);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        BackColor = Color.FromArgb(245, 247, 250);

        // ===== 查询区：Label 宽度=AutoSize实际值（5字+冒号=100px） =====
        Label lblSearchID = new()
        {
            Text = "供货商编号：",
            Font = new Font("Microsoft YaHei UI", 9F),
            Location = new Point(15, 18),
            BackColor = Color.Transparent,
            AutoSize = false,
            Size = new Size(100, 20),
            TextAlign = ContentAlignment.MiddleLeft
        };
        _txtSearchID = new TextBox
        {
            Font = new Font("Microsoft YaHei UI", 9F),
            Location = new Point(120, 15),
            Size = new Size(130, 25)
        };

        Label lblSearchName = new()
        {
            Text = "供货商名称：",
            Font = new Font("Microsoft YaHei UI", 9F),
            Location = new Point(260, 18),
            BackColor = Color.Transparent,
            AutoSize = false,
            Size = new Size(100, 20),
            TextAlign = ContentAlignment.MiddleLeft
        };
        _txtSearchName = new TextBox
        {
            Font = new Font("Microsoft YaHei UI", 9F),
            Location = new Point(365, 15),
            Size = new Size(150, 25)
        };

        _btnSearch = CreateButton("查询", 525, 13);
        _btnSearch.Click += (s, e) => LoadData();

        // ===== 列表区：宽度适配 ClientSize 884（左 15 + 宽 854 + 右 15） =====
        _dgvSuppliers = new DataGridView
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
        _dgvSuppliers.SelectionChanged += DgvSuppliers_SelectionChanged;

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
            Text = "供货商信息",
            Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold),
            ForeColor = Color.FromArgb(64, 158, 255),
            BackColor = Color.Transparent,
            Location = new Point(10, 5),
            AutoSize = true
        };

        // 第一行：供货商编号(100)、供货商名称(100)、联系人(85) —— Label 宽度=AutoSize实际值
        Label lblID = new() { Text = "供货商编号：", Font = new Font("Microsoft YaHei UI", 9F), Location = new Point(15, 33), BackColor = Color.Transparent, AutoSize = false, Size = new Size(100, 20), TextAlign = ContentAlignment.MiddleLeft };
        _txtSupplierID = new TextBox { Font = new Font("Microsoft YaHei UI", 9F), Location = new Point(120, 30), Size = new Size(130, 25) };

        Label lblName = new() { Text = "供货商名称：", Font = new Font("Microsoft YaHei UI", 9F), Location = new Point(260, 33), BackColor = Color.Transparent, AutoSize = false, Size = new Size(100, 20), TextAlign = ContentAlignment.MiddleLeft };
        _txtSupplierName = new TextBox { Font = new Font("Microsoft YaHei UI", 9F), Location = new Point(365, 30), Size = new Size(180, 25) };

        Label lblContact = new() { Text = "联系人：", Font = new Font("Microsoft YaHei UI", 9F), Location = new Point(555, 33), BackColor = Color.Transparent, AutoSize = false, Size = new Size(85, 20), TextAlign = ContentAlignment.MiddleLeft };
        _txtContactPerson = new TextBox { Font = new Font("Microsoft YaHei UI", 9F), Location = new Point(645, 30), Size = new Size(100, 25) };

        // 第二行：联系电话(100)、地址(65) —— Label 宽度=AutoSize实际值
        Label lblPhone = new() { Text = "联系电话：", Font = new Font("Microsoft YaHei UI", 9F), Location = new Point(15, 71), BackColor = Color.Transparent, AutoSize = false, Size = new Size(100, 20), TextAlign = ContentAlignment.MiddleLeft };
        _txtPhone = new TextBox { Font = new Font("Microsoft YaHei UI", 9F), Location = new Point(120, 68), Size = new Size(130, 25) };

        Label lblAddress = new() { Text = "地址：", Font = new Font("Microsoft YaHei UI", 9F), Location = new Point(260, 71), BackColor = Color.Transparent, AutoSize = false, Size = new Size(65, 20), TextAlign = ContentAlignment.MiddleLeft };
        _txtAddress = new TextBox { Font = new Font("Microsoft YaHei UI", 9F), Location = new Point(330, 68), Size = new Size(415, 25) };

        // 第三行：法人代表(100)、注册日期(100) —— Label 宽度=AutoSize实际值
        Label lblLegalPerson = new() { Text = "法人代表：", Font = new Font("Microsoft YaHei UI", 9F), Location = new Point(15, 110), BackColor = Color.Transparent, AutoSize = false, Size = new Size(100, 20), TextAlign = ContentAlignment.MiddleLeft };
        _txtLegalPerson = new TextBox { Font = new Font("Microsoft YaHei UI", 9F), Location = new Point(120, 108), Size = new Size(150, 25) };

        Label lblRegisterDate = new() { Text = "注册日期：", Font = new Font("Microsoft YaHei UI", 9F), Location = new Point(280, 110), BackColor = Color.Transparent, AutoSize = false, Size = new Size(100, 20), TextAlign = ContentAlignment.MiddleLeft };
        _dtpRegisterDate = new DateTimePicker { Font = new Font("Microsoft YaHei UI", 9F), Location = new Point(385, 108), Size = new Size(150, 25), Format = DateTimePickerFormat.Short };

        // 第四行：操作按钮（从 Y=108 下移至 Y=145，为第三行字段腾出空间）
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
            lblID, _txtSupplierID,
            lblName, _txtSupplierName,
            lblContact, _txtContactPerson,
            lblPhone, _txtPhone,
            lblAddress, _txtAddress,
            lblLegalPerson, _txtLegalPerson,
            lblRegisterDate, _dtpRegisterDate,
            _btnAdd, _btnUpdate, _btnDelete, _btnClear
        });

        // 返回按钮（Y=540 适配 ClientSize 594，避免超出底部）
        _btnBack = CreateButton("返回", 790, 540);
        _btnBack.Click += (s, e) => Close();

        Controls.AddRange(new Control[]
        {
            lblSearchID, _txtSearchID,
            lblSearchName, _txtSearchName,
            _btnSearch,
            _dgvSuppliers,
            _grpInput,
            _btnBack
        });

        // 窗体加载时读取数据
        Load += (s, e) => LoadData();

        // 根据权限启用/禁用操作按钮
        ApplyPermission();
    }

    /// <summary>
    /// 按查询条件加载供货商列表
    /// </summary>
    private void LoadData()
    {
        try
        {
            string id = _txtSearchID.Text.Trim();
            string name = _txtSearchName.Text.Trim();

            List<Supplier> suppliers = _supplierService.SearchSuppliers(id, name);
            _dgvSuppliers.DataSource = suppliers;
            SetColumnHeaders();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"加载数据失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    /// <summary>
    /// 设置 DataGridView 列标题、宽度与显示顺序
    /// </summary>
    private void SetColumnHeaders()
    {
        // 列宽总和=850，适配 DataGridView 宽度 854（预留竖向滚动条余量）
        SetColumn(nameof(Supplier.SupplierID), "供货商编号", 100);
        SetColumn(nameof(Supplier.SupplierName), "供货商名称", 150);
        SetColumn(nameof(Supplier.ContactPerson), "联系人", 80);
        SetColumn(nameof(Supplier.Phone), "联系电话", 110);
        SetColumn(nameof(Supplier.Address), "地址", 170);
        SetColumn(nameof(Supplier.LegalPerson), "法人代表", 120);
        SetColumn(nameof(Supplier.RegisterDate), "注册日期", 120);
    }

    /// <summary>
    /// 设置单列标题、宽度
    /// </summary>
    private void SetColumn(string propName, string headerText, int width)
    {
        if (!_dgvSuppliers.Columns.Contains(propName))
        {
            return;
        }
        _dgvSuppliers.Columns[propName].HeaderText = headerText;
        _dgvSuppliers.Columns[propName].Width = width;
    }

    /// <summary>
    /// 列表选中行变化时回填输入框
    /// </summary>
    private void DgvSuppliers_SelectionChanged(object sender, EventArgs e)
    {
        // 绑定切换瞬间 DataBoundItem 可能为 null，需做类型守卫避免空引用
        if (_dgvSuppliers.CurrentRow?.DataBoundItem is not Supplier supplier)
        {
            return;
        }

        _txtSupplierID.Text = supplier.SupplierID;
        _txtSupplierName.Text = supplier.SupplierName;
        _txtContactPerson.Text = supplier.ContactPerson;
        _txtPhone.Text = supplier.Phone;
        _txtAddress.Text = supplier.Address;
        _txtLegalPerson.Text = supplier.LegalPerson;
        // 注册日期为空时回填今日，避免 DateTimePicker 显示默认值造成误解
        _dtpRegisterDate.Value = supplier.RegisterDate ?? DateTime.Today;

        // 供货商编号为主键，修改时不可编辑
        _txtSupplierID.ReadOnly = true;
    }

    /// <summary>
    /// 添加按钮点击事件
    /// </summary>
    private void BtnAdd_Click(object sender, EventArgs e)
    {
        try
        {
            _supplierService.AddSupplier(
                _txtSupplierID.Text.Trim(),
                _txtSupplierName.Text.Trim(),
                _txtContactPerson.Text.Trim(),
                _txtPhone.Text.Trim(),
                _txtAddress.Text.Trim(),
                _txtLegalPerson.Text.Trim(),
                _dtpRegisterDate.Value);

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
            _supplierService.UpdateSupplier(
                _txtSupplierID.Text.Trim(),
                _txtSupplierName.Text.Trim(),
                _txtContactPerson.Text.Trim(),
                _txtPhone.Text.Trim(),
                _txtAddress.Text.Trim(),
                _txtLegalPerson.Text.Trim(),
                _dtpRegisterDate.Value);

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
        string supplierID = _txtSupplierID.Text.Trim();
        if (string.IsNullOrEmpty(supplierID))
        {
            MessageBox.Show("请先选择要删除的供货商", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (MessageBox.Show($"确定要删除供货商「{_txtSupplierName.Text}」吗？", "确认删除",
            MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
        {
            return;
        }

        try
        {
            _supplierService.DeleteSupplier(supplierID);
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
        _txtSupplierID.Clear();
        _txtSupplierName.Clear();
        _txtContactPerson.Clear();
        _txtPhone.Clear();
        _txtAddress.Clear();
        _txtLegalPerson.Clear();
        _dtpRegisterDate.Value = DateTime.Today;
        _txtSupplierID.ReadOnly = false;
        _txtSupplierID.Focus();
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
