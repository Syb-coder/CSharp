using System.Data;
using Microsoft.Data.SqlClient;
using LibraryManagement.Models;

namespace LibraryManagement.DAL;

/// <summary>
/// 读者数据访问类，对应 tbl_Reader 表的 CRUD 操作
/// </summary>
public class ReaderDAL
{
    /// <summary>
    /// 查询全部读者
    /// </summary>
    /// <returns>读者列表</returns>
    public List<Reader> GetAllReaders()
    {
        const string sql = "SELECT readerID, readerName, readerSex, phone, department, registerDate FROM tbl_Reader ORDER BY readerID";
        List<Reader> list = new();
        using SqlConnection conn = new(DBConnection.GetConnectionString());
        using SqlCommand cmd = new(sql, conn);
        conn.Open();
        using SqlDataReader reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(MapReaderToReader(reader));
        }
        return list;
    }

    /// <summary>
    /// 多条件查询读者
    /// </summary>
    /// <param name="readerID">读者编号（可空）</param>
    /// <param name="readerName">姓名关键字（可空）</param>
    /// <param name="department">院系关键字（可空）</param>
    /// <returns>读者列表</returns>
    public List<Reader> SearchReaders(string readerID, string readerName, string department)
    {
        const string sql = @"
            SELECT readerID, readerName, readerSex, phone, department, registerDate
            FROM tbl_Reader
            WHERE (@readerID IS NULL OR readerID = @readerID)
              AND (@readerName IS NULL OR readerName LIKE @readerName)
              AND (@department IS NULL OR department LIKE @department)
            ORDER BY readerID";
        List<Reader> list = new();
        using SqlConnection conn = new(DBConnection.GetConnectionString());
        using SqlCommand cmd = new(sql, conn);
        cmd.Parameters.Add(new SqlParameter("@readerID", SqlDbType.NVarChar, 20)
        {
            Value = string.IsNullOrWhiteSpace(readerID) ? DBNull.Value : readerID
        });
        cmd.Parameters.Add(new SqlParameter("@readerName", SqlDbType.NVarChar, 8)
        {
            Value = string.IsNullOrWhiteSpace(readerName) ? DBNull.Value : $"%{readerName}%"
        });
        cmd.Parameters.Add(new SqlParameter("@department", SqlDbType.NVarChar, 20)
        {
            Value = string.IsNullOrWhiteSpace(department) ? DBNull.Value : $"%{department}%"
        });
        conn.Open();
        using SqlDataReader reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(MapReaderToReader(reader));
        }
        return list;
    }

    /// <summary>
    /// 根据读者编号查询单个读者
    /// </summary>
    /// <param name="readerID">读者编号</param>
    /// <returns>读者实体，未找到返回 null</returns>
    public Reader GetReaderById(string readerID)
    {
        const string sql = "SELECT readerID, readerName, readerSex, phone, department, registerDate FROM tbl_Reader WHERE readerID = @readerID";
        using SqlConnection conn = new(DBConnection.GetConnectionString());
        using SqlCommand cmd = new(sql, conn);
        cmd.Parameters.Add(new SqlParameter("@readerID", SqlDbType.NVarChar, 20) { Value = readerID });
        conn.Open();
        using SqlDataReader reader = cmd.ExecuteReader();
        if (reader.Read())
        {
            return MapReaderToReader(reader);
        }
        return null;
    }

    /// <summary>
    /// 新增读者
    /// </summary>
    /// <param name="reader">读者实体</param>
    /// <returns>成功返回 true，编号已存在返回 false</returns>
    public bool InsertReader(Reader reader)
    {
        const string sql = @"
            INSERT INTO tbl_Reader (readerID, readerName, readerSex, phone, department, registerDate)
            VALUES (@readerID, @readerName, @readerSex, @phone, @department, @registerDate)";
        using SqlConnection conn = new(DBConnection.GetConnectionString());
        using SqlCommand cmd = new(sql, conn);
        AddReaderParameters(cmd, reader);
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
    /// 修改读者信息
    /// </summary>
    /// <param name="reader">读者实体</param>
    /// <returns>成功返回 true</returns>
    public bool UpdateReader(Reader reader)
    {
        const string sql = @"
            UPDATE tbl_Reader SET
                readerName = @readerName, readerSex = @readerSex, phone = @phone,
                department = @department, registerDate = @registerDate
            WHERE readerID = @readerID";
        using SqlConnection conn = new(DBConnection.GetConnectionString());
        using SqlCommand cmd = new(sql, conn);
        AddReaderParameters(cmd, reader);
        conn.Open();
        return cmd.ExecuteNonQuery() > 0;
    }

    /// <summary>
    /// 删除读者
    /// </summary>
    /// <param name="readerID">读者编号</param>
    /// <returns>成功返回 true</returns>
    public bool DeleteReader(string readerID)
    {
        const string sql = "DELETE FROM tbl_Reader WHERE readerID = @readerID";
        using SqlConnection conn = new(DBConnection.GetConnectionString());
        using SqlCommand cmd = new(sql, conn);
        cmd.Parameters.Add(new SqlParameter("@readerID", SqlDbType.NVarChar, 20) { Value = readerID });
        conn.Open();
        return cmd.ExecuteNonQuery() > 0;
    }

    /// <summary>
    /// 统计指定读者的未归还借阅数量
    /// </summary>
    /// <param name="readerID">读者编号</param>
    /// <returns>未归还数量</returns>
    public int CountUnreturnedBorrows(string readerID)
    {
        const string sql = "SELECT COUNT(1) FROM tbl_Borrow WHERE readerID = @readerID AND status = N'借出'";
        using SqlConnection conn = new(DBConnection.GetConnectionString());
        using SqlCommand cmd = new(sql, conn);
        cmd.Parameters.Add(new SqlParameter("@readerID", SqlDbType.NVarChar, 20) { Value = readerID });
        conn.Open();
        return (int)cmd.ExecuteScalar();
    }

    /// <summary>
    /// 添加读者参数到 SqlCommand
    /// </summary>
    private static void AddReaderParameters(SqlCommand cmd, Reader reader)
    {
        cmd.Parameters.Add(new SqlParameter("@readerID", SqlDbType.NVarChar, 20) { Value = reader.ReaderID });
        cmd.Parameters.Add(new SqlParameter("@readerName", SqlDbType.NVarChar, 8) { Value = reader.ReaderName });
        cmd.Parameters.Add(new SqlParameter("@readerSex", SqlDbType.NVarChar, 2) { Value = reader.ReaderSex });
        cmd.Parameters.Add(new SqlParameter("@phone", SqlDbType.NVarChar, 15)
        {
            Value = string.IsNullOrEmpty(reader.Phone) ? DBNull.Value : reader.Phone
        });
        cmd.Parameters.Add(new SqlParameter("@department", SqlDbType.NVarChar, 20)
        {
            Value = string.IsNullOrEmpty(reader.Department) ? DBNull.Value : reader.Department
        });
        cmd.Parameters.Add(new SqlParameter("@registerDate", SqlDbType.Date)
        {
            Value = reader.RegisterDate.HasValue ? reader.RegisterDate.Value : DBNull.Value
        });
    }

    /// <summary>
    /// 将 SqlDataReader 映射为 Reader 实体
    /// </summary>
    private static Reader MapReaderToReader(SqlDataReader reader)
    {
        return new Reader
        {
            ReaderID = reader["readerID"].ToString(),
            ReaderName = reader["readerName"].ToString(),
            ReaderSex = reader["readerSex"].ToString(),
            Phone = reader["phone"] == DBNull.Value ? null : reader["phone"].ToString(),
            Department = reader["department"] == DBNull.Value ? null : reader["department"].ToString(),
            RegisterDate = reader["registerDate"] == DBNull.Value ? null : (DateTime?)reader["registerDate"]
        };
    }
}
