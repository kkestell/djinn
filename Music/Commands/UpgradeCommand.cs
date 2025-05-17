using System.CommandLine;
using Music.Commands.Handlers;

namespace Music.Commands;

public class UpgradeCommand : Command
{
    public static readonly Option<bool> Randomize = new("--randomize", "Randomize download order");
    public static readonly Option<bool> Verbose = new("--verbose", "Verbose output");

    public UpgradeCommand() : base("upgrade", "Replace mp3 files with flac files")
    {
        AddOption(Randomize);
        AddOption(Verbose);
        
        Handler = new UpgradeCommandHandler();
    }
}