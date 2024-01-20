namespace Djinn.Metadata;

// https://xiph.org/vorbis/doc/v-comment.html
public class Field
{
    private readonly string value;

    private Field(string value)
    {
        this.value = value;
    }

    public static Field Title => new("TITLE");
    public static Field Album => new("ALBUM");
    public static Field Artist => new("ARTIST");
    public static Field Date => new("DATE");
    public static Field TrackNumber => new("TRACKNUMBER");
    public static Field TotalTracks => new("TOTALTRACKS");
    public static Field DiscNumber => new("DISCNUMBER");
    public static Field TotalDiscs => new("TOTALDISCS");
    public static Field MusicbrainzTrackId => new("MUSICBRAINZ_TRACKID");
    public static Field MusicbrainzReleaseGroupId => new("MUSICBRAINZ_RELEASEGROUPID");
    public static Field MusicbrainzArtistId => new("MUSICBRAINZ_ARTISTID");

    public override string ToString()
    {
        return value;
    }
}
