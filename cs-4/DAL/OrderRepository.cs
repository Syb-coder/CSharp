using System.Data;
using CampusStore.Models;
using Microsoft.Data.SqlClient;

namespace CampusStore.DAL;

/// <summary>
/// 订单数据访问类：调用 sp_Order_Create / sp_Order_Confirm 存储过程
/// </summary>
/// <remarks>
/// 与 cs-2/cs-3 差异化：
///   - cs-2/cs-3 在 BLL 层显式管理 SqlTransaction
///   - cs-4 通过 sp_Order_Create 存储过程内部事务完成订单+明细一次性插入，
///     库存扣减由 tr_OrderItem_Insert 触发器自动完成，应用层无需管理事务
/// </remarks>
public class OrderRepository : BaseRepository
{
    /// <summary>
    /// 创建订单（主表 + 明细一次性插入，使用 Table-Valued Parameter 传递明细列表）
    /// </summary>
    /// <param name="order">订单信息（含明细列表）</param>
    /// <returns>新订单 ID</returns>
    /// <exception cref="InvalidOperationException">触发器库存检查失败时由数据库抛出</exception>
    public int CreateOrder(OrderInfo order)
    {
        // 构建 Table-Valued Parameter
        DataTable itemsTable = new();
        itemsTable.Columns.Add("productID", typeof(int));
        itemsTable.Columns.Add("productName", typeof(string));
        itemsTable.Columns.Add("unitPrice", typeof(decimal));
        itemsTable.Columns.Add("quantity", typeof(int));

        foreach (var item in order.Items)
        {
            itemsTable.Rows.Add(item.ProductID, item.ProductName, item.UnitPrice, item.Quantity);
        }

        // SqlParameter 指定 SqlDbType.Structured 类型
        SqlParameter itemsParam = new("@items", itemsTable)
        {
            SqlDbType = SqlDbType.Structured,
            TypeName = "dbo.OrderItemListType"
        };

        object result = ExecuteSpScalar("sp_Order_Create",
            MakeParam("@paymentMethod", order.PaymentMethod),
            MakeParam("@paymentStatus", order.PaymentStatus),
            MakeParam("@receiverName", order.ReceiverName),
            MakeParam("@receiverPhone", order.ReceiverPhone),
            MakeParam("@receiverAddress", order.ReceiverAddress),
            itemsParam);
        return Convert.ToInt32(result);
    }

    /// <summary>
    /// 确认结款
    /// </summary>
    /// <returns>0=订单不存在或已结款，>0=结款成功</returns>
    public int Confirm(int orderID)
    {
        object result = ExecuteSpScalar("sp_Order_Confirm", MakeParam("@orderID", orderID));
        return Convert.ToInt32(result);
    }

    /// <summary>
    /// 查询全部订单
    /// </summary>
    public DataTable GetAll()
    {
        return ExecuteViewDataTable("(SELECT * FROM tbl_Order) AS t");
    }

    /// <summary>
    /// 按收货人名称、结款状态组合条件检索
    /// </summary>
    public DataTable Search(string receiverName, string paymentStatus)
    {
        List<string> conditions = new();
        List<SqlParameter> parameters = new();

        if (!string.IsNullOrWhiteSpace(receiverName))
        {
            conditions.Add("receiverName LIKE @receiverName");
            parameters.Add(MakeParam("@receiverName", $"%{receiverName}%"));
        }
        if (!string.IsNullOrWhiteSpace(paymentStatus))
        {
            conditions.Add("paymentStatus = @paymentStatus");
            parameters.Add(MakeParam("@paymentStatus", paymentStatus));
        }

        string whereClause = conditions.Count > 0 ? string.Join(" AND ", conditions) : null;
        return ExecuteViewDataTable("(SELECT * FROM tbl_Order) AS t", whereClause, parameters.ToArray());
    }

    /// <summary>
    /// 按订单编号查询订单详情（含明细列表）
    /// </summary>
    public OrderInfo GetByID(int orderID)
    {
        DataTable orderDt = ExecuteViewDataTable("(SELECT * FROM tbl_Order) AS t",
            "orderID = @orderID", MakeParam("@orderID", orderID));
        if (orderDt.Rows.Count == 0)
            return null;

        DataRow row = orderDt.Rows[0];
        OrderInfo order = new()
        {
            OrderID = row.Field<int>("orderID"),
            OrderDate = row.Field<DateTime>("orderDate"),
            PaymentMethod = row.Field<string>("paymentMethod") ?? string.Empty,
            PaymentTime = row.Field<DateTime?>("paymentTime"),
            PaymentStatus = row.Field<string>("paymentStatus") ?? string.Empty,
            ReceiverName = row.Field<string>("receiverName") ?? string.Empty,
            ReceiverPhone = row.Field<string>("receiverPhone") ?? string.Empty,
            ReceiverAddress = row.Field<string>("receiverAddress") ?? string.Empty,
            TotalAmount = row.Field<decimal>("totalAmount")
        };

        // 加载订单明细
        DataTable itemDt = ExecuteViewDataTable("(SELECT * FROM tbl_OrderItem) AS t",
            "orderID = @orderID", MakeParam("@orderID", orderID));
        foreach (DataRow itemRow in itemDt.Rows)
        {
            order.Items.Add(new OrderItemInfo
            {
                ItemID = itemRow.Field<int>("itemID"),
                OrderID = itemRow.Field<int>("orderID"),
                ProductID = itemRow.Field<int>("productID"),
                ProductName = itemRow.Field<string>("productName") ?? string.Empty,
                UnitPrice = itemRow.Field<decimal>("unitPrice"),
                Quantity = itemRow.Field<int>("quantity"),
                Amount = itemRow.Field<decimal>("amount")
            });
        }
        return order;
    }

    /// <summary>
    /// 查询订单明细列表
    /// </summary>
    public List<OrderItemInfo> GetItems(int orderID)
    {
        DataTable dt = ExecuteViewDataTable("(SELECT * FROM tbl_OrderItem) AS t",
            "orderID = @orderID", MakeParam("@orderID", orderID));
        return dt.AsEnumerable().Select(row => new OrderItemInfo
        {
            ItemID = row.Field<int>("itemID"),
            OrderID = row.Field<int>("orderID"),
            ProductID = row.Field<int>("productID"),
            ProductName = row.Field<string>("productName") ?? string.Empty,
            UnitPrice = row.Field<decimal>("unitPrice"),
            Quantity = row.Field<int>("quantity"),
            Amount = row.Field<decimal>("amount")
        }).ToList();
    }
}
