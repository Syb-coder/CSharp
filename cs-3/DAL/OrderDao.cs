using System.Data;
using CampusMart.Models;
using CampusMart.Common;
using Microsoft.Data.SqlClient;

namespace CampusMart.DAL;

/// <summary>
/// 订单数据访问类，对应 tbl_Order 和 tbl_OrderItem 表
/// </summary>
/// <remarks>
/// 与 cs-2 差异化：
///   1. 命名 OrderDao（cs-2 为 OrderDAL）
///   2. 事务在 DAO 内部管理（cs-2 由 BLL 传入 SqlTransaction）
///   3. 订单表含 orderNo（可读订单号）和 userID（操作员外键）
///   4. 订单明细小计字段名为 subtotal（cs-2 为 totalPrice）
///   5. 支付状态取值为"未支付/已支付"（cs-2 为"待支付/已支付"）
/// </remarks>
public class OrderDao : BaseDao
{
    /// <summary>
    /// 创建订单（事务操作：插入订单头 + 插入明细 + 扣减库存，三者原子性提交）
    /// </summary>
    /// <param name="order">订单头信息（orderNo 在此方法内生成）</param>
    /// <param name="items">订单明细列表（productName/unitPrice 为快照值，subtotal 已计算）</param>
    /// <returns>成功返回 true</returns>
    /// <exception cref="InvalidOperationException">库存不足时抛出（由 BLL 转为 BusinessException）</exception>
    public bool CreateOrder(OrderInfo order, List<OrderItemInfo> items)
    {
        // 事务在 DAO 内部完整管理，BLL 无需感知连接与事务生命周期（与 cs-2 差异化）
        using SqlConnection conn = CreateConnection();
        conn.Open();
        using SqlTransaction tran = conn.BeginTransaction();

        try
        {
            // 1. 生成可读订单号：ORD + yyyyMMdd + 3位流水号
            order.OrderNo = GenerateOrderNo(conn, tran);

            // 2. 插入订单头记录
            const string insertOrderSql = @"INSERT INTO tbl_Order
                (orderNo, receiverName, receiverPhone, receiverAddress, paymentMethod, paymentStatus, paymentTime, totalAmount, orderDate, userID)
                VALUES (@orderNo, @receiverName, @receiverPhone, @receiverAddress, @paymentMethod, @paymentStatus, @paymentTime, @totalAmount, @orderDate, @userID);
                SELECT CAST(SCOPE_IDENTITY() AS INT);";
            using (SqlCommand cmd = new(insertOrderSql, conn, tran))
            {
                cmd.Parameters.AddRange(new SqlParameter[]
                {
                    new("@orderNo", SqlDbType.NVarChar, 20) { Value = order.OrderNo },
                    new("@receiverName", SqlDbType.NVarChar, 20) { Value = order.ReceiverName },
                    new("@receiverPhone", SqlDbType.NVarChar, 11) { Value = order.ReceiverPhone },
                    new("@receiverAddress", SqlDbType.NVarChar, 200) { Value = (object)order.ReceiverAddress ?? DBNull.Value },
                    new("@paymentMethod", SqlDbType.NVarChar, 10) { Value = (object)order.PaymentMethod ?? DBNull.Value },
                    new("@paymentStatus", SqlDbType.NVarChar, 10) { Value = order.PaymentStatus },
                    new("@paymentTime", SqlDbType.DateTime) { Value = (object)order.PaymentTime ?? DBNull.Value },
                    new("@totalAmount", SqlDbType.Decimal) { Value = order.TotalAmount },
                    new("@orderDate", SqlDbType.DateTime) { Value = order.OrderDate },
                    new("@userID", SqlDbType.Int) { Value = order.UserID }
                });
                order.OrderID = (int)cmd.ExecuteScalar();
            }

            // 3. 逐条插入订单明细，并扣减对应商品库存
            const string insertItemSql = @"INSERT INTO tbl_OrderItem
                (orderID, productID, productName, unitPrice, quantity, subtotal)
                VALUES (@orderID, @productID, @productName, @unitPrice, @quantity, @subtotal)";
            // 扣减库存带 stockQty >= @qty 条件：防御性校验，并发场景下若库存已被其他订单扣减则影响 0 行
            const string deductStockSql = "UPDATE tbl_Product SET stockQty = stockQty - @qty WHERE productID = @productID AND stockQty >= @qty";

            foreach (OrderItemInfo item in items)
            {
                // 插入明细
                using (SqlCommand cmd = new(insertItemSql, conn, tran))
                {
                    cmd.Parameters.AddRange(new SqlParameter[]
                    {
                        new("@orderID", SqlDbType.Int) { Value = order.OrderID },
                        new("@productID", SqlDbType.Int) { Value = item.ProductID },
                        new("@productName", SqlDbType.NVarChar, 50) { Value = item.ProductName },
                        new("@unitPrice", SqlDbType.Decimal) { Value = item.UnitPrice },
                        new("@quantity", SqlDbType.Int) { Value = item.Quantity },
                        new("@subtotal", SqlDbType.Decimal) { Value = item.Subtotal }
                    });
                    cmd.ExecuteNonQuery();
                }
                // 扣减库存
                using (SqlCommand cmd = new(deductStockSql, conn, tran))
                {
                    cmd.Parameters.AddRange(new SqlParameter[]
                    {
                        new("@qty", SqlDbType.Int) { Value = item.Quantity },
                        new("@productID", SqlDbType.Int) { Value = item.ProductID }
                    });
                    int affected = cmd.ExecuteNonQuery();
                    // 影响行数为 0 说明库存不足（并发或校验遗漏），抛异常触发回滚
                    if (affected == 0)
                    {
                        throw new InvalidOperationException($"商品【{item.ProductName}】库存不足，当前库存不足以满足订购数量 {item.Quantity}");
                    }
                }
            }

            tran.Commit();
            return true;
        }
        catch
        {
            // 经验四：用内层 try-catch 包裹 Rollback，防止 Rollback 异常掩盖原始业务异常
            try { tran.Rollback(); } catch { /* 忽略 Rollback 自身的异常 */ }
            throw; // 始终传播原始异常
        }
    }

