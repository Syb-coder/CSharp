using System.Data;
using CampusMart.Models;
using Microsoft.Data.SqlClient;

namespace CampusMart.DAL;

/// <summary>
/// 商品数据访问类，对应 tbl_Product 表的 CRUD 操作
/// </summary>
/// <remarks>
/// 与 cs-2 的 ProductDAL 差异化命名（GoodsDao）。无 specification 字段，字段名 stockQty / produceDate。
/// </remarks>
public class GoodsDao : BaseDao
{
    /// <summary>
    /// 查询全部商品（联表获取类别名称和供货商名称）
    /// </summary>
    /// <returns>商品列表（含类别名、供货商名）</returns>
    public List<GoodsInfo> GetAllGoods()
    {
        const string sql = @"SELECT p.productID, p.productName, p.categoryID, p.unitPrice, p.origin,
                                   p.produceDate, p.stockQty, p.supplierID,
                                   c.categoryName, s.supplierName
                            FROM tbl_Product p
                            LEFT JOIN tbl_Category c ON p.categoryID = c.categoryID
                            LEFT JOIN tbl_Supplier s ON p.supplierID = s.supplierID
                            ORDER BY p.productID";
        DataTable table = ExecuteDataTable(sql);
        List<GoodsInfo> list = new(table.Rows.Count);
        foreach (DataRow row in table.Rows)
        {
            list.Add(MapRowToGoods(row));
        }
        return list;
    }

