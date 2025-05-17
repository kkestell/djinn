using System.CommandLine.Invocation;
using Music.Configuration;

namespace Music.Commands.Handlers;

public class StatsCommandHandler : ICommandHandler
{
    public Task<int> InvokeAsync(InvocationContext context)
    {
        return Task.FromResult(Invoke(context));
    }

    public int Invoke(InvocationContext context)
    {
        var config = MusicConfig.Load();
        
        var libraryDirectory = new DirectoryInfo(config.LibraryPath);
        var artistDirectories = libraryDirectory.EnumerateDirectories().ToList();
        var numArtists = artistDirectories.Count;


        var numAlbums = 0;
        var numTracks = 0;
        
        Console.CursorVisible = false;

        foreach (var artistDirectory in artistDirectories)
        {
            var albumDirectories = artistDirectory.EnumerateDirectories();
            foreach (var albumDirectory in albumDirectories)
            {
                numAlbums++;
                numTracks += albumDirectory.EnumerateFiles().Count(file => file.Extension.Equals(".mp3", StringComparison.OrdinalIgnoreCase) || file.Extension.Equals(".flac", StringComparison.OrdinalIgnoreCase));
            }

            Console.CursorLeft = 0;
            Console.Write($"Artists: {numArtists}, Albums: {numAlbums}, Tracks: {numTracks}");
        }

        Console.WriteLine();

        Console.CursorVisible = true;
        
        return 0;
    }
}
