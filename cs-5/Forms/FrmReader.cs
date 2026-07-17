using LibrarySys.BLL;
using LibrarySys.Common;
using LibrarySys.Models;
using System.ComponentModel;

namespace LibrarySys.Forms;

/// <summary>
/// 读者信息管理窗体
/// 固定坐标布局：查询区(顶部) → DataGridView(中间) → 编辑区(底部)
/// </summary>
public partial class FrmReader : Form
{
    private readonly ReaderBiz _biz = new();
    private readonly UserInfo _currentUser;
    private readonly bool _canEdit;

    private TextBox _txtReaderID, _txtReaderName, _txtPhone, _txtDepartment;
    private ComboBox _cboSex;
    private DateTimePicker _dtpRegisterDate;
    private TextBox _txtQueryID, _txtQueryName;
    private DataGridView _dgv;
    private bool _isLoading;

    public FrmReader()
    {
        _canEdit = true;
        InitializeComponent();
        BuildUI();
        Load += (_, _) => LoadData();
    }

    public FrmReader(UserInfo currentUser)
    {
        _currentUser = currentUser;
        _canEdit = currentUser.UserPurview == BusinessConstants.ROLE_ADMIN;
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
        Text = "读者信息管理";
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(1150, 700);
        MinimumSize = new Size(1100, 680);
        Font = UiHelper.DefaultFont;

        // 创建控件
        _txtReaderID = UiHelper.CreateTextBox();
        _txtReaderName = UiHelper.CreateTextBox();
        _cboSex = UiHelper.CreateComboBox();
        _cboSex.Items.AddRange(new object[] { "男", "女" });
        _txtPhone = UiHelper.CreateTextBox();
        _txtDepartment = UiHelper.CreateTextBox();
        _dtpRegisterDate = UiHelper.CreateDateTimePicker();

        _txtQueryID = UiHelper.CreateTextBox();
        _txtQueryName = UiHelper.CreateTextBox();

        // ===== 顶部查询区（固定坐标） =====
        GroupBox grpQuery = new GroupBox()
        {
            Text = "查询条件",
            Location = new Point(0, 0),
            Size = new Size(1000, 125),
            Dock = DockStyle.Top
        };

        UiHelper.LayoutFields(grpQuery.Controls, new List<UiHelper.FieldDef>
        {
            new("读者编号：", _txtQueryID),
            new("读者姓名：", _txtQueryName),
        }, pairsPerRow: 2, startX: 20, startY: 30);

        Button btnQuery = UiHelper.CreateButton("查询");
        Button btnShowAll = UiHelper.CreateButton("显示全部");
        btnQuery.Click += (_, _) => DoQuery();
        btnShowAll.Click += (_, _) => DoShowAll();
        UiHelper.LayoutButtons(grpQuery.Controls, new[] { btnQuery, btnShowAll }, 20, 72);

        // ===== DataGridView（手动定位，不依赖 Dock，避免嵌入窗体时布局错乱） =====
        _dgv = new DataGridView();
        UiHelper.StyleDataGridView(_dgv);
        _dgv.SelectionChanged += (_, _) => BindEditForm();

        // ===== 底部编辑区（固定坐标） =====
        GroupBox grpEdit = new GroupBox()
        {
            Text = "读者信息",
            Dock = DockStyle.Bottom,
            Height = 195
        };

        UiHelper.LayoutFields(grpEdit.Controls, new List<UiHelper.FieldDef>
        {
            new("读者编号：", _txtReaderID),
            new("姓名：", _txtReaderName),
            new("性别：", _cboSex),
            new("联系电话：", _txtPhone),
            new("所在院系：", _txtDepartment),
            new("注册日期：", _dtpRegisterDate),
        }, pairsPerRow: 3, startX: 20, startY: 30);

        Button btnAdd = UiHelper.CreateButton("新增"); btnAdd.Enabled = _canEdit;
        Button btnUpdate = UiHelper.CreateButton("修改"); btnUpdate.Enabled = _canEdit;
        Button btnDelete = UiHelper.CreateButton("删除"); btnDelete.Enabled = _canEdit;
        Button btnClear = UiHelper.CreateButton("清空");
        Button btnReturn = UiHelper.CreateButton("返回");
        btnAdd.Click += (_, _) => DoAdd();
        btnUpdate.Click += (_, _) => DoUpdate();
        btnDelete.Click += (_, _) => DoDelete();
        btnClear.Click += (_, _) => DoClear();
        btnReturn.Click += (_, _) => Close();
        UiHelper.LayoutButtonsWithReturn(
            grpEdit.Controls,
            new[] { btnAdd, btnUpdate, btnDelete, btnClear },
            btnReturn, 20, 140, 1150);

        // 添加顺序：先底部，再顶部，最后 Fill
        Controls.Add(grpEdit);
        Controls.Add(grpQuery);
        Controls.Add(_dgv);

        // 手动控制 DataGridView 位置，确保在 grpQuery 和 grpEdit 之间
        // 嵌入窗体时 WinForms Dock 布局可能异常，手动定位更可靠
        Layout += (_, _) =>
        {
            _dgv.Location = new Point(0, grpQuery.Bottom);
            _dgv.Size = new Size(ClientSize.Width, grpEdit.Top - grpQuery.Bottom);
        };
    }

