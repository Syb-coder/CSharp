using System.Data;
using CampusStore.Models;

namespace CampusStore.DAL;

/// <summary>
/// 商品类别数据访问类：调用 sp_Category_* 存储过程 + v_Category_List 视图
/// </summary>
public class CategoryRepository : BaseRepository
{
    /// <summary>
    /// 添加类别
    /// </summary>
    /// <returns>新类别ID</returns>
    public int Add(string categoryName, string categoryDesc, int sortOrder)
    {
        object result = ExecuteSpScalar("sp_Category_Add",
            MakeParam("@categoryName", categoryName),
            MakeParam("@categoryDesc", categoryDesc),
            MakeParam("@sortOrder", sortOrder));
        return Convert.ToInt32(result);
    }

    /// <summary>
    /// 修改类别
    /// </summary>
    public int Update(int categoryID, string categoryName, string categoryDesc, int sortOrder)
    {
        object result = ExecuteSpScalar("sp_Category_Update",
            MakeParam("@categoryID", categoryID),
            MakeParam("@categoryName", categoryName),
            MakeParam("@categoryDesc", categoryDesc),
            MakeParam("@sortOrder", sortOrder));
        return Convert.ToInt32(result);
    }

    /// <summary>
    /// 删除类别
    /// </summary>
    /// <returns>0=有关联商品无法删除，>0=删除成功</returns>
    public int Delete(int categoryID)
    {
        object result = ExecuteSpScalar("sp_Category_Delete", MakeParam("@categoryID", categoryID));
        return Convert.ToInt32(result);
    }

    /// <summary>
    /// 查询全部类别（按排序号升序）
    /// </summary>
    public DataTable GetAll()
    {
        // 视图已统计 productCount，无需在应用层 JOIN
        DataTable dt = ExecuteViewDataTable("v_Category_List");
        // 按 sortOrder 升序排序
        dt.DefaultView.Sort = "sortOrder ASC";
        return dt.DefaultView.ToTable();
    }

    /// <summary>
    /// 按名称关键字模糊检索
    /// </summary>
    public DataTable Search(string keyword)
    {
        DataTable dt = ExecuteViewDataTable("v_Category_List",
            "categoryName LIKE @keyword",
            MakeParam("@keyword", $"%{keyword}%"));
        dt.DefaultView.Sort = "sortOrder ASC";
        return dt.DefaultView.ToTable();
    }

    /// <summary>
    /// 查询全部类别（实体列表形式，供下拉框使用）
    /// </summary>
    public List<CategoryInfo> GetAllList()
    {
        DataTable dt = GetAll();
        return dt.AsEnumerable().Select(MapRow).ToList();
    }

    /// <summary>
    /// DataRow 到实体映射
    /// </summary>
    private static CategoryInfo MapRow(DataRow row) => new()
    {
        CategoryID = row.Field<int>("categoryID"),
        CategoryName = row.Field<string>("categoryName") ?? string.Empty,
        CategoryDesc = row.Field<string>("categoryDesc") ?? string.Empty,
        AddTime = row.Field<DateTime>("addTime"),
        SortOrder = row.Field<int>("sortOrder"),
        ProductCount = row.Field<int>("productCount")
    };
}
