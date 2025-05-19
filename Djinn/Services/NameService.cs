using Djinn.Models;

namespace Djinn.Services;

public static class NameService
{
    public static string SortName(string name)
    {
        if (name.StartsWith("The "))
        {
            return $"{name[4..]}, The";
        }
        
        return name;
    }
    
    public static string FormatArtist(Album album, string formatString)
    {
        var tokens = new Dictionary<string, string>
        {
            { "%A", string.Join(", ", album.Artists.Select(x => x.Name)) },
            { "%S", string.Join("; ", album.Artists.Select(x => x.SortName)) },
            { "%%", "%" }
        };
        return ReplaceTokens(formatString, tokens);
    }

    public static string FormatAlbum(Album album, string formatString)
    {
        var tokens = new Dictionary<string, string>
        {
            { "%A", string.Join(", ", album.Artists.Select(x => x.Name)) },
            { "%S", string.Join("; ", album.Artists.Select(x => x.SortName)) },
            { "%T", album.Title },
            { "%Y", album.Date.Year.ToString() },
            { "%%", "%" }
        };
        return ReplaceTokens(formatString, tokens);
    }

    public static string FormatTrack(Track track, Album album, string formatString)
    {
        var tokens = new Dictionary<string, string>
        {
            { "%A", string.Join(", ", album.Artists.Select(x => x.Name)) },
            { "%S", string.Join("; ", album.Artists.Select(x => x.SortName)) },
            { "%T", album.Title },
            { "%Y", album.Date.Year.ToString("00") },
            { "%t", track.Title },
            { "%n", track.Number.ToString("D2") },
            { "%N", album.Tracks.Count.ToString("D2") },
            { "%%", "%" }
        };
        return ReplaceTokens(formatString, tokens);
    }

    private static string ReplaceTokens(string formatString, Dictionary<string, string> tokens)
    {
        var result = formatString;
        foreach (var token in tokens)
        {
            result = result.Replace(token.Key, token.Value);
        }
        return result;
    }
}