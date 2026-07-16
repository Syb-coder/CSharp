# -*- coding: utf-8 -*-
"""
docx辅助函数库
提供字体设置、段落格式、表格操作、页面设置等底层能力
所有函数均为无状态纯函数，可被生成引擎直接调用

依赖: python-docx, pywin32(可选,用于更新目录)
"""
from docx.shared import Pt, Cm, RGBColor
from docx.enum.text import WD_ALIGN_PARAGRAPH, WD_LINE_SPACING, WD_BREAK
from docx.enum.table import WD_TABLE_ALIGNMENT, WD_ALIGN_VERTICAL
from docx.oxml.ns import qn
from docx.oxml import OxmlElement


# ============================================================
# 字体与Run操作
# ============================================================

def set_run_font(run, ascii_font='Times New Roman', east_asia='宋体',
                 size=None, bold=None, italic=None, color=None):
    """
    设置run的字体属性

    参数:
        run: docx的Run对象
        ascii_font: 西文字体名称
        east_asia: 中文字体名称(eastAsia slot)
        size: 字号, Pt对象, 如 Pt(12)
        bold: 是否加粗
        italic: 是否斜体
        color: RGBColor对象, 如 RGBColor(0,0,0)
    """
    if size is not None:
        run.font.size = size
    if bold is not None:
        run.font.bold = bold
    if italic is not None:
        run.font.italic = italic
    if color is not None:
        run.font.color.rgb = color
    if ascii_font:
        run.font.name = ascii_font

    # 设置eastAsia字体(必须操作XML)
    r = run._element
    rPr = r.find(qn('w:rPr'))
    if rPr is None:
        rPr = OxmlElement('w:rPr')
        r.insert(0, rPr)
    rFonts = rPr.find(qn('w:rFonts'))
    if rFonts is None:
        rFonts = OxmlElement('w:rFonts')
        rPr.insert(0, rFonts)
    if ascii_font:
        rFonts.set(qn('w:ascii'), ascii_font)
        rFonts.set(qn('w:hAnsi'), ascii_font)
    if east_asia:
        rFonts.set(qn('w:eastAsia'), east_asia)


def add_page_number_field(paragraph):
    """
    在段落中插入PAGE域(自动页码)
    用于页脚显示当前页码
    """
    run = paragraph.add_run()
    fldChar1 = OxmlElement('w:fldChar')
    fldChar1.set(qn('w:fldCharType'), 'begin')
    run._element.append(fldChar1)

    run2 = paragraph.add_run()
    instrText = OxmlElement('w:instrText')
    instrText.set(qn('xml:space'), 'preserve')
    instrText.text = ' PAGE '
    run2._element.append(instrText)

    run3 = paragraph.add_run()
    fldChar2 = OxmlElement('w:fldChar')
    fldChar2.set(qn('w:fldCharType'), 'end')
    run3._element.append(fldChar2)


def add_toc_field(doc):
    """
    在文档中插入TOC域(自动目录)
    生成后需用Word打开并更新域才能显示目录内容
    """
    p = doc.add_paragraph()
    run = p.add_run()
    fldChar1 = OxmlElement('w:fldChar')
    fldChar1.set(qn('w:fldCharType'), 'begin')
    run._element.append(fldChar1)

    run2 = p.add_run()
    instrText = OxmlElement('w:instrText')
    instrText.set(qn('xml:space'), 'preserve')
    instrText.text = r' TOC \o "1-3" \h \z \u '
    run2._element.append(instrText)

    run3 = p.add_run()
    fldChar2 = OxmlElement('w:fldChar')
    fldChar2.set(qn('w:fldCharType'), 'separate')
    run3._element.append(fldChar2)

    run4 = p.add_run('请右键更新域以生成目录')
    set_run_font(run4, size=Pt(12))

    run5 = p.add_run()
    fldChar3 = OxmlElement('w:fldChar')
    fldChar3.set(qn('w:fldCharType'), 'end')
    run5._element.append(fldChar3)


# ============================================================
# 段落操作
# ============================================================

