-- ============================================================
-- 校园易购信息管理系统 - 数据库初始化脚本
-- 目标数据库：SQL Server（Windows 认证）
-- 执行方式：在 SSMS 中以 Windows 认证连接后执行本脚本
-- ============================================================

-- 1. 创建数据库
IF DB_ID('CampusShop') IS NULL
BEGIN
    CREATE DATABASE CampusShop;
END
GO

USE CampusShop;
GO

-- 2. 创建数据表

-- 2.1 系统用户表
IF OBJECT_ID('tbl_User', 'U') IS NULL
BEGIN
    CREATE TABLE tbl_User (
        userName     NVARCHAR(16)  NOT NULL,
        userPassword NVARCHAR(64)  NOT NULL,  -- SHA-256 哈希值（64位十六进制字符串）
        userPurview  NVARCHAR(16)  NOT NULL,  -- 取值：管理员 / 普通用户
        CONSTRAINT PK_tbl_User PRIMARY KEY (userName),
        CONSTRAINT CK_tbl_User_purview CHECK (userPurview IN (N'管理员', N'普通用户'))
    );
END
GO

-- 2.2 商品类别表（支持树状层级结构）
IF OBJECT_ID('tbl_Category', 'U') IS NULL
BEGIN
    CREATE TABLE tbl_Category (
        categoryID       NVARCHAR(10)  NOT NULL,
        categoryName     NVARCHAR(20)  NOT NULL,
        parentCategoryID NVARCHAR(10)  NULL,  -- 外键引用本表 categoryID，NULL 表示根类别
        categoryDesc     NVARCHAR(100) NULL,  -- 类别描述
        addTime          DATETIME      NULL DEFAULT GETDATE(),  -- 添加时间
        CONSTRAINT PK_tbl_Category PRIMARY KEY (categoryID),
        CONSTRAINT FK_tbl_Category_Parent FOREIGN KEY (parentCategoryID)
            REFERENCES tbl_Category(categoryID)
    );
END
GO

-- 2.3 供货商信息表
IF OBJECT_ID('tbl_Supplier', 'U') IS NULL
BEGIN
    CREATE TABLE tbl_Supplier (
        supplierID     NVARCHAR(20)  NOT NULL,
        supplierName   NVARCHAR(50)  NOT NULL,
        contactPerson  NVARCHAR(20)  NULL,
        phone          NVARCHAR(15)  NULL,
        address        NVARCHAR(100) NULL,
        legalPerson    NVARCHAR(20)  NULL,   -- 法人代表
        registerDate   DATE          NULL,   -- 注册日期
        CONSTRAINT PK_tbl_Supplier PRIMARY KEY (supplierID)
    );
END
GO

-- 2.4 商品信息表
IF OBJECT_ID('tbl_Product', 'U') IS NULL
BEGIN
    CREATE TABLE tbl_Product (
        productID       NVARCHAR(20)   NOT NULL,
        productName     NVARCHAR(50)   NOT NULL,
        specification   NVARCHAR(30)   NULL,
        unitPrice       DECIMAL(10, 2) NOT NULL,
        stockQuantity   INT            NOT NULL,  -- 冗余字段，由售卖业务逻辑维护
        categoryID      NVARCHAR(10)   NOT NULL,
        supplierID      NVARCHAR(20)   NOT NULL,
        origin          NVARCHAR(50)   NULL,   -- 产地
        productionDate  DATE           NULL,   -- 生产日期
        CONSTRAINT PK_tbl_Product PRIMARY KEY (productID),
        CONSTRAINT FK_tbl_Product_Category FOREIGN KEY (categoryID)
            REFERENCES tbl_Category(categoryID),
        CONSTRAINT FK_tbl_Product_Supplier FOREIGN KEY (supplierID)
            REFERENCES tbl_Supplier(supplierID),
        CONSTRAINT CK_tbl_Product_price CHECK (unitPrice > 0),
        CONSTRAINT CK_tbl_Product_stock CHECK (stockQuantity >= 0)
    );
END
GO

