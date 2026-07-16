# Claude 项目指南

## 项目概述

本目录是 CSharp 课程设计项目的统一工作区，包含多个独立项目，按 `cs-0`、`cs-1`、`cs-2`、`cs-3`... 顺序命名。

## 目录结构

```
CSharp/
├── Claude.md              ← 本文件（项目指南）
├── cs-0/                  ← 公共资源目录（论文模板 + 经验文档）
│   ├── experience.md      ← 【重要】开发经验文档，记录踩坑与解决方案
│   ├── template/          ← 论文生成模板系统
│   │   ├── README.md      ← 模板使用说明
│   │   ├── config_template.json   ← 配置文件模板
│   │   ├── generate_thesis.py     ← 主生成引擎（JSON → docx）
│   │   └── docx_helpers.py        ← docx 底层辅助函数库
│   └── *.docx             ← 已生成的示例论文
├── cs-1/                  ← 项目1：图书馆信息管理系统
├── cs-2/                  ← 项目2（待创建）
├── cs-3/                  ← 项目3（待创建）
└── ...
```

## cs-0 公共资源说明

### 论文生成模板（cs-0/template/）

cs-0 中包含一套**通用的课程设计报告生成引擎**。通过填写一份 JSON 配置文件，即可一键生成格式完全符合东北石油大学课程设计规范的 Word 文档（.docx）。

**快速使用流程：**

```bash
# 1. 环境准备
pip install python-docx pywin32

# 2. 复制配置模板并修改内容
copy cs-0/template/config_template.json my_project.json

# 3. 生成论文
python cs-0/template/generate_thesis.py my_project.json "输出路径.docx"
```

每个新项目只需修改配置文件中的内容（项目名称、学生信息、章节文本、数据库表、测试用例等），运行一条命令即可生成完整论文。详见 [cs-0/template/README.md](cs-0/template/README.md)。

### 开发经验文档（cs-0/experience.md）

**开发新项目前务必先阅读此文档**，避免重复踩坑。

当前记录的经验：

| 编号 | 经验主题 | 来源项目 | 核心要点 |
|---|---|---|---|
| 经验一 | WinForms 控件遮盖问题 | cs-1 | Label 宽度必须测量而非估算，AutoSize=true 测量后再固定宽度 |

后续项目遇到的关键问题会持续追加到该文档中。

## 新项目创建规范

1. 项目目录命名为 `cs-2`、`cs-3`... 递增
2. 需求文档存放于 `项目目录/docs/` 下
3. 开发中遇到的关键问题记录到 `cs-0/experience.md`
4. 论文生成使用 `cs-0/template/` 下的模板引擎
