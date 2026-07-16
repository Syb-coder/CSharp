using System.Security.Cryptography;
using System.Text;

namespace HotelSys.Common;

/// <summary>
/// 安全工具类，提供密码哈希等安全相关功能
/// cs-6 使用 SHA-256 算法（区别于 cs-5 的 MD5），输出 64 位十六进制哈希串
/// </summary>
public static class SecurityUtil
{
    /// <summary>
    /// 计算字符串的 SHA-256 哈希值（大写十六进制字符串）
    /// </summary>
    /// <param name="input">原始字符串</param>
    /// <returns>64 位大写十六进制哈希字符串</returns>
    /// <exception cref="ArgumentNullException">输入为 null</exception>
    public static string ComputeSha256Hash(string input)
    {
        // 卫语句：提前校验入参，避免空引用异常
        if (input == null)
            throw new ArgumentNullException(nameof(input));

        // 使用 using 确保 SHA256 实例被正确释放
        using SHA256 sha256 = SHA256.Create();
        byte[] inputBytes = Encoding.UTF8.GetBytes(input);
        byte[] hashBytes = sha256.ComputeHash(inputBytes);

        // 用 StringBuilder 拼接十六进制字符串，避免多次字符串分配
        StringBuilder sb = new StringBuilder(64);
        foreach (byte b in hashBytes)
        {
            // X2 表示大写两位十六进制（如 0A、FF）
            sb.Append(b.ToString("X2"));
        }
        return sb.ToString();
    }

    /// <summary>
    /// 验证原始字符串与哈希值是否匹配
    /// </summary>
    /// <param name="input">原始字符串</param>
    /// <param name="hash">待比对的哈希值（大写十六进制）</param>
    /// <returns>匹配返回 true，否则 false</returns>
    public static bool VerifyHash(string input, string hash)
    {
        if (string.IsNullOrEmpty(hash))
            return false;
        // 统一转大写比较，避免大小写差异导致校验失败
        return ComputeSha256Hash(input) == hash.ToUpperInvariant();
    }
}
