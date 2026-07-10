using LibraryManagement.BLL;
using LibraryManagement.Models;

namespace LibraryManagement.Forms;

/// <summary>
/// 读者管理窗体：读者的增删改查及权限控制
/// </summary>
public class ReaderForm : Form
{
    private readonly ReaderService _readerService = new();
    private readonly User _currentUser;

    private readonly DataGridView _dgvReaders;
    private readonly TextBox _txtSearchID;
    private readonly TextBox _txtSearchName;
    private readonly TextBox _txtSearchDepartment;
    private readonly Button _btnSearch;
    private readonly Button _btnAdd;
    private readonly Button _btnUpdate;
    private readonly Button _btnDelete;
    private readonly Button _btnClear;
    private readonly Button _btnBack;
    private readonly GroupBox _grpInput;
    private readonly TextBox _txtReaderID;
    private readonly TextBox _txtReaderName;
    private readonly ComboBox _cmbSex;
    private readonly TextBox _txtPhone;
    private readonly TextBox _txtDepartment;
    private readonly DateTimePicker _dtpRegisterDate;

    /// <summary>
    /// 构造读者管理窗体
    /// </summary>
    /// <param name="currentUser">当前登录用户，用于权限判断</param>
    public ReaderForm(User currentUser)
    {
        _currentUser = currentUser;

        // 窗体基本属性
        Text = "读者管理";
        Size = new Size(800, 560);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        BackColor = Color.FromArgb(245, 247, 250);

        // ===== 查询区域 =====
        Label lblSearchID = new()
        {
            Text = "读者编号：",
            Font = new Font("Microsoft YaHei UI", 9F),
            Location = new Point(15, 18),
            AutoSize = true
        };
        _txtSearchID = new TextBox
        {
            Font = new Font("Microsoft YaHei UI", 9F),
            Location = new Point(85, 15),
            Size = new Size(120, 25)
        };

        Label lblSearchName = new()
        {
            Text = "姓名：",
            Font = new Font("Microsoft YaHei UI", 9F),
            Location = new Point(225, 18),
            AutoSize = true
        };
        _txtSearchName = new TextBox
        {
            Font = new Font("Microsoft YaHei UI", 9F),
            Location = new Point(270, 15),
            Size = new Size(120, 25)
        };

        Label lblSearchDept = new()
        {
            Text = "院系：",
            Font = new Font("Microsoft YaHei UI", 9F),
            Location = new Point(410, 18),
            AutoSize = true
        };
        _txtSearchDepartment = new TextBox
        {
            Font = new Font("Microsoft YaHei UI", 9F),
            Location = new Point(455, 15),
            Size = new Size(120, 25)
        };

        _btnSearch = CreateButton("查询", 600, 12);
        _btnSearch.Click += (s, e) => LoadData();

        // ===== DataGridView 列表 =====
        _dgvReaders = new DataGridView
        {
            Location = new Point(15, 50),
            Size = new Size(760, 220),
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            ReadOnly = true,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            BackgroundColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle
        };
        _dgvReaders.SelectionChanged += DgvReaders_SelectionChanged;

        // ===== 输入区域 =====
        _grpInput = new GroupBox
        {
            Text = "读者信息",
            Font = new Font("Microsoft YaHei UI", 9F),
            Location = new Point(15, 280),
            Size = new Size(760, 170)
        };

        Label lblID = new() { Text = "读者编号：", Location = new Point(15, 30), AutoSize = true, Font = new Font("Microsoft YaHei UI", 9F) };
        _txtReaderID = new TextBox { Location = new Point(85, 27), Size = new Size(140, 25), Font = new Font("Microsoft YaHei UI", 9F) };

        Label lblName = new() { Text = "姓名：", Location = new Point(245, 30), AutoSize = true, Font = new Font("Microsoft YaHei UI", 9F) };
        _txtReaderName = new TextBox { Location = new Point(290, 27), Size = new Size(140, 25), Font = new Font("Microsoft YaHei UI", 9F) };

        Label lblSex = new() { Text = "性别：", Location = new Point(450, 30), AutoSize = true, Font = new Font("Microsoft YaHei UI", 9F) };
        _cmbSex = new ComboBox
        {
            Location = new Point(495, 27),
            Size = new Size(90, 25),
            Font = new Font("Microsoft YaHei UI", 9F),
            // 仅允许从下拉项中选择，禁止自由输入，确保性别取值为“男”或“女”
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        _cmbSex.Items.AddRange(new object[] { "男", "女" });

        Label lblPhone = new() { Text = "联系电话：", Location = new Point(15, 70), AutoSize = true, Font = new Font("Microsoft YaHei UI", 9F) };
        _txtPhone = new TextBox { Location = new Point(85, 67), Size = new Size(140, 25), Font = new Font("Microsoft YaHei UI", 9F) };

        Label lblDept = new() { Text = "所在院系：", Location = new Point(245, 70), AutoSize = true, Font = new Font("Microsoft YaHei UI", 9F) };
        _txtDepartment = new TextBox { Location = new Point(320, 67), Size = new Size(140, 25), Font = new Font("Microsoft YaHei UI", 9F) };

        Label lblDate = new() { Text = "注册日期：", Location = new Point(480, 70), AutoSize = true, Font = new Font("Microsoft YaHei UI", 9F) };
        _dtpRegisterDate = new DateTimePicker
        {
            Location = new Point(550, 67),
            Size = new Size(150, 25),
            Font = new Font("Microsoft YaHei UI", 9F),
            Format = DateTimePickerFormat.Short,
            // 启用复选框以表示可空的注册日期：未勾选视为未设置
            ShowCheckBox = true
        };

        _btnAdd = CreateButton("添加", 85, 110);
        _btnAdd.Click += BtnAdd_Click;
        _btnUpdate = CreateButton("修改", 185, 110);
        _btnUpdate.Click += BtnUpdate_Click;
        _btnDelete = CreateButton("删除", 285, 110);
        _btnDelete.Click += BtnDelete_Click;
        _btnClear = CreateButton("清空", 385, 110);
        _btnClear.Click += (s, e) => ClearInput();

        _grpInput.Controls.AddRange(new Control[]
        {
            lblID, _txtReaderID,
            lblName, _txtReaderName,
            lblSex, _cmbSex,
            lblPhone, _txtPhone,
            lblDept, _txtDepartment,
            lblDate, _dtpRegisterDate,
            _btnAdd, _btnUpdate, _btnDelete, _btnClear
        });

        // ===== 返回按钮 =====
        _btnBack = CreateButton("返回", 695, 460);
        _btnBack.Click += (s, e) => Close();

        Controls.AddRange(new Control[]
        {
            lblSearchID, _txtSearchID,
            lblSearchName, _txtSearchName,
            lblSearchDept, _txtSearchDepartment,
            _btnSearch, _dgvReaders, _grpInput, _btnBack
        });

        // 根据权限设置操作按钮可用性
        ApplyPermission();

        // 窗体加载时读取全部读者
        Load += (s, e) => LoadData();
    }

    /// <summary>
    /// 加载读者数据到 DataGridView
    /// 任一查询条件非空时按条件查询，否则加载全部，避免空条件查询语义歧义
    /// </summary>
    private void LoadData()
    {
        try
        {
            string readerID = _txtSearchID.Text.Trim();
            string readerName = _txtSearchName.Text.Trim();
            string department = _txtSearchDepartment.Text.Trim();

            bool anyCondition = !string.IsNullOrEmpty(readerID)
                || !string.IsNullOrEmpty(readerName)
                || !string.IsNullOrEmpty(department);

            List<Reader> readers = anyCondition
                ? _readerService.SearchReaders(readerID, readerName, department)
                : _readerService.GetAllReaders();

            _dgvReaders.DataSource = readers;
            // 设置中文列标题
            _dgvReaders.Columns[nameof(Reader.ReaderID)].HeaderText = "读者编号";
            _dgvReaders.Columns[nameof(Reader.ReaderName)].HeaderText = "姓名";
            _dgvReaders.Columns[nameof(Reader.ReaderSex)].HeaderText = "性别";
            _dgvReaders.Columns[nameof(Reader.Phone)].HeaderText = "联系电话";
            _dgvReaders.Columns[nameof(Reader.Department)].HeaderText = "所在院系";
            _dgvReaders.Columns[nameof(Reader.RegisterDate)].HeaderText = "注册日期";
            // 注册日期列格式化，空值显示为空字符串
            _dgvReaders.Columns[nameof(Reader.RegisterDate)].DefaultCellStyle.Format = "yyyy-MM-dd";
            _dgvReaders.Columns[nameof(Reader.RegisterDate)].DefaultCellStyle.NullValue = string.Empty;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"加载数据失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    /// <summary>
    /// 列表选中行变化时回填输入框
    /// </summary>
    private void DgvReaders_SelectionChanged(object sender, EventArgs e)
    {
        if (_dgvReaders.CurrentRow?.DataBoundItem is Reader reader)
        {
            _txtReaderID.Text = reader.ReaderID;
            _txtReaderName.Text = reader.ReaderName;
            _cmbSex.SelectedItem = reader.ReaderSex;
            _txtPhone.Text = reader.Phone;
            _txtDepartment.Text = reader.Department;
            // 注册日期为可空类型：有值则勾选并回填，无值则取消勾选
            if (reader.RegisterDate.HasValue)
            {
                _dtpRegisterDate.Checked = true;
                _dtpRegisterDate.Value = reader.RegisterDate.Value;
            }
            else
            {
                _dtpRegisterDate.Checked = false;
            }
            // 编号为主键，选中后变为只读，避免修改主键
            _txtReaderID.ReadOnly = true;
            _txtReaderID.BackColor = Color.FromArgb(240, 240, 240);
        }
    }

    /// <summary>
    /// 添加按钮点击事件
    /// </summary>
    private void BtnAdd_Click(object sender, EventArgs e)
    {
        try
        {
            Reader reader = BuildReaderFromInput();
            _readerService.AddReader(reader);
            MessageBox.Show("添加成功", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            ClearInput();
            LoadData();
        }
        catch (BusinessException ex)
        {
            MessageBox.Show(ex.Message, "操作失败", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"操作失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    /// <summary>
    /// 修改按钮点击事件
    /// </summary>
    private void BtnUpdate_Click(object sender, EventArgs e)
    {
        if (_dgvReaders.CurrentRow?.DataBoundItem is not Reader selected)
        {
            MessageBox.Show("请先选择要修改的读者", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        try
        {
            Reader reader = BuildReaderFromInput();
            // 编号不可修改，沿用选中行主键，避免输入框被篡改导致主键漂移
            reader.ReaderID = selected.ReaderID;
            _readerService.UpdateReader(reader);
            MessageBox.Show("修改成功", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            LoadData();
        }
        catch (BusinessException ex)
        {
            MessageBox.Show(ex.Message, "操作失败", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"操作失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    /// <summary>
    /// 删除按钮点击事件
    /// </summary>
    private void BtnDelete_Click(object sender, EventArgs e)
    {
        if (_dgvReaders.CurrentRow?.DataBoundItem is not Reader selected)
        {
            MessageBox.Show("请先选择要删除的读者", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        if (MessageBox.Show($"确定要删除读者“{selected.ReaderName}”（{selected.ReaderID}）吗？", "确认删除",
            MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
        {
            return;
        }

        try
        {
            _readerService.DeleteReader(selected.ReaderID);
            MessageBox.Show("删除成功", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            ClearInput();
            LoadData();
        }
        catch (BusinessException ex)
        {
            MessageBox.Show(ex.Message, "操作失败", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"操作失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    /// <summary>
    /// 从输入控件构建 Reader 实体
    /// </summary>
    /// <returns>填充后的读者实体</returns>
    private Reader BuildReaderFromInput()
    {
        return new Reader
        {
            ReaderID = _txtReaderID.Text.Trim(),
            ReaderName = _txtReaderName.Text.Trim(),
            ReaderSex = _cmbSex.SelectedItem?.ToString() ?? string.Empty,
            Phone = _txtPhone.Text.Trim(),
            Department = _txtDepartment.Text.Trim(),
            // DateTimePicker 未勾选时表示未设置注册日期
            RegisterDate = _dtpRegisterDate.Checked ? _dtpRegisterDate.Value : null
        };
    }

    /// <summary>
    /// 清空输入框并重置编号可编辑状态
    /// </summary>
    private void ClearInput()
    {
        _txtReaderID.Clear();
        _txtReaderName.Clear();
        _cmbSex.SelectedIndex = -1;
        _txtPhone.Clear();
        _txtDepartment.Clear();
        _dtpRegisterDate.Checked = false;
        _txtReaderID.ReadOnly = false;
        _txtReaderID.BackColor = Color.White;
        if (_dgvReaders.CurrentRow != null)
        {
            _dgvReaders.ClearSelection();
        }
    }

    /// <summary>
    /// 根据登录用户权限启用/禁用操作按钮
    /// 管理员：全部按钮可用
    /// 普通用户：添加/修改/删除按钮禁用并变灰
    /// </summary>
    private void ApplyPermission()
    {
        bool isAdmin = _currentUser.UserPurview == BusinessConstants.ROLE_ADMIN;

        _btnAdd.Enabled = isAdmin;
        _btnUpdate.Enabled = isAdmin;
        _btnDelete.Enabled = isAdmin;

        // 普通用户操作按钮变灰，与禁用状态视觉一致
        if (!isAdmin)
        {
            _btnAdd.BackColor = Color.FromArgb(200, 200, 200);
            _btnUpdate.BackColor = Color.FromArgb(200, 200, 200);
            _btnDelete.BackColor = Color.FromArgb(200, 200, 200);
        }
    }

    /// <summary>
    /// 创建统一风格的按钮：蓝色背景、白色文字、扁平样式
    /// </summary>
    /// <param name="text">按钮文本</param>
    /// <param name="x">横坐标</param>
    /// <param name="y">纵坐标</param>
    /// <returns>按钮实例</returns>
    private static Button CreateButton(string text, int x, int y)
    {
        return new Button
        {
            Text = text,
            Font = new Font("Microsoft YaHei UI", 9F),
            Size = new Size(80, 30),
            Location = new Point(x, y),
            BackColor = Color.FromArgb(64, 158, 255),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            TextAlign = ContentAlignment.MiddleCenter
        };
    }
}
