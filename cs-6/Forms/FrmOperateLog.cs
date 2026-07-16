using HotelSys.BLL;
using HotelSys.Common;
using HotelSys.Models;

namespace HotelSys.Forms;

/// <summary>
/// 操作日志查询窗体（PRD 4.2.13 / F-22 / AC-23）
/// 只读查询，日志不可修改、不可删除
/// 按用户名/操作类型/时间范围筛选
/// </summary>
public class FrmOperateLog : Form
{
    private readonly LogManager _mgr = new();
    private readonly UserInfo _currentUser;

    private GroupBox _queryBox;
    private DataGridView _dgv;
    private TextBox _txtQueryUser;
    private ComboBox _cboQueryType;
    private DateTimePicker _dtpFrom, _dtpTo;
    private Button _btnQuery, _btnClear;

    public FrmOperateLog(UserInfo currentUser) : this()
    {
        _currentUser = currentUser;
    }

    public FrmOperateLog()
    {
        DoubleBuffered = true;
        InitializeUI();
        Load += (_, _) => LoadData();
    }

    private void InitializeUI()
    {
        Text = "操作日志";
        BackColor = ThemeColor.BgPage;
        Font = UiHelper.DefaultFont;

        _queryBox = UiHelper.CreateStyledGroupBox("查询");
        _queryBox.Dock = DockStyle.Top;
        BuildQueryArea(_queryBox);

        _dgv = new DataGridView { Dock = DockStyle.Fill };
        UiHelper.StyleDataGridView(_dgv);
        BuildGridColumns();

        Controls.Add(_dgv);
        Controls.Add(_queryBox);

        _btnQuery.Click += (_, _) => LoadData();
        _btnClear.Click += (_, _) => ClearQuery();

        Resize += (_, _) => AdjustLayoutHeights();
        Shown += (_, _) => AdjustLayoutHeights();
    }

    private void AdjustLayoutHeights()
    {
        _queryBox.Height = UiHelper.CalcQueryHeightByGroupBox(_queryBox);
    }

    private void BuildQueryArea(GroupBox container)
    {
        _txtQueryUser = UiHelper.CreateTextBox(100);
        _cboQueryType = UiHelper.CreateComboBox(100);
        _dtpFrom = UiHelper.CreateDateTimePicker(130);
        _dtpTo = UiHelper.CreateDateTimePicker(130);
        _btnQuery = UiHelper.CreatePrimaryButton("查询");
        _btnClear = UiHelper.CreateSecondaryButton("清空");

        var pairs = new List<UiHelper.QueryPair>
        {
            new("用户名：", _txtQueryUser),
            new("操作类型：", _cboQueryType),
            new("从：", _dtpFrom),
            new("到：", _dtpTo),
        };

        FlowLayoutPanel flow = UiHelper.BuildQueryPanel(pairs, _btnQuery, _btnClear);
        container.Controls.Add(flow);

        LoadQueryTypes();
        _dtpFrom.Value = DateTime.Today.AddDays(-7);
        _dtpTo.Value = DateTime.Today;
    }

    /// <summary>加载操作类型下拉框</summary>
    private void LoadQueryTypes()
    {
        _cboQueryType.Items.Add("全部");
        _cboQueryType.Items.AddRange(new object[]
        {
            BusinessConstants.LOG_LOGIN, BusinessConstants.LOG_LOGOUT,
            BusinessConstants.LOG_CHECKIN, BusinessConstants.LOG_CHECKOUT,
            BusinessConstants.LOG_EXTEND, BusinessConstants.LOG_CHANGE_ROOM,
            BusinessConstants.LOG_RESERVE, BusinessConstants.LOG_CANCEL_RESERVE,
            BusinessConstants.LOG_CONSUME, BusinessConstants.LOG_SET_MAINTENANCE,
            BusinessConstants.LOG_RESTORE_FREE,
            BusinessConstants.LOG_ADD, BusinessConstants.LOG_UPDATE, BusinessConstants.LOG_DELETE
        });
        _cboQueryType.SelectedIndex = 0;
    }

    private void BuildGridColumns()
    {
        _dgv.Columns.Add("LogID", "编号");
        _dgv.Columns.Add("UserName", "用户名");
        _dgv.Columns.Add("OperateTime", "操作时间");
        _dgv.Columns.Add("OperateType", "操作类型");
        _dgv.Columns.Add("OperateContent", "操作内容");
        _dgv.Columns.Add("Detail", "详情");
    }

    private void LoadData()
    {
        try
        {
            string userName = _txtQueryUser?.Text.Trim();
            string operateType = _cboQueryType?.SelectedIndex > 0 ? _cboQueryType.SelectedItem.ToString() : null;
            DateTime? from = _dtpFrom?.Value.Date;
            DateTime? to = _dtpTo?.Value.Date.AddDays(1);

            List<OperateLogInfo> list = _mgr.Search(userName, operateType, from, to);
            _dgv.Rows.Clear();
            foreach (var item in list)
            {
                _dgv.Rows.Add(item.LogID, item.UserName, item.OperateTime.ToString("yyyy-MM-dd HH:mm:ss"),
                    item.OperateType, item.OperateContent, item.Detail ?? "");
            }
        }
        catch (Exception ex)
        {
            UiHelper.Error("查询失败：" + ex.Message);
        }
    }

    private void ClearQuery()
    {
        _txtQueryUser.Text = "";
        _cboQueryType.SelectedIndex = 0;
        _dtpFrom.Value = DateTime.Today.AddDays(-7);
        _dtpTo.Value = DateTime.Today;
        LoadData();
    }
}
