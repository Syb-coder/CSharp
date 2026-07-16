-- ============================================================
-- 校园易购信息管理系统（CampusMart）- 数据库初始化脚本
-- 目标数据库：SQL Server 2019+（Windows 认证）
-- 数据库名称：CampusMart
-- 执行方式：在 SSMS 中以 Windows 认证连接后执行本脚本
--           （请勿使用 sqlcmd 直接执行 UTF-8 脚本，避免编码问题）
-- 设计要点：
--   1. 全部表采用 int IDENTITY 自增主键（与 cs-2 字符串主键差异化）
--   2. 商品类别为扁平结构（无 parentCategoryID）
--   3. 订单表新增 orderNo（可读订单号）与 userID（操作员外键）
--   4. 订单明细小计字段命名为 subtotal（cs-2 为 totalPrice）
--   5. 支付状态取值为"未支付/已支付"（cs-2 为"待支付/已支付"）
-- ============================================================

-- 1. 创建数据库
IF DB_ID('CampusMart') IS NULL
BEGIN
    CREATE DATABASE CampusMart;
END
GO

USE CampusMart;
GO

-- 2. 创建数据表

-- 2.1 系统用户表（5 字段：自增主键 + 登录名 + 密码哈希 + 真实姓名 + 角色）
IF OBJECT_ID('tbl_User', 'U') IS NULL
BEGIN
    CREATE TABLE tbl_User (
        userID     INT            IDENTITY(1,1),
        loginName  NVARCHAR(20)   NOT NULL,          -- 登录用户名（唯一）
        password   NVARCHAR(64)   NOT NULL,          -- SHA-256 哈希值（64 位十六进制字符串）
        realName   NVARCHAR(20)   NULL,              -- 真实姓名
        role       NVARCHAR(10)   NOT NULL,          -- 角色：管理员 / 操作员
        CONSTRAINT PK_tbl_User PRIMARY KEY (userID),
        CONSTRAINT UQ_tbl_User_loginName UNIQUE (loginName),
        CONSTRAINT CK_tbl_User_role CHECK (role IN (N'管理员', N'操作员'))
    );
END
GO

-- 2.2 商品类别表（扁平结构，无父子层级）
IF OBJECT_ID('tbl_Category', 'U') IS NULL
BEGIN
    CREATE TABLE tbl_Category (
        categoryID    INT            IDENTITY(1,1),
        categoryName  NVARCHAR(20)   NOT NULL,        -- 类别名称（必填）
        categoryDesc  NVARCHAR(100)  NULL,            -- 类别描述（选填）
        addTime       DATETIME       NOT NULL DEFAULT GETDATE(),  -- 添加时间，系统自动填充
        CONSTRAINT PK_tbl_Category PRIMARY KEY (categoryID)
    );
END
GO

-- 2.3 供货商信息表（含法人代表、注册日期等工商信息）
IF OBJECT_ID('tbl_Supplier', 'U') IS NULL
BEGIN
    CREATE TABLE tbl_Supplier (
        supplierID     INT            IDENTITY(1,1),
        supplierName   NVARCHAR(50)   NOT NULL,        -- 供货商名称（必填）
        legalPerson    NVARCHAR(20)   NULL,            -- 法人代表
        registerDate   DATE           NULL,            -- 注册日期
        contactPerson  NVARCHAR(20)   NULL,            -- 联系人
        phone          NVARCHAR(15)   NULL,            -- 联系电话
        address        NVARCHAR(100)  NULL,            -- 地址
        CONSTRAINT PK_tbl_Supplier PRIMARY KEY (supplierID)
    );
END
GO