def add_para(doc, text='', style=None, alignment=None,
             first_line_indent=None, left_indent=None,
             line_spacing=None, space_before=None, space_after=None,
             ascii_font='Times New Roman', east_asia='宋体',
             size=Pt(12), bold=False, color=None,
             page_break=False):
    """
    添加一个段落并设置格式

    参数:
        doc: Document对象
        text: 段落文本
        style: 段落样式名(如'Heading 1')
        alignment: 对齐方式, WD_ALIGN_PARAGRAPH枚举
        first_line_indent: 首行缩进, Cm对象
        left_indent: 左缩进, Cm对象
        line_spacing: 行间距倍数
        space_before: 段前间距, Pt对象
        space_after: 段后间距, Pt对象
        ascii_font/east_asia/size/bold/color: 字体属性
        page_break: 是否在段落后插入分页符

    返回:
        Paragraph对象
    """
    p = doc.add_paragraph(style=style)
    pf = p.paragraph_format

    if alignment is not None:
        p.alignment = alignment
    if first_line_indent is not None:
        pf.first_line_indent = first_line_indent
    if left_indent is not None:
        pf.left_indent = left_indent
    if line_spacing is not None:
        pf.line_spacing = line_spacing
    if space_before is not None:
        pf.space_before = space_before
    if space_after is not None:
        pf.space_after = space_after

    if text:
        run = p.add_run(text)
        set_run_font(run, ascii_font=ascii_font, east_asia=east_asia,
                     size=size, bold=bold, color=color)

    if page_break:
        run = p.add_run()
        run.add_break(WD_BREAK.PAGE)

    return p


def add_empty_para(doc, count=1, line_spacing=1.2):
    """添加空段落(用于间距控制)"""
    for _ in range(count):
        add_para(doc, '', line_spacing=line_spacing)


# ============================================================
# 表格操作
# ============================================================

def set_table_grid(table, widths):
    """
    设置表格列宽(底层XML操作)

    参数:
        table: Table对象
        widths: 列宽列表, DXA单位(twips), 如 [2130, 2130, 2131, 2131]
    """
    tbl = table._tbl
    existing = tbl.find(qn('w:tblGrid'))
    if existing is not None:
        tbl.remove(existing)
    tblGrid = OxmlElement('w:tblGrid')
    for w in widths:
        gridCol = OxmlElement('w:gridCol')
        gridCol.set(qn('w:w'), str(w))
        tblGrid.append(gridCol)
    tblPr = tbl.tblPr
    tblPr.addnext(tblGrid)


def set_table_borders(table, top_sz='12', bottom_sz='12',
                      inside_h_sz='2', inside_v_sz='2',
                      left_val='none', right_val='none'):
    """
    设置表格边框

    参数:
        table: Table对象
        top_sz/bottom_sz: 上下边框粗细(twips/8), '12'=1.5pt
        inside_h_sz/inside_v_sz: 内部横竖线粗细
        left_val/right_val: 左右边框, 'single'或'none'
    """
    tbl = table._tbl
    tblPr = tbl.tblPr
    existing = tblPr.find(qn('w:tblBorders'))
    if existing is not None:
        tblPr.remove(existing)

    borders = OxmlElement('w:tblBorders')
    for name, val, sz in [
        ('top', 'single', top_sz),
        ('left', left_val, '0'),
        ('bottom', 'single', bottom_sz),
        ('right', right_val, '0'),
        ('insideH', 'single', inside_h_sz),
        ('insideV', 'single', inside_v_sz)
    ]:
        border = OxmlElement(f'w:{name}')
        border.set(qn('w:val'), val)
        border.set(qn('w:sz'), sz)
        border.set(qn('w:space'), '0')
        border.set(qn('w:color'), 'auto')
        borders.append(border)
    tblPr.append(borders)


def merge_cells_h(row, start_col, span):
    """
    水平合并单元格(设置gridSpan + 删除多余tc)

    参数:
        row: TableRow对象
        start_col: 起始列索引(0-based)
        span: 合并的列数
    """
    tr = row._tr
    tcs = tr.findall(qn('w:tc'))
    if start_col >= len(tcs):
        return

    tc = tcs[start_col]
    tcPr = tc.find(qn('w:tcPr'))
    if tcPr is None:
        tcPr = OxmlElement('w:tcPr')
        tc.insert(0, tcPr)

    existing = tcPr.find(qn('w:gridSpan'))
    if existing is not None:
        tcPr.remove(existing)
    gridSpan = OxmlElement('w:gridSpan')
    gridSpan.set(qn('w:val'), str(span))
    tcPr.append(gridSpan)

    # 删除被合并的多余tc元素
    for i in range(1, span):
        if start_col + i < len(tcs):
            tr.remove(tcs[start_col + i])


