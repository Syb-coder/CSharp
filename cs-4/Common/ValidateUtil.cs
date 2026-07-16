using System.Text.RegularExpressions;

namespace CampusStore.Common;

/// <summary>
/// 输入校验工具类
/// </summary>
/// <remarks>
/// 提供通用的输入校验方法，UI 层在提交前调用此类完成前端校验，
/// 减少无效数据库请求。数据库层的约束（CHECK/NOT NULL）作为最终保障。
/// </remarks>
public static class ValidateUtil
{
    /// <summary>
    /// 检查字符串是否为空或仅含空白字符
    /// </summary>
    /// <param name="value">待校验字符串</param>
    /// <returns>为空或全空白返回 true，否则返回 false</returns>
    public static bool IsNullOrWhiteSpace(string value)
        => string.IsNullOrWhiteSpace(value);

    /// <summary>
    /// 检查字符串长度是否超过指定上限
    /// </summary>
    /// <param name="value">待校验字符串</param>
    /// <param name="maxLength">最大长度</param>
    /// <returns>超过返回 true，否则返回 false</returns>
    public static bool IsExceedLength(string value, int maxLength)
        => value != null && value.Length > maxLength;

    /// <summary>
    /// 校验手机号格式（11 位数字、首位为 1）
    /// </summary>
    /// <param name="phone">手机号字符串</param>
    /// <returns>合法返回 true，空值或非法返回 false</returns>
    /// <remarks>空值视为合法（选填字段），由调用方按需校验</remarks>
    public static bool IsValidPhone(string phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
            return true;
        return Regex.IsMatch(phone, @"^1\d{10}$");
    }

    /// <summary>
    /// 校验数值是否大于 0
    /// </summary>
    /// <param name="value">待校验数值</param>
    /// <returns>大于 0 返回 true，否则返回 false</returns>
    public static bool IsGreaterThanZero(decimal value) => value > 0;

    /// <summary>
    /// 校验数值是否为非负整数
    /// </summary>
    /// <param name="value">待校验数值</param>
    /// <returns>非负返回 true，否则返回 false</returns>
    public static bool IsNonNegative(int value) => value >= 0;

    /// <summary>
    /// 校验日期是否不超过当前日期
    /// </summary>
    /// <param name="date">待校验日期</param>
    /// <returns>不超过当前日期返回 true，否则返回 false</returns>
    /// <remarks>用于生产日期、注册日期等不可为未来时间的字段</remarks>
    public static bool IsNotFutureDate(DateTime? date)
    {
        if (!date.HasValue)
            return true;
        return date.Value.Date <= DateTime.Today;
    }
}
