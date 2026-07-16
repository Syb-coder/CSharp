# 论文模板生成系统

## 概述

本系统是一个**通用的课程设计报告生成引擎**。通过填写一份 JSON 配置文件，即可一键生成格式完全符合东北石油大学课程设计规范的 Word 文档（.docx）。

后续每个新项目只需修改配置文件中的内容（项目名称、学生信息、章节文本、数据库表、测试用例等），运行一条命令即可生成完整论文。

## 文件结构

```
cs-0/template/
├── config_template.json   # 配置文件模板（复制后修改内容）
├── generate_thesis.py     # 主生成引擎（读取配置→生成docx）
├── docx_helpers.py        # docx底层辅助函数库（一般不需要修改）
├── README.md              # 本说明文件
└── test_output.docx       # 测试生成的样例文档
```

## 快速开始

### 1. 环境准备

```bash
pip install python-docx
pip install pywin32    # 可选，用于自动更新目录域（需要安装Microsoft Word）
```

### 2. 创建项目配置

复制配置模板并重命名：
```bash
copy config_template.json my_project.json
```

### 3. 修改配置内容

用任意文本编辑器打开 `my_project.json`，修改以下关键部分：

| 配置节 | 说明 | 示例 |
|--------|------|------|
| `meta` | 学校名称、封面字体日期 | 东北石油大学 |
| `page` | 页面尺寸和边距 | A4, 左3cm右2.5cm |
| `student` | 学生信息 | 姓名、学号、班级 |
| `task_page` | 任务书内容 | 课程、题目、主要内容、要求 |
| `chapters` | 四章正文内容 | 段落文本、数据库表、测试用例 |
| `conclusion` | 结论 | 功能列表、技术总结 |
| `references` | 参考文献 | 文献列表 |
| `appendix` | 附录 | 会议记录、成绩评价表 |

### 4. 生成论文

```bash
python generate_thesis.py my_project.json "输出路径.docx"
```

不指定输出路径时，默认生成在同目录下，文件名格式为：
```
报告-{班级}-{姓名}-{题目}.docx
```

## 配置文件详解

### meta - 封面元信息

```json
{
  "university": "东北石油大学",
  "university_spaced": "东 北 石 油 大 学",
  "doc_type": "课  程  设  计",
  "cover_date": "2026年 7 月25日",
  "cover_font": "华文行楷"
}
```

### student - 学生信息

```json
{
  "class_name": "计科25-1班01",
  "name": "张三",
  "student_id": "250702940701",
  "department": "计算机与信息技术学院"
}
```

### chapters - 正文章节

章节结构支持递归嵌套，每章包含 `sections`，每个 section 可包含 `paragraphs`、`subsections`、`figure`、`code_blocks`：

```json
{
  "heading": "1.1 项目的目的和意义",
  "paragraphs": ["段落1", "段落2"],
  "subsections": [
    {
      "heading": "1.1.1 子标题",
      "paragraphs": ["子段落"]
    }
  ],
  "figure": {
    "caption": "图1-1 示意图",
    "placeholder_lines": 5
  }
}
```

### chapters.chapter2.db_tables - 数据库表

每个表自动生成标题、介绍文字和数据表格：

```json
{
  "title": "表2-1 系统用户表",
  "label": "1．系统用户表",
  "intro": "系统用户表用于存放...",
  "headers": ["列名", "说明", "数据类型", "约束"],
  "rows": [
    ["userName", "用户名", "字符串", "主键"]
  ],
  "col_widths": [2130, 1398, 1980, 3014]
}
```

支持续表（跨页表格拆分）：
```json
{
  "continued_title": "续表2-3",
  "continued_rows": [
    ["Ccredit", "学分", "整数", "-"]
  ]
}
```

### chapters.chapter4 - 测试结果表

```json
{
  "test_table": {
    "title": "表4-1系统测试结果表",
    "headers": ["测试项目", "验证过程", "预期结果", "实际结果", "结论说明"],
    "rows": [["登录", "输入账号密码", "进入系统", "进入系统", "通过"]],
    "continued_rows": [["查询", "输入条件", "显示结果", "显示结果", "通过"]]
  }
}
```

### appendix - 附录

附录包含团队会议记录和成绩评价表，全部参数化：

```json
{
  "team_name": "开发团队名称",
  "instructor": "指导教师",
  "meetings": [
    {
      "title": "附录1团队开发会议记录1",
      "subject": "会议主题",
      "content_summary": ["议题1", "议题2"],
      "content_details": ["详情1", "详情2"]
    }
  ],
  "grade_table": {
    "eval_items": [
      {
        "name": "团队合作（10分）",
        "max_score": "10",
        "a_standard": "A级标准描述",
        "c_standard": "C级标准描述"
      }
    ]
  }
}
```

## 格式规范

系统自动应用的格式（符合东北石油大学课程设计要求）：

| 元素 | 字体 | 字号 | 说明 |
|------|------|------|------|
| 正文 | 宋体/Times New Roman | 五号(10.5pt) | 首行缩进2字符 |
| 一级标题 | 黑体 | 二号(22pt) | 加粗,居中 |
| 二级标题 | 黑体 | 小四(12pt) | 加粗 |
| 三级标题 | 黑体 | 三号(16pt) | 加粗 |
| 页面 | A4 | 21x29.7cm | 左3/右2.5/上3/下2.5cm |
| 页脚 | - | - | 居中自动页码 |
| 目录 | - | - | TOC自动生成域 |

## 常见问题

### Q: 生成的文档目录显示"请右键更新域"？
A: 如果安装了 pywin32 和 Microsoft Word，系统会自动更新目录。否则请手动用 Word 打开，右键点击目录区域选择"更新域"。

### Q: 如何添加图片？
A: 在配置中将 `placeholder_lines` 设为合适的行数，生成后在 Word 中手动插入图片到占位区域。后续版本计划支持自动插入图片文件。

### Q: 如何修改格式（字体、字号等）？
A: 修改 `docx_helpers.py` 中的 `setup_styles()` 函数，调整对应的字号和字体设置。

### Q: 如何增减章节？
A: 在配置文件的 `chapters` 中添加或删除对应的章节块，生成引擎会自动遍历所有章节。

## 新项目工作流

```
1. 复制 config_template.json → my_project.json
2. 修改 my_project.json 中的内容（约30分钟）
3. 运行: python generate_thesis.py my_project.json
4. 用 Word 打开生成的 docx，插入截图
5. 完成
```
