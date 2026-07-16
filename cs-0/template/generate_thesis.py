# -*- coding: utf-8 -*-
"""
论文模板生成引擎
读取JSON配置文件，生成完整的东北石油大学课程设计报告docx

用法:
    python generate_thesis.py config.json [输出路径]

如不指定输出路径，默认输出到当前目录下，文件名格式为:
    报告-{班级}-{学号后4位}-{姓名}-{题目}.docx

依赖:
    pip install python-docx
    可选: pip install pywin32 (用于自动更新目录域)
"""
import json
import sys
import os

from docx import Document
from docx.shared import Pt, Cm, RGBColor
from docx.enum.text import WD_ALIGN_PARAGRAPH, WD_BREAK
from docx.enum.table import WD_TABLE_ALIGNMENT
from docx.oxml.ns import qn
from docx.oxml import OxmlElement

from docx_helpers import (
    set_run_font, add_page_number_field, add_toc_field,
    add_para, add_empty_para,
    set_table_grid, set_table_borders, merge_cells_h, set_vmerge,
    set_cell_text, create_data_table, create_data_table_no_header,
    setup_styles, setup_page
)


# ============================================================
# 封面
# ============================================================

def build_cover(doc, cfg):
    """生成封面页面"""
    meta = cfg['meta']
    cover_font = meta.get('cover_font', '华文行楷')

    add_empty_para(doc, 2)
    add_para(doc, meta['university_spaced'],
             alignment=WD_ALIGN_PARAGRAPH.CENTER, line_spacing=2.0,
             ascii_font=cover_font, east_asia=cover_font,
             size=Pt(meta['cover_university_size']))
    add_empty_para(doc, 1)
    add_para(doc, meta['doc_type'],
             alignment=WD_ALIGN_PARAGRAPH.CENTER, line_spacing=2.0,
             ascii_font=cover_font, east_asia=cover_font,
             size=Pt(meta['cover_doctype_size']))
    add_empty_para(doc, 4)
    add_para(doc, meta['cover_date'],
             alignment=WD_ALIGN_PARAGRAPH.CENTER, line_spacing=2.0,
             ascii_font=cover_font, east_asia=cover_font,
             size=Pt(meta['cover_date_size']))

    p = doc.add_paragraph()
    p.add_run().add_break(WD_BREAK.PAGE)


# ============================================================
# 任务书
# ============================================================

def build_task_page(doc, cfg):
    """生成任务书页面"""
    tp = cfg['task_page']
    stu = cfg['student']

    # 标题
    add_para(doc, tp['title'],
             alignment=WD_ALIGN_PARAGRAPH.CENTER, line_spacing=2.0,
             ascii_font='宋体', east_asia='宋体', size=Pt(26))
    add_empty_para(doc, 1)

    # 课程和题目
    add_para(doc, f"课程                       {tp['course']}",
             alignment=WD_ALIGN_PARAGRAPH.JUSTIFY, line_spacing=2.0, size=Pt(12))
    add_para(doc, f"题目                   {tp['subject']}",
             alignment=WD_ALIGN_PARAGRAPH.JUSTIFY, line_spacing=2.0, size=Pt(12))

    # 院系信息行
    dept_line = (f"院系  {stu['department']}  姓名     {stu['name']}"
                 f"     学号  {stu['student_id']}")
    add_para(doc, dept_line,
             alignment=WD_ALIGN_PARAGRAPH.JUSTIFY, line_spacing=2.0, size=Pt(12))

    add_para(doc, '主要内容、基本要求、主要参考资料等',
             alignment=WD_ALIGN_PARAGRAPH.JUSTIFY, line_spacing=2.0, size=Pt(12))

    # 主要内容
    add_para(doc, '一、主要内容',
             alignment=WD_ALIGN_PARAGRAPH.JUSTIFY, line_spacing=1.5,
             space_before=Pt(7.85), size=Pt(12))
    add_para(doc, tp['main_content'],
             alignment=WD_ALIGN_PARAGRAPH.JUSTIFY,
             first_line_indent=Cm(0.74), line_spacing=1.5, size=Pt(12))

    # 基本要求
    add_para(doc, '二、基本要求',
             alignment=WD_ALIGN_PARAGRAPH.JUSTIFY, line_spacing=1.5,
             space_before=Pt(7.85), size=Pt(12))
    for req in tp['requirements']:
        add_para(doc, req,
                 alignment=WD_ALIGN_PARAGRAPH.JUSTIFY,
                 first_line_indent=Cm(0.74), line_spacing=1.5, size=Pt(12))

    # 参考资料
    add_para(doc, '三、主要参考资料',
             alignment=WD_ALIGN_PARAGRAPH.JUSTIFY, line_spacing=1.5,
             space_before=Pt(7.85), size=Pt(12))
    for ref in tp['references']:
        add_para(doc, ref,
                 alignment=WD_ALIGN_PARAGRAPH.JUSTIFY,
                 first_line_indent=Cm(0.74), line_spacing=1.5, size=Pt(12))

    # 签名行
    add_empty_para(doc, 1)
    add_para(doc, f"完成期限       {tp['deadline']}      ",
             alignment=WD_ALIGN_PARAGRAPH.JUSTIFY, line_spacing=1.5, size=Pt(12))
    add_para(doc, '指导教师                    ',
             alignment=WD_ALIGN_PARAGRAPH.JUSTIFY, line_spacing=1.5, size=Pt(12))
    add_para(doc, '专业负责人                  ',
             alignment=WD_ALIGN_PARAGRAPH.JUSTIFY, line_spacing=1.5, size=Pt(12))
    add_empty_para(doc, 1)
    add_para(doc, f"                                                      {tp['task_date']}",
             alignment=WD_ALIGN_PARAGRAPH.CENTER, line_spacing=1.5, size=Pt(12))

    # 分页
    add_para(doc, '', first_line_indent=Cm(1.27), line_spacing=1.2)
    p = doc.add_paragraph()
    p.add_run().add_break(WD_BREAK.PAGE)