def set_vmerge(cell, val='restart'):
    """
    设置垂直合并

    参数:
        cell: TableCell对象
        val: 'restart'=合并起始行, 'continue'=被合并行
    """
    tc = cell._tc
    tcPr = tc.find(qn('w:tcPr'))
    if tcPr is None:
        tcPr = OxmlElement('w:tcPr')
        tc.insert(0, tcPr)

    existing = tcPr.find(qn('w:vMerge'))
    if existing is not None:
        tcPr.remove(existing)
    vMerge = OxmlElement('w:vMerge')
    if val:
        vMerge.set(qn('w:val'), val)
    tcPr.append(vMerge)


def set_cell_text(cell, text, font_name='宋体', ascii_font='Times New Roman',
                  size=Pt(9), bold=False, alignment=WD_ALIGN_PARAGRAPH.JUSTIFY):
    """
    设置单元格文本和字体格式

    参数:
        cell: TableCell对象
        text: 文本内容(支持多行,用\\n分隔)
        font_name: 中文字体
        ascii_font: 西文字体
        size: 字号
        bold: 是否加粗
        alignment: 对齐方式
    """
    cell.text = ''
    lines = text.split('\n')
    for i, line in enumerate(lines):
        if i == 0:
            para = cell.paragraphs[0]
        else:
            para = cell.add_paragraph()
        para.alignment = alignment
        run = para.add_run(line)
        set_run_font(run, ascii_font=ascii_font, east_asia=font_name,
                     size=size, bold=bold)


def create_data_table(doc, headers, rows, col_widths,
                      border_h_sz='6', border_v_sz='6',
                      header_bold=True, cell_size=Pt(9)):
    """
    创建标准数据表格(表头行 + 数据行)

    参数:
        doc: Document对象
        headers: 表头列表, 如 ['列名', '说明', '数据类型', '约束']
        rows: 数据行列表(二维列表)
        col_widths: 列宽列表(DXA)
        border_h_sz: 水平内边框粗细
        border_v_sz: 垂直内边框粗细
        header_bold: 表头是否加粗
        cell_size: 单元格字号

    返回:
        Table对象
    """
    all_rows = [headers] + rows
    table = doc.add_table(rows=len(all_rows), cols=len(headers))
    table.alignment = WD_TABLE_ALIGNMENT.CENTER
    set_table_grid(table, col_widths)
    set_table_borders(table, top_sz='12', bottom_sz='12',
                      inside_h_sz=border_h_sz, inside_v_sz=border_v_sz)

    for r_idx, row_data in enumerate(all_rows):
        for c_idx, cell_text in enumerate(row_data):
            cell = table.rows[r_idx].cells[c_idx]
            is_header = (r_idx == 0)
            is_first_col = (c_idx == 0)
            if is_header or is_first_col:
                align = WD_ALIGN_PARAGRAPH.CENTER
            else:
                align = WD_ALIGN_PARAGRAPH.JUSTIFY
            set_cell_text(cell, cell_text, size=cell_size,
                          bold=(header_bold if is_header else False),
                          alignment=align)

    return table


def create_data_table_no_header(doc, rows, col_widths,
                                border_h_sz='6', border_v_sz='6',
                                cell_size=Pt(9)):
    """
    创建无表头数据表格(用于续表)

    参数:
        doc: Document对象
        rows: 数据行列表(二维列表)
        col_widths: 列宽列表(DXA)
        border_h_sz: 水平内边框粗细
        border_v_sz: 垂直内边框粗细
        cell_size: 单元格字号

    返回:
        Table对象
    """
    table = doc.add_table(rows=len(rows), cols=len(rows[0]) if rows else 1)
    table.alignment = WD_TABLE_ALIGNMENT.CENTER
    set_table_grid(table, col_widths)
    set_table_borders(table, top_sz='12', bottom_sz='12',
                      inside_h_sz=border_h_sz, inside_v_sz=border_v_sz)

    for r_idx, row_data in enumerate(rows):
        for c_idx, cell_text in enumerate(row_data):
            cell = table.rows[r_idx].cells[c_idx]
            is_first_col = (c_idx == 0)
            align = WD_ALIGN_PARAGRAPH.CENTER if is_first_col else WD_ALIGN_PARAGRAPH.JUSTIFY
            set_cell_text(cell, cell_text, size=cell_size, alignment=align)

    return table


