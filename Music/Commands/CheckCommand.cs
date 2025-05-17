using System.CommandLine;
using Music.Commands.Handlers;

namespace Music.Commands;

public class CheckCommand : Command
{
    public static readonly Option<bool> Verbose = new("--verbose", "Verbose output");
    public static readonly Option<bool> Fix = new("--fix", "Automatically fix issues");

    public CheckCommand() : base("check", "Check library for issues")
    {
        AddOption(Verbose);
        AddOption(Fix);

        Handler = new CheckCommandHandler();
    }
}