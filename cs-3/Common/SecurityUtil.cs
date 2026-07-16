using System.Security.Cryptography;
using System.Text;

namespace CampusMart.Common;

/// <summary>
/// 安全工具类，提供密码哈希与校验功能
/// </summary>
/// <remarks>
/// 与 cs-2 的 PasswordHelper 差异化命名。
/// 密码采用 SHA-256 哈希存储，数据库中不保存明文，符合 PRD 安全性要求。
/// </remarks>
public static class SecurityUtil
{
    /// <summary>
    /// 计算密码的 SHA-256 哈希值（大写十六进制字符串）
    /// </summary>
    /// <param name="password">明文密码</param>
    /// <returns>64 位十六进制哈希字符串</returns>
    public static string ComputeHash(string password)
    {
        // 选择 SHA-256 而非 MD5：MD5 已被证明存在碰撞漏洞，不适用于密码存储
        // SHA-256 生成 256 位摘要，抗碰撞强度满足当前安全要求
        // 使用静态 HashData 方法避免创建 SHA256 实例，减少 IDisposable 资源开销
        byte[] hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
        // Convert.ToHexString 返回大写形式，统一为大写可在后续比较时免去大小写转换的歧义
        return Convert.ToHexString(hashBytes);
    }

    /// <summary>
    /// 验证明文密码与已存储的哈希值是否匹配
    /// </summary>
    /// <param name="password">明文密码</param>
    /// <param name="storedHash">已存储的哈希值</param>
    /// <returns>匹配返回 true，否则 false</returns>
    public static bool Verify(string password, string storedHash)
    {
        // 空值保护：任意一方为空则直接判定不匹配，避免后续异常
        if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(storedHash))
            return false;

        string computedHash = ComputeHash(password);
        // 使用固定时间比较防止时序攻击：攻击者无法通过响应时长逐字节推测哈希值
        // OrdinalIgnoreCase 忽略大小写：数据库中存储的哈希可能因导入来源不同大小写不一
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(computedHash.ToUpper()),
            Encoding.UTF8.GetBytes(storedHash.ToUpper()));
    }
}
