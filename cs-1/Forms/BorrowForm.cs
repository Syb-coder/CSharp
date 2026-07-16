using LibraryManagement.BLL;
using LibraryManagement.Models;

namespace LibraryManagement.Forms;

/// <summary>
/// 图书借阅管理窗体：提供借阅记录的多条件查询、办理借书与还书操作
/// 权限说明：普通用户仅可查询；管理员可办理借书/还书
/// </summary>
public class BorrowForm : Form
{
    private readonly BorrowService _borrowService = new();
    private readonly User _currentUser;

    // ===== 查询区控件 =====
    private readonly TextBox _txtSearchReaderID;
    private readonly TextBox _txtSearchBookID;
    private readonly ComboBox _cboStatus;
    private readonly DateTimePicker _dtpStartDate;
    private readonly DateTimePicker _dtpEndDate;
    private readonly Button _btnSearch;

    // ===== 列表区控件 =====
    private readonly DataGridView _dgvBorrows;

    // ===== 操作区控件 =====
    private readonly Panel _grpOperation;
    private readonly TextBox _txtBorrowReaderID;
    private readonly TextBox _txtBorrowBookID;
    private readonly Button _btnBorrow;
    private readonly Button _btnReturn;
    private readonly Button _btnBack;

    /// <summary>
    /// 构造图书借阅管理窗体
    /// </summary>
    /// <param name="currentUser">当前登录用户，用于权限控制</param>
    public BorrowForm(User currentUser)
    {
        _currentUser = currentUser;

        // 窗体基本属性
        Text = "图书借阅管理";
        Size = new Size(900, 600);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        BackColor = Color.FromArgb(245, 247, 250);

        // ===== 查询区第一行：Label 宽度=AutoSize实际值（4字+冒号=100px, 2字+冒号=65px） =====
        Label lblReader = new()
        {
            Text = "读者编号：",
            Font = new Font("Microsoft YaHei UI", 9F),
            Location = new Point(15, 15),
            AutoSize = false,
            Size = new Size(100, 20),
            TextAlign = ContentAlignment.MiddleLeft,
            BackColor = Color.Transparent
        };
        _txtSearchReaderID = new TextBox
        {
            Font = new Font("Microsoft YaHei UI", 9F),
            Location = new Point(120, 12),
            Size = new Size(110, 25)
        };
        Label lblBook = new()
        {
            Text = "图书编号：",
            Font = new Font("Microsoft YaHei UI", 9F),
            Location = new Point(240, 15),
            AutoSize = false,
            Size = new Size(100, 20),
            TextAlign = ContentAlignment.MiddleLeft,
            BackColor = Color.Transparent
        };
        _txtSearchBookID = new TextBox
        {
            Font = new Font("Microsoft YaHei UI", 9F),
            Location = new Point(345, 12),
            Size = new Size(110, 25)
        };
        Label lblStatus = new()
        {
            Text = "状态：",
            Font = new Font("Microsoft YaHei UI", 9F),
            Location = new Point(465, 15),
            AutoSize = false,
            Size = new Size(65, 20),
            TextAlign = ContentAlignment.MiddleLeft,
            BackColor = Color.Transparent
        };
        _cboStatus = new ComboBox
        {
            Font = new Font("Microsoft YaHei UI", 9F),
            Location = new Point(535, 12),
            Size = new Size(80, 25),
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        // 首项"全部"在查询时转为 null，DAL 层将 null 转为 DBNull 表示不按状态过滤
        _cboStatus.Items.AddRange(new object[] { "全部", BusinessConstants.STATUS_BORROWED, BusinessConstants.STATUS_RETURNED });
        _cboStatus.SelectedIndex = 0;

        _btnSearch = CreateButton("查询", 780, 10);
        _btnSearch.Click += (s, e) => LoadData();

        // ===== 查询区第二行：借出日期起止（5字+冒号=120px） =====
        Label lblStart = new()
        {
            Text = "借出日期起：",
            Font = new Font("Microsoft YaHei UI", 9F),
            Location = new Point(15, 48),
            AutoSize = false,
            Size = new Size(120, 20),
            TextAlign = ContentAlignment.MiddleLeft,
            BackColor = Color.Transparent
        };
        _dtpStartDate = new DateTimePicker
        {
            Font = new Font("Microsoft YaHei UI", 9F),
            Location = new Point(140, 45),
            Size = new Size(140, 25),
            Format = DateTimePickerFormat.Short,
            // ShowCheckBox 使日期范围查询变为可选：未勾选表示不限制起止日期
            ShowCheckBox = true,
            Checked = false,
            Value = DateTime.Today
        };
        Label lblEnd = new()
        {
            Text = "借出日期止：",
            Font = new Font("Microsoft YaHei UI", 9F),
            Location = new Point(290, 48),
            AutoSize = false,
            Size = new Size(120, 20),
            TextAlign = ContentAlignment.MiddleLeft,
            BackColor = Color.Transparent
        };
        _dtpEndDate = new DateTimePicker
        {
            Font = new Font("Microsoft YaHei UI", 9F),
            Location = new Point(415, 45),
            Size = new Size(140, 25),
            Format = DateTimePickerFormat.Short,
            ShowCheckBox = true,
            Checked = false,
            Value = DateTime.Today
        };

        // ===== 列表区：DataGridView =====
        _dgvBorrows = new DataGridView
        {
            Location = new Point(15, 80),
            Size = new Size(855, 260),
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            ReadOnly = true,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None,
            BackgroundColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            // 关闭自动生成列，改用 SetupGridColumns 手动定义列，以精确控制列顺序、表头文本和日期格式
            AutoGenerateColumns = false,
            RowHeadersVisible = false
        };
        SetupGridColumns();

        // ===== 操作区（Panel 无圆角边框遮盖） =====
        _grpOperation = new Panel
        {
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = Color.FromArgb(245, 247, 250),
            Location = new Point(15, 350),
            Size = new Size(855, 155)
        };
        Label lblGrpTitle = new()
        {
            Text = "借书 / 还书操作",
            Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold),
            ForeColor = Color.FromArgb(64, 158, 255),
            Location = new Point(10, 5),
            AutoSize = true,
            BackColor = Color.Transparent
        };

        // 借书区：读者编号、图书编号输入框 + 确认借出按钮
        Label lblBorrowTitle = new()
        {
            Text = "【借书】",
            Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold),
            ForeColor = Color.FromArgb(64, 158, 255),
            Location = new Point(15, 22),
            AutoSize = true,
            BackColor = Color.Transparent
        };
        Label lblBorrowReader = new()
        {
            Text = "读者编号：",
            Font = new Font("Microsoft YaHei UI", 9F),
            Location = new Point(15, 55),
            AutoSize = false,
            Size = new Size(100, 20),
            TextAlign = ContentAlignment.MiddleLeft,
            BackColor = Color.Transparent
        };
        _txtBorrowReaderID = new TextBox
        {
            Font = new Font("Microsoft YaHei UI", 9F),
            Location = new Point(120, 52),
            Size = new Size(110, 25)
        };
        Label lblBorrowBook = new()
        {
            Text = "图书编号：",
            Font = new Font("Microsoft YaHei UI", 9F),
            Location = new Point(240, 55),
            AutoSize = false,
            Size = new Size(100, 20),
            TextAlign = ContentAlignment.MiddleLeft,
            BackColor = Color.Transparent
        };
        _txtBorrowBookID = new TextBox
        {
            Font = new Font("Microsoft YaHei UI", 9F),
            Location = new Point(345, 52),
            Size = new Size(110, 25)
        };
        _btnBorrow = CreateButton("确认借出", 15, 88);
        _btnBorrow.Size = new Size(100, 30);

