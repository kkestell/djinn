using Djinn.Services;
using MetaBrainz.MusicBrainz.Interfaces.Entities;

namespace Djinn.Models;

public class Artist(Guid id, string name, string sortName)
{
    public Guid Id { get; } = id;
    public string Name { get; } = name;
    public string SortName { get; } = sortName;

    public static Artist Create(IArtist artist)
    {
        var artistId = artist.Id;
        var artistName = artist.Name!;
        var artistSort = NameService.SortName(artistName);

        return new Artist(artistId, artistName, artistSort);
    }
}