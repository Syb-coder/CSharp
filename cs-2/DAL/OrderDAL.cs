using System.Data;
using CampusShop.Models;
using Microsoft.Data.SqlClient;

namespace CampusShop.DAL;

/// <summary>
/// 订单数据访问类，对应 tbl_Order 和 tbl_OrderItem 表的 CRUD 操作
/// </summary>
public class OrderDAL
{
    /// <summary>
    /// 查询全部订单（含明细列表）
    /// </summary>
    /// <returns>订单列表</returns>
    public List<Order> GetAllOrders()
    {
        // 先查询所有订单主表记录，再逐条加载明细
        const string orderSql = @"
            SELECT orderID, orderDate, paymentMethod, paymentTime, paymentStatus,
                   receiverName, receiverPhone, receiverAddress, totalAmount
            FROM tbl_Order
            ORDER BY orderDate DESC, orderID DESC";
        List<Order> list = new();
        using SqlConnection conn = new(DBConnection.GetConnectionString());
        using SqlCommand cmd = new(orderSql, conn);
        conn.Open();
        using SqlDataReader reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(MapReaderToOrder(reader));
        }
        reader.Close();

        // 为每笔订单加载明细列表
        foreach (Order order in list)
        {
            order.Items = GetOrderItems(conn, order.OrderID);
        }
        return list;
    }

    /// <summary>
    /// 按多条件检索订单
    /// </summary>
    /// <param name="receiverName">收货人名称关键字（可为空）</param>
    /// <param name="paymentStatus">支付状态（可为空）</param>
    /// <param name="startDate">开始日期（可为空）</param>
    /// <param name="endDate">结束日期（可为空）</param>
    /// <returns>订单列表</returns>
    public List<Order> SearchOrders(string receiverName, string paymentStatus, DateTime? startDate, DateTime? endDate)
    {
        List<string> conditions = new();
        if (!string.IsNullOrEmpty(receiverName))
        {
            conditions.Add("receiverName LIKE '%' + @receiverName + '%'");
        }
        if (!string.IsNullOrEmpty(paymentStatus))
        {
            conditions.Add("paymentStatus = @paymentStatus");
        }
        if (startDate.HasValue)
        {
            conditions.Add("orderDate >= @startDate");
        }
        if (endDate.HasValue)
        {
            conditions.Add("orderDate <= @endDate");
        }

        string whereClause = conditions.Count > 0 ? "WHERE " + string.Join(" AND ", conditions) : "";
        string sql = $@"
            SELECT orderID, orderDate, paymentMethod, paymentTime, paymentStatus,
                   receiverName, receiverPhone, receiverAddress, totalAmount
            FROM tbl_Order
            {whereClause}
            ORDER BY orderDate DESC, orderID DESC";

        List<Order> list = new();
        using SqlConnection conn = new(DBConnection.GetConnectionString());
        using SqlCommand cmd = new(sql, conn);
        if (!string.IsNullOrEmpty(receiverName))
        {
            cmd.Parameters.Add(new SqlParameter("@receiverName", SqlDbType.NVarChar, 20) { Value = receiverName });
        }
        if (!string.IsNullOrEmpty(paymentStatus))
        {
            cmd.Parameters.Add(new SqlParameter("@paymentStatus", SqlDbType.NVarChar, 10) { Value = paymentStatus });
        }
        if (startDate.HasValue)
        {
            cmd.Parameters.Add(new SqlParameter("@startDate", SqlDbType.Date) { Value = startDate.Value });
        }
        if (endDate.HasValue)
        {
            cmd.Parameters.Add(new SqlParameter("@endDate", SqlDbType.Date) { Value = endDate.Value });
        }
        conn.Open();
        using SqlDataReader reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(MapReaderToOrder(reader));
        }
        reader.Close();

        foreach (Order order in list)
        {
            order.Items = GetOrderItems(conn, order.OrderID);
        }
        return list;
    }

    /// <summary>
    /// 插入订单主表记录（在事务中调用），返回新生成的 orderID
    /// </summary>
    /// <param name="transaction">外部事务对象</param>
    /// <param name="conn">已打开的连接</param>
    /// <param name="order">订单实体</param>
    /// <returns>新生成的订单编号</returns>
    public int InsertOrder(SqlTransaction transaction, SqlConnection conn, Order order)
    {
        const string sql = @"INSERT INTO tbl_Order
            (orderDate, paymentMethod, paymentTime, paymentStatus,
             receiverName, receiverPhone, receiverAddress, totalAmount)
            VALUES (@orderDate, @paymentMethod, @paymentTime, @paymentStatus,
                    @receiverName, @receiverPhone, @receiverAddress, @totalAmount);
            SELECT CAST(SCOPE_IDENTITY() AS INT);";
        using SqlCommand cmd = new(sql, conn, transaction);
        cmd.Parameters.Add(new SqlParameter("@orderDate", SqlDbType.Date) { Value = order.OrderDate });
        cmd.Parameters.Add(new SqlParameter("@paymentMethod", SqlDbType.NVarChar, 20) { Value = (object)order.PaymentMethod ?? DBNull.Value });
        cmd.Parameters.Add(new SqlParameter("@paymentTime", SqlDbType.DateTime) { Value = (object)order.PaymentTime ?? DBNull.Value });
        cmd.Parameters.Add(new SqlParameter("@paymentStatus", SqlDbType.NVarChar, 10) { Value = order.PaymentStatus });
        cmd.Parameters.Add(new SqlParameter("@receiverName", SqlDbType.NVarChar, 20) { Value = (object)order.ReceiverName ?? DBNull.Value });
        cmd.Parameters.Add(new SqlParameter("@receiverPhone", SqlDbType.NVarChar, 15) { Value = (object)order.ReceiverPhone ?? DBNull.Value });
        cmd.Parameters.Add(new SqlParameter("@receiverAddress", SqlDbType.NVarChar, 200) { Value = (object)order.ReceiverAddress ?? DBNull.Value });
        cmd.Parameters.Add(new SqlParameter("@totalAmount", SqlDbType.Decimal) { Value = order.TotalAmount, Precision = 10, Scale = 2 });
        return (int)cmd.ExecuteScalar();
    }

    /// <summary>
    /// 插入订单明细记录（在事务中调用）
    /// </summary>
    /// <param name="transaction">外部事务对象</param>
    /// <param name="conn">已打开的连接</param>
    /// <param name="item">订单明细实体</param>
    public void InsertOrderItem(SqlTransaction transaction, SqlConnection conn, OrderItem item)
    {
        const string sql = @"INSERT INTO tbl_OrderItem
            (orderID, productID, productName, unitPrice, quantity, totalPrice)
            VALUES (@orderID, @productID, @productName, @unitPrice, @quantity, @totalPrice)";
        using SqlCommand cmd = new(sql, conn, transaction);
        cmd.Parameters.Add(new SqlParameter("@orderID", SqlDbType.Int) { Value = item.OrderID });
        cmd.Parameters.Add(new SqlParameter("@productID", SqlDbType.NVarChar, 20) { Value = item.ProductID });
        cmd.Parameters.Add(new SqlParameter("@productName", SqlDbType.NVarChar, 50) { Value = item.ProductName });
        cmd.Parameters.Add(new SqlParameter("@unitPrice", SqlDbType.Decimal) { Value = item.UnitPrice, Precision = 10, Scale = 2 });
        cmd.Parameters.Add(new SqlParameter("@quantity", SqlDbType.Int) { Value = item.Quantity });
        cmd.Parameters.Add(new SqlParameter("@totalPrice", SqlDbType.Decimal) { Value = item.TotalPrice, Precision = 10, Scale = 2 });
        cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// 更新订单支付状态（用于确认支付）
    /// </summary>
    /// <param name="orderID">订单编号</param>
    /// <param name="paymentStatus">新支付状态</param>
    /// <param name="paymentTime">支付时间</param>
    /// <returns>成功返回 true</returns>
    public bool UpdatePaymentStatus(int orderID, string paymentStatus, DateTime? paymentTime)
    {
        const string sql = @"UPDATE tbl_Order SET
            paymentStatus = @paymentStatus,
            paymentTime = @paymentTime
            WHERE orderID = @orderID";
        using SqlConnection conn = new(DBConnection.GetConnectionString());
        using SqlCommand cmd = new(sql, conn);
        cmd.Parameters.Add(new SqlParameter("@paymentStatus", SqlDbType.NVarChar, 10) { Value = paymentStatus });
        cmd.Parameters.Add(new SqlParameter("@paymentTime", SqlDbType.DateTime) { Value = (object)paymentTime ?? DBNull.Value });
        cmd.Parameters.Add(new SqlParameter("@orderID", SqlDbType.Int) { Value = orderID });
        conn.Open();
        return cmd.ExecuteNonQuery() > 0;
    }

    /// <summary>
    /// 查询指定订单的明细列表
    /// </summary>
    /// <param name="conn">已打开的连接</param>
    /// <param name="orderID">订单编号</param>
    /// <returns>明细列表</returns>
    private List<OrderItem> GetOrderItems(SqlConnection conn, int orderID)
    {
        const string sql = @"SELECT itemID, orderID, productID, productName, unitPrice, quantity, totalPrice
            FROM tbl_OrderItem WHERE orderID = @orderID ORDER BY itemID";
        List<OrderItem> items = new();
        using SqlCommand cmd = new(sql, conn);
        cmd.Parameters.Add(new SqlParameter("@orderID", SqlDbType.Int) { Value = orderID });
        using SqlDataReader reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            items.Add(new OrderItem
            {
                ItemID = Convert.ToInt32(reader["itemID"]),
                OrderID = Convert.ToInt32(reader["orderID"]),
                ProductID = reader["productID"].ToString(),
                ProductName = reader["productName"].ToString(),
                UnitPrice = Convert.ToDecimal(reader["unitPrice"]),
                Quantity = Convert.ToInt32(reader["quantity"]),
                TotalPrice = Convert.ToDecimal(reader["totalPrice"])
            });
        }
        return items;
    }

    /// <summary>
    /// 将 SqlDataReader 映射为 Order 实体
    /// </summary>
    private static Order MapReaderToOrder(SqlDataReader reader)
    {
        return new Order
        {
            OrderID = Convert.ToInt32(reader["orderID"]),
            OrderDate = Convert.ToDateTime(reader["orderDate"]),
            PaymentMethod = reader["paymentMethod"] == DBNull.Value ? null : reader["paymentMethod"].ToString(),
            PaymentTime = reader["paymentTime"] == DBNull.Value ? null : Convert.ToDateTime(reader["paymentTime"]),
            PaymentStatus = reader["paymentStatus"].ToString(),
            ReceiverName = reader["receiverName"] == DBNull.Value ? null : reader["receiverName"].ToString(),
            ReceiverPhone = reader["receiverPhone"] == DBNull.Value ? null : reader["receiverPhone"].ToString(),
            ReceiverAddress = reader["receiverAddress"] == DBNull.Value ? null : reader["receiverAddress"].ToString(),
            TotalAmount = Convert.ToDecimal(reader["totalAmount"])
        };
    }
}
