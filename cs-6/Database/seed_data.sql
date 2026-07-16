-- ============================================================
-- 智慧酒店管理系统 HotelDB 初始化数据脚本（PRD 第10节）
-- 说明：程序启动时 DBInitializer 会自动补默认数据，此脚本为手动初始化备选
-- 执行方式：在 SSMS 中直接执行，或 sqlcmd -S localhost -E -f 65001 -i seed_data.sql
-- ============================================================
USE HotelDB;
GO

-- ===== 1. 默认管理员账号（密码 admin123 的 SHA-256 哈希） =====
-- 注意：SHA-256("admin123") = 240BE518fabd2724ddb6f04eeb1da5967448d7e831c08c8fa822809f74c720a9
IF NOT EXISTS (SELECT 1 FROM T_User WHERE userName='admin')
    INSERT INTO T_User (userName, userPassword, userPurview, realName)
    VALUES ('admin', '240BE518FABD2724DDB6F04EEB1DA5967448D7E831C08C8FA822809F74C720A9', N'管理员', N'系统管理员');
GO

-- ===== 2. 6种预置客房类型（PRD 第10节） =====
IF NOT EXISTS (SELECT 1 FROM T_RoomType WHERE typeID='RT01')
    INSERT INTO T_RoomType (typeID, typeName, price, bedCount, description)
    VALUES ('RT01', N'豪华套间', 588, 2, N'豪华装修，配独立客厅');
IF NOT EXISTS (SELECT 1 FROM T_RoomType WHERE typeID='RT02')
    INSERT INTO T_RoomType (typeID, typeName, price, bedCount, description)
    VALUES ('RT02', N'标准套间', 368, 2, N'标准装修，配独立客厅');
IF NOT EXISTS (SELECT 1 FROM T_RoomType WHERE typeID='RT03')
    INSERT INTO T_RoomType (typeID, typeName, price, bedCount, description)
    VALUES ('RT03', N'三人间', 268, 3, N'三张单人床');
IF NOT EXISTS (SELECT 1 FROM T_RoomType WHERE typeID='RT04')
    INSERT INTO T_RoomType (typeID, typeName, price, bedCount, description)
    VALUES ('RT04', N'标准间', 198, 2, N'两张单人床');
IF NOT EXISTS (SELECT 1 FROM T_RoomType WHERE typeID='RT05')
    INSERT INTO T_RoomType (typeID, typeName, price, bedCount, description)
    VALUES ('RT05', N'单人间', 138, 1, N'一张单人床');
IF NOT EXISTS (SELECT 1 FROM T_RoomType WHERE typeID='RT06')
    INSERT INTO T_RoomType (typeID, typeName, price, bedCount, description)
    VALUES ('RT06', N'其它', 100, 1, N'其它房型');
GO

-- ===== 3. 示例客房（PRD 第10节：2楼201-206，3楼301-306） =====
-- 房型分配：单号标准间、双号单人间、尾号6为套间
IF NOT EXISTS (SELECT 1 FROM T_Room WHERE roomNo='201')
    INSERT INTO T_Room (roomNo, typeID, floor, bedCount, roomStatus) VALUES ('201', 'RT04', 2, 2, N'空闲');
IF NOT EXISTS (SELECT 1 FROM T_Room WHERE roomNo='202')
    INSERT INTO T_Room (roomNo, typeID, floor, bedCount, roomStatus) VALUES ('202', 'RT05', 2, 1, N'空闲');
IF NOT EXISTS (SELECT 1 FROM T_Room WHERE roomNo='203')
    INSERT INTO T_Room (roomNo, typeID, floor, bedCount, roomStatus) VALUES ('203', 'RT04', 2, 2, N'空闲');
IF NOT EXISTS (SELECT 1 FROM T_Room WHERE roomNo='204')
    INSERT INTO T_Room (roomNo, typeID, floor, bedCount, roomStatus) VALUES ('204', 'RT05', 2, 1, N'空闲');
IF NOT EXISTS (SELECT 1 FROM T_Room WHERE roomNo='205')
    INSERT INTO T_Room (roomNo, typeID, floor, bedCount, roomStatus) VALUES ('205', 'RT04', 2, 2, N'空闲');
IF NOT EXISTS (SELECT 1 FROM T_Room WHERE roomNo='206')
    INSERT INTO T_Room (roomNo, typeID, floor, bedCount, roomStatus) VALUES ('206', 'RT02', 2, 2, N'空闲');
IF NOT EXISTS (SELECT 1 FROM T_Room WHERE roomNo='301')
    INSERT INTO T_Room (roomNo, typeID, floor, bedCount, roomStatus) VALUES ('301', 'RT04', 3, 2, N'空闲');
