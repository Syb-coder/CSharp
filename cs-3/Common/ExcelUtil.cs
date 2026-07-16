using System.Data;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;

namespace CampusMart.Common;

/// <summary>
/// Excel 导出工具类，基于 NPOI 实现 .xlsx 文件导出
/// </summary>
/// <remarks>
/// 与 cs-2 差异化：cs-3 统一封装在 Common/ExcelUtil，采用 NPOI 流式写入。
/// 导出规则（符合 PRD 4.2.7）：
///   - 表头行：加粗、居中、浅蓝色背景
///   - 数据行：自动适配列宽
///   - 文件名规则：{前缀}_YYYYMMDD.xlsx
/// </remarks>
public static class ExcelUtil
{
    /// <summary>
    /// 将 DataTable 数据导出为 Excel 文件
    /// </summary>
    /// <param name="table">待导出的数据表（列名作为表头）</param>
    /// <param name="filePath">导出文件完整路径</param>
    /// <exception cref="ArgumentNullException">参数为空时抛出</exception>
    /// <exception cref="InvalidOperationException">文件写入失败时抛出</exception>
    public static void ExportDataTable(DataTable table, string filePath)
    {
        if (table == null) throw new ArgumentNullException(nameof(table));
        if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentNullException(nameof(filePath));

        // 使用 XSSFWorkbook 对应 .xlsx（Office Open XML）格式
        IWorkbook workbook = new XSSFWorkbook();
        ISheet sheet = workbook.CreateSheet("Sheet1");

        // 表头样式：加粗 + 居中 + 浅蓝色背景
        ICellStyle headerStyle = workbook.CreateCellStyle();
        IFont headerFont = workbook.CreateFont();
        headerFont.IsBold = true;
        headerStyle.SetFont(headerFont);
        // 使用完全限定名避免与 System.Windows.Forms.HorizontalAlignment 歧义
        headerStyle.Alignment = NPOI.SS.UserModel.HorizontalAlignment.Center;
        headerStyle.FillForegroundColor = NPOI.HSSF.Util.HSSFColor.PaleBlue.Index;
        headerStyle.FillPattern = FillPattern.SolidForeground;
        headerStyle.BorderTop = NPOI.SS.UserModel.BorderStyle.Thin;
        headerStyle.BorderBottom = NPOI.SS.UserModel.BorderStyle.Thin;
        headerStyle.BorderLeft = NPOI.SS.UserModel.BorderStyle.Thin;
        headerStyle.BorderRight = NPOI.SS.UserModel.BorderStyle.Thin;

        // 数据行样式：带细边框
        ICellStyle dataStyle = workbook.CreateCellStyle();
        dataStyle.BorderTop = NPOI.SS.UserModel.BorderStyle.Thin;
        dataStyle.BorderBottom = NPOI.SS.UserModel.BorderStyle.Thin;
        dataStyle.BorderLeft = NPOI.SS.UserModel.BorderStyle.Thin;
        dataStyle.BorderRight = NPOI.SS.UserModel.BorderStyle.Thin;

        // 写入表头行
        IRow headerRow = sheet.CreateRow(0);
        for (int i = 0; i < table.Columns.Count; i++)
        {
            ICell cell = headerRow.CreateCell(i);
            cell.SetCellValue(table.Columns[i].ColumnName);
            cell.CellStyle = headerStyle;
        }

        // 写入数据行
        for (int rowIdx = 0; rowIdx < table.Rows.Count; rowIdx++)
        {
            IRow row = sheet.CreateRow(rowIdx + 1);
            DataRow dataRow = table.Rows[rowIdx];
            for (int colIdx = 0; colIdx < table.Columns.Count; colIdx++)
            {
                ICell cell = row.CreateCell(colIdx);
                object value = dataRow[colIdx];
                // null/DBNull 统一写空字符串，避免 NPOI 抛 NullReferenceException
                cell.SetCellValue(value == null || value == DBNull.Value ? string.Empty : value.ToString());
                cell.CellStyle = dataStyle;
            }
        }

        // 自动适配列宽：根据内容长度设置，避免内容被裁切
        // 末参数 false 表示不合并相同列（性能更优），256 为字符宽度单位
        for (int i = 0; i < table.Columns.Count; i++)
        {
            sheet.AutoSizeColumn(i);
        }

        // 写入文件：使用 FileStream 配合 using 确保资源释放
        try
        {
            using (FileStream fs = new FileStream(filePath, FileMode.Create, FileAccess.Write))
            {
                workbook.Write(fs, true);
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Excel 文件写入失败：{ex.Message}", ex);
        }
        finally
        {
            workbook.Close();
        }
    }

    /// <summary>
    /// 生成默认导出文件名（前缀_YYYYMMDD.xlsx）
    /// </summary>
    /// <param name="prefix">文件名前缀，如"商品清单"或"订单列表"</param>
    /// <returns>完整的文件名，如"商品清单_20260711.xlsx"</returns>
    public static string BuildFileName(string prefix)
    {
        return $"{prefix}_{DateTime.Now:yyyyMMdd}.xlsx";
    }
}
