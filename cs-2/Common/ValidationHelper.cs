using System.Text.RegularExpressions;

namespace CampusShop.Common;

/// <summary>
/// 输入校验工具类，提供通用的数据验证方法
/// </summary>
// 声明为 static 类：所有方法均为无状态的全局工具函数，无需实例化，也防止被误实例化
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
        // null 视为"不存在"，不存在长度溢出问题，返回 false 以跳过该字段的长度校验
        if (value == null) return false;
        return value.Length > maxLength;
    }

    /// <summary>
    /// 检查字符串是否为有效的正整数
    /// </summary>
    /// <param name="value">待检查的字符串</param>
    /// <returns>是正整数返回 true</returns>
    // 用于售卖数量等业务场景：数量必须大于 0，0 和负数无业务意义
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
    // 用于库存数量等业务场景：库存允许为 0（表示售罄），但不能为负数
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
    // 用于商品单价校验：单价必须大于 0
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
        // 电话为选填字段：供货商可能不愿提供联系方式，空值直接放行
        if (IsNullOrWhiteSpace(phone)) return true; // 电话选填，空值合法
        // 正则允许空格和连字符以兼容多种输入习惯（如 010-12345678、138 0000 0000）
        return Regex.IsMatch(phone, @"^[\d\s\-]{6,15}$");
    }
}
