# CSharp 项目经验文档

> 本文档记录 CSharp 课程设计项目开发过程中遇到的关键问题与解决方案，供后续项目参考避免重复踩坑。

---

## 经验一：WinForms 控件遮盖问题（cs-1 图书馆管理系统）

### 问题现象

窗体界面中 Label 文字被白色块（TextBox/ComboBox）遮挡，最后一个字（通常是冒号"："）不可见。经过多轮修复仍未解决，用户反复反馈"字被盖住了"。

### 根因分析

问题经历了**三个阶段**的演变：

**阶段一：AutoSize=true 导致宽度不可控**

Label 默认 `AutoSize=true`，实际渲染宽度由字体、文字内容、DPI 共同决定，与代码中预设的 TextBox 起始 X 坐标产生重叠。

- 示例：`"读者编号："` 在 Microsoft YaHei UI 9pt 下实际宽度=100px，但 TextBox.X 预设为 85px
- 结果：TextBox 覆盖了 Label 右侧约 15px 的文字

**阶段二：修复时矫枉过正，固定宽度过小**

将 Label 改为 `AutoSize=false` + 固定宽度后，**宽度过小导致文字被自身裁切**：

| Label 文字 | 实际需要 | 修复时设置 | 结果 |
|---|---|---|---|
| `"书名："` | 64px | 45px | 冒号被裁切 |
| `"读者编号："` | 100px | 80px | 最后一个字被裁切 |

紧邻的白色 TextBox 在视觉上仍然像"盖住了最后一个字"。

**阶段三：根据实际渲染宽度设置固定宽度**

用诊断程序测量 AutoSize=true 时的真实宽度，据此设置固定宽度，问题彻底解决。

### 解决方案

**核心原则：WinForms 手动布局中，任何坐标都应该是"测量出来的"，而非"估算出来的"。**

#### 1. Label 正确配置模板

```csharp
Label lbl = new()
{
    Text = "读者编号：",
    Font = new Font("Microsoft YaHei UI", 9F),
    Location = new Point(15, 15),
    AutoSize = false,                          // 关闭自动尺寸
    Size = new Size(100, 20),                  // 宽度=AutoSize=true 时的实际值
    TextAlign = ContentAlignment.MiddleLeft,   // 左对齐，避免文字居中留白
    BackColor = Color.Transparent              // 透明背景，避免色差"白块"
};
// TextBox.X = Label.X + Label.Width + 5（5px 间距）
TextBox txt = new() { Location = new Point(120, 12), Size = new Size(120, 25) };
```

#### 2. 中文 Label 宽度参考表

| 文字类型 | 示例 | 实际宽度（YaHei UI 9pt） |
|---|---|---|
| 2字+冒号 | 书名：、作者：、类别： | 64-65px |
| 3字+冒号 | 出版社：、用户名： | 82-85px |
| 4字+冒号 | 读者编号：、图书编号： | 100px |
| 5字+冒号 | 借出日期起： | 118-120px |
| 英文+冒号 | ISBN： | 69-70px |

#### 3. 封装统一的 Label 创建方法（推荐）

```csharp
/// <summary>
/// 创建固定宽度 Label，宽度根据文字内容自动测量
/// </summary>
/// <param name="text">显示文字</param>
/// <param name="x">X坐标</param>
/// <param name="y">Y坐标</param>
/// <returns>配置好的 Label，宽度已固定为实际渲染值</returns>
private static Label CreateLabel(string text, int x, int y)
{
    // 先用 AutoSize 测量，再固定宽度，确保文字完整显示
    Label lbl = new()
    {
        Text = text,
        Font = new Font("Microsoft YaHei UI", 9F),
        AutoSize = true,
        Location = new Point(x, y),
        TextAlign = ContentAlignment.MiddleLeft,
        BackColor = Color.Transparent
    };
    using (Graphics g = lbl.CreateGraphics())
    {
        SizeF measured = g.MeasureString(text, lbl.Font);
        lbl.AutoSize = false;
        lbl.Size = new Size((int)measured.Width + 2, 20); // +2px 保险余量
    }
    return lbl;
}

/// <summary>
/// 根据 Label 右边缘计算下一个输入控件的 X 坐标
/// </summary>
/// <param name="lbl">前一个 Label</param>
/// <param name="gap">间距，默认 5px</param>
/// <returns>输入控件的 X 坐标</returns>
private static int NextX(Label lbl, int gap = 5) => lbl.Location.X + lbl.Width + gap;
```

