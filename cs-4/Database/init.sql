/* ============================================================================
 * 校园易购信息管理系统 - 数据库初始化脚本
 * 项目代号：CampusStore (cs-4)
 *
 * 架构特色：存储过程 + 视图 + 触发器 + 计算列 驱动
 *   - 6 张表
 *   - 15 个存储过程（全部 CRUD 操作）
 *   - 3 个视图（组合查询、列表绑定、库存预警）
 *   - 2 个触发器（库存自动扣减/回补 + 订单总金额维护）
 *   - 3 个计算列（displayNo / amount / totalAmount 逻辑计算列）
 *
 * 执行方式：请在 SSMS 中直接执行本脚本（UTF-8 编码）
 *   说明：sqlcmd 命令行默认按 OEM 代码页（GBK）读取，UTF-8 中文注释会被
 *   乱码解析导致语句边界错误（参见 cs-0/experience.md 经验二）。
 *   如必须用 sqlcmd，请先转为 GBK 编码再执行。
 * ========================================================================== */

USE master;
GO

-- 若已存在则先删除，保证脚本可重复执行
IF DB_ID('CampusStore') IS NOT NULL
BEGIN
    ALTER DATABASE CampusStore SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE CampusStore;
END
GO

CREATE DATABASE CampusStore;
GO

USE CampusStore;
GO

-- 计算列（PERSISTED）要求 QUOTED_IDENTIFIER 和 ANSI_NULLS 均为 ON
-- sqlcmd 默认可能关闭这些选项，显式设置确保计算列表创建成功
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

/* ============================================================================
 * 一、数据表定义
 * ========================================================================== */

-- 1. 系统用户表（管理员/店员）
CREATE TABLE tbl_User (
    userID     INT IDENTITY(1,1) NOT NULL,
    loginName   NVARCHAR(20)  NOT NULL,           -- 登录名
    password    NVARCHAR(64)  NOT NULL,            -- SHA-256 哈希值（64 位十六进制字符串）
    role        NVARCHAR(10)  NOT NULL,            -- 角色：管理员/店员
    CONSTRAINT PK_tbl_User PRIMARY KEY (userID),
    CONSTRAINT UQ_tbl_User_loginName UNIQUE (loginName),
    CONSTRAINT CK_tbl_User_role CHECK (role IN (N'管理员', N'店员'))
);
GO

-- 2. 商品类别表（扁平结构 + 排序号）
CREATE TABLE tbl_Category (
    categoryID   INT IDENTITY(1,1) NOT NULL,
    categoryName NVARCHAR(20)  NOT NULL,           -- 类别名称
    categoryDesc NVARCHAR(100) NULL,               -- 类别描述
    addTime      DATETIME      NOT NULL DEFAULT GETDATE(),  -- 添加时间
    sortOrder    INT           NOT NULL DEFAULT 0,          -- 排序号
    CONSTRAINT PK_tbl_Category PRIMARY KEY (categoryID)
);
GO

-- 3. 供货商信息表
CREATE TABLE tbl_Supplier (
    supplierID    INT IDENTITY(1,1) NOT NULL,
    supplierName  NVARCHAR(50)  NOT NULL,          -- 供货商名称
    legalPerson   NVARCHAR(20)  NULL,              -- 法人代表
    registerDate  DATE          NULL,              -- 注册日期
    contactPerson NVARCHAR(20)  NULL,              -- 联系人
    phone         NVARCHAR(15)  NULL,              -- 联系电话
    address       NVARCHAR(100) NULL,              -- 地址
    CONSTRAINT PK_tbl_Supplier PRIMARY KEY (supplierID)
);
GO

