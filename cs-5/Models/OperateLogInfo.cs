using System;

namespace LibrarySys.Models
{
    /// <summary>
    /// 操作日志实体类
    /// 对应数据库 T_OperateLog 表，cs-5 新增功能
    /// </summary>
    public class OperateLogInfo
    {
        /// <summary>日志编号（自增）</summary>
        public int LogID { get; set; }

        /// <summary>操作者用户名</summary>
        public string UserName { get; set; } = string.Empty;

        /// <summary>操作时间</summary>
        public DateTime OperateTime { get; set; }

        /// <summary>操作类型（登录/新增/修改/删除/借书/还书/预约/取消预约/缴费）</summary>
        public string OperateType { get; set; } = string.Empty;

        /// <summary>操作对象（如"图书：TP001"、"读者：2025001"）</summary>
        public string OperateContent { get; set; } = string.Empty;

        /// <summary>详细说明</summary>
        public string? Detail { get; set; }
    }
}