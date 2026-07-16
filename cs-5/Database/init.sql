-- ============================================================
-- 智慧图书馆管理系统（cs-5） - 数据库初始化脚本
-- 目标数据库：SQL Server 2019+（Windows 认证）
-- 执行方式：在 SSMS 中以 Windows 认证连接后执行本脚本
-- 表命名规范：T_ 前缀（与 cs-1 的 tbl_ 前缀区分，共用 LibraryDB 实例）
-- 密码加密：MD5 哈希（32 位十六进制字符串）
-- 借阅期限：45 天 | 罚款：0.3 元/天 | 借阅上限：3 本
-- ============================================================

-- 1. 创建数据库（与 cs-1 共用，已存在则跳过）
IF DB_ID('LibraryDB') IS NULL
BEGIN
    CREATE DATABASE LibraryDB;
END
GO

USE LibraryDB;
GO

-- 2. 创建数据表

-- 2.1 系统用户表（T_User）
IF OBJECT_ID('T_User', 'U') IS NULL
BEGIN
    CREATE TABLE T_User (
        userName     NVARCHAR(16) NOT NULL,                -- 用户名（主键）
        userPassword NVARCHAR(32) NOT NULL,                -- 密码 MD5 哈希值（32位十六进制）
        userPurview  NVARCHAR(8) NOT NULL,                 -- 权限：管理员 / 普通用户
        CONSTRAINT PK_T_User PRIMARY KEY (userName),
        CONSTRAINT CK_T_User_purview CHECK (userPurview IN (N'管理员', N'普通用户'))
    );
END
GO

-- 2.2 图书类型表（T_BookType）
IF OBJECT_ID('T_BookType', 'U') IS NULL
BEGIN
    CREATE TABLE T_BookType (
        typeID   NVARCHAR(10) NOT NULL,                    -- 类型编号（主键）
        typeName NVARCHAR(20) NOT NULL,                    -- 类型名称
        CONSTRAINT PK_T_BookType PRIMARY KEY (typeID)
    );
END
GO

-- 2.3 图书信息表（T_Book）
IF OBJECT_ID('T_Book', 'U') IS NULL
BEGIN
    CREATE TABLE T_Book (
        bookID         NVARCHAR(20)   NOT NULL,            -- 图书编号（主键）
        bookName       NVARCHAR(100)  NOT NULL,            -- 书名
        author         NVARCHAR(50)   NULL,                -- 作者
        publisher      NVARCHAR(50)   NULL,                -- 出版社
        publishDate    DATE           NULL,                -- 出版日期
        ISBN           NVARCHAR(13)   NULL,                -- ISBN 号
        price          DECIMAL(8, 2)  NULL,                -- 价格
        typeID         NVARCHAR(10)   NOT NULL,            -- 类型编号（外键）
        totalCount     INT            NOT NULL DEFAULT 0,  -- 馆藏数量
        availableCount INT            NOT NULL DEFAULT 0,  -- 可借数量（冗余字段，借还书事务维护一致性）
        coverImage     NVARCHAR(200)  NULL,                -- 封面图片路径（预留字段）
        CONSTRAINT PK_T_Book PRIMARY KEY (bookID),
        CONSTRAINT FK_T_Book_BookType FOREIGN KEY (typeID)
            REFERENCES T_BookType(typeID),
        CONSTRAINT CK_T_Book_count CHECK (totalCount >= 0 AND availableCount >= 0 AND availableCount <= totalCount),
        CONSTRAINT CK_T_Book_price CHECK (price IS NULL OR price > 0)
    );
END
GO

-- 2.4 读者信息表（T_Reader）
IF OBJECT_ID('T_Reader', 'U') IS NULL
BEGIN
    CREATE TABLE T_Reader (
        readerID     NVARCHAR(20) NOT NULL,                -- 读者编号（主键）
        readerName   NVARCHAR(8)  NOT NULL,                -- 姓名
        readerSex    NVARCHAR(2)  NOT NULL,                -- 性别
        phone        NVARCHAR(15) NULL,                    -- 联系电话
        department   NVARCHAR(30) NULL,                    -- 所在院系
        registerDate DATE         NOT NULL DEFAULT GETDATE(), -- 注册日期
        CONSTRAINT PK_T_Reader PRIMARY KEY (readerID),
        CONSTRAINT CK_T_Reader_sex CHECK (readerSex IN (N'男', N'女'))
    );
