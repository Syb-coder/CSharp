using System;
using System.Windows.Forms;
using CampusStore.BLL;
using CampusStore.Common;

namespace CampusStore.Forms
{
    /// <summary>
    /// 修改密码窗体
    /// </summary>
    /// <remarks>
    /// 职责：验证旧密码并修改为新密码。
    /// 所有输入框使用密码字符显示，密码校验逻辑由 BLL.ChangePassword 统一处理。
    /// </remarks>
    public partial class FrmChangePassword : Form
    {
        /// <summary>待修改密码的用户 ID</summary>
        private readonly int _userID;

        // ===== 控件字段 =====
        private Label lblOldPassword;
        private Label lblNewPassword;
        private Label lblConfirmPassword;
        private TextBox txtOldPassword;
        private TextBox txtNewPassword;
        private TextBox txtConfirmPassword;
        private Button btnConfirm;
        private Button btnCancel;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="userID">当前用户 ID</param>
        public FrmChangePassword(int userID)
        {
            _userID = userID;
            InitializeComponent();
            BindEvents();
        }

        /// <summary>
        /// 初始化所有控件属性与布局
        /// </summary>
        private void InitializeComponent()
        {
            UiHelper.SetupForm(this, "修改密码", 400, 280);
            // SetupForm 已设置 CenterParent / FixedDialog，符合需求

            // 旧密码行（Y=30，Label 起始 X=50）
            lblOldPassword = UiHelper.CreateLabel("旧密码:", 50, 30);
            txtOldPassword = UiHelper.CreateTextBox(UiHelper.NextX(lblOldPassword), 30, isPassword: true);

            // 新密码行（Y=70）
            lblNewPassword = UiHelper.CreateLabel("新密码:", 50, 70);
            txtNewPassword = UiHelper.CreateTextBox(UiHelper.NextX(lblNewPassword), 70, isPassword: true);

            // 确认新密码行（Y=110）
            lblConfirmPassword = UiHelper.CreateLabel("确认密码:", 50, 110);
            txtConfirmPassword = UiHelper.CreateTextBox(UiHelper.NextX(lblConfirmPassword), 110, isPassword: true);

            // 按钮行（Y=160）
            btnConfirm = UiHelper.CreatePrimaryButton("确认", 120, 160, 80, 30);
            btnCancel = UiHelper.CreateSecondaryButton("取消", 210, 160, 80, 30);

            this.Controls.AddRange(new Control[]
            {
                lblOldPassword, txtOldPassword,
                lblNewPassword, txtNewPassword,
                lblConfirmPassword, txtConfirmPassword,
                btnConfirm, btnCancel
            });
        }

        /// <summary>
        /// 绑定事件（在 InitializeComponent 之后调用）
        /// </summary>
        private void BindEvents()
        {
            btnConfirm.Click += BtnConfirm_Click;
            btnCancel.Click += BtnCancel_Click;
        }

        /// <summary>
        /// 确认按钮：调用 BLL 修改密码
        /// </summary>
        private void BtnConfirm_Click(object sender, EventArgs e)
        {
            try
            {
                string oldPassword = txtOldPassword.Text;
                string newPassword = txtNewPassword.Text;
                string confirmPassword = txtConfirmPassword.Text;

                // 业务层负责校验旧密码正确性、新旧密码一致性、密码强度等
                new UserManager().ChangePassword(_userID, oldPassword, newPassword, confirmPassword);

                MessageBox.Show("密码修改成功", "提示",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.Close();
            }
            catch (Exception ex)
            {
                UiHelper.HandleException(ex, this);
            }
        }

        /// <summary>
        /// 取消按钮：关闭窗体
        /// </summary>
        private void BtnCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
