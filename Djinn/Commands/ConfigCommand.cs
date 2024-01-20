using System.CommandLine;
using Djinn.Commands.Handlers;

namespace Djinn.Commands;

public class ConfigCommand : Command
{
    public ConfigCommand() : base("config", "Display configuration file path and values")
    {
        Handler = new ConfigCommandHandler();
    }
}