-- 2.4 商品信息表（无 specification 字段；字段名 stockQty / produceDate 与 cs-2 差异化）
IF OBJECT_ID('tbl_Product', 'U') IS NULL
BEGIN
    CREATE TABLE tbl_Product (
        productID     INT            IDENTITY(1,1),
        productName   NVARCHAR(50)   NOT NULL,         -- 商品名称（必填）
        categoryID    INT            NOT NULL,         -- 类别外键
        unitPrice     DECIMAL(10, 2) NOT NULL,         -- 单价（必填，>0）
        origin        NVARCHAR(50)   NULL,             -- 产地
        produceDate   DATE           NULL,             -- 生产日期
        stockQty      INT            NOT NULL,         -- 库存数量（>=0，由订单业务逻辑维护）
        supplierID    INT            NOT NULL,         -- 供货商外键
        CONSTRAINT PK_tbl_Product PRIMARY KEY (productID),
        CONSTRAINT FK_tbl_Product_Category FOREIGN KEY (categoryID)
            REFERENCES tbl_Category(categoryID),
        CONSTRAINT FK_tbl_Product_Supplier FOREIGN KEY (supplierID)
            REFERENCES tbl_Supplier(supplierID),
        CONSTRAINT CK_tbl_Product_price CHECK (unitPrice > 0),
        CONSTRAINT CK_tbl_Product_stock CHECK (stockQty >= 0)
    );
END
GO

-- 2.5 订单主表（11 字段：新增 orderNo 可读订单号 + userID 操作员外键）
IF OBJECT_ID('tbl_Order', 'U') IS NULL
BEGIN
    CREATE TABLE tbl_Order (
        orderID           INT            IDENTITY(1,1),
        orderNo           NVARCHAR(20)   NOT NULL,     -- 可读订单号：ORD + yyyyMMdd + 3位流水号
        receiverName      NVARCHAR(20)   NOT NULL,     -- 收货人姓名（必填）
        receiverPhone     NVARCHAR(11)   NOT NULL,     -- 收货人手机号（必填，11 位纯数字）
        receiverAddress   NVARCHAR(200)  NULL,         -- 收货地址
        paymentMethod     NVARCHAR(10)   NULL,         -- 支付方式：现金/微信/支付宝
        paymentStatus     NVARCHAR(10)   NOT NULL DEFAULT N'未支付',  -- 未支付/已支付
        paymentTime       DATETIME       NULL,         -- 支付时间（状态变更时填充）
        totalAmount       DECIMAL(10, 2) NOT NULL DEFAULT 0,          -- 订单总金额（明细小计之和）
        orderDate         DATETIME       NOT NULL DEFAULT GETDATE(),  -- 下单日期
        userID            INT            NOT NULL,     -- 操作员外键（创建订单的用户）
        CONSTRAINT PK_tbl_Order PRIMARY KEY (orderID),
        CONSTRAINT UQ_tbl_Order_orderNo UNIQUE (orderNo),
        CONSTRAINT FK_tbl_Order_User FOREIGN KEY (userID)
            REFERENCES tbl_User(userID),
        CONSTRAINT CK_tbl_Order_status CHECK (paymentStatus IN (N'未支付', N'已支付')),
        CONSTRAINT CK_tbl_Order_method CHECK (paymentMethod IS NULL OR paymentMethod IN (N'现金', N'微信', N'支付宝')),
        CONSTRAINT CK_tbl_Order_phone CHECK (receiverPhone LIKE '[0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9]')
    );
END
GO

-- 2.6 订单商品明细表（小计字段名为 subtotal；商品名称和单价为下单快照）
IF OBJECT_ID('tbl_OrderItem', 'U') IS NULL
BEGIN
    CREATE TABLE tbl_OrderItem (
        itemID      INT            IDENTITY(1,1),
        orderID     INT            NOT NULL,            -- 订单外键
        productID   INT            NOT NULL,            -- 商品外键
        productName NVARCHAR(50)   NOT NULL,            -- 商品名称快照（下单时从商品表读取）
        unitPrice   DECIMAL(10, 2) NOT NULL,            -- 单价快照
        quantity    INT            NOT NULL,             -- 购买数量（>0）
        subtotal    DECIMAL(10, 2) NOT NULL,            -- 小计金额 = unitPrice × quantity
        CONSTRAINT PK_tbl_OrderItem PRIMARY KEY (itemID),
        CONSTRAINT FK_tbl_OrderItem_Order FOREIGN KEY (orderID)
            REFERENCES tbl_Order(orderID),
        CONSTRAINT FK_tbl_OrderItem_Product FOREIGN KEY (productID)
            REFERENCES tbl_Product(productID),
        CONSTRAINT CK_tbl_OrderItem_qty CHECK (quantity > 0),
        CONSTRAINT CK_tbl_OrderItem_price CHECK (unitPrice > 0 AND subtotal >= 0)
    );