IF NOT EXISTS (SELECT 1 FROM T_Room WHERE roomNo='302')
    INSERT INTO T_Room (roomNo, typeID, floor, bedCount, roomStatus) VALUES ('302', 'RT05', 3, 1, N'空闲');
IF NOT EXISTS (SELECT 1 FROM T_Room WHERE roomNo='303')
    INSERT INTO T_Room (roomNo, typeID, floor, bedCount, roomStatus) VALUES ('303', 'RT04', 3, 2, N'空闲');
IF NOT EXISTS (SELECT 1 FROM T_Room WHERE roomNo='304')
    INSERT INTO T_Room (roomNo, typeID, floor, bedCount, roomStatus) VALUES ('304', 'RT05', 3, 1, N'空闲');
IF NOT EXISTS (SELECT 1 FROM T_Room WHERE roomNo='305')
    INSERT INTO T_Room (roomNo, typeID, floor, bedCount, roomStatus) VALUES ('305', 'RT04', 3, 2, N'空闲');
IF NOT EXISTS (SELECT 1 FROM T_Room WHERE roomNo='306')
    INSERT INTO T_Room (roomNo, typeID, floor, bedCount, roomStatus) VALUES ('306', 'RT01', 3, 2, N'空闲');
GO

-- ===== 4. 预设客户数据（5条示例） =====
-- SHA-256("front123") = 4A92475E32CEF713E47A3DCB24925F3DF0ADC20D87D8156F7BF0E0EBA5C3D2DB
IF NOT EXISTS (SELECT 1 FROM T_User WHERE userName='front01')
    INSERT INTO T_User (userName, userPassword, userPurview, realName)
    VALUES ('front01', '4A92475E32CEF713E47A3DCB24925F3DF0ADC20D87D8156F7BF0E0EBA5C3D2DB', N'前台', N'前台操作员');
GO

-- 按身份证号去重，避免重复插入
IF NOT EXISTS (SELECT 1 FROM T_Customer WHERE idNumber='110101199001011234')
    INSERT INTO T_Customer (customerName, gender, idType, idNumber, phone, address)
    VALUES (N'张三', N'男', N'身份证', '110101199001011234', '13800001111', N'北京市朝阳区建国路100号');
IF NOT EXISTS (SELECT 1 FROM T_Customer WHERE idNumber='310101199205201234')
    INSERT INTO T_Customer (customerName, gender, idType, idNumber, phone, address)
    VALUES (N'李四', N'女', N'身份证', '310101199205201234', '13900002222', N'上海市浦东新区陆家嘴1号');
IF NOT EXISTS (SELECT 1 FROM T_Customer WHERE idNumber='E12345678')
    INSERT INTO T_Customer (customerName, gender, idType, idNumber, phone, address)
    VALUES (N'王五', N'男', N'护照', 'E12345678', '13600003333', N'广东省广州市天河区体育西路50号');
IF NOT EXISTS (SELECT 1 FROM T_Customer WHERE idNumber='440101199807152345')
    INSERT INTO T_Customer (customerName, gender, idType, idNumber, phone, address)
    VALUES (N'赵六', N'女', N'身份证', '440101199807152345', '13700004444', N'深圳市南山区科技园路88号');
IF NOT EXISTS (SELECT 1 FROM T_Customer WHERE idNumber='500101198503101345')
    INSERT INTO T_Customer (customerName, gender, idType, idNumber, phone, address)
    VALUES (N'陈七', N'男', N'身份证', '500101198503101345', '13500005555', N'重庆市渝中区解放碑5号');
GO

-- ===== 5. 预设预订数据（3条示例） =====
-- 张三预订 201房（标准间）3天，明天入住
INSERT INTO T_Reservation (customerID, roomNo, expectCheckIn, expectDays, contactPhone, status, remark)
SELECT c.customerID, '201', CAST(DATEADD(day, 1, GETDATE()) AS DATE), 3, c.phone, N'待入住', N'商务出差'
FROM T_Customer c WHERE c.idNumber='110101199001011234'
AND NOT EXISTS (SELECT 1 FROM T_Reservation r JOIN T_Customer c2 ON r.customerID=c2.customerID WHERE c2.idNumber='110101199001011234' AND r.roomNo='201');
UPDATE T_Room SET roomStatus=N'预留' WHERE roomNo='201' AND roomStatus=N'空闲';
-- 李四预订 301房（标准间）2天，后天入住
INSERT INTO T_Reservation (customerID, roomNo, expectCheckIn, expectDays, contactPhone, status, remark)
SELECT c.customerID, '301', CAST(DATEADD(day, 2, GETDATE()) AS DATE), 2, c.phone, N'待入住', N'旅游度假'
FROM T_Customer c WHERE c.idNumber='310101199205201234'
AND NOT EXISTS (SELECT 1 FROM T_Reservation r JOIN T_Customer c2 ON r.customerID=c2.customerID WHERE c2.idNumber='310101199205201234' AND r.roomNo='301');
UPDATE T_Room SET roomStatus=N'预留' WHERE roomNo='301' AND roomStatus=N'空闲';
-- 陈七预订 206房（标准套间）1天，三天后入住
INSERT INTO T_Reservation (customerID, roomNo, expectCheckIn, expectDays, contactPhone, status, remark)
SELECT c.customerID, '206', CAST(DATEADD(day, 3, GETDATE()) AS DATE), 1, c.phone, N'待入住', N'会议'
FROM T_Customer c WHERE c.idNumber='500101198503101345'
AND NOT EXISTS (SELECT 1 FROM T_Reservation r JOIN T_Customer c2 ON r.customerID=c2.customerID WHERE c2.idNumber='500101198503101345' AND r.roomNo='206');
UPDATE T_Room SET roomStatus=N'预留' WHERE roomNo='206' AND roomStatus=N'空闲';
GO

