namespace CampusMart.Models;

/// <summary>
/// 供货商实体类（对应 tbl_Supplier 表）
/// </summary>
/// <remarks>
/// 与 cs-2 的 Supplier 差异化命名。
/// </remarks>
public class SupplierInfo
{
    /// <summary>供货商编号（自增主键）</summary>
    public int SupplierID { get; set; }

    /// <summary>供货商名称（必填）</summary>
    public string SupplierName { get; set; }

    /// <summary>法人代表</summary>
    public string LegalPerson { get; set; }

    /// <summary>注册日期</summary>
    public DateTime? RegisterDate { get; set; }

    /// <summary>联系人</summary>
    public string ContactPerson { get; set; }

    /// <summary>联系电话</summary>
    public string Phone { get; set; }

    /// <summary>地址</summary>
    public string Address { get; set; }
}