        // 还书区：选中借出状态记录后点击确认归还
        Label lblReturnTitle = new()
        {
            Text = "【还书】",
            Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold),
            ForeColor = Color.FromArgb(64, 158, 255),
            Location = new Point(445, 22),
            AutoSize = true,
            BackColor = Color.Transparent
        };
        Label lblReturnTip = new()
        {
            Text = "请先在上方列表选中“借出”状态的记录",
            Font = new Font("Microsoft YaHei UI", 9F),
            ForeColor = Color.FromArgb(102, 102, 102),
            Location = new Point(445, 55),
            AutoSize = true,
            BackColor = Color.Transparent
        };
        _btnReturn = CreateButton("确认归还", 445, 88);
        _btnReturn.Size = new Size(100, 30);

        _grpOperation.Controls.AddRange(new Control[]
        {
            lblGrpTitle,
            lblBorrowTitle, lblBorrowReader, _txtBorrowReaderID,
            lblBorrowBook, _txtBorrowBookID, _btnBorrow,
            lblReturnTitle, lblReturnTip, _btnReturn
        });

        // 返回按钮（Y=510 适配 ClientSize 544，避免超出底部）
        _btnBack = CreateButton("返回", 770, 510);

        // 事件绑定
        _btnBorrow.Click += BtnBorrow_Click;
        _btnReturn.Click += BtnReturn_Click;
        _btnBack.Click += (s, e) => Close();

        Controls.AddRange(new Control[]
        {
            lblReader, _txtSearchReaderID, lblBook, _txtSearchBookID,
            lblStatus, _cboStatus, _btnSearch,
            lblStart, _dtpStartDate, lblEnd, _dtpEndDate,
            _dgvBorrows, _grpOperation, _btnBack
        });

        // 根据权限启用/禁用借书、还书按钮
        ApplyPermission();

        // 窗体加载时拉取全部借阅记录
        Load += (s, e) => LoadData();
    }

    /// <summary>
    /// 配置 DataGridView 列（手动绑定以控制列顺序、表头文本与日期格式）
    /// </summary>
    private void SetupGridColumns()
    {
        _dgvBorrows.Columns.AddRange(new DataGridViewColumn[]
        {
            new DataGridViewTextBoxColumn
            {
                Name = "ColBorrowID", HeaderText = "借阅编号",
                DataPropertyName = nameof(BorrowRecord.BorrowID), Width = 70
            },
            new DataGridViewTextBoxColumn
            {
                Name = "ColReaderID", HeaderText = "读者编号",
                DataPropertyName = nameof(BorrowRecord.ReaderID), Width = 80
            },
            new DataGridViewTextBoxColumn
            {
                Name = "ColReaderName", HeaderText = "读者姓名",
                DataPropertyName = nameof(BorrowRecord.ReaderName), Width = 80
            },
            new DataGridViewTextBoxColumn
            {
                Name = "ColBookID", HeaderText = "图书编号",
                DataPropertyName = nameof(BorrowRecord.BookID), Width = 80
            },
            new DataGridViewTextBoxColumn
            {
                Name = "ColBookName", HeaderText = "书名",
                DataPropertyName = nameof(BorrowRecord.BookName), Width = 160
            },
            new DataGridViewTextBoxColumn
            {
                Name = "ColBorrowDate", HeaderText = "借出日期",
                DataPropertyName = nameof(BorrowRecord.BorrowDate),
                DefaultCellStyle = new DataGridViewCellStyle { Format = "yyyy-MM-dd" },
                Width = 95
            },
            new DataGridViewTextBoxColumn
            {
                Name = "ColDueDate", HeaderText = "应还日期",
                DataPropertyName = nameof(BorrowRecord.DueDate),
                DefaultCellStyle = new DataGridViewCellStyle { Format = "yyyy-MM-dd" },
                Width = 95
            },
            new DataGridViewTextBoxColumn
            {
                Name = "ColReturnDate", HeaderText = "归还日期",
                DataPropertyName = nameof(BorrowRecord.ReturnDate),
                // 归还日期为 null 时显示为空字符串
                DefaultCellStyle = new DataGridViewCellStyle { Format = "yyyy-MM-dd", NullValue = "" },
                Width = 95
            },
            new DataGridViewTextBoxColumn
            {
                Name = "ColStatus", HeaderText = "状态",
                DataPropertyName = nameof(BorrowRecord.Status), Width = 60
            }
        });
    }

    /// <summary>
    /// 根据查询条件加载借阅记录到列表
    /// </summary>
    private void LoadData()
    {
        try
        {
            string readerID = _txtSearchReaderID.Text.Trim();
            string bookID = _txtSearchBookID.Text.Trim();
            // "全部"时传 null 表示不按状态过滤（DAL 层会将空值转为 DBNull）
            string status = _cboStatus.SelectedIndex == 0 ? null : _cboStatus.SelectedItem.ToString();
            // 日期复选框未勾选时传 null 表示不限
            DateTime? startDate = _dtpStartDate.Checked ? _dtpStartDate.Value.Date : null;
            DateTime? endDate = _dtpEndDate.Checked ? _dtpEndDate.Value.Date : null;

            List<BorrowRecord> list = _borrowService.SearchBorrows(readerID, bookID, status, startDate, endDate);
            _dgvBorrows.DataSource = list;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"加载数据失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    /// <summary>
    /// 确认借出按钮点击事件：校验输入后调用 BorrowBook
    /// </summary>
    private void BtnBorrow_Click(object sender, EventArgs e)
    {
        string readerID = _txtBorrowReaderID.Text.Trim();
        string bookID = _txtBorrowBookID.Text.Trim();

        // 卫语句：校验输入非空
        if (readerID.Length == 0 || bookID.Length == 0)
        {
            MessageBox.Show("请填写读者编号和图书编号", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        if (MessageBox.Show("确认借出该图书吗？", "确认借出", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
        {
            return;
        }

        try
        {
            _borrowService.BorrowBook(readerID, bookID);
            MessageBox.Show("借出成功", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            _txtBorrowReaderID.Clear();
            _txtBorrowBookID.Clear();
            LoadData();
        }
        catch (BusinessException ex)
        {
            // 业务规则校验失败：显示警告
            MessageBox.Show(ex.Message, "操作失败", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        catch (Exception ex)
        {
            // 其他异常：显示错误
            MessageBox.Show($"操作失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    /// <summary>
    /// 确认归还按钮点击事件：校验选中记录状态后调用 ReturnBook
    /// </summary>
    private void BtnReturn_Click(object sender, EventArgs e)
    {
        // 卫语句：校验是否选中行
        if (_dgvBorrows.CurrentRow?.DataBoundItem is not BorrowRecord record)
        {
            MessageBox.Show("请先选择要归还的借阅记录", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        // 卫语句：仅"借出"状态可归还，避免重复归还同一记录
        if (record.Status != BusinessConstants.STATUS_BORROWED)
        {
            MessageBox.Show("该记录已归还，无需重复操作", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        if (MessageBox.Show($"确认归还《{record.BookName}》吗？", "确认归还", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
        {
            return;
        }

        try
        {
            (int overdueDays, decimal fineAmount) = _borrowService.ReturnBook(record.BorrowID);
            // 逾期天数>0 时弹逾期提示，包含罚款金额
            if (overdueDays > 0)
            {
                MessageBox.Show($"该书已逾期 {overdueDays} 天，逾期罚款 {fineAmount:F2} 元，请提醒读者缴纳", "逾期提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            MessageBox.Show("归还成功", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            LoadData();
        }
        catch (BusinessException ex)
        {
            // 业务规则校验失败：显示警告
            MessageBox.Show(ex.Message, "操作失败", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        catch (Exception ex)
        {
            // 其他异常：显示错误
            MessageBox.Show($"操作失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    /// <summary>
    /// 根据登录用户权限启用/禁用借书、还书按钮
    /// 管理员：可用；普通用户：禁用并变灰
    /// </summary>
    private void ApplyPermission()
    {
        bool isAdmin = _currentUser?.UserPurview == BusinessConstants.ROLE_ADMIN;

        _btnBorrow.Enabled = isAdmin;
        _btnReturn.Enabled = isAdmin;

        // 普通用户操作按钮变灰
        if (!isAdmin)
        {
            _btnBorrow.BackColor = Color.FromArgb(200, 200, 200);
            _btnReturn.BackColor = Color.FromArgb(200, 200, 200);
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
