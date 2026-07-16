using CampusStore.Common;
using CampusStore.DAL;
using CampusStore.Models;

namespace CampusStore.BLL;

/// <summary>
/// 用户业务逻辑类
/// </summary>
/// <remarks>
/// 职责：输入校验、密码哈希、调用 UserRepository。
/// 密码在 BLL 层进行 SHA-256 哈希后传入 DAL 层，DAL 层不接触明文密码。
/// </remarks>
public class UserManager
{
    private readonly UserRepository _repo = new();

    /// <summary>
    /// 用户登录验证
    /// </summary>
    /// <param name="loginName">登录名</param>
    /// <param name="plainPassword">明文密码</param>
    /// <param name="role">角色（管理员/店员）</param>
    /// <returns>验证通过返回 UserInfo，否则返回 null</returns>
    /// <exception cref="BusinessException">输入为空时抛出</exception>
    public UserInfo Login(string loginName, string plainPassword, string role)
    {
        // 卫语句：前端校验，避免无效数据库请求
        if (ValidateUtil.IsNullOrWhiteSpace(loginName))
            throw new BusinessException("请输入用户名");
        if (ValidateUtil.IsNullOrWhiteSpace(plainPassword))
            throw new BusinessException("请输入密码");
        if (ValidateUtil.IsNullOrWhiteSpace(role))
            throw new BusinessException("请选择身份");

        // 密码哈希后传入存储过程
        string passwordHash = SecurityUtil.Sha256Hash(plainPassword);
        return _repo.Validate(loginName, passwordHash, role);
    }

    /// <summary>
    /// 添加用户
    /// </summary>
    /// <param name="loginName">登录名</param>
    /// <param name="password">明文密码</param>
    /// <param name="confirmPassword">确认密码</param>
    /// <param name="role">角色</param>
    /// <returns>新用户 ID</returns>
    /// <exception cref="BusinessException">输入校验失败或用户名已存在时抛出</exception>
    public int AddUser(string loginName, string password, string confirmPassword, string role)
    {
        if (ValidateUtil.IsNullOrWhiteSpace(loginName))
            throw new BusinessException("请输入用户名");
        if (ValidateUtil.IsExceedLength(loginName, 20))
            throw new BusinessException("用户名长度不能超过 20 个字符");
        if (ValidateUtil.IsNullOrWhiteSpace(password))
            throw new BusinessException("请输入密码");
        if (password != confirmPassword)
            throw new BusinessException("两次输入的密码不一致");
        if (ValidateUtil.IsNullOrWhiteSpace(role))
            throw new BusinessException("请选择权限");

        string passwordHash = SecurityUtil.Sha256Hash(password);
        int result = _repo.Add(loginName, passwordHash, role);
        if (result == 0)
            throw new BusinessException("用户名已存在，请更换");
        return result;
    }

    /// <summary>
    /// 修改用户（密码 + 角色）
    /// </summary>
    public int UpdateUser(int userID, string password, string role)
    {
        if (userID <= 0)
            throw new BusinessException("用户编号无效");
        if (ValidateUtil.IsNullOrWhiteSpace(password))
            throw new BusinessException("请输入密码");
        if (ValidateUtil.IsNullOrWhiteSpace(role))
            throw new BusinessException("请选择权限");

        string passwordHash = SecurityUtil.Sha256Hash(password);
        return _repo.Update(userID, passwordHash, role);
    }

    /// <summary>
    /// 删除用户（不允许删除当前登录用户自身账号）
    /// </summary>
    /// <param name="userID">待删除用户 ID</param>
    /// <param name="currentUserID">当前登录用户 ID</param>
    /// <exception cref="BusinessException">尝试删除自身时抛出</exception>
    public int DeleteUser(int userID, int currentUserID)
    {
        if (userID == currentUserID)
            throw new BusinessException("不允许删除当前登录用户自身账号");
        return _repo.Delete(userID);
    }

    /// <summary>
    /// 修改密码
    /// </summary>
    /// <param name="userID">用户 ID</param>
    /// <param name="oldPassword">旧密码（明文）</param>
    /// <param name="newPassword">新密码（明文）</param>
    /// <param name="confirmPassword">确认新密码</param>
    /// <exception cref="BusinessException">旧密码错误或两次新密码不一致时抛出</exception>
    public void ChangePassword(int userID, string oldPassword, string newPassword, string confirmPassword)
    {
        if (ValidateUtil.IsNullOrWhiteSpace(oldPassword))
            throw new BusinessException("请输入旧密码");
        if (ValidateUtil.IsNullOrWhiteSpace(newPassword))
            throw new BusinessException("请输入新密码");
        if (newPassword != confirmPassword)
            throw new BusinessException("两次输入的新密码不一致");

        // 验证旧密码是否正确
        UserInfo user = _repo.GetByID(userID);
        if (user == null)
            throw new BusinessException("用户不存在");

        string oldHash = SecurityUtil.Sha256Hash(oldPassword);
        if (!string.Equals(oldHash, user.Password, StringComparison.OrdinalIgnoreCase))
            throw new BusinessException("旧密码不正确");

        // 更新密码
        string newHash = SecurityUtil.Sha256Hash(newPassword);
        _repo.Update(userID, newHash, user.Role);
    }

    /// <summary>
    /// 查询全部用户
    /// </summary>
    public List<UserInfo> GetAll() => _repo.GetAll();

    /// <summary>
    /// 按用户编号查询
    /// </summary>
    public UserInfo GetByID(int userID) => _repo.GetByID(userID);
}