END
GO

-- 2.5 借阅信息表（T_Borrow）
IF OBJECT_ID('T_Borrow', 'U') IS NULL
BEGIN
    CREATE TABLE T_Borrow (
        borrowID   INT          IDENTITY(1,1),             -- 借阅编号（自增主键）
        readerID   NVARCHAR(20) NOT NULL,                  -- 读者编号（外键）
        bookID     NVARCHAR(20) NOT NULL,                  -- 图书编号（外键）
        borrowDate DATE         NOT NULL,                  -- 借出日期
        dueDate    DATE         NOT NULL,                  -- 应还日期（借出日期 + 45 天）
        returnDate DATE         NULL,                      -- 归还日期（未归还时为 NULL）
        status     NVARCHAR(4)  NOT NULL,                  -- 状态：借出 / 已还
        CONSTRAINT PK_T_Borrow PRIMARY KEY (borrowID),
        CONSTRAINT FK_T_Borrow_Reader FOREIGN KEY (readerID)
            REFERENCES T_Reader(readerID),
        CONSTRAINT FK_T_Borrow_Book FOREIGN KEY (bookID)
            REFERENCES T_Book(bookID),
        CONSTRAINT CK_T_Borrow_status CHECK (status IN (N'借出', N'已还'))
    );
END
GO

-- 2.6 罚款信息表（T_Fine）
IF OBJECT_ID('T_Fine', 'U') IS NULL
BEGIN
    CREATE TABLE T_Fine (
        fineID      INT           IDENTITY(1,1),           -- 罚款编号（自增主键）
        readerID    NVARCHAR(20)  NOT NULL,                -- 读者编号（外键）
        bookID      NVARCHAR(20)  NOT NULL,                -- 图书编号（外键）
        borrowID    INT           NOT NULL,                -- 借阅编号（外键）
        overdueDays INT           NOT NULL,                -- 逾期天数
        fineAmount  DECIMAL(8, 2) NOT NULL,                -- 罚款金额（逾期天数 × 0.3 元）
        fineStatus  NVARCHAR(4)   NOT NULL,                -- 罚款状态：未缴 / 已缴
        createDate  DATE          NOT NULL,                -- 生成日期
        payDate     DATE          NULL,                    -- 缴费日期（未缴费时为 NULL）
        CONSTRAINT PK_T_Fine PRIMARY KEY (fineID),
        CONSTRAINT FK_T_Fine_Reader FOREIGN KEY (readerID)
            REFERENCES T_Reader(readerID),
        CONSTRAINT FK_T_Fine_Book FOREIGN KEY (bookID)
            REFERENCES T_Book(bookID),
        CONSTRAINT FK_T_Fine_Borrow FOREIGN KEY (borrowID)
            REFERENCES T_Borrow(borrowID),
        CONSTRAINT CK_T_Fine_status CHECK (fineStatus IN (N'未缴', N'已缴')),
        CONSTRAINT CK_T_Fine_days CHECK (overdueDays > 0),
        CONSTRAINT CK_T_Fine_amount CHECK (fineAmount > 0),
        -- 同一条借阅记录最多生成一条罚款记录
        CONSTRAINT UQ_T_Fine_borrow UNIQUE (borrowID)
    );
END
GO

