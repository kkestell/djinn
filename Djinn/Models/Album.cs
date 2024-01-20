using MetaBrainz.MusicBrainz.Interfaces.Entities;

namespace Djinn.Models;

public class Album
{
    private Album(Artist artist, Guid id, string title, DateOnly date, List<Track> tracks)
    {
        Artist = artist;
        Id = id;
        Title = title;
        Date = date;
        Tracks = tracks;
    }

    public Artist Artist { get; }
    public Guid Id { get; }
    public string Title { get; }
    public DateOnly Date { get; }
    public List<Track> Tracks { get; }

    public static Album CreateFromRelease(Artist artist, IRelease release, DateOnly? date = null)
    {
        var albumId = release.Id;
        var albumTitle = release.ReleaseGroup!.Title!;

        DateOnly releaseDate;
        if (date is not null)
        {
            releaseDate = date.Value;
        }
        else
        {
            if (release.ReleaseGroup.FirstReleaseDate is null)
                throw new Exception("Release contains no date.");

            releaseDate = DateOnly.FromDateTime(release.ReleaseGroup.FirstReleaseDate!.NearestDate);
        }

        var releaseTracks = release.Media!.SelectMany(x => x.Tracks ?? new List<ITrack>()).ToList();

        var albumTracks = releaseTracks.Select(track =>
        {
            var trackId = track.Id;
            var trackNumber = releaseTracks.IndexOf(track) + 1;
            var trackTitle = track.Title!;

            return new Track(trackId, trackNumber, trackTitle);
        }).ToList();

        return new Album(artist, albumId, albumTitle, releaseDate, albumTracks);
    }

    public override string ToString()
    {
        return $"{Artist.Name} - {Title} ({Date.Year})";
    }
}