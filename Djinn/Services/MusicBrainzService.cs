using Djinn.Models;
using Djinn.Utils;
using FuzzySharp;
using MetaBrainz.MusicBrainz;
using MetaBrainz.MusicBrainz.Interfaces.Entities;
using MetaBrainz.MusicBrainz.Interfaces.Searches;

namespace Djinn.Services;

internal class MusicBrainzService
{
    private readonly Query _query = new();

    #region Artist
    public async Task<Artist> FindArtist(Guid artistId)
    {
        var artist = await _query.LookupArtistAsync(artistId, Include.ReleaseGroups | Include.Releases);
        return Artist.Create(artist);
    }
    
    public async Task<Artist?> FindArtist(string artistName)
    {
        var artists = await _query.FindArtistsAsync(artistName, limit: 10);
                
        if (artists.TotalResults == 0)
            return null;

        IArtist artist;
        
        if (artists.Results.Count == 1)
        {
            artist = artists.Results.First().Item;
        }
        else
        {
            var longestName = artists.Results.Max(x => x.Item.Name?.Length)!.Value;
            var longestDisambiguation = artists.Results.Max(x => x.Item.Disambiguation?.Length)!.Value;
            
            if (longestName > 39)
                longestName = 39;
            
            if (longestDisambiguation > 40)
                longestDisambiguation = 40;
            
            var selectList = new SelectList(artists.Results.Select(x =>
            {
                var sb = new FixedWidthStringBuilder();

                var name = x.Item.Name!;
                if (name.Length > longestName)
                    name = name.Substring(0, longestName - 1) + "…";
                sb.Append(name, longestName + 1);

                var disambiguation = x.Item.Disambiguation;
                if (!string.IsNullOrEmpty(disambiguation))
                {
                    if (disambiguation.Length > longestDisambiguation)
                        disambiguation = disambiguation.Substring(0, longestDisambiguation - 1) + "…";
                    sb.Append(disambiguation, longestDisambiguation);
                }

                return sb.ToString();
            }).ToList());
                    
            var selectedArtistIndex = selectList.Show($"Please select the correct artist for {artistName}:");
            artist = artists.Results[selectedArtistIndex].Item;
        }

        return Artist.Create(artist);
    }
    
    public async Task<List<Album>> LocateAlbums(Guid artistId)
    {
        Log.Verbose($"Locating albums by artist id: {artistId}…");
        
        var mbArtist = await _query.LookupArtistAsync(artistId, Include.ReleaseGroups | Include.Releases);
        var artist = Artist.Create(mbArtist);
        
        var albums = new List<Album>();

        var releases = await FetchAllReleases(mbArtist);
        
        var releasesGroupedByReleaseGroup = releases.GroupBy(x => x.ReleaseGroup!.Id).ToList();
        
        Log.Verbose($"Found {releasesGroupedByReleaseGroup.Count} release groups");

        foreach (var releaseGroup in releasesGroupedByReleaseGroup)
        {
            var release = GetBestRelease(releaseGroup.ToList());
            
            if (release is null)
            {
                Log.Verbose($"Skipping release group {releaseGroup.Key} with no suitable releases.");
                continue;
            }
            
            var album = Album.CreateFromRelease(artist, release);
            albums.Add(album);
            
            Log.Verbose($"Found suitable release {release.Title} {release.Country} {release.ReleaseGroup?.FirstReleaseDate?.Year} {release.Media?.FirstOrDefault()?.Format}");
        }
        
        albums.Sort((x, y) => x.Date.CompareTo(y.Date));

        return albums;
    }
    
    private IRelease? GetBestRelease(IReadOnlyList<IRelease> releases)
    {
        Log.Verbose($"Evaluating release group {releases.First().Title}…");
            
        var release = releases.FirstOrDefault(x =>
        {
            var year = x.ReleaseGroup?.FirstReleaseDate?.Year;
            var coverArt = x.CoverArtArchive?.Front ?? false;
                
            return year is not null && coverArt;
        });
            
        release ??= releases.FirstOrDefault(x => x.ReleaseGroup?.FirstReleaseDate?.Year is not null);

        return release;
    }
    
    private async Task<List<IRelease>> FetchAllReleases(IArtist artist)
    {
        const int limit = 100;
        const Include includes = Include.ReleaseGroups | Include.ArtistCredits | Include.Media | Include.Recordings;
        var allReleases = new List<IRelease>();

        var offset = 0;
        int totalResults;
    
        do
        {
            var releases = await _query.BrowseReleasesAsync(artist, inc: includes, type: ReleaseType.Album | ReleaseType.EP, status: ReleaseStatus.Official, limit: limit, offset: offset);
            allReleases.AddRange(releases.Results);
            totalResults = releases.TotalResults;
            offset += limit;
        } while (offset < totalResults);

        return allReleases;
    }
    #endregion

    #region Album

    public async Task<Album> FindAlbum(Guid releaseId, string artistName, DateOnly? releaseDate = null)
    {
        var release = await _query.LookupReleaseAsync(releaseId, Include.ReleaseGroups | Include.ArtistCredits | Include.Media | Include.Recordings);

        if (release.ArtistCredit is null)
            throw new Exception("Release contains no artist credit.");

        Guid? selectedArtistId;

        if (release.ArtistCredit.Count == 1)
        {
            selectedArtistId = release.ArtistCredit.First().Artist?.Id;
        }
        else
        {
            var bestScore = 0;
            selectedArtistId = null;
            
            foreach (var artistCredit in release.ArtistCredit)
            {
                var score = Fuzz.Ratio(artistCredit.Name, artistName);
                
                if (score > bestScore)
                {
                    bestScore = score;
                    selectedArtistId = artistCredit.Artist?.Id;
                }
            }
        }
        
        if (selectedArtistId is null)
            throw new Exception($"Unable to locate artist for release {releaseId}.");
            
        var artist = await FindArtist(selectedArtistId.Value);

        var album = Album.CreateFromRelease(artist, release, releaseDate);
        return album;
    }
    
