using System.CommandLine;
using Djinn.Commands.Handlers;

namespace Djinn.Commands;

public class StatsCommand : Command
{
    public StatsCommand() : base("stats", "Get statistics about your music library")
    {
        Handler = new StatsCommandHandler();
    }
}