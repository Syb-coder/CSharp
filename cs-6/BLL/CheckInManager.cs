using HotelSys.Common;
using HotelSys.DAL;
using HotelSys.Models;
using Microsoft.Data.SqlClient;

namespace HotelSys.BLL;

/// <summary>
/// 入住业务逻辑类（PRD 4.2.8 / F-12 / F-14 / F-15，核心业务）
/// 入住登记、续住、换房、退房结算，均使用数据库事务保证房态与入住记录原子性
/// </summary>
public class CheckInManager
{
    private readonly CheckInDao _dao = new();
    private readonly RoomDao _roomDao = new();

    /// <summary>查询所有在住记录（PRD F-13 / 6.5 接口契约）</summary>
    public List<CheckInInfo> GetOccupiedList() => _dao.GetOccupiedList();

    /// <summary>查询所有已结账历史记录（PRD 6.5 接口契约）</summary>
    public List<CheckInInfo> GetHistoryList() => _dao.GetHistoryList();

    /// <summary>按入住单号查询单条记录（PRD 6.5 接口契约）</summary>
    public CheckInInfo GetById(int checkInID) => _dao.GetById(checkInID);

    /// <summary>按房号查询在住记录（退房/换房时定位入住单）</summary>
    public CheckInInfo GetOccupiedByRoom(string roomNo) => _dao.GetOccupiedByRoom(roomNo);

    /// <summary>多条件查询入住记录（PRD F-13）</summary>
    public List<CheckInInfo> Search(string roomNo, string customerName, DateTime? from, DateTime? to, string status)
        => _dao.Search(roomNo, customerName, from, to, status);

    /// <summary>
    /// 入住登记（PRD F-12 / 6.5 接口契约）
    /// 事务：INSERT 入住记录 + UPDATE 房态→在住
    /// </summary>
    /// <param name="roomNo">房号</param>
    /// <param name="customerID">客户编号</param>
    /// <param name="days">入住天数（≥1）</param>
    /// <param name="deposit">押金金额（≥0）</param>
    /// <returns>新生成的入住单信息</returns>
    /// <exception cref="BusinessException">房间不存在/非空闲 / 客户不存在 / 天数<1 / 押金<0</exception>
    public CheckInInfo CheckIn(string roomNo, int customerID, int days, decimal deposit)
    {
        // 入住前校验：房间必须存在且为空闲状态，避免重复开房
        RoomInfo room = _roomDao.FindById(roomNo)
            ?? throw new BusinessException($"房号 {roomNo} 不存在");

        if (room.RoomStatus != RoomStatusConstants.FREE)
            throw new BusinessException($"房号 {roomNo} 当前状态为 {room.RoomStatus}，仅空闲房间可入住");

        if (customerID <= 0)
            throw new BusinessException("请选择客户");

        if (days < 1)
            throw new BusinessException("入住天数必须大于0");

        if (deposit < 0)
            throw new BusinessException("押金不能为负数");

        CheckInInfo checkIn = new()
        {
            CustomerID = customerID,
            RoomNo = roomNo,
            CheckInTime = DateTime.Now,
            ExpectCheckOut = DateTime.Now.AddDays(days),
            Deposit = deposit,
            Status = BusinessConstants.CHECKIN_OCCUPIED
        };

        // 事务：插入入住记录 + 更新房态为在住，保证原子性
        using SqlConnection conn = new(DBConnection.GetConnectionString());
        conn.Open();
        using SqlTransaction trans = conn.BeginTransaction();
        try
        {
            int checkInID = _dao.InsertWithTransaction(checkIn, conn, trans);
            UpdateRoomStatusInTransaction(conn, trans, roomNo, RoomStatusConstants.OCCUPIED);
            trans.Commit();

            checkIn.CheckInID = checkInID;
        }
        catch
        {
            trans.Rollback();
            throw;
        }

        LogManager.Record(CurrentUser.UserName, BusinessConstants.LOG_CHECKIN,
            $"入住登记 房号:{roomNo}", $"入住单:{checkIn.CheckInID} 天数:{days} 押金:{deposit}");

        return checkIn;
    }

    /// <summary>
    /// 续住办理（PRD F-14 / 6.5 接口契约）
    /// 修改预计退房日期=原预计退房日期+续住天数
    /// </summary>
    /// <param name="checkInID">入住单号</param>
    /// <param name="extraDays">续住天数（≥1）</param>
    /// <exception cref="BusinessException">入住单不存在/已退房 / 续住天数<1</exception>
    public void ExtendStay(int checkInID, int extraDays)
    {
        CheckInInfo checkIn = _dao.GetById(checkInID)
            ?? throw new BusinessException($"入住单号 {checkInID} 不存在");

        // 仅在住状态的入住单可续住，避免对已退房记录操作
        if (checkIn.Status != BusinessConstants.CHECKIN_OCCUPIED)
            throw new BusinessException($"入住单 {checkInID} 状态为 {checkIn.Status}，仅在住记录可续住");

        if (extraDays < 1)
            throw new BusinessException("续住天数必须大于0");

        // 新预计退房日期=原预计退房日期+续住天数
        checkIn.ExpectCheckOut = checkIn.ExpectCheckOut.AddDays(extraDays);
        _dao.Update(checkIn);

        LogManager.Record(CurrentUser.UserName, BusinessConstants.LOG_EXTEND,
            $"续住 入住单:{checkInID}", $"续住{extraDays}天，新退房日期:{checkIn.ExpectCheckOut:yyyy-MM-dd}");
    }