-- 4. 商品信息表
--    displayNo 为 PERSISTED 计算列，格式 SP00001
CREATE TABLE tbl_Product (
    productID     INT IDENTITY(1,1) NOT NULL,
    displayNo     AS ('SP' + RIGHT('00000' + CAST(productID AS NVARCHAR(10)), 5)) PERSISTED,
    productName   NVARCHAR(50)   NOT NULL,         -- 商品名称
    categoryID    INT            NOT NULL,         -- 类别编号（外键）
    unitPrice     DECIMAL(10,2)  NOT NULL,         -- 单价
    origin        NVARCHAR(50)   NULL,             -- 产地
    produceDate   DATE           NULL,             -- 生产日期
    stockQuantity INT            NOT NULL,         -- 库存数量
    supplierID    INT            NOT NULL,         -- 供货商编号（外键）
    CONSTRAINT PK_tbl_Product PRIMARY KEY (productID),
    CONSTRAINT FK_tbl_Product_Category FOREIGN KEY (categoryID) REFERENCES tbl_Category(categoryID),
    CONSTRAINT FK_tbl_Product_Supplier FOREIGN KEY (supplierID) REFERENCES tbl_Supplier(supplierID),
    CONSTRAINT CK_tbl_Product_unitPrice CHECK (unitPrice > 0),
    CONSTRAINT CK_tbl_Product_stock CHECK (stockQuantity >= 0)
);
GO

-- 5. 订单主表
--    totalAmount 为"逻辑计算列"：由触发器维护，非 SQL Server 计算列公式
--    （跨表 SUM 无法用 PERSISTED 计算列表达式实现）
CREATE TABLE tbl_Order (
    orderID          INT IDENTITY(1,1) NOT NULL,
    orderDate        DATETIME       NOT NULL DEFAULT GETDATE(),   -- 下单日期
    paymentMethod    NVARCHAR(10)   NULL,        -- 支付方式（现金/微信/支付宝）
    paymentTime      DATETIME       NULL,        -- 结款时间（未结款为 NULL）
    paymentStatus    NVARCHAR(10)   NOT NULL DEFAULT N'未结款',    -- 结款状态
    receiverName     NVARCHAR(20)   NULL,        -- 收货人姓名
    receiverPhone    NVARCHAR(15)   NULL,        -- 收货人手机号
    receiverAddress  NVARCHAR(200)  NULL,        -- 收货地址
    totalAmount      DECIMAL(10,2)  NOT NULL DEFAULT 0,            -- 总金额（触发器维护）
    CONSTRAINT PK_tbl_Order PRIMARY KEY (orderID),
    CONSTRAINT CK_tbl_Order_status CHECK (paymentStatus IN (N'未结款', N'已结款'))
);
GO

-- 6. 订单商品明细表
--    amount 为 PERSISTED 计算列：unitPrice * quantity，数据库自动维护
CREATE TABLE tbl_OrderItem (
    itemID      INT IDENTITY(1,1) NOT NULL,
    orderID     INT            NOT NULL,          -- 订单编号（外键）
    productID   INT            NOT NULL,          -- 商品编号（外键）
    productName NVARCHAR(50)   NOT NULL,          -- 商品名称快照
    unitPrice   DECIMAL(10,2)  NOT NULL,          -- 单价快照
    quantity    INT            NOT NULL,          -- 购买数量
    amount      AS (unitPrice * quantity) PERSISTED,  -- 小计金额（计算列）
    CONSTRAINT PK_tbl_OrderItem PRIMARY KEY (itemID),
    CONSTRAINT FK_tbl_OrderItem_Order FOREIGN KEY (orderID) REFERENCES tbl_Order(orderID),
    CONSTRAINT FK_tbl_OrderItem_Product FOREIGN KEY (productID) REFERENCES tbl_Product(productID),
    CONSTRAINT CK_tbl_OrderItem_qty CHECK (quantity > 0)
);
GO

/* ============================================================================
 * 二、触发器定义
 * ========================================================================== */

