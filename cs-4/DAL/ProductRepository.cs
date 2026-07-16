using System.Data;
using CampusStore.Models;

namespace CampusStore.DAL;

/// <summary>
/// 商品数据访问类：调用 sp_Product_* 存储过程 + v_Product_Detail 视图
/// </summary>
public class ProductRepository : BaseRepository
{
    /// <summary>
    /// 添加商品
    /// </summary>
    public int Add(ProductInfo info)
    {
        object result = ExecuteSpScalar("sp_Product_Add",
            MakeParam("@productName", info.ProductName),
            MakeParam("@categoryID", info.CategoryID),
            MakeParam("@unitPrice", info.UnitPrice),
            MakeParam("@origin", info.Origin),
            MakeParam("@produceDate", info.ProduceDate),
            MakeParam("@stockQty", info.StockQuantity),
            MakeParam("@supplierID", info.SupplierID));
        return Convert.ToInt32(result);
    }

    /// <summary>
    /// 修改商品
    /// </summary>
    public int Update(ProductInfo info)
    {
        object result = ExecuteSpScalar("sp_Product_Update",
            MakeParam("@productID", info.ProductID),
            MakeParam("@productName", info.ProductName),
            MakeParam("@categoryID", info.CategoryID),
            MakeParam("@unitPrice", info.UnitPrice),
            MakeParam("@origin", info.Origin),
            MakeParam("@produceDate", info.ProduceDate),
            MakeParam("@stockQty", info.StockQuantity),
            MakeParam("@supplierID", info.SupplierID));
        return Convert.ToInt32(result);
    }

    /// <summary>
    /// 删除商品
    /// </summary>
    /// <returns>0=有订单明细无法删除，>0=删除成功</returns>
    public int Delete(int productID)
    {
        object result = ExecuteSpScalar("sp_Product_Delete", MakeParam("@productID", productID));
        return Convert.ToInt32(result);
    }

    /// <summary>
    /// 查询全部商品（通过视图 v_Product_Detail，自动 JOIN 类别名称和供货商名称）
    /// </summary>
    public DataTable GetAll()
    {
        return ExecuteViewDataTable("v_Product_Detail");
    }

    /// <summary>
    /// 按商品名称、类别、供货商组合条件检索
    /// </summary>
    public DataTable Search(string productName, int? categoryID, int? supplierID)
    {
        // 动态拼接 WHERE 子句（参数化，防 SQL 注入）
        List<string> conditions = new();
        List<Microsoft.Data.SqlClient.SqlParameter> parameters = new();

        if (!string.IsNullOrWhiteSpace(productName))
        {
            conditions.Add("productName LIKE @productName");
            parameters.Add(MakeParam("@productName", $"%{productName}%"));
        }
        if (categoryID.HasValue)
        {
            conditions.Add("categoryID = @categoryID");
            parameters.Add(MakeParam("@categoryID", categoryID.Value));
        }
        if (supplierID.HasValue)
        {
            conditions.Add("supplierID = @supplierID");
            parameters.Add(MakeParam("@supplierID", supplierID.Value));
        }

        string whereClause = conditions.Count > 0 ? string.Join(" AND ", conditions) : null;
        return ExecuteViewDataTable("v_Product_Detail", whereClause, parameters.ToArray());
    }

    /// <summary>
    /// 按商品编号查询
    /// </summary>
    public ProductInfo GetByID(int productID)
    {
        DataTable dt = ExecuteViewDataTable("v_Product_Detail",
            "productID = @productID",
            MakeParam("@productID", productID));
        return dt.AsEnumerable().Select(MapRow).FirstOrDefault();
    }

    /// <summary>
    /// 查询全部商品（实体列表形式，供下拉框使用）
    /// </summary>
    public List<ProductInfo> GetAllList()
    {
        DataTable dt = GetAll();
        return dt.AsEnumerable().Select(MapRow).ToList();
    }

    /// <summary>
    /// DataRow 到实体映射
    /// </summary>
    private static ProductInfo MapRow(DataRow row) => new()
    {
        ProductID = row.Field<int>("productID"),
        DisplayNo = row.Field<string>("displayNo") ?? string.Empty,
        ProductName = row.Field<string>("productName") ?? string.Empty,
        CategoryID = row.Field<int>("categoryID"),
        CategoryName = row.Field<string>("categoryName") ?? string.Empty,
        UnitPrice = row.Field<decimal>("unitPrice"),
        Origin = row.Field<string>("origin") ?? string.Empty,
        ProduceDate = row.Field<DateTime?>("produceDate"),
        StockQuantity = row.Field<int>("stockQuantity"),
        SupplierID = row.Field<int>("supplierID"),
        SupplierName = row.Field<string>("supplierName") ?? string.Empty
    };
}