    private void LoadData()
    {
        _isLoading = true;
        _dgv.DataSource = _biz.GetAll();
        SetChineseHeaders();
        _isLoading = false;
    }

    private void SetChineseHeaders()
    {
        if (_dgv.Columns.Count == 0) return;
        _dgv.Columns["ReaderID"].HeaderText = "读者编号";
        _dgv.Columns["ReaderName"].HeaderText = "姓名";
        _dgv.Columns["ReaderSex"].HeaderText = "性别";
        _dgv.Columns["Phone"].HeaderText = "联系电话";
        _dgv.Columns["Department"].HeaderText = "所在院系";
        _dgv.Columns["RegisterDate"].HeaderText = "注册日期";
    }

    private void DoQuery()
    {
        _dgv.DataSource = _biz.Search(_txtQueryID.Text.Trim(), _txtQueryName.Text.Trim());
        SetChineseHeaders();
    }

    private void DoShowAll()
    {
        _txtQueryID.Clear();
        _txtQueryName.Clear();
        LoadData();
    }

    private void BindEditForm()
    {
        if (_isLoading) return;
        if (_dgv.CurrentRow?.DataBoundItem is not ReaderInfo item) return;
        _txtReaderID.Text = item.ReaderID;
        _txtReaderName.Text = item.ReaderName;
        _cboSex.SelectedItem = item.ReaderSex;
        _txtPhone.Text = item.Phone;
        _txtDepartment.Text = item.Department;
        _dtpRegisterDate.Value = item.RegisterDate ?? DateTime.Today;
        _txtReaderID.ReadOnly = true;
    }

    private ReaderInfo BuildEntity()
    {
        return new ReaderInfo
        {
            ReaderID = _txtReaderID.Text.Trim(),
            ReaderName = _txtReaderName.Text.Trim(),
            ReaderSex = _cboSex.SelectedItem?.ToString(),
            Phone = _txtPhone.Text.Trim(),
            Department = _txtDepartment.Text.Trim(),
            RegisterDate = _dtpRegisterDate.Value.Date
        };
    }

    private void DoAdd()
    {
        try
        {
            _biz.Add(BuildEntity());
            UiHelper.Info("新增成功");
            LoadData();
            DoClear();
        }
        catch (BusinessException ex) { UiHelper.Error(ex.Message); }
        catch (Exception ex) { UiHelper.Error($"操作失败：{ex.Message}"); }
    }

    private void DoUpdate()
    {
        try
        {
            _biz.Update(BuildEntity());
            UiHelper.Info("修改成功");
            LoadData();
        }
        catch (BusinessException ex) { UiHelper.Error(ex.Message); }
        catch (Exception ex) { UiHelper.Error($"操作失败：{ex.Message}"); }
    }

    private void DoDelete()
    {
        if (_dgv.CurrentRow?.DataBoundItem is not ReaderInfo item) return;
        if (!UiHelper.Confirm($"确认删除读者【{item.ReaderName}】吗？")) return;
        try
        {
            _biz.Delete(item.ReaderID);
            UiHelper.Info("删除成功");
            LoadData();
            DoClear();
        }
        catch (BusinessException ex) { UiHelper.Error(ex.Message); }
        catch (Exception ex) { UiHelper.Error($"操作失败：{ex.Message}"); }
    }

    private void DoClear()
    {
        _txtReaderID.Clear();
        _txtReaderName.Clear();
        _cboSex.SelectedIndex = 0;
        _txtPhone.Clear();
        _txtDepartment.Clear();
        _dtpRegisterDate.Value = DateTime.Today;
        _txtReaderID.ReadOnly = false;
        _dgv.ClearSelection();
    }
}
