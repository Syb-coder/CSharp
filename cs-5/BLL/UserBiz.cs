using LibrarySys.DAL;
using LibrarySys.Models;
using LibrarySys.Common;

namespace LibrarySys.BLL;

/// <summary>
/// 系统用户业务逻辑类
/// </summary>
public class UserBiz
{
    private readonly UserDao _dao = new();

    /// <summary>查询全部用户</summary>
    public List<UserInfo> GetAll() => _dao.FindAll();

    /// <summary>
    /// 用户登录验证：验证用户名、密码和身份三者匹配
    /// </summary>
    /// <param name="userName">用户名</param>
    /// <param name="rawPassword">明文密码</param>
    /// <param name="purview">用户选择的身份（管理员 / 普通用户）</param>
    /// <returns>验证通过返回 UserInfo（含数据库中真实角色），否则 null</returns>
    public UserInfo Login(string userName, string rawPassword, string purview)
    {
        // 登录验证前先将明文密码转为 MD5 哈希，与数据库存储的哈希比对
        string passwordHash = SecurityUtil.ComputeMd5Hash(rawPassword);
        return _dao.ValidateUser(userName, passwordHash, purview);
    }

    /// <summary>
    /// 新增用户
    /// </summary>
    /// <param name="entity">用户实体（UserPassword 字段存明文，方法内加密）</param>
    /// <param name="confirmPassword">确认密码</param>
    /// <exception cref="BusinessException">用户名已存在 / 必填项为空 / 两次密码不一致</exception>
    public void Add(UserInfo entity, string confirmPassword)
    {
        if (string.IsNullOrWhiteSpace(entity.UserName))
            throw new BusinessException("用户名不能为空");
        if (string.IsNullOrWhiteSpace(entity.UserPassword))
            throw new BusinessException("密码不能为空");
        if (string.IsNullOrWhiteSpace(entity.UserPurview))
            throw new BusinessException("请选择权限");
        // 两次密码一致性校验，防止输入错误
        if (entity.UserPassword != confirmPassword)
            throw new BusinessException("两次输入的密码不一致");

        if (_dao.FindById(entity.UserName) != null)
            throw new BusinessException($"用户名 {entity.UserName} 已存在");

        // 密码 MD5 加密后存储，不保留明文
        entity.UserPassword = SecurityUtil.ComputeMd5Hash(entity.UserPassword);
        _dao.Insert(entity);
    }

    /// <summary>
    /// 修改用户信息（支持部分更新：密码为空则保留原密码，权限为空则保留原权限）
    /// </summary>
    /// <param name="entity">用户实体（UserPassword 字段存明文，方法内加密）</param>
    /// <param name="originalUserName">原始用户名（用于定位记录，支持修改用户名）</param>
    /// <exception cref="BusinessException">用户不存在 / 新用户名已存在</exception>
    public void Update(UserInfo entity, string originalUserName)
    {
        // 获取原始用户信息
        UserInfo original = _dao.FindById(originalUserName)
            ?? throw new BusinessException($"用户名 {originalUserName} 不存在");

        // 密码为空则保留原密码（已是哈希值，无需再加密）
        if (string.IsNullOrWhiteSpace(entity.UserPassword))
        {
            entity.UserPassword = original.UserPassword;
        }
        else
        {
            entity.UserPassword = SecurityUtil.ComputeMd5Hash(entity.UserPassword);
        }

        // 权限为空则保留原权限
        if (string.IsNullOrWhiteSpace(entity.UserPurview))
        {
            entity.UserPurview = original.UserPurview;
        }

        // 如果修改了用户名，检查新用户名是否已存在
        if (!string.IsNullOrWhiteSpace(entity.UserName) && entity.UserName != originalUserName)
        {
            if (_dao.FindById(entity.UserName) != null)
                throw new BusinessException($"用户名 {entity.UserName} 已存在");
        }

        _dao.UpdatePartial(entity, originalUserName);
    }

    /// <summary>
    /// 删除用户（不允许删除当前登录用户）
    /// </summary>
    /// <param name="userName">待删除用户名</param>
    /// <param name="currentUserName">当前登录用户名</param>
    /// <exception cref="BusinessException">不能删除当前登录用户</exception>
    public void Delete(string userName, string currentUserName)
    {
        // 防止管理员删除自己的账号导致无法登录
        if (userName == currentUserName)
            throw new BusinessException("不能删除当前登录的用户账号");

        _dao.Delete(userName);
    }

    /// <summary>
    /// 修改当前用户密码
    /// </summary>
    /// <param name="userName">用户名</param>
    /// <param name="oldPassword">旧密码（明文）</param>
    /// <param name="newPassword">新密码（明文）</param>
    /// <param name="confirmPassword">确认新密码</param>
    /// <exception cref="BusinessException">旧密码错误 / 两次新密码不一致</exception>
    public void ChangePassword(string userName, string oldPassword, string newPassword, string confirmPassword)
    {
        UserInfo user = _dao.FindById(userName)
            ?? throw new BusinessException($"用户名 {userName} 不存在");

        // 旧密码校验：将输入的明文加密后与数据库哈希比对
        if (!SecurityUtil.VerifyHash(oldPassword, user.UserPassword))
            throw new BusinessException("旧密码不正确");

        if (string.IsNullOrWhiteSpace(newPassword))
            throw new BusinessException("新密码不能为空");
        if (newPassword != confirmPassword)
            throw new BusinessException("两次输入的新密码不一致");

        user.UserPassword = SecurityUtil.ComputeMd5Hash(newPassword);
        _dao.Update(user);
    }
}
