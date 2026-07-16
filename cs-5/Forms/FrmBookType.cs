using LibrarySys.BLL;
using LibrarySys.Common;
using LibrarySys.Models;

namespace LibrarySys.Forms;

/// <summary>
/// 图书类型管理窗体
/// 固定坐标布局：DataGridView(中间) → 编辑区(底部)，无查询区
/// </summary>
public class FrmBookType : Form
{
    private readonly BookTypeBiz _biz = new();
    private readonly UserInfo _currentUser;
    private readonly bool _canEdit;

    private DataGridView _dgv;
    private TextBox _txtTypeId, _txtTypeName;
    private bool _isLoading;

    public FrmBookType()
    {
        _canEdit = true;
        InitializeUI();
        Load += (_, _) => LoadData();
    }

    public FrmBookType(UserInfo currentUser)
    {
        _currentUser = currentUser;
        _canEdit = currentUser.UserPurview == BusinessConstants.ROLE_ADMIN;
        InitializeUI();
        Load += (_, _) => LoadData();
    }

    private void InitializeUI()
    {
        DoubleBuffered = true;
        Text = "图书类型管理";
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(1150, 700);
        MinimumSize = new Size(1100, 680);
        Font = UiHelper.DefaultFont;

        _txtTypeId = UiHelper.CreateTextBox();
        _txtTypeName = UiHelper.CreateTextBox();

        // ===== 底部编辑区（固定坐标） =====
        GroupBox grpEdit = new()
        {
            Text = "类型信息",
            Dock = DockStyle.Bottom,
            Height = 195
        };

        UiHelper.LayoutFields(grpEdit.Controls, new List<UiHelper.FieldDef>
        {
            new("类型编号：", _txtTypeId),
            new("类型名称：", _txtTypeName),
        }, pairsPerRow: 2, startX: 20, startY: 30);

        Button btnAdd = UiHelper.CreateButton("新增"); btnAdd.Enabled = _canEdit;
        Button btnUpdate = UiHelper.CreateButton("修改"); btnUpdate.Enabled = _canEdit;
        Button btnDelete = UiHelper.CreateButton("删除"); btnDelete.Enabled = _canEdit;
        Button btnReset = UiHelper.CreateButton("重置");
        Button btnReturn = UiHelper.CreateButton("返回");
        btnAdd.Click += (_, _) => DoAdd();
        btnUpdate.Click += (_, _) => DoUpdate();
        btnDelete.Click += (_, _) => DoDelete();
        btnReset.Click += (_, _) => DoClear();
        btnReturn.Click += (_, _) => Close();
        UiHelper.LayoutButtonsWithReturn(
            grpEdit.Controls,
            new[] { btnAdd, btnUpdate, btnDelete, btnReset },
            btnReturn, 20, 140, 1150);

        // ===== DataGridView（中间填充） =====
        _dgv = new DataGridView { Dock = DockStyle.Fill };
        UiHelper.StyleDataGridView(_dgv);
        _dgv.SelectionChanged += (_, _) => BindEditForm();

        // 添加顺序：先底部，最后 Fill（无查询区）
        Controls.Add(grpEdit);
        Controls.Add(_dgv);
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
        _dgv.Columns["TypeID"].HeaderText = "类型编号";
        _dgv.Columns["TypeName"].HeaderText = "类型名称";
    }

    private void BindEditForm()
    {
        if (_isLoading) return;
        if (_dgv.CurrentRow?.DataBoundItem is not BookTypeInfo item) return;
        _txtTypeId.Text = item.TypeID;
        _txtTypeName.Text = item.TypeName;
        // 修改时编号不允许编辑
        _txtTypeId.ReadOnly = true;
    }

    private BookTypeInfo BuildEntity()
    {
        return new BookTypeInfo
        {
            TypeID = _txtTypeId.Text.Trim(),
            TypeName = _txtTypeName.Text.Trim()
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
        if (_dgv.CurrentRow?.DataBoundItem is not BookTypeInfo item) return;
        if (!UiHelper.Confirm($"确认删除类型【{item.TypeName}】吗？")) return;
        try
        {
            _biz.Delete(item.TypeID);
            UiHelper.Info("删除成功");
            LoadData();
            DoClear();
        }
        catch (BusinessException ex) { UiHelper.Error(ex.Message); }
        catch (Exception ex) { UiHelper.Error($"操作失败：{ex.Message}"); }
    }

    private void DoClear()
    {
        _txtTypeId.Clear();
        _txtTypeName.Clear();
        _txtTypeId.ReadOnly = false;
        _dgv.ClearSelection();
    }
}
