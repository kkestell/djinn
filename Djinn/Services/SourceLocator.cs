using Djinn.Models;
using Djinn.Utils;
using FuzzySharp;
using Soulseek;
using File = Soulseek.File;

namespace Djinn.Services;

public class SourceLocator
{
    private readonly IReadOnlyCollection<string> _fileExtensions;
    private readonly SoulseekClient _soulseekClient;

    public SourceLocator(SoulseekClient soulseekClient, IReadOnlyCollection<string> fileExtensions)
    {
        _soulseekClient = soulseekClient;
        _fileExtensions = fileExtensions;
    }

    public async Task<List<DownloadSource>> LocateSources(Album album, CancellationToken stoppingToken = default)
    {
        var responses = await Search(album, stoppingToken);
        var sources = BuildSourceList(responses, album);
        return sources;
    }

    private async Task<List<SearchResponse>> DoSearch(string query, CancellationToken stoppingToken)
    {
        var searchQuery = new SearchQuery(query);
        var searchOptions = new SearchOptions(
            fileFilter: file =>
                _fileExtensions.Contains(Path.GetExtension(PathUtils.SoulseekFilename(file)).ToLowerInvariant())
        );
        var results = await _soulseekClient.SearchAsync(
            searchQuery,
            options: searchOptions,
            cancellationToken: stoppingToken
        );
        var responses = results.Responses
            .OrderByDescending(x => x.HasFreeUploadSlot)
            .ThenByDescending(x => x.UploadSpeed)
            .ToList();
        Log.Verbose($"Received {responses.Count} responses for {query}");
        return responses;
    }
    
    private async Task<List<SearchResponse>> Search(Album album, CancellationToken stoppingToken)
    {
        var query = $"{album.Artist.Name} {album.Title}";
        var responses = await DoSearch(query, stoppingToken);
        if (responses.Count > 0)
            return responses;
        Log.Verbose($"No results found for {query}");
        if (album.Title.Contains(':') || album.Title.Contains('-'))
        {
            query = $"{album.Artist.Name} {album.Title.Split(':', '-').First()}";
            responses = await DoSearch(query, stoppingToken);
            if (responses.Count > 0)
                return responses;
            Log.Verbose($"No results found for {query}");
        }
        return [];
    }

    private List<DownloadSource> BuildSourceList(List<SearchResponse> responses, Album album)
    {
        var sources = new List<DownloadSource>();
        foreach (var response in responses)
        {
            Log.Verbose($"Matching tracks to files from {response.Username}…");
            var tracks = MatchTracksToFiles(album.Tracks, response.Files.ToList());
            if (tracks is null)
            {
                Log.Verbose($"No matches found for {response.Username}");
                continue;
            }
            Log.Verbose($"Found {tracks.Count} matches for {response.Username}");
            sources.Add(new DownloadSource(response.Username, tracks));
        }
        return sources;
    }

    private Dictionary<Track, File>? MatchTracksToFiles(List<Track> tracks, IReadOnlyCollection<File> files)
    {
        if (files.Count != tracks.Count)
            return null;
        var matches = new Dictionary<Track, File>();
        foreach (var track in tracks)
        {
            var ranked = files
                .Where(f => _fileExtensions.Contains(Path.GetExtension(f.Filename.Replace('\\', '/'))))
                .Where(f => !matches.ContainsValue(f))
                .Select(
                    f =>
                    {
                        var fileName = Path.GetFileName(f.Filename.Replace('\\', '/'));
                        var trackNumber = $"{tracks.IndexOf(track) + 1:00}";
                        var score = Fuzz.PartialRatio(fileName, $"{trackNumber} {track.Title}");

                        return (f, ratio: score);
                    }
                );
            var (bestFile, bestScore) = ranked.OrderByDescending(x => x.Item2).FirstOrDefault();
            if (bestFile is null)
                return null;
            if (bestScore < 70)
                return null;
            Log.Verbose($"{tracks.IndexOf(track) + 1:00} {track.Title} -> {PathUtils.SoulseekFilenameWithoutExtension(bestFile)} ({bestScore})");
            matches.Add(track, bestFile);
        }
        if (matches.Select(x => Path.GetDirectoryName(x.Value.Filename.Replace('\\', '/'))).Distinct().Count() > 1)
            return null;
        var extensions = matches.Values.Select(x => Path.GetExtension(x.Filename.Replace('\\', '/'))).Distinct().ToList();
        if (extensions.Distinct().Count() > 1)
            return null;
        return matches;
    }

    public class DownloadSource(string username, Dictionary<Track, File> files)
    {
        public string Username { get; } = username;
        public Dictionary<Track, File> Files { get; } = files;
    }
}