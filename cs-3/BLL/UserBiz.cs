using CampusMart.Common;
using CampusMart.DAL;
using CampusMart.Models;

namespace CampusMart.BLL;

/// <summary>
/// 用户业务逻辑类，处理登录验证、用户管理、密码修改等业务逻辑
/// </summary>
/// <remarks>
/// 与 cs-2 的 UserService 差异化命名（UserBiz）。
/// 角色取值为"管理员/操作员"（cs-2 为"管理员/普通用户"）。
/// </remarks>
public class UserBiz
{
    private readonly UserDao _userDao = new();

    /// <summary>
    /// 用户登录验证
    /// </summary>
    /// <param name="loginName">登录用户名</param>
    /// <param name="password">明文密码</param>
    /// <param name="role">选择的角色（管理员/操作员）</param>
    /// <returns>验证通过返回 UserInfo 实体</returns>
    /// <exception cref="BusinessException">用户不存在、密码错误或角色不匹配</exception>
    public UserInfo Login(string loginName, string password, string role)
    {
        // 卫语句：校验输入非空，快速失败避免无意义的数据库查询
        if (ValidateUtil.IsNullOrWhiteSpace(loginName))
        {
            throw new BusinessException("请输入用户名");
        }
        if (ValidateUtil.IsNullOrWhiteSpace(password))
        {
            throw new BusinessException("请输入密码");
        }
        if (ValidateUtil.IsNullOrWhiteSpace(role))
        {
            throw new BusinessException("请选择角色");
        }

        // 第一步：查询用户是否存在
        UserInfo user = _userDao.GetUserByLoginName(loginName);
        if (user == null)
        {
            throw new BusinessException("用户不存在");
        }

        // 第二步：验证密码哈希（固定时间比较，防时序攻击）
        if (!SecurityUtil.Verify(password, user.Password))
        {
            throw new BusinessException("密码错误");
        }

        // 第三步：验证角色是否一致，防止操作员通过选择"管理员"角色登录
        if (user.Role != role)
        {
            throw new BusinessException("角色信息不符");
        }

        return user;
    }

    /// <summary>
    /// 查询全部用户
    /// </summary>
    /// <returns>用户列表</returns>
    public List<UserInfo> GetAllUsers()
    {
        return _userDao.GetAllUsers();
    }

    /// <summary>
    /// 新增用户
    /// </summary>
    /// <param name="loginName">登录用户名</param>
    /// <param name="password">明文密码</param>
    /// <param name="confirmPassword">确认密码</param>
    /// <param name="realName">真实姓名</param>
    /// <param name="role">角色</param>
    /// <exception cref="BusinessException">校验失败</exception>
    public void AddUser(string loginName, string password, string confirmPassword, string realName, string role)
    {
        // ===== 输入校验 =====
        if (ValidateUtil.IsNullOrWhiteSpace(loginName))
        {
            throw new BusinessException("用户名不能为空");
        }
        if (ValidateUtil.IsExceedLength(loginName, 20))
        {
            throw new BusinessException("用户名长度不能超过20个字符");
        }
        if (ValidateUtil.IsNullOrWhiteSpace(password))
        {
            throw new BusinessException("密码不能为空");
        }
        // 明文密码长度限制 32 位（足够安全强度），哈希后固定 64 位存库
        if (ValidateUtil.IsExceedLength(password, 32))
        {
            throw new BusinessException("密码长度不能超过32个字符");
        }
        if (password != confirmPassword)
        {
            throw new BusinessException("两次输入的密码不一致");
        }
        if (ValidateUtil.IsExceedLength(realName, 20))
        {
            throw new BusinessException("真实姓名长度不能超过20个字符");
        }
        if (role != RoleConstants.ADMIN && role != RoleConstants.OPERATOR)
        {
            throw new BusinessException("角色必须为管理员或操作员");
        }

        // 构造实体并写入数据库
        UserInfo user = new()
        {
            LoginName = loginName,
            Password = SecurityUtil.ComputeHash(password),
            RealName = realName,
            Role = role
        };

        if (!_userDao.InsertUser(user))
        {
            throw new BusinessException("用户名已存在");
        }
    }

    /// <summary>
    /// 修改用户信息（密码和角色）
    /// </summary>
    /// <param name="userID">用户编号</param>
    /// <param name="password">新密码（为空表示不修改密码）</param>
    /// <param name="realName">真实姓名</param>
    /// <param name="role">角色</param>
    /// <exception cref="BusinessException">校验失败</exception>
    public void UpdateUser(int userID, string password, string realName, string role)
    {
        UserInfo existingUser = _userDao.GetAllUsers().FirstOrDefault(u => u.UserID == userID)
            ?? throw new BusinessException("用户不存在");

        if (ValidateUtil.IsExceedLength(realName, 20))
        {
            throw new BusinessException("真实姓名长度不能超过20个字符");
        }
        if (role != RoleConstants.ADMIN && role != RoleConstants.OPERATOR)
        {
            throw new BusinessException("角色必须为管理员或操作员");
        }

        // 密码为空表示不修改密码，保留原密码哈希
        string passwordHash = existingUser.Password;
        if (!ValidateUtil.IsNullOrWhiteSpace(password))
        {
            if (ValidateUtil.IsExceedLength(password, 32))
            {
                throw new BusinessException("密码长度不能超过32个字符");
            }
            passwordHash = SecurityUtil.ComputeHash(password);
        }

        UserInfo user = new()
        {
            UserID = userID,
            LoginName = existingUser.LoginName, // 登录名不允许修改
            Password = passwordHash,
            RealName = realName,
            Role = role
        };

        _userDao.UpdateUser(user);
    }

    /// <summary>
    /// 删除用户（不允许删除当前登录用户）
    /// </summary>
    /// <param name="userID">待删除的用户编号</param>
    /// <param name="currentUserID">当前登录用户编号</param>
    /// <exception cref="BusinessException">校验失败</exception>
    public void DeleteUser(int userID, int currentUserID)
    {
        if (userID == currentUserID)
        {
            throw new BusinessException("不允许删除当前登录用户自身账号");
        }

        if (!_userDao.DeleteUser(userID))
        {
            throw new BusinessException("用户不存在或已被删除");
        }
    }

    /// <summary>
    /// 修改密码
    /// </summary>
    /// <param name="userID">用户编号</param>
    /// <param name="oldPassword">旧密码</param>
    /// <param name="newPassword">新密码</param>
    /// <param name="confirmPassword">确认新密码</param>
    /// <exception cref="BusinessException">校验失败</exception>
    public void ChangePassword(int userID, string oldPassword, string newPassword, string confirmPassword)
    {
        UserInfo user = _userDao.GetAllUsers().FirstOrDefault(u => u.UserID == userID)
            ?? throw new BusinessException("用户不存在");

        if (!SecurityUtil.Verify(oldPassword, user.Password))
        {
            throw new BusinessException("旧密码错误");
        }

        if (ValidateUtil.IsNullOrWhiteSpace(newPassword))
        {
            throw new BusinessException("新密码不能为空");
        }
        if (ValidateUtil.IsExceedLength(newPassword, 32))
        {
            throw new BusinessException("新密码长度不能超过32个字符");
        }
        if (newPassword != confirmPassword)
        {
            throw new BusinessException("两次输入的新密码不一致");
        }

        _userDao.UpdatePassword(userID, SecurityUtil.ComputeHash(newPassword));
    }
}
