using LibrarySys.DAL;
using LibrarySys.Models;

namespace LibrarySys.BLL;

/// <summary>
/// 读者业务逻辑类
/// </summary>
public class ReaderBiz
{
    private readonly ReaderDao _dao = new();

    /// <summary>查询全部读者</summary>
    public List<ReaderInfo> GetAll() => _dao.FindAll();

    /// <summary>按编号查询读者</summary>
    public ReaderInfo GetById(string readerID) => _dao.FindById(readerID);

    /// <summary>
    /// 新增读者
    /// </summary>
    /// <param name="entity">读者实体</param>
    /// <exception cref="BusinessException">编号已存在 / 必填项为空</exception>
    public void Add(ReaderInfo entity)
    {
        if (string.IsNullOrWhiteSpace(entity.ReaderID))
            throw new BusinessException("读者编号不能为空");
        if (string.IsNullOrWhiteSpace(entity.ReaderName))
            throw new BusinessException("姓名不能为空");
        if (string.IsNullOrWhiteSpace(entity.ReaderSex))
            throw new BusinessException("请选择性别");

        if (_dao.FindById(entity.ReaderID) != null)
            throw new BusinessException($"读者编号 {entity.ReaderID} 已存在");

        entity.RegisterDate ??= DateTime.Today;

        _dao.Insert(entity);
    }

    /// <summary>
    /// 修改读者信息
    /// </summary>
    /// <param name="entity">读者实体</param>
    /// <exception cref="BusinessException">读者不存在 / 必填项为空</exception>
    public void Update(ReaderInfo entity)
    {
        if (string.IsNullOrWhiteSpace(entity.ReaderName))
            throw new BusinessException("姓名不能为空");
        if (string.IsNullOrWhiteSpace(entity.ReaderSex))
            throw new BusinessException("请选择性别");
        if (_dao.FindById(entity.ReaderID) == null)
            throw new BusinessException($"读者编号 {entity.ReaderID} 不存在");

        _dao.Update(entity);
    }

    /// <summary>
    /// 删除读者（业务校验后级联删除历史记录）
    /// 硬约束：有未归还借阅、未缴罚款、活跃预约（排队中/待取书）时禁止删除
    /// 历史记录（已还借阅、已缴罚款、已完成/取消预约）在事务中一并删除
    /// </summary>
    /// <param name="readerID">读者编号</param>
    /// <exception cref="BusinessException">存在业务约束导致无法删除</exception>
    public void Delete(string readerID)
    {
        if (_dao.HasActiveBorrow(readerID))
            throw new BusinessException("该读者有未归还的图书，无法删除");
        if (_dao.HasUnpaidFine(readerID))
            throw new BusinessException("该读者有未缴清的罚款，无法删除");
        if (_dao.HasActiveReservation(readerID))
            throw new BusinessException("该读者有进行中的预约（排队中/待取书），请先取消预约再删除");

        _dao.DeleteCascade(readerID);
    }

    /// <summary>按编号、姓名多条件查询</summary>
    public List<ReaderInfo> Search(string readerID, string readerName)
        => _dao.Search(readerID, readerName);
}
