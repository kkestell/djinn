namespace Djinn.Utils;

public class SelectList
{
    private readonly IReadOnlyList<string> _items;

    public SelectList(IReadOnlyList<string> items)
    {
        _items = items.Take(Console.WindowHeight - 2).ToList();
    }

    public int Show(string title)
    {
        var index = 0;
        var selected = false;

        Console.CursorVisible = false;

        Console.WriteLine(title);
        while (!selected)
        {
            for (var i = 0; i < _items.Count; i++)
            {
                var item = _items[i];

                if (i == index)
                {
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine($"> {item}");
                    Console.ResetColor();
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Gray;
                    Console.WriteLine($"  {item}");
                    Console.ResetColor();
                }
            }

            var key = Console.ReadKey(true)
                .Key;

            switch (key)
            {
                case ConsoleKey.UpArrow:
                    index = Math.Max(0, index - 1);
                    Console.CursorTop -= _items.Count;
                    break;
                case ConsoleKey.DownArrow:
                    index = Math.Min(_items.Count - 1, index + 1);
                    Console.CursorTop -= _items.Count;
                    break;
                case ConsoleKey.Enter:
                    selected = true;
                    break;
            }
        }

        Console.CursorVisible = true;

        return index;
    }
}