-- 2.7 预约信息表（T_Reservation）【cs-5 核心新增表，区别于 cs-1】
IF OBJECT_ID('T_Reservation', 'U') IS NULL
BEGIN
    CREATE TABLE T_Reservation (
        reserveID    INT           IDENTITY(1,1),           -- 预约编号（自增主键）
        readerID     NVARCHAR(20)  NOT NULL,                -- 读者编号（外键）
        bookID       NVARCHAR(20)  NOT NULL,                -- 图书编号（外键）
        reserveDate  DATE          NOT NULL DEFAULT GETDATE(), -- 预约日期
        expireDate   DATE          NOT NULL,                -- 预约有效期至（预约日期 + 3 天）
        notifyDate   DATE          NULL,                    -- 通知取书日期（升级为"待取书"时填写）
        completeDate DATE          NULL,                    -- 完成日期（实际借书时填写）
        cancelDate   DATE          NULL,                    -- 取消日期
        cancelReason NVARCHAR(50)  NULL,                    -- 取消原因（手动取消/预约超时/取书超时）
        status       NVARCHAR(10)  NOT NULL,                -- 预约状态：排队中/待取书/已完成/已取消
        CONSTRAINT PK_T_Reservation PRIMARY KEY (reserveID),
        CONSTRAINT FK_T_Reservation_Reader FOREIGN KEY (readerID)
            REFERENCES T_Reader(readerID),
        CONSTRAINT FK_T_Reservation_Book FOREIGN KEY (bookID)
            REFERENCES T_Book(bookID),
        CONSTRAINT CK_T_Reservation_status CHECK (status IN (N'排队中', N'待取书', N'已完成', N'已取消'))
    );
    -- 同一读者对同一本书只能有一条活跃预约（排队中/待取书）
    -- 使用过滤唯一索引实现业务层约束
    CREATE UNIQUE NONCLUSTERED INDEX UQ_T_Reservation_active
        ON T_Reservation(readerID, bookID)
        WHERE status IN (N'排队中', N'待取书');
END
GO

-- 2.8 操作日志表（T_OperateLog）【cs-5 新增表，区别于 cs-1】
IF OBJECT_ID('T_OperateLog', 'U') IS NULL
BEGIN
    CREATE TABLE T_OperateLog (
        logID          INT           IDENTITY(1,1),         -- 日志编号（自增主键）
        userName       NVARCHAR(16)  NOT NULL,              -- 操作者用户名
        operateTime    DATETIME      NOT NULL DEFAULT GETDATE(), -- 操作时间
        operateType    NVARCHAR(20)  NOT NULL,              -- 操作类型（登录/新增/修改/删除/借书/还书/预约/取消预约/缴费）
        operateContent NVARCHAR(100) NOT NULL,              -- 操作对象（如"图书：TP001"、"读者：2025001"）
        detail         NVARCHAR(200) NULL,                  -- 详细说明
        CONSTRAINT PK_T_OperateLog PRIMARY KEY (logID)
    );
END
GO

-- 3. 初始化测试数据
-- T_Reservation 有筛选唯一索引，后续 INSERT 需要 QUOTED_IDENTIFIER ON
SET QUOTED_IDENTIFIER ON;
GO

-- 3.1 用户数据（密码的 MD5 哈希值，由 SecurityUtil.ComputeMd5Hash 生成，大写十六进制）
--     admin123 -> 0192023A7BBD73250516F069DF18B500
--     user123  -> 6AD14BA9986E3615423DFCA256D04E3F
IF NOT EXISTS (SELECT 1 FROM T_User)
BEGIN
    INSERT INTO T_User (userName, userPassword, userPurview) VALUES
    (N'admin', N'0192023A7BBD73250516F069DF18B500', N'管理员'),
    (N'user01', N'6AD14BA9986E3615423DFCA256D04E3F', N'普通用户');
END
GO

-- 3.2 图书类型数据（8 种）
IF NOT EXISTS (SELECT 1 FROM T_BookType)
BEGIN
    INSERT INTO T_BookType (typeID, typeName) VALUES
    (N'T01', N'计算机类'),
    (N'T02', N'文学类'),
    (N'T03', N'经济管理类'),
    (N'T04', N'历史类'),
    (N'T05', N'哲学类'),
    (N'T06', N'自然科学类'),
    (N'T07', N'外语类'),
    (N'T08', N'艺术类');
END
GO

