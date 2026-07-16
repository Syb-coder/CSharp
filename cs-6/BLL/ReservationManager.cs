using HotelSys.Common;
using HotelSys.DAL;
using HotelSys.Models;
using Microsoft.Data.SqlClient;

namespace HotelSys.BLL;

/// <summary>
/// 预订管理业务逻辑类（PRD 4.2.7 / F-09 / F-10 / F-11）
/// 预订登记（含时间重叠校验+事务）、取消、转入住、自动过期
/// 简化方案：预订即预留指定房间，预订成功后房态变为"预留"
/// </summary>
public class ReservationManager
{
    private readonly ReservationDao _dao = new();
    private readonly RoomDao _roomDao = new();
    private readonly CheckInDao _checkInDao = new();

    /// <summary>查询全部预订记录</summary>
    public List<ReservationInfo> GetAll() => _dao.FindAll();

    /// <summary>按预订编号查询单条记录</summary>
    public ReservationInfo GetById(int reserveID) => _dao.GetById(reserveID);

    /// <summary>
    /// 多条件查询预订记录（PRD F-10）
    /// </summary>
    /// <param name="customerName">客户姓名（模糊）</param>
    /// <param name="status">预订状态</param>
    /// <param name="fromDate">预计入住起始日期</param>
    /// <param name="toDate">预计入住结束日期</param>
    public List<ReservationInfo> Search(string customerName, string status, DateTime? fromDate, DateTime? toDate)
        => _dao.Search(customerName, status, fromDate, toDate);

    /// <summary>
    /// 预订登记（PRD F-09 / 6.5 接口契约）
    /// 事务：INSERT 预订记录 + UPDATE 房态→预留
    /// </summary>
    /// <param name="info">预订信息实体</param>
    /// <exception cref="BusinessException">必填项为空 / 房间不存在 / 房间非空闲 / 时间重叠冲突</exception>
    public void Reserve(ReservationInfo info)
    {
        ValidateReservation(info);

        // 校验房间存在且为空闲状态，避免预订在住/预留/维护中的房间
        RoomInfo room = _roomDao.FindById(info.RoomNo)
            ?? throw new BusinessException($"房号 {info.RoomNo} 不存在");

        if (room.RoomStatus != RoomStatusConstants.FREE)
            throw new BusinessException($"房号 {info.RoomNo} 当前状态为 {room.RoomStatus}，仅空闲房间可预订");

        // 同一房间同一时间段不可重复预订（时间重叠校验，PRD F-09）
        if (_dao.HasTimeConflict(info.RoomNo, info.ExpectCheckIn, info.ExpectDays))
            throw new BusinessException($"房号 {info.RoomNo} 在该时间段已有预订，请更换日期或房间");

        // 事务：插入预订记录 + 更新房态为预留，保证原子性
        using SqlConnection conn = new(DBConnection.GetConnectionString());
        conn.Open();
        using SqlTransaction trans = conn.BeginTransaction();
        try
        {
            info.Status = BusinessConstants.RESERVE_WAITING;
            _dao.Insert(info);

            // 在事务内直接更新房态，确保预订与房态变更原子性一致
            UpdateRoomStatusInTransaction(conn, trans, info.RoomNo, RoomStatusConstants.RESERVED);

            trans.Commit();
        }
        catch
        {
            trans.Rollback();
            throw;
        }

        LogManager.Record(CurrentUser.UserName, BusinessConstants.LOG_RESERVE,
            $"预订房号 {info.RoomNo}", $"预计入住:{info.ExpectCheckIn:yyyy-MM-dd} 天数:{info.ExpectDays}");
    }

    /// <summary>
    /// 取消预订（PRD 6.5 接口契约）
    /// 事务：UPDATE 预订状态→已取消 + UPDATE 房态→空闲
    /// </summary>
    /// <param name="reserveID">预订编号</param>
    /// <exception cref="BusinessException">预订不存在 / 预订状态非待入住</exception>
    public void Cancel(int reserveID)
    {
        ReservationInfo reservation = _dao.GetById(reserveID)
            ?? throw new BusinessException($"预订编号 {reserveID} 不存在");

        // 仅待入住状态的预订可取消，避免取消已入住/已过期的预订
        if (reservation.Status != BusinessConstants.RESERVE_WAITING)
            throw new BusinessException($"预订状态为 {reservation.Status}，仅待入住的预订可取消");

        using SqlConnection conn = new(DBConnection.GetConnectionString());
        conn.Open();
        using SqlTransaction trans = conn.BeginTransaction();
        try
        {
            _dao.UpdateStatus(reserveID, BusinessConstants.RESERVE_CANCELLED);
            UpdateRoomStatusInTransaction(conn, trans, reservation.RoomNo, RoomStatusConstants.FREE);
            trans.Commit();
        }
        catch
        {
            trans.Rollback();
            throw;
        }

        LogManager.Record(CurrentUser.UserName, BusinessConstants.LOG_CANCEL_RESERVE,
            $"取消预订 编号:{reserveID}", $"房号:{reservation.RoomNo}");
    }

