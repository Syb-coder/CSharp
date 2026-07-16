using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using LibrarySys.Models;

namespace LibrarySys.DAL
{
    /// <summary>
    /// 操作日志数据访问层（T_OperateLog 表）
    /// </summary>
    public class LogDao : BaseRepository<OperateLogInfo>
    {
        #region 抽象方法实现

        public override List<OperateLogInfo> FindAll()
        {
            return MapDataTable(ExecuteQueryDataTable("SELECT * FROM T_OperateLog ORDER BY operateTime DESC"));
        }

        public override OperateLogInfo? FindById(string id)
        {
            var list = MapDataTable(ExecuteQueryDataTable("SELECT * FROM T_OperateLog WHERE logID = @logID",
                CommandType.Text, new SqlParameter("@logID", int.Parse(id))));
            return list.Count > 0 ? list[0] : null;
        }

        public override int Insert(OperateLogInfo entity) => throw new NotSupportedException("请使用 InsertLog 方法");

        public override int Update(OperateLogInfo entity) => throw new NotSupportedException("操作日志不支持更新");

        public override int Delete(string id) => throw new NotSupportedException("操作日志不支持删除");

        #endregion

        /// <summary>将 DataTable 映射为列表</summary>
        private List<OperateLogInfo> MapDataTable(DataTable dt)
        {
            var list = new List<OperateLogInfo>();
            foreach (DataRow row in dt.Rows)
            {
                list.Add(new OperateLogInfo
                {
                    LogID = Convert.ToInt32(row["logID"]),
                    UserName = row["userName"].ToString()!,
                    OperateTime = Convert.ToDateTime(row["operateTime"]),
                    OperateType = row["operateType"].ToString()!,
                    OperateContent = row["operateContent"].ToString()!,
                    Detail = row["detail"] == DBNull.Value ? null : row["detail"].ToString()
                });
            }
            return list;
        }

        /// <summary>写入日志</summary>
    public int InsertLog(OperateLogInfo info)
        {
            const string sql = @"
                INSERT INTO T_OperateLog (userName, operateTime, operateType, operateContent, detail)
                VALUES (@userName, @operateTime, @operateType, @operateContent, @detail);
                SELECT SCOPE_IDENTITY();";
            return ExecuteScalar(sql, CommandType.Text,
                new SqlParameter("@userName", info.UserName),
                new SqlParameter("@operateTime", info.OperateTime),
                new SqlParameter("@operateType", info.OperateType),
                new SqlParameter("@operateContent", info.OperateContent),
                new SqlParameter("@detail", (object?)info.Detail ?? DBNull.Value));
        }

        /// <summary>查询日志</summary>
        public IList<OperateLogInfo> Search(DateTime? fromTime, DateTime? toTime, string? operateType, string? userName)
        {
            var sql = "SELECT * FROM T_OperateLog WHERE 1=1";
            var parameters = new List<SqlParameter>();

            if (fromTime.HasValue)
            {
                sql += " AND operateTime >= @fromTime";
                parameters.Add(new SqlParameter("@fromTime", fromTime.Value));
            }
            if (toTime.HasValue)
            {
                sql += " AND operateTime <= @toTime";
                parameters.Add(new SqlParameter("@toTime", toTime.Value));
            }
            if (!string.IsNullOrEmpty(operateType))
            {
                sql += " AND operateType = @operateType";
                parameters.Add(new SqlParameter("@operateType", operateType));
            }
            if (!string.IsNullOrEmpty(userName))
            {
                sql += " AND userName LIKE @userName";
                parameters.Add(new SqlParameter("@userName", $"%{userName}%"));
            }

            sql += " ORDER BY operateTime DESC";
            return MapDataTable(ExecuteQueryDataTable(sql, CommandType.Text, parameters.ToArray()));
        }

        /// <summary>获取总数</summary>
        public int CountTotal()
        {
            return ExecuteScalar("SELECT COUNT(1) FROM T_OperateLog");
        }
    }
}