# ============================================================
# 目录
# ============================================================

def build_toc(doc):
    """生成目录页(TOC域)"""
    add_para(doc, '目 录',
             alignment=WD_ALIGN_PARAGRAPH.CENTER, line_spacing=1.2,
             space_before=Pt(31.2), space_after=Pt(31.2),
             ascii_font='黑体', east_asia='黑体', size=Pt(18))
    add_toc_field(doc)


# ============================================================
# 正文章节构建
# ============================================================

def build_section(doc, section, heading_level=2):
    """
    递归构建章节内容
    支持任意层级的section/subsection嵌套

    参数:
        doc: Document对象
        section: 配置中的章节字典
        heading_level: 当前标题层级(2=H2, 3=H3)
    """
    style_name = f'Heading {heading_level}'

    # 添加标题
    p = doc.add_paragraph(style=style_name)
    p.paragraph_format.first_line_indent = Cm(0)
    run = p.add_run(section['heading'])
    set_run_font(run, ascii_font='黑体', east_asia='黑体',
                 size=Pt(18 if heading_level == 1 else 15 if heading_level == 2 else 14),
                 bold=False)

    # 添加段落
    for text in section.get('paragraphs', []):
        add_para(doc, text,
                 alignment=WD_ALIGN_PARAGRAPH.JUSTIFY,
                 first_line_indent=Cm(0.85), line_spacing=1.2, size=Pt(12))

    # 添加图片（支持实际图片或占位空行）
    if 'figure' in section:
        fig = section['figure']
        if 'image_path' in fig:
            # 插入实际图片
            p_img = doc.add_paragraph()
            p_img.alignment = WD_ALIGN_PARAGRAPH.CENTER
            run_img = p_img.add_run()
            img_width = fig.get('image_width_cm', 14)
            run_img.add_picture(fig['image_path'], width=Cm(img_width))
        else:
            # 占位空行（无图片时）
            add_empty_para(doc, fig.get('placeholder_lines', 2))
        add_para(doc, fig['caption'],
                 alignment=WD_ALIGN_PARAGRAPH.CENTER, line_spacing=1.2,
                 ascii_font='黑体', east_asia='黑体', size=Pt(10.5))

    # 添加代码块(用于3.4等章节的编码实现)
    if 'code_blocks' in section:
        for block in section['code_blocks']:
            add_para(doc, block['title'],
                     first_line_indent=Cm(0.77), line_spacing=1.2, size=Pt(12))
            add_para(doc, block['desc'],
                     first_line_indent=Cm(0.77), line_spacing=1.2, size=Pt(12))
            for seg in block.get('segments', []):
                add_para(doc, seg['label'],
                         first_line_indent=Cm(0.77), line_spacing=1.2, size=Pt(12))
                for line in seg['lines']:
                    add_para(doc, line,
                             first_line_indent=Cm(0.77), line_spacing=1.0,
                             ascii_font='Times New Roman', east_asia='Times New Roman',
                             size=Pt(10.5))
            add_empty_para(doc, 1)

    # 递归处理子章节
    for sub in section.get('subsections', []):
        build_section(doc, sub, heading_level + 1)


