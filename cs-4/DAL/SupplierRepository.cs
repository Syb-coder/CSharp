using System.Data;
using CampusStore.Models;

namespace CampusStore.DAL;

/// <summary>
/// 供货商数据访问类：调用 sp_Supplier_* 存储过程
/// </summary>
public class SupplierRepository : BaseRepository
{
    /// <summary>
    /// 添加供货商
    /// </summary>
    public int Add(SupplierInfo info)
    {
        object result = ExecuteSpScalar("sp_Supplier_Add",
            MakeParam("@supplierName", info.SupplierName),
            MakeParam("@legalPerson", info.LegalPerson),
            MakeParam("@registerDate", info.RegisterDate),
            MakeParam("@contactPerson", info.ContactPerson),
            MakeParam("@phone", info.Phone),
            MakeParam("@address", info.Address));
        return Convert.ToInt32(result);
    }

    /// <summary>
    /// 修改供货商
    /// </summary>
    public int Update(SupplierInfo info)
    {
        object result = ExecuteSpScalar("sp_Supplier_Update",
            MakeParam("@supplierID", info.SupplierID),
            MakeParam("@supplierName", info.SupplierName),
            MakeParam("@legalPerson", info.LegalPerson),
            MakeParam("@registerDate", info.RegisterDate),
            MakeParam("@contactPerson", info.ContactPerson),
            MakeParam("@phone", info.Phone),
            MakeParam("@address", info.Address));
        return Convert.ToInt32(result);
    }

    /// <summary>
    /// 删除供货商
    /// </summary>
    /// <returns>0=有关联商品无法删除，>0=删除成功</returns>
    public int Delete(int supplierID)
    {
        object result = ExecuteSpScalar("sp_Supplier_Delete", MakeParam("@supplierID", supplierID));
        return Convert.ToInt32(result);
    }

    /// <summary>
    /// 查询全部供货商
    /// </summary>
    public DataTable GetAll()
    {
        return ExecuteViewDataTable("(SELECT * FROM tbl_Supplier) AS t");
    }

    /// <summary>
    /// 按名称关键字模糊检索
    /// </summary>
    public DataTable Search(string keyword)
    {
        return ExecuteViewDataTable("(SELECT * FROM tbl_Supplier) AS t",
            "supplierName LIKE @keyword",
            MakeParam("@keyword", $"%{keyword}%"));
    }

    /// <summary>
    /// 查询全部供货商（实体列表形式，供下拉框使用）
    /// </summary>
    public List<SupplierInfo> GetAllList()
    {
        DataTable dt = GetAll();
        return dt.AsEnumerable().Select(MapRow).ToList();
    }

    /// <summary>
    /// DataRow 到实体映射
    /// </summary>
    private static SupplierInfo MapRow(DataRow row) => new()
    {
        SupplierID = row.Field<int>("supplierID"),
        SupplierName = row.Field<string>("supplierName") ?? string.Empty,
        LegalPerson = row.Field<string>("legalPerson") ?? string.Empty,
        RegisterDate = row.Field<DateTime?>("registerDate"),
        ContactPerson = row.Field<string>("contactPerson") ?? string.Empty,
        Phone = row.Field<string>("phone") ?? string.Empty,
        Address = row.Field<string>("address") ?? string.Empty
    };
}
