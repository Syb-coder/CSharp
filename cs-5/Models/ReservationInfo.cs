using System;

namespace LibrarySys.Models
{
    /// <summary>
    /// 预约信息实体类
    /// 对应数据库 T_Reservation 表，cs-5 核心新增功能
    /// </summary>
    public class ReservationInfo
    {
        /// <summary>预约编号（自增）</summary>
        public int ReserveID { get; set; }

        /// <summary>读者编号</summary>
        public string ReaderID { get; set; } = string.Empty;

        /// <summary>图书编号</summary>
        public string BookID { get; set; } = string.Empty;

        /// <summary>预约日期</summary>
        public DateTime ReserveDate { get; set; }

        /// <summary>预约有效期至</summary>
        public DateTime ExpireDate { get; set; }

        /// <summary>通知取书日期</summary>
        public DateTime? NotifyDate { get; set; }

        /// <summary>完成日期（实际借书时填写）</summary>
        public DateTime? CompleteDate { get; set; }

        /// <summary>取消日期</summary>
        public DateTime? CancelDate { get; set; }

        /// <summary>取消原因</summary>
        public string? CancelReason { get; set; }

        /// <summary>预约状态（排队中/待取书/已完成/已取消）</summary>
        public string Status { get; set; } = string.Empty;

        // ===== 显示用扩展字段（来自 JOIN 查询） =====

        /// <summary>读者姓名（来自 T_Reader）</summary>
        public string? ReaderName { get; set; }

        /// <summary>书名（来自 T_Book）</summary>
        public string? BookName { get; set; }
    }
}