END
GO

-- 3. 初始化测试数据

-- 3.1 用户数据（密码 123456 的 SHA-256 哈希值，由 SecurityUtil.ComputeHash 生成）
--     123456 -> 8D969EEF6ECAD3C29A3A629280E686CF0C3F5D5A86AFF3CA12020C923ADC6C92
IF NOT EXISTS (SELECT 1 FROM tbl_User)
BEGIN
    INSERT INTO tbl_User (loginName, password, realName, role) VALUES
    (N'admin',  N'8D969EEF6ECAD3C29A3A629280E686CF0C3F5D5A86AFF3CA12020C923ADC6C92', N'系统管理员', N'管理员'),
    (N'op01',   N'8D969EEF6ECAD3C29A3A629280E686CF0C3F5D5A86AFF3CA12020C923ADC6C92', N'值班操作员', N'操作员');
END
GO

-- 3.2 商品类别数据（扁平结构，无父子层级）
IF NOT EXISTS (SELECT 1 FROM tbl_Category)
BEGIN
    INSERT INTO tbl_Category (categoryName, categoryDesc, addTime) VALUES
    (N'食品类',   N'各类食品、零食、饮料',         GETDATE()),
    (N'文具类',   N'学生文具、办公用品',           GETDATE()),
    (N'日用品类', N'日常生活用品',                 GETDATE()),
    (N'数码类',   N'数码产品及配件',               GETDATE());
END
GO

-- 3.3 供货商数据（含法人代表、注册日期）
IF NOT EXISTS (SELECT 1 FROM tbl_Supplier)
BEGIN
    INSERT INTO tbl_Supplier (supplierName, legalPerson, registerDate, contactPerson, phone, address) VALUES
    (N'校园超市供货商',     N'张三', '2020-03-15', N'张经理', N'13800138001', N'大庆市高新区学府街1号'),
    (N'晨光文具有限公司',   N'李四', '2018-06-20', N'李经理', N'13900139002', N'上海市浦东新区张江路2号'),
    (N'康师傅食品有限公司', N'王五', '2015-09-10', N'王经理', N'13700137003', N'天津市滨海新区新华路3号');
END
GO

-- 3.4 商品数据（无 specification 字段；含产地、生产日期、库存）
IF NOT EXISTS (SELECT 1 FROM tbl_Product)
BEGIN
    -- categoryID 与 supplierID 通过子查询获取，避免依赖自增种子的具体值
    INSERT INTO tbl_Product (productName, categoryID, unitPrice, origin, produceDate, stockQty, supplierID)
    SELECT N'康师傅红烧牛肉面', c.categoryID, 4.50, N'天津市',   '2026-06-01', 120, s.supplierID
    FROM tbl_Category c, tbl_Supplier s
    WHERE c.categoryName = N'食品类' AND s.supplierName = N'康师傅食品有限公司'
    UNION ALL
    SELECT N'可口可乐', c.categoryID, 3.00, N'上海市', '2026-06-15', 80, s.supplierID
    FROM tbl_Category c, tbl_Supplier s
    WHERE c.categoryName = N'食品类' AND s.supplierName = N'校园超市供货商'
    UNION ALL
    SELECT N'晨光中性笔', c.categoryID, 2.00, N'上海市', '2026-05-20', 200, s.supplierID
    FROM tbl_Category c, tbl_Supplier s
    WHERE c.categoryName = N'文具类' AND s.supplierName = N'晨光文具有限公司'
    UNION ALL
    SELECT N'清风抽纸', c.categoryID, 5.50, N'东莞市', '2026-06-10', 60, s.supplierID
    FROM tbl_Category c, tbl_Supplier s
    WHERE c.categoryName = N'日用品类' AND s.supplierName = N'校园超市供货商';