def build_chapter1(doc, cfg):
    """构建第1章"""
    ch = cfg['chapters']['chapter1']
    p = doc.add_paragraph(style='Heading 1')
    p.add_run(ch['title'])

    for section in ch['sections']:
        build_section(doc, section, heading_level=2)


def build_chapter2(doc, cfg):
    """构建第2章 系统设计"""
    ch = cfg['chapters']['chapter2']

    # 章标题
    p = doc.add_paragraph(style='Heading 1')
    p.add_run(ch['title'])

    # 2.1 功能模块设计
    p = doc.add_paragraph(style='Heading 2')
    p.paragraph_format.first_line_indent = Cm(0)
    p.add_run('2.1 系统功能模块设计')

    # 功能模块描述段落
    for text in ch['modules']:
        add_para(doc, text,
                 alignment=WD_ALIGN_PARAGRAPH.JUSTIFY,
                 first_line_indent=Cm(0.85), line_spacing=1.2, size=Pt(12))

    # 功能模块图（支持实际图片或占位空行）
    fig = ch.get('module_figure', {})
    if 'image_path' in fig:
        p_img = doc.add_paragraph()
        p_img.alignment = WD_ALIGN_PARAGRAPH.CENTER
        run_img = p_img.add_run()
        img_width = fig.get('image_width_cm', 14)
        run_img.add_picture(fig['image_path'], width=Cm(img_width))
    else:
        add_empty_para(doc, fig.get('placeholder_lines', 9))
    add_para(doc, fig.get('caption', ''),
             alignment=WD_ALIGN_PARAGRAPH.CENTER, line_spacing=1.2,
             ascii_font='黑体', east_asia='黑体', size=Pt(10.5))
    add_empty_para(doc, 1)

    # 2.2 数据库设计
    p = doc.add_paragraph(style='Heading 2')
    p.paragraph_format.first_line_indent = Cm(0)
    p.add_run('2.2 数据库设计')

    add_para(doc, ch['intro'],
             alignment=WD_ALIGN_PARAGRAPH.JUSTIFY,
             first_line_indent=Cm(0.85), line_spacing=1.2, size=Pt(12))

    # 遍历数据库表
    for tbl in ch['db_tables']:
        # 表标签(如"1．系统用户表")
        add_para(doc, tbl['label'],
                 first_line_indent=Cm(0.85), line_spacing=1.2, size=Pt(12))
        # 表介绍
        add_para(doc, tbl['intro'],
                 first_line_indent=Cm(0.85), line_spacing=1.2, size=Pt(12))
        add_empty_para(doc, 1)
        # 表标题
        add_para(doc, tbl['title'],
                 alignment=WD_ALIGN_PARAGRAPH.CENTER, line_spacing=1.2,
                 ascii_font='黑体', east_asia='黑体', size=Pt(10.5))
        # 创建表格
        create_data_table(doc, tbl['headers'], tbl['rows'],
                          tbl['col_widths'],
                          border_h_sz=tbl.get('border_h_sz', '6'))
        # 续表处理
        if 'continued_title' in tbl:
            add_empty_para(doc, 1)
            add_para(doc, tbl['continued_title'],
                     alignment=WD_ALIGN_PARAGRAPH.RIGHT, line_spacing=1.2, size=Pt(12))
            create_data_table_no_header(doc, tbl['continued_rows'],
                                        tbl['col_widths'],
                                        border_h_sz=tbl.get('border_h_sz', '6'))
        add_empty_para(doc, 1)


def build_chapter3(doc, cfg):
    """构建第3章"""
    ch = cfg['chapters']['chapter3']
    p = doc.add_paragraph(style='Heading 1')
    p.add_run(ch['title'])

    for section in ch['sections']:
        build_section(doc, section, heading_level=2)


