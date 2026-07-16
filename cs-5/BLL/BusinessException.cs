namespace LibrarySys.BLL;

/// <summary>
/// 业务异常类，用于在 BLL 层抛出业务校验失败等可预期错误
/// UI 层只需捕获 BusinessException（业务错误）和 Exception（系统错误）两类
/// </summary>
public class BusinessException : Exception
{
    /// <summary>
    /// 构造业务异常
    /// </summary>
    /// <param name="message">业务错误提示信息（可直接展示给用户）</param>
    public BusinessException(string message) : base(message) { }

    /// <summary>
    /// 构造业务异常并指定内部异常
    /// </summary>
    /// <param name="message">业务错误提示信息</param>
    /// <param name="innerException">内部异常</param>
    public BusinessException(string message, Exception innerException) : base(message, innerException) { }
}
