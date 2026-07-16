using LibraryManagement.Common;
using LibraryManagement.DAL;
using LibraryManagement.Models;

namespace LibraryManagement.BLL;

/// <summary>
/// 读者业务服务类，处理读者增删改查及关联检查
/// </summary>
public class ReaderService
{
    private readonly ReaderDAL _readerDAL = new();

    /// <summary>
    /// 查询全部读者
    /// </summary>
    /// <returns>读者列表</returns>
    public List<Reader> GetAllReaders()
    {
        return _readerDAL.GetAllReaders();
    }

    /// <summary>
    /// 多条件查询读者
    /// </summary>
    /// <param name="readerID">读者编号（可空）</param>
    /// <param name="readerName">姓名（可空）</param>
    /// <param name="department">院系（可空）</param>
    /// <returns>读者列表</returns>
    public List<Reader> SearchReaders(string readerID, string readerName, string department)
    {
        return _readerDAL.SearchReaders(readerID, readerName, department);
    }

    /// <summary>
    /// 新增读者
    /// </summary>
    /// <param name="reader">读者实体</param>
    /// <exception cref="BusinessException">校验失败</exception>
    public void AddReader(Reader reader)
    {
        ValidateReader(reader);

        // 注册日期默认为当前日期
        reader.RegisterDate ??= DateTime.Today;

        if (!_readerDAL.InsertReader(reader))
        {
            throw new BusinessException("读者编号已存在");
        }
    }

    /// <summary>
    /// 修改读者信息
    /// </summary>
    /// <param name="reader">读者实体</param>
    /// <exception cref="BusinessException">校验失败</exception>
    public void UpdateReader(Reader reader)
    {
        ValidateReader(reader);

        if (!_readerDAL.UpdateReader(reader))
        {
            throw new BusinessException("读者不存在");
        }
    }

    /// <summary>
    /// 删除读者（存在未归还借阅时不允许删除）
    /// </summary>
    /// <param name="readerID">读者编号</param>
    /// <exception cref="BusinessException">存在未归还图书或删除失败</exception>
    public void DeleteReader(string readerID)
    {
        // 参照完整性保护：存在未归还借阅记录的读者不允许删除
        // 否则借阅记录中的 readerID 将变成悬空引用，还书时无法关联读者信息
        int unreturnedCount = _readerDAL.CountUnreturnedBorrows(readerID);
        if (unreturnedCount > 0)
        {
            throw new BusinessException($"存在未归还图书（{unreturnedCount}本），无法删除");
        }

        if (!_readerDAL.DeleteReader(readerID))
        {
            throw new BusinessException("读者不存在或已被删除");
        }
    }

    /// <summary>
    /// 校验读者字段合法性
    /// </summary>
    /// <param name="reader">读者实体</param>
    /// <exception cref="BusinessException">校验失败</exception>
    private static void ValidateReader(Reader reader)
    {
        if (ValidationHelper.IsNullOrWhiteSpace(reader.ReaderID))
        {
            throw new BusinessException("读者编号不能为空");
        }
        if (ValidationHelper.IsExceedLength(reader.ReaderID, 20))
        {
            throw new BusinessException("读者编号长度不能超过20个字符");
        }
        if (ValidationHelper.IsNullOrWhiteSpace(reader.ReaderName))
        {
            throw new BusinessException("姓名不能为空");
        }
        // 姓名长度限制 8 个字符（NVarChar），对应中文姓名最多约 4 个汉字，符合国内姓名惯例
        if (ValidationHelper.IsExceedLength(reader.ReaderName, 8))
        {
            throw new BusinessException("姓名长度不能超过8个字符");
        }
        // 性别校验使用硬编码枚举值"男"/"女"而非枚举类型，因为数据库字段为 NVarChar(2)
        // 业务约束：只接受这两个确定值，拒绝其他任何输入
        if (reader.ReaderSex != "男" && reader.ReaderSex != "女")
        {
            throw new BusinessException("性别必须为男或女");
        }
        if (!ValidationHelper.IsNullOrWhiteSpace(reader.Phone)
            && !ValidationHelper.IsValidPhone(reader.Phone))
        {
            throw new BusinessException("联系电话格式不合法（允许数字、空格、连字符，长度6-15）");
        }
        if (!ValidationHelper.IsNullOrWhiteSpace(reader.Department)
            && ValidationHelper.IsExceedLength(reader.Department, 20))
        {
            throw new BusinessException("所在院系长度不能超过20个字符");
        }
    }
}