def build_chapter4(doc, cfg):
    """构建第4章 系统测试"""
    ch = cfg['chapters']['chapter4']

    p = doc.add_paragraph(style='Heading 1')
    p.add_run(ch['title'])

    # 章引言(使用Body Text样式)
    add_para(doc, ch['intro'],
             alignment=WD_ALIGN_PARAGRAPH.JUSTIFY,
             first_line_indent=Cm(0.85), line_spacing=1.2,
             space_after=Pt(6.0),
             ascii_font='宋体', east_asia='宋体', size=Pt(12))

    for section in ch['sections']:
        # 标题
        p = doc.add_paragraph(style='Heading 2')
        p.paragraph_format.first_line_indent = Cm(0)
        p.add_run(section['heading'])

        # 段落
        for text in section.get('paragraphs', []):
            add_para(doc, text,
                     alignment=WD_ALIGN_PARAGRAPH.JUSTIFY,
                     first_line_indent=Cm(0.85), line_spacing=1.2, size=Pt(12))

        # 测试结果表
        if 'test_table' in section:
            tt = section['test_table']
            # 表标题
            add_para(doc, tt['title'],
                     alignment=WD_ALIGN_PARAGRAPH.CENTER, line_spacing=1.2,
                     ascii_font='黑体', east_asia='黑体', size=Pt(10.5))
            # 主表
            create_data_table(doc, tt['headers'], tt['rows'],
                              tt['col_widths'], border_h_sz='4', border_v_sz='4')
            add_empty_para(doc, 2)
            # 续表
            if 'continued_title' in tt:
                add_para(doc, tt['continued_title'],
                         alignment=WD_ALIGN_PARAGRAPH.RIGHT, line_spacing=1.2)
                add_empty_para(doc, 1)
                create_data_table_no_header(doc, tt['continued_rows'],
                                            tt['col_widths'], border_h_sz='4', border_v_sz='4')
            add_empty_para(doc, 2)


# ============================================================
# 结论
# ============================================================

def build_conclusion(doc, cfg):
    """生成结论"""
    conc = cfg['conclusion']

    p = doc.add_paragraph(style='Heading 1')
    p.add_run(conc['title'])

    add_para(doc, conc['intro'],
             first_line_indent=Cm(0.85), line_spacing=1.2, size=Pt(12))

    for func in conc['functions']:
        add_para(doc, func,
                 first_line_indent=Cm(0.85), line_spacing=1.2, size=Pt(12))

    add_para(doc, conc['tech_intro'],
             first_line_indent=Cm(0.85), line_spacing=1.2, size=Pt(12))

    for tech in conc['technologies']:
        add_para(doc, tech,
                 first_line_indent=Cm(0.85), line_spacing=1.2, size=Pt(12))

    add_para(doc, conc['limitation_intro'],
             first_line_indent=Cm(0.85), line_spacing=1.2, size=Pt(12))

    for lim in conc['limitations']:
        add_para(doc, lim,
                 first_line_indent=Cm(0.85), line_spacing=1.2, size=Pt(12))

    add_empty_para(doc, 4)


# ============================================================
# 参考文献
# ============================================================

def build_references(doc, cfg):
    """生成参考文献"""
    refs = cfg['references']

    p = doc.add_paragraph(style='Heading 1')
    p.add_run(refs['title'])

    add_empty_para(doc, 2)

    for item in refs['items']:
        add_para(doc, item,
                 alignment=WD_ALIGN_PARAGRAPH.LEFT,
                 first_line_indent=Cm(-0.64), left_indent=Cm(0.64),
                 line_spacing=1.2, size=Pt(12))

    if refs.get('note'):
        add_para(doc, refs['note'],
                 first_line_indent=Cm(-0.64), left_indent=Cm(0.64),
                 line_spacing=1.2, size=Pt(12))
    if refs.get('note2'):
        add_para(doc, '    ' + refs['note2'],
                 first_line_indent=Cm(-0.64), left_indent=Cm(0.64),
                 line_spacing=1.2, size=Pt(12))


# ============================================================
# 附录
# ============================================================

