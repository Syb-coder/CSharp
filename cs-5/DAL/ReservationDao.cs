using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using LibrarySys.Common;
using LibrarySys.Models;

namespace LibrarySys.DAL
{
    /// <summary>
    /// 预约数据访问层（T_Reservation 表）
    /// </summary>
    public class ReservationDao : BaseRepository<ReservationInfo>
    {
        #region 抽象方法实现

        public override List<ReservationInfo> FindAll()
        {
            const string sql = @"SELECT r.*, rd.readerName, bk.bookName
                FROM T_Reservation r
                LEFT JOIN T_Reader rd ON r.readerID = rd.readerID
                LEFT JOIN T_Book bk ON r.bookID = bk.bookID
                ORDER BY r.reserveDate DESC";
            return MapDataTable(ExecuteQueryDataTable(sql));
        }

        public override ReservationInfo? FindById(string id) => FindById(int.Parse(id));

        public override int Insert(ReservationInfo entity) => throw new NotSupportedException("请使用 InsertReservation 方法");

        public override int Update(ReservationInfo entity) => throw new NotSupportedException("请使用 UpdateStatus 方法");

        public override int Delete(string id) => throw new NotSupportedException("请使用 Cancel 方法");

        #endregion

        /// <summary>将 DataTable 映射为列表</summary>
        private List<ReservationInfo> MapDataTable(DataTable dt)
        {
            var list = new List<ReservationInfo>();
            foreach (DataRow row in dt.Rows)
            {
                list.Add(new ReservationInfo
                {
                    ReserveID = Convert.ToInt32(row["reserveID"]),
                    ReaderID = row["readerID"].ToString()!,
                    BookID = row["bookID"].ToString()!,
                    ReserveDate = Convert.ToDateTime(row["reserveDate"]),
                    ExpireDate = Convert.ToDateTime(row["expireDate"]),
                    NotifyDate = row["notifyDate"] == DBNull.Value ? null : Convert.ToDateTime(row["notifyDate"]),
                    CompleteDate = row["completeDate"] == DBNull.Value ? null : Convert.ToDateTime(row["completeDate"]),
                    CancelDate = row["cancelDate"] == DBNull.Value ? null : Convert.ToDateTime(row["cancelDate"]),
                    CancelReason = row["cancelReason"] == DBNull.Value ? null : row["cancelReason"].ToString(),
                    Status = row["status"].ToString()!,
                    ReaderName = row.Table.Columns.Contains("readerName") && row["readerName"] != DBNull.Value ? row["readerName"].ToString() : null,
                    BookName = row.Table.Columns.Contains("bookName") && row["bookName"] != DBNull.Value ? row["bookName"].ToString() : null
                });
            }
            return list;
        }

        /// <summary>新增预约</summary>
    public int InsertReservation(ReservationInfo info)
        {
            const string sql = @"
                INSERT INTO T_Reservation (readerID, bookID, reserveDate, expireDate, status)
                VALUES (@readerID, @bookID, @reserveDate, @expireDate, @status);
                SELECT SCOPE_IDENTITY();";
            return ExecuteScalar(sql, CommandType.Text,
                new SqlParameter("@readerID", info.ReaderID),
                new SqlParameter("@bookID", info.BookID),
                new SqlParameter("@reserveDate", info.ReserveDate),
                new SqlParameter("@expireDate", info.ExpireDate),
                new SqlParameter("@status", info.Status));
        }

        /// <summary>更新预约状态</summary>
        public bool UpdateStatus(int reserveID, string status, DateTime? notifyDate = null,
            DateTime? completeDate = null, DateTime? cancelDate = null, string? cancelReason = null)
        {
            const string sql = @"
                UPDATE T_Reservation
                SET status = @status,
                    notifyDate = @notifyDate,
                    completeDate = @completeDate,
                    cancelDate = @cancelDate,
                    cancelReason = @cancelReason
                WHERE reserveID = @reserveID;";
            return ExecuteNonQuery(sql, CommandType.Text,
                new SqlParameter("@reserveID", reserveID),
                new SqlParameter("@status", status),
                DBNullParameter("@notifyDate", notifyDate),
                DBNullParameter("@completeDate", completeDate),
                DBNullParameter("@cancelDate", cancelDate),
                new SqlParameter("@cancelReason", (object?)cancelReason ?? DBNull.Value)) > 0;
        }