-- 3.3 图书数据（25 本，覆盖各类型）
IF NOT EXISTS (SELECT 1 FROM T_Book)
BEGIN
    INSERT INTO T_Book (bookID, bookName, author, publisher, publishDate, ISBN, price, typeID, totalCount, availableCount) VALUES
    -- 计算机类
    (N'B001', N'C# 入门经典', N'Karli Watson', N'清华大学出版社', '2020-03-15', '9787302533918', 89.00, N'T01', 5, 3),
    (N'B002', N'数据结构与算法', N'严蔚敏', N'人民邮电出版社', '2019-08-01', '9787115415592', 69.00, N'T01', 3, 2),
    (N'B003', N'深入理解计算机系统', N'Randal E. Bryant', N'机械工业出版社', '2016-11-01', '9787111544937', 139.00, N'T01', 4, 4),
    (N'B004', N'算法导论', N'Thomas H. Cormen', N'机械工业出版社', '2013-01-01', '9787111407010', 128.00, N'T01', 3, 3),
    -- 文学类
    (N'B005', N'红楼梦', N'曹雪芹', N'人民文学出版社', '2018-05-20', '9787020002207', 59.00, N'T02', 4, 2),
    (N'B006', N'百年孤独', N'加西亚·马尔克斯', N'南海出版公司', '2017-06-10', '9787544291170', 39.50, N'T02', 2, 1),
    (N'B007', N'围城', N'钱钟书', N'人民文学出版社', '2019-01-15', '9787020024759', 36.00, N'T02', 3, 3),
    (N'B008', N'活着', N'余华', N'作家出版社', '2020-07-01', '9787506365437', 28.00, N'T02', 5, 4),
    (N'B009', N'三体', N'刘慈欣', N'重庆出版社', '2008-01-01', '9787536692930', 93.00, N'T02', 4, 3),
    -- 经济管理类
    (N'B010', N'经济学原理', N'曼昆', N'北京大学出版社', '2021-01-15', '9787301318025', 128.00, N'T03', 3, 2),
    (N'B011', N'国富论', N'亚当·斯密', N'商务印书馆', '2015-06-01', '9787100101174', 88.00, N'T03', 2, 2),
    (N'B012', N'卓有成效的管理者', N'彼得·德鲁克', N'机械工业出版社', '2019-04-20', '9787111617990', 49.00, N'T03', 3, 3),
    -- 历史类
    (N'B013', N'史记', N'司马迁', N'中华书局', '2016-04-01', '9787101001042', 98.00, N'T04', 2, 1),
    (N'B014', N'全球通史', N'斯塔夫里阿诺斯', N'北京大学出版社', '2020-09-01', '9787301204689', 168.00, N'T04', 3, 3),
    (N'B015', N'万历十五年', N'黄仁宇', N'中华书局', '2018-03-10', '9787101009826', 42.00, N'T04', 4, 3),
    -- 哲学类
    (N'B016', N'苏菲的世界', N'乔斯坦·贾德', N'作家出版社', '2017-08-20', '9787506365369', 35.00, N'T05', 3, 2),
    (N'B017', N'中国哲学简史', N'冯友兰', N'北京大学出版社', '2013-01-01', '9787301215692', 58.00, N'T05', 2, 2),
    -- 自然科学类
    (N'B018', N'时间简史', N'史蒂芬·霍金', N'湖南科学技术出版社', '2018-05-01', '9787535732309', 45.00, N'T06', 3, 2),
    (N'B019', N'物种起源', N'达尔文', N'商务印书馆', '2015-01-01', '9787100105776', 49.00, N'T06', 2, 2),
    (N'B020', N'上帝掷骰子吗', N'曹天元', N'北京联合出版公司', '2019-06-15', '9787559630612', 59.80, N'T06', 3, 3),
    -- 外语类
    (N'B021', N'新概念英语2', N'亚历山大', N'外语教学与研究出版社', '2008-06-01', '9787560013473', 38.90, N'T07', 5, 4),
    (N'B022', N'牛津高阶英汉双解词典', N'A.S. Hornby', N'商务印书馆', '2018-03-01', '9787100158606', 169.00, N'T07', 2, 2),
    -- 艺术类
    (N'B023', N'艺术的故事', N'贡布里希', N'广西美术出版社', '2014-04-01', '9787549404650', 280.00, N'T08', 2, 1),
    (N'B024', N'写给大家看的设计书', N'Robin Williams', N'人民邮电出版社', '2016-01-01', '9787115404930', 59.00, N'T08', 3, 3),
    (N'B025', N'摄影笔记', N'宁思潇潇', N'人民邮电出版社', '2020-10-01', '9787115545381', 79.00, N'T08', 2, 2);