-- 触发器 1：tr_OrderItem_Insert
--   职责：插入订单明细时自动扣减库存，库存不足时回滚；同步更新订单总金额
CREATE TRIGGER tr_OrderItem_Insert
ON tbl_OrderItem AFTER INSERT
AS
BEGIN
    SET NOCOUNT ON;

    -- 库存充足性检查：扣减后库存 < 0 则回滚并抛出错误
    IF EXISTS (
        SELECT 1
        FROM inserted i
        JOIN tbl_Product p ON i.productID = p.productID
        WHERE p.stockQuantity - i.quantity < 0
    )
    BEGIN
        DECLARE @productName NVARCHAR(50);
        SELECT TOP 1 @productName = p.productName
        FROM inserted i
        JOIN tbl_Product p ON i.productID = p.productID
        WHERE p.stockQuantity - i.quantity < 0;

        -- 抛出用户可读错误，应用层捕获后展示
        RAISERROR(N'商品 [%s] 库存不足，无法创建订单', 16, 1, @productName);
        ROLLBACK TRANSACTION;
        RETURN;
    END

    -- 库存扣减
    UPDATE p
        SET p.stockQuantity = p.stockQuantity - i.quantity
    FROM tbl_Product p
    JOIN inserted i ON p.productID = i.productID;

    -- 更新订单总金额（SUM 明细 amount）
    UPDATE o
        SET o.totalAmount = (
            SELECT ISNULL(SUM(amount), 0)
            FROM tbl_OrderItem
            WHERE orderID = i.orderID
        )
    FROM tbl_Order o
    JOIN (SELECT DISTINCT orderID FROM inserted) i
        ON o.orderID = i.orderID;
END;
GO

-- 触发器 2：tr_OrderItem_Delete
--   职责：删除订单明细时自动回补库存；同步更新订单总金额
CREATE TRIGGER tr_OrderItem_Delete
ON tbl_OrderItem AFTER DELETE
AS
BEGIN
    SET NOCOUNT ON;

    -- 回补库存
    UPDATE p
        SET p.stockQuantity = p.stockQuantity + d.quantity
    FROM tbl_Product p
    JOIN deleted d ON p.productID = d.productID;

    -- 更新订单总金额
    UPDATE o
        SET o.totalAmount = (
            SELECT ISNULL(SUM(amount), 0)
            FROM tbl_OrderItem
            WHERE orderID = d.orderID
        )
    FROM tbl_Order o
    JOIN (SELECT DISTINCT orderID FROM deleted) d
        ON o.orderID = d.orderID;
END;
GO

/* ============================================================================
 * 三、视图定义
 * ========================================================================== */

-- 视图 1：v_Product_Detail
--   功能：商品列表（含类别名称、供货商名称），用于商品管理界面绑定
CREATE VIEW v_Product_Detail
AS
SELECT
    p.productID,
    p.displayNo,
    p.productName,
    p.categoryID,
    c.categoryName,
    p.unitPrice,
    p.origin,
    p.produceDate,
    p.stockQuantity,
    p.supplierID,
    s.supplierName
FROM tbl_Product p
LEFT JOIN tbl_Category c ON p.categoryID = c.categoryID
LEFT JOIN tbl_Supplier s ON p.supplierID = s.supplierID;
GO

-- 视图 2：v_Category_List
--   功能：类别列表（含商品数量统计），用于类别管理界面绑定
CREATE VIEW v_Category_List
AS
SELECT
    c.categoryID,
    c.categoryName,
    c.categoryDesc,
    c.addTime,
    c.sortOrder,
    (SELECT COUNT(*) FROM tbl_Product p WHERE p.categoryID = c.categoryID) AS productCount
FROM tbl_Category c;
GO

-- 视图 3：v_LowStock_Product
--   功能：低库存商品（库存 < 20），用于库存预警面板
CREATE VIEW v_LowStock_Product
AS
SELECT
    p.productID,
    p.displayNo,
    p.productName,
    p.stockQuantity,
    c.categoryName,
    s.supplierName,
    p.origin
FROM tbl_Product p
LEFT JOIN tbl_Category c ON p.categoryID = c.categoryID
LEFT JOIN tbl_Supplier s ON p.supplierID = s.supplierID
WHERE p.stockQuantity < 20;
GO

/* ============================================================================
 * 四、存储过程定义
 * ========================================================================== */

/* ----------------------------- 用户相关 ----------------------------------- */

-- sp_User_Validate：验证登录
--   传入：用户名、SHA-256 密码哈希、角色
--   返回：匹配则返回用户记录，否则返回空结果集
CREATE PROCEDURE sp_User_Validate
    @loginName NVARCHAR(20),
    @password  NVARCHAR(64),
    @role      NVARCHAR(10)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT userID, loginName, role
    FROM tbl_User
    WHERE loginName = @loginName
      AND password  = @password
      AND role      = @role;