    /// <summary>
    /// 生成可读订单号：ORD + yyyyMMdd + 3位流水号（如 ORD20260711001）
    /// </summary>
    /// <param name="conn">已打开的连接</param>
    /// <param name="tran">当前事务</param>
    /// <returns>新的订单号</returns>
    /// <remarks>
    /// 流水号 = 当天已有订单数 + 1。课程设计阶段不处理高并发，若并发产生重复，
    /// 数据库 UNIQUE 约束会拦截，事务回滚，UI 层提示重试即可。
    /// </remarks>
    private static string GenerateOrderNo(SqlConnection conn, SqlTransaction tran)
    {
        string datePart = DateTime.Now.ToString("yyyyMMdd");
        string prefix = "ORD" + datePart;
        const string sql = "SELECT COUNT(1) FROM tbl_Order WHERE orderNo LIKE @prefix";
        using SqlCommand cmd = new(sql, conn, tran);
        cmd.Parameters.Add(new SqlParameter("@prefix", SqlDbType.NVarChar, 20) { Value = prefix + "%" });
        int count = Convert.ToInt32(cmd.ExecuteScalar());
        // 流水号 +1 并左补零至 3 位
        return prefix + (count + 1).ToString("D3");
    }

    /// <summary>
    /// 查询全部订单（联表获取操作员姓名）
    /// </summary>
    /// <returns>订单列表</returns>
    public List<OrderInfo> GetAllOrders()
    {
        const string sql = @"SELECT o.orderID, o.orderNo, o.receiverName, o.receiverPhone, o.receiverAddress,
                                   o.paymentMethod, o.paymentStatus, o.paymentTime, o.totalAmount, o.orderDate, o.userID,
                                   u.realName AS operatorName
                            FROM tbl_Order o
                            LEFT JOIN tbl_User u ON o.userID = u.userID
                            ORDER BY o.orderID DESC";
        DataTable table = ExecuteDataTable(sql);
        List<OrderInfo> list = new(table.Rows.Count);
        foreach (DataRow row in table.Rows)
        {
            list.Add(MapRowToOrder(row));
        }
        return list;
    }

