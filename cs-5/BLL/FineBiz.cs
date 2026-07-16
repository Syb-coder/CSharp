using LibrarySys.DAL;
using LibrarySys.Models;
using LibrarySys.Common;

namespace LibrarySys.BLL;

/// <summary>
/// 罚款业务逻辑类
/// </summary>
public class FineBiz
{
    private readonly FineDao _dao = new();

    /// <summary>查询全部罚款记录</summary>
    public List<FineInfo> GetAll() => _dao.FindAll();

    /// <summary>按读者编号、罚款状态查询</summary>
    public List<FineInfo> Search(string readerID, string fineStatus) => _dao.Search(readerID, fineStatus);

    /// <summary>
    /// 登记缴费：将未缴罚款更新为已缴
    /// </summary>
    /// <param name="fineID">罚款编号</param>
    /// <exception cref="BusinessException">罚款记录不存在 / 罚款已缴清</exception>
    public void Pay(int fineID)
    {
        FineInfo fine = _dao.FindById(fineID)
            ?? throw new BusinessException($"罚款编号 {fineID} 不存在");

        // 已缴罚款不允许重复缴费
        if (fine.FineStatus == BusinessConstants.FINE_PAID)
            throw new BusinessException("该罚款已缴清，无需重复缴费");

        fine.FineStatus = BusinessConstants.FINE_PAID;
        fine.PayDate = DateTime.Today;
        _dao.Update(fine);
    }

    /// <summary>
    /// 检查读者是否有未缴清罚款（借书前校验）
    /// </summary>
    /// <param name="readerID">读者编号</param>
    /// <returns>有未缴罚款返回 true，否则 false</returns>
    public bool HasUnpaidFine(string readerID)
        => _dao.FindUnpaidByReader(readerID).Count > 0;
}
