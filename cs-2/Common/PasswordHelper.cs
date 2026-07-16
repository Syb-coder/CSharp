using System.Security.Cryptography;
using System.Text;

namespace CampusShop.Common;

/// <summary>
/// 密码工具类，提供 SHA-256 哈希计算
/// </summary>
public static class PasswordHelper
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
        return Convert.ToHexString(hashBytes); // .NET 8 提供，返回大写十六进制
    }

    /// <summary>
    /// 验证明文密码与哈希值是否匹配
    /// </summary>
    /// <param name="password">明文密码</param>
    /// <param name="hash">已存储的哈希值</param>
    /// <returns>匹配返回 true，否则 false</returns>
    public static bool Verify(string password, string hash)
    {
        string computedHash = ComputeHash(password);
        // FixedTimeEquals 逐字节比较且耗时固定，不因首个不匹配字节提前返回
        // 普通 == 运算符在发现差异时立即返回，攻击者可通过测量响应时间逐字节推测哈希值（时序攻击）
        // ToUpper 统一为大小写无关比较：存储的哈希值可能因不同来源大小写不一致
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(computedHash),
            Encoding.UTF8.GetBytes(hash.ToUpper()));
    }
}