    /// <summary>
    /// 按多条件组合检索订单（所有条件均可为空，空表示不限制）
    /// </summary>
    /// <param name="orderNo">订单号关键字（模糊匹配，空则不限）</param>
    /// <param name="receiverName">收货人姓名关键字（模糊匹配，空则不限）</param>
    /// <param name="paymentStatus">支付状态（空则不限）</param>
    /// <param name="dateFrom">下单日期下限（含，null 则不限）</param>
    /// <param name="dateTo">下单日期上限（含，null 则不限）</param>
    /// <returns>匹配的订单列表</returns>
    public List<OrderInfo> SearchOrders(string orderNo, string receiverName, string paymentStatus, DateTime? dateFrom, DateTime? dateTo)
    {
        System.Text.StringBuilder sb = new();
        sb.Append(@"SELECT o.orderID, o.orderNo, o.receiverName, o.receiverPhone, o.receiverAddress,
                          o.paymentMethod, o.paymentStatus, o.paymentTime, o.totalAmount, o.orderDate, o.userID,
                          u.realName AS operatorName
                   FROM tbl_Order o
                   LEFT JOIN tbl_User u ON o.userID = u.userID
                   WHERE 1=1");
        List<SqlParameter> ps = new();
        if (!string.IsNullOrWhiteSpace(orderNo))
        {
            sb.Append(" AND o.orderNo LIKE @orderNo");
            ps.Add(new SqlParameter("@orderNo", SqlDbType.NVarChar, 20) { Value = $"%{orderNo}%" });
        }
        if (!string.IsNullOrWhiteSpace(receiverName))
        {
            sb.Append(" AND o.receiverName LIKE @receiverName");
            ps.Add(new SqlParameter("@receiverName", SqlDbType.NVarChar, 20) { Value = $"%{receiverName}%" });
        }
        if (!string.IsNullOrWhiteSpace(paymentStatus))
        {
            sb.Append(" AND o.paymentStatus = @paymentStatus");
            ps.Add(new SqlParameter("@paymentStatus", SqlDbType.NVarChar, 10) { Value = paymentStatus });
        }
        if (dateFrom.HasValue)
        {
            sb.Append(" AND o.orderDate >= @dateFrom");
            ps.Add(new SqlParameter("@dateFrom", SqlDbType.DateTime) { Value = dateFrom.Value });
        }
        if (dateTo.HasValue)
        {
            // dateTo 取当天结束（23:59:59），保证包含当天的订单
            sb.Append(" AND o.orderDate <= @dateTo");
            ps.Add(new SqlParameter("@dateTo", SqlDbType.DateTime) { Value = dateTo.Value.Date.AddDays(1).AddSeconds(-1) });
        }
        sb.Append(" ORDER BY o.orderID DESC");

        DataTable table = ExecuteDataTable(sb.ToString(), ps.ToArray());
        List<OrderInfo> list = new(table.Rows.Count);
        foreach (DataRow row in table.Rows)
        {
            list.Add(MapRowToOrder(row));
        }
        return list;
    }

    /// <summary>
    /// 查询指定订单的商品明细列表
    /// </summary>
    /// <param name="orderID">订单编号</param>
    /// <returns>订单明细列表</returns>
    public List<OrderItemInfo> GetOrderItems(int orderID)
    {
        const string sql = "SELECT itemID, orderID, productID, productName, unitPrice, quantity, subtotal FROM tbl_OrderItem WHERE orderID = @orderID ORDER BY itemID";
        SqlParameter p = new("@orderID", SqlDbType.Int) { Value = orderID };
        DataTable table = ExecuteDataTable(sql, p);
        List<OrderItemInfo> list = new(table.Rows.Count);
        foreach (DataRow row in table.Rows)
        {
            list.Add(MapRowToOrderItem(row));
        }
        return list;
    }

    /// <summary>
    /// 更新订单支付状态（未支付 → 已支付，同时记录支付时间）
    /// </summary>
    /// <param name="orderID">订单编号</param>
    /// <param name="status">目标支付状态</param>
    /// <returns>成功返回 true</returns>
    public bool UpdatePaymentStatus(int orderID, string status)
    {
        // 仅当状态变更为"已支付"时记录支付时间，变更为"未支付"则清空支付时间
        DateTime? paymentTime = status == PaymentConstants.PAID ? DateTime.Now : (DateTime?)null;
        const string sql = "UPDATE tbl_Order SET paymentStatus = @paymentStatus, paymentTime = @paymentTime WHERE orderID = @orderID";
        SqlParameter[] ps =
        {
            new("@paymentStatus", SqlDbType.NVarChar, 10) { Value = status },
            new("@paymentTime", SqlDbType.DateTime) { Value = (object)paymentTime ?? DBNull.Value },
            new("@orderID", SqlDbType.Int) { Value = orderID }
        };
        return ExecuteNonQuery(sql, ps) > 0;
    }

    /// <summary>
    /// 将 DataRow 映射为 OrderInfo 实体
    /// </summary>
    private static OrderInfo MapRowToOrder(DataRow row)
    {
        return new OrderInfo
        {
            OrderID = Convert.ToInt32(row["orderID"]),
            OrderNo = row["orderNo"].ToString(),
            ReceiverName = row["receiverName"].ToString(),
            ReceiverPhone = row["receiverPhone"].ToString(),
            ReceiverAddress = row["receiverAddress"] == DBNull.Value ? null : row["receiverAddress"].ToString(),
            PaymentMethod = row["paymentMethod"] == DBNull.Value ? null : row["paymentMethod"].ToString(),
            PaymentStatus = row["paymentStatus"].ToString(),
            PaymentTime = row["paymentTime"] == DBNull.Value ? null : Convert.ToDateTime(row["paymentTime"]),
            TotalAmount = Convert.ToDecimal(row["totalAmount"]),
            OrderDate = Convert.ToDateTime(row["orderDate"]),
            UserID = Convert.ToInt32(row["userID"]),
            OperatorName = row["operatorName"] == DBNull.Value ? null : row["operatorName"].ToString()
        };
    }

    /// <summary>
    /// 将 DataRow 映射为 OrderItemInfo 实体
    /// </summary>
    private static OrderItemInfo MapRowToOrderItem(DataRow row)
    {
        return new OrderItemInfo
        {
            ItemID = Convert.ToInt32(row["itemID"]),
            OrderID = Convert.ToInt32(row["orderID"]),
            ProductID = Convert.ToInt32(row["productID"]),
            ProductName = row["productName"].ToString(),
            UnitPrice = Convert.ToDecimal(row["unitPrice"]),
            Quantity = Convert.ToInt32(row["quantity"]),
            Subtotal = Convert.ToDecimal(row["subtotal"])
        };
    }
}
