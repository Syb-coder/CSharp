using LibraryManagement.Common;
using LibraryManagement.DAL;
using LibraryManagement.Models;

namespace LibraryManagement.BLL;

/// <summary>
/// 用户业务服务类，处理登录验证、用户管理、密码修改等业务逻辑
/// </summary>
public class UserService
{
    private readonly UserDAL _userDAL = new();

    /// <summary>
    /// 用户登录验证
    /// </summary>
    /// <param name="userName">用户名</param>
    /// <param name="password">明文密码</param>
    /// <param name="role">选择的身份（管理员/普通用户）</param>
    /// <returns>验证通过返回 User 实体</returns>
    /// <exception cref="BusinessException">用户不存在、密码错误或身份不匹配</exception>
    public User Login(string userName, string password, string role)
    {
        // 卫语句：校验输入非空
        if (ValidationHelper.IsNullOrWhiteSpace(userName))
        {
            throw new BusinessException("请输入用户名");
        }
        if (ValidationHelper.IsNullOrWhiteSpace(password))
        {
            throw new BusinessException("请输入密码");
        }
        if (ValidationHelper.IsNullOrWhiteSpace(role))
        {
            throw new BusinessException("请选择身份");
        }

        User user = _userDAL.GetUserByName(userName);
        if (user == null)
        {
            throw new BusinessException("用户不存在");
        }

        // 验证密码哈希
        if (!PasswordHelper.Verify(password, user.UserPassword))
        {
            throw new BusinessException("密码错误");
        }

        // 验证身份是否一致
        if (user.UserPurview != role)
        {
            throw new BusinessException("身份信息不符");
        }

        return user;
    }

    /// <summary>
    /// 查询全部用户
    /// </summary>
    /// <returns>用户列表</returns>
    public List<User> GetAllUsers()
    {
        return _userDAL.GetAllUsers();
    }

    /// <summary>
    /// 新增用户
    /// </summary>
    /// <param name="userName">用户名</param>
    /// <param name="password">明文密码</param>
    /// <param name="confirmPassword">确认密码</param>
    /// <param name="role">权限</param>
    /// <exception cref="BusinessException">校验失败</exception>
    public void AddUser(string userName, string password, string confirmPassword, string role)
    {
        if (ValidationHelper.IsNullOrWhiteSpace(userName))
        {
            throw new BusinessException("用户名不能为空");
        }
        if (ValidationHelper.IsExceedLength(userName, 16))
        {
            throw new BusinessException("用户名长度不能超过16个字符");
        }
        if (ValidationHelper.IsNullOrWhiteSpace(password))
        {
            throw new BusinessException("密码不能为空");
        }
        if (ValidationHelper.IsExceedLength(password, 16))
        {
            throw new BusinessException("密码长度不能超过16个字符");
        }
        if (password != confirmPassword)
        {
            throw new BusinessException("两次输入的密码不一致");
        }
        if (role != BusinessConstants.ROLE_ADMIN && role != BusinessConstants.ROLE_USER)
        {
            throw new BusinessException("权限必须为管理员或普通用户");
        }

        User user = new()
        {
            UserName = userName,
            UserPassword = PasswordHelper.ComputeHash(password),
            UserPurview = role
        };

        if (!_userDAL.InsertUser(user))
        {
            throw new BusinessException("用户名已存在");
        }
    }

    /// <summary>
    /// 修改用户密码和权限
    /// </summary>
    /// <param name="userName">用户名</param>
    /// <param name="password">新密码（为空则不修改密码）</param>
    /// <param name="role">权限</param>
    /// <exception cref="BusinessException">校验失败</exception>
    public void UpdateUser(string userName, string password, string role)
    {
        User existingUser = _userDAL.GetUserByName(userName)
            ?? throw new BusinessException("用户不存在");

        if (role != BusinessConstants.ROLE_ADMIN && role != BusinessConstants.ROLE_USER)
        {
            throw new BusinessException("权限必须为管理员或普通用户");
        }

        // 密码为空表示不修改密码，保留原密码
        string passwordHash = existingUser.UserPassword;
        if (!ValidationHelper.IsNullOrWhiteSpace(password))
        {
            if (ValidationHelper.IsExceedLength(password, 16))
            {
                throw new BusinessException("密码长度不能超过16个字符");
            }
            passwordHash = PasswordHelper.ComputeHash(password);
        }

        User user = new()
        {
            UserName = userName,
            UserPassword = passwordHash,
            UserPurview = role
        };

        _userDAL.UpdateUser(user);
    }

    /// <summary>
    /// 删除用户（不允许删除当前登录用户）
    /// </summary>
    /// <param name="userName">待删除的用户名</param>
    /// <param name="currentUserName">当前登录用户名</param>
    /// <exception cref="BusinessException">校验失败</exception>
    public void DeleteUser(string userName, string currentUserName)
    {
        if (userName == currentUserName)
        {
            throw new BusinessException("不允许删除当前登录用户自身账号");
        }

        if (!_userDAL.DeleteUser(userName))
        {
            throw new BusinessException("用户不存在或已被删除");
        }
    }

    /// <summary>
    /// 修改密码
    /// </summary>
    /// <param name="userName">用户名</param>
    /// <param name="oldPassword">旧密码</param>
    /// <param name="newPassword">新密码</param>
    /// <param name="confirmPassword">确认新密码</param>
    /// <exception cref="BusinessException">校验失败</exception>
    public void ChangePassword(string userName, string oldPassword, string newPassword, string confirmPassword)
    {
        User user = _userDAL.GetUserByName(userName)
            ?? throw new BusinessException("用户不存在");

        if (!PasswordHelper.Verify(oldPassword, user.UserPassword))
        {
            throw new BusinessException("旧密码错误");
        }

        if (ValidationHelper.IsNullOrWhiteSpace(newPassword))
        {
            throw new BusinessException("新密码不能为空");
        }
        if (ValidationHelper.IsExceedLength(newPassword, 16))
        {
            throw new BusinessException("新密码长度不能超过16个字符");
        }
        if (newPassword != confirmPassword)
        {
            throw new BusinessException("两次输入的新密码不一致");
        }

        _userDAL.UpdatePassword(userName, PasswordHelper.ComputeHash(newPassword));
    }
}
