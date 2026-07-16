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
    private readonly Panel _grpInput;
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

        // ===== 查询区域：Label 宽度=AutoSize实际值（4字+冒号=100px, 2字+冒号=65px） =====
        Label lblSearchID = new()
        {
            Text = "读者编号：",
            Font = new Font("Microsoft YaHei UI", 9F),
            Location = new Point(15, 18),
            AutoSize = false,
            Size = new Size(100, 20),
            TextAlign = ContentAlignment.MiddleLeft,
            BackColor = Color.Transparent
        };
        _txtSearchID = new TextBox
        {
            Font = new Font("Microsoft YaHei UI", 9F),
            Location = new Point(120, 15),
            Size = new Size(110, 25)
        };

        Label lblSearchName = new()
        {
            Text = "姓名：",
            Font = new Font("Microsoft YaHei UI", 9F),
            Location = new Point(240, 18),
            AutoSize = false,
            Size = new Size(65, 20),
            TextAlign = ContentAlignment.MiddleLeft,
            BackColor = Color.Transparent
        };
        _txtSearchName = new TextBox
        {
            Font = new Font("Microsoft YaHei UI", 9F),
            Location = new Point(310, 15),
            Size = new Size(110, 25)
        };

        Label lblSearchDept = new()
        {
            Text = "院系：",
            Font = new Font("Microsoft YaHei UI", 9F),
            Location = new Point(430, 18),
            AutoSize = false,
            Size = new Size(65, 20),
            TextAlign = ContentAlignment.MiddleLeft,
            BackColor = Color.Transparent
        };
        _txtSearchDepartment = new TextBox
        {
            Font = new Font("Microsoft YaHei UI", 9F),
            Location = new Point(500, 15),
            Size = new Size(110, 25)
        };

        _btnSearch = CreateButton("查询", 620, 12);
        _btnSearch.Click += (s, e) => LoadData();

        // ===== DataGridView 列表 =====
        _dgvReaders = new DataGridView
        {
            Location = new Point(15, 50),
            Size = new Size(780, 230),
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            ReadOnly = true,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None,
            BackgroundColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            RowHeadersVisible = false
        };
        _dgvReaders.SelectionChanged += DgvReaders_SelectionChanged;

        // ===== 输入区域（Panel 无圆角边框遮盖） =====
        _grpInput = new Panel
        {
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = Color.FromArgb(245, 247, 250),
            Location = new Point(15, 290),
            Size = new Size(780, 170)
        };
        Label lblGrpTitle = new()
        {
            Text = "读者信息",
            Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold),
            ForeColor = Color.FromArgb(64, 158, 255),
            Location = new Point(10, 5),
            AutoSize = true,
            BackColor = Color.Transparent
        };

        // 第一行：读者编号(100)、姓名(65)、性别(65) —— Label 宽度=AutoSize实际值
        Label lblID = new() { Text = "读者编号：", Location = new Point(15, 30), AutoSize = false, Size = new Size(100, 20), TextAlign = ContentAlignment.MiddleLeft, Font = new Font("Microsoft YaHei UI", 9F), BackColor = Color.Transparent };
        _txtReaderID = new TextBox { Location = new Point(120, 27), Size = new Size(130, 25), Font = new Font("Microsoft YaHei UI", 9F) };

        Label lblName = new() { Text = "姓名：", Location = new Point(260, 30), AutoSize = false, Size = new Size(65, 20), TextAlign = ContentAlignment.MiddleLeft, Font = new Font("Microsoft YaHei UI", 9F), BackColor = Color.Transparent };
        _txtReaderName = new TextBox { Location = new Point(330, 27), Size = new Size(130, 25), Font = new Font("Microsoft YaHei UI", 9F) };

        Label lblSex = new() { Text = "性别：", Location = new Point(470, 30), AutoSize = false, Size = new Size(65, 20), TextAlign = ContentAlignment.MiddleLeft, Font = new Font("Microsoft YaHei UI", 9F), BackColor = Color.Transparent };
        // 性别使用 ComboBox 而非 TextBox：通过枚举值约束输入，避免用户填入非法性别文本
        _cmbSex = new ComboBox
        {
            Location = new Point(540, 27),
            Size = new Size(90, 25),
            Font = new Font("Microsoft YaHei UI", 9F),
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        _cmbSex.Items.AddRange(new object[] { "男", "女" });

        // 第二行：联系电话(100)、所在院系(100)、注册日期(100) —— Label 宽度=AutoSize实际值
        Label lblPhone = new() { Text = "联系电话：", Location = new Point(15, 70), AutoSize = false, Size = new Size(100, 20), TextAlign = ContentAlignment.MiddleLeft, Font = new Font("Microsoft YaHei UI", 9F), BackColor = Color.Transparent };
        _txtPhone = new TextBox { Location = new Point(120, 67), Size = new Size(130, 25), Font = new Font("Microsoft YaHei UI", 9F) };

        Label lblDept = new() { Text = "所在院系：", Location = new Point(260, 70), AutoSize = false, Size = new Size(100, 20), TextAlign = ContentAlignment.MiddleLeft, Font = new Font("Microsoft YaHei UI", 9F), BackColor = Color.Transparent };
        _txtDepartment = new TextBox { Location = new Point(365, 67), Size = new Size(130, 25), Font = new Font("Microsoft YaHei UI", 9F) };

        Label lblDate = new() { Text = "注册日期：", Location = new Point(505, 70), AutoSize = false, Size = new Size(100, 20), TextAlign = ContentAlignment.MiddleLeft, Font = new Font("Microsoft YaHei UI", 9F), BackColor = Color.Transparent };
        _dtpRegisterDate = new DateTimePicker
        {
            Location = new Point(610, 67),
            Size = new Size(150, 25),
            Font = new Font("Microsoft YaHei UI", 9F),
            Format = DateTimePickerFormat.Short,
            // ShowCheckBox 配合可空的注册日期字段：勾选表示有值，取消勾选表示未设置
            ShowCheckBox = true
        };

        _btnAdd = CreateButton("添加", 100, 110);
        _btnAdd.Click += BtnAdd_Click;
        _btnUpdate = CreateButton("修改", 200, 110);
        _btnUpdate.Click += BtnUpdate_Click;
        _btnDelete = CreateButton("删除", 300, 110);
        _btnDelete.Click += BtnDelete_Click;
        _btnClear = CreateButton("清空", 400, 110);
        _btnClear.Click += (s, e) => ClearInput();

        _grpInput.Controls.AddRange(new Control[]
        {
            lblGrpTitle,
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
            _dgvReaders.Columns[nameof(Reader.ReaderID)].HeaderText = "读者编号";
            _dgvReaders.Columns[nameof(Reader.ReaderID)].Width = 90;
            _dgvReaders.Columns[nameof(Reader.ReaderName)].HeaderText = "姓名";
            _dgvReaders.Columns[nameof(Reader.ReaderName)].Width = 80;
            _dgvReaders.Columns[nameof(Reader.ReaderSex)].HeaderText = "性别";
            _dgvReaders.Columns[nameof(Reader.ReaderSex)].Width = 60;
            _dgvReaders.Columns[nameof(Reader.Phone)].HeaderText = "联系电话";
            _dgvReaders.Columns[nameof(Reader.Phone)].Width = 120;
            _dgvReaders.Columns[nameof(Reader.Department)].HeaderText = "所在院系";
            _dgvReaders.Columns[nameof(Reader.Department)].Width = 150;
            _dgvReaders.Columns[nameof(Reader.RegisterDate)].HeaderText = "注册日期";
            _dgvReaders.Columns[nameof(Reader.RegisterDate)].Width = 100;

            _dgvReaders.Columns[nameof(Reader.RegisterDate)].DefaultCellStyle.Format = "yyyy-MM-dd";
            // 注册日期为 null 时显示为空字符串，而非默认的 "null" 文字
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
        // 数据绑定切换瞬间 DataBoundItem 可能为 null，类型匹配失败时静默跳过，避免空引用异常
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
        // 清除列表选中状态，避免选中行高亮与已清空的输入框内容不一致
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