def build_meeting_table(doc, meeting, apx):
    """生成会议记录表格"""
    data = [
        ['开发小组', apx['team_name'], '指导教师', apx['instructor']],
        ['团队组长', apx['leader'], '记录人员', apx['recorder']],
        ['会议主题', meeting['subject'], '会议地点', apx['location']],
        ['会议时间', meeting['date'], '记录时间', meeting['date']],
        ['团队组员', apx['members'], '记录形式', '文档'],
    ]

    # 构建完整行数据
    rows_data = []
    # 前5行: [label1, value1, label2, value2]
    rows_data.extend(data)
    # 会议内容概要标题
    rows_data.append(['会议内容概要', None, None, None])
    for item in meeting['content_summary']:
        rows_data.append([item[0] if item[0].isdigit() else '', item])
    # 会议详情标题
    rows_data.append(['会议详情', None, None, None])
    for i, detail in enumerate(meeting['content_details']):
        rows_data.append([str(i+1), detail])
    # 沟通情况标题
    rows_data.append(['小组沟通情况总结', None, None, None])
    for i, item in enumerate(meeting['communication_summary']):
        rows_data.append([str(i+1), item])
    # 纪律情况标题
    rows_data.append(['纪律情况总结', None, None, None])
    for i, item in enumerate(meeting['discipline_summary']):
        rows_data.append([str(i+1), item])

    table = doc.add_table(rows=len(rows_data), cols=5)
    table.alignment = WD_TABLE_ALIGNMENT.CENTER
    set_table_grid(table, [675, 851, 3827, 1276, 1893])
    set_table_borders(table, top_sz='4', bottom_sz='4',
                      inside_h_sz='4', inside_v_sz='4',
                      left_val='single', right_val='single')

    title_row_indices = set()
    idx = 5
    title_row_indices.add(idx)  # 会议内容概要
    idx += 1 + len(meeting['content_summary'])
    title_row_indices.add(idx)  # 会议详情
    idx += 1 + len(meeting['content_details'])
    title_row_indices.add(idx)  # 小组沟通情况总结
    idx += 1 + len(meeting['communication_summary'])
    title_row_indices.add(idx)  # 纪律情况总结

    for r_idx, row_data in enumerate(rows_data):
        row = table.rows[r_idx]
        if r_idx < 5:
            set_cell_text(row.cells[0], row_data[0],
                          alignment=WD_ALIGN_PARAGRAPH.CENTER)
            set_cell_text(row.cells[2], row_data[1],
                          alignment=WD_ALIGN_PARAGRAPH.LEFT)
            set_cell_text(row.cells[3], row_data[2],
                          alignment=WD_ALIGN_PARAGRAPH.CENTER)
            set_cell_text(row.cells[4], row_data[3],
                          alignment=WD_ALIGN_PARAGRAPH.CENTER)
            merge_cells_h(row, 0, 2)
        elif r_idx in title_row_indices:
            set_cell_text(row.cells[0], row_data[0],
                          alignment=WD_ALIGN_PARAGRAPH.CENTER)
            merge_cells_h(row, 0, 5)
        else:
            set_cell_text(row.cells[0], row_data[0],
                          alignment=WD_ALIGN_PARAGRAPH.CENTER)
            set_cell_text(row.cells[1], row_data[1],
                          alignment=WD_ALIGN_PARAGRAPH.LEFT)
            merge_cells_h(row, 1, 4)


