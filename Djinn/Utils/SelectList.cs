using Wcwidth;

namespace Djinn.Utils;

public class SelectList
{
    private readonly IReadOnlyList<string> _items;

    public SelectList(IReadOnlyList<string> items)
    {
        _items = items;
    }

    private static int GetDisplayWidth(string str)
    {
        return str.Sum(c => UnicodeCalculator.GetWidth(c));
    }

    public int? Show(string title)
    {
        var selected = false;
        var currentIndex = 0;
        var scrollOffset = 0;
        var maxVisible = Console.WindowHeight;
        
        Console.CursorVisible = false;
        
        // Switch to alternate screen buffer
        Console.Write("\x1b[?1049h");
        
        while (!selected)
        {
            Console.Clear();
            
            Console.SetCursorPosition(0, 0);
            Console.WriteLine(title);
            
            if (currentIndex < scrollOffset)
                scrollOffset = currentIndex;
            
            if (currentIndex >= scrollOffset + maxVisible - 1)
                scrollOffset = currentIndex - maxVisible + 2;
            
            var endIndex = Math.Min(scrollOffset + maxVisible - 1, _items.Count);
            
            for (var i = scrollOffset; i < endIndex; i++)
            {
                var lineIndex = i - scrollOffset + 1;
                Console.SetCursorPosition(0, lineIndex);
                
                if (i == currentIndex)
                    Console.ForegroundColor = ConsoleColor.Cyan;
                else
                    Console.ForegroundColor = ConsoleColor.Gray;
                
                Console.Write(i == currentIndex ? "> " : "  ");
                
                var item = _items[i];
                Console.Write(item);
                
                var displayWidth = GetDisplayWidth(item);
                var prefixWidth = 2;
                var remainingSpace = Console.WindowWidth - displayWidth - prefixWidth;
                
                if (remainingSpace > 0)
                    Console.Write(new string(' ', remainingSpace));
                
                Console.ResetColor();
            }
            
            var key = Console.ReadKey(true).Key;
            
            switch (key)
            {
                case ConsoleKey.UpArrow:
                    if (currentIndex > 0)
                        currentIndex--;
                    break;
                    
                case ConsoleKey.DownArrow:
                    if (currentIndex < _items.Count - 1)
                        currentIndex++;
                    break;
                    
                case ConsoleKey.PageUp:
                    currentIndex = Math.Max(0, currentIndex - (maxVisible - 1));
                    break;
                    
                case ConsoleKey.PageDown:
                    currentIndex = Math.Min(_items.Count - 1, currentIndex + (maxVisible - 1));
                    break;
                    
                case ConsoleKey.Home:
                    currentIndex = 0;
                    scrollOffset = 0;
                    break;
                    
                case ConsoleKey.End:
                    currentIndex = _items.Count - 1;
                    break;
                    
                case ConsoleKey.Enter:
                    selected = true;
                    break;
                
                case ConsoleKey.Escape:
                    Console.Clear();
                    Console.CursorVisible = true;
                    return null;
            }
        }
        
        // Switch back to normal screen buffer
        Console.Write("\x1b[?1049l");

        Console.CursorVisible = true;
        
        return currentIndex;
    }
}