-- 2.5 订单主表（由原 tbl_Sale 重构而来，支持一笔订单包含多个商品）
IF OBJECT_ID('tbl_Order', 'U') IS NULL
BEGIN
    CREATE TABLE tbl_Order (
        orderID         INT            IDENTITY(1,1),
        orderDate       DATE           NOT NULL,           -- 下单日期
        paymentMethod   NVARCHAR(20)   NULL,               -- 支付方式（现金/微信/支付宝等）
        paymentTime     DATETIME       NULL,               -- 支付时间
        paymentStatus   NVARCHAR(10)   NOT NULL DEFAULT N'待支付',  -- 支付状态：待支付/已支付
        receiverName    NVARCHAR(20)   NULL,               -- 收货人姓名
        receiverPhone   NVARCHAR(15)   NULL,               -- 收货人手机号
        receiverAddress NVARCHAR(200)  NULL,               -- 收货地址
        totalAmount     DECIMAL(10, 2) NOT NULL DEFAULT 0, -- 订单总金额（所有明细总价之和）
        CONSTRAINT PK_tbl_Order PRIMARY KEY (orderID),
        CONSTRAINT CK_tbl_Order_status CHECK (paymentStatus IN (N'待支付', N'已支付'))
    );
END
GO

-- 2.6 订单商品明细表（一笔订单可包含多条商品明细）
IF OBJECT_ID('tbl_OrderItem', 'U') IS NULL
BEGIN
    CREATE TABLE tbl_OrderItem (
        itemID      INT            IDENTITY(1,1),
        orderID     INT            NOT NULL,
        productID   NVARCHAR(20)   NOT NULL,
        productName NVARCHAR(50)   NOT NULL,               -- 商品名称快照（下单时从商品表读取）
        unitPrice   DECIMAL(10, 2) NOT NULL,               -- 单价快照
        quantity    INT            NOT NULL,                -- 购买数量
        totalPrice  DECIMAL(10, 2) NOT NULL,               -- 总价 = unitPrice × quantity
        CONSTRAINT PK_tbl_OrderItem PRIMARY KEY (itemID),
        CONSTRAINT FK_tbl_OrderItem_Order FOREIGN KEY (orderID)
            REFERENCES tbl_Order(orderID),
        CONSTRAINT FK_tbl_OrderItem_Product FOREIGN KEY (productID)
            REFERENCES tbl_Product(productID),
        CONSTRAINT CK_tbl_OrderItem_qty CHECK (quantity > 0),
        CONSTRAINT CK_tbl_OrderItem_price CHECK (unitPrice > 0 AND totalPrice >= 0)
    );
END
GO

-- 3. 初始化测试数据

-- 3.1 用户数据（密码的 SHA-256 哈希值，由 PasswordHelper.ComputeHash 生成）
--     123456 -> 8D969EEF6ECAD3C29A3A629280E686CF0C3F5D5A86AFF3CA12020C923ADC6C92
IF NOT EXISTS (SELECT 1 FROM tbl_User)
BEGIN
    INSERT INTO tbl_User (userName, userPassword, userPurview) VALUES
    (N'admin', N'8D969EEF6ECAD3C29A3A629280E686CF0C3F5D5A86AFF3CA12020C923ADC6C92', N'管理员'),
    (N'user01', N'8D969EEF6ECAD3C29A3A629280E686CF0C3F5D5A86AFF3CA12020C923ADC6C92', N'普通用户');
END
GO

-- 3.2 商品类别数据（含父子层级）
IF NOT EXISTS (SELECT 1 FROM tbl_Category)
BEGIN
    INSERT INTO tbl_Category (categoryID, categoryName, parentCategoryID, categoryDesc, addTime) VALUES
    (N'C01',    N'食品类',   NULL, N'各类食品、零食、饮料',           GETDATE()),
    (N'C0101',  N'零食',     N'C01', N'袋装零食、膨化食品',           GETDATE()),
    (N'C0102',  N'饮料',     N'C01', N'瓶装饮料、乳制品',             GETDATE()),
    (N'C02',    N'文具类',   NULL, N'学生文具、办公用品',             GETDATE()),
    (N'C0201',  N'笔类',     N'C02', N'中性笔、圆珠笔、铅笔',         GETDATE()),
    (N'C03',    N'日用品类', NULL, N'日常生活用品',                   GETDATE());
END
GO