    /// <summary>
    /// 预订转入住（PRD 6.5 接口契约）
    /// 事务：UPDATE 预订状态→已入住 + INSERT 入住记录 + UPDATE 房态→在住
    /// </summary>
    /// <param name="reserveID">预订编号</param>
    /// <param name="deposit">押金金额</param>
    /// <returns>新生成的入住单信息</returns>
    /// <exception cref="BusinessException">预订不存在 / 预订状态非待入住 / 押金<0</exception>
    public CheckInInfo ConvertToCheckIn(int reserveID, decimal deposit)
    {
        ReservationInfo reservation = _dao.GetById(reserveID)
            ?? throw new BusinessException($"预订编号 {reserveID} 不存在");

        if (reservation.Status != BusinessConstants.RESERVE_WAITING)
            throw new BusinessException($"预订状态为 {reservation.Status}，仅待入住的预订可转入住");

        if (deposit < 0)
            throw new BusinessException("押金不能为负数");

        // 事务：更新预订状态 + 新增入住记录 + 更新房态，三步原子完成
        using SqlConnection conn = new(DBConnection.GetConnectionString());
        conn.Open();
        using SqlTransaction trans = conn.BeginTransaction();
        try
        {
            // 1. 更新预订状态为已入住
            UpdateReservationStatusInTransaction(conn, trans, reserveID, BusinessConstants.RESERVE_CHECKEDIN);

            // 2. 新增入住记录（事务版本，复用 CheckInDao 的事务方法）
            CheckInInfo checkIn = new()
            {
                CustomerID = reservation.CustomerID,
                RoomNo = reservation.RoomNo,
                CheckInTime = DateTime.Now,
                ExpectCheckOut = reservation.ExpectCheckIn.AddDays(reservation.ExpectDays),
                Deposit = deposit,
                Status = BusinessConstants.CHECKIN_OCCUPIED
            };
            int checkInID = _checkInDao.InsertWithTransaction(checkIn, conn, trans);

            // 3. 更新房态为在住
            UpdateRoomStatusInTransaction(conn, trans, reservation.RoomNo, RoomStatusConstants.OCCUPIED);

            trans.Commit();

            checkIn.CheckInID = checkInID;
            LogManager.Record(CurrentUser.UserName, BusinessConstants.LOG_CHECKIN,
                $"预订转入住 编号:{reserveID}", $"新入住单:{checkInID} 房号:{reservation.RoomNo}");

            return checkIn;
        }
        catch
        {
            trans.Rollback();
            throw;
        }
    }

    /// <summary>
    /// 自动过期超期预订（PRD AC-11.1 / 6.5 接口契约）
    /// 扫描超过预计入住日期1天仍未入住的预订，标记为已过期并恢复房态为空闲
    /// 在登录成功或打开房态看板时调用
    /// </summary>
    public void AutoExpire()
    {
        List<ReservationInfo> expiredList = _dao.GetExpiredReservations();
        if (expiredList.Count == 0) return;

        // 逐条处理过期预订，每条独立事务，避免单条失败影响其他过期处理
        foreach (ReservationInfo reservation in expiredList)
        {
            try
            {
                using SqlConnection conn = new(DBConnection.GetConnectionString());
                conn.Open();
                using SqlTransaction trans = conn.BeginTransaction();
                try
                {
                    UpdateReservationStatusInTransaction(conn, trans, reservation.ReserveID, BusinessConstants.RESERVE_EXPIRED);
                    UpdateRoomStatusInTransaction(conn, trans, reservation.RoomNo, RoomStatusConstants.FREE);
                    trans.Commit();
                }
                catch
                {
                    trans.Rollback();
                }
            }
            catch
            {
                // 单条过期处理失败不影响其他记录，继续处理下一条
            }
        }

        // 过期处理为系统自动行为，记录汇总日志
        LogManager.Record("系统", "自动过期", $"自动过期 {expiredList.Count} 条超期预订");
    }

    /// <summary>预订数据基础校验</summary>
    private static void ValidateReservation(ReservationInfo info)
    {
        if (info.CustomerID <= 0)
            throw new BusinessException("请选择客户");
        if (string.IsNullOrWhiteSpace(info.RoomNo))
            throw new BusinessException("请选择房间");
        if (info.ExpectDays <= 0)
            throw new BusinessException("预计入住天数必须大于0");
        if (string.IsNullOrWhiteSpace(info.ContactPhone))
            throw new BusinessException("联系电话不能为空");
    }

    /// <summary>在事务内更新房间状态（直接执行SQL，避免RoomDao使用独立连接破坏事务）</summary>
    private static void UpdateRoomStatusInTransaction(SqlConnection conn, SqlTransaction trans, string roomNo, string status)
    {
        const string sql = "UPDATE T_Room SET roomStatus = @status WHERE roomNo = @roomNo";
        using SqlCommand cmd = new(sql, conn, trans);
        cmd.Parameters.AddWithValue("@status", status);
        cmd.Parameters.AddWithValue("@roomNo", roomNo);
        cmd.ExecuteNonQuery();
    }

    /// <summary>在事务内更新预订状态</summary>
    private static void UpdateReservationStatusInTransaction(SqlConnection conn, SqlTransaction trans, int reserveID, string status)
    {
        const string sql = "UPDATE T_Reservation SET status = @status WHERE reserveID = @reserveID";
        using SqlCommand cmd = new(sql, conn, trans);
        cmd.Parameters.AddWithValue("@status", status);
        cmd.Parameters.AddWithValue("@reserveID", reserveID);
        cmd.ExecuteNonQuery();
    }
}