#### 4. 布局诊断程序模板

遇到布局问题时，用以下程序输出所有控件的绝对坐标，精确判断重叠：

```csharp
/// <summary>
/// 递归输出控件树及绝对坐标，用于诊断布局重叠
/// </summary>
private static void DumpControls(Control.ControlCollection controls, StringBuilder sb, int depth)
{
    for (int i = 0; i < controls.Count; i++)
    {
        Control c = controls[i];
        Point abs = GetAbsolutePosition(c);
        string indent = new string(' ', depth * 2 + 2);
        sb.AppendLine($"{indent}[{i}] {c.GetType().Name} \"{c.Text}\" AbsX={abs.X} W={c.Width} AbsRight={abs.X + c.Width}");
        if (c.HasChildren && c.Controls.Count > 0)
            DumpControls(c.Controls, sb, depth + 1);
    }
}

/// <summary>
/// 计算控件相对屏幕的绝对位置（累加所有父容器偏移）
/// </summary>
private static Point GetAbsolutePosition(Control c)
{
    int x = c.Location.X, y = c.Location.Y;
    Control parent = c.Parent;
    while (parent != null)
    {
        x += parent.Location.X;
        y += parent.Location.Y;
        parent = parent.Parent;
    }
    return new Point(x, y);
}
```

### 预防措施

1. **建立"测量→布局→验证"闭环**：先测量 Label 实际宽度，再设置 TextBox 坐标，最后运行诊断核对无重叠
2. **优先考虑 TableLayoutPanel**：表单类界面用表格布局，列宽自动适配，永远不会重叠
3. **布局问题必须先诊断再修改**：遇到任何布局问题，第一步运行诊断程序获取真实坐标数据，而非凭经验直接改代码
4. **CI 加入布局校验**：检查所有 Label.AbsRight < 紧邻输入控件.AbsX

---

## 经验二：sqlcmd 执行 UTF-8 编码 SQL 脚本失败（cs-2 校园易购系统）

### 问题现象

使用 `sqlcmd -i init.sql` 执行包含中文注释和中文数据的 SQL 脚本时，部分 CREATE TABLE 语句的主键未被创建，导致后续外键引用报错：

```
消息 1776，级别 16，状态 1
在被引用表 'tbl_Category' 中没有与外键 'FK_tbl_Product_Category' 中的引用列列表匹配的主键或候选键。
```

但直接在 SSMS 中执行同一脚本完全正常。

### 根因分析

`sqlcmd` 命令行工具默认按 **OEM 代码页**（中文 Windows 为 GBK/936）读取输入文件。当脚本保存为 **UTF-8** 编码时，中文字符的多字节序列被 sqlcmd 按 GBK 解析，产生不可见的乱码字符，干扰了 T-SQL 解析器对语句边界的识别：

- 含中文注释的 `CREATE TABLE` 语句被错误截断
- `CONSTRAINT PK_xxx PRIMARY KEY` 子句丢失，主键未创建
- 后续依赖该主键的外键创建失败

**关键特征**：tbl_Supplier（注释较少）创建成功，tbl_Category 和 tbl_User（注释较多）失败，说明中文注释越多越容易触发。

### 解决方案

#### 方案一：转换为 GBK 编码后执行（推荐用于 sqlcmd）

