using System.Text.RegularExpressions;

namespace LibraryManagement.Common;

/// <summary>
/// 输入校验工具类，提供通用的数据验证方法
/// </summary>
public static class ValidationHelper
{
    /// <summary>
    /// 检查字符串是否为空或空白
    /// </summary>
    /// <param name="value">待检查的字符串</param>
    /// <returns>为空或空白返回 true</returns>
    public static bool IsNullOrWhiteSpace(string value)
    {
        return string.IsNullOrWhiteSpace(value);
    }

    /// <summary>
    /// 检查字符串长度是否超过指定上限
    /// </summary>
    /// <param name="value">待检查的字符串</param>
    /// <param name="maxLength">最大长度</param>
    /// <returns>超过返回 true</returns>
    public static bool IsExceedLength(string value, int maxLength)
    {
        if (value == null) return false;
        return value.Length > maxLength;
    }

    /// <summary>
    /// 检查字符串是否为有效的正整数
    /// </summary>
    /// <param name="value">待检查的字符串</param>
    /// <returns>是正整数返回 true</returns>
    public static bool IsPositiveInteger(string value)
    {
        if (IsNullOrWhiteSpace(value)) return false;
        return int.TryParse(value, out int result) && result > 0;
    }

    /// <summary>
    /// 检查字符串是否为有效的非负整数
    /// </summary>
    /// <param name="value">待检查的字符串</param>
    /// <returns>是非负整数返回 true</returns>
    public static bool IsNonNegativeInteger(string value)
    {
        if (IsNullOrWhiteSpace(value)) return false;
        return int.TryParse(value, out int result) && result >= 0;
    }

    /// <summary>
    /// 检查字符串是否为有效的正小数
    /// </summary>
    /// <param name="value">待检查的字符串</param>
    /// <returns>是正小数返回 true</returns>
    public static bool IsPositiveDecimal(string value)
    {
        if (IsNullOrWhiteSpace(value)) return false;
        return decimal.TryParse(value, out decimal result) && result > 0;
    }

    /// <summary>
    /// 检查电话号码格式是否合法（允许数字、空格、连字符，长度6-15）
    /// </summary>
    /// <param name="phone">电话号码字符串</param>
    /// <returns>合法返回 true</returns>
    public static bool IsValidPhone(string phone)
    {
        if (IsNullOrWhiteSpace(phone)) return true; // 电话选填，空值合法
        return Regex.IsMatch(phone, @"^[\d\s\-]{6,15}$");
    }
}
