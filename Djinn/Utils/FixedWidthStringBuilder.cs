using System.Text;

namespace Djinn.Utils;

public enum Alignment
{
    Left,
    Right
}

public class FixedWidthStringBuilder
{
    private readonly StringBuilder _stringBuilder = new();

    public void Append(string? value, int? width = null, Alignment align = Alignment.Left)
    {
        if (value is null)
            return;
        
        var finalWidth = width ?? value.Length;

        if (finalWidth < 4 && value.Length > finalWidth)
        {
            value = value[..finalWidth];
        }
        else if (value.Length > finalWidth)
        {
            value = value[..(finalWidth - 3)] + "...";
        }

        var formattedValue = align switch
        {
            Alignment.Left => value.PadRight(finalWidth),
            Alignment.Right => value.PadLeft(finalWidth),
            _ => string.Empty
        };

        _stringBuilder.Append(formattedValue);
    }


    public override string ToString()
    {
        return _stringBuilder.ToString();
    }
}