END;
GO

-- sp_User_Add：添加用户
--   返回值：0 表示用户名已存在，>0 表示新增成功（返回新 userID）
CREATE PROCEDURE sp_User_Add
    @loginName NVARCHAR(20),
    @password  NVARCHAR(64),
    @role      NVARCHAR(10)
AS
BEGIN
    SET NOCOUNT ON;
    -- 用户名唯一性检查
    IF EXISTS (SELECT 1 FROM tbl_User WHERE loginName = @loginName)
    BEGIN
        SELECT 0 AS result;
        RETURN;
    END

    INSERT INTO tbl_User (loginName, password, role)
    VALUES (@loginName, @password, @role);

    SELECT SCOPE_IDENTITY() AS result;
END;
GO

-- sp_User_Update：修改用户（密码 + 角色）
CREATE PROCEDURE sp_User_Update
    @userID    INT,
    @password  NVARCHAR(64),
    @role      NVARCHAR(10)
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE tbl_User
    SET password = @password,
        role     = @role
    WHERE userID = @userID;
    SELECT @@ROWCOUNT AS result;
END;
GO

-- sp_User_Delete：删除用户
CREATE PROCEDURE sp_User_Delete
    @userID INT
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM tbl_User WHERE userID = @userID;
    SELECT @@ROWCOUNT AS result;
END;
GO

/* --------------------------- 商品类别相关 --------------------------------- */

-- sp_Category_Add：添加类别，返回新 categoryID
CREATE PROCEDURE sp_Category_Add
    @categoryName NVARCHAR(20),
    @categoryDesc NVARCHAR(100),
    @sortOrder    INT
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO tbl_Category (categoryName, categoryDesc, sortOrder)
    VALUES (@categoryName, @categoryDesc, @sortOrder);
    SELECT SCOPE_IDENTITY() AS result;
END;
GO

-- sp_Category_Update：修改类别
CREATE PROCEDURE sp_Category_Update
    @categoryID   INT,
    @categoryName NVARCHAR(20),
    @categoryDesc NVARCHAR(100),
    @sortOrder    INT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE tbl_Category
    SET categoryName = @categoryName,
        categoryDesc = @categoryDesc,
        sortOrder    = @sortOrder
    WHERE categoryID = @categoryID;
    SELECT @@ROWCOUNT AS result;
END;
GO

-- sp_Category_Delete：删除类别
--   返回 0 表示有关联商品无法删除，>0 表示删除成功
CREATE PROCEDURE sp_Category_Delete
    @categoryID INT
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM tbl_Product WHERE categoryID = @categoryID)
    BEGIN
        SELECT 0 AS result;
        RETURN;
    END
    DELETE FROM tbl_Category WHERE categoryID = @categoryID;
    SELECT @@ROWCOUNT AS result;
END;
GO

/* ----------------------------- 商品相关 ----------------------------------- */

-- sp_Product_Add：添加商品
CREATE PROCEDURE sp_Product_Add
    @productName NVARCHAR(50),
    @categoryID  INT,
    @unitPrice   DECIMAL(10,2),
    @origin      NVARCHAR(50),
    @produceDate DATE,
    @stockQty    INT,
    @supplierID  INT
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO tbl_Product (productName, categoryID, unitPrice, origin, produceDate, stockQuantity, supplierID)
    VALUES (@productName, @categoryID, @unitPrice, @origin, @produceDate, @stockQty, @supplierID);
    SELECT SCOPE_IDENTITY() AS result;
END;
GO

-- sp_Product_Update：修改商品
CREATE PROCEDURE sp_Product_Update
    @productID   INT,
    @productName NVARCHAR(50),
    @categoryID  INT,
    @unitPrice   DECIMAL(10,2),
    @origin      NVARCHAR(50),
    @produceDate DATE,
    @stockQty    INT,
    @supplierID  INT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE tbl_Product
    SET productName   = @productName,
        categoryID    = @categoryID,
        unitPrice     = @unitPrice,
        origin        = @origin,
        produceDate   = @produceDate,
        stockQuantity = @stockQty,
        supplierID    = @supplierID
    WHERE productID = @productID;
    SELECT @@ROWCOUNT AS result;