-- ===== 6. 预设入住数据（2条示例，在住状态） =====
-- 王五 入住 202房（单人间）2天，昨天入住
INSERT INTO T_CheckIn (customerID, roomNo, checkInTime, expectCheckOut, deposit, status, remark)
SELECT c.customerID, '202', DATEADD(day, -1, GETDATE()), DATEADD(day, 1, GETDATE()), 138, N'在住', N'安静楼层'
FROM T_Customer c WHERE c.idNumber='E12345678'
AND NOT EXISTS (SELECT 1 FROM T_CheckIn ci JOIN T_Customer c2 ON ci.customerID=c2.customerID WHERE c2.idNumber='E12345678' AND ci.roomNo='202' AND ci.status=N'在住');
UPDATE T_Room SET roomStatus=N'在住' WHERE roomNo='202' AND roomStatus=N'空闲';
-- 赵六 入住 303房（标准间）3天，前天入住
INSERT INTO T_CheckIn (customerID, roomNo, checkInTime, expectCheckOut, deposit, status, remark)
SELECT c.customerID, '303', DATEADD(day, -2, GETDATE()), DATEADD(day, 1, GETDATE()), 198, N'在住', N'高层景观房'
FROM T_Customer c WHERE c.idNumber='440101199807152345'
AND NOT EXISTS (SELECT 1 FROM T_CheckIn ci JOIN T_Customer c2 ON ci.customerID=c2.customerID WHERE c2.idNumber='440101199807152345' AND ci.roomNo='303' AND ci.status=N'在住');
UPDATE T_Room SET roomStatus=N'在住' WHERE roomNo='303' AND roomStatus=N'空闲';
GO

