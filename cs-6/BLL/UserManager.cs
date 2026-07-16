using HotelSys.Common;
using HotelSys.DAL;
using HotelSys.Models;

namespace HotelSys.BLL;

/// <summary>
/// 系统用户业务逻辑类（PRD 4.2.1 / 4.2.12）
/// 负责登录验证（含身份选择）、用户增删改、密码修改
/// 密码采用 SHA-256 哈希存储（区别于 cs-5 的 MD5）
/// </summary>
public class UserManager
{
    private readonly UserDao _dao = new();

    /// <summary>
    /// 用户登录验证：校验用户名、密码、身份三者是否匹配（PRD 4.2.1）
    /// </summary>
    /// <param name="userName">用户名</param>
    /// <param name="password">明文密码</param>
    /// <param name="purview">选择的身份（管理员/前台）</param>
    /// <returns>三者均匹配返回 UserInfo，否则 null</returns>
    public UserInfo Login(string userName, string password, string purview)
    {
        // 明文密码先转 SHA-256 哈希，再与数据库存储的哈希比对
        string passwordHash = SecurityUtil.ComputeSha256Hash(password);
        return _dao.ValidateUser(userName, passwordHash, purview);
    }

    /// <summary>查询全部用户</summary>
    public List<UserInfo> GetAll() => _dao.FindAll();

    /// <summary>按用户名查询单条记录</summary>
    public UserInfo GetById(string userName) => _dao.FindById(userName);

    /// <summary>
    /// 新增用户
    /// </summary>
    /// <param name="entity">用户实体（UserPassword 字段存明文，方法内加密）</param>
    /// <param name="confirmPassword">确认密码</param>
    /// <exception cref="BusinessException">必填项为空 / 两次密码不一致 / 用户名已存在</exception>
    public void Add(UserInfo entity, string confirmPassword)
    {
        // 卫语句校验：必填项逐项检查，提前抛出业务异常
        if (string.IsNullOrWhiteSpace(entity.UserName))
            throw new BusinessException("用户名不能为空");
        if (string.IsNullOrWhiteSpace(entity.UserPassword))
            throw new BusinessException("密码不能为空");
        if (string.IsNullOrWhiteSpace(entity.UserPurview))
            throw new BusinessException("请选择权限");

        // 两次密码一致性校验，防止输入错误
        if (entity.UserPassword != confirmPassword)
            throw new BusinessException("两次输入的密码不一致");

        // 用户名唯一性校验
        if (_dao.FindById(entity.UserName) != null)
            throw new BusinessException($"用户名 {entity.UserName} 已存在");

        // 密码 SHA-256 加密后存储，不保留明文
        entity.UserPassword = SecurityUtil.ComputeSha256Hash(entity.UserPassword);
        _dao.Insert(entity);

        LogManager.Record(entity.UserName, BusinessConstants.LOG_ADD, $"新增用户 {entity.UserName}",
            $"权限:{entity.UserPurview}");
    }

    /// <summary>
    /// 修改用户信息（支持部分更新：密码为空则保留原密码）
    /// </summary>
    /// <param name="entity">用户实体（UserPassword 为空则保留原密码）</param>
    /// <param name="originalUserName">原始用户名（用于定位记录，支持修改用户名）</param>
    /// <exception cref="BusinessException">用户不存在 / 新用户名已存在</exception>
    public void Update(UserInfo entity, string originalUserName)
    {
        UserInfo original = _dao.FindById(originalUserName)
            ?? throw new BusinessException($"用户名 {originalUserName} 不存在");

        // 密码为空则保留原密码（已是哈希值，无需再加密）
        if (string.IsNullOrWhiteSpace(entity.UserPassword))
        {
            entity.UserPassword = original.UserPassword;
        }
        else
        {
            entity.UserPassword = SecurityUtil.ComputeSha256Hash(entity.UserPassword);
        }

        // 权限为空则保留原权限
        if (string.IsNullOrWhiteSpace(entity.UserPurview))
        {
            entity.UserPurview = original.UserPurview;
        }

        // 修改用户名时检查新用户名是否已存在
        if (!string.IsNullOrWhiteSpace(entity.UserName) && entity.UserName != originalUserName)
        {
            if (_dao.FindById(entity.UserName) != null)
                throw new BusinessException($"用户名 {entity.UserName} 已存在");
        }

        _dao.UpdatePartial(entity, originalUserName);

        LogManager.Record(CurrentUser.UserName, BusinessConstants.LOG_UPDATE,
            $"修改用户 {originalUserName}", $"新用户名:{entity.UserName}");
    }

    /// <summary>
    /// 删除用户（不允许删除当前登录用户）
    /// </summary>
    /// <param name="userName">待删除用户名</param>
    /// <exception cref="BusinessException">不能删除当前登录用户</exception>
    public void Delete(string userName)
    {
        // 防止管理员删除自己的账号导致无法登录
        if (userName == CurrentUser.UserName)
            throw new BusinessException("不能删除当前登录的用户账号");

        _dao.Delete(userName);

        LogManager.Record(CurrentUser.UserName, BusinessConstants.LOG_DELETE, $"删除用户 {userName}");
    }

    /// <summary>
    /// 修改当前用户密码（PRD 6.5 接口契约）
    /// </summary>
    /// <param name="userName">用户名</param>
    /// <param name="oldPwd">旧密码（明文）</param>
    /// <param name="newPwd">新密码（明文）</param>
    /// <exception cref="BusinessException">用户不存在 / 旧密码错误 / 新密码为空</exception>
    public void ChangePassword(string userName, string oldPwd, string newPwd)
    {
        UserInfo user = _dao.FindById(userName)
            ?? throw new BusinessException($"用户名 {userName} 不存在");

        // 旧密码校验：明文加密后与数据库哈希比对
        if (!SecurityUtil.VerifyHash(oldPwd, user.UserPassword))
            throw new BusinessException("旧密码不正确");

        if (string.IsNullOrWhiteSpace(newPwd))
            throw new BusinessException("新密码不能为空");

        user.UserPassword = SecurityUtil.ComputeSha256Hash(newPwd);
        _dao.Update(user);

        LogManager.Record(userName, "修改密码", $"用户 {userName} 修改了密码");
    }
}