```powershell
# 用 PowerShell 将 UTF-8 脚本转换为 GBK 编码
[IO.File]::WriteAllText(
    'init_gbk.sql',
    [IO.File]::ReadAllText('init.sql', [Text.Encoding]::UTF8),
    [Text.Encoding]::GetEncoding(936)
)
# 然后用 sqlcmd 执行 GBK 版本
sqlcmd -S localhost -E -i init_gbk.sql
```

#### 方案二：在 SSMS 中直接执行（推荐用于开发环境）

SSMS 能正确识别 UTF-8 编码，直接打开 init.sql 执行即可，无需转换。

#### 方案三：sqlcmd 指定 UTF-8 代码页（推荐用于自动化脚本，cs-5 验证）

sqlcmd 支持 `-f` 参数指定输入输出代码页，`65001` 即 UTF-8。此方案无需转码文件，一行命令即可解决：

```powershell
# -f 65001 让 sqlcmd 以 UTF-8 解析输入文件，中文注释和中文数据均正常
sqlcmd -S localhost -E -f 65001 -i cs-5\Database\init.sql
```

**cs-5 验证案例**：执行 `sqlcmd -S localhost -E -i init.sql`（未加 `-f`）时，T_Book 表创建失败但无明确错误，仅表现为后续外键报错"列名 'price' 无效"和"外键引用无效列 'bookID'"。加 `-f 65001` 后全部表正常创建。

**三种方案对比**：

| 方案 | 适用场景 | 优点 | 缺点 |
|---|---|---|---|
| 转码为 GBK | 必须用 sqlcmd 且不支持 -f 参数 | 兼容性最好 | 需额外转码步骤 |
| SSMS 执行 | 开发环境人工操作 | 零配置 | 无法用于自动化 |
| `-f 65001` | 自动化脚本/CICD | 一行命令解决 | 需 sqlcmd 版本支持该参数 |

### 预防措施

1. **统一执行方式**：文档中明确标注"init.sql 请在 SSMS 中执行或使用 `sqlcmd -f 65001`"，避免直接用 sqlcmd 默认编码
2. **sqlcmd 执行 UTF-8 脚本必须加 `-f 65001`**：如 `sqlcmd -S localhost -E -f 65001 -i init.sql`，无需转码文件
3. **sqlcmd 执行前转码**：若 sqlcmd 版本不支持 `-f` 参数，先转换为 GBK 编码再执行
4. **验证建库结果**：执行后用以下查询验证主键完整性

```sql
-- 验证所有表是否都有主键
SELECT t.name AS table_name,
       COALESCE(i.name, '(无主键)') AS pk_name
FROM sys.tables t
LEFT JOIN sys.indexes i
    ON t.object_id = i.object_id AND i.is_primary_key = 1
ORDER BY t.name;
```

---

## 经验三：SQL Server 自引用外键在 CREATE TABLE 内联定义的注意事项（cs-2 校园易购系统）

### 问题现象

在 `CREATE TABLE` 语句中内联定义自引用外键时，某些 SQL Server 版本/工具组合下会报错：

```sql
CREATE TABLE tbl_Category (
    categoryID NVARCHAR(10) NOT NULL,
    parentCategoryID NVARCHAR(10) NULL,
    CONSTRAINT PK_tbl_Category PRIMARY KEY (categoryID),
    CONSTRAINT FK_tbl_Category_Parent FOREIGN KEY (parentCategoryID)
        REFERENCES tbl_Category(categoryID)
);
```

错误信息：
```
在被引用表 'tbl_Category' 中没有与外键 'FK_tbl_Category_Parent' 中的引用列列表匹配的主键或候选键。
```

### 根因分析

实际上是经验二的编码问题导致的衍生现象——主键因编码问题未创建成功，外键找不到引用列。自引用外键在 `CREATE TABLE` 内联定义本身是合法的 T-SQL 语法，SQL Server 2025 也支持。

验证方法：单独执行简化版测试，确认语法本身无误：

```sql
CREATE TABLE TestSelfRef (
    id INT PRIMARY KEY,
    parent_id INT,
    FOREIGN KEY (parent_id) REFERENCES TestSelfRef(id)
);
DROP TABLE TestSelfRef;
```