END
GO

-- 3.4 读者数据（12 人）
IF NOT EXISTS (SELECT 1 FROM T_Reader)
BEGIN
    INSERT INTO T_Reader (readerID, readerName, readerSex, phone, department, registerDate) VALUES
    (N'R001', N'张三', N'男', N'13800138001', N'计算机学院', '2024-09-01'),
    (N'R002', N'李四', N'女', N'13800138002', N'文学院', '2024-09-05'),
    (N'R003', N'王五', N'男', N'13800138003', N'经济管理学院', '2024-10-10'),
    (N'R004', N'赵六', N'女', N'13900139001', N'历史学院', '2024-10-15'),
    (N'R005', N'孙七', N'男', N'13900139002', N'计算机学院', '2024-11-01'),
    (N'R006', N'周八', N'女', N'13900139003', N'外国语学院', '2024-11-20'),
    (N'R007', N'吴九', N'男', N'13700137001', N'物理学院', '2024-12-01'),
    (N'R008', N'郑十', N'女', N'13700137002', N'艺术学院', '2025-01-05'),
    (N'R009', N'陈小明', N'男', N'13600136001', N'哲学学院', '2025-02-15'),
    (N'R010', N'林小红', N'女', N'13600136002', N'文学院', '2025-03-01'),
    (N'R011', N'黄大伟', N'男', N'13500135001', N'经济管理学院', '2025-03-20'),
    (N'R012', N'刘芳', N'女', N'13500135002', N'计算机学院', '2025-04-10');
END
GO

-- 3.5 借阅数据（混合：借出中、已归还、逾期，假设当前日期为 2025-07-14）
IF NOT EXISTS (SELECT 1 FROM T_Borrow)
BEGIN
    INSERT INTO T_Borrow (readerID, bookID, borrowDate, dueDate, returnDate, status) VALUES
    -- 已归还（正常）
    (N'R001', N'B004', '2025-04-01', '2025-05-16', '2025-04-20', N'已还'),
    (N'R002', N'B007', '2025-04-10', '2025-05-25', '2025-05-15', N'已还'),
    (N'R003', N'B012', '2025-05-05', '2025-06-19', '2025-06-10', N'已还'),
    (N'R005', N'B018', '2025-05-20', '2025-07-04', '2025-06-25', N'已还'),
    (N'R006', N'B021', '2025-06-01', '2025-07-16', '2025-07-10', N'已还'),
    -- 借出中（正常未逾期）
    (N'R001', N'B001', '2025-06-15', '2025-07-30', NULL, N'借出'),
    (N'R004', N'B005', '2025-06-20', '2025-08-04', NULL, N'借出'),
    (N'R007', N'B010', '2025-06-25', '2025-08-09', NULL, N'借出'),
    (N'R008', N'B023', '2025-07-01', '2025-08-15', NULL, N'借出'),
    (N'R009', N'B016', '2025-07-05', '2025-08-19', NULL, N'借出'),
    (N'R010', N'B008', '2025-07-10', '2025-08-24', NULL, N'借出'),
    -- 借出中（已逾期）
    (N'R002', N'B006', '2025-05-20', '2025-07-04', NULL, N'借出'),
    (N'R003', N'B013', '2025-05-10', '2025-06-24', NULL, N'借出'),
    (N'R005', N'B001', '2025-05-01', '2025-06-15', NULL, N'借出'),
    (N'R011', N'B002', '2025-04-15', '2025-05-30', NULL, N'借出'),
    (N'R012', N'B009', '2025-06-01', '2025-07-16', NULL, N'借出'),
    (N'R006', N'B005', '2025-05-25', '2025-07-09', NULL, N'借出');
END
GO

