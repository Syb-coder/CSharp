-- ============================================================
-- 图书馆信息管理系统 - 数据库初始化脚本
-- 目标数据库：SQL Server 2025（Windows 认证）
-- 执行方式：在 SSMS 中以 Windows 认证连接后执行本脚本
-- ============================================================

-- 1. 创建数据库
IF DB_ID('LibraryDB') IS NULL
BEGIN
    CREATE DATABASE LibraryDB;
END
GO

USE LibraryDB;
GO

-- 2. 创建数据表

-- 2.1 系统用户表
IF OBJECT_ID('tbl_User', 'U') IS NULL
BEGIN
    CREATE TABLE tbl_User (
        userName     NVARCHAR(16)  NOT NULL,
        userPassword NVARCHAR(64)  NOT NULL,  -- SHA-256 哈希值（64位十六进制字符串）
        userPurview  NVARCHAR(16)  NOT NULL,  -- 取值：管理员 / 普通用户
        CONSTRAINT PK_tbl_User PRIMARY KEY (userName)
    );
END
GO

-- 2.2 图书类别表
IF OBJECT_ID('tbl_BookCategory', 'U') IS NULL
BEGIN
    CREATE TABLE tbl_BookCategory (
        categoryID   NVARCHAR(10)   NOT NULL,
        categoryName NVARCHAR(20)   NOT NULL,
        borrowDays   INT            NOT NULL DEFAULT 30,   -- 可借阅天数
        finePerDay   DECIMAL(10, 2) NOT NULL DEFAULT 0.50, -- 单日逾期罚款标准（元/天）
        CONSTRAINT PK_tbl_BookCategory PRIMARY KEY (categoryID)
    );
END
GO

-- 2.3 图书信息表
IF OBJECT_ID('tbl_Book', 'U') IS NULL
BEGIN
    CREATE TABLE tbl_Book (
        bookID         NVARCHAR(20)   NOT NULL,
        bookName       NVARCHAR(100)  NOT NULL,
        author         NVARCHAR(50)   NULL,
        publisher      NVARCHAR(50)   NULL,
        publishDate    DATE           NULL,
        ISBN           NVARCHAR(13)   NULL,
        price          DECIMAL(10, 2) NULL,
        categoryID     NVARCHAR(10)   NOT NULL,
        totalCount     INT            NOT NULL,
        availableCount INT            NOT NULL,
        CONSTRAINT PK_tbl_Book PRIMARY KEY (bookID),
        CONSTRAINT FK_tbl_Book_Category FOREIGN KEY (categoryID)
            REFERENCES tbl_BookCategory(categoryID),
        CONSTRAINT CK_tbl_Book_count CHECK (totalCount >= 0 AND availableCount >= 0 AND availableCount <= totalCount),
        CONSTRAINT CK_tbl_Book_price CHECK (price IS NULL OR price > 0)
    );
END
GO

-- 2.4 读者信息表
IF OBJECT_ID('tbl_Reader', 'U') IS NULL
BEGIN
    CREATE TABLE tbl_Reader (
        readerID     NVARCHAR(20) NOT NULL,
        readerName   NVARCHAR(8)  NOT NULL,
        readerSex    NVARCHAR(2)  NOT NULL,
        phone        NVARCHAR(15) NULL,
        department   NVARCHAR(20) NULL,
        registerDate DATE         NULL,
        CONSTRAINT PK_tbl_Reader PRIMARY KEY (readerID),
        CONSTRAINT CK_tbl_Reader_sex CHECK (readerSex IN (N'男', N'女'))
    );
END
GO

-- 2.5 借阅信息表
IF OBJECT_ID('tbl_Borrow', 'U') IS NULL
BEGIN
    CREATE TABLE tbl_Borrow (
        borrowID    INT          IDENTITY(1,1),
        readerID    NVARCHAR(20) NOT NULL,
        bookID      NVARCHAR(20) NOT NULL,
        borrowDate  DATE         NOT NULL,
        dueDate     DATE         NOT NULL,
        returnDate  DATE         NULL,
        status      NVARCHAR(4)  NOT NULL,  -- 取值：借出 / 已还
        CONSTRAINT PK_tbl_Borrow PRIMARY KEY (borrowID),
        CONSTRAINT FK_tbl_Borrow_Reader FOREIGN KEY (readerID)
            REFERENCES tbl_Reader(readerID),
        CONSTRAINT FK_tbl_Borrow_Book FOREIGN KEY (bookID)
            REFERENCES tbl_Book(bookID),
        CONSTRAINT CK_tbl_Borrow_status CHECK (status IN (N'借出', N'已还'))
    );