### 结论

**自引用外键内联定义语法正确，无需改为 ALTER TABLE 后添加**。遇到此错误时应先排查编码问题（见经验二），而非修改 SQL 语法。

---

## 经验四：事务回滚异常可能掩盖原始业务异常（cs-2 校园易购系统）

### 问题现象

`SaleService.ProcessSale` 方法在 catch 块中调用 `transaction.Rollback()` 后重新抛出异常。但当原始异常是连接断开导致的 `SqlException` 时，`Rollback()` 自身可能再抛异常，导致原始业务异常被掩盖，调用方拿到的是 Rollback 的异常而非真正的根因。

### 解决方案

用内层 try-catch 包裹 Rollback，确保原始异常始终被传播：

```csharp
catch
{
    // 用内层 try-catch 包裹 Rollback，防止 Rollback 异常掩盖原始异常
    try { transaction.Rollback(); } catch { /* 忽略 Rollback 异常 */ }
    throw;  // 始终传播原始异常
}
```

同时，DAL 层抛出的 `InvalidOperationException`（如库存不足）应在 BLL 层转为 `BusinessException`，以便 UI 层统一处理：

```csharp
catch (InvalidOperationException ex)
{
    try { transaction.Rollback(); } catch { }
    throw new BusinessException(ex.Message);
}
```

### 预防措施

1. **事务回滚必须用 try-catch 包裹**：确保 Rollback 异常不会掩盖原始异常
2. **DAL 层异常应在 BLL 层统一转换**：UI 层只需处理 `BusinessException`（业务校验失败）和 `Exception`（系统错误）两类异常

---

## 经验五：并行 Subagent 开发中的三类编译问题（cs-3 CampusMart 系统）

### 问题现象

cs-3 项目采用两个并行 Task subagent 分工创建 8 个 UI 窗体，合并后出现三类编译错误，且两个 subagent 对同一问题的报告存在矛盾（一个报错、一个报通过），需主程序实际编译验证。

### 问题一：NPOI 类型歧义（CS0104）

**现象**：`ExcelUtil.cs` 中 `HorizontalAlignment` 和 `BorderStyle` 报 CS0104 错误——`"HorizontalAlignment"是"System.Windows.Forms.HorizontalAlignment"和"NPOI.SS.UserModel.HorizontalAlignment"之间的不明确的引用`。

**根因**：.NET 8.0 项目启用 `ImplicitUsings` 后，`System.Windows.Forms` 命名空间被自动导入，其中存在与 NPOI 同名的 `HorizontalAlignment` 枚举和 `BorderStyle` 枚举，编译器无法判断使用哪一个。

**解决方案**：使用完全限定名消除歧义：

```csharp
// 错误写法（歧义）
style.Alignment = HorizontalAlignment.Center;
style.BorderTop = BorderStyle.Thin;

// 正确写法（完全限定名）
style.Alignment = NPOI.SS.UserModel.HorizontalAlignment.Center;
style.BorderTop = NPOI.SS.UserModel.BorderStyle.Thin;
```

**替代方案**：在文件顶部使用 using 别名：
```csharp
using NpoiHAlign = NPOI.SS.UserModel.HorizontalAlignment;
using NpoiBorder = NPOI.SS.UserModel.BorderStyle;
// 然后用 NpoiHAlign.Center
```

### 问题二：源文件被截断（CS0246 连锁报错）

**现象**：`BusinessException.cs` 报 CS0246（找不到类型或命名空间名"BusinessException"），且全项目 60+ 处引用 `BusinessException` 的代码全部报错。但 `BusinessException.cs` 文件确实存在。

**根因**：磁盘上的 `BusinessException.cs` 文件被截断至仅 39 字节，内容在 `/// <summary` 处中断，类定义完全缺失。可能是 subagent 写入过程中磁盘 I/O 异常或并发写入冲突导致。