-- 3.6 罚款数据（逾期未还的借阅记录生成罚款，罚款 = 逾期天数 × 0.3 元）
IF NOT EXISTS (SELECT 1 FROM T_Fine)
BEGIN
    INSERT INTO T_Fine (readerID, bookID, borrowID, overdueDays, fineAmount, fineStatus, createDate) VALUES
    (N'R002', N'B006', (SELECT borrowID FROM T_Borrow WHERE readerID=N'R002' AND bookID=N'B006' AND status=N'借出'), 10, 3.00, N'未缴', '2025-07-14'),
    (N'R003', N'B013', (SELECT borrowID FROM T_Borrow WHERE readerID=N'R003' AND bookID=N'B013' AND status=N'借出'), 20, 6.00, N'未缴', '2025-07-14'),
    (N'R005', N'B001', (SELECT borrowID FROM T_Borrow WHERE readerID=N'R005' AND bookID=N'B001' AND status=N'借出'), 29, 8.70, N'未缴', '2025-07-14'),
    (N'R011', N'B002', (SELECT borrowID FROM T_Borrow WHERE readerID=N'R011' AND bookID=N'B002' AND status=N'借出'), 45, 13.50, N'未缴', '2025-07-14'),
    (N'R006', N'B005', (SELECT borrowID FROM T_Borrow WHERE readerID=N'R006' AND bookID=N'B005' AND status=N'借出'), 5, 1.50, N'未缴', '2025-07-14');
    -- 已缴罚款
    INSERT INTO T_Fine (readerID, bookID, borrowID, overdueDays, fineAmount, fineStatus, createDate, payDate) VALUES
    (N'R001', N'B004', (SELECT borrowID FROM T_Borrow WHERE readerID=N'R001' AND bookID=N'B004' AND status=N'已还'), 3, 0.90, N'已缴', '2025-04-23', '2025-04-25');
END
GO

-- 3.7 预约数据（各状态混合：排队中/待取书/已完成/已取消）
IF NOT EXISTS (SELECT 1 FROM T_Reservation)
BEGIN
    INSERT INTO T_Reservation (readerID, bookID, reserveDate, expireDate, notifyDate, status) VALUES
    -- 排队中
    (N'R003', N'B003', '2025-07-10', '2025-07-13', NULL, N'排队中'),
    (N'R004', N'B001', '2025-07-12', '2025-07-15', NULL, N'排队中'),
    (N'R009', N'B014', '2025-07-13', '2025-07-16', NULL, N'排队中'),
    -- 待取书（已通知）
    (N'R005', N'B020', '2025-07-08', '2025-07-11', '2025-07-11', N'待取书'),
    (N'R007', N'B025', '2025-07-09', '2025-07-12', '2025-07-12', N'待取书'),
    (N'R010', N'B015', '2025-07-06', '2025-07-09', '2025-07-09', N'待取书'),
    -- 已完成
    (N'R001', N'B011', '2025-07-01', '2025-07-04', '2025-07-03', N'已完成'),
    (N'R002', N'B017', '2025-06-25', '2025-06-28', '2025-06-28', N'已完成'),
    (N'R006', N'B022', '2025-06-20', '2025-06-23', '2025-06-22', N'已完成'),
    -- 已取消
    (N'R008', N'B019', '2025-07-05', '2025-07-08', NULL, N'已取消'),
    (N'R011', N'B024', '2025-06-28', '2025-07-01', NULL, N'已取消');

    -- 更新取消原因和完成日期
    UPDATE T_Reservation SET cancelReason=N'手动取消', cancelDate='2025-07-07' WHERE readerID=N'R008' AND bookID=N'B019';
    UPDATE T_Reservation SET cancelReason=N'预约超时', cancelDate='2025-07-02' WHERE readerID=N'R011' AND bookID=N'B024';
    UPDATE T_Reservation SET completeDate='2025-07-04' WHERE readerID=N'R001' AND bookID=N'B011';
    UPDATE T_Reservation SET completeDate='2025-06-29' WHERE readerID=N'R002' AND bookID=N'B017';
    UPDATE T_Reservation SET completeDate='2025-06-23' WHERE readerID=N'R006' AND bookID=N'B022';
END
GO

