using HotelSys.DAL;
using HotelSys.Models;

namespace HotelSys.BLL;

/// <summary>
/// 操作日志业务逻辑类（PRD 4.2.13 / F-22）
/// 提供静态记录方法供其他 Manager 调用，以及实例查询方法供 UI 调用
/// 日志仅支持插入和查询，不可修改、不可删除
/// </summary>
public class LogManager
{
    private readonly LogDao _dao = new();

    /// <summary>
    /// 静态记录操作日志（供其他 Manager 在业务操作后调用，无需实例化）
    /// </summary>
    /// <param name="userName">操作者用户名</param>
    /// <param name="operateType">操作类型（取自 BusinessConstants.LOG_*）</param>
    /// <param name="operateContent">操作内容摘要（≤100字符）</param>
    /// <param name="detail">操作详情（可空，≤200字符）</param>
    public static void Record(string userName, string operateType, string operateContent, string detail = null)
    {
        // 日志记录失败不应影响主业务流程，吞掉异常避免回滚业务事务
        try
        {
            LogDao logDao = new();
            logDao.Insert(new OperateLogInfo
            {
                UserName = userName,
                OperateType = operateType,
                OperateContent = operateContent,
                Detail = detail
            });
        }
        catch
        {
            // 日志写入失败静默处理，避免影响主业务操作的结果
        }
    }

    /// <summary>查询全部日志（按时间倒序）</summary>
    public List<OperateLogInfo> GetAll() => _dao.FindAll();

    /// <summary>
    /// 多条件查询日志（PRD F-22）
    /// </summary>
    /// <param name="userName">用户名（空表示不限）</param>
    /// <param name="operateType">操作类型（空表示不限）</param>
    /// <param name="from">起始时间（空表示不限）</param>
    /// <param name="to">结束时间（空表示不限）</param>
    public List<OperateLogInfo> Search(string userName, string operateType, DateTime? from, DateTime? to)
        => _dao.Search(userName, operateType, from, to);
}
