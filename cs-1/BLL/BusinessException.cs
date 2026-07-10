namespace LibraryManagement.BLL;

/// <summary>
/// 业务异常类，用于封装业务规则校验失败信息
/// </summary>
public class BusinessException : Exception
{
    /// <summary>
    /// 构造业务异常
    /// </summary>
    /// <param name="message">异常提示信息</param>
    public BusinessException(string message) : base(message)
    {
    }
}
