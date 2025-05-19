using System.Text.Encodings.Web;
using System.Text.Json;
using Djinn.Configuration;
using Djinn.Models;
using Djinn.Utils;
using Soulseek;
using File = System.IO.File;

namespace Djinn.Services;

public class AlbumDownloader
{
    private readonly DjinnConfig _config;
    private readonly CoverArtDownloader _coverArtDownloader;
    private readonly MetadataService _metadataService;
    private readonly SourceDownloader _sourceDownloader;

    public AlbumDownloader(DjinnConfig config, CoverArtDownloader coverArtDownloader, MetadataService metadataService, SourceDownloader sourceDownloader)
    {
        _config = config;
        _coverArtDownloader = coverArtDownloader;
        _metadataService = metadataService;
        _sourceDownloader = sourceDownloader;
    }

    public async Task<bool> Download(Album album, bool replace, IReadOnlyCollection<string> fileTypes)
    {
        var artistDirectoryName = PathUtils.SanitizePath(NameService.FormatArtist(album, _config.ArtistFormat));
        var artistDirectoryPath = Path.Combine(_config.LibraryPath, artistDirectoryName);
        var artistDirectory = new DirectoryInfo(artistDirectoryPath);
        
        var albumDirectoryName = PathUtils.SanitizePath(NameService.FormatAlbum(album, _config.AlbumFormat));
        var albumDirectoryPath = Path.Combine(artistDirectory.FullName, albumDirectoryName);
        var albumDirectory = new DirectoryInfo(albumDirectoryPath);

        if (albumDirectory.Exists && !replace)
        {
            Log.Success($"{album} already exists in library");
            return true;
        }

        SourceDownloader.DownloadResult? downloadResult = null;

        using (var soulseekClient = new SoulseekClient())
        {
            await soulseekClient.ConnectAsync(_config.SoulseekUsername, _config.SoulseekPassword);

            Log.Information($"Searching for {album}…");

            var sourceLocator = new SourceLocator(soulseekClient, fileTypes);
            var downloadSources = await sourceLocator.LocateSources(album);

            if (!downloadSources.Any())
            {
                Log.Error($"No download sources found for {album}");
                return false;
            }

            Log.Information($"Downloading {album} from {downloadSources.Count} sources…");

            downloadResult = await _sourceDownloader.DownloadFiles(soulseekClient, album, downloadSources);
        }

        if (downloadResult is null)
        {
            Log.Error($"Error downloading {album}");
            return false;
        }

        var coverImageFile = await _coverArtDownloader.DownloadCoverArt(album, downloadResult.TempDirectory);

        _metadataService.Update(album, downloadResult.Files, coverImageFile);
        
        if (!artistDirectory.Exists)
        {
            Log.Verbose($"Creating artist directory {artistDirectory.FullName}");
            artistDirectory.Create();
        }

        if (albumDirectory.Exists && replace)
        {
            Log.Verbose($"Deleting existing album directory {albumDirectory.FullName}");
            albumDirectory.Delete(true);
        }

        Log.Verbose($"Creating album directory {albumDirectory.FullName}");
        albumDirectory.Create();

        foreach (var file in downloadResult.TempDirectory.GetFiles())
        {
            var filePath = Path.Combine(albumDirectory.FullName, file.Name);
            file.CopyTo(filePath);
            Log.Verbose($"Copied {file.Name} to {filePath}");
        }

        Log.Verbose($"Deleting temporary directory {downloadResult.TempDirectory.FullName}");
        downloadResult.TempDirectory.Delete(true);

        Log.Success($"Successfully downloaded {album} to {albumDirectory.FullName}");

        var metadataFilePath = Path.Combine(albumDirectory.FullName, ".metadata.json");
        var metadataJson = JsonSerializer.Serialize(album, new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        });
        await File.WriteAllTextAsync(metadataFilePath, metadataJson);
        Log.Verbose($"Saved album metadata to {metadataFilePath}");
        
        return true;
    }
}