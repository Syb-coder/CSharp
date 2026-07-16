using CampusMart.BLL;
using CampusMart.Common;
using CampusMart.Models;

namespace CampusMart.Forms;

/// <summary>
/// 商品类别管理窗体（扁平结构，DataGridView 列表展示）
/// </summary>
/// <remarks>
/// 权限控制：操作员禁用增删改按钮，仅允许查询。
/// Label 宽度使用 UiHelper 动态测量，杜绝硬编码像素导致文字截断。
/// 多列输入区采用链式布局：Label1 → TextBox1 → Label2 → TextBox2，每步 X 坐标由前一个控件动态计算。
/// </remarks>
public class FrmCategory : Form
{
    private readonly UserInfo _currentUser;
    private readonly CategoryBiz _categoryBiz = new();

    private readonly TextBox _txtSearchName;
    private readonly Button _btnSearch;

    private readonly DataGridView _dgv;

    private readonly TextBox _txtCategoryID;
    private readonly TextBox _txtCategoryName;
    private readonly TextBox _txtCategoryDesc;
    private readonly DateTimePicker _dtpAddTime;

    private readonly Button _btnAdd;
    private readonly Button _btnUpdate;
    private readonly Button _btnDelete;
    private readonly Button _btnClear;
    private readonly Button _btnReturn;

    /// <summary>
    /// 构造商品类别管理窗体
    /// </summary>
    /// <param name="currentUser">当前登录用户，用于权限控制</param>
    public FrmCategory(UserInfo currentUser)
    {
        _currentUser = currentUser;

        Text = "商品类别管理";
        Size = new Size(800, 560);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        BackColor = Color.FromArgb(245, 247, 250);

        Font labelFont = new("Microsoft YaHei UI", 9F);

        // ===== 查询区 =====
        const int margin = 15;
        const int rowY = 13;
        const int inputGap = 8;

        Label lblSearchName = UiHelper.CreateLabel("类别名称：", margin, rowY, labelFont);
        _txtSearchName = new TextBox
        {
            Font = labelFont,
            Size = new Size(220, 25),
            Location = new Point(UiHelper.NextX(lblSearchName), rowY - 3)
        };
        _btnSearch = UiHelper.CreateButton("查询", _txtSearchName.Right + inputGap, rowY - 5, labelFont);
        _btnSearch.Click += BtnSearch_Click;

        // ===== DataGridView =====
        _dgv = new DataGridView
        {
            Location = new Point(margin, 50),
            Size = new Size(760, 270),
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
            new DataGridViewTextBoxColumn
            {
                HeaderText = "类别编号", DataPropertyName = "CategoryID", Width = 80, ReadOnly = true
            },
            new DataGridViewTextBoxColumn
            {
                HeaderText = "类别名称", DataPropertyName = "CategoryName", Width = 150, ReadOnly = true
            },
            new DataGridViewTextBoxColumn
            {
                HeaderText = "类别描述",
                DataPropertyName = "CategoryDesc",
                Width = 350,
                ReadOnly = true,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
            },
            new DataGridViewTextBoxColumn
            {
                HeaderText = "添加时间",
                DataPropertyName = "AddTime",
                Width = 180,
                ReadOnly = true,
                DefaultCellStyle = new DataGridViewCellStyle { Format = "yyyy-MM-dd HH:mm" }
            }
        });
        _dgv.SelectionChanged += Dgv_SelectionChanged;

        // ===== 输入区 Panel =====
        const int panelY = 330;
        Panel pnlInput = new()
        {
            Location = new Point(margin, panelY),
            Size = new Size(760, 110),
            BackColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle
        };

        // 第一行（Panel 内部坐标）
        const int pRow1Y = 13;
        const int pMargin = 15;
        const int pGap = 15;

        Label lblCategoryID = UiHelper.CreateLabel("类别编号：", pMargin, pRow1Y, labelFont);
        _txtCategoryID = new TextBox
        {
            Font = labelFont,
            Size = new Size(130, 25),
            Location = new Point(UiHelper.NextX(lblCategoryID), pRow1Y - 3),
            ReadOnly = true
        };

