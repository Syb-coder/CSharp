using System;
using System.Collections.Generic;
using LibrarySys.Common;
using LibrarySys.DAL;
using LibrarySys.Models;

namespace LibrarySys.BLL
{
    /// <summary>
    /// 预约业务逻辑层
    /// 处理预约登记、通知取书、取消预约、过期清理等业务逻辑
    /// </summary>
    public class ReservationBiz
    {
        private readonly ReservationDao _dao = new ReservationDao();
        private readonly BookDao _bookDao = new BookDao();
        private readonly ReaderDao _readerDao = new ReaderDao();
        private readonly BorrowDao _borrowDao = new BorrowDao();

        /// <summary>预约登记（读者预约某本书）</summary>
        /// <exception cref="BusinessException">当图书不存在、有库存、已有活跃预约、已达预约上限时抛出</exception>
        public int Reserve(string readerID, string bookID, string userName)
        {
            // 校验读者是否存在
            var reader = _readerDao.FindById(readerID);
            if (reader == null)
                throw new BusinessException("读者不存在，请检查读者编号");

            // 校验图书是否存在
            var book = _bookDao.FindById(bookID);
            if (book == null)
                throw new BusinessException("图书不存在，请检查图书编号");

            // 如果当前有可借库存，提示直接借阅，不适用预约
            if (book.AvailableCount > 0)
                throw new BusinessException($"该书当前有 {book.AvailableCount} 本可借，请直接借阅，无需预约");

            // 检查读者是否已有该书的活跃预约
            if (_dao.HasActiveReservation(readerID, bookID))
                throw new BusinessException("您已预约过该书，请勿重复预约");

            // 检查读者活跃预约数是否已达上限（3本）
            int activeCount = _dao.CountActiveReservations(readerID);
            if (activeCount >= BusinessConstants.MAX_BORROW_LIMIT)
                throw new BusinessException($"您的活跃预约已达 {BusinessConstants.MAX_BORROW_LIMIT} 本上限，请先取消部分预约");

            var now = DateTime.Now;
            var info = new ReservationInfo
            {
                ReaderID = readerID,
                BookID = bookID,
                ReserveDate = now,
                ExpireDate = now.Date.AddDays(BusinessConstants.RESERVE_VALID_DAYS),
                Status = BusinessConstants.RESERVE_QUEUING
            };

            int id = _dao.InsertReservation(info);

            // 记录操作日志
            try
            {
                LogBiz.Log(userName, BusinessConstants.LOG_RESERVE,
                    $"预约：{bookID}", $"读者 {readerID} 预约了 {book.BookName}({bookID})");
            }
            catch { /* 日志写入失败不影响主流程 */ }

            return id;
        }

        /// <summary>通知取书（将排队预约升级为待取书）</summary>
        /// <param name="reserveID">预约编号</param>
        /// <param name="userName">操作者用户名</param>
        /// <exception cref="BusinessException">当预约不存在或状态不是"排队中"时抛出</exception>
        public void NotifyPickup(int reserveID, string userName)
        {
            var reservation = _dao.FindById(reserveID);
            if (reservation == null)
                throw new BusinessException("预约记录不存在");
            if (reservation.Status != BusinessConstants.RESERVE_QUEUING)
                throw new BusinessException($"该预约状态为「{reservation.Status}」，无法通知取书");

            _dao.UpdateStatus(reserveID, BusinessConstants.RESERVE_WAITING,
                notifyDate: DateTime.Now);
        }

        /// <summary>完成预约（读者来取书时标记为已完成）</summary>
        /// <param name="reserveID">预约编号</param>
        /// <param name="userName">操作者用户名</param>
        public void Complete(int reserveID, string userName)
        {
            _dao.UpdateStatus(reserveID, BusinessConstants.RESERVE_COMPLETED,
                completeDate: DateTime.Now);
        }

        /// <summary>取消预约</summary>
        /// <param name="reserveID">预约编号</param>
        /// <param name="reason">取消原因</param>
        /// <param name="userName">操作者用户名</param>
        public void Cancel(int reserveID, string reason, string userName)
        {
            var reservation = _dao.FindById(reserveID);
            if (reservation == null)
                throw new BusinessException("预约记录不存在");

            _dao.UpdateStatus(reserveID, BusinessConstants.RESERVE_CANCELLED,
                cancelDate: DateTime.Now, cancelReason: reason);

            // 记录操作日志
            try
            {
                LogBiz.Log(userName, BusinessConstants.LOG_CANCEL_RESERVE,
                    $"取消预约：{reservation.BookID}",
                    $"读者 {reservation.ReaderID} 取消预约 {reservation.BookName}({reservation.BookID})，原因：{reason}");
            }
            catch { }
        }

        /// <summary>还书后触发预约通知：检查归还的书是否有排队预约，若有则通知最早预约者</summary>
        /// <param name="bookID">归还的图书编号</param>
        /// <param name="userName">操作者用户名</param>
        /// <returns>通知的预约数</returns>
        public int NotifyAfterReturn(string bookID, string userName)
        {
            int notified = 0;
            // 检查当前可借数量
            var book = _bookDao.FindById(bookID);
            if (book == null || book.AvailableCount <= 0)
                return notified;

            // 依次通知排队预约者，每通知一个减一本可借数
            while (book.AvailableCount > 0)
            {
                var first = _dao.GetFirstQueuing(bookID);
                if (first == null) break;

                _dao.UpdateStatus(first.ReserveID, BusinessConstants.RESERVE_WAITING,
                    notifyDate: DateTime.Now);
                notified++;
                book.AvailableCount--;
            }

            return notified;
        }

        /// <summary>清理过期预约（定时任务或登录时触发）</summary>
        /// <returns>清理的过期预约数</returns>
        public int CleanExpired()
        {
            int count = _dao.CancelExpired();
            count += _dao.CancelWaitingExpired();
            return count;
        }

        /// <summary>查询预约列表</summary>
        public IList<ReservationInfo> Search(string? readerID, string? bookID, string? status)
        {
            return _dao.Search(readerID, bookID, status);
        }

        /// <summary>根据 ID 查询预约</summary>
        public ReservationInfo? FindById(int reserveID)
        {
            return _dao.FindById(reserveID);
        }

        // ===== 统计方法 =====

        /// <summary>获取总预约数</summary>
        public int CountTotal()
        {
            return _dao.CountTotal();
        }

        /// <summary>获取活跃预约数</summary>
        public int CountActive()
        {
            return _dao.CountActive();
        }
    }
}