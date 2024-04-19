using Djinn.Configuration;
using Djinn.Models;
using IF.Lastfm.Core.Api;
using MetaBrainz.MusicBrainz.CoverArt;

namespace Djinn.Services;

public class CoverArtDownloader
{
    private const string Contact = "https://github.com/kkestell/djinn";
    private const string Product = "Djinn";
    private const int VersionMajor = 1;
    private const int VersionMinor = 0;
    private const int VersionPatch = 0;

    private readonly DjinnConfig _config;

    public CoverArtDownloader(DjinnConfig config)
    {
        _config = config;
    }

    public async Task<FileInfo?> DownloadCoverArt(Album release, DirectoryInfo downloadDirectory)
    {
        Log.Verbose($"Downloading cover art for {release.Artist.Name} - {release.Title}");
        
        var coverImage = await DownloadFromMusicBrainz(release, downloadDirectory.FullName);
        coverImage ??= await DownloadFromLastFm(release, downloadDirectory.FullName);

        return coverImage;
    }

    private static async Task<FileInfo?> DownloadFromMusicBrainz(Album album, string directory)
    {
        try
        {
            Log.Verbose($"Downloading cover art from MusicBrainz for {album.Artist.Name} - {album.Title}");

            var client = new CoverArt(Product, new Version(VersionMajor, VersionMinor, VersionPatch), new Uri(Contact));
            var response = await client.FetchReleaseAsync(album.Id);

            var coverUrl = response.Images.FirstOrDefault(x => x.Front)?.Location;

            if (coverUrl is null)
                return null;

            var coverImage = await DownloadFile(coverUrl, directory);
            
            return coverImage;
        }
        catch (Exception e)
        {
            Log.Error(e, $"Error downloading cover art for {album.Artist.Name} - {album.Title}");
            return null;
        }
    }

    private async Task<FileInfo?> DownloadFromLastFm(Album album, string directory)
    {
        try
        {
            Log.Verbose($"Downloading cover art from Last.fm for {album.Artist.Name} - {album.Title}");
            
            var client = new LastfmClient(_config.LastFmApiKey, _config.LastFmApiSecret);
            var response = await client.Album.GetInfoAsync(album.Artist.Name, album.Title);

            var coverUrl = response.Content?.Images?.Largest?.AbsoluteUri;

            if (coverUrl is null)
                return null;

            var coverImage = await DownloadFile(new Uri(coverUrl), directory);
            
            return coverImage;
        }
        catch (Exception e)
        {
            Log.Error(e, $"Error downloading cover art for {album.Artist.Name} - {album.Title}");
            return null;
        }
    }

    private static async Task<FileInfo?> DownloadFile(Uri uri, string directory)
    {
        using var httpClient = new HttpClient();

        var bytes = await httpClient.GetByteArrayAsync(uri);
        var fileExtension = Path.GetExtension(uri.AbsolutePath);
        var fileName = $"cover{fileExtension}";
        var filePath = Path.Combine(directory, fileName);

        await File.WriteAllBytesAsync(filePath, bytes);

        return new FileInfo(filePath);
    }
}