**诊断方法**：用 `Get-Item xxx.cs | Select-Object Length` 检查文件大小，正常应 >700 字节，异常时仅几十字节。

**解决方案**：删除损坏文件后重新写入完整内容：

```powershell
# 1. 检查文件大小
Get-Item c:\000\code\CSharp\cs-3\BLL\BusinessException.cs | Select-Object Length

# 2. 删除损坏文件
Remove-Item c:\000\code\CSharp\cs-3\BLL\BusinessException.cs

# 3. 用 WriteAllText 重新写入完整内容
[IO.File]::WriteAllText('c:\000\code\CSharp\cs-3\BLL\BusinessException.cs', $content, [Text.Encoding]::UTF8)
```

### 问题三：泛型方法类型推断失败（CS0411）

**现象**：`FrmGoods.cs` 调用 `SelectComboItem(cbo, index, x => x.CategoryID)` 报 CS0411——`"System.Action<int>"无法从用法中推断出类型参数 T"`。

**根因**：泛型方法 `SelectComboItem<T>(ComboBox, int, Func<T, int>)` 的第三个参数是 lambda 表达式 `x => x.CategoryID`，编译器无法仅从 lambda 的 `x` 推断 T 的类型（因为 x 的类型取决于 T，而 T 是待推断的）。

**解决方案**：显式指定泛型类型参数：

```csharp
// 错误写法（无法推断 T）
SelectComboItem(cboCategory, index, x => x.CategoryID);

// 正确写法（显式指定 T 为 CategoryInfo）
SelectComboItem<CategoryInfo>(cboCategory, index, x => x.CategoryID);
SelectComboItem<SupplierInfo>(cboSupplier, index, x => x.SupplierID);
```

**通用规则**：当泛型方法的参数列表中包含 `Func<T, ...>` 或 `Action<T>` 等 lambda 参数，且 T 仅出现在 lambda 参数位置时，必须显式指定类型参数。

### 预防措施

1. **并行 subagent 报告需主程序验证**：多个 subagent 并行开发时，对同一代码库的状态认知可能不一致，必须由主程序执行 `dotnet build` 做最终编译验证
2. **关键基础设施类（如 BusinessException）由主程序预创建**：避免多个 subagent 并行写入同一文件导致截断
3. **NPOI 项目禁用 ImplicitUsings 或使用完全限定名**：NPOI 与 WinForms 存在多处枚举重名，最稳妥的做法是在涉及 NPOI 的文件中使用完全限定名
4. **泛型方法 + lambda 参数必须显式指定类型**：避免依赖编译器类型推断

---

## 经验六：论文生成格式与老师模板不匹配（cs-1 图书馆管理系统）

### 问题现象

老师反馈生成的课程设计论文存在三个问题：
1. **没有模块图**：第2章系统功能模块图位置只有空白占位行，无实际图形
2. **格式不一样**：标题字号、表格字号、图表标题字体与老师发的模板不匹配
3. **瞅着太乱**：整体排版不规范

### 根因分析

通过用 Word COM 接口读取老师发回的 `.doc` 模板文件，提取每段的样式和字体信息，对比生成引擎的默认设置，发现以下差异：

| 格式元素 | 老师模板 | 生成引擎原设置 | 差异 |
|---|---|---|---|
| 标题1 | 黑体 18pt 不加粗 居中 | 黑体 22pt 加粗 居中 | 字号大4pt、多了加粗 |
| 标题2 | 黑体 15pt 不加粗 两端对齐 | 黑体 12pt 加粗 | 字号小3pt、多了加粗、缺两端对齐 |
| 标题3 | 黑体 14pt 不加粗 两端对齐 | 黑体 16pt 加粗 | 字号大2pt、多了加粗、缺两端对齐 |
| 表格单元格 | 宋体 9pt | 宋体 10.5pt | 字号大1.5pt |
| 图/表标题 | 黑体 10.5pt 不加粗 | 宋体 12pt | 字体错、字号大1.5pt |
| 模块图 | 有 Shape 图形 | 仅空白占位行 | 无实际图片 |

