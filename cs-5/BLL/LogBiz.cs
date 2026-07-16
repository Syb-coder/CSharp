using System;
using System.Collections.Generic;
using LibrarySys.Common;
using LibrarySys.DAL;
using LibrarySys.Models;

namespace LibrarySys.BLL
{
    /// <summary>
    /// 操作日志业务逻辑层
    /// 提供日志写入和查询功能
    /// </summary>
    public class LogBiz
    {
        private static readonly LogDao _dao = new LogDao();

        /// <summary>
        /// 写入操作日志（静态方法，方便全局调用）
        /// </summary>
        /// <param name="userName">操作者用户名</param>
        /// <param name="operateType">操作类型（使用 BusinessConstants.LOG_* 常量）</param>
        /// <param name="operateContent">操作对象</param>
        /// <param name="detail">详细说明</param>
        public static void Log(string userName, string operateType, string operateContent, string? detail = null)
        {
            var info = new OperateLogInfo
            {
                UserName = userName,
                OperateTime = DateTime.Now,
                OperateType = operateType,
                OperateContent = operateContent,
                Detail = detail
            };
            _dao.InsertLog(info);
        }

        /// <summary>查询日志</summary>
        public IList<OperateLogInfo> Search(DateTime? fromTime, DateTime? toTime, string? operateType, string? userName)
        {
            return _dao.Search(fromTime, toTime, operateType, userName);
        }

        /// <summary>获取日志总数</summary>
        public int CountTotal()
        {
            return _dao.CountTotal();
        }
    }
}