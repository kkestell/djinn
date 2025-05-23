using Djinn.Configuration;
using Djinn.Models;
using IF.Lastfm.Core.Api;
using MetaBrainz.MusicBrainz.CoverArt;
using SpotifyAPI.Web;

namespace Djinn.Services;

public class CoverArtDownloader
{
    private const string Contact = "https://github.com/kkestell/music";
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
        Log.Verbose($"Downloading cover art for {release.ArtistNames} - {release.Title}");
        
        var coverImage = await DownloadFromMusicBrainz(release, downloadDirectory.FullName);
        coverImage ??= await DownloadFromLastFm(release, downloadDirectory.FullName);
        coverImage ??= await DownloadFromSpotify(release, downloadDirectory.FullName);

        if (coverImage is not null)
        {
            if (_config.CoverDisplayStyle == CoverDisplayStyle.Sixel)
            {
                var imageSixel = ImageToSixel.Encode(coverImage, 500);
                Console.Out.WriteLine(imageSixel);
            }
            else if (_config.CoverDisplayStyle == CoverDisplayStyle.Ansi)
            {
                
                var imageAnsi = ImageToAnsiConverter.ImageToAnsi(coverImage, Console.WindowHeight - 4);
                Console.WriteLine(imageAnsi);
            }
        }
        
        return coverImage;
    }

    private static async Task<FileInfo?> DownloadFromMusicBrainz(Album album, string directory)
    {
        try
        {
            Log.Verbose($"Downloading cover art from MusicBrainz for {album.ArtistNames} - {album.Title}");

            var client = new CoverArt(Product, new Version(VersionMajor, VersionMinor, VersionPatch), new Uri(Contact));
            var response = await client.FetchReleaseAsync(album.Id);

            var coverUrl = response.Images.FirstOrDefault(x => x.Front)?.Location;

            if (coverUrl is null)
                return null;

            var coverImage = await DownloadFile(coverUrl, directory);
            
            return coverImage;
        }
        catch (MetaBrainz.Common.HttpError e)
        {
            return null;
        }
        catch (Exception e)
        {
            Log.Error(e, $"Error downloading cover art for {album.ArtistNames} - {album.Title}");
            return null;
        }
    }

    private async Task<FileInfo?> DownloadFromLastFm(Album album, string directory)
    {
        try
        {
            Log.Verbose($"Downloading cover art from Last.fm for {album.ArtistNames} - {album.Title}");
            
            var client = new LastfmClient(_config.LastFmApiKey, _config.LastFmApiSecret);
            var response = await client.Album.GetInfoAsync(album.ArtistNames, album.Title);

            var coverUrl = response.Content?.Images?.Largest?.AbsoluteUri;

            if (coverUrl is null)
                return null;

            var coverImage = await DownloadFile(new Uri(coverUrl), directory);
            
            return coverImage;
        }
        catch (Exception e)
        {
            Log.Error(e, $"Error downloading cover art for {album.ArtistNames} - {album.Title}");
            return null;
        }
    }
    
    private async Task<FileInfo?> DownloadFromSpotify(Album album, string directory)
    {
        try
        {
            Log.Verbose($"Downloading cover art from Spotify for {album.ArtistNames} - {album.Title}");
            var config = SpotifyClientConfig
                .CreateDefault()
                .WithAuthenticator(new ClientCredentialsAuthenticator(_config.SpotifyClientId, _config.SpotifyClientSecret));

            var spotify = new SpotifyClient(config);

            var searchRequest = new SearchRequest(SearchRequest.Types.Album, $"{album.ArtistNames} {album.Title}");
            var searchResponse = await spotify.Search.Item(searchRequest);

            if (searchResponse.Albums.Items is null)
                return null;
            
            if (searchResponse.Albums.Items.Count == 0)
                return null;

            var spotifyAlbum = searchResponse.Albums.Items.First();
            var coverUrl = spotifyAlbum.Images.OrderByDescending(i => i.Height).First().Url;

            if (string.IsNullOrEmpty(coverUrl))
                return null;

            var coverImage = await DownloadFile(new Uri(coverUrl), directory);
            
            return coverImage;
        }
        catch (Exception e)
        {
            Log.Error(e, $"Error downloading cover art from Spotify for {album.ArtistNames} - {album.Title}");
            return null;
        }
    }

    private static async Task<FileInfo?> DownloadFile(Uri uri, string directory)
    {
        using var httpClient = new HttpClient();

        using var response = await httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Head, uri));
        response.EnsureSuccessStatusCode();

        var contentType = response.Content.Headers.ContentType?.MediaType;
        var fileExtension = GetFileExtensionFromContentType(contentType);

        var fileName = $"cover{fileExtension}";
        var filePath = Path.Combine(directory, fileName);

        var bytes = await httpClient.GetByteArrayAsync(uri);
        await File.WriteAllBytesAsync(filePath, bytes);

        return new FileInfo(filePath);
    }

    private static string GetFileExtensionFromContentType(string? contentType)
    {
        return contentType?.ToLower() switch
        {
            "image/jpeg" => ".jpg",
            "image/png" => ".png",
            "image/gif" => ".gif",
            "image/webp" => ".webp",
            _ => ".jpg"  // Default to .jpg if content type is unknown
        };
    }
}