using System.CommandLine.Invocation;
using Djinn.Configuration;
using Djinn.Services;
using File = TagLib.File;

namespace Djinn.Commands.Handlers;

internal record ScanResult(string ArtistName, Guid ReleaseId);

public class UpgradeCommandHandler : ICommandHandler
{
    public async Task<int> InvokeAsync(InvocationContext context)
    {
        var config = DjinnConfig.Load();
        var verbose = context.ParseResult.GetValueForOption(UpgradeCommand.Verbose);
        var randomize = context.ParseResult.GetValueForOption(UpgradeCommand.Randomize);
        if (verbose)
            Log.Level = LogLevel.Verbose;
        var musicBrainzService = new MusicBrainzService();
        var coverArtDownloader = new CoverArtDownloader(config);
        var metadataUpdater = new MetadataService(config);
        var sourceDownloader = new SourceDownloader(config);
        var albumDownloader = new AlbumDownloader(config, coverArtDownloader, metadataUpdater, sourceDownloader);
        foreach (var scanResult in GetReleaseIds(new DirectoryInfo(config.LibraryPath), randomize))
        {
            try
            {
                var album = await musicBrainzService.FindAlbum(scanResult.ReleaseId);
                
                if (album is null)
                {
                    Log.Error($"Unable to find album {scanResult.ReleaseId}");
                    continue;
                }
                
                await albumDownloader.Download(album, true, new List<string> { ".flac" });
            }
            catch(Exception e)
            {
                Log.Error(e, $"Error downloading {scanResult.ArtistName} - {scanResult.ReleaseId}");
            }
        }

        return 0;
    }

    public int Invoke(InvocationContext context)
    {
        throw new NotImplementedException();
    }

    private static IEnumerable<ScanResult> GetReleaseIds(DirectoryInfo libraryDirectory, bool randomize)
    {
        var random = new Random();
        var artistDirectories = libraryDirectory.EnumerateDirectories();
        if (randomize)
            artistDirectories = artistDirectories.OrderBy(_ => random.Next());
        foreach (var artistDirectory in artistDirectories)
        {
            var artistName = artistDirectory.Name;
            var albumDirectories = artistDirectory.EnumerateDirectories();
            if (randomize)
                albumDirectories = albumDirectories.OrderBy(_ => random.Next());
            foreach (var albumDirectory in albumDirectories)
            {
                var firstFile = albumDirectory.EnumerateFiles("*.mp3").FirstOrDefault();
                if (firstFile is null)
                    continue;
                var releaseId = Guid.Empty;
                try
                {
                    var tags = File.Create(firstFile.FullName);
                    var id = tags.Tag.MusicBrainzReleaseId;
                    if (!Guid.TryParse(id, out releaseId))
                    {
                        Log.Verbose($"Unable to parse release ID {id}");
                        continue;
                    }
                }
                catch (Exception e)
                {
                    Log.Error(e, $"Error reading tags for {firstFile.FullName}");
                }

                yield return new ScanResult(artistName, releaseId);
            }
        }
    }
}