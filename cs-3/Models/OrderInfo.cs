namespace CampusMart.Models;

/// <summary>
/// 订单实体类（对应 tbl_Order 表，11 字段）
/// </summary>
/// <remarks>
/// 与 cs-2 的 Order 差异化命名。
/// 字段差异：新增 orderNo（可读订单号）和 userID（操作员外键）；支付状态取值为"未支付/已支付"。
/// 扩展字段 UserName / OperatorName 用于联表查询结果展示。
/// </remarks>
public class OrderInfo
{
    /// <summary>订单编号（自增主键）</summary>
    public int OrderID { get; set; }

    /// <summary>可读订单号（ORD + yyyyMMdd + 3位流水号）</summary>
    public string OrderNo { get; set; }

    /// <summary>收货人姓名（必填）</summary>
    public string ReceiverName { get; set; }

    /// <summary>收货人手机号（必填，11 位纯数字）</summary>
    public string ReceiverPhone { get; set; }

    /// <summary>收货地址</summary>
    public string ReceiverAddress { get; set; }

    /// <summary>支付方式：现金/微信/支付宝</summary>
    public string PaymentMethod { get; set; }

    /// <summary>支付状态：未支付/已支付</summary>
    public string PaymentStatus { get; set; }

    /// <summary>支付时间（状态变更时填充）</summary>
    public DateTime? PaymentTime { get; set; }

    /// <summary>订单总金额（明细小计之和）</summary>
    public decimal TotalAmount { get; set; }

    /// <summary>下单日期（系统自动填充）</summary>
    public DateTime OrderDate { get; set; }

    /// <summary>操作员编号（外键，创建订单的用户）</summary>
    public int UserID { get; set; }

    // ===== 以下为联表查询扩展字段，非 tbl_Order 实际列 =====

    /// <summary>操作员姓名（联表 tbl_User 获取，用于列表展示）</summary>
    public string OperatorName { get; set; }
}
