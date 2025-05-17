using System.CommandLine.Invocation;
using Music.Collections;
using Music.Configuration;
using Music.Models;
using Music.Services;

namespace Music.Commands.Handlers;

public class DownloadCommandHandler : ICommandHandler
{
    private readonly MusicBrainzService _musicBrainzService = new();
    private readonly RetryableQueue<Album> _downloadQueue = new();
    private readonly MusicConfig _config = MusicConfig.Load();

    public async Task<int> InvokeAsync(InvocationContext context)
    {
        ParseOptions(context);

        if (await HandleArtistDownloads(context) || await HandleAlbumDownloads(context))
            return 1;

        return await PerformDownloads(context);
    }

    private void ParseOptions(InvocationContext context)
    {
        _config.Verbose = context.ParseResult.GetValueForOption(DownloadCommand.Verbose);
        _config.NoProgress = context.ParseResult.GetValueForOption(DownloadCommand.NoProgress);
        _config.StripExistingMetadata = context.ParseResult.GetValueForOption(DownloadCommand.StripExistingMetadata);

        if (_config.Verbose)
            Log.Level = LogLevel.Verbose;
    }

    private async Task<bool> HandleArtistDownloads(InvocationContext context)
    {
        var artistId = context.ParseResult.GetValueForOption(DownloadCommand.ArtistId);
        var artistName = context.ParseResult.GetValueForOption(DownloadCommand.ArtistName);

        if (artistId is not null || artistName is not null)
        {
            var artist = artistId is not null 
                ? await _musicBrainzService.FindArtist(artistId.Value)
                : await _musicBrainzService.FindArtist(artistName!);

            if (artist is null)
            {
                Log.Error("Unable to locate artist");
                return true;
            }

            var albums = await _musicBrainzService.LocateAlbums(artist.Id);
            if (albums.Count == 0)
            {
                Log.Error("Unable to locate any albums");
                return true;
            }

            _downloadQueue.EnqueueMany(albums);
        }

        return false;
    }

    private async Task<bool> HandleAlbumDownloads(InvocationContext context)
    {
        var releaseId = context.ParseResult.GetValueForOption(DownloadCommand.ReleaseId);
        var releaseTitle = context.ParseResult.GetValueForOption(DownloadCommand.ReleaseTitle);

        if (releaseId is not null || releaseTitle is not null)
        {
            var album = releaseId is not null 
                ? await _musicBrainzService.FindAlbum(releaseId.Value)
                : await _musicBrainzService.FindAlbum(releaseTitle!);

            if (album is null)
            {
                Log.Error("Unable to locate album");
                return true;
            }

            _downloadQueue.Enqueue(album);
        }

        return false;
    }

    private async Task<int> PerformDownloads(InvocationContext context)
    {
        var replace = context.ParseResult.GetValueForOption(DownloadCommand.Replace);
        var fileTypes = context.ParseResult.GetValueForOption(DownloadCommand.FileTypes)!;

        var coverArtDownloader = new CoverArtDownloader(_config);
        var metadataUpdater = new MetadataService(_config);
        var sourceDownloader = new SourceDownloader(_config);
        var albumDownloader = new AlbumDownloader(_config, coverArtDownloader, metadataUpdater, sourceDownloader);

        while (_downloadQueue.TryDequeue(out var album))
        {
            if (!await albumDownloader.Download(album!, replace, fileTypes))
                _downloadQueue.Enqueue(album!);
        }

        return 0;
    }

    public int Invoke(InvocationContext context)
    {
        throw new NotImplementedException();
    }
}

// using System.CommandLine.Invocation;
// using Music.Collections;
// using Music.Configuration;
// using Music.Models;
// using Music.Services;
//
// namespace Music.Commands.Handlers;
//
// public class DownloadCommandHandler : ICommandHandler
// {
//     public async Task<int> InvokeAsync(InvocationContext context)
//     {
//         var config = DjinnConfig.Load();
//
//         var releaseId = context.ParseResult.GetValueForOption(DownloadCommand.ReleaseId);
//         var releaseTitle = context.ParseResult.GetValueForOption(DownloadCommand.ReleaseTitle);
//         
//         var artistId = context.ParseResult.GetValueForOption(DownloadCommand.ArtistId);
//         var artistName = context.ParseResult.GetValueForOption(DownloadCommand.ArtistName);
//         
//         var replace = context.ParseResult.GetValueForOption(DownloadCommand.Replace);
//         var fileTypes = context.ParseResult.GetValueForOption(DownloadCommand.FileTypes)!;
//         var verbose = context.ParseResult.GetValueForOption(DownloadCommand.Verbose);
//         var noProgress = context.ParseResult.GetValueForOption(DownloadCommand.NoProgress);
//         var year = context.ParseResult.GetValueForOption(DownloadCommand.Year);
//         var stripExistingMetadata = context.ParseResult.GetValueForOption(DownloadCommand.StripExistingMetadata);
//
//         config.Verbose = verbose;
//         config.NoProgress = noProgress;
//         config.StripExistingMetadata = stripExistingMetadata;
//
//         if (verbose)
//             Log.Level = LogLevel.Verbose;
//
//         var musicBrainzService = new MusicBrainzService();
//         var downloadQueue = new RetryableQueue<Album>();
//
//         if (artistId is not null || artistName is not null)
//         {
//             Artist? artist = null;
//
//             if (artistId is not null)
//                 artist = await musicBrainzService.FindArtist(artistId.Value);
//             else if (artistName is not null)
//                 artist = await musicBrainzService.FindArtist(artistName);
//             
//             if (artist is null)
//             {
//                 Log.Error("Unable to locate artist");
//                 return 1;
//             }
//             
//             var albums = await musicBrainzService.LocateAlbums(artist.Id);
//             
//             if (albums.Count == 0)
//             {
//                 Log.Error("Unable to locate any albums");
//                 return 1;
//             }
//             
//             downloadQueue.EnqueueMany(albums);
//         }
//         else if (releaseId is not null || releaseTitle is not null)
//         {
//             Album? album = null;
//
//             if (releaseId is not null)
//                 album = await musicBrainzService.FindAlbum(releaseId.Value, year);
//             else if (releaseTitle is not null)
//                 album = await musicBrainzService.FindAlbum(releaseTitle, year);
//
//             if (album is null)
//             {
//                 Log.Error("Unable to locate album");
//                 return 1;
//             }
//             
//             downloadQueue.Enqueue(album);
//         }
//         
//         config.Verbose = verbose;
//         
//         var coverArtDownloader = new CoverArtDownloader(config);
//         var metadataUpdater = new MetadataUpdater(config);
//         var sourceDownloader = new SourceDownloader(config);
//         var albumDownloader = new AlbumDownloader(config, coverArtDownloader, metadataUpdater, sourceDownloader);
//
//         while (downloadQueue.TryDequeue(out var album))
//         {
//             if (!await albumDownloader.Download(album!, replace, fileTypes))
//                 downloadQueue.Enqueue(album!);
//         }
//
//         return 0;
//     }
//     
//     public int Invoke(InvocationContext context)
//     {
//         throw new NotImplementedException();
//     }
// }