using LibrarySys.BLL;
using LibrarySys.Common;
using LibrarySys.Models;
using System.ComponentModel;

namespace LibrarySys.Forms;

/// <summary>
/// 预约管理窗体：预约登记、通知取书、取消预约、查询
/// </summary>
public partial class FrmReservation : Form
{
    private readonly UserInfo _currentUser;
    private readonly ReservationBiz _biz = new();
    private DataGridView _dgv;
    private TextBox _txtReaderID;
    private TextBox _txtBookID;
    private ComboBox _cmbStatus;
    private Button _btnSearch;
    private Button _btnReserve;
    private Button _btnNotify;
    private Button _btnCancel;
    private Button _btnRefresh;

    /// <summary>无参构造，仅供 VS 设计器使用</summary>
    public FrmReservation()
    {
        InitializeComponent();
        BuildUI();
    }

    public FrmReservation(UserInfo currentUser)
    {
        _currentUser = currentUser;
        Text = "预约管理";
        InitializeComponent();
        BuildUI();
        Load += (_, _) => LoadData();
    }

    private void BuildUI()
    {
        // 设计器模式下跳过：设计器已在 InitializeComponent 中创建控件骨架
        if (LicenseManager.UsageMode == LicenseUsageMode.Designtime) return;
        // 运行时：清除 InitializeComponent 创建的骨架控件，重新完整构建
        Controls.Clear();

        DoubleBuffered = true;
        BackColor = Color.FromArgb(240, 242, 245);
        Font = UiHelper.DefaultFont;

        // ===== 查询区域 =====
        GroupBox gbQuery = new GroupBox
        {
            Text = "查询条件",
            Dock = DockStyle.Top,
            Height = 70,
            Font = new Font("Microsoft YaHei UI", 10F),
            Padding = new Padding(10, 20, 10, 5)
        };

        Label lblReader = new Label { Text = "读者编号：", Location = new Point(15, 25), AutoSize = true };
        _txtReaderID = new TextBox { Location = new Point(85, 22), Width = 120, Anchor = AnchorStyles.Left };

        Label lblBook = new Label { Text = "图书编号：", Location = new Point(220, 25), AutoSize = true };
        _txtBookID = new TextBox { Location = new Point(290, 22), Width = 120, Anchor = AnchorStyles.Left };

        Label lblStatus = new Label { Text = "状态：", Location = new Point(425, 25), AutoSize = true };
        _cmbStatus = new ComboBox
        {
            Location = new Point(465, 22),
            Width = 100,
            DropDownStyle = ComboBoxStyle.DropDownList,
            Anchor = AnchorStyles.Left
        };
        _cmbStatus.Items.AddRange(new[] { "全部", "排队中", "待取书", "已完成", "已取消" });
        _cmbStatus.SelectedIndex = 0;

        _btnSearch = new Button { Text = "查询", Location = new Point(580, 20), Width = 70 };
        _btnRefresh = new Button { Text = "刷新", Location = new Point(660, 20), Width = 70 };

        _btnSearch.Click += (_, _) => LoadData();
        _btnRefresh.Click += (_, _) => { _txtReaderID.Clear(); _txtBookID.Clear(); _cmbStatus.SelectedIndex = 0; LoadData(); };

        gbQuery.Controls.AddRange(new Control[] { lblReader, _txtReaderID, lblBook, _txtBookID, lblStatus, _cmbStatus, _btnSearch, _btnRefresh });

        // ===== DataGridView =====
        _dgv = new DataGridView { Dock = DockStyle.Fill };
        UiHelper.StyleDataGridView(_dgv);
        _dgv.Columns.AddRange(new DataGridViewColumn[]
        {
            new DataGridViewTextBoxColumn { HeaderText = "预约ID", Name = "ReserveID", Width = 60 },
            new DataGridViewTextBoxColumn { HeaderText = "读者编号", Name = "ReaderID", Width = 80 },
            new DataGridViewTextBoxColumn { HeaderText = "读者姓名", Name = "ReaderName", Width = 80 },
            new DataGridViewTextBoxColumn { HeaderText = "图书编号", Name = "BookID", Width = 80 },
            new DataGridViewTextBoxColumn { HeaderText = "书名", Name = "BookName", Width = 120 },
            new DataGridViewTextBoxColumn { HeaderText = "预约日期", Name = "ReserveDate", Width = 90 },
            new DataGridViewTextBoxColumn { HeaderText = "有效期至", Name = "ExpireDate", Width = 90 },
            new DataGridViewTextBoxColumn { HeaderText = "通知日期", Name = "NotifyDate", Width = 90 },
            new DataGridViewTextBoxColumn { HeaderText = "状态", Name = "Status", Width = 70 },
            new DataGridViewTextBoxColumn { HeaderText = "取消原因", Name = "CancelReason", Width = 100 }
        });
        _dgv.EnableHeadersVisualStyles = false;
        _dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(240, 242, 245);
        _dgv.ColumnHeadersDefaultCellStyle.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold);
        _dgv.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 249, 250);

        // ===== 底部操作按钮栏 =====
        Panel bottomPanel = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 50,
            BackColor = Color.FromArgb(240, 242, 245)
        };

        _btnReserve = new Button { Text = "预约登记", Location = new Point(10, 10), Width = 90, BackColor = Color.FromArgb(23, 162, 184), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
        _btnReserve.FlatAppearance.BorderSize = 0;
        _btnNotify = new Button { Text = "通知取书", Location = new Point(110, 10), Width = 90, BackColor = Color.FromArgb(46, 139, 87), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
        _btnNotify.FlatAppearance.BorderSize = 0;
        _btnCancel = new Button { Text = "取消预约", Location = new Point(210, 10), Width = 90, BackColor = Color.FromArgb(220, 53, 69), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
        _btnCancel.FlatAppearance.BorderSize = 0;

        _btnReserve.Click += (_, _) => DoReserve();
        _btnNotify.Click += (_, _) => DoNotify();
        _btnCancel.Click += (_, _) => DoCancel();

        bottomPanel.Controls.AddRange(new Control[] { _btnReserve, _btnNotify, _btnCancel });

        // 权限控制：普通用户可预约登记和取消预约，但通知取书仅管理员可操作
        // 设计器无参构造时 _currentUser 为 null，跳过权限控制
        if (_currentUser != null && _currentUser.UserPurview == BusinessConstants.ROLE_USER)
        {
            _btnNotify.Enabled = false;
        }

        Controls.Add(gbQuery);
        Controls.Add(_dgv);
        Controls.Add(bottomPanel);
    }

    /// <summary>加载预约数据</summary>
    private void LoadData()
    {
        try
        {
            string? readerID = string.IsNullOrWhiteSpace(_txtReaderID.Text) ? null : _txtReaderID.Text.Trim();
            string? bookID = string.IsNullOrWhiteSpace(_txtBookID.Text) ? null : _txtBookID.Text.Trim();
            string? status = _cmbStatus.SelectedIndex > 0 ? _cmbStatus.SelectedItem!.ToString() : null;

            var list = _biz.Search(readerID, bookID, status);
            _dgv.SuspendLayout();
            _dgv.Rows.Clear();
            foreach (var item in list)
            {
                _dgv.Rows.Add(
                    item.ReserveID, item.ReaderID, item.ReaderName ?? "-",
                    item.BookID, item.BookName ?? "-",
                    item.ReserveDate.ToString("yyyy-MM-dd"),
                    item.ExpireDate.ToString("yyyy-MM-dd"),
                    item.NotifyDate?.ToString("yyyy-MM-dd") ?? "",
                    item.Status,
                    item.CancelReason ?? ""
                );
            }
            _dgv.ResumeLayout();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"加载数据失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    /// <summary>预约登记</summary>
    private void DoReserve()
    {
        // 弹出输入对话框
        using Form inputForm = new Form
        {
            Text = "预约登记",
            Size = new Size(350, 200),
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false,
            MinimizeBox = false
        };

        Label lblReader = new Label { Text = "读者编号：", Location = new Point(20, 25), AutoSize = true };
        TextBox txtReader = new TextBox { Location = new Point(100, 22), Width = 200 };
        Label lblBook = new Label { Text = "图书编号：", Location = new Point(20, 60), AutoSize = true };
        TextBox txtBook = new TextBox { Location = new Point(100, 57), Width = 200 };
        Button btnOK = new Button { Text = "确认", Location = new Point(140, 105), Width = 80, DialogResult = DialogResult.OK };
        Button btnCancel = new Button { Text = "取消", Location = new Point(230, 105), Width = 80, DialogResult = DialogResult.Cancel };

        inputForm.Controls.AddRange(new Control[] { lblReader, txtReader, lblBook, txtBook, btnOK, btnCancel });
        inputForm.AcceptButton = btnOK;
        inputForm.CancelButton = btnCancel;

        if (inputForm.ShowDialog() == DialogResult.OK)
        {
            string readerID = txtReader.Text.Trim();
            string bookID = txtBook.Text.Trim();

            if (string.IsNullOrEmpty(readerID) || string.IsNullOrEmpty(bookID))
            {
                MessageBox.Show("读者编号和图书编号不能为空", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                _biz.Reserve(readerID, bookID, _currentUser.UserName);
                MessageBox.Show("预约登记成功", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                LoadData();
            }
            catch (BusinessException ex)
            {
                MessageBox.Show(ex.Message, "预约失败", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"操作失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    /// <summary>通知取书（将"排队中"升级为"待取书"）</summary>
    private void DoNotify()
    {
        if (_dgv.SelectedRows.Count == 0)
        {
            MessageBox.Show("请先选择一条预约记录", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        int reserveID = Convert.ToInt32(_dgv.SelectedRows[0].Cells["ReserveID"].Value);
        string status = _dgv.SelectedRows[0].Cells["Status"].Value?.ToString() ?? "";

        if (status != BusinessConstants.RESERVE_QUEUING)
        {
            MessageBox.Show("只能通知「排队中」状态的预约", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        try
        {
            _biz.NotifyPickup(reserveID, _currentUser.UserName);
            MessageBox.Show("已通知读者取书", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
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

    /// <summary>取消预约</summary>
    private void DoCancel()
    {
        if (_dgv.SelectedRows.Count == 0)
        {
            MessageBox.Show("请先选择一条预约记录", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        int reserveID = Convert.ToInt32(_dgv.SelectedRows[0].Cells["ReserveID"].Value);
        string status = _dgv.SelectedRows[0].Cells["Status"].Value?.ToString() ?? "";

        if (status != BusinessConstants.RESERVE_QUEUING && status != BusinessConstants.RESERVE_WAITING)
        {
            MessageBox.Show("只能取消「排队中」或「待取书」状态的预约", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (MessageBox.Show("确认取消该预约？", "确认", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            return;

        try
        {
            _biz.Cancel(reserveID, "手动取消", _currentUser.UserName);
            MessageBox.Show("预约已取消", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
}