        /// <summary>查询预约列表（支持多条件筛选）</summary>
        public IList<ReservationInfo> Search(string? readerID, string? bookID, string? status)
        {
            var sql = @"SELECT r.*, rd.readerName, bk.bookName
                FROM T_Reservation r
                LEFT JOIN T_Reader rd ON r.readerID = rd.readerID
                LEFT JOIN T_Book bk ON r.bookID = bk.bookID
                WHERE 1=1";
            var parameters = new List<SqlParameter>();

            if (!string.IsNullOrEmpty(readerID))
            {
                sql += " AND r.readerID LIKE @readerID";
                parameters.Add(new SqlParameter("@readerID", $"%{readerID}%"));
            }
            if (!string.IsNullOrEmpty(bookID))
            {
                sql += " AND r.bookID LIKE @bookID";
                parameters.Add(new SqlParameter("@bookID", $"%{bookID}%"));
            }
            if (!string.IsNullOrEmpty(status))
            {
                sql += " AND r.status = @status";
                parameters.Add(new SqlParameter("@status", status));
            }

            sql += " ORDER BY r.reserveDate DESC";
            return MapDataTable(ExecuteQueryDataTable(sql, CommandType.Text, parameters.ToArray()));
        }

        /// <summary>根据 ID 查询单条预约</summary>
        public ReservationInfo? FindById(int reserveID)
        {
            const string sql = @"SELECT r.*, rd.readerName, bk.bookName
                FROM T_Reservation r
                LEFT JOIN T_Reader rd ON r.readerID = rd.readerID
                LEFT JOIN T_Book bk ON r.bookID = bk.bookID
                WHERE r.reserveID = @reserveID;";
            var list = MapDataTable(ExecuteQueryDataTable(sql, CommandType.Text,
                new SqlParameter("@reserveID", reserveID)));
            return list.Count > 0 ? list[0] : null;
        }

        /// <summary>检查读者是否已有该书的活跃预约</summary>
        public bool HasActiveReservation(string readerID, string bookID)
        {
            const string sql = @"SELECT COUNT(1) FROM T_Reservation
                WHERE readerID = @readerID AND bookID = @bookID
                  AND status IN (N'排队中', N'待取书');";
            return ExecuteScalar(sql, CommandType.Text,
                new SqlParameter("@readerID", readerID),
                new SqlParameter("@bookID", bookID)) > 0;
        }

        /// <summary>获取某本书的最早排队预约</summary>
        public ReservationInfo? GetFirstQueuing(string bookID)
        {
            const string sql = @"SELECT TOP 1 r.*, rd.readerName, bk.bookName
                FROM T_Reservation r
                LEFT JOIN T_Reader rd ON r.readerID = rd.readerID
                LEFT JOIN T_Book bk ON r.bookID = bk.bookID
                WHERE r.bookID = @bookID AND r.status = N'排队中'
                ORDER BY r.reserveDate ASC;";
            var list = MapDataTable(ExecuteQueryDataTable(sql, CommandType.Text,
                new SqlParameter("@bookID", bookID)));
            return list.Count > 0 ? list[0] : null;
        }

        /// <summary>批量取消过期预约</summary>
        public int CancelExpired()
        {
            const string sql = @"UPDATE T_Reservation
                SET status = N'已取消', cancelDate = GETDATE(), cancelReason = N'预约超时'
                WHERE status = N'排队中' AND expireDate < CAST(GETDATE() AS DATE);";
            return ExecuteNonQuery(sql);
        }

        /// <summary>取消待取书超时预约</summary>
        public int CancelWaitingExpired()
        {
            const string sql = @"UPDATE T_Reservation
                SET status = N'已取消', cancelDate = GETDATE(), cancelReason = N'取书超时'
                WHERE status = N'待取书'
                  AND DATEADD(DAY, @validDays, ISNULL(notifyDate, reserveDate)) < CAST(GETDATE() AS DATE);";
            return ExecuteNonQuery(sql, CommandType.Text,
                new SqlParameter("@validDays", BusinessConstants.RESERVE_VALID_DAYS));
        }

        /// <summary>获取读者的活跃预约数</summary>
        public int CountActiveReservations(string readerID)
        {
            const string sql = @"SELECT COUNT(1) FROM T_Reservation
                WHERE readerID = @readerID AND status IN (N'排队中', N'待取书');";
            return ExecuteScalar(sql, CommandType.Text,
                new SqlParameter("@readerID", readerID));
        }

        /// <summary>获取总预约数</summary>
        public int CountTotal()
        {
            return ExecuteScalar("SELECT COUNT(1) FROM T_Reservation");
        }

        /// <summary>获取活跃预约数</summary>
        public int CountActive()
        {
            return ExecuteScalar("SELECT COUNT(1) FROM T_Reservation WHERE status IN (N'排队中', N'待取书')");
        }
    }
}