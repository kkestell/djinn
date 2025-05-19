using Wcwidth;

namespace Djinn.Services;

internal enum LogFormat
{
    Plain,
    Fancy
}

internal enum LogLevel
{
    Debug,
    Verbose,
    Information,
    Warning,
    Error,
    Silent
}

internal static class Log
{
    private static readonly Lock LockObj = new();

    public static LogLevel Level { get; set; } = LogLevel.Information;
    
    public static LogFormat Format { get; set; } = LogFormat.Fancy;

    private static int GetDisplayWidth(string str) => str.Sum(c => UnicodeCalculator.GetWidth(c));

    private static string TruncateByDisplayWidth(string text, int maxWidth)
    {
        var currentWidth = 0;
        var i = 0;
            
        for (; i < text.Length; i++)
        {
            var charWidth = UnicodeCalculator.GetWidth(text[i]);
            if (currentWidth + charWidth > maxWidth)
                break;
                
            currentWidth += charWidth;
        }
            
        return text.Substring(0, i);
    }

    private static string PadRightByDisplayWidth(string text, int totalWidth)
    {
        var displayWidth = GetDisplayWidth(text);
        if (displayWidth >= totalWidth)
            return text;
                
        return text + new string(' ', totalWidth - displayWidth);
    }

    private static void WriteMessage(string message, bool boxed = false, ConsoleColor? foreground = null, ConsoleColor? background = null)
    {
        if (Format == LogFormat.Plain)
        {
            Console.WriteLine(message);
            return;
        }
        
        if (foreground.HasValue) 
            Console.ForegroundColor = foreground.Value;
        if (background.HasValue) 
            Console.BackgroundColor = background.Value;
        
        if (boxed)
        {
            var width = Console.WindowWidth;
            var contentWidth = width - 2;
                
            Console.WriteLine($"╭{new string('─', contentWidth)}╮");
                
            var lines = message.Split('\n');
            foreach (var line in lines)
            {
                var remaining = line;
                while (!string.IsNullOrEmpty(remaining))
                {
                    var chunk = TruncateByDisplayWidth(remaining, contentWidth);
                    Console.WriteLine($"│{PadRightByDisplayWidth(chunk, contentWidth)}│");
                    remaining = chunk.Length < remaining.Length 
                        ? remaining.Substring(chunk.Length) 
                        : "";
                }
            }
                
            Console.WriteLine($"╰{new string('─', contentWidth)}╯");
        }
        else
        {
            Console.WriteLine(message);
        }
            
        Console.ResetColor();
    }

    public static void Debug(string message)
    {
        if (Level > LogLevel.Debug)
            return;

        lock (LockObj)
        {
            WriteMessage(message, false, ConsoleColor.Black, ConsoleColor.Yellow);
        }
    }
        
    public static void Verbose(string message)
    {
        if (Level > LogLevel.Verbose)
            return;

        lock (LockObj)
        {
            WriteMessage(message, false, ConsoleColor.DarkGray);
        }
    }

    public static void Information(string message)
    {
        if (Level > LogLevel.Information)
            return;

        lock (LockObj)
        {
            WriteMessage(message);
        }
    }

    public static void Success(string message)
    {
        if (Level > LogLevel.Information)
            return;

        lock (LockObj)
        {
            WriteMessage(message, Format == LogFormat.Fancy, ConsoleColor.Green);
        }
    }

    public static void Warning(string message)
    {
        if (Level > LogLevel.Warning)
            return;

        lock (LockObj)
        {
            WriteMessage(message, Format == LogFormat.Fancy, ConsoleColor.Yellow);
        }
    }

    public static void Error(string message)
    {
        if (Level > LogLevel.Error)
            return;

        lock (LockObj)
        {
            WriteMessage(message, Format == LogFormat.Fancy, ConsoleColor.Red);
        }
    }

    public static void Error(Exception exception, string message)
    {
        if (Level > LogLevel.Error)
            return;

        lock (LockObj)
        {
            if (Format == LogFormat.Fancy)
            {
                WriteMessage($"{message}\n{exception}", true, ConsoleColor.Red);
            }
            else
            {
                WriteMessage(message);
                WriteMessage(exception.ToString());
            }
        }
    }

    public static void Divider()
    {
        if (Format == LogFormat.Plain)
            return;
            
        lock (LockObj)
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine(new string('┄', Console.WindowWidth));
            Console.ResetColor();
        }
    }
}