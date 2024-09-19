using System.CommandLine.Invocation;
using Djinn.Configuration;
using Djinn.Services;
using File = TagLib.File;

namespace Djinn.Commands.Handlers;

internal record CoverScanResult(DirectoryInfo AlbumDirectory, string ArtistName, Guid ReleaseId);

public class CoversCommandHandler : ICommandHandler
{
    public async Task<int> InvokeAsync(InvocationContext context)
    {
        var config = DjinnConfig.Load();
        
        var verbose = context.ParseResult.GetValueForOption(CoverCommand.Verbose);
        var force = context.ParseResult.GetValueForOption(CoverCommand.Force);
        
        if (verbose)
            Log.Level = LogLevel.Debug;
        
        var musicBrainzService = new MusicBrainzService();
        var coverArtDownloader = new CoverArtDownloader(config);
        
        foreach (var scanResult in GetReleaseIds(new DirectoryInfo(config.LibraryPath), force))
        {
            var album = await musicBrainzService.FindAlbum(scanResult.ReleaseId, scanResult.ArtistName);

            try
            {
                var cover = await coverArtDownloader.DownloadCoverArt(album, scanResult.AlbumDirectory);

                if (cover is null)
                {
                    Log.Error($"No cover art found for {scanResult.ArtistName} - {album.Title}");
                    continue;
                }
                
                Log.Information($"Downloaded cover art for {scanResult.ArtistName} - {album.Title}");
            }
            catch(Exception e)
            {
                Log.Error(e, $"Error downloading cover art for {scanResult.ArtistName} - {album.Title}");
            }
        }

        return 0;
    }

    public int Invoke(InvocationContext context)
    {
        throw new NotImplementedException();
    }

    private static IEnumerable<CoverScanResult> GetReleaseIds(DirectoryInfo libraryDirectory, bool force)
    {
        var artistDirectories = libraryDirectory.EnumerateDirectories();
        
        foreach (var artistDirectory in artistDirectories)
        {
            var artistName = artistDirectory.Name;
            var albumDirectories = artistDirectory.EnumerateDirectories();
            
            foreach (var albumDirectory in albumDirectories)
            {
                Log.Verbose($"Scanning {albumDirectory.FullName}");
                
                if (!force && albumDirectory.EnumerateFiles("cover.*").Any())
                {
                    Log.Verbose($"Skipping {albumDirectory.FullName} (cover already exists)");
                    continue;
                }
                
                var firstFile = albumDirectory.EnumerateFiles("*.flac").FirstOrDefault() ?? albumDirectory.EnumerateFiles("*.mp3").FirstOrDefault();
                
                if (firstFile is null)
                    continue;
                
                var releaseId = Guid.Empty;
                
                try
                {
                    Log.Verbose($"Reading tags for {firstFile.FullName}");
                    
                    var tags = File.Create(firstFile.FullName);
                    var id = tags.Tag.MusicBrainzReleaseId;
                    
                    if (!Guid.TryParse(id, out releaseId))
                    {
                        Log.Verbose($"Unable to parse release ID {id}");
                        continue;
                    }
                    
                    Log.Verbose($"Release ID: {releaseId}");
                }
                catch (Exception e)
                {
                    Log.Error(e, $"Error reading tags for {firstFile.FullName}");
                }

                yield return new CoverScanResult(albumDirectory, artistName, releaseId);
            }
        }
    }
}