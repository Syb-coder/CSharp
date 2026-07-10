# 图书馆信息管理系统 - 技术设计文档

> 文档创建日期：2026-07-10
> 对应 PRD：docs/cs-1/prd.md

---

## 1. 技术选型

| 决策项 | 选择 | 说明 |
|--------|------|------|
| UI 框架 | WinForms | 与 PRD 控件描述匹配，DataGridView 展示数据表格 |
| 数据访问 | ADO.NET | SqlConnection + SqlCommand + SqlParameter 参数化查询 |
| 运行时 | .NET 8 (LTS) | VS 最新版默认支持 |
| 数据库 | SQL Server 2025 | Windows 认证 |
| 项目结构 | 分层单项目 | 按文件夹分层（UI/BLL/DAL/Models/Common） |
| 密码加密 | SHA-256 | 哈希存储，非明文 |

## 2. 项目结构

```
cs-1/  (LibraryManagement)
├── App.config                    # 连接字符串配置
├── Database/
│   └── init.sql                  # 建库建表+测试数据脚本
├── Models/                       # 实体类（对应数据表）
├── DAL/                          # 数据访问层
├── BLL/                          # 业务逻辑层
├── Common/                       # 公共工具
├── Forms/                        # UI 层（WinForms 窗体）
└── Program.cs                    # 入口
```

## 3. 分层职责

- **Models**：纯实体类，属性映射数据表字段
- **DAL**：每个表一个 DAL 类，只做 CRUD，返回 Model 对象，全部使用 SqlParameter
- **BLL**：封装业务规则（校验、关联检查、事务编排），UI 层只调用 BLL
- **Common**：无状态工具（PasswordHelper 哈希、ValidationHelper 校验）
- **Forms**：WinForms 窗体，负责交互与展示

## 4. 关键业务规则

- 借阅上限 `MAX_BORROW_LIMIT = 5`
- 借阅期限 `DEFAULT_BORROW_DAYS = 30`
- 借还书在同一 SqlTransaction 内完成（INSERT/UPDATE 借阅记录 + UPDATE 图书可借数量）
- 删除前检查关联数据（图书查未归还借阅、读者查未归还借阅、类别查关联图书）
- 用户密码 SHA-256 哈希存储
- 不允许删除当前登录用户

## 5. 数据库连接

App.config 中配置 Windows 认证连接字符串：
```
Server=localhost;Database=LibraryDB;Integrated Security=True;TrustServerCertificate=True;
```