### 解决方案

#### 1. 修改模板引擎样式定义（docx_helpers.py）

```python
# setup_styles() 中修改标题样式
h1.font.size = Pt(18)       # 原 22pt
h1.font.bold = False         # 原 True
h2.font.size = Pt(15)       # 原 12pt
h2.font.bold = False         # 原 True
h2.paragraph_format.alignment = WD_ALIGN_PARAGRAPH.JUSTIFY  # 新增两端对齐
h3.font.size = Pt(14)       # 原 16pt
h3.font.bold = False         # 原 True
h3.paragraph_format.alignment = WD_ALIGN_PARAGRAPH.JUSTIFY  # 新增两端对齐

# set_cell_text() 和 create_data_table() 默认字号
size=Pt(9)                   # 原 10.5pt
```

#### 2. 修改论文生成脚本（generate_thesis.py）

```python
# build_section() 标题字号和粗体
size=Pt(18 if heading_level == 1 else 15 if heading_level == 2 else 14),
bold=False    # 原 bold=True

# 图/表标题改为黑体 10.5pt（原宋体 12pt）
add_para(doc, fig['caption'],
         ascii_font='黑体', east_asia='黑体', size=Pt(10.5))
```

#### 3. 增加图片插入功能

在 `build_section()` 和 `build_chapter2()` 中增加 `image_path` 支持：

```python
if 'image_path' in fig:
    p_img = doc.add_paragraph()
    p_img.alignment = WD_ALIGN_PARAGRAPH.CENTER
    run_img = p_img.add_run()
    run_img.add_picture(fig['image_path'], width=Cm(15))
else:
    add_empty_para(doc, fig.get('placeholder_lines', 2))
```

config.json 中配置：
```json
"module_figure": {
    "caption": "图2-1  系统功能模块图",
    "image_path": "c:\\000\\code\\CSharp\\cs-1\\module_diagram.png",
    "image_width_cm": 15
}
```

#### 4. 用 matplotlib 生成功能模块图

用 Python matplotlib 绘制三层树状结构图：根节点（系统名）→ 一级模块（7个）→ 二级子功能，保存为 PNG 后嵌入 docx。

### 预防措施

1. **新项目首次生成论文后，必须与老师模板逐项对比格式**：标题字号/粗体、表格字号、图表标题字体/字号
2. **用 Word COM 接口提取模板格式**：`win32com.client.Dispatch("Word.Application")` 读取每段的 Style、Font.Size、Font.Bold，生成格式对比表
3. **功能模块图必须生成实际图片**：不能只留空白占位行，需用 matplotlib/PIL 生成 PNG 并通过 `image_path` 嵌入

---

## 经验七：WinForms TableLayoutPanel Percent 列在 DPI 缩放时导致控件重叠（cs-5 图书馆系统）

### 问题现象

窗体在不同 DPI 缩放比例（100%/125%/150%）下出现控件重叠：Label 文字被相邻 TextBox 遮挡，或多个输入框互相挤压。在本机 100% 缩放下正常，在别人电脑（125%/150% 缩放）下重叠严重。

### 根因分析

TableLayoutPanel 使用 `SizeType.Percent` 列存放输入控件时，Percent 列的宽度 = `(容器宽度 - AutoSize列总宽度) / 列数`。当 DPI 缩放为 150% 时：

1. **Label 的 AutoSize 实际宽度增大**：9pt 字体在 150% 缩放下渲染宽度增加 50%，4字+冒号的 Label 从 100px 变为约 150px
2. **AutoSize 列占用更多空间**：3 个 Label 列总宽度从 300px 变为 450px
3. **Percent 列被压缩**：容器宽度不变（Dock=Top 限制为父容器宽度），剩余空间 = 900 - 450 = 450px，每个 Percent 列 = 450/3 = 150px
4. **控件 MinimumSize 不足以阻止压缩**：TextBox 的 MinimumSize=80px，150px > 80px 所以不触发保护
5. **结果**：输入框宽度从预期的 200px 被压缩到 150px，与相邻 Label 边缘重叠

