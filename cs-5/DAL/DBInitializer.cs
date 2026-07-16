using System.Configuration;
using Microsoft.Data.SqlClient;

namespace LibrarySys.DAL;

/// <summary>
/// 数据库自动初始化器：程序启动时自动探测 SQL Server 实例、建库、建表、补默认用户
/// 解决部署到新电脑时因实例名不匹配、未执行 SQL 脚本导致登录失败的问题
/// </summary>
public static class DBInitializer
{
    private const string DbName = "LibraryDB";

    /// <summary>
    /// 默认账号密码（MD5 大写十六进制哈希，与 SecurityUtil.ComputeMd5Hash 结果一致）
    /// admin123 -> 0192023A7BBD73250516F069DF18B500
    /// user123  -> 6AD14BA9986E3615423DFCA256D04E3F
    /// </summary>
    private const string AdminPwdHash = "0192023A7BBD73250516F069DF18B500";
    private const string UserPwdHash = "6AD14BA9986E3615423DFCA256D04E3F";

    /// <summary>候选 SQL Server 实例地址，按优先级依次尝试连接</summary>
    private static readonly string[] _candidateServers = { "localhost", @"localhost\SQLEXPRESS", ".", @".\SQLEXPRESS", "(localdb)\\MSSQLLocalDB" };

    /// <summary>
    /// 执行数据库完整性检查与自动修复，可安全重复调用（幂等）
    /// </summary>
    /// <returns>初始化成功返回 true，所有候选实例均无法连接时返回 false</returns>
    public static bool EnsureDatabaseReady(out string errorMessage)
    {
        errorMessage = string.Empty;
        try
        {
            // 1. 自动探测可用 SQL Server 实例并构建连接字符串
            string connStr = ResolveConnectionString();
            if (connStr == null)
            {
                errorMessage = "无法连接到任何 SQL Server 实例。请确认已安装 SQL Server（Express 版即可）且服务已启动。";
                return false;
            }

            string masterConnStr = ChangeDatabase(connStr, "master");

            // 2. 确保 LibraryDB 数据库存在
            EnsureDatabaseExists(masterConnStr);

            // 3. 连接到 LibraryDB，建表 + 补默认用户
            string dbConnStr = ChangeDatabase(connStr, DbName);
            using (SqlConnection conn = new(dbConnStr))
            {
                conn.Open();
                EnsureTablesExist(conn);
                EnsureDefaultUsers(conn);
            }

            // 4. 将探测到的正确连接字符串写回 DBConnection，后续所有 DAO 自动使用正确实例
            DBConnection.SetConnectionString(dbConnStr);

            return true;
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
            return false;
        }
    }

    /// <summary>
    /// 依次尝试所有候选服务器，返回第一个能成功连接 master 库的连接字符串
    /// </summary>
    private static string ResolveConnectionString()
    {
        // 先尝试 App.config 中配置的地址
        string configured = ConfigurationManager.ConnectionStrings["LibraryDB"]?.ConnectionString;
        if (!string.IsNullOrEmpty(configured))
        {
            string normalized = NormalizeConnectionString(configured);
            if (TestConnection(normalized)) return normalized;
        }

        // 配置的地址连不上，依次尝试候选实例
        foreach (string server in _candidateServers)
        {
            string cs = BuildConnectionString(server, "master");
            if (TestConnection(cs))
            {
                return BuildConnectionString(server, DbName);
            }
        }
        return null;
    }

    /// <summary>
    /// 构建标准连接字符串（Windows 认证 + 信任服务器证书）
    /// </summary>
    private static string BuildConnectionString(string server, string db)
    {
        SqlConnectionStringBuilder b = new()
        {
            DataSource = server,
            InitialCatalog = db,
            IntegratedSecurity = true,
            TrustServerCertificate = true,
            ConnectTimeout = 3
        };
        return b.ConnectionString;
    }

    /// <summary>
    /// 将任意连接字符串标准化（设置连接超时、信任证书）以便测试
    /// </summary>
    private static string NormalizeConnectionString(string cs)
    {
        try
        {
            SqlConnectionStringBuilder b = new(cs)
            {
                TrustServerCertificate = true,
                ConnectTimeout = 3
            };
            return b.ConnectionString;
        }
        catch
        {
            return cs;
        }
    }

