using CampusMart.BLL;
using CampusMart.Common;
using CampusMart.Models;

namespace CampusMart.Forms;

/// <summary>
/// 供货商管理窗体
/// </summary>
/// <remarks>
/// 权限控制：操作员禁用增删改按钮，仅允许查询。
/// Label 宽度使用 UiHelper 动态测量，多列输入区采用链式布局。
/// </remarks>
public class FrmSupplier : Form
{
    private readonly UserInfo _currentUser;
    private readonly SupplierBiz _supplierBiz = new();

    private readonly TextBox _txtKeyword;
    private readonly Button _btnSearch;

    private readonly DataGridView _dgv;

    private readonly TextBox _txtSupplierID;
    private readonly TextBox _txtSupplierName;
    private readonly TextBox _txtLegalPerson;
    private readonly DateTimePicker _dtpRegisterDate;
    private readonly TextBox _txtContactPerson;
    private readonly TextBox _txtPhone;
    private readonly TextBox _txtAddress;

    private readonly Button _btnAdd;
    private readonly Button _btnUpdate;
    private readonly Button _btnDelete;
    private readonly Button _btnClear;
    private readonly Button _btnReturn;

    /// <summary>
    /// 构造供货商管理窗体
    /// </summary>
    /// <param name="currentUser">当前登录用户，用于权限控制</param>
    public FrmSupplier(UserInfo currentUser)
    {
        _currentUser = currentUser;

        Text = "供货商管理";
        Size = new Size(850, 600);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        BackColor = Color.FromArgb(245, 247, 250);

        Font labelFont = new("Microsoft YaHei UI", 9F);

        // ===== 查询区 =====
        const int margin = 15;
        const int rowY = 13;

        Label lblKeyword = UiHelper.CreateLabel("关键字：", margin, rowY, labelFont);
        _txtKeyword = new TextBox
        {
            Font = labelFont,
            Size = new Size(220, 25),
            Location = new Point(UiHelper.NextX(lblKeyword), rowY - 3)
        };
        _btnSearch = UiHelper.CreateButton("查询", _txtKeyword.Right + 10, rowY - 5, labelFont);
        _btnSearch.Click += BtnSearch_Click;

        // ===== DataGridView =====
        _dgv = new DataGridView
        {
            Location = new Point(margin, 50),
            Size = new Size(810, 260),
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
        _dgv.ColumnHeadersDefaultCellStyle.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold);
        _dgv.DefaultCellStyle.Font = labelFont;
        _dgv.Columns.AddRange(new DataGridViewColumn[]
        {
            new DataGridViewTextBoxColumn { HeaderText = "供货商编号", DataPropertyName = "SupplierID", Width = 70, ReadOnly = true },
            new DataGridViewTextBoxColumn { HeaderText = "名称", DataPropertyName = "SupplierName", Width = 160, ReadOnly = true },
            new DataGridViewTextBoxColumn { HeaderText = "法人代表", DataPropertyName = "LegalPerson", Width = 100, ReadOnly = true },
            new DataGridViewTextBoxColumn
            {
                HeaderText = "注册日期", DataPropertyName = "RegisterDate", Width = 120, ReadOnly = true,
                DefaultCellStyle = new DataGridViewCellStyle { Format = "yyyy-MM-dd", NullValue = "" }
            },
            new DataGridViewTextBoxColumn { HeaderText = "联系人", DataPropertyName = "ContactPerson", Width = 90, ReadOnly = true },
            new DataGridViewTextBoxColumn { HeaderText = "电话", DataPropertyName = "Phone", Width = 120, ReadOnly = true },
            new DataGridViewTextBoxColumn
            {
                HeaderText = "地址", DataPropertyName = "Address", Width = 150, ReadOnly = true,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
            }
        });
        _dgv.SelectionChanged += Dgv_SelectionChanged;

        // ===== 输入区 Panel =====
        Panel pnlInput = new()
        {
            Location = new Point(margin, 320),
            Size = new Size(810, 120),
            BackColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle
        };

        const int pMargin = 15;
        const int pGap = 12;

        // 第一行：编号、名称、法人代表
        const int pRow1Y = 13;
        Label lblSupplierID = UiHelper.CreateLabel("编号：", pMargin, pRow1Y, labelFont);
        _txtSupplierID = new TextBox
        {
            Font = labelFont,
            Size = new Size(90, 25),
            Location = new Point(UiHelper.NextX(lblSupplierID), pRow1Y - 3),
            ReadOnly = true
        };

        int col2X = _txtSupplierID.Right + pGap;
        Label lblSupplierName = UiHelper.CreateLabel("名称：", col2X, pRow1Y, labelFont);
        _txtSupplierName = new TextBox
        {
            Font = labelFont,
            Size = new Size(160, 25),
            Location = new Point(UiHelper.NextX(lblSupplierName), pRow1Y - 3)
        };

        int col3X = _txtSupplierName.Right + pGap;
        Label lblLegalPerson = UiHelper.CreateLabel("法人代表：", col3X, pRow1Y, labelFont);
        _txtLegalPerson = new TextBox
        {
            Font = labelFont,
            Size = new Size(160, 25),
            Location = new Point(UiHelper.NextX(lblLegalPerson), pRow1Y - 3)
        };

        // 第二行：注册日期、联系人、电话
        const int pRow2Y = 43;
        Label lblRegisterDate = UiHelper.CreateLabel("注册日期：", pMargin, pRow2Y, labelFont);
        _dtpRegisterDate = new DateTimePicker
        {
            Font = labelFont,
            Size = new Size(160, 25),
            Location = new Point(UiHelper.NextX(lblRegisterDate), pRow2Y - 3),
            Format = DateTimePickerFormat.Custom,
            CustomFormat = "yyyy-MM-dd"
        };

        int col2X2 = _dtpRegisterDate.Right + pGap;
        Label lblContactPerson = UiHelper.CreateLabel("联系人：", col2X2, pRow2Y, labelFont);
        _txtContactPerson = new TextBox
        {
            Font = labelFont,
            Size = new Size(130, 25),
            Location = new Point(UiHelper.NextX(lblContactPerson), pRow2Y - 3)
        };

        int col3X2 = _txtContactPerson.Right + pGap;
        Label lblPhone = UiHelper.CreateLabel("电话：", col3X2, pRow2Y, labelFont);
        _txtPhone = new TextBox
        {
            Font = labelFont,
            Size = new Size(130, 25),
            Location = new Point(UiHelper.NextX(lblPhone), pRow2Y - 3)
        };

        // 第三行：地址
        const int pRow3Y = 73;
        Label lblAddress = UiHelper.CreateLabel("地址：", pMargin, pRow3Y, labelFont);
        _txtAddress = new TextBox
        {
            Font = labelFont,
            Size = new Size(560, 25),
            Location = new Point(UiHelper.NextX(lblAddress), pRow3Y - 3)
        };

        pnlInput.Controls.AddRange(new Control[]
        {
            lblSupplierID, _txtSupplierID,
            lblSupplierName, _txtSupplierName,
            lblLegalPerson, _txtLegalPerson,
            lblRegisterDate, _dtpRegisterDate,
            lblContactPerson, _txtContactPerson,
            lblPhone, _txtPhone,
            lblAddress, _txtAddress
        });

        // ===== 操作按钮区 =====
        const int btnY = 455;
        _btnAdd = UiHelper.CreateButton("添加", margin, btnY, labelFont);
        _btnUpdate = UiHelper.CreateButton("修改", _btnAdd.Right + 10, btnY, labelFont);
        _btnDelete = UiHelper.CreateButton("删除", _btnUpdate.Right + 10, btnY, labelFont);
        _btnClear = UiHelper.CreateButton("清空", _btnDelete.Right + 10, btnY, labelFont);
        _btnReturn = UiHelper.CreateButton("返回", _btnClear.Right + 10, btnY, labelFont);
        _btnReturn.BackColor = Color.FromArgb(200, 200, 200);

        _btnAdd.Click += BtnAdd_Click;
        _btnUpdate.Click += BtnUpdate_Click;
        _btnDelete.Click += BtnDelete_Click;
        _btnClear.Click += BtnClear_Click;
        _btnReturn.Click += BtnReturn_Click;

        Controls.AddRange(new Control[]
        {
            lblKeyword, _txtKeyword, _btnSearch,
            _dgv,
            pnlInput,
            _btnAdd, _btnUpdate, _btnDelete, _btnClear, _btnReturn
        });

        ApplyPermission();
        LoadData();
    }

