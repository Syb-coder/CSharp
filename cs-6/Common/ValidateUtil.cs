namespace HotelSys.Common;

/// <summary>
/// 输入验证工具类，提供通用的数据校验方法（沿用 cs-5）
/// </summary>
public static class ValidateUtil
{
    /// <summary>
    /// 校验字符串是否为空或仅含空白字符
    /// </summary>
    /// <param name="value">待校验字符串</param>
    /// <returns>为空或全空白返回 true，否则 false</returns>
    public static bool IsNullOrWhiteSpace(string value)
        => string.IsNullOrWhiteSpace(value);

    /// <summary>
    /// 校验字符串长度是否在指定范围内
    /// </summary>
    /// <param name="value">待校验字符串</param>
    /// <param name="maxLength">最大长度（含）</param>
    /// <returns>为 null 或长度超过 maxLength 返回 false</returns>
    public static bool IsValidLength(string value, int maxLength)
        => value != null && value.Length <= maxLength;

    /// <summary>
    /// 校验字符串是否为有效的正整数
    /// </summary>
    /// <param name="value">待校验字符串</param>
    /// <returns>是正整数返回 true，否则 false</returns>
    public static bool IsPositiveInteger(string value)
        => int.TryParse(value, out int n) && n > 0;

    /// <summary>
    /// 校验字符串是否为有效的大于 0 的数值
    /// </summary>
    /// <param name="value">待校验字符串</param>
    /// <returns>是大于 0 的数值返回 true，否则 false</returns>
    public static bool IsPositiveDecimal(string value)
        => decimal.TryParse(value, out decimal n) && n > 0;

    /// <summary>
    /// 校验字符串是否为有效的非负数值（≥0，用于押金等字段）
    /// </summary>
    /// <param name="value">待校验字符串</param>
    /// <returns>是≥0的数值返回 true，否则 false</returns>
    public static bool IsNonNegativeDecimal(string value)
        => decimal.TryParse(value, out decimal n) && n >= 0;

    /// <summary>
    /// 校验字符串是否为有效的日期格式
    /// </summary>
    /// <param name="value">待校验字符串</param>
    /// <returns>是有效日期返回 true，否则 false</returns>
    public static bool IsValidDate(string value)
        => DateTime.TryParse(value, out _);
}