    /// <summary>
    /// 按多条件组合检索商品（所有条件均可为空，空表示不限制）
    /// </summary>
    /// <param name="productName">商品名称关键字（模糊匹配，空则不限）</param>
    /// <param name="categoryID">类别编号（0 表示不限）</param>
    /// <param name="supplierID">供货商编号（0 表示不限）</param>
    /// <returns>匹配的商品列表</returns>
    public List<GoodsInfo> SearchGoods(string productName, int categoryID, int supplierID)
    {
        // 动态拼接 WHERE 子句：仅对非空条件加入过滤，避免 NULL 干扰
        // 虽然使用拼接，但所有值均通过 SqlParameter 传递，从根本上防止 SQL 注入
        System.Text.StringBuilder sb = new();
        sb.Append(@"SELECT p.productID, p.productName, p.categoryID, p.unitPrice, p.origin,
                          p.produceDate, p.stockQty, p.supplierID,
                          c.categoryName, s.supplierName
                   FROM tbl_Product p
                   LEFT JOIN tbl_Category c ON p.categoryID = c.categoryID
                   LEFT JOIN tbl_Supplier s ON p.supplierID = s.supplierID
                   WHERE 1=1");
        List<SqlParameter> ps = new();
        if (!string.IsNullOrWhiteSpace(productName))
        {
            sb.Append(" AND p.productName LIKE @productName");
            ps.Add(new SqlParameter("@productName", SqlDbType.NVarChar, 50) { Value = $"%{productName}%" });
        }
        if (categoryID > 0)
        {
            sb.Append(" AND p.categoryID = @categoryID");
            ps.Add(new SqlParameter("@categoryID", SqlDbType.Int) { Value = categoryID });
        }
        if (supplierID > 0)
        {
            sb.Append(" AND p.supplierID = @supplierID");
            ps.Add(new SqlParameter("@supplierID", SqlDbType.Int) { Value = supplierID });
        }
        sb.Append(" ORDER BY p.productID");

        DataTable table = ExecuteDataTable(sb.ToString(), ps.ToArray());
        List<GoodsInfo> list = new(table.Rows.Count);
        foreach (DataRow row in table.Rows)
        {
            list.Add(MapRowToGoods(row));
        }
        return list;
    }

    /// <summary>
    /// 按商品编号查询
    /// </summary>
    /// <param name="productID">商品编号</param>
    /// <returns>商品实体，未找到返回 null</returns>
    public GoodsInfo GetGoodsByID(int productID)
    {
        const string sql = @"SELECT p.productID, p.productName, p.categoryID, p.unitPrice, p.origin,
                                    p.produceDate, p.stockQty, p.supplierID,
                                    c.categoryName, s.supplierName
                             FROM tbl_Product p
                             LEFT JOIN tbl_Category c ON p.categoryID = c.categoryID
                             LEFT JOIN tbl_Supplier s ON p.supplierID = s.supplierID
                             WHERE p.productID = @productID";
        SqlParameter p = new("@productID", SqlDbType.Int) { Value = productID };
        DataTable table = ExecuteDataTable(sql, p);
        if (table.Rows.Count == 0) return null;
        return MapRowToGoods(table.Rows[0]);
    }

    /// <summary>
    /// 新增商品
    /// </summary>
    /// <param name="goods">商品实体</param>
    /// <returns>成功返回 true</returns>
    public bool InsertGoods(GoodsInfo goods)
    {
        const string sql = @"INSERT INTO tbl_Product (productName, categoryID, unitPrice, origin, produceDate, stockQty, supplierID)
                             VALUES (@productName, @categoryID, @unitPrice, @origin, @produceDate, @stockQty, @supplierID)";
        SqlParameter[] ps =
        {
            new("@productName", SqlDbType.NVarChar, 50) { Value = goods.ProductName },
            new("@categoryID", SqlDbType.Int) { Value = goods.CategoryID },
            new("@unitPrice", SqlDbType.Decimal) { Value = goods.UnitPrice },
            new("@origin", SqlDbType.NVarChar, 50) { Value = (object)goods.Origin ?? DBNull.Value },
            new("@produceDate", SqlDbType.Date) { Value = (object)goods.ProduceDate ?? DBNull.Value },
            new("@stockQty", SqlDbType.Int) { Value = goods.StockQty },
            new("@supplierID", SqlDbType.Int) { Value = goods.SupplierID }
        };
        return ExecuteNonQuery(sql, ps) > 0;
    }

    /// <summary>
    /// 修改商品信息
    /// </summary>
    /// <param name="goods">商品实体</param>
    /// <returns>成功返回 true</returns>
    public bool UpdateGoods(GoodsInfo goods)
    {
        const string sql = @"UPDATE tbl_Product
                             SET productName = @productName, categoryID = @categoryID, unitPrice = @unitPrice,
                                 origin = @origin, produceDate = @produceDate, stockQty = @stockQty, supplierID = @supplierID
                             WHERE productID = @productID";
        SqlParameter[] ps =
        {
            new("@productName", SqlDbType.NVarChar, 50) { Value = goods.ProductName },
            new("@categoryID", SqlDbType.Int) { Value = goods.CategoryID },
            new("@unitPrice", SqlDbType.Decimal) { Value = goods.UnitPrice },
            new("@origin", SqlDbType.NVarChar, 50) { Value = (object)goods.Origin ?? DBNull.Value },
            new("@produceDate", SqlDbType.Date) { Value = (object)goods.ProduceDate ?? DBNull.Value },
            new("@stockQty", SqlDbType.Int) { Value = goods.StockQty },
            new("@supplierID", SqlDbType.Int) { Value = goods.SupplierID },
            new("@productID", SqlDbType.Int) { Value = goods.ProductID }
        };
        return ExecuteNonQuery(sql, ps) > 0;
    }

    /// <summary>
    /// 检查指定商品是否存在订单明细记录
    /// </summary>
    /// <param name="productID">商品编号</param>
    /// <returns>存在订单记录返回 true</returns>
    public bool HasOrderItems(int productID)
    {
        const string sql = "SELECT COUNT(1) FROM tbl_OrderItem WHERE productID = @productID";
        SqlParameter p = new("@productID", SqlDbType.Int) { Value = productID };
        object result = ExecuteScalar(sql, p);
        return Convert.ToInt32(result) > 0;
    }

    /// <summary>
    /// 删除商品
    /// </summary>
    /// <param name="productID">商品编号</param>
    /// <returns>成功返回 true</returns>
    public bool DeleteGoods(int productID)
    {
        const string sql = "DELETE FROM tbl_Product WHERE productID = @productID";
        SqlParameter p = new("@productID", SqlDbType.Int) { Value = productID };
        return ExecuteNonQuery(sql, p) > 0;
    }

    /// <summary>
    /// 将 DataRow 映射为 GoodsInfo 实体
    /// </summary>
    private static GoodsInfo MapRowToGoods(DataRow row)
    {
        return new GoodsInfo
        {
            ProductID = Convert.ToInt32(row["productID"]),
            ProductName = row["productName"].ToString(),
            CategoryID = Convert.ToInt32(row["categoryID"]),
            UnitPrice = Convert.ToDecimal(row["unitPrice"]),
            Origin = row["origin"] == DBNull.Value ? null : row["origin"].ToString(),
            ProduceDate = row["produceDate"] == DBNull.Value ? null : Convert.ToDateTime(row["produceDate"]),
            StockQty = Convert.ToInt32(row["stockQty"]),
            SupplierID = Convert.ToInt32(row["supplierID"]),
            CategoryName = row["categoryName"] == DBNull.Value ? null : row["categoryName"].ToString(),
            SupplierName = row["supplierName"] == DBNull.Value ? null : row["supplierName"].ToString()
        };
    }
}