-- ===== 7. 历史已结账记录（5条，用于统计界面演示） =====
-- 注意：这些是历史退房数据，不修改房间当前状态
-- 张三 201房（标准间198元）3天前入住，住2天，昨天退房，total=396
IF NOT EXISTS (SELECT 1 FROM T_CheckIn ci JOIN T_Customer c ON ci.customerID=c.customerID WHERE c.idNumber='110101199001011234' AND ci.roomNo='201' AND ci.status=N'已结账')
INSERT INTO T_CheckIn (customerID, roomNo, checkInTime, expectCheckOut, checkOutTime, actualDays, deposit, roomCharge, otherCharge, consumeAmount, totalAmount, status, remark)
SELECT c.customerID, '201', DATEADD(day,-3,GETDATE()), DATEADD(day,-1,GETDATE()), DATEADD(day,-1,GETDATE()), 2, 198, 396, 0, 0, 396, N'已结账', N'正常退房'
FROM T_Customer c WHERE c.idNumber='110101199001011234';
-- 李四 301房（标准间198元）5天前入住，住5天，今天退房，total=990
IF NOT EXISTS (SELECT 1 FROM T_CheckIn ci JOIN T_Customer c ON ci.customerID=c.customerID WHERE c.idNumber='310101199205201234' AND ci.roomNo='301' AND ci.status=N'已结账')
INSERT INTO T_CheckIn (customerID, roomNo, checkInTime, expectCheckOut, checkOutTime, actualDays, deposit, roomCharge, otherCharge, consumeAmount, totalAmount, status, remark)
SELECT c.customerID, '301', DATEADD(day,-5,GETDATE()), GETDATE(), GETDATE(), 5, 198, 990, 0, 0, 990, N'已结账', N'正常退房'
FROM T_Customer c WHERE c.idNumber='310101199205201234';
-- 陈七 206房（标准套间368元）10天前入住，住2天，8天前退房，total=736
IF NOT EXISTS (SELECT 1 FROM T_CheckIn ci JOIN T_Customer c ON ci.customerID=c.customerID WHERE c.idNumber='500101198503101345' AND ci.roomNo='206' AND ci.status=N'已结账')
INSERT INTO T_CheckIn (customerID, roomNo, checkInTime, expectCheckOut, checkOutTime, actualDays, deposit, roomCharge, otherCharge, consumeAmount, totalAmount, status, remark)
SELECT c.customerID, '206', DATEADD(day,-10,GETDATE()), DATEADD(day,-8,GETDATE()), DATEADD(day,-8,GETDATE()), 2, 368, 736, 0, 0, 736, N'已结账', N'正常退房'
FROM T_Customer c WHERE c.idNumber='500101198503101345';
-- 张三 305房（标准间198元）15天前入住，住2天，13天前退房，total=396
IF NOT EXISTS (SELECT 1 FROM T_CheckIn ci JOIN T_Customer c ON ci.customerID=c.customerID WHERE c.idNumber='110101199001011234' AND ci.roomNo='305' AND ci.status=N'已结账')
INSERT INTO T_CheckIn (customerID, roomNo, checkInTime, expectCheckOut, checkOutTime, actualDays, deposit, roomCharge, otherCharge, consumeAmount, totalAmount, status, remark)
SELECT c.customerID, '305', DATEADD(day,-15,GETDATE()), DATEADD(day,-13,GETDATE()), DATEADD(day,-13,GETDATE()), 2, 198, 396, 0, 0, 396, N'已结账', N'正常退房'
FROM T_Customer c WHERE c.idNumber='110101199001011234';
-- 王五 306房（豪华套间588元）20天前入住，住2天，18天前退房，total=1176
IF NOT EXISTS (SELECT 1 FROM T_CheckIn ci JOIN T_Customer c ON ci.customerID=c.customerID WHERE c.idNumber='E12345678' AND ci.roomNo='306' AND ci.status=N'已结账')
INSERT INTO T_CheckIn (customerID, roomNo, checkInTime, expectCheckOut, checkOutTime, actualDays, deposit, roomCharge, otherCharge, consumeAmount, totalAmount, status, remark)
SELECT c.customerID, '306', DATEADD(day,-20,GETDATE()), DATEADD(day,-18,GETDATE()), DATEADD(day,-18,GETDATE()), 2, 588, 1176, 0, 0, 1176, N'已结账', N'正常退房'
FROM T_Customer c WHERE c.idNumber='E12345678';
GO

-- ===== 8. 预设消费数据（3条示例） =====
-- 王五消费：矿泉水 10元
INSERT INTO T_Consume (checkInID, itemName, amount, remark)
SELECT ci.checkInID, N'矿泉水', 10, N'客房服务'
FROM T_CheckIn ci JOIN T_Customer c ON ci.customerID=c.customerID
WHERE c.idNumber='E12345678' AND ci.roomNo='202' AND ci.status=N'在住'
AND NOT EXISTS (SELECT 1 FROM T_Consume co JOIN T_CheckIn ci2 ON co.checkInID=ci2.checkInID JOIN T_Customer c2 ON ci2.customerID=c2.customerID WHERE c2.idNumber='E12345678' AND ci2.roomNo='202' AND ci2.status=N'在住' AND co.itemName=N'矿泉水');
-- 赵六消费：早餐 30元
INSERT INTO T_Consume (checkInID, itemName, amount, remark)
SELECT ci.checkInID, N'早餐', 30, N'餐饮部'
FROM T_CheckIn ci JOIN T_Customer c ON ci.customerID=c.customerID
WHERE c.idNumber='440101199807152345' AND ci.roomNo='303' AND ci.status=N'在住'
AND NOT EXISTS (SELECT 1 FROM T_Consume co JOIN T_CheckIn ci2 ON co.checkInID=ci2.checkInID JOIN T_Customer c2 ON ci2.customerID=c2.customerID WHERE c2.idNumber='440101199807152345' AND ci2.roomNo='303' AND ci2.status=N'在住' AND co.itemName=N'早餐');
-- 赵六消费：洗衣服务 50元
INSERT INTO T_Consume (checkInID, itemName, amount, remark)
SELECT ci.checkInID, N'洗衣服务', 50, N'客房服务'
FROM T_CheckIn ci JOIN T_Customer c ON ci.customerID=c.customerID
WHERE c.idNumber='440101199807152345' AND ci.roomNo='303' AND ci.status=N'在住'
AND NOT EXISTS (SELECT 1 FROM T_Consume co JOIN T_CheckIn ci2 ON co.checkInID=ci2.checkInID JOIN T_Customer c2 ON ci2.customerID=c2.customerID WHERE c2.idNumber='440101199807152345' AND ci2.roomNo='303' AND ci2.status=N'在住' AND co.itemName=N'洗衣服务');
GO
