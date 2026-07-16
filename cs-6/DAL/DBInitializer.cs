using System.Configuration;
using Microsoft.Data.SqlClient;
using HotelSys.Common;

namespace HotelSys.DAL;

/// <summary>
/// 数据库自动初始化器：程序启动时自动探测 SQL Server 实例、建库、建表、补默认数据
/// 解决部署到新电脑时因实例名不匹配、未执行 SQL 脚本导致登录失败的问题（沿用 cs-5 零配置部署方案）
/// cs-6 使用 SHA-256 密码哈希、9 张业务表、6 种预置房型
/// </summary>
public static class DBInitializer
{
    private const string DbName = "HotelDB";

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

            // 2. 确保 HotelDB 数据库存在
            EnsureDatabaseExists(masterConnStr);

            // 3. 连接到 HotelDB，建表 + 补默认数据
            string dbConnStr = ChangeDatabase(connStr, DbName);
            using (SqlConnection conn = new(dbConnStr))
            {
                conn.Open();
                EnsureTablesExist(conn);
                EnsureDefaultUsers(conn);
                EnsureDefaultRoomTypes(conn);
                EnsureDefaultRooms(conn);
                EnsureDefaultCustomers(conn);
                EnsureDefaultReservations(conn);
                EnsureDefaultCheckIns(conn);
                EnsureDefaultCheckedOutRecords(conn);
                EnsureDefaultConsumes(conn);
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
        string configured = ConfigurationManager.ConnectionStrings["HotelDB"]?.ConnectionString;
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
    /// 确保 HotelDB 数据库存在，不存在则自动创建
    /// </summary>
    private static void EnsureDatabaseExists(string masterConnStr)
    {
        using SqlConnection conn = new(masterConnStr);
        conn.Open();
        using SqlCommand cmd = new($"IF DB_ID(N'{DbName}') IS NULL CREATE DATABASE [{DbName}];", conn);
        cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// 确保9张核心业务表存在，不存在则自动建表（幂等）
    /// 建表顺序按外键依赖：先无依赖表，再有依赖表
    /// </summary>
    private static void EnsureTablesExist(SqlConnection conn)
    {
        // T_User（系统用户表，PRD 5.1.1）
        ExecuteIfTableMissing(conn, "T_User", @"
CREATE TABLE T_User (
    userName     NVARCHAR(16) NOT NULL CONSTRAINT PK_T_User PRIMARY KEY,
    userPassword NVARCHAR(64) NOT NULL,
    userPurview  NVARCHAR(8)  NOT NULL CONSTRAINT CK_T_User_purview CHECK (userPurview IN (N'管理员', N'前台')),
    realName     NVARCHAR(20) NULL
);");

        // T_RoomType（客房类型表，PRD 5.1.2）
        ExecuteIfTableMissing(conn, "T_RoomType", @"
CREATE TABLE T_RoomType (
    typeID      NVARCHAR(10)  NOT NULL CONSTRAINT PK_T_RoomType PRIMARY KEY,
    typeName    NVARCHAR(20)  NOT NULL CONSTRAINT UQ_T_RoomType_name UNIQUE,
    price       DECIMAL(8, 2) NOT NULL,
    bedCount    INT           NOT NULL,
    description NVARCHAR(200) NULL,
    CONSTRAINT CK_T_RoomType_price CHECK (price > 0),
    CONSTRAINT CK_T_RoomType_bed CHECK (bedCount > 0)
);");

        // T_Room（客房信息表，PRD 5.1.3）
        ExecuteIfTableMissing(conn, "T_Room", @"
CREATE TABLE T_Room (
    roomNo     NVARCHAR(10) NOT NULL CONSTRAINT PK_T_Room PRIMARY KEY,
    typeID     NVARCHAR(10) NOT NULL CONSTRAINT FK_T_Room_RoomType FOREIGN KEY REFERENCES T_RoomType(typeID),
    floor      INT          NOT NULL,
    bedCount   INT          NOT NULL,
    roomStatus NVARCHAR(6)  NOT NULL CONSTRAINT CK_T_Room_status CHECK (roomStatus IN (N'空闲', N'在住', N'预留', N'维护')),
    remark     NVARCHAR(200) NULL,
    CONSTRAINT CK_T_Room_floor CHECK (floor > 0),
    CONSTRAINT CK_T_Room_bed CHECK (bedCount > 0)
);");

        // T_Customer（客户信息表，PRD 5.1.4）
        ExecuteIfTableMissing(conn, "T_Customer", @"
CREATE TABLE T_Customer (
    customerID   INT           IDENTITY(1,1) CONSTRAINT PK_T_Customer PRIMARY KEY,
    customerName NVARCHAR(20)  NOT NULL,
    gender       NVARCHAR(2)   NULL CONSTRAINT CK_T_Customer_gender CHECK (gender IN (N'男', N'女')),
    idType       NVARCHAR(10)  NOT NULL,
    idNumber     NVARCHAR(30)  NOT NULL,
    phone        NVARCHAR(15)  NULL,
    address      NVARCHAR(200) NULL,
    createTime   DATETIME      NOT NULL CONSTRAINT DF_T_Customer_time DEFAULT GETDATE()
);");

        // T_Reservation（预订记录表，PRD 5.1.5）
        ExecuteIfTableMissing(conn, "T_Reservation", @"
CREATE TABLE T_Reservation (
    reserveID    INT          IDENTITY(1,1) CONSTRAINT PK_T_Reservation PRIMARY KEY,
    customerID   INT          NOT NULL CONSTRAINT FK_T_Reservation_Customer FOREIGN KEY REFERENCES T_Customer(customerID),
    roomNo       NVARCHAR(10) NOT NULL CONSTRAINT FK_T_Reservation_Room FOREIGN KEY REFERENCES T_Room(roomNo),
    expectCheckIn DATE        NOT NULL,
    expectDays   INT          NOT NULL,
    contactPhone NVARCHAR(15) NOT NULL,
    reserveTime  DATETIME     NOT NULL CONSTRAINT DF_T_Reservation_time DEFAULT GETDATE(),
    status       NVARCHAR(8)  NOT NULL CONSTRAINT DF_T_Reservation_status DEFAULT N'待入住'
                  CONSTRAINT CK_T_Reservation_status CHECK (status IN (N'待入住', N'已入住', N'已取消', N'已过期')),
    remark       NVARCHAR(200) NULL,
    CONSTRAINT CK_T_Reservation_days CHECK (expectDays > 0)
);");

        // T_CheckIn（入住记录表，PRD 5.1.6，核心业务表）
        ExecuteIfTableMissing(conn, "T_CheckIn", @"
CREATE TABLE T_CheckIn (
    checkInID     INT           IDENTITY(1,1) CONSTRAINT PK_T_CheckIn PRIMARY KEY,
    customerID    INT           NOT NULL CONSTRAINT FK_T_CheckIn_Customer FOREIGN KEY REFERENCES T_Customer(customerID),
    roomNo        NVARCHAR(10)  NOT NULL CONSTRAINT FK_T_CheckIn_Room FOREIGN KEY REFERENCES T_Room(roomNo),
    checkInTime   DATETIME      NOT NULL CONSTRAINT DF_T_CheckIn_inTime DEFAULT GETDATE(),
    expectCheckOut DATETIME     NOT NULL,
    checkOutTime  DATETIME      NULL,
    actualDays    INT           NULL,
    deposit       DECIMAL(8, 2) NOT NULL,
    roomCharge    DECIMAL(8, 2) NULL,
    otherCharge   DECIMAL(8, 2) NULL CONSTRAINT DF_T_CheckIn_other DEFAULT 0,
    consumeAmount DECIMAL(8, 2) NULL CONSTRAINT DF_T_CheckIn_consume DEFAULT 0,
    totalAmount   DECIMAL(10, 2) NULL,
    status        NVARCHAR(6)   NOT NULL CONSTRAINT DF_T_CheckIn_status DEFAULT N'在住'
                   CONSTRAINT CK_T_CheckIn_status CHECK (status IN (N'在住', N'已结账', N'已取消')),
    remark        NVARCHAR(200) NULL,
    CONSTRAINT CK_T_CheckIn_deposit CHECK (deposit >= 0)
);");

        // T_Consume（消费记录表，PRD 5.1.7）
        ExecuteIfTableMissing(conn, "T_Consume", @"
CREATE TABLE T_Consume (
    consumeID   INT           IDENTITY(1,1) CONSTRAINT PK_T_Consume PRIMARY KEY,
    checkInID   INT           NOT NULL CONSTRAINT FK_T_Consume_CheckIn FOREIGN KEY REFERENCES T_CheckIn(checkInID),
    itemName    NVARCHAR(50)  NOT NULL,
    amount      DECIMAL(8, 2) NOT NULL,
    consumeTime DATETIME      NOT NULL CONSTRAINT DF_T_Consume_time DEFAULT GETDATE(),
    remark      NVARCHAR(200) NULL,
    CONSTRAINT CK_T_Consume_amount CHECK (amount > 0)
);");

        // T_OperateLog（操作日志表，PRD 5.1.8）
        ExecuteIfTableMissing(conn, "T_OperateLog", @"
CREATE TABLE T_OperateLog (
    logID          INT           IDENTITY(1,1) CONSTRAINT PK_T_OperateLog PRIMARY KEY,
    userName       NVARCHAR(16)  NOT NULL,
    operateTime    DATETIME      NOT NULL CONSTRAINT DF_T_OperateLog_time DEFAULT GETDATE(),
    operateType    NVARCHAR(20)  NOT NULL,
    operateContent NVARCHAR(100) NOT NULL,
    detail         NVARCHAR(300) NULL
);");

        // ===== 创建索引（PRD 5.3，索引创建失败不影响核心功能） =====
        CreateIndexIfMissing(conn, "IX_T_Room_status", "T_Room", "roomStatus");
        CreateIndexIfMissing(conn, "IX_T_Room_floor", "T_Room", "floor");
        CreateIndexIfMissing(conn, "IX_T_CheckIn_status", "T_CheckIn", "status");
        CreateIndexIfMissing(conn, "IX_T_CheckIn_customer", "T_CheckIn", "customerID");
        CreateIndexIfMissing(conn, "IX_T_CheckIn_room", "T_CheckIn", "roomNo");
        CreateIndexIfMissing(conn, "IX_T_CheckIn_time", "T_CheckIn", "checkInTime, checkOutTime");
        CreateIndexIfMissing(conn, "IX_T_Customer_name", "T_Customer", "customerName");
        CreateIndexIfMissing(conn, "IX_T_Reservation_status", "T_Reservation", "status");
        CreateIndexIfMissing(conn, "IX_T_Consume_checkIn", "T_Consume", "checkInID");
        CreateIndexIfMissing(conn, "IX_T_OperateLog_time", "T_OperateLog", "operateTime");
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
    /// 创建非聚集索引（幂等，失败时忽略）
    /// </summary>
    private static void CreateIndexIfMissing(SqlConnection conn, string indexName, string table, string columns)
    {
        try
        {
            string sql = $@"IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='{indexName}' AND object_id=OBJECT_ID('{table}'))
CREATE NONCLUSTERED INDEX {indexName} ON {table} ({columns});";
            using SqlCommand cmd = new(sql, conn);
            cmd.ExecuteNonQuery();
        }
        catch { /* 索引创建失败不影响核心功能 */ }
    }

    /// <summary>
    /// 确保默认管理员账号存在（密码 SHA-256 哈希，动态计算避免硬编码错误）
    /// </summary>
    private static void EnsureDefaultUsers(SqlConnection conn)
    {
        // 动态计算 SHA-256 哈希，与 SecurityUtil 保持一致
        string adminPwdHash = SecurityUtil.ComputeSha256Hash("admin123");
        string frontPwdHash = SecurityUtil.ComputeSha256Hash("front123");
        InsertUserIfMissing(conn, "admin", adminPwdHash, "管理员", "系统管理员");
        InsertUserIfMissing(conn, "front01", frontPwdHash, "前台", "前台操作员");
    }

    private static void InsertUserIfMissing(SqlConnection conn, string name, string pwdHash, string purview, string realName)
    {
        using SqlCommand cmd = new(
            "IF NOT EXISTS (SELECT 1 FROM T_User WHERE userName=@name) " +
            "INSERT INTO T_User (userName, userPassword, userPurview, realName) VALUES (@name, @pwd, @purview, @realName)", conn);
        cmd.Parameters.AddWithValue("@name", name);
        cmd.Parameters.AddWithValue("@pwd", pwdHash);
        cmd.Parameters.AddWithValue("@purview", purview);
        cmd.Parameters.AddWithValue("@realName", (object)realName ?? DBNull.Value);
        cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// 确保6种预置客房类型存在（PRD 第10节）
    /// </summary>
    private static void EnsureDefaultRoomTypes(SqlConnection conn)
    {
        InsertRoomTypeIfMissing(conn, "RT01", "豪华套间", 588m, 2, "豪华装修，配独立客厅");
        InsertRoomTypeIfMissing(conn, "RT02", "标准套间", 368m, 2, "标准装修，配独立客厅");
        InsertRoomTypeIfMissing(conn, "RT03", "三人间", 268m, 3, "三张单人床");
        InsertRoomTypeIfMissing(conn, "RT04", "标准间", 198m, 2, "两张单人床");
        InsertRoomTypeIfMissing(conn, "RT05", "单人间", 138m, 1, "一张单人床");
        InsertRoomTypeIfMissing(conn, "RT06", "其它", 100m, 1, "其它房型");
    }

    private static void InsertRoomTypeIfMissing(SqlConnection conn, string id, string name, decimal price, int bed, string desc)
    {
        using SqlCommand cmd = new(
            "IF NOT EXISTS (SELECT 1 FROM T_RoomType WHERE typeID=@id) " +
            "INSERT INTO T_RoomType (typeID, typeName, price, bedCount, description) VALUES (@id, @name, @price, @bed, @desc)", conn);
        cmd.Parameters.AddWithValue("@id", id);
        cmd.Parameters.AddWithValue("@name", name);
        cmd.Parameters.AddWithValue("@price", price);
        cmd.Parameters.AddWithValue("@bed", bed);
        cmd.Parameters.AddWithValue("@desc", (object)desc ?? DBNull.Value);
        cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// 确保示例客房存在（PRD 第10节：2楼201-206，3楼301-306）
    /// 房型分配：单号标准间、双号单人间、尾号6为套间
    /// </summary>
    private static void EnsureDefaultRooms(SqlConnection conn)
    {
        // 2楼：201标准间/202单人间/203标准间/204单人间/205标准间/206标准套间
        InsertRoomIfMissing(conn, "201", "RT04", 2, 2);
        InsertRoomIfMissing(conn, "202", "RT05", 2, 1);
        InsertRoomIfMissing(conn, "203", "RT04", 2, 2);
        InsertRoomIfMissing(conn, "204", "RT05", 2, 1);
        InsertRoomIfMissing(conn, "205", "RT04", 2, 2);
        InsertRoomIfMissing(conn, "206", "RT02", 2, 2);
        // 3楼：301标准间/302单人间/303标准间/304单人间/305标准间/306豪华套间
        InsertRoomIfMissing(conn, "301", "RT04", 3, 2);
        InsertRoomIfMissing(conn, "302", "RT05", 3, 1);
        InsertRoomIfMissing(conn, "303", "RT04", 3, 2);
        InsertRoomIfMissing(conn, "304", "RT05", 3, 1);
        InsertRoomIfMissing(conn, "305", "RT04", 3, 2);
        InsertRoomIfMissing(conn, "306", "RT01", 3, 2);
    }

    private static void InsertRoomIfMissing(SqlConnection conn, string roomNo, string typeId, int floor, int bed)
    {
        using SqlCommand cmd = new(
            "IF NOT EXISTS (SELECT 1 FROM T_Room WHERE roomNo=@roomNo) " +
            "INSERT INTO T_Room (roomNo, typeID, floor, bedCount, roomStatus) VALUES (@roomNo, @typeId, @floor, @bed, N'空闲')", conn);
        cmd.Parameters.AddWithValue("@roomNo", roomNo);
        cmd.Parameters.AddWithValue("@typeId", typeId);
        cmd.Parameters.AddWithValue("@floor", floor);
        cmd.Parameters.AddWithValue("@bed", bed);
        cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// 确保预设客户数据存在（用于演示 / 首次使用有数据可看）
    /// 按身份证号去重，避免重复插入
    /// </summary>
    private static void EnsureDefaultCustomers(SqlConnection conn)
    {
        InsertCustomerIfMissing(conn, "张三", "男", "身份证", "110101199001011234", "13800001111", "北京市朝阳区建国路100号");
        InsertCustomerIfMissing(conn, "李四", "女", "身份证", "310101199205201234", "13900002222", "上海市浦东新区陆家嘴1号");
        InsertCustomerIfMissing(conn, "王五", "男", "护照", "E12345678", "13600003333", "广东省广州市天河区体育西路50号");
        InsertCustomerIfMissing(conn, "赵六", "女", "身份证", "440101199807152345", "13700004444", "深圳市南山区科技园路88号");
        InsertCustomerIfMissing(conn, "陈七", "男", "身份证", "500101198503101345", "13500005555", "重庆市渝中区解放碑5号");
    }

    private static void InsertCustomerIfMissing(SqlConnection conn, string name, string gender, string idType, string idNumber, string phone, string address)
    {
        using SqlCommand cmd = new(
            "IF NOT EXISTS (SELECT 1 FROM T_Customer WHERE idNumber=@idNumber) " +
            "INSERT INTO T_Customer (customerName, gender, idType, idNumber, phone, address) " +
            "VALUES (@name, @gender, @idType, @idNumber, @phone, @address)", conn);
        cmd.Parameters.AddWithValue("@name", name);
        cmd.Parameters.AddWithValue("@gender", gender);
        cmd.Parameters.AddWithValue("@idType", idType);
        cmd.Parameters.AddWithValue("@idNumber", idNumber);
        cmd.Parameters.AddWithValue("@phone", phone);
        cmd.Parameters.AddWithValue("@address", address);
        cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// 确保预设预订数据存在（用于演示）
    /// 通过身份证号关联客户，通过房号关联房间
    /// </summary>
    private static void EnsureDefaultReservations(SqlConnection conn)
    {
        // 张三预订 201房（标准间）3天，明天入住
        InsertReservationIfMissing(conn, "110101199001011234", "201", 1, 3, "13800001111", "商务出差");
        // 李四预订 301房（标准间）2天，后天入住
        InsertReservationIfMissing(conn, "310101199205201234", "301", 2, 2, "13900002222", "旅游度假");
        // 陈七预订 206房（标准套间）1天，三天后入住
        InsertReservationIfMissing(conn, "500101198503101345", "206", 3, 1, "13500005555", "会议");
    }

    private static void InsertReservationIfMissing(SqlConnection conn, string idNumber, string roomNo, int daysFromNow, int expectDays, string phone, string remark)
    {
        using SqlCommand cmd = new(
            "IF NOT EXISTS (SELECT 1 FROM T_Reservation r JOIN T_Customer c ON r.customerID=c.customerID WHERE c.idNumber=@idNumber AND r.roomNo=@roomNo) " +
            "BEGIN " +
            "  INSERT INTO T_Reservation (customerID, roomNo, expectCheckIn, expectDays, contactPhone, status, remark) " +
            "  SELECT c.customerID, @roomNo, CAST(DATEADD(day, @daysFromNow, GETDATE()) AS DATE), @expectDays, @phone, N'待入住', @remark " +
            "  FROM T_Customer c WHERE c.idNumber=@idNumber; " +
            "  UPDATE T_Room SET roomStatus=N'预留' WHERE roomNo=@roomNo; " +
            "END", conn);
        cmd.Parameters.AddWithValue("@idNumber", idNumber);
        cmd.Parameters.AddWithValue("@roomNo", roomNo);
        cmd.Parameters.AddWithValue("@daysFromNow", daysFromNow);
        cmd.Parameters.AddWithValue("@expectDays", expectDays);
        cmd.Parameters.AddWithValue("@phone", phone);
        cmd.Parameters.AddWithValue("@remark", (object)remark ?? DBNull.Value);
        cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// 确保预设入住数据存在（用于演示）
    /// </summary>
    private static void EnsureDefaultCheckIns(SqlConnection conn)
    {
        // 王五 入住 202房（单人间）2天，昨天入住，在住中
        InsertCheckInIfMissing(conn, "E12345678", "202", -1, 2, 138m, "安静楼层");
        // 赵六 入住 303房（标准间）3天，前天入住，在住中
        InsertCheckInIfMissing(conn, "440101199807152345", "303", -2, 3, 198m, "高层景观房");
    }

    private static void InsertCheckInIfMissing(SqlConnection conn, string idNumber, string roomNo, int daysAgo, int stayDays, decimal deposit, string remark)
    {
        using SqlCommand cmd = new(
            "IF NOT EXISTS (SELECT 1 FROM T_CheckIn ci JOIN T_Customer c ON ci.customerID=c.customerID WHERE c.idNumber=@idNumber AND ci.roomNo=@roomNo AND ci.status=N'在住') " +
            "BEGIN " +
            "  INSERT INTO T_CheckIn (customerID, roomNo, checkInTime, expectCheckOut, deposit, status, remark) " +
            "  SELECT c.customerID, @roomNo, DATEADD(day, @daysAgo, GETDATE()), DATEADD(day, @daysAgo + @stayDays, GETDATE()), @deposit, N'在住', @remark " +
            "  FROM T_Customer c WHERE c.idNumber=@idNumber; " +
            "  UPDATE T_Room SET roomStatus=N'在住' WHERE roomNo=@roomNo; " +
            "END", conn);
        cmd.Parameters.AddWithValue("@idNumber", idNumber);
        cmd.Parameters.AddWithValue("@roomNo", roomNo);
        cmd.Parameters.AddWithValue("@daysAgo", daysAgo);
        cmd.Parameters.AddWithValue("@stayDays", stayDays);
        cmd.Parameters.AddWithValue("@deposit", deposit);
        cmd.Parameters.AddWithValue("@remark", (object)remark ?? DBNull.Value);
        cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// 确保历史已结账记录存在（用于统计界面演示，PRD 4.2.11）
    /// 补充5条不同房型的已结账入住记录，覆盖今日退房、历史退房场景
    /// 注意：不修改房间当前状态，仅插入历史退房数据供统计聚合
    /// </summary>
    private static void EnsureDefaultCheckedOutRecords(SqlConnection conn)
    {
        // 张三 201房（标准间RT04,198元）3天前入住，住2天，昨天退房
        InsertCheckedOutIfMissing(conn, "110101199001011234", "201", -3, 2, 198m, 396m);
        // 李四 301房（标准间RT04,198元）5天前入住，住5天，今天退房
        InsertCheckedOutIfMissing(conn, "310101199205201234", "301", -5, 5, 198m, 990m);
        // 陈七 206房（标准套间RT02,368元）10天前入住，住2天，8天前退房
        InsertCheckedOutIfMissing(conn, "500101198503101345", "206", -10, 2, 368m, 736m);
        // 张三 305房（标准间RT04,198元）15天前入住，住2天，13天前退房
        InsertCheckedOutIfMissing(conn, "110101199001011234", "305", -15, 2, 198m, 396m);
        // 王五 306房（豪华套间RT01,588元）20天前入住，住2天，18天前退房
        InsertCheckedOutIfMissing(conn, "E12345678", "306", -20, 2, 588m, 1176m);
    }

    /// <summary>
    /// 插入已结账历史记录（幂等，不修改房间当前状态）
    /// </summary>
    /// <param name="conn">数据库连接</param>
    /// <param name="idNumber">客户证件号</param>
    /// <param name="roomNo">房号</param>
    /// <param name="checkInDaysAgo">入住时间距今天的天数（负数表示过去）</param>
    /// <param name="stayDays">实际住宿天数</param>
    /// <param name="dailyPrice">每日房费</param>
    /// <param name="totalAmount">总金额（=每日房费×住宿天数）</param>
    private static void InsertCheckedOutIfMissing(SqlConnection conn, string idNumber, string roomNo, int checkInDaysAgo, int stayDays, decimal dailyPrice, decimal totalAmount)
    {
        // 去重：同一客户在同一房间已有已结账记录则跳过
        using SqlCommand cmd = new(
            "IF NOT EXISTS (SELECT 1 FROM T_CheckIn ci JOIN T_Customer c ON ci.customerID=c.customerID " +
            "WHERE c.idNumber=@idNumber AND ci.roomNo=@roomNo AND ci.status=N'已结账') " +
            "BEGIN " +
            "  INSERT INTO T_CheckIn (customerID, roomNo, checkInTime, expectCheckOut, checkOutTime, actualDays, deposit, roomCharge, otherCharge, consumeAmount, totalAmount, status, remark) " +
            "  SELECT c.customerID, @roomNo, DATEADD(day, @checkInDaysAgo, GETDATE()), " +
            "         DATEADD(day, @checkInDaysAgo + @stayDays, GETDATE()), " +
            "         DATEADD(day, @checkInDaysAgo + @stayDays, GETDATE()), " +
            "         @stayDays, @dailyPrice, @totalAmount, 0, 0, @totalAmount, N'已结账', N'正常退房' " +
            "  FROM T_Customer c WHERE c.idNumber=@idNumber; " +
            "END", conn);
        cmd.Parameters.AddWithValue("@idNumber", idNumber);
        cmd.Parameters.AddWithValue("@roomNo", roomNo);
        cmd.Parameters.AddWithValue("@checkInDaysAgo", checkInDaysAgo);
        cmd.Parameters.AddWithValue("@stayDays", stayDays);
        cmd.Parameters.AddWithValue("@dailyPrice", dailyPrice);
        cmd.Parameters.AddWithValue("@totalAmount", totalAmount);
        cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// 确保预设消费数据存在（用于演示）
    /// 通过身份证号关联到在住客户的入住记录
    /// </summary>
    private static void EnsureDefaultConsumes(SqlConnection conn)
    {
        // 王五消费：矿泉水 10元
        InsertConsumeIfMissing(conn, "E12345678", "202", "矿泉水", 10m, "客房服务");
        // 赵六消费：早餐 30元
        InsertConsumeIfMissing(conn, "440101199807152345", "303", "早餐", 30m, "餐饮部");
        // 赵六消费：洗衣服务 50元
        InsertConsumeIfMissing(conn, "440101199807152345", "303", "洗衣服务", 50m, "客房服务");
    }

    private static void InsertConsumeIfMissing(SqlConnection conn, string idNumber, string roomNo, string itemName, decimal amount, string remark)
    {
        using SqlCommand cmd = new(
            "IF NOT EXISTS (SELECT 1 FROM T_Consume co " +
            "JOIN T_CheckIn ci ON co.checkInID=ci.checkInID " +
            "JOIN T_Customer c ON ci.customerID=c.customerID " +
            "WHERE c.idNumber=@idNumber AND ci.roomNo=@roomNo AND ci.status=N'在住' AND co.itemName=@itemName) " +
            "INSERT INTO T_Consume (checkInID, itemName, amount, remark) " +
            "SELECT ci.checkInID, @itemName, @amount, @remark " +
            "FROM T_CheckIn ci JOIN T_Customer c ON ci.customerID=c.customerID " +
            "WHERE c.idNumber=@idNumber AND ci.roomNo=@roomNo AND ci.status=N'在住'", conn);
        cmd.Parameters.AddWithValue("@idNumber", idNumber);
        cmd.Parameters.AddWithValue("@roomNo", roomNo);
        cmd.Parameters.AddWithValue("@itemName", itemName);
        cmd.Parameters.AddWithValue("@amount", amount);
        cmd.Parameters.AddWithValue("@remark", (object)remark ?? DBNull.Value);
        cmd.ExecuteNonQuery();
    }
}
