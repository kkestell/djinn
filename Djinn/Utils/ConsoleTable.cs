using System.Text;
using Wcwidth;

namespace Djinn.Utils;

public enum ConsoleTableColumnAlignment
{
    Left,
    Center,
    Right
}

public enum ConsoleTableColumnSizing
{
    Fit,
    Grow
}

public record ConsoleTableColumn(
    ConsoleTableColumnAlignment Alignment,
    ConsoleTableColumnSizing Sizing = ConsoleTableColumnSizing.Fit);

public class ConsoleTable
{
    private readonly List<ConsoleTableColumn> _columns;
    private readonly List<List<string>> _data;

    public ConsoleTable(List<ConsoleTableColumn> columns, List<List<string>> data)
    {
        _columns = columns;
        _data = data;
    }

    private static int GetDisplayWidth(string str) => str.Sum(c => UnicodeCalculator.GetWidth(c));

    public IReadOnlyList<string> Render(int width)
    {
        var result = new List<string>();
        
        // Calculate initial column widths based on the longest item in each column
        var columnWidths = new int[_columns.Count];
        for (var colIndex = 0; colIndex < _columns.Count; colIndex++)
        {
            columnWidths[colIndex] = _data
                .Select(row => colIndex < row.Count ? GetDisplayWidth(row[colIndex]) : 0)
                .DefaultIfEmpty(0)
                .Max();
        }
        
        // Calculate the total width needed and available space
        var borderWidth = _columns.Count + 1; // Borders between columns plus start and end
        var totalContentWidth = columnWidths.Sum();
        var availableWidth = width - borderWidth;
        
        // Identify columns with Grow sizing
        var growColumns = Enumerable.Range(0, _columns.Count)
            .Where(i => _columns[i].Sizing == ConsoleTableColumnSizing.Grow)
            .ToList();
            
        if (totalContentWidth > availableWidth)
        {
            // Shrink columns if necessary to fit available width
            // Calculate excess width that needs to be removed
            var excessWidth = totalContentWidth - availableWidth;
            
            // Shrink columns proportionally to their width
            // Wider columns will be shrunk more than narrower ones
            var totalWidthForShrinking = columnWidths.Sum();
            var remainingExcess = excessWidth;
            
            for (var i = 0; i < columnWidths.Length; i++)
            {
                // Calculate proportional shrink for this column
                var shrinkAmount = (int)Math.Ceiling(excessWidth * ((double)columnWidths[i] / totalWidthForShrinking));
                
                // Avoid shrinking below 1
                shrinkAmount = Math.Min(shrinkAmount, columnWidths[i] - 1);
                shrinkAmount = Math.Min(shrinkAmount, remainingExcess);
                
                columnWidths[i] -= shrinkAmount;
                remainingExcess -= shrinkAmount;
                
                if (remainingExcess <= 0)
                    break;
            }
            
            // If we still have excess width to remove, take one from each column
            // starting with the widest until we fit
            if (remainingExcess > 0)
            {
                var columnsOrderedByWidth = Enumerable.Range(0, columnWidths.Length)
                    .OrderByDescending(i => columnWidths[i])
                    .ToList();
                    
                foreach (var colIndex in columnsOrderedByWidth)
                {
                    if (columnWidths[colIndex] > 1)
                    {
                        columnWidths[colIndex]--;
                        remainingExcess--;
                    }
                    
                    if (remainingExcess <= 0)
                        break;
                }
            }
        }
        else if (totalContentWidth < availableWidth && growColumns.Count > 0)
        {
            // Expand "Grow" columns to fill available space
            var extraSpace = availableWidth - totalContentWidth;
            
            // Calculate the total current width of all growing columns
            var totalGrowWidth = growColumns.Sum(i => columnWidths[i]);
            
            // Distribute extra space proportionally among growing columns
            var remainingExtra = extraSpace;
            
            foreach (var colIndex in growColumns)
            {
                // Calculate proportional growth for this column
                var growAmount = (int)Math.Floor(extraSpace * ((double)columnWidths[colIndex] / totalGrowWidth));
                
                // Ensure we don't exceed available extra space
                growAmount = Math.Min(growAmount, remainingExtra);
                
                columnWidths[colIndex] += growAmount;
                remainingExtra -= growAmount;
            }
            
            // Distribute any remaining pixels one by one to growing columns
            var growColIndex = 0;
            while (remainingExtra > 0 && growColumns.Count > 0)
            {
                columnWidths[growColumns[growColIndex]]++;
                remainingExtra--;
                
                growColIndex = (growColIndex + 1) % growColumns.Count;
            }
        }
        
        // Render each row
        foreach (var row in _data)
        {
            var renderedRow = new StringBuilder();
            
            // Start the row with a border character
            renderedRow.Append('|');
            
            for (var colIndex = 0; colIndex < _columns.Count; colIndex++)
            {
                var content = colIndex < row.Count ? row[colIndex] : string.Empty;
                var alignment = _columns[colIndex].Alignment;
                var columnWidth = columnWidths[colIndex];
                
                // Truncate content if it's too long
                if (GetDisplayWidth(content) > columnWidth)
                {
                    content = TruncateString(content, columnWidth);
                }
                
                // Pad content based on alignment
                var displayWidth = GetDisplayWidth(content);
                var paddingNeeded = columnWidth - displayWidth;
                
                var leftPad = alignment switch
                {
                    ConsoleTableColumnAlignment.Left => 0,
                    ConsoleTableColumnAlignment.Center => paddingNeeded / 2,
                    ConsoleTableColumnAlignment.Right => paddingNeeded,
                    _ => 0
                };
                
                var rightPad = paddingNeeded - leftPad;
                
                renderedRow.Append(new string(' ', leftPad));
                renderedRow.Append(content);
                renderedRow.Append(new string(' ', rightPad));
                
                // Add a border after each cell
                renderedRow.Append('|');
            }
            
            result.Add(renderedRow.ToString());
        }
        
        return result;
    }

    private string TruncateString(string str, int maxWidth)
    {
        if (GetDisplayWidth(str) <= maxWidth)
            return str;
            
        var result = new StringBuilder();
        var currentWidth = 0;
        
        foreach (var c in str)
        {
            var charWidth = UnicodeCalculator.GetWidth(c);
            if (currentWidth + charWidth > maxWidth)
                break;
                
            result.Append(c);
            currentWidth += charWidth;
        }
        
        return result.ToString();
    }
}