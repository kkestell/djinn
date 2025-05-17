using System.CommandLine;
using Music.Commands.Handlers;

namespace Music.Commands;

public class StatsCommand : Command
{
    public StatsCommand() : base("stats", "Get statistics about your music library")
    {
        Handler = new StatsCommandHandler();
    }
}