using System.CommandLine;
using Music.Commands.Handlers;

namespace Music.Commands;

public class ConfigCommand : Command
{
    public ConfigCommand() : base("config", "Display configuration file path and values")
    {
        Handler = new ConfigCommandHandler();
    }
}