    /// <summary>
    /// 换房办理（PRD F-15 / 6.5 接口契约，简化方案）
    /// 事务：UPDATE 入住单 roomNo+remark + UPDATE 原房态→空闲 + UPDATE 新房态→在住
    /// 简化处理：在 remark 字段记录换房轨迹，退房时按新房间价格计算（可手动调整）
    /// </summary>
    /// <param name="checkInID">入住单号</param>
    /// <param name="newRoomNo">新房间号</param>
    /// <exception cref="BusinessException">入住单不存在/已退房 / 新房间不存在/非空闲 / 新旧房间相同</exception>
    public void ChangeRoom(int checkInID, string newRoomNo)
    {
        CheckInInfo checkIn = _dao.GetById(checkInID)
            ?? throw new BusinessException($"入住单号 {checkInID} 不存在");

        if (checkIn.Status != BusinessConstants.CHECKIN_OCCUPIED)
            throw new BusinessException($"入住单 {checkInID} 状态为 {checkIn.Status}，仅在住记录可换房");

        if (checkIn.RoomNo == newRoomNo)
            throw new BusinessException("新房间与原房间相同，无需换房");

        // 新房间必须存在且为空闲状态
        RoomInfo newRoom = _roomDao.FindById(newRoomNo)
            ?? throw new BusinessException($"新房号 {newRoomNo} 不存在");

        if (newRoom.RoomStatus != RoomStatusConstants.FREE)
            throw new BusinessException($"新房号 {newRoomNo} 状态为 {newRoom.RoomStatus}，仅空闲房间可换入");

        // 在 remark 中记录换房轨迹，供退房结算时参考
        string changeLog = $"[换房] {DateTime.Now:yyyy-MM-dd HH:mm} 从{checkIn.RoomNo}换至{newRoomNo}";
        string newRemark = string.IsNullOrEmpty(checkIn.Remark)
            ? changeLog
            : $"{checkIn.Remark}；{changeLog}";

        // 事务：更新入住单房号+备注 + 原房态→空闲 + 新房态→在住
        using SqlConnection conn = new(DBConnection.GetConnectionString());
        conn.Open();
        using SqlTransaction trans = conn.BeginTransaction();
        try
        {
            // 更新入住单：房号改为新房间，备注追加换房记录
            checkIn.RoomNo = newRoomNo;
            checkIn.Remark = newRemark;
            _dao.UpdateWithTransaction(checkIn, conn, trans);

            UpdateRoomStatusInTransaction(conn, trans, checkIn.RoomNo, RoomStatusConstants.FREE);
            UpdateRoomStatusInTransaction(conn, trans, newRoomNo, RoomStatusConstants.OCCUPIED);

            trans.Commit();
        }
        catch
        {
            trans.Rollback();
            throw;
        }

        LogManager.Record(CurrentUser.UserName, BusinessConstants.LOG_CHANGE_ROOM,
            $"换房 入住单:{checkInID}", changeLog);
    }

    /// <summary>
    /// 退房结算（PRD 4.2.10 / 6.5 接口契约）
    /// 事务：UPDATE 入住单结账字段 + UPDATE 房态→空闲
    /// 最终保存的数字以操作员手动填写/确认的值为准
    /// </summary>
    /// <param name="info">包含手动填写费用字段的入住单实体</param>
    /// <exception cref="BusinessException">入住单不存在/已退房</exception>
    public void CheckOut(CheckInInfo info)
    {
        CheckInInfo original = _dao.GetById(info.CheckInID)
            ?? throw new BusinessException($"入住单号 {info.CheckInID} 不存在");

        // 仅在住状态的入住单可退房，避免重复结账
        if (original.Status != BusinessConstants.CHECKIN_OCCUPIED)
            throw new BusinessException($"入住单 {info.CheckInID} 状态为 {original.Status}，仅在住记录可退房");

        // 设置结账快照字段：实际退房时间、状态改为已结账
        info.CheckOutTime = DateTime.Now;
        info.Status = BusinessConstants.CHECKIN_CHECKEDOUT;
        // 保留原始入住时间和客户等不可变字段
        info.CustomerID = original.CustomerID;
        info.CheckInTime = original.CheckInTime;
        info.Deposit = original.Deposit;
        if (string.IsNullOrWhiteSpace(info.Remark))
            info.Remark = original.Remark;

        // 事务：更新入住单结账字段 + 房态恢复空闲
        using SqlConnection conn = new(DBConnection.GetConnectionString());
        conn.Open();
        using SqlTransaction trans = conn.BeginTransaction();
        try
        {
            _dao.UpdateWithTransaction(info, conn, trans);
            UpdateRoomStatusInTransaction(conn, trans, original.RoomNo, RoomStatusConstants.FREE);
            trans.Commit();
        }
        catch
        {
            trans.Rollback();
            throw;
        }

        // 计算应收应退用于日志记录（总金额-押金，正数为应收，负数为应退）
        decimal balance = (info.TotalAmount ?? 0) - original.Deposit;
        string balanceDesc = balance >= 0 ? $"应收:{balance:F2}" : $"应退:{-balance:F2}";

        LogManager.Record(CurrentUser.UserName, BusinessConstants.LOG_CHECKOUT,
            $"退房结账 入住单:{info.CheckInID} 房号:{original.RoomNo}",
            $"总金额:{info.TotalAmount} 押金:{original.Deposit} {balanceDesc}");
    }

    /// <summary>在事务内更新房间状态（直接执行SQL，保证与入住记录变更同事务）</summary>
    private static void UpdateRoomStatusInTransaction(SqlConnection conn, SqlTransaction trans, string roomNo, string status)
    {
        const string sql = "UPDATE T_Room SET roomStatus = @status WHERE roomNo = @roomNo";
        using SqlCommand cmd = new(sql, conn, trans);
        cmd.Parameters.AddWithValue("@status", status);
        cmd.Parameters.AddWithValue("@roomNo", roomNo);
        cmd.ExecuteNonQuery();
    }
}
