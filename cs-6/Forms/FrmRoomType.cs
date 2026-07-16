using HotelSys.BLL;
using HotelSys.Common;
using HotelSys.Models;

namespace HotelSys.Forms;

/// <summary>
/// 客房类型管理窗体（PRD 4.2.4 / F-01 / AC-05）
/// 标准增删改查：查询区(Top) + DataGridView(Fill) + 编辑区+按钮(Bottom)
/// 删除前由 BLL 校验关联客房，校验不通过抛 BusinessException
/// </summary>
public class FrmRoomType : Form
{
    private readonly RoomTypeManager _mgr = new();
    private readonly UserInfo _currentUser;

    private DataGridView _dgv;
    private TextBox _txtTypeID, _txtTypeName, _txtPrice, _txtBedCount, _txtDescription, _txtQuery;
    private Button _btnAdd, _btnUpdate, _btnDelete, _btnQuery, _btnClear;
    private bool _isLoading;

    /// <summary>主窗体调用构造函数</summary>
    public FrmRoomType(UserInfo currentUser) : this()
    {
        _currentUser = currentUser;
    }

    /// <summary>默认构造函数</summary>
    public FrmRoomType()
    {
        DoubleBuffered = true;
        InitializeUI();
        Load += (_, _) => LoadData();
    }

    private GroupBox _queryBox, _editBox;
    private List<UiHelper.FieldDef> _editFields;
    private Button _btnRefresh;

    private void InitializeUI()
    {
        Text = "客房类型管理";
        BackColor = ThemeColor.BgPage;
        Font = UiHelper.DefaultFont;

        // 查询区（Dock=Top）
        _queryBox = UiHelper.CreateStyledGroupBox("查询");
        _queryBox.Dock = DockStyle.Top;
        BuildQueryArea(_queryBox);

        // 编辑区+按钮（Dock=Bottom）
        _editBox = UiHelper.CreateStyledGroupBox("编辑");
        _editBox.Dock = DockStyle.Bottom;
        BuildEditArea(_editBox);

        // 数据表格（Dock=Fill）
        _dgv = new DataGridView { Dock = DockStyle.Fill };
        UiHelper.StyleDataGridView(_dgv);
        BuildGridColumns();

        // Dock 添加顺序：Fill 先 → Bottom 次 → Top 最后（WinForms Z-order 规则）
        Controls.Add(_dgv);
        Controls.Add(_editBox);
        Controls.Add(_queryBox);

        _dgv.SelectionChanged += (_, _) => OnRowSelected();
        _btnQuery.Click += (_, _) => LoadData();
        _btnClear.Click += (_, _) => ClearEdit();
        _btnAdd.Click += (_, _) => DoAdd();
        _btnUpdate.Click += (_, _) => DoUpdate();
        _btnDelete.Click += (_, _) => DoDelete();

        // 响应式高度：窗口大小变化时重新计算 GroupBox 高度
        Resize += (_, _) => AdjustLayoutHeights();
        Shown += (_, _) => AdjustLayoutHeights();
    }

    private void AdjustLayoutHeights()
    {
        _queryBox.Height = UiHelper.CalcQueryHeightByGroupBox(_queryBox);
        LayoutEditArea(_editBox);
    }

    /// <summary>构建查询区：响应式 FlowLayoutPanel</summary>
    private void BuildQueryArea(GroupBox container)
    {
        _txtQuery = UiHelper.CreateTextBox(200);
        _btnQuery = UiHelper.CreatePrimaryButton("查询");
        _btnClear = UiHelper.CreateSecondaryButton("清空");

        var pairs = new List<UiHelper.QueryPair>
        {
            new("房型名称：", _txtQuery),
        };
        FlowLayoutPanel flow = UiHelper.BuildQueryPanel(pairs, _btnQuery, _btnClear);
        container.Controls.Add(flow);
    }

    /// <summary>构建编辑区：5个字段 + 4个操作按钮</summary>
    private void BuildEditArea(GroupBox container)
    {
        _txtTypeID = UiHelper.CreateTextBox(150);
        _txtTypeName = UiHelper.CreateTextBox(150);
        _txtPrice = UiHelper.CreateTextBox(150);
        _txtBedCount = UiHelper.CreateTextBox(150);
        _txtDescription = UiHelper.CreateTextBox(300);

        _editFields = new List<UiHelper.FieldDef>
        {
            new("类型编号：", _txtTypeID),
            new("类型名称：", _txtTypeName),
            new("单价(元/天)：", _txtPrice),
            new("床位数：", _txtBedCount),
            new("描述：", _txtDescription),
        };

        _btnAdd = UiHelper.CreatePrimaryButton("新增");
        _btnUpdate = UiHelper.CreatePrimaryButton("修改");
        _btnDelete = UiHelper.CreateDangerButton("删除");
        _btnRefresh = UiHelper.CreateSecondaryButton("刷新");
        _btnRefresh.Click += (_, _) => LoadData();

        LayoutEditArea(container);
    }