-- 3.3 供货商数据
IF NOT EXISTS (SELECT 1 FROM tbl_Supplier)
BEGIN
    INSERT INTO tbl_Supplier (supplierID, supplierName, contactPerson, phone, address, legalPerson, registerDate) VALUES
    (N'S001', N'校园超市供应商',   N'张三', N'13800138001', N'大庆市高新区路1号', N'张三', '2020-03-15'),
    (N'S002', N'晨光文具有限公司', N'李四', N'13900139002', N'上海市浦东新区路2号', N'李四', '2018-06-20'),
    (N'S003', N'康师傅食品有限公司', N'王五', N'13700137003', N'天津市滨海新区路3号', N'王五', '2015-09-10');
END
GO

-- 3.4 商品数据
IF NOT EXISTS (SELECT 1 FROM tbl_Product)
BEGIN
    INSERT INTO tbl_Product (productID, productName, specification, unitPrice, stockQuantity, categoryID, supplierID, origin, productionDate) VALUES
    (N'P0001', N'康师傅红烧牛肉面', N'105g',      4.50,  120, N'C0101', N'S003', N'天津市',   '2026-06-01'),
    (N'P0002', N'可口可乐',         N'330ml',     3.00,   80, N'C0102', N'S001', N'上海市',   '2026-06-15'),
    (N'P0003', N'晨光中性笔',       N'0.5mm黑色', 2.00,  200, N'C0201', N'S002', N'上海市',   '2026-05-20'),
    (N'P0004', N'清风抽纸',         N'200抽',     5.50,   60, N'C03',   N'S001', N'东莞市',   '2026-06-10');
END
GO

-- 3.5 订单数据（含订单主表和明细表）
IF NOT EXISTS (SELECT 1 FROM tbl_Order)
BEGIN
    DECLARE @orderID1 INT, @orderID2 INT, @orderID3 INT;

    -- 订单1：2天前，已支付，现金，包含2件商品（牛肉面×2 + 可口可乐×1）
    INSERT INTO tbl_Order (orderDate, paymentMethod, paymentTime, paymentStatus, receiverName, receiverPhone, receiverAddress, totalAmount) VALUES
    (DATEADD(DAY, -2, CAST(GETDATE() AS DATE)), N'现金', DATEADD(DAY, -2, GETDATE()), N'已支付', N'张同学', N'13800000001', N'学生公寓1号楼101室', 13.00);
    SET @orderID1 = SCOPE_IDENTITY();
    INSERT INTO tbl_OrderItem (orderID, productID, productName, unitPrice, quantity, totalPrice) VALUES
    (@orderID1, N'P0001', N'康师傅红烧牛肉面', 4.50, 2, 9.00),
    (@orderID1, N'P0002', N'可口可乐',         3.00, 1, 3.00);

    -- 订单2：1天前，已支付，微信，包含1件商品（可口可乐×2）
    INSERT INTO tbl_Order (orderDate, paymentMethod, paymentTime, paymentStatus, receiverName, receiverPhone, receiverAddress, totalAmount) VALUES
    (DATEADD(DAY, -1, CAST(GETDATE() AS DATE)), N'微信', DATEADD(DAY, -1, GETDATE()), N'已支付', N'李同学', N'13800000002', N'学生公寓2号楼203室', 6.00);
    SET @orderID2 = SCOPE_IDENTITY();
    INSERT INTO tbl_OrderItem (orderID, productID, productName, unitPrice, quantity, totalPrice) VALUES
    (@orderID2, N'P0002', N'可口可乐', 3.00, 2, 6.00);

    -- 订单3：今天，待支付，支付宝，包含1件商品（晨光中性笔×5）
    INSERT INTO tbl_Order (orderDate, paymentMethod, paymentTime, paymentStatus, receiverName, receiverPhone, receiverAddress, totalAmount) VALUES
    (CAST(GETDATE() AS DATE), N'支付宝', NULL, N'待支付', N'王同学', N'13800000003', N'学生公寓3号楼305室', 10.00);
    SET @orderID3 = SCOPE_IDENTITY();
    INSERT INTO tbl_OrderItem (orderID, productID, productName, unitPrice, quantity, totalPrice) VALUES
    (@orderID3, N'P0003', N'晨光中性笔', 2.00, 5, 10.00);
END
GO

PRINT N'CampusShop 数据库初始化完成';
GO