# ============================================================
# 样式与页面设置
# ============================================================

def setup_styles(doc):
    """
    设置文档默认样式
    包括Normal、Heading 1/2/3，符合东北石油大学课程设计格式规范

    Normal:    宋体/Times New Roman, 五号(10.5pt)
    Heading 1: 黑体, 18pt, 居中（不加粗）
    Heading 2: 黑体, 15pt, 两端对齐（不加粗）
    Heading 3: 黑体, 14pt, 两端对齐（不加粗）
    """
    styles = doc.styles

    # --- Normal ---
    normal = styles['Normal']
    normal.font.name = 'Times New Roman'
    normal.font.size = Pt(10.5)
    _set_style_fonts(normal, 'Times New Roman', '宋体')

    # --- Heading 1 ---
    h1 = styles['Heading 1']
    h1.font.size = Pt(18)
    h1.font.bold = False
    h1.font.color.rgb = RGBColor(0, 0, 0)
    h1.paragraph_format.alignment = WD_ALIGN_PARAGRAPH.CENTER
    h1.paragraph_format.line_spacing = 1.2
    h1.paragraph_format.space_before = Pt(31.2)
    h1.paragraph_format.space_after = Pt(31.2)
    _set_style_fonts(h1, '黑体', '黑体')

    # --- Heading 2 ---
    h2 = styles['Heading 2']
    h2.font.size = Pt(15)
    h2.font.bold = False
    h2.font.color.rgb = RGBColor(0, 0, 0)
    h2.paragraph_format.alignment = WD_ALIGN_PARAGRAPH.JUSTIFY
    h2.paragraph_format.line_spacing = 1.2
    h2.paragraph_format.space_before = Pt(15.6)
    h2.paragraph_format.space_after = Pt(15.6)
    _set_style_fonts(h2, '黑体', '黑体')

    # --- Heading 3 ---
    h3 = styles['Heading 3']
    h3.font.size = Pt(14)
    h3.font.bold = False
    h3.font.color.rgb = RGBColor(0, 0, 0)
    h3.paragraph_format.alignment = WD_ALIGN_PARAGRAPH.JUSTIFY
    h3.paragraph_format.line_spacing = 1.2
    h3.paragraph_format.space_before = Pt(15.6)
    h3.paragraph_format.space_after = Pt(0)
    _set_style_fonts(h3, '黑体', '黑体')


def _set_style_fonts(style, ascii_font, east_asia):
    """设置样式的字体XML属性(内部函数)"""
    rPr = style.element.find(qn('w:rPr'))
    if rPr is None:
        rPr = OxmlElement('w:rPr')
        style.element.insert(0, rPr)
    rFonts = rPr.find(qn('w:rFonts'))
    if rFonts is None:
        rFonts = OxmlElement('w:rFonts')
        rPr.insert(0, rFonts)
    rFonts.set(qn('w:ascii'), ascii_font)
    rFonts.set(qn('w:hAnsi'), ascii_font)
    rFonts.set(qn('w:eastAsia'), east_asia)


def setup_page(doc, config):
    """
    设置页面尺寸和边距

    参数:
        doc: Document对象
        config: 配置字典, 包含page节
    """
    page = config['page']
    section = doc.sections[0]
    section.page_width = Cm(page['width_cm'])
    section.page_height = Cm(page['height_cm'])
    section.left_margin = Cm(page['left_margin_cm'])
    section.right_margin = Cm(page['right_margin_cm'])
    section.top_margin = Cm(page['top_margin_cm'])
    section.bottom_margin = Cm(page['bottom_margin_cm'])
    section.header_distance = Cm(page['header_distance_cm'])
    section.footer_distance = Cm(page['footer_distance_cm'])

    # 页脚页码
    footer = section.footer
    footer.is_linked_to_previous = False
    footer_para = footer.paragraphs[0]
    footer_para.alignment = WD_ALIGN_PARAGRAPH.CENTER
    add_page_number_field(footer_para)