END;
GO

-- sp_Product_Delete：删除商品
--   返回 0 表示有订单明细无法删除，>0 表示删除成功
CREATE PROCEDURE sp_Product_Delete
    @productID INT
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM tbl_OrderItem WHERE productID = @productID)
    BEGIN
        SELECT 0 AS result;
        RETURN;
    END
    DELETE FROM tbl_Product WHERE productID = @productID;
    SELECT @@ROWCOUNT AS result;
END;
GO

/* ---------------------------- 供货商相关 ---------------------------------- */

-- sp_Supplier_Add：添加供货商
CREATE PROCEDURE sp_Supplier_Add
    @supplierName  NVARCHAR(50),
    @legalPerson   NVARCHAR(20),
    @registerDate  DATE,
    @contactPerson NVARCHAR(20),
    @phone         NVARCHAR(15),
    @address       NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO tbl_Supplier (supplierName, legalPerson, registerDate, contactPerson, phone, address)
    VALUES (@supplierName, @legalPerson, @registerDate, @contactPerson, @phone, @address);
    SELECT SCOPE_IDENTITY() AS result;
END;
GO

-- sp_Supplier_Update：修改供货商
CREATE PROCEDURE sp_Supplier_Update
    @supplierID    INT,
    @supplierName  NVARCHAR(50),
    @legalPerson   NVARCHAR(20),
    @registerDate  DATE,
    @contactPerson NVARCHAR(20),
    @phone         NVARCHAR(15),
    @address       NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE tbl_Supplier
    SET supplierName  = @supplierName,
        legalPerson   = @legalPerson,
        registerDate  = @registerDate,
        contactPerson = @contactPerson,
        phone         = @phone,
        address       = @address
    WHERE supplierID = @supplierID;
    SELECT @@ROWCOUNT AS result;
END;
GO

-- sp_Supplier_Delete：删除供货商
--   返回 0 表示有关联商品无法删除，>0 表示删除成功
CREATE PROCEDURE sp_Supplier_Delete
    @supplierID INT
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM tbl_Product WHERE supplierID = @supplierID)
    BEGIN
        SELECT 0 AS result;
        RETURN;
    END
    DELETE FROM tbl_Supplier WHERE supplierID = @supplierID;
    SELECT @@ROWCOUNT AS result;
END;
GO

/* ----------------------------- 订单相关 ----------------------------------- */

-- 订单明细表值参数类型（用于 sp_Order_Create 批量传入明细）
CREATE TYPE dbo.OrderItemListType AS TABLE (
    productID   INT            NOT NULL,
    productName NVARCHAR(50)    NOT NULL,
    unitPrice   DECIMAL(10,2)  NOT NULL,
    quantity    INT            NOT NULL
);
GO

-- sp_Order_Create：创建订单（主表 + 明细一次性插入）
--   输入：订单头信息 + 明细列表（Table-Valued Parameter）
--   事务内逐条 INSERT tbl_OrderItem，由 tr_OrderItem_Insert 触发器扣减库存
--   返回：新订单 orderID
CREATE PROCEDURE sp_Order_Create
    @paymentMethod   NVARCHAR(10),
    @paymentStatus   NVARCHAR(10),
    @receiverName    NVARCHAR(20),
    @receiverPhone   NVARCHAR(15),
    @receiverAddress NVARCHAR(200),
    @items           dbo.OrderItemListType READONLY
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;  -- 任何错误自动回滚整个事务

    BEGIN TRY
        BEGIN TRANSACTION;

        -- 插入订单主表
        INSERT INTO tbl_Order (paymentMethod, paymentStatus, receiverName, receiverPhone, receiverAddress)
        VALUES (@paymentMethod, @paymentStatus, @receiverName, @receiverPhone, @receiverAddress);

        DECLARE @orderID INT = SCOPE_IDENTITY();

        -- 逐条插入明细，触发器自动扣减库存并检查库存不足
        INSERT INTO tbl_OrderItem (orderID, productID, productName, unitPrice, quantity)
        SELECT @orderID, productID, productName, unitPrice, quantity
        FROM @items;

        COMMIT TRANSACTION;
        SELECT @orderID AS result;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0
            ROLLBACK TRANSACTION;
        -- 重新抛出错误，应用层捕获
        THROW;
    END CATCH
