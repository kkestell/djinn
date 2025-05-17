using System.Text;
using Wcwidth;

namespace Music.Utils;

public enum Alignment
{
    Left,
    Right
}

public class FixedWidthStringBuilder
{
    private readonly StringBuilder _stringBuilder = new();

    private static int GetDisplayWidth(string str) => str.Sum(c => UnicodeCalculator.GetWidth(c));

    private static string TruncateToDisplayWidth(string str, int maxWidth)
    {
        var currentWidth = 0;
        var charCount = 0;
        
        foreach (var c in str)
        {
            var charWidth = UnicodeCalculator.GetWidth(c);
            if (currentWidth + charWidth > maxWidth)
                break;
            
            currentWidth += charWidth;
            charCount++;
        }
        
        return str[..charCount];
    }

    public void Append(string? value, int? width = null, Alignment align = Alignment.Left)
    {
        if (value is null)
            return;
        
        var displayWidth = GetDisplayWidth(value);
        var finalWidth = width ?? displayWidth;
        
        if (displayWidth > finalWidth)
        {
            if (finalWidth < 4)
            {
                value = TruncateToDisplayWidth(value, finalWidth);
            }
            else
            {
                value = TruncateToDisplayWidth(value, finalWidth - 1) + "…";
            }
            
            displayWidth = GetDisplayWidth(value);
        }
        
        var padding = finalWidth - displayWidth;
        var formattedValue = align switch
        {
            Alignment.Left => value + new string(' ', padding),
            Alignment.Right => new string(' ', padding) + value,
            _ => string.Empty
        };
        
        _stringBuilder.Append(formattedValue);
    }
    
    public override string ToString() => _stringBuilder.ToString();
}