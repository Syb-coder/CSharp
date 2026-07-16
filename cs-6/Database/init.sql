-- ============================================================
-- 智慧酒店管理系统 HotelDB 数据库初始化脚本（PRD 第5节）
-- 项目编号：cs-6
-- 说明：程序启动时 DBInitializer 会自动建库建表，此脚本为手动初始化备选
-- 执行方式：在 SSMS 中直接执行，或 sqlcmd -S localhost -E -f 65001 -i init.sql
-- 注意：sqlcmd 执行 UTF-8 脚本必须加 -f 65001（见经验文档经验二）
-- ============================================================

IF DB_ID(N'HotelDB') IS NULL
    CREATE DATABASE HotelDB;
GO
USE HotelDB;
GO

-- ===== 1. 系统用户表 T_User（PRD 5.1.1） =====
IF OBJECT_ID(N'T_User', N'U') IS NULL
CREATE TABLE T_User (
    userName     NVARCHAR(16) NOT NULL CONSTRAINT PK_T_User PRIMARY KEY,
    userPassword NVARCHAR(64) NOT NULL,  -- SHA-256 64位十六进制
    userPurview  NVARCHAR(8)  NOT NULL CONSTRAINT CK_T_User_purview CHECK (userPurview IN (N'管理员', N'前台')),
    realName     NVARCHAR(20) NULL
);
GO

-- ===== 2. 客房类型表 T_RoomType（PRD 5.1.2） =====
IF OBJECT_ID(N'T_RoomType', N'U') IS NULL
CREATE TABLE T_RoomType (
    typeID      NVARCHAR(10)  NOT NULL CONSTRAINT PK_T_RoomType PRIMARY KEY,
    typeName    NVARCHAR(20)  NOT NULL CONSTRAINT UQ_T_RoomType_name UNIQUE,
    price       DECIMAL(8, 2) NOT NULL,
    bedCount    INT           NOT NULL,
    description NVARCHAR(200) NULL,
    CONSTRAINT CK_T_RoomType_price CHECK (price > 0),
    CONSTRAINT CK_T_RoomType_bed CHECK (bedCount > 0)
);
GO

-- ===== 3. 客房信息表 T_Room（PRD 5.1.3） =====
IF OBJECT_ID(N'T_Room', N'U') IS NULL
CREATE TABLE T_Room (
    roomNo     NVARCHAR(10) NOT NULL CONSTRAINT PK_T_Room PRIMARY KEY,
    typeID     NVARCHAR(10) NOT NULL CONSTRAINT FK_T_Room_RoomType FOREIGN KEY REFERENCES T_RoomType(typeID),
    floor      INT          NOT NULL,
    bedCount   INT          NOT NULL,
    roomStatus NVARCHAR(6)  NOT NULL CONSTRAINT CK_T_Room_status CHECK (roomStatus IN (N'空闲', N'在住', N'预留', N'维护')),
    remark     NVARCHAR(200) NULL,
    CONSTRAINT CK_T_Room_floor CHECK (floor > 0),
    CONSTRAINT CK_T_Room_bed CHECK (bedCount > 0)
);
GO

-- ===== 4. 客户信息表 T_Customer（PRD 5.1.4） =====
IF OBJECT_ID(N'T_Customer', N'U') IS NULL
CREATE TABLE T_Customer (
    customerID   INT           IDENTITY(1,1) CONSTRAINT PK_T_Customer PRIMARY KEY,
    customerName NVARCHAR(20)  NOT NULL,
    gender       NVARCHAR(2)   NULL CONSTRAINT CK_T_Customer_gender CHECK (gender IN (N'男', N'女')),
    idType       NVARCHAR(10)  NOT NULL,
    idNumber     NVARCHAR(30)  NOT NULL,
    phone        NVARCHAR(15)  NULL,
    address      NVARCHAR(200) NULL,
    createTime   DATETIME      NOT NULL CONSTRAINT DF_T_Customer_time DEFAULT GETDATE()
);
GO