    public async Task<Album> FindAlbum(Guid releaseId, DateOnly? releaseDate = null)
    {
        var release = await _query.LookupReleaseAsync(releaseId, Include.ReleaseGroups | Include.ArtistCredits | Include.Media | Include.Recordings);

        if (release.ArtistCredit is null)
            throw new Exception("Release contains no artist credit.");

        Guid? selectedArtistId;

        if (release.ArtistCredit.Count == 1)
        {
            selectedArtistId = release.ArtistCredit.First().Artist?.Id;
        }
        else
        {
            var selectList = new SelectList(release.ArtistCredit.Select(x =>
            {
                var sb = new FixedWidthStringBuilder();

                var name = x.Name!;
                sb.Append(name, 40);

                var disambiguation = x.Artist?.Disambiguation;
                if (!string.IsNullOrEmpty(disambiguation))
                {
                    sb.Append(disambiguation, 40);
                }

                return sb.ToString();
            }).ToList());
                    
            var selectedArtistIndex = selectList.Show($"Please select the correct artist for {release.Title}:");
            
            selectedArtistId = release.ArtistCredit[selectedArtistIndex].Artist?.Id;
        }
        
        if (selectedArtistId is null)
            throw new Exception($"Unable to locate artist for release {releaseId}.");
            
        var artist = await FindArtist(selectedArtistId.Value);

        var album = Album.CreateFromRelease(artist, release, releaseDate);
        return album;
    }

    public async Task<Album?> FindAlbum(string albumTitle, DateOnly? releaseDate = null)
    {
        var searchResults = await _query.FindReleaseGroupsAsync(albumTitle);
        
        if (!searchResults.Results.Any())
            return null;

        IReleaseGroup releaseGroup;
        
        if (searchResults.TotalResults == 1)
        {
            releaseGroup = searchResults.Results.First().Item;
        }
        else
        {
            var releaseGroupsWithValidData = searchResults.Results
                .Where(
                    x => !string.IsNullOrEmpty(x.Item.Title) &&
                         x.Item.ArtistCredit?[0].Artist?.Name is not null &&
                         x.Item.FirstReleaseDate?.Year is not null)
                .OrderBy(x => x.Item.FirstReleaseDate!.Year)
                .ToList();
            
            var selectList = new SelectList(releaseGroupsWithValidData.Select(x =>
            {
                var sb = new FixedWidthStringBuilder();

                var title = x.Item.Title!;
                sb.Append(title, 40);

                var artists = string.Join(", ", x.Item.ArtistCredit!.Select(x => x.Artist!.Name));
                sb.Append(artists, 40);

                var firstReleaseYear = x.Item.FirstReleaseDate!.Year!;
                sb.Append(firstReleaseYear?.ToString(), 10, Alignment.Right);

                return sb.ToString();
            }).ToList());
                    
            var selectedReleaseIndex = selectList.Show($"Please select the correct release group for {albumTitle}:");
            releaseGroup = releaseGroupsWithValidData[selectedReleaseIndex].Item;
        }
        
        var releases = await _query.BrowseReleasesAsync(releaseGroup, inc: Include.ArtistCredits | Include.Media | Include.Recordings);
        
        if (!releases.Results.Any())
            return null;

        IRelease release;
        
        if (releases.Results.Count == 1)
        {
            release = releases.Results.First();
        }
        else
        {
            var releasesWithValidData = releases.Results
                .Where(
                    x => x.Date?.Year is not null && 
                         x.ArtistCredit?[0].Artist?.Name is not null &&
                         x.Media?[0].Format is not null)
                .OrderBy(x => x.Date!.Year)
                .ToList();
            
            var selectList = new SelectList(releasesWithValidData.Select(x =>
            {
                var sb = new FixedWidthStringBuilder();

                var title = x.Title!;
                sb.Append(title, 30);

                var artist = x.ArtistCredit![0].Artist!.Name!;
                sb.Append(artist, 20);

                var releaseYear = x.Date!.Year!;
                sb.Append(releaseYear.Value.ToString(), 6, Alignment.Right);

                var country = x.Country!;
                sb.Append(country, 10, Alignment.Right);

                var format = x.Media![0].Format;
                sb.Append(format, 15, Alignment.Right);

                var numTracks = x.Media!.SelectMany(m => m.Tracks ?? new List<ITrack>()).Count();
                sb.Append(numTracks.ToString(), 5, Alignment.Right);

                var numMedia = x.Media!.Count;
                sb.Append(numMedia.ToString(), 5, Alignment.Right);

                var hasCoverArt = x.CoverArtArchive is not null && x.CoverArtArchive.Front ? "Has Cover" : "";
                sb.Append(hasCoverArt, 12, Alignment.Right);
                    
                return sb.ToString();
            }).ToList());

            
            var selectedReleaseIndex = selectList.Show($"Please select the correct release for {albumTitle}:");
            release = releases.Results[selectedReleaseIndex];
        }
        
        return await FindAlbum(release.Id, releaseDate);
    }
    #endregion
}