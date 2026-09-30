# CSharp 课程设计项目集

> 基于 .NET 8 和 Windows Forms 的 C# 桌面应用课程设计集合，包含图书馆、校园商城、酒店管理等独立项目，以及配套的开发经验与课程设计资料。

## 项目内容

仓库中的每个 `cs-*` 目录通常对应一个独立的 Windows Forms 应用：

| 目录 | 内容 |
| --- | --- |
| `cs-0` | 公共资料、开发经验和论文模板资源 |
| `cs-1` | 图书馆信息管理系统 |
| `cs-2` | 校园易购信息管理系统 |
| `cs-3` | 校园易购/校园商城项目变体 |
| `cs-4` | 校园商店项目 |
| `cs-5` | 智慧图书馆管理系统 |
| `cs-6` | 酒店管理系统 |
| `_diag_login` | 登录界面诊断与测量辅助项目 |

不同项目的业务功能、数据库表和配置可能存在差异，请以对应目录的源码、`.csproj` 和 `App.config` 为准。

## 技术栈

- C# / .NET 8
- Windows Forms
- Microsoft.Data.SqlClient
- SQL Server
- Visual Studio 2022 或兼容 .NET 8 SDK 的 IDE

## 环境要求

- Windows（项目使用 `net8.0-windows` 和 Windows Forms）
- .NET 8 SDK
- SQL Server（需要数据库的项目）
- Visual Studio 2022（推荐）

## 快速开始

以图书馆项目为例：

```powershell
cd cs-1
dotnet restore
dotnet build
dotnet run
```

也可以直接用 Visual Studio 打开对应目录中的 `.csproj` 文件，等待 NuGet 依赖还原后运行项目。其他项目的启动命令相同，只需替换为目标目录：

```powershell
cd cs-6
dotnet restore
dotnet run
```

## 数据库配置

运行需要数据库的项目之前：

1. 启动 SQL Server 服务。
2. 检查项目目录中的 `App.config` 或 `config.json`。
3. 将连接字符串修改为本机数据库信息。
4. 按项目提供的脚本或说明创建数据库和数据表。

请勿提交真实数据库密码。建议使用本地配置、环境变量或用户级配置覆盖连接信息。

## 论文模板与开发经验

`cs-0` 中包含通用课程设计报告生成资源（如果使用该模板，请先查看目录内说明）以及开发过程中积累的经验文档：

- [开发经验](cs-0/experience.md)
- [项目指南](CLAUDE.md)

开发新项目时，建议先阅读经验文档，并将需求文档、数据库设计和关键问题记录在对应项目目录中。

## 注意事项

- 这些项目主要用于学习、课程设计和桌面应用开发实践。
- Windows Forms 项目无法直接在 Linux 或 macOS 上运行。
- 不同项目可能使用相似但不兼容的数据库结构，请不要混用配置文件。
- 运行前请审查 SQL、身份验证和输入校验逻辑，不要直接用于生产环境。