END
GO

-- 3.5 订单数据（含订单主表和明细表，订单号采用 ORD + yyyyMMdd + 3位流水号格式）
IF NOT EXISTS (SELECT 1 FROM tbl_Order)
BEGIN
    DECLARE @orderID1 INT, @orderID2 INT, @orderID3 INT;
    DECLARE @adminID INT, @opID INT;
    SELECT @adminID = userID FROM tbl_User WHERE loginName = N'admin';
    SELECT @opID = userID FROM tbl_User WHERE loginName = N'op01';

    -- 订单1：2天前，已支付，现金，包含2件商品（牛肉面×2 + 可口可乐×1）
    INSERT INTO tbl_Order (orderNo, receiverName, receiverPhone, receiverAddress, paymentMethod, paymentStatus, paymentTime, totalAmount, orderDate, userID)
    VALUES (N'ORD' + CONVERT(NVARCHAR(8), DATEADD(DAY, -2, GETDATE()), 112) + N'001',
            N'张同学', N'13800000001', N'学生公寓1号楼101室',
            N'现金', N'已支付', DATEADD(DAY, -2, GETDATE()), 13.00, DATEADD(DAY, -2, GETDATE()), @adminID);
    SET @orderID1 = SCOPE_IDENTITY();
    INSERT INTO tbl_OrderItem (orderID, productID, productName, unitPrice, quantity, subtotal)
    SELECT @orderID1, productID, productName, unitPrice, 2, unitPrice * 2 FROM tbl_Product WHERE productName = N'康师傅红烧牛肉面'
    UNION ALL
    SELECT @orderID1, productID, productName, unitPrice, 1, unitPrice * 1 FROM tbl_Product WHERE productName = N'可口可乐';

    -- 订单2：1天前，已支付，微信，包含1件商品（可口可乐×2）
    INSERT INTO tbl_Order (orderNo, receiverName, receiverPhone, receiverAddress, paymentMethod, paymentStatus, paymentTime, totalAmount, orderDate, userID)
    VALUES (N'ORD' + CONVERT(NVARCHAR(8), DATEADD(DAY, -1, GETDATE()), 112) + N'001',
            N'李同学', N'13800000002', N'学生公寓2号楼203室',
            N'微信', N'已支付', DATEADD(DAY, -1, GETDATE()), 6.00, DATEADD(DAY, -1, GETDATE()), @opID);
    SET @orderID2 = SCOPE_IDENTITY();
    INSERT INTO tbl_OrderItem (orderID, productID, productName, unitPrice, quantity, subtotal)
    SELECT @orderID2, productID, productName, unitPrice, 2, unitPrice * 2 FROM tbl_Product WHERE productName = N'可口可乐';

    -- 订单3：今天，未支付，支付宝，包含1件商品（晨光中性笔×5）
    INSERT INTO tbl_Order (orderNo, receiverName, receiverPhone, receiverAddress, paymentMethod, paymentStatus, paymentTime, totalAmount, orderDate, userID)
    VALUES (N'ORD' + CONVERT(NVARCHAR(8), GETDATE(), 112) + N'001',
            N'王同学', N'13800000003', N'学生公寓3号楼305室',
            N'支付宝', N'未支付', NULL, 10.00, GETDATE(), @opID);
    SET @orderID3 = SCOPE_IDENTITY();
    INSERT INTO tbl_OrderItem (orderID, productID, productName, unitPrice, quantity, subtotal)
    SELECT @orderID3, productID, productName, unitPrice, 5, unitPrice * 5 FROM tbl_Product WHERE productName = N'晨光中性笔';
END
GO

-- 4. 验证主键完整性（执行后可在 SSMS 消息栏查看验证结果）
SELECT t.name AS table_name,
       COALESCE(i.name, N'(无主键)') AS pk_name
FROM sys.tables t
LEFT JOIN sys.indexes i
    ON t.object_id = i.object_id AND i.is_primary_key = 1
ORDER BY t.name;

PRINT N'CampusMart 数据库初始化完成';
GO
