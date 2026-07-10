using System.Security.Cryptography;
using System.Text;

namespace LibraryManagement.Common;

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
        // 使用 SHA-256 算法计算哈希，Using 保证资源释放
        byte[] hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
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
        // 使用恒定时间比较防止时序攻击
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(computedHash),
            Encoding.UTF8.GetBytes(hash.ToUpper()));
    }
}
