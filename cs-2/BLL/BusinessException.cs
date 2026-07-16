namespace CampusShop.BLL;

/// <summary>
/// 业务异常类，用于封装业务规则校验失败信息
/// </summary>
// 自定义业务异常的意义：将"业务校验失败"（如编号已存在、字段超长）与"系统异常"（如数据库连接失败）区分开
// UI 层捕获 BusinessException 时向用户展示友好提示，捕获其他异常时显示"系统错误"
// 这样既避免将技术错误信息暴露给用户，又能让业务校验消息精准传达
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
