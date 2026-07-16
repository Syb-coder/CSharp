using Microsoft.Data.SqlClient;
using LibrarySys.Models;

namespace LibrarySys.DAL;

/// <summary>
/// 图书类型数据访问类，继承泛型基类 BaseRepository&lt;BookTypeInfo&gt;
/// </summary>
public class BookTypeDao : BaseRepository<BookTypeInfo>
{
    /// <summary>查询全部图书类型</summary>
    public override List<BookTypeInfo> FindAll()
    {
        const string sql = "SELECT typeID, typeName FROM T_BookType ORDER BY typeID";
        List<BookTypeInfo> list = new();
        using SqlConnection conn = new(DBConnection.GetConnectionString());
        conn.Open();
        using SqlCommand cmd = new(sql, conn);
        using SqlDataReader reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(MapReader(reader));
        }
        return list;
    }

    /// <summary>按类型编号查询单条记录</summary>
    public override BookTypeInfo FindById(string id)
    {
        const string sql = "SELECT typeID, typeName FROM T_BookType WHERE typeID = @typeID";
        using SqlConnection conn = new(DBConnection.GetConnectionString());
        conn.Open();
        using SqlCommand cmd = new(sql, conn);
        cmd.Parameters.AddWithValue("@typeID", id);
        using SqlDataReader reader = cmd.ExecuteReader();
        return reader.Read() ? MapReader(reader) : null;
    }

    /// <summary>新增图书类型</summary>
    public override int Insert(BookTypeInfo entity)
    {
        const string sql = "INSERT INTO T_BookType (typeID, typeName) VALUES (@typeID, @typeName)";
        using SqlConnection conn = new(DBConnection.GetConnectionString());
        conn.Open();
        using SqlCommand cmd = new(sql, conn);
        cmd.Parameters.AddWithValue("@typeID", entity.TypeID);
        cmd.Parameters.AddWithValue("@typeName", entity.TypeName);
        return cmd.ExecuteNonQuery();
    }

    /// <summary>修改图书类型名称（编号为主键不允许修改）</summary>
    public override int Update(BookTypeInfo entity)
    {
        const string sql = "UPDATE T_BookType SET typeName = @typeName WHERE typeID = @typeID";
        using SqlConnection conn = new(DBConnection.GetConnectionString());
        conn.Open();
        using SqlCommand cmd = new(sql, conn);
        cmd.Parameters.AddWithValue("@typeName", entity.TypeName);
        cmd.Parameters.AddWithValue("@typeID", entity.TypeID);
        return cmd.ExecuteNonQuery();
    }

    /// <summary>按类型编号删除</summary>
    public override int Delete(string id)
    {
        const string sql = "DELETE FROM T_BookType WHERE typeID = @typeID";
        using SqlConnection conn = new(DBConnection.GetConnectionString());
        conn.Open();
        using SqlCommand cmd = new(sql, conn);
        cmd.Parameters.AddWithValue("@typeID", id);
        return cmd.ExecuteNonQuery();
    }

    // ===== 以下为 BookType 特有查询，不在基类中定义 =====

    /// <summary>检查该类型下是否有关联图书（删除前校验）</summary>
    /// <param name="typeID">类型编号</param>
    /// <returns>有关联图书返回 true，否则 false</returns>
    public bool HasBooks(string typeID)
    {
        const string sql = "SELECT COUNT(1) FROM T_Book WHERE typeID = @typeID";
        using SqlConnection conn = new(DBConnection.GetConnectionString());
        conn.Open();
        using SqlCommand cmd = new(sql, conn);
        cmd.Parameters.AddWithValue("@typeID", typeID);
        return (int)cmd.ExecuteScalar() > 0;
    }

    /// <summary>按类型名称关键字模糊查询</summary>
    /// <param name="keyword">类型名称关键字，传 null 或空字符串查询全部</param>
    /// <returns>匹配的类型列表</returns>
    public List<BookTypeInfo> SearchByName(string keyword)
    {
        // 关键字为空时查询全部，避免 LIKE '%%' 的低效执行计划
        if (string.IsNullOrWhiteSpace(keyword))
            return FindAll();

        const string sql = "SELECT typeID, typeName FROM T_BookType WHERE typeName LIKE '%' + @keyword + '%' ORDER BY typeID";
        List<BookTypeInfo> list = new();
        using SqlConnection conn = new(DBConnection.GetConnectionString());
        conn.Open();
        using SqlCommand cmd = new(sql, conn);
        cmd.Parameters.AddWithValue("@keyword", keyword);
        using SqlDataReader reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(MapReader(reader));
        }
        return list;
    }

    /// <summary>SqlDataReader 映射为 BookTypeInfo 实体</summary>
    private static BookTypeInfo MapReader(SqlDataReader reader)
        => new()
        {
            TypeID = reader.GetString(0),
            TypeName = reader.GetString(1)
        };
}