    /// <summary>按当前容器宽度重新布局编辑区（控件 + 按钮）</summary>
    private void LayoutEditArea(GroupBox container)
    {
        container.Controls.Clear();
        int containerW = container.Width - 16;
        int btnY = UiHelper.LayoutFields(container.Controls, _editFields, containerW, 0, 20, 30);

        UiHelper.LayoutButtonsWithReturn(
            container.Controls,
            new[] { _btnAdd, _btnUpdate, _btnDelete },
            _btnRefresh, 20, btnY + 10, containerW);

        container.Height = UiHelper.CalcEditHeight(_editFields.Count, UiHelper.CalcPairsPerRow(containerW));
    }

    /// <summary>配置 DataGridView 列</summary>
    private void BuildGridColumns()
    {
        _dgv.Columns.Add("TypeID", "类型编号");
        _dgv.Columns.Add("TypeName", "类型名称");
        _dgv.Columns.Add("Price", "单价(元/天)");
        _dgv.Columns.Add("BedCount", "床位数");
        _dgv.Columns.Add("Description", "描述");
    }

    /// <summary>加载数据到表格</summary>
    private void LoadData()
    {
        try
        {
            _isLoading = true;
            List<RoomTypeInfo> list;
            string keyword = _txtQuery?.Text.Trim();
            if (string.IsNullOrEmpty(keyword))
                list = _mgr.GetAll();
            else
                list = _mgr.GetAll().FindAll(r => (r.TypeName ?? "").Contains(keyword));
            _dgv.Rows.Clear();
            foreach (var item in list)
            {
                _dgv.Rows.Add(item.TypeID, item.TypeName, item.Price, item.BedCount, item.Description ?? "");
            }
            ClearEdit();
        }
        catch (Exception ex)
        {
            UiHelper.Error("查询失败：" + ex.Message);
        }
        finally
        {
            _isLoading = false;
        }
    }

    /// <summary>表格行选中时填充编辑区</summary>
    private void OnRowSelected()
    {
        if (_isLoading) return;
        if (_dgv.CurrentRow == null || _dgv.CurrentRow.Index < 0) return;

        // 修改时主键不可编辑
        _txtTypeID.Text = _dgv.CurrentRow.Cells["TypeID"].Value?.ToString();
        _txtTypeID.Enabled = false;
        _txtTypeName.Text = _dgv.CurrentRow.Cells["TypeName"].Value?.ToString();
        _txtPrice.Text = _dgv.CurrentRow.Cells["Price"].Value?.ToString();
        _txtBedCount.Text = _dgv.CurrentRow.Cells["BedCount"].Value?.ToString();
        _txtDescription.Text = _dgv.CurrentRow.Cells["Description"].Value?.ToString();
    }

    /// <summary>清空编辑区，恢复新增模式</summary>
    private void ClearEdit()
    {
        _isLoading = true;
        _txtTypeID.Text = "";
        _txtTypeID.Enabled = true;
        _txtTypeName.Text = "";
        _txtPrice.Text = "";
        _txtBedCount.Text = "";
        _txtDescription.Text = "";
        _isLoading = false;
    }

    /// <summary>从编辑区构建实体</summary>
    private RoomTypeInfo BuildEntityFromInput()
    {
        return new RoomTypeInfo
        {
            TypeID = _txtTypeID.Text.Trim(),
            TypeName = _txtTypeName.Text.Trim(),
            Price = decimal.TryParse(_txtPrice.Text.Trim(), out decimal p) ? p : 0,
            BedCount = int.TryParse(_txtBedCount.Text.Trim(), out int b) ? b : 0,
            Description = _txtDescription.Text.Trim()
        };
    }

    private void DoAdd()
    {
        try
        {
            _mgr.Add(BuildEntityFromInput());
            UiHelper.Info("新增成功");
            LoadData();
        }
        catch (BusinessException ex) { UiHelper.Warning(ex.Message); }
        catch (Exception ex) { UiHelper.Error("新增失败：" + ex.Message); }
    }

    private void DoUpdate()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(_txtTypeID.Text))
            {
                UiHelper.Warning("请先选择要修改的记录");
                return;
            }
            _mgr.Update(BuildEntityFromInput());
            UiHelper.Info("修改成功");
            LoadData();
        }
        catch (BusinessException ex) { UiHelper.Warning(ex.Message); }
        catch (Exception ex) { UiHelper.Error("修改失败：" + ex.Message); }
    }

    private void DoDelete()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(_txtTypeID.Text))
            {
                UiHelper.Warning("请先选择要删除的记录");
                return;
            }
            if (!UiHelper.Confirm($"确认删除房型 {_txtTypeName.Text} 吗？")) return;
            _mgr.Delete(_txtTypeID.Text.Trim());
            UiHelper.Info("删除成功");
            LoadData();
        }
        catch (BusinessException ex) { UiHelper.Warning(ex.Message); }
        catch (Exception ex) { UiHelper.Error("删除失败：" + ex.Message); }
    }
}
