namespace CampusStore.Models;

/// <summary>
/// 供货商实体类
/// </summary>
/// <remarks>对应数据库表 tbl_Supplier</remarks>
public class SupplierInfo
{
    /// <summary>供货商编号（主键，自增）</summary>
    public int SupplierID { get; set; }

    /// <summary>供货商名称</summary>
    public string SupplierName { get; set; } = string.Empty;

    /// <summary>法人代表</summary>
    public string LegalPerson { get; set; } = string.Empty;

    /// <summary>注册日期</summary>
    public DateTime? RegisterDate { get; set; }

    /// <summary>联系人</summary>
    public string ContactPerson { get; set; } = string.Empty;

    /// <summary>联系电话</summary>
    public string Phone { get; set; } = string.Empty;

    /// <summary>地址</summary>
    public string Address { get; set; } = string.Empty;
}
