using System.Text.RegularExpressions;

namespace CampusMart.Common;

/// <summary>
/// 数据校验工具类，提供通用的输入验证方法
/// </summary>
/// <remarks>
/// 与 cs-2 的 ValidationHelper 差异化命名。
/// 所有方法返回 bool，不抛异常，调用方根据返回值决定提示信息。
/// </remarks>
public static class ValidateUtil
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
    /// <returns>超过返回 true；null 视为不超长</returns>
    public static bool IsExceedLength(string value, int maxLength)
    {
        // null 视为"不存在"，不存在长度溢出问题，返回 false 以跳过该字段的长度校验
        if (value == null) return false;
        return value.Length > maxLength;
    }

    /// <summary>
    /// 检查字符串是否为有效的正整数（大于 0）
    /// </summary>
    /// <param name="value">待检查的字符串</param>
    /// <returns>是正整数返回 true</returns>
    // 用于订单数量校验：购买数量必须大于 0
    public static bool IsPositiveInteger(string value)
    {
        if (IsNullOrWhiteSpace(value)) return false;
        return int.TryParse(value, out int result) && result > 0;
    }

    /// <summary>
    /// 检查字符串是否为有效的非负整数（大于等于 0）
    /// </summary>
    /// <param name="value">待检查的字符串</param>
    /// <returns>是非负整数返回 true</returns>
    // 用于库存数量校验：库存允许为 0（售罄），但不能为负数
    public static bool IsNonNegativeInteger(string value)
    {
        if (IsNullOrWhiteSpace(value)) return false;
        return int.TryParse(value, out int result) && result >= 0;
    }

    /// <summary>
    /// 检查字符串是否为有效的正小数（大于 0）
    /// </summary>
    /// <param name="value">待检查的字符串</param>
    /// <returns>是正小数返回 true</returns>
    // 用于商品单价校验：单价必须大于 0
    public static bool IsPositiveDecimal(string value)
    {
        if (IsNullOrWhiteSpace(value)) return false;
        return decimal.TryParse(value, out decimal result) && result > 0;
    }

    /// <summary>
    /// 检查收货人手机号格式是否合法（必须为 11 位纯数字）
    /// </summary>
    /// <param name="phone">手机号字符串</param>
    /// <returns>11 位纯数字返回 true</returns>
    // PRD 强制要求收货人手机号为 11 位纯数字，与供货商电话（允许连字符）校验规则不同
    public static bool IsValidMobilePhone(string phone)
    {
        if (IsNullOrWhiteSpace(phone)) return false;
        // ^1\d{10}$：以 1 开头，后接 10 位数字，共 11 位，符合国内手机号规范
        return Regex.IsMatch(phone, @"^1\d{10}$");
    }

    /// <summary>
    /// 检查供货商联系电话格式是否合法（允许数字、空格、连字符，长度 6-15）
    /// </summary>
    /// <param name="phone">联系电话字符串</param>
    /// <returns>合法返回 true；空值视为合法（选填字段）</returns>
    public static bool IsValidContactPhone(string phone)
    {
        // 供货商联系电话为选填字段，空值直接放行
        if (IsNullOrWhiteSpace(phone)) return true;
        return Regex.IsMatch(phone, @"^[\d\s\-]{6,15}$");
    }
}
