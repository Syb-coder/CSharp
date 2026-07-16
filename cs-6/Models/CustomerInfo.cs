namespace HotelSys.Models;

/// <summary>
/// 客户信息实体类（对应 T_Customer 表，PRD 5.1.4）
/// </summary>
public class CustomerInfo
{
    /// <summary>客户编号（主键，自增）</summary>
    public int CustomerID { get; set; }

    /// <summary>姓名</summary>
    public string CustomerName { get; set; }

    /// <summary>性别：男/女</summary>
    public string Gender { get; set; }

    /// <summary>证件类型：身份证/护照/军官证/其他</summary>
    public string IdType { get; set; }

    /// <summary>证件号码</summary>
    public string IdNumber { get; set; }

    /// <summary>手机号（可空）</summary>
    public string Phone { get; set; }

    /// <summary>地址（可空）</summary>
    public string Address { get; set; }

    /// <summary>登记时间</summary>
    public DateTime CreateTime { get; set; }
}
