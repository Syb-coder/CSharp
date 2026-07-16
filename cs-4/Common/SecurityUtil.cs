using System.Security.Cryptography;
using System.Text;

namespace CampusStore.Common;

/// <summary>
/// 安全工具类：密码哈希计算
/// </summary>
/// <remarks>
/// 采用 SHA-256 算法对明文密码进行哈希后存储。
/// 哈希结果以 64 位小写十六进制字符串形式返回，与数据库 init.sql 中初始密码哈希一致。
/// </remarks>
public static class SecurityUtil
{
    /// <summary>
    /// 计算字符串的 SHA-256 哈希值
    /// </summary>
    /// <param name="input">待哈希的原始字符串</param>
    /// <returns>64 位小写十六进制字符串</returns>
    /// <exception cref="ArgumentNullException">输入为 null 时抛出</exception>
    public static string Sha256Hash(string input)
    {
        // 卫语句：空值检查，避免后续 NullReferenceException
        if (input == null)
            throw new ArgumentNullException(nameof(input));

        // 使用 UTF-8 编码将字符串转为字节数组
        // SHA-256 对同一字符串始终产生相同的哈希值（无盐值，因课程设计阶段不涉及彩虹表攻击防护）
        byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));

        // 转为小写十六进制字符串（与数据库初始数据保持一致）
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    /// <summary>
    /// 验证明文密码是否与给定哈希值匹配
    /// </summary>
    /// <param name="plainPassword">明文密码</param>
    /// <param name="hashValue">已存储的哈希值</param>
    /// <returns>匹配返回 true，否则返回 false</returns>
    public static bool VerifyPassword(string plainPassword, string hashValue)
    {
        if (string.IsNullOrEmpty(plainPassword) || string.IsNullOrEmpty(hashValue))
            return false;
        return string.Equals(Sha256Hash(plainPassword), hashValue, StringComparison.OrdinalIgnoreCase);
    }
}
