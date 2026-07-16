using System.Data;
using CampusShop.Models;
using Microsoft.Data.SqlClient;

namespace CampusShop.DAL;

/// <summary>
/// 商品数据访问类，对应 tbl_Product 表的 CRUD 操作
/// </summary>
public class ProductDAL
{
    /// <summary>
    /// 查询全部商品（含类别名称和供货商名称）
    /// </summary>
    /// <returns>商品列表</returns>
    public List<Product> GetAllProducts()
    {
        const string sql = @"
            SELECT p.productID, p.productName, p.specification, p.unitPrice, p.stockQuantity,
                   p.categoryID, p.supplierID, p.origin, p.productionDate,
                   c.categoryName, s.supplierName
            FROM tbl_Product p
            INNER JOIN tbl_Category c ON p.categoryID = c.categoryID
            INNER JOIN tbl_Supplier s ON p.supplierID = s.supplierID
            ORDER BY p.productID";
        List<Product> list = new();
        using SqlConnection conn = new(DBConnection.GetConnectionString());
        using SqlCommand cmd = new(sql, conn);
        conn.Open();
        using SqlDataReader reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(MapReaderToProduct(reader));
        }
        return list;
    }

    /// <summary>
    /// 按多条件组合检索商品
    /// </summary>
    /// <param name="name">商品名称关键字（可为空）</param>
    /// <param name="categoryID">类别编号（可为空）</param>
    /// <param name="supplierID">供货商编号（可为空）</param>
    /// <returns>商品列表</returns>
    public List<Product> SearchProducts(string name, string categoryID, string supplierID)
    {
        List<string> conditions = new();
        if (!string.IsNullOrEmpty(name))
        {
            conditions.Add("p.productName LIKE '%' + @name + '%'");
        }
        if (!string.IsNullOrEmpty(categoryID))
        {
            conditions.Add("p.categoryID = @categoryID");
        }
        if (!string.IsNullOrEmpty(supplierID))
        {
            conditions.Add("p.supplierID = @supplierID");
        }

        string whereClause = conditions.Count > 0 ? "WHERE " + string.Join(" AND ", conditions) : "";
        string sql = $@"
            SELECT p.productID, p.productName, p.specification, p.unitPrice, p.stockQuantity,
                   p.categoryID, p.supplierID, p.origin, p.productionDate,
                   c.categoryName, s.supplierName
            FROM tbl_Product p
            INNER JOIN tbl_Category c ON p.categoryID = c.categoryID
            INNER JOIN tbl_Supplier s ON p.supplierID = s.supplierID
            {whereClause}
            ORDER BY p.productID";

        List<Product> list = new();
        using SqlConnection conn = new(DBConnection.GetConnectionString());
        using SqlCommand cmd = new(sql, conn);
        if (!string.IsNullOrEmpty(name))
        {
            cmd.Parameters.Add(new SqlParameter("@name", SqlDbType.NVarChar, 50) { Value = name });
        }
        if (!string.IsNullOrEmpty(categoryID))
        {
            cmd.Parameters.Add(new SqlParameter("@categoryID", SqlDbType.NVarChar, 10) { Value = categoryID });
        }
        if (!string.IsNullOrEmpty(supplierID))
        {
            cmd.Parameters.Add(new SqlParameter("@supplierID", SqlDbType.NVarChar, 20) { Value = supplierID });
        }
        conn.Open();
        using SqlDataReader reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(MapReaderToProduct(reader));
        }
        return list;
    }

    /// <summary>
    /// 根据商品编号查询
    /// </summary>
    /// <param name="productID">商品编号</param>
    /// <returns>商品实体，未找到返回 null</returns>
    public Product GetProductByID(string productID)
    {
        const string sql = @"
            SELECT productID, productName, specification, unitPrice, stockQuantity,
                   categoryID, supplierID, origin, productionDate
            FROM tbl_Product WHERE productID = @productID";
        using SqlConnection conn = new(DBConnection.GetConnectionString());
        using SqlCommand cmd = new(sql, conn);
        cmd.Parameters.Add(new SqlParameter("@productID", SqlDbType.NVarChar, 20) { Value = productID });
        conn.Open();
        using SqlDataReader reader = cmd.ExecuteReader();
        if (reader.Read())
        {
            return MapReaderToProduct(reader);
        }
        return null;
    }

    /// <summary>
    /// 新增商品
    /// </summary>
    /// <param name="product">商品实体</param>
    /// <returns>成功返回 true，编号已存在返回 false</returns>
    public bool InsertProduct(Product product)
    {
        const string sql = @"INSERT INTO tbl_Product
            (productID, productName, specification, unitPrice, stockQuantity, categoryID, supplierID, origin, productionDate)
            VALUES (@productID, @productName, @specification, @unitPrice, @stockQuantity, @categoryID, @supplierID, @origin, @productionDate)";
        using SqlConnection conn = new(DBConnection.GetConnectionString());
        using SqlCommand cmd = new(sql, conn);
        cmd.Parameters.Add(new SqlParameter("@productID", SqlDbType.NVarChar, 20) { Value = product.ProductID });
        cmd.Parameters.Add(new SqlParameter("@productName", SqlDbType.NVarChar, 50) { Value = product.ProductName });
        cmd.Parameters.Add(new SqlParameter("@specification", SqlDbType.NVarChar, 30) { Value = (object)product.Specification ?? DBNull.Value });
        cmd.Parameters.Add(new SqlParameter("@unitPrice", SqlDbType.Decimal) { Value = product.UnitPrice });
        cmd.Parameters.Add(new SqlParameter("@stockQuantity", SqlDbType.Int) { Value = product.StockQuantity });
        cmd.Parameters.Add(new SqlParameter("@categoryID", SqlDbType.NVarChar, 10) { Value = product.CategoryID });
        cmd.Parameters.Add(new SqlParameter("@supplierID", SqlDbType.NVarChar, 20) { Value = product.SupplierID });
        cmd.Parameters.Add(new SqlParameter("@origin", SqlDbType.NVarChar, 50) { Value = (object)product.Origin ?? DBNull.Value });
        cmd.Parameters.Add(new SqlParameter("@productionDate", SqlDbType.Date) { Value = (object)product.ProductionDate ?? DBNull.Value });
        conn.Open();
        try
        {
            return cmd.ExecuteNonQuery() > 0;
        }
        catch (SqlException ex) when (ex.Number == 2627)
        {
            return false;
        }
    }

    /// <summary>
    /// 修改商品信息（编号不可改）
    /// </summary>
    /// <param name="product">商品实体</param>
    /// <returns>成功返回 true</returns>
    public bool UpdateProduct(Product product)
    {
        const string sql = @"UPDATE tbl_Product SET
            productName = @productName, specification = @specification,
            unitPrice = @unitPrice, stockQuantity = @stockQuantity,
            categoryID = @categoryID, supplierID = @supplierID,
            origin = @origin, productionDate = @productionDate
            WHERE productID = @productID";
        using SqlConnection conn = new(DBConnection.GetConnectionString());
        using SqlCommand cmd = new(sql, conn);
        cmd.Parameters.Add(new SqlParameter("@productName", SqlDbType.NVarChar, 50) { Value = product.ProductName });
        cmd.Parameters.Add(new SqlParameter("@specification", SqlDbType.NVarChar, 30) { Value = (object)product.Specification ?? DBNull.Value });
        cmd.Parameters.Add(new SqlParameter("@unitPrice", SqlDbType.Decimal) { Value = product.UnitPrice });
        cmd.Parameters.Add(new SqlParameter("@stockQuantity", SqlDbType.Int) { Value = product.StockQuantity });
        cmd.Parameters.Add(new SqlParameter("@categoryID", SqlDbType.NVarChar, 10) { Value = product.CategoryID });
        cmd.Parameters.Add(new SqlParameter("@supplierID", SqlDbType.NVarChar, 20) { Value = product.SupplierID });
        cmd.Parameters.Add(new SqlParameter("@origin", SqlDbType.NVarChar, 50) { Value = (object)product.Origin ?? DBNull.Value });
        cmd.Parameters.Add(new SqlParameter("@productionDate", SqlDbType.Date) { Value = (object)product.ProductionDate ?? DBNull.Value });
        cmd.Parameters.Add(new SqlParameter("@productID", SqlDbType.NVarChar, 20) { Value = product.ProductID });
        conn.Open();
        return cmd.ExecuteNonQuery() > 0;
    }

    /// <summary>
    /// 删除商品
    /// </summary>
    /// <param name="productID">商品编号</param>
    /// <returns>成功返回 true</returns>
    public bool DeleteProduct(string productID)
    {
        const string sql = "DELETE FROM tbl_Product WHERE productID = @productID";
        using SqlConnection conn = new(DBConnection.GetConnectionString());
        using SqlCommand cmd = new(sql, conn);
        cmd.Parameters.Add(new SqlParameter("@productID", SqlDbType.NVarChar, 20) { Value = productID });
        conn.Open();
        return cmd.ExecuteNonQuery() > 0;
    }

    /// <summary>
    /// 检查指定商品是否有订单明细记录
    /// </summary>
    /// <param name="productID">商品编号</param>
    /// <returns>存在订单明细记录返回 true</returns>
    public bool HasOrderItems(string productID)
    {
        const string sql = "SELECT COUNT(1) FROM tbl_OrderItem WHERE productID = @productID";
        using SqlConnection conn = new(DBConnection.GetConnectionString());
        using SqlCommand cmd = new(sql, conn);
        cmd.Parameters.Add(new SqlParameter("@productID", SqlDbType.NVarChar, 20) { Value = productID });
        conn.Open();
        return (int)cmd.ExecuteScalar() > 0;
    }

    /// <summary>
    /// 扣减商品库存（供订单业务逻辑在同一事务中调用）
    /// </summary>
    /// <param name="transaction">外部事务对象</param>
    /// <param name="conn">已打开的连接</param>
    /// <param name="productID">商品编号</param>
    /// <param name="quantity">扣减数量（正数）</param>
    public void DeductStock(SqlTransaction transaction, SqlConnection conn, string productID, int quantity)
    {
        const string sql = "UPDATE tbl_Product SET stockQuantity = stockQuantity - @quantity WHERE productID = @productID AND stockQuantity >= @quantity";
        using SqlCommand cmd = new(sql, conn, transaction);
        cmd.Parameters.Add(new SqlParameter("@quantity", SqlDbType.Int) { Value = quantity });
        cmd.Parameters.Add(new SqlParameter("@productID", SqlDbType.NVarChar, 20) { Value = productID });
        int affected = cmd.ExecuteNonQuery();
        if (affected == 0)
        {
            throw new InvalidOperationException("库存不足，扣减失败");
        }
    }

    /// <summary>
    /// 将 SqlDataReader 映射为 Product 实体
    /// </summary>
    private static Product MapReaderToProduct(SqlDataReader reader)
    {
        return new Product
        {
            ProductID = reader["productID"].ToString(),
            ProductName = reader["productName"].ToString(),
            Specification = reader["specification"] == DBNull.Value ? null : reader["specification"].ToString(),
            UnitPrice = Convert.ToDecimal(reader["unitPrice"]),
            StockQuantity = Convert.ToInt32(reader["stockQuantity"]),
            CategoryID = reader["categoryID"].ToString(),
            SupplierID = reader["supplierID"].ToString(),
            Origin = HasColumn(reader, "origin") && reader["origin"] != DBNull.Value
                ? reader["origin"].ToString() : null,
            ProductionDate = HasColumn(reader, "productionDate") && reader["productionDate"] != DBNull.Value
                ? Convert.ToDateTime(reader["productionDate"]) : null,
            CategoryName = HasColumn(reader, "categoryName") && reader["categoryName"] != DBNull.Value
                ? reader["categoryName"].ToString() : null,
            SupplierName = HasColumn(reader, "supplierName") && reader["supplierName"] != DBNull.Value
                ? reader["supplierName"].ToString() : null
        };
    }

    /// <summary>
    /// 判断 SqlDataReader 当前结果集中是否存在指定列
    /// </summary>
    private static bool HasColumn(SqlDataReader reader, string columnName)
    {
        for (int i = 0; i < reader.FieldCount; i++)
        {
            if (reader.GetName(i) == columnName)
            {
                return true;
            }
        }
        return false;
    }
}
