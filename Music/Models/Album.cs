using System.Text.Json.Serialization;
using MetaBrainz.MusicBrainz.Interfaces.Entities;
using Music.Services;

namespace Music.Models;

public class Album
{
    [JsonConstructor]
    public Album(List<Artist> artists, Guid id, string title, DateOnly date, List<Track> tracks)
    {
        Artists = artists;
        Id = id;
        Title = title;
        Date = date;
        Tracks = tracks;
    }

    public List<Artist> Artists { get; }
    public Guid Id { get; }
    public string Title { get; }
    public DateOnly Date { get; }
    public List<Track> Tracks { get; }
    
    [JsonIgnore]
    public string ArtistNames => string.Join(", ", Artists.Select(x => x.Name));

    public static Album CreateFromRelease(IRelease release)
    {
        var albumId = release.Id;
        var albumTitle = release.ReleaseGroup!.Title!;

        if (release.ReleaseGroup.FirstReleaseDate is null)
            throw new Exception("Release contains no date.");

        var releaseDate = DateOnly.FromDateTime(release.ReleaseGroup.FirstReleaseDate!.NearestDate);

        var releaseTracks = release.Media!.SelectMany(x => x.Tracks ?? new List<ITrack>()).ToList();

        var albumTracks = releaseTracks.Select(track =>
        {
            var trackId = track.Id;
            var trackNumber = releaseTracks.IndexOf(track) + 1;
            var trackTitle = track.Title!;

            return new Track(trackId, trackNumber, trackTitle);
        }).ToList();

        var artists = release.ArtistCredit.Select(artist =>
        {
            var artistId = artist.Artist.Id;
            var artistName = artist.Artist.Name!;
            var artistSort = NameService.SortName(artistName);

            return new Artist(artistId, artistName, artistSort);
        }).ToList();
        
        return new Album(artists, albumId, albumTitle, releaseDate, albumTracks);
    }

    public override string ToString()
    {
        return $"{string.Join(", ", Artists)} - {Title} ({Date.Year})";
    }
}