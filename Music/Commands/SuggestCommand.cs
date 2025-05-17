using System.CommandLine;
using Music.Commands.Handlers;

namespace Music.Commands;

public class SuggestCommand : Command
{
    public static readonly Option<string> ArtistName = new("--artist-name", "MusicBrainz artist name");

    public SuggestCommand() : base("suggest", "Suggest new releases and related artists")
    {
        AddOption(ArtistName);
        
        Handler = new SuggestCommandHandler();
    }
}