    /// <summary>
    /// 测试连接是否可用（3秒超时）
    /// </summary>
    private static bool TestConnection(string connStr)
    {
        try
        {
            using SqlConnection conn = new(connStr);
            conn.Open();
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 从连接字符串中替换 Initial Catalog，生成新的连接字符串
    /// </summary>
    private static string ChangeDatabase(string connStr, string newDb)
    {
        SqlConnectionStringBuilder b = new(connStr) { InitialCatalog = newDb };
        return b.ConnectionString;
    }

    /// <summary>
    /// 确保 LibraryDB 数据库存在，不存在则自动创建
    /// </summary>
    private static void EnsureDatabaseExists(string masterConnStr)
    {
        using SqlConnection conn = new(masterConnStr);
        conn.Open();
        using SqlCommand cmd = new($"IF DB_ID(N'{DbName}') IS NULL CREATE DATABASE [{DbName}];", conn);
        cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// 确保8张核心业务表存在，不存在则自动建表（幂等）
    /// </summary>
    private static void EnsureTablesExist(SqlConnection conn)
    {
        // T_User
        ExecuteIfTableMissing(conn, "T_User", @"
CREATE TABLE T_User (
    userName     NVARCHAR(16) NOT NULL CONSTRAINT PK_T_User PRIMARY KEY,
    userPassword NVARCHAR(32) NOT NULL,
    userPurview  NVARCHAR(8)  NOT NULL CONSTRAINT CK_T_User_purview CHECK (userPurview IN (N'管理员', N'普通用户'))
);");

        // T_BookType
        ExecuteIfTableMissing(conn, "T_BookType", @"
CREATE TABLE T_BookType (
    typeID   NVARCHAR(10) NOT NULL CONSTRAINT PK_T_BookType PRIMARY KEY,
    typeName NVARCHAR(20) NOT NULL
);");

        // T_Book
        ExecuteIfTableMissing(conn, "T_Book", @"
CREATE TABLE T_Book (
    bookID         NVARCHAR(20)  NOT NULL CONSTRAINT PK_T_Book PRIMARY KEY,
    bookName       NVARCHAR(100) NOT NULL,
    author         NVARCHAR(50)  NULL,
    publisher      NVARCHAR(50)  NULL,
    publishDate    DATE          NULL,
    ISBN           NVARCHAR(13)  NULL,
    price          DECIMAL(8, 2) NULL,
    typeID         NVARCHAR(10)  NOT NULL CONSTRAINT FK_T_Book_BookType FOREIGN KEY REFERENCES T_BookType(typeID),
    totalCount     INT           NOT NULL DEFAULT 0,
    availableCount INT           NOT NULL DEFAULT 0,
    coverImage     NVARCHAR(200) NULL,
    CONSTRAINT CK_T_Book_count CHECK (totalCount >= 0 AND availableCount >= 0 AND availableCount <= totalCount),
    CONSTRAINT CK_T_Book_price CHECK (price IS NULL OR price > 0)
);");

        // T_Reader
        ExecuteIfTableMissing(conn, "T_Reader", @"
CREATE TABLE T_Reader (
    readerID     NVARCHAR(20) NOT NULL CONSTRAINT PK_T_Reader PRIMARY KEY,
    readerName   NVARCHAR(8)  NOT NULL,
    readerSex    NVARCHAR(2)  NOT NULL CONSTRAINT CK_T_Reader_sex CHECK (readerSex IN (N'男', N'女')),
    phone        NVARCHAR(15) NULL,
    department   NVARCHAR(30) NULL,
    registerDate DATE         NOT NULL DEFAULT GETDATE()
);");

        // T_Borrow
        ExecuteIfTableMissing(conn, "T_Borrow", @"
CREATE TABLE T_Borrow (
    borrowID   INT          IDENTITY(1,1) CONSTRAINT PK_T_Borrow PRIMARY KEY,
    readerID   NVARCHAR(20) NOT NULL CONSTRAINT FK_T_Borrow_Reader FOREIGN KEY REFERENCES T_Reader(readerID),
    bookID     NVARCHAR(20) NOT NULL CONSTRAINT FK_T_Borrow_Book FOREIGN KEY REFERENCES T_Book(bookID),
    borrowDate DATE         NOT NULL,
    dueDate    DATE         NOT NULL,
    returnDate DATE         NULL,
    status     NVARCHAR(4)  NOT NULL CONSTRAINT CK_T_Borrow_status CHECK (status IN (N'借出', N'已还'))
);");

        // T_Fine
        ExecuteIfTableMissing(conn, "T_Fine", @"
CREATE TABLE T_Fine (
    fineID      INT           IDENTITY(1,1) CONSTRAINT PK_T_Fine PRIMARY KEY,
    readerID    NVARCHAR(20)  NOT NULL CONSTRAINT FK_T_Fine_Reader FOREIGN KEY REFERENCES T_Reader(readerID),
    bookID      NVARCHAR(20)  NOT NULL CONSTRAINT FK_T_Fine_Book FOREIGN KEY REFERENCES T_Book(bookID),
    borrowID    INT           NOT NULL CONSTRAINT FK_T_Fine_Borrow FOREIGN KEY REFERENCES T_Borrow(borrowID),
    overdueDays INT           NOT NULL,
    fineAmount  DECIMAL(8, 2) NOT NULL,
    fineStatus  NVARCHAR(4)   NOT NULL CONSTRAINT CK_T_Fine_status CHECK (fineStatus IN (N'未缴', N'已缴')),
    createDate  DATE          NOT NULL,
    payDate     DATE          NULL,
    CONSTRAINT CK_T_Fine_days CHECK (overdueDays > 0),
    CONSTRAINT CK_T_Fine_amount CHECK (fineAmount > 0),
    CONSTRAINT UQ_T_Fine_borrow UNIQUE (borrowID)
);");

        // T_Reservation
        ExecuteIfTableMissing(conn, "T_Reservation", @"
CREATE TABLE T_Reservation (
    reserveID    INT           IDENTITY(1,1) CONSTRAINT PK_T_Reservation PRIMARY KEY,
    readerID     NVARCHAR(20)  NOT NULL CONSTRAINT FK_T_Reservation_Reader FOREIGN KEY REFERENCES T_Reader(readerID),
    bookID       NVARCHAR(20)  NOT NULL CONSTRAINT FK_T_Reservation_Book FOREIGN KEY REFERENCES T_Book(bookID),
    reserveDate  DATE          NOT NULL DEFAULT GETDATE(),
    expireDate   DATE          NOT NULL,
    notifyDate   DATE          NULL,
    completeDate DATE          NULL,
    cancelDate   DATE          NULL,
    cancelReason NVARCHAR(50)  NULL,
    status       NVARCHAR(10)  NOT NULL CONSTRAINT CK_T_Reservation_status CHECK (status IN (N'排队中', N'待取书', N'已完成', N'已取消'))
);");
        // 预约活跃记录过滤唯一索引（单独执行，CREATE TABLE 内不支持过滤索引）
        try
        {
            using SqlCommand idxCmd = new(@"
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='UQ_T_Reservation_active' AND object_id=OBJECT_ID('T_Reservation'))
CREATE UNIQUE NONCLUSTERED INDEX UQ_T_Reservation_active ON T_Reservation(readerID, bookID) WHERE status IN (N'排队中', N'待取书');", conn);
            idxCmd.ExecuteNonQuery();
        }
        catch { /* 索引创建失败不影响核心登录功能 */ }

        // T_OperateLog
        ExecuteIfTableMissing(conn, "T_OperateLog", @"
CREATE TABLE T_OperateLog (
    logID          INT           IDENTITY(1,1) CONSTRAINT PK_T_OperateLog PRIMARY KEY,
    userName       NVARCHAR(16)  NOT NULL,
    operateTime    DATETIME      NOT NULL DEFAULT GETDATE(),
    operateType    NVARCHAR(20)  NOT NULL,
    operateContent NVARCHAR(100) NOT NULL,
    detail         NVARCHAR(200) NULL
);");
    }

    /// <summary>
    /// 若指定表不存在则执行建表 SQL（幂等）
    /// </summary>
    private static void ExecuteIfTableMissing(SqlConnection conn, string tableName, string createSql)
    {
        using SqlCommand checkCmd = new(
            "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME=@tableName", conn);
        checkCmd.Parameters.AddWithValue("@tableName", tableName);
        int exists = Convert.ToInt32(checkCmd.ExecuteScalar());
        if (exists == 0)
        {
            using SqlCommand createCmd = new(createSql, conn);
            createCmd.ExecuteNonQuery();
        }
    }

    /// <summary>
    /// 确保默认管理员和普通用户账号存在，不存在则自动插入（密码 MD5 哈希与 SecurityUtil 一致）
    /// </summary>
    private static void EnsureDefaultUsers(SqlConnection conn)
    {
        InsertUserIfMissing(conn, "admin", AdminPwdHash, "管理员");
        InsertUserIfMissing(conn, "user01", UserPwdHash, "普通用户");
    }

    private static void InsertUserIfMissing(SqlConnection conn, string name, string pwdHash, string purview)
    {
        using SqlCommand cmd = new(
            "IF NOT EXISTS (SELECT 1 FROM T_User WHERE userName=@name) " +
            "INSERT INTO T_User (userName, userPassword, userPurview) VALUES (@name, @pwd, @purview)", conn);
        cmd.Parameters.AddWithValue("@name", name);
        cmd.Parameters.AddWithValue("@pwd", pwdHash);
        cmd.Parameters.AddWithValue("@purview", purview);
        cmd.ExecuteNonQuery();
    }
}
