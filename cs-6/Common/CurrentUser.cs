using HotelSys.Models;

namespace HotelSys.Common;

/// <summary>
/// 全局当前登录用户状态类（PRD 4.2.1）
/// 登录成功后设置，主窗体及各子窗体据此控制权限
/// </summary>
public static class CurrentUser
{
    /// <summary>当前登录用户信息</summary>
    public static UserInfo User { get; private set; }

    /// <summary>当前用户名</summary>
    public static string UserName => User?.UserName;

    /// <summary>当前用户权限</summary>
    public static string UserPurview => User?.UserPurview;

    /// <summary>是否为管理员</summary>
    public static bool IsAdmin => User?.UserPurview == BusinessConstants.ROLE_ADMIN;

    /// <summary>是否为前台操作员</summary>
    public static bool IsFrontDesk => User?.UserPurview == BusinessConstants.ROLE_USER;

    /// <summary>登录成功后设置当前用户</summary>
    /// <param name="user">已通过验证的用户信息</param>
    public static void SetUser(UserInfo user) => User = user;

    /// <summary>退出登录时清空当前用户</summary>
    public static void Clear() => User = null;
}