def build_grade_table(doc, cfg):
    """生成成绩评价表"""
    gt = cfg['appendix']['grade_table']

    # 展开评价项(处理子项)
    eval_rows = []
    for item in gt['eval_items']:
        eval_rows.append({
            'name': item['name'],
            'max_score': item['max_score'],
            'a_standard': item['a_standard'],
            'c_standard': item['c_standard'],
            'is_main': True
        })
        for sub in item.get('sub_items', []):
            eval_rows.append({
                'name': '',
                'max_score': '',
                'a_standard': sub['a_standard'],
                'c_standard': sub['c_standard'],
                'is_main': False
            })

    total_rows = 5 + len(eval_rows) + 2  # 表头4行 + 评价行 + 总分行 + 评语行
    table = doc.add_table(rows=total_rows, cols=9)
    table.alignment = WD_TABLE_ALIGNMENT.CENTER
    set_table_grid(table, [1134, 1277, 849, 1418, 1134, 1276, 799, 736, 681])
    set_table_borders(table, top_sz='4', bottom_sz='4',
                      inside_h_sz='4', inside_v_sz='4',
                      left_val='single', right_val='single')

    # Row 0: 课程名称
    set_cell_text(table.rows[0].cells[0], '课程名称',
                  alignment=WD_ALIGN_PARAGRAPH.CENTER)
    set_cell_text(table.rows[0].cells[1], gt['course_name'],
                  alignment=WD_ALIGN_PARAGRAPH.CENTER)
    merge_cells_h(table.rows[0], 1, 8)

    # Row 1: 题目名称
    set_cell_text(table.rows[1].cells[0], '题目名称',
                  alignment=WD_ALIGN_PARAGRAPH.CENTER)
    set_cell_text(table.rows[1].cells[1], gt['subject'],
                  alignment=WD_ALIGN_PARAGRAPH.CENTER)
    merge_cells_h(table.rows[1], 1, 8)

    # Row 2: 学生信息
    r2 = ['学生姓名', gt['eval_student_name'], '学号', gt['eval_student_id'],
          '指导教师姓名', gt['eval_instructor'], '职称', gt['eval_instructor_title']]
    for i, text in enumerate(r2):
        set_cell_text(table.rows[2].cells[i], text,
                      alignment=WD_ALIGN_PARAGRAPH.CENTER)
    merge_cells_h(table.rows[2], 7, 2)

    # Row 3: 表头
    set_cell_text(table.rows[3].cells[0], '评价项目',
                  alignment=WD_ALIGN_PARAGRAPH.CENTER)
    set_cell_text(table.rows[3].cells[1], '评分标准',
                  alignment=WD_ALIGN_PARAGRAPH.CENTER)
    merge_cells_h(table.rows[3], 1, 6)
    set_cell_text(table.rows[3].cells[7], '满分',
                  alignment=WD_ALIGN_PARAGRAPH.CENTER)
    set_cell_text(table.rows[3].cells[8], '评分',
                  alignment=WD_ALIGN_PARAGRAPH.CENTER)

    # Row 4: A/C子表头
    set_cell_text(table.rows[4].cells[0], '评价项目',
                  alignment=WD_ALIGN_PARAGRAPH.CENTER)
    set_cell_text(table.rows[4].cells[1], 'A',
                  alignment=WD_ALIGN_PARAGRAPH.CENTER)
    merge_cells_h(table.rows[4], 1, 3)
    set_cell_text(table.rows[4].cells[4], 'C',
                  alignment=WD_ALIGN_PARAGRAPH.CENTER)
    merge_cells_h(table.rows[4], 4, 3)
    set_cell_text(table.rows[4].cells[7], '满分',
                  alignment=WD_ALIGN_PARAGRAPH.CENTER)
    set_cell_text(table.rows[4].cells[8], '评分',
                  alignment=WD_ALIGN_PARAGRAPH.CENTER)

    # Row 5~: 评价内容
    for i, ev in enumerate(eval_rows):
        row_idx = 5 + i
        row = table.rows[row_idx]

        if ev['is_main']:
            set_cell_text(row.cells[0], ev['name'],
                          alignment=WD_ALIGN_PARAGRAPH.CENTER)
            set_vmerge(row.cells[0], 'restart')
        else:
            set_vmerge(row.cells[0], 'continue')

        set_cell_text(row.cells[1], ev['a_standard'],
                      alignment=WD_ALIGN_PARAGRAPH.JUSTIFY)
        merge_cells_h(row, 1, 3)

        set_cell_text(row.cells[4], ev['c_standard'],
                      alignment=WD_ALIGN_PARAGRAPH.JUSTIFY)
        merge_cells_h(row, 4, 3)

        if ev['is_main']:
            set_cell_text(row.cells[7], ev['max_score'],
                          alignment=WD_ALIGN_PARAGRAPH.CENTER)
            set_vmerge(row.cells[7], 'restart')
        else:
            set_vmerge(row.cells[7], 'continue')

        if ev['is_main']:
            set_vmerge(row.cells[8], 'restart')
        else:
            set_vmerge(row.cells[8], 'continue')

    # 总分行
    total_row = 5 + len(eval_rows)
    set_cell_text(table.rows[total_row].cells[0], '总分',
                  alignment=WD_ALIGN_PARAGRAPH.CENTER)
    merge_cells_h(table.rows[total_row], 1, 8)

    # 评语行
    comment_row = total_row + 1
    set_cell_text(table.rows[comment_row].cells[0],
                  gt['conclusion_text'],
                  alignment=WD_ALIGN_PARAGRAPH.JUSTIFY)
    merge_cells_h(table.rows[comment_row], 0, 9)