### 解决方案

**核心原则：输入列用 `SizeType.Absolute` 固定宽度，不用 `SizeType.Percent`。**

```csharp
// 错误写法：Percent 列在 DPI 缩放时会被压缩
panel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));     // Label 列
panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33f)); // 输入列 ← 会重叠

// 正确写法：Absolute 列宽固定，不受 DPI 缩放影响
panel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));      // Label 列：自动适配文字宽度
panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 200)); // 输入列：固定200px，不会被压缩
```

**为什么 Absolute 不会重叠**：
- Absolute 列宽是逻辑像素，WinForms 的 DPI 缩放会等比例放大列宽
- 200 逻辑像素在 150% DPI 下 = 300 物理像素，足够容纳控件
- Label 列 AutoSize 也会等比例缩放，但不会侵占输入列空间（因为输入列是固定宽度）

### 补充：窗体异常打不开的处理

**问题**：修改布局代码后，在别人电脑上所有子窗体都打不开（点击菜单无反应或崩溃）。

**根因**：
1. `FrmMain.OpenForm()` 没有 try-catch，窗体构造函数中的 `LoadData()` 抛 SqlException 时直接崩溃
2. `FrmReader` 使用 `Load += delegate { LoadData(); };` 延迟加载，WinForms 在某些情况下会吞掉 Load 事件中的异常

**解决方案**：
```csharp
// 1. OpenForm 加 try-catch，显示完整错误信息
private void OpenForm(string formKey)
{
    try
    {
        Form form = formKey switch { ... };
        form.ShowDialog();
        form.Dispose();
    }
    catch (Exception ex)
    {
        MessageBox.Show($"打开窗体失败：{ex.Message}\n\n{ex.StackTrace}", "错误", ...);
    }
}

// 2. 所有窗体在构造函数中直接调用 LoadData()，不用 Load 事件延迟加载
public FrmReader(UserInfo currentUser)
{
    _currentUser = currentUser;
    _canEdit = currentUser.UserPurview == BusinessConstants.ROLE_ADMIN;
    InitializeUI();
    LoadData();  // 直接调用，不用 Load += ...
}
```

### 预防措施

1. **TableLayoutPanel 输入列必须用 Absolute，不用 Percent**：Percent 列在 DPI 缩放时会被压缩，Absolute 不会
2. **OpenForm 必须加 try-catch**：防止子窗体构造函数异常导致整个程序崩溃
3. **LoadData 在构造函数中直接调用**：不要用 Load 事件延迟加载，WinForms 可能吞掉 Load 中的异常
4. **部署时拷贝整个输出目录**：不能只拷贝 exe，需要包含 App.config（数据库连接字符串）和所有依赖 DLL

---

## 经验八：部署到新电脑时用户表为空导致登录失败（cs-5 图书馆系统）

### 问题现象

程序在开发机上运行正常，拷贝到别人电脑后登录失败，提示"用户名、密码或身份不正确"。用户输入的账号密码与开发机一致（user01/user123），但数据库查询返回空结果。

### 根因分析

三层问题叠加导致此现象：

**第一层：sqlcmd 编码问题导致 INSERT 失败**

在别人电脑上执行 init.sql 时未加 `-f 65001`，UTF-8 中文注释/中文值被 sqlcmd 以 GBK 解析乱码。虽然 CREATE TABLE 部分（SQL 关键字和ASCII标识符）可能侥幸成功，但含有中文字符串（如 N'管理员'、N'普通用户'）的 INSERT 语句解析失败，导致：
- T_User 表结构存在，但表为空
- 登录时 SQL 查询能成功执行，但返回 0 行，因此提示"密码错误"而非"连接失败"

**第二层：fix-db.ps1 只检查数据库存在性，不检查数据完整性**

