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
        // 登录验证三步设计：用户存在 -> 密码匹配 -> 身份匹配
        // 逐步校验而非合并校验，是为了给出精确的错误提示（区分"用户不存在"和"密码错误"）
        // 安全考虑：虽然分开提示理论上会暴露用户是否存在，但本系统为内部管理系统，可接受此取舍

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

        // 第一步：查询用户是否存在
        User user = _userDAL.GetUserByName(userName);
        if (user == null)
        {
            throw new BusinessException("用户不存在");
        }

        // 第二步：验证密码哈希（PasswordHelper.Verify 内部对明文做 SHA-256 后与存储的哈希比较）
        if (!PasswordHelper.Verify(password, user.UserPassword))
        {
            throw new BusinessException("密码错误");
        }

        // 第三步：验证身份是否一致（管理员/普通用户必须与注册时的权限匹配）
        // 防止普通用户通过选择"管理员"身份登录获取高权限
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

        // 密码为空表示不修改密码，保留原密码哈希
        // 设计原因：修改用户权限时不应强制要求重设密码，降低操作摩擦
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
        // 安全保护：禁止删除当前登录用户自身账号
        // 否则管理员删除自己后，会话状态与数据库不一致，导致后续操作权限校验异常
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
        // 修改密码校验流程：验证旧密码 -> 校验新密码合法性 -> 确认密码一致性
        // 先验证旧密码确保操作者确实知道当前密码，防止会话劫持后改密码

        User user = _userDAL.GetUserByName(userName)
            ?? throw new BusinessException("用户不存在");

        // 验证旧密码哈希，确认操作者身份
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
        // 确认密码一致性，防止用户输入错误
        if (newPassword != confirmPassword)
        {
            throw new BusinessException("两次输入的新密码不一致");
        }

        _userDAL.UpdatePassword(userName, PasswordHelper.ComputeHash(newPassword));
    }
}
