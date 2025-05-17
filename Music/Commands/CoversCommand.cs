using System.CommandLine;
using Music.Commands.Handlers;

namespace Music.Commands;

public class CoverCommand : Command
{
    public static readonly Option<bool> Force = new("--force", "Force download of cover art");
    public static readonly Option<bool> Verbose = new("--verbose", "Verbose output");

    public CoverCommand() : base("covers", "Download missing cover art")
    {
        AddOption(Force);
        AddOption(Verbose);
        
        Handler = new CoversCommandHandler();
    }
}