旧版 fix-db.ps1 检测到 LibraryDB 数据库已存在就跳过了全部初始化步骤，不会检查表是否完整、用户数据是否存在。

**第三层：App.config 中 Server 地址硬编码为 localhost**

如果别人电脑上只有 SQLEXPRESS 命名实例（默认安装就是这个），`Server=localhost` 连不上，需要改为 `Server=localhost\SQLEXPRESS`。

### 解决方案

**最佳方案：程序启动时自动检测并初始化数据库（零配置部署）**

1. 创建 `DBInitializer` 静态类，在 `Program.Main()` 中、`Application.Run()` 之前调用：
   - 自动依次尝试 `localhost`、`localhost\SQLEXPRESS`、`.`、`.\SQLEXPRESS`、`(localdb)\MSSQLLocalDB` 等候选实例
   - 先连 master 库确保 LibraryDB 存在
   - 再连 LibraryDB 检查8张表是否存在，缺失则自动建表（使用参数化SQL，无需外部 .sql 文件）
   - 检查默认用户 admin/user01 是否存在，缺失则插入（密码用MD5哈希，与SecurityUtil一致）
   - 将探测到的正确连接字符串动态写回 DBConnection，DAO层无需感知

2. 核心代码结构：

```csharp
// Program.cs
[STAThread]
static void Main()
{
    ApplicationConfiguration.Initialize();
    if (!DBInitializer.EnsureDatabaseReady(out string error))
    {
        MessageBox.Show($"数据库初始化失败：{error}\n可尝试双击 install.bat 修复。", ...);
        return;
    }
    Application.Run(new FrmLogin());
}
```

```csharp
// DBInitializer.cs 关键方法
private static readonly string[] _candidates = { "localhost", @"localhost\SQLEXPRESS", ".", @".\SQLEXPRESS", "(localdb)\\MSSQLLocalDB" };

private static string ResolveConnectionString()
{
    // 先试配置的地址，失败则依次尝试候选实例
    string configured = ConfigurationManager.ConnectionStrings["LibraryDB"]?.ConnectionString;
    if (TestConnection(configured)) return configured;
    foreach (var server in _candidates)
        if (TestConnection(BuildConnStr(server, "master")))
            return BuildConnStr(server, "LibraryDB");
    return null;
}

private static void EnsureDefaultUsers(SqlConnection conn)
{
    // 用参数化SQL的IF NOT EXISTS INSERT，幂等安全
    InsertIfMissing(conn, "admin", AdminPwdHash, "管理员");
    InsertIfMissing(conn, "user01", UserPwdHash, "普通用户");
}
```

**辅助方案：install.bat 一键修复**

对于无法自动修复的情况（如 SQL Server 未安装），提供 .bat 批处理绕过 PowerShell 执行策略限制：

```bat
@echo off
chcp 65001 >nul
powershell -ExecutionPolicy Bypass -NoProfile -File "%~dp0fix-db.ps1"
pause
```

别人电脑只需双击 install.bat，无需手动开 PowerShell 或改执行策略。

### 预防措施

1. **WinForms + SQL Server 项目必须实现启动时自动初始化**：不能依赖用户手动执行 SQL 脚本，程序本身应具备"零配置启动"能力
2. **数据库初始化必须幂等**：使用 `IF NOT EXISTS` 模式，重复执行不会出错或插入重复数据
3. **连接字符串不能硬编码单一实例名**：必须自动探测 localhost/default 和 localhost\SQLEXPRESS 两种最常见情况
4. **提供 install.bat 兜底**：双击即可修复，绕过 PowerShell 执行策略限制（默认 Restricted 策略禁止运行 ps1 脚本）
5. **fix-db.ps1 必须检查数据完整性而非仅检查数据库存在**：即使数据库存在，也要验证表和初始用户是否完整
6. **部署文档明确说明 SQL Server 要求**：对方电脑必须安装 SQL Server Express（免费），并确保服务正在运行

---

后续项目遇到的关键问题继续在此文档追加记录。
