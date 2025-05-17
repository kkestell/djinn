namespace Music.Models;

public class Track(Guid id, int number, string title)
{
    public Guid Id { get; } = id;
    public int Number { get; } = number;
    public string Title { get; } = title;

    public override string ToString()
    {
        return $"{Number:00} - {Title}";
    }
}