END
GO

-- 3. 初始化测试数据

-- 3.1 用户数据（密码的 SHA-256 哈希值，由 PasswordHelper.ComputeHash 生成）
--     admin123 -> 240BE518FABD2724DDB6F04EEB1DA5967448D7E831C08C8FA822809F74C720A9
--     user123  -> E606E38B0D8C19B24CF0EE3808183162EA7CD63FF7912DBB22B5E803286B4446
IF NOT EXISTS (SELECT 1 FROM tbl_User)
BEGIN
    INSERT INTO tbl_User (userName, userPassword, userPurview) VALUES
    (N'admin', N'240BE518FABD2724DDB6F04EEB1DA5967448D7E831C08C8FA822809F74C720A9', N'管理员'),
    (N'user01', N'E606E38B0D8C19B24CF0EE3808183162EA7CD63FF7912DBB22B5E803286B4446', N'普通用户');
END
GO

-- 3.2 图书类别数据
IF NOT EXISTS (SELECT 1 FROM tbl_BookCategory)
BEGIN
    INSERT INTO tbl_BookCategory (categoryID, categoryName, borrowDays, finePerDay) VALUES
    (N'C01', N'计算机类', 15, 1.00),
    (N'C02', N'文学类',   30, 0.50),
    (N'C03', N'经济类',   20, 0.80);
END
GO

-- 3.3 图书数据
IF NOT EXISTS (SELECT 1 FROM tbl_Book)
BEGIN
    INSERT INTO tbl_Book (bookID, bookName, author, publisher, publishDate, ISBN, price, categoryID, totalCount, availableCount) VALUES
    (N'B001', N'C# 入门经典', N'Karli Watson', N'清华大学出版社', '2020-03-15', '9787302533918', 89.00, N'C01', 5, 4),
    (N'B002', N'数据结构与算法', N'严蔚敏', N'人民邮电出版社', '2019-08-01', '9787115415592', 69.00, N'C01', 3, 3),
    (N'B003', N'红楼梦', N'曹雪芹', N'人民文学出版社', '2018-05-20', '9787020002207', 59.00, N'C02', 4, 3),
    (N'B004', N'百年孤独', N'加西亚·马尔克斯', N'南海出版公司', '2017-06-10', '9787544291170', 39.50, N'C02', 2, 2),
    (N'B005', N'经济学原理', N'曼昆', N'北京大学出版社', '2021-01-15', '9787301318025', 128.00, N'C03', 3, 3);
END
GO

-- 3.4 读者数据
IF NOT EXISTS (SELECT 1 FROM tbl_Reader)
BEGIN
    INSERT INTO tbl_Reader (readerID, readerName, readerSex, phone, department, registerDate) VALUES
    (N'R001', N'张三', N'男', N'13800138001', N'计算机学院', '2024-09-01'),
    (N'R002', N'李四', N'女', N'13800138002', N'文学院', '2024-09-05'),
    (N'R003', N'王五', N'男', N'13800138003', N'经济管理学院', '2024-10-10');
END
GO

-- 3.5 借阅数据（B001 已借出 1 本，B003 已借出 1 本）
IF NOT EXISTS (SELECT 1 FROM tbl_Borrow)
BEGIN
    INSERT INTO tbl_Borrow (readerID, bookID, borrowDate, dueDate, returnDate, status) VALUES
    (N'R001', N'B001', '2025-06-15', '2025-07-15', NULL, N'借出'),
    (N'R002', N'B003', '2025-06-20', '2025-07-20', NULL, N'借出'),
    (N'R001', N'B002', '2025-05-10', '2025-06-09', '2025-06-05', N'已还');
END
GO

PRINT N'LibraryDB 数据库初始化完成';
GO