    private void LoadData()
    {
        try
        {
            List<SupplierInfo> list = _supplierBiz.GetAllSuppliers();
            _dgv.DataSource = list;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"加载数据失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void BtnSearch_Click(object sender, EventArgs e)
    {
        try
        {
            string keyword = _txtKeyword.Text.Trim();
            List<SupplierInfo> list = _supplierBiz.SearchSuppliers(keyword);
            _dgv.DataSource = list;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"查询失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void Dgv_SelectionChanged(object sender, EventArgs e)
    {
        if (_dgv.SelectedRows.Count == 0) return;
        SupplierInfo supplier = _dgv.SelectedRows[0].DataBoundItem as SupplierInfo;
        if (supplier == null) return;
        _txtSupplierID.Text = supplier.SupplierID.ToString();
        _txtSupplierName.Text = supplier.SupplierName;
        _txtLegalPerson.Text = supplier.LegalPerson;
        _dtpRegisterDate.Value = supplier.RegisterDate ?? DateTime.Today;
        _txtContactPerson.Text = supplier.ContactPerson;
        _txtPhone.Text = supplier.Phone;
        _txtAddress.Text = supplier.Address;
    }

    private void BtnAdd_Click(object sender, EventArgs e)
    {
        if (!string.IsNullOrEmpty(_txtSupplierID.Text))
        {
            MessageBox.Show("请先点击\"清空\"再添加新供货商", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        try
        {
            SupplierInfo supplier = BuildSupplierFromInput();
            _supplierBiz.AddSupplier(supplier);
            MessageBox.Show("添加成功", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            LoadData();
            ClearInput();
        }
        catch (BusinessException ex)
        {
            MessageBox.Show(ex.Message, "操作失败", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"系统错误：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void BtnUpdate_Click(object sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(_txtSupplierID.Text))
        {
            MessageBox.Show("请先选择要修改的供货商", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        try
        {
            SupplierInfo supplier = BuildSupplierFromInput();
            supplier.SupplierID = int.Parse(_txtSupplierID.Text);
            _supplierBiz.UpdateSupplier(supplier);
            MessageBox.Show("修改成功", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            LoadData();
        }
        catch (BusinessException ex)
        {
            MessageBox.Show(ex.Message, "操作失败", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"系统错误：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void BtnDelete_Click(object sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(_txtSupplierID.Text))
        {
            MessageBox.Show("请先选择要删除的供货商", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        if (MessageBox.Show("确定要删除该供货商吗？", "确认删除", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            return;
        try
        {
            int id = int.Parse(_txtSupplierID.Text);
            _supplierBiz.DeleteSupplier(id);
            MessageBox.Show("删除成功", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            LoadData();
            ClearInput();
        }
        catch (BusinessException ex)
        {
            MessageBox.Show(ex.Message, "操作失败", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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

    private SupplierInfo BuildSupplierFromInput()
    {
        return new SupplierInfo
        {
            SupplierName = _txtSupplierName.Text.Trim(),
            LegalPerson = _txtLegalPerson.Text.Trim(),
            RegisterDate = _dtpRegisterDate.Value,
            ContactPerson = _txtContactPerson.Text.Trim(),
            Phone = _txtPhone.Text.Trim(),
            Address = _txtAddress.Text.Trim()
        };
    }

    private void ClearInput()
    {
        _dgv.ClearSelection();
        _txtSupplierID.Clear();
        _txtSupplierName.Clear();
        _txtLegalPerson.Clear();
        _dtpRegisterDate.Value = DateTime.Today;
        _txtContactPerson.Clear();
        _txtPhone.Clear();
        _txtAddress.Clear();
    }

    private void ApplyPermission()
    {
        bool isAdmin = _currentUser.Role == RoleConstants.ADMIN;
        _btnAdd.Enabled = isAdmin;
        _btnUpdate.Enabled = isAdmin;
        _btnDelete.Enabled = isAdmin;
    }
}
