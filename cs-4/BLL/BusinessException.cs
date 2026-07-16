namespace CampusStore.BLL;

/// <summary>
/// 业务异常类
/// </summary>
/// <remarks>
/// 用于在 BLL 层抛出业务校验失败异常，与系统异常（SqlException 等）区分。
/// UI 层只需捕获 BusinessException（业务错误，向用户展示 Message）和 Exception（系统错误）两类。
/// </remarks>
public class BusinessException : Exception
{
    /// <summary>
    /// 构造业务异常
    /// </summary>
    /// <param name="message">异常消息（将直接展示给用户）</param>
    public BusinessException(string message) : base(message) { }

    /// <summary>
    /// 构造业务异常并指定内部异常
    /// </summary>
    /// <param name="message">异常消息</param>
    /// <param name="innerException">内部异常</param>
    public BusinessException(string message, Exception innerException) : base(message, innerException) { }
}
