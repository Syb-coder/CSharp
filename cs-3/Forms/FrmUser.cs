using CampusMart.BLL;
using CampusMart.Common;
using CampusMart.Models;

namespace CampusMart.Forms;

/// <summary>
/// 用户管理窗体：提供用户的增删改查功能
/// </summary>
/// <remarks>
/// 删除用户时禁止删除当前登录用户自身账号（传递 currentUser.UserID 给 BLL 校验）。
/// 密码字段：新增时必填，修改时留空表示不修改密码。
/// Label 宽度使用 UiHelper 动态测量，多列采用链式布局。
/// </remarks>
public class FrmUser : Form
{
    private readonly UserInfo _currentUser;
    private readonly UserBiz _userBiz = new();

    private readonly DataGridView _dgvUsers;

    private readonly TextBox _txtUserID;
    private readonly TextBox _txtLoginName;
    private readonly TextBox _txtRealName;
    private readonly TextBox _txtPassword;
    private readonly ComboBox _cmbRole;

    private readonly Button _btnAdd;
    private readonly Button _btnUpdate;
    private readonly Button _btnDelete;
    private readonly Button _btnClear;
    private readonly Button _btnReturn;

    private const int CtrlHeight = 25;

    /// <summary>
    /// 构造用户管理窗体
    /// </summary>
    /// <param name="currentUser">当前登录用户，删除时禁止删除自身</param>
    public FrmUser(UserInfo currentUser)
    {
        _currentUser = currentUser;

        Text = "用户管理";
        Size = new Size(800, 550);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        BackColor = Color.FromArgb(245, 247, 250);

        Font labelFont = new("Microsoft YaHei UI", 9F);

        // ===== DataGridView =====
        _dgvUsers = new DataGridView
        {
            Font = labelFont,
            Size = new Size(764, 280),
            Location = new Point(10, 10),
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
        _dgvUsers.ColumnHeadersDefaultCellStyle.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold);
        _dgvUsers.Columns.AddRange(new DataGridViewColumn[]
        {
            new DataGridViewTextBoxColumn { Name = "ColUserID", HeaderText = "用户编号", DataPropertyName = "UserID", Width = 80 },
            new DataGridViewTextBoxColumn { Name = "ColLoginName", HeaderText = "用户名", DataPropertyName = "LoginName", Width = 150 },
            new DataGridViewTextBoxColumn { Name = "ColRealName", HeaderText = "真实姓名", DataPropertyName = "RealName", Width = 150 },
            new DataGridViewTextBoxColumn
            {
                Name = "ColRole", HeaderText = "角色", DataPropertyName = "Role", Width = 100,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
            }
        });
        _dgvUsers.SelectionChanged += DgvUsers_SelectionChanged;

        // ===== 输入区 GroupBox =====
        GroupBox grpInput = new()
        {
            Text = "用户信息",
            Font = labelFont,
            Size = new Size(764, 150),
            Location = new Point(10, 300)
        };

        const int pMargin = 20;
        const int pGap = 8;

        // 第1行：用户编号、用户名
        const int pRow1Y = 30;
        Label lblUserID = UiHelper.CreateLabel("用户编号：", pMargin, pRow1Y, labelFont);
        _txtUserID = new TextBox
        {
            Font = labelFont,
            Size = new Size(150, CtrlHeight),
            Location = new Point(UiHelper.NextX(lblUserID), pRow1Y - 3),
            ReadOnly = true,
            BackColor = Color.FromArgb(240, 240, 240)
        };

        int p1Col2X = _txtUserID.Right + pGap + 40;
        Label lblLoginName = UiHelper.CreateLabel("用户名：", p1Col2X, pRow1Y, labelFont);
        _txtLoginName = new TextBox
        {
            Font = labelFont,
            Size = new Size(150, CtrlHeight),
            Location = new Point(UiHelper.NextX(lblLoginName), pRow1Y - 3)
        };

        // 第2行：真实姓名、角色
        const int pRow2Y = 65;
        Label lblRealName = UiHelper.CreateLabel("真实姓名：", pMargin, pRow2Y, labelFont);
        _txtRealName = new TextBox
        {
            Font = labelFont,
            Size = new Size(150, CtrlHeight),
            Location = new Point(UiHelper.NextX(lblRealName), pRow2Y - 3)
        };

        int p2Col2X = _txtRealName.Right + pGap + 40;
        Label lblRole = UiHelper.CreateLabel("角色：", p2Col2X, pRow2Y, labelFont);
        _cmbRole = new ComboBox
        {
            Font = labelFont,
            Size = new Size(150, CtrlHeight),
            Location = new Point(UiHelper.NextX(lblRole), pRow2Y - 3),
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        _cmbRole.Items.AddRange(new object[] { RoleConstants.ADMIN, RoleConstants.OPERATOR });

        // 第3行：密码、提示
        const int pRow3Y = 100;
        Label lblPassword = UiHelper.CreateLabel("密码：", pMargin, pRow3Y, labelFont);
        _txtPassword = new TextBox
        {
            Font = labelFont,
            Size = new Size(150, CtrlHeight),
            Location = new Point(UiHelper.NextX(lblPassword), pRow3Y - 3),
            UseSystemPasswordChar = true
        };

        Label lblPasswordTip = new()
        {
            Text = "新增时必填，修改时留空表示不修改密码",
            Font = new Font("Microsoft YaHei UI", 8F, FontStyle.Italic),
            ForeColor = Color.Gray,
            AutoSize = true,
            Location = new Point(_txtPassword.Right + 10, pRow3Y),
            TextAlign = ContentAlignment.MiddleLeft
        };

        grpInput.Controls.AddRange(new Control[]
        {
            lblUserID, _txtUserID,
            lblLoginName, _txtLoginName,
            lblRealName, _txtRealName,
            lblRole, _cmbRole,
            lblPassword, _txtPassword,
            lblPasswordTip
        });

        // ===== 操作按钮区 =====
        const int btnY = 460;
        _btnAdd = UiHelper.CreateButton("添加", 20, btnY - 5, labelFont);
        _btnAdd.Click += BtnAdd_Click;
        _btnUpdate = UiHelper.CreateButton("修改", _btnAdd.Right + 10, btnY - 5, labelFont);
        _btnUpdate.Click += BtnUpdate_Click;
        _btnDelete = UiHelper.CreateButton("删除", _btnUpdate.Right + 10, btnY - 5, labelFont);
        _btnDelete.Click += BtnDelete_Click;
        _btnClear = UiHelper.CreateButton("清空", _btnDelete.Right + 10, btnY - 5, labelFont);
        _btnClear.Click += BtnClear_Click;
        _btnReturn = UiHelper.CreateButton("返回", 660, btnY - 5, labelFont);
        _btnReturn.BackColor = Color.FromArgb(200, 200, 200);
        _btnReturn.Click += BtnReturn_Click;

        Controls.AddRange(new Control[]
        {
            _dgvUsers,
            grpInput,
            _btnAdd, _btnUpdate, _btnDelete, _btnClear, _btnReturn
        });

        LoadUsers();
    }

    private void LoadUsers()
    {
        _dgvUsers.DataSource = _userBiz.GetAllUsers();
    }

    private void DgvUsers_SelectionChanged(object sender, EventArgs e)
    {
        if (_dgvUsers.CurrentRow == null || _dgvUsers.CurrentRow.DataBoundItem == null)
        {
            return;
        }

        UserInfo user = (UserInfo)_dgvUsers.CurrentRow.DataBoundItem;
        _txtUserID.Text = user.UserID.ToString();
        _txtLoginName.Text = user.LoginName;
        _txtRealName.Text = user.RealName;
        SelectRoleItem(user.Role);
        _txtPassword.Clear();
    }

    private void SelectRoleItem(string role)
    {
        for (int i = 0; i < _cmbRole.Items.Count; i++)
        {
            if (_cmbRole.Items[i].ToString() == role)
            {
                _cmbRole.SelectedIndex = i;
                return;
            }
        }
        _cmbRole.SelectedIndex = -1;
    }

    private void BtnAdd_Click(object sender, EventArgs e)
    {
        try
        {
            string loginName = _txtLoginName.Text.Trim();
            string password = _txtPassword.Text;
            string realName = _txtRealName.Text.Trim();
            string role = _cmbRole.SelectedItem?.ToString();

            _userBiz.AddUser(loginName, password, password, realName, role);
            MessageBox.Show("添加成功", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            LoadUsers();
            ClearInput();
        }
        catch (BusinessException ex)
        {
            MessageBox.Show(ex.Message, "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"系统错误：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void BtnUpdate_Click(object sender, EventArgs e)
    {
        try
        {
            if (string.IsNullOrEmpty(_txtUserID.Text))
            {
                MessageBox.Show("请先选择要修改的用户", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int userID = int.Parse(_txtUserID.Text.Trim());
            string password = _txtPassword.Text;
            string realName = _txtRealName.Text.Trim();
            string role = _cmbRole.SelectedItem?.ToString();

            _userBiz.UpdateUser(userID, password, realName, role);
            MessageBox.Show("修改成功", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            LoadUsers();
        }
        catch (BusinessException ex)
        {
            MessageBox.Show(ex.Message, "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"系统错误：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void BtnDelete_Click(object sender, EventArgs e)
    {
        try
        {
            if (string.IsNullOrEmpty(_txtUserID.Text))
            {
                MessageBox.Show("请先选择要删除的用户", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (MessageBox.Show("确定要删除该用户吗？", "确认", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            {
                return;
            }

            int userID = int.Parse(_txtUserID.Text.Trim());
            _userBiz.DeleteUser(userID, _currentUser.UserID);
            MessageBox.Show("删除成功", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            LoadUsers();
            ClearInput();
        }
        catch (BusinessException ex)
        {
            MessageBox.Show(ex.Message, "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
        _dgvUsers.ClearSelection();
        _txtUserID.Clear();
        _txtLoginName.Clear();
        _txtRealName.Clear();
        _txtPassword.Clear();
        _cmbRole.SelectedIndex = -1;
    }
}