def build_appendix(doc, cfg):
    """生成附录"""
    apx = cfg['appendix']

    for meeting in apx['meetings']:
        # 附录标题
        p = doc.add_paragraph(style='Heading 1')
        p.alignment = WD_ALIGN_PARAGRAPH.LEFT
        p.add_run(meeting['title'])

        add_empty_para(doc, 1)
        add_para(doc, meeting['doc_title'],
                 alignment=WD_ALIGN_PARAGRAPH.CENTER, size=Pt(12))
        add_empty_para(doc, 2)

        build_meeting_table(doc, meeting, apx)
        add_empty_para(doc, 2)

    # 成绩评价表标题
    gt = apx['grade_table']
    add_para(doc, gt['title'],
             alignment=WD_ALIGN_PARAGRAPH.CENTER, line_spacing=2.0, size=Pt(12))
    add_para(doc, f"指导教师：                                              {apx['grade_date']}",
             alignment=WD_ALIGN_PARAGRAPH.CENTER, line_spacing=2.0, size=Pt(12))

    build_grade_table(doc, cfg)


# ============================================================
# 目录更新(可选)
# ============================================================

def update_toc_with_word(docx_path):
    """
    使用Word COM接口更新目录域和所有域
    需要安装pywin32和Microsoft Word
    如果不可用则跳过(用户可手动右键更新域)
    """
    try:
        import win32com.client
        word = win32com.client.Dispatch("Word.Application")
        word.Visible = False
        doc = word.Documents.Open(docx_path)

        for toc in doc.TablesOfContents:
            toc.Update()
        doc.Fields.Update()

        doc.Save()
        doc.Close()
        word.Quit()
        print("[OK] 目录域已自动更新")
        return True
    except ImportError:
        print("[SKIP] pywin32未安装，请手动用Word打开后右键更新目录域")
        return False
    except Exception as e:
        print(f"[WARN] 目录更新失败: {e}，请手动用Word打开后右键更新目录域")
        return False


# ============================================================
# 主函数
# ============================================================

def generate(config_path, output_path=None):
    """
    主入口: 读取配置文件，生成完整论文docx

    参数:
        config_path: JSON配置文件路径
        output_path: 输出docx路径(可选)
    """
    # 读取配置
    with open(config_path, 'r', encoding='utf-8') as f:
        cfg = json.load(f)

    # 默认输出路径
    if output_path is None:
        stu = cfg['student']
        subject = cfg['task_page']['subject']
        filename = f"报告-{stu['class_name']}-{stu['name']}-{subject}.docx"
        output_path = os.path.join(os.path.dirname(config_path), filename)

    # 创建文档
    doc = Document()

    # 样式与页面
    setup_styles(doc)
    setup_page(doc, cfg)

    # 封面
    build_cover(doc, cfg)

    # 任务书
    build_task_page(doc, cfg)

    # 目录
    build_toc(doc)

    # 正文四章
    build_chapter1(doc, cfg)
    build_chapter2(doc, cfg)
    build_chapter3(doc, cfg)
    build_chapter4(doc, cfg)

    # 结论
    build_conclusion(doc, cfg)

    # 参考文献
    build_references(doc, cfg)

    # 附录
    build_appendix(doc, cfg)

    # 保存
    doc.save(output_path)
    print(f"[OK] 文档生成完成: {output_path}")
    print(f"     段落数: {len(doc.paragraphs)}, 表格数: {len(doc.tables)}")

    # 自动更新目录
    update_toc_with_word(output_path)

    return output_path


if __name__ == '__main__':
    # 命令行入口
    if len(sys.argv) < 2:
        print("用法: python generate_thesis.py <config.json> [输出路径]")
        print("示例: python generate_thesis.py config_template.json output.docx")
        sys.exit(1)

    config_path = sys.argv[1]
    output_path = sys.argv[2] if len(sys.argv) > 2 else None

    if not os.path.exists(config_path):
        print(f"错误: 配置文件不存在: {config_path}")
        sys.exit(1)

    generate(config_path, output_path)