END;
GO

-- sp_Order_Confirm：确认结款
--   仅对"未结款"订单执行更新，已结款订单返回 0
CREATE PROCEDURE sp_Order_Confirm
    @orderID INT
AS
BEGIN
    SET NOCOUNT ON;
    IF NOT EXISTS (SELECT 1 FROM tbl_Order WHERE orderID = @orderID AND paymentStatus = N'未结款')
    BEGIN
        SELECT 0 AS result;
        RETURN;
    END
    UPDATE tbl_Order
    SET paymentStatus = N'已结款',
        paymentTime   = GETDATE()
    WHERE orderID = @orderID;
    SELECT @@ROWCOUNT AS result;
END;
GO

/* ============================================================================
 * 五、初始数据
 * ========================================================================== */

-- 初始管理员账号
--   登录名：admin，密码：123456（SHA-256 哈希值）
--   SHA-256("123456") = 8d969eef6ecad3c29a3a629280e686cf0c3f5d5a86aff3ca12020c923adc6c92
--   注意：密码哈希在应用层计算后传入存储过程，此处直接写入哈希值
INSERT INTO tbl_User (loginName, password, role)
VALUES (
    N'admin',
    N'8d969eef6ecad3c29a3a629280e686cf0c3f5d5a86aff3ca12020c923adc6c92',
    N'管理员'
);
GO

-- 初始店员账号
INSERT INTO tbl_User (loginName, password, role)
VALUES (
    N'clerk',
    N'8d969eef6ecad3c29a3a629280e686cf0c3f5d5a86aff3ca12020c923adc6c92',
    N'店员'
);
GO

-- 初始商品类别
INSERT INTO tbl_Category (categoryName, categoryDesc, sortOrder) VALUES
(N'食品饮料', N'零食、饮料、方便食品', 1),
(N'日用品',   N'洗护用品、纸品',       2),
(N'文具用品', N'笔、本、办公用品',     3),
(N'电子产品', N'数码配件、外设',       4);
GO

-- 初始供货商
INSERT INTO tbl_Supplier (supplierName, legalPerson, registerDate, contactPerson, phone, address) VALUES
(N'中粮集团',     N'吕军',  '2018-05-10', N'王经理', '13800138001', N'北京市朝阳区曙光西里28号'),
(N'宝洁中国',     N'马睿睿', '2015-09-20', N'李经理', '13900139002', N'广州市天河区珠江新城'),
(N'晨光文具',     N'陈湖雄', '2008-07-15', N'张经理', '13700137003', N'上海市青浦区工业园'),
(N'小米科技',     N'雷军',  '2010-04-06', N'刘经理', '13600136004', N'北京市海淀区清河中街68号');
GO

-- 初始商品
INSERT INTO tbl_Product (productName, categoryID, unitPrice, origin, produceDate, stockQuantity, supplierID) VALUES
(N'康师傅红烧牛肉面', 1, 4.50,  N'天津',  '2026-06-01', 100, 1),
(N'农夫山泉矿泉水',   1, 2.00,  N'杭州',  '2026-06-15', 50,  1),
(N'可口可乐',         1, 3.00,  N'上海',  '2026-05-20', 8,   1),
(N'海飞丝洗发水',     2, 29.90, N'广州',  '2026-04-10', 30,  2),
(N'舒肤佳香皂',       2, 5.50,  N'广州',  '2026-05-01', 15,  2),
(N'晨光中性笔',       3, 2.50,  N'上海',  '2026-06-20', 200, 3),
(N'晨光笔记本',       3, 8.80,  N'上海',  '2026-06-20', 60,  3),
(N'小米充电宝',       4, 79.00, N'南京',  '2026-03-15', 10,  4),
(N'小米数据线',       4, 19.90, N'南京',  '2026-03-15', 5,   4);
GO

/* ============================================================================
 * 脚本执行完毕
 * ========================================================================== */
