using System.Security.Cryptography;
using System.Text;

namespace LibrarySys.Common;

/// <summary>
/// 安全工具类，提供密码哈希等安全相关功能
/// </summary>
public static class SecurityUtil
{
    /// <summary>
    /// 计算字符串的 MD5 哈希值（大写十六进制字符串）
    /// </summary>
    /// <param name="input">原始字符串</param>
    /// <returns>32 位大写十六进制哈希字符串</returns>
    /// <exception cref="ArgumentNullException">输入为 null</exception>
    public static string ComputeMd5Hash(string input)
    {
        // 卫语句：提前校验入参，避免空引用异常
        if (input == null)
            throw new ArgumentNullException(nameof(input));

        // 使用 using 确保 MD5 实例被正确释放（MD5 实现了 IDisposable）
        using MD5 md5 = MD5.Create();
        byte[] inputBytes = Encoding.UTF8.GetBytes(input);
        byte[] hashBytes = md5.ComputeHash(inputBytes);

        // 用 StringBuilder 拼接十六进制字符串，避免多次字符串分配
        StringBuilder sb = new StringBuilder(32);
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
        return ComputeMd5Hash(input) == hash.ToUpperInvariant();
    }
}
