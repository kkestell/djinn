using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Parsing;
using System.Text;
using Djinn.Commands;
using Djinn.Configuration;
using Djinn.Services;

namespace Djinn;

internal abstract class Program
{
    private static async Task<int> Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;

        try
        {
            DjinnConfig.Validate();
        }
        catch (ConfigurationError e)
        {
            Log.Error(e.Message);
            return 1;
        }

        var rootCommand = new RootCommand
        {
            new DownloadCommand(),
            new CheckCommand(),
            new ConfigCommand(),
            new CoverCommand(),
            new UpgradeCommand(),
            new SuggestCommand(),
            new StatsCommand()
        };

        var commandLine = new CommandLineBuilder(rootCommand)
            .UseDefaults()
            .Build();

        return await commandLine.InvokeAsync(args);
    }
}