-- ============================================================
-- 图书类别表字段迁移脚本
-- 新增 borrowDays（可借阅天数）和 finePerDay（单日逾期罚款标准）
-- 执行方式：在 SSMS 中连接目标数据库后执行本脚本
-- ============================================================

USE LibraryDB;
GO

-- 1. 新增 borrowDays 字段（若不存在）
IF NOT EXISTS (SELECT 1 FROM sys.columns
               WHERE object_id = OBJECT_ID('tbl_BookCategory') AND name = 'borrowDays')
BEGIN
    ALTER TABLE tbl_BookCategory ADD borrowDays INT NOT NULL DEFAULT 30;
    PRINT N'borrowDays 字段添加成功，默认值 30 天';
END
ELSE
BEGIN
    PRINT N'borrowDays 字段已存在，跳过';
END
GO

-- 2. 新增 finePerDay 字段（若不存在）
IF NOT EXISTS (SELECT 1 FROM sys.columns
               WHERE object_id = OBJECT_ID('tbl_BookCategory') AND name = 'finePerDay')
BEGIN
    ALTER TABLE tbl_BookCategory ADD finePerDay DECIMAL(10, 2) NOT NULL DEFAULT 0.50;
    PRINT N'finePerDay 字段添加成功，默认值 0.50 元/天';
END
ELSE
BEGIN
    PRINT N'finePerDay 字段已存在，跳过';
END
GO

-- 3. 更新已有类别数据（根据业务规则设置差异化借阅天数和罚款标准）
UPDATE tbl_BookCategory SET borrowDays = 15, finePerDay = 1.00 WHERE categoryID = N'C01';
UPDATE tbl_BookCategory SET borrowDays = 30, finePerDay = 0.50 WHERE categoryID = N'C02';
UPDATE tbl_BookCategory SET borrowDays = 20, finePerDay = 0.80 WHERE categoryID = N'C03';

PRINT N'图书类别表字段迁移完成';
GO