-- 3.8 操作日志（30 条历史操作记录）
IF NOT EXISTS (SELECT 1 FROM T_OperateLog)
BEGIN
    INSERT INTO T_OperateLog (userName, operateTime, operateType, operateContent, detail) VALUES
    (N'admin', '2025-07-14 09:00:00', N'登录', N'系统', N'用户 admin 登录系统'),
    (N'admin', '2025-07-14 09:05:00', N'新增', N'图书：B025', N'新增图书《摄影笔记》'),
    (N'admin', '2025-07-14 09:10:00', N'新增', N'读者：R012', N'新增读者刘芳'),
    (N'user01', '2025-07-14 09:30:00', N'登录', N'系统', N'用户 user01 登录系统'),
    (N'admin', '2025-07-13 14:00:00', N'借书', N'读者：R010，图书：B008', N'借出《活着》'),
    (N'admin', '2025-07-13 14:05:00', N'预约', N'读者：R009，图书：B014', N'预约《全球通史》'),
    (N'user01', '2025-07-13 15:00:00', N'还书', N'读者：R006，图书：B021', N'归还《新概念英语2》'),
    (N'admin', '2025-07-12 10:00:00', N'修改', N'图书：B001', N'修改价格从79.00改为89.00'),
    (N'admin', '2025-07-12 10:30:00', N'新增', N'读者：R011', N'新增读者黄大伟'),
    (N'admin', '2025-07-11 16:00:00', N'缴费', N'读者：R001', N'缴纳罚款0.90元'),
    (N'admin', '2025-07-11 09:00:00', N'通知取书', N'预约：R005-B020', N'通知读者孙七取书《上帝掷骰子吗》'),
    (N'admin', '2025-07-10 11:00:00', N'借书', N'读者：R009，图书：B016', N'借出《苏菲的世界》'),
    (N'admin', '2025-07-10 10:00:00', N'取消预约', N'预约：R008-B019', N'手动取消预约'),
    (N'user01', '2025-07-09 08:30:00', N'登录', N'系统', N'用户 user01 登录系统'),
    (N'admin', '2025-07-08 14:00:00', N'借书', N'读者：R008，图书：B023', N'借出《艺术的故事》'),
    (N'admin', '2025-07-08 14:10:00', N'删除', N'图书：B026', N'删除重复图书记录'),
    (N'admin', '2025-07-07 09:00:00', N'新增', N'图书类型：T08', N'新增艺术类'),
    (N'admin', '2025-07-07 09:10:00', N'新增', N'图书：B023', N'新增图书《艺术的故事》'),
    (N'admin', '2025-07-07 09:20:00', N'新增', N'图书：B024', N'新增图书《写给大家看的设计书》'),
    (N'admin', '2025-07-06 15:00:00', N'借书', N'读者：R007，图书：B010', N'借出《经济学原理》'),
    (N'admin', '2025-07-06 15:10:00', N'预约', N'读者：R010，图书：B015', N'预约《万历十五年》'),
    (N'admin', '2025-07-05 10:00:00', N'借书', N'读者：R004，图书：B005', N'借出《红楼梦》'),
    (N'admin', '2025-07-05 10:05:00', N'借书', N'读者：R009，图书：B016', N'借出《苏菲的世界》'),
    (N'admin', '2025-07-04 16:00:00', N'修改', N'读者：R010', N'修改联系电话'),
    (N'admin', '2025-07-03 09:00:00', N'新增', N'读者：R010', N'新增读者林小红'),
    (N'admin', '2025-07-01 10:00:00', N'借书', N'读者：R008，图书：B023', N'借出《艺术的故事》'),
    (N'admin', '2025-07-01 10:10:00', N'预约', N'读者：R001，图书：B011', N'预约《国富论》'),
    (N'admin', '2025-06-30 14:00:00', N'还书', N'读者：R005，图书：B018', N'归还《时间简史》'),
    (N'admin', '2025-06-28 11:00:00', N'新增', N'图书：B021', N'新增图书《新概念英语2》'),
    (N'admin', '2025-06-28 11:10:00', N'新增', N'图书：B022', N'新增图书《牛津高阶英汉双解词典》');
END
GO

PRINT N'LibraryDB（cs-5）数据库初始化完成！';
PRINT N'图书：25 本 | 读者：12 人 | 借阅：17 条 | 罚款：6 条 | 预约：11 条 | 操作日志：30 条';
GO