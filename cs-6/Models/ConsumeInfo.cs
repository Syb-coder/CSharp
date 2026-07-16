namespace HotelSys.Models;

/// <summary>
/// 消费记录实体类（对应 T_Consume 表，PRD 5.1.7）
/// 记录在住客人的附加消费（餐饮/小商品/洗衣/其他），退房时统一结算
/// </summary>
public class ConsumeInfo
{
    /// <summary>消费编号（主键，自增）</summary>
    public int ConsumeID { get; set; }

    /// <summary>入住单号（外键→T_CheckIn）</summary>
    public int CheckInID { get; set; }

    /// <summary>消费项目：餐饮/小商品/洗衣/其他</summary>
    public string ItemName { get; set; }

    /// <summary>消费金额</summary>
    public decimal Amount { get; set; }

    /// <summary>消费时间</summary>
    public DateTime ConsumeTime { get; set; }

    /// <summary>备注（可空）</summary>
    public string Remark { get; set; }

    // ===== 关联显示字段（JOIN 查询时填充，非数据库列） =====

    /// <summary>房号（关联 T_CheckIn→T_Room，用于显示）</summary>
    public string RoomNo { get; set; }

    /// <summary>客户姓名（关联 T_CheckIn→T_Customer，用于显示）</summary>
    public string CustomerName { get; set; }
}
