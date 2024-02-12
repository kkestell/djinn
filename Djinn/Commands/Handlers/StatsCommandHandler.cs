using System.CommandLine.Invocation;
using Djinn.Configuration;

namespace Djinn.Commands.Handlers;

public class StatsCommandHandler : ICommandHandler
{
    public Task<int> InvokeAsync(InvocationContext context)
    {
        return Task.FromResult(Invoke(context));
    }

    public int Invoke(InvocationContext context)
    {
        var config = DjinnConfig.Load();
        
        var libraryDirectory = new DirectoryInfo(config.LibraryPath);
        var artistDirectories = libraryDirectory.EnumerateDirectories().ToList();
        var numArtists = artistDirectories.Count;

        Console.WriteLine($"Artists: {numArtists}");

        var numAlbums = 0;
        var numTracks = 0;
        
        Console.CursorVisible = false;
        var currentRow = Console.CursorTop;

        foreach (var artistDirectory in artistDirectories)
        {
            var albumDirectories = artistDirectory.EnumerateDirectories();
            foreach (var albumDirectory in albumDirectories)
            {
                numAlbums++;
                numTracks += albumDirectory.EnumerateFiles().Count(file => file.Extension.Equals(".mp3", StringComparison.OrdinalIgnoreCase) || file.Extension.Equals(".flac", StringComparison.OrdinalIgnoreCase));
            }

            Console.SetCursorPosition(0, currentRow);
            Console.WriteLine($"Albums:  {numAlbums}");
            Console.WriteLine($"Tracks:  {numTracks}");
        }

        Console.CursorVisible = true;
        
        return 0;
    }
}