        int col2X = _txtCategoryID.Right + pGap;
        Label lblCategoryName = UiHelper.CreateLabel("类别名称：", col2X, pRow1Y, labelFont);
        _txtCategoryName = new TextBox
        {
            Font = labelFont,
            Size = new Size(200, 25),
            Location = new Point(UiHelper.NextX(lblCategoryName), pRow1Y - 3)
        };

        // 第二行
        const int pRow2Y = 43;
        Label lblCategoryDesc = UiHelper.CreateLabel("类别描述：", pMargin, pRow2Y, labelFont);
        _txtCategoryDesc = new TextBox
        {
            Font = labelFont,
            Size = new Size(560, 25),
            Location = new Point(UiHelper.NextX(lblCategoryDesc), pRow2Y - 3)
        };

        // 第三行
        const int pRow3Y = 73;
        Label lblAddTime = UiHelper.CreateLabel("添加时间：", pMargin, pRow3Y, labelFont);
        _dtpAddTime = new DateTimePicker
        {
            Font = labelFont,
            Size = new Size(200, 25),
            Location = new Point(UiHelper.NextX(lblAddTime), pRow3Y - 3),
            Format = DateTimePickerFormat.Custom,
            CustomFormat = "yyyy-MM-dd HH:mm",
            Enabled = false
        };

        pnlInput.Controls.AddRange(new Control[]
        {
            lblCategoryID, _txtCategoryID,
            lblCategoryName, _txtCategoryName,
            lblCategoryDesc, _txtCategoryDesc,
            lblAddTime, _dtpAddTime
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
            lblSearchName, _txtSearchName, _btnSearch,
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
            List<CategoryInfo> list = _categoryBiz.GetAllCategories();
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
            string keyword = _txtSearchName.Text.Trim();
            List<CategoryInfo> list = _categoryBiz.SearchByName(keyword);
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
        CategoryInfo category = _dgv.SelectedRows[0].DataBoundItem as CategoryInfo;
        if (category == null) return;
        _txtCategoryID.Text = category.CategoryID.ToString();
        _txtCategoryName.Text = category.CategoryName;
        _txtCategoryDesc.Text = category.CategoryDesc;
        _dtpAddTime.Value = category.AddTime;
    }

    private void BtnAdd_Click(object sender, EventArgs e)
    {
        if (!string.IsNullOrEmpty(_txtCategoryID.Text))
        {
            MessageBox.Show("请先点击\"清空\"再添加新类别", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        try
        {
            string name = _txtCategoryName.Text.Trim();
            string desc = _txtCategoryDesc.Text.Trim();
            _categoryBiz.AddCategory(name, desc);
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
        if (string.IsNullOrEmpty(_txtCategoryID.Text))
        {
            MessageBox.Show("请先选择要修改的类别", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        try
        {
            int id = int.Parse(_txtCategoryID.Text);
            string name = _txtCategoryName.Text.Trim();
            string desc = _txtCategoryDesc.Text.Trim();
            _categoryBiz.UpdateCategory(id, name, desc);
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
        if (string.IsNullOrEmpty(_txtCategoryID.Text))
        {
            MessageBox.Show("请先选择要删除的类别", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        if (MessageBox.Show("确定要删除该类别吗？", "确认删除", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            return;
        try
        {
            int id = int.Parse(_txtCategoryID.Text);
            _categoryBiz.DeleteCategory(id);
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

    private void ClearInput()
    {
        _dgv.ClearSelection();
        _txtCategoryID.Clear();
        _txtCategoryName.Clear();
        _txtCategoryDesc.Clear();
        _dtpAddTime.Value = DateTime.Now;
    }

    private void ApplyPermission()
    {
        bool isAdmin = _currentUser.Role == RoleConstants.ADMIN;
        _btnAdd.Enabled = isAdmin;
        _btnUpdate.Enabled = isAdmin;
        _btnDelete.Enabled = isAdmin;
    }
}