-- ===== 5. 预订记录表 T_Reservation（PRD 5.1.5） =====
IF OBJECT_ID(N'T_Reservation', N'U') IS NULL
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
);
GO

-- ===== 6. 入住记录表 T_CheckIn（PRD 5.1.6，核心业务表） =====
IF OBJECT_ID(N'T_CheckIn', N'U') IS NULL
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
);
GO

-- ===== 7. 消费记录表 T_Consume（PRD 5.1.7） =====
IF OBJECT_ID(N'T_Consume', N'U') IS NULL
CREATE TABLE T_Consume (
    consumeID   INT           IDENTITY(1,1) CONSTRAINT PK_T_Consume PRIMARY KEY,
    checkInID   INT           NOT NULL CONSTRAINT FK_T_Consume_CheckIn FOREIGN KEY REFERENCES T_CheckIn(checkInID),
    itemName    NVARCHAR(50)  NOT NULL,
    amount      DECIMAL(8, 2) NOT NULL,
    consumeTime DATETIME      NOT NULL CONSTRAINT DF_T_Consume_time DEFAULT GETDATE(),
    remark      NVARCHAR(200) NULL,
    CONSTRAINT CK_T_Consume_amount CHECK (amount > 0)
);
GO

-- ===== 8. 操作日志表 T_OperateLog（PRD 5.1.8） =====
IF OBJECT_ID(N'T_OperateLog', N'U') IS NULL
CREATE TABLE T_OperateLog (
    logID          INT           IDENTITY(1,1) CONSTRAINT PK_T_OperateLog PRIMARY KEY,
    userName       NVARCHAR(16)  NOT NULL,
    operateTime    DATETIME      NOT NULL CONSTRAINT DF_T_OperateLog_time DEFAULT GETDATE(),
    operateType    NVARCHAR(20)  NOT NULL,
    operateContent NVARCHAR(100) NOT NULL,
    detail         NVARCHAR(300) NULL
);
GO

-- ===== 9. 索引（PRD 5.3） =====
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_T_Room_status' AND object_id=OBJECT_ID('T_Room'))
    CREATE NONCLUSTERED INDEX IX_T_Room_status ON T_Room(roomStatus);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_T_Room_floor' AND object_id=OBJECT_ID('T_Room'))
    CREATE NONCLUSTERED INDEX IX_T_Room_floor ON T_Room(floor);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_T_CheckIn_status' AND object_id=OBJECT_ID('T_CheckIn'))
    CREATE NONCLUSTERED INDEX IX_T_CheckIn_status ON T_CheckIn(status);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_T_CheckIn_customer' AND object_id=OBJECT_ID('T_CheckIn'))
    CREATE NONCLUSTERED INDEX IX_T_CheckIn_customer ON T_CheckIn(customerID);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_T_CheckIn_room' AND object_id=OBJECT_ID('T_CheckIn'))
    CREATE NONCLUSTERED INDEX IX_T_CheckIn_room ON T_CheckIn(roomNo);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_T_CheckIn_time' AND object_id=OBJECT_ID('T_CheckIn'))
    CREATE NONCLUSTERED INDEX IX_T_CheckIn_time ON T_CheckIn(checkInTime, checkOutTime);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_T_Customer_name' AND object_id=OBJECT_ID('T_Customer'))
    CREATE NONCLUSTERED INDEX IX_T_Customer_name ON T_Customer(customerName);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_T_Reservation_status' AND object_id=OBJECT_ID('T_Reservation'))
    CREATE NONCLUSTERED INDEX IX_T_Reservation_status ON T_Reservation(status);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_T_Consume_checkIn' AND object_id=OBJECT_ID('T_Consume'))
    CREATE NONCLUSTERED INDEX IX_T_Consume_checkIn ON T_Consume(checkInID);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_T_OperateLog_time' AND object_id=OBJECT_ID('T_OperateLog'))
    CREATE NONCLUSTERED INDEX IX_T_OperateLog_time ON T_OperateLog(operateTime);
GO
