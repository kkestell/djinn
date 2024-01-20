using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Parsing;
using System.Text;

using Djinn.Commands;

namespace Djinn;

internal abstract class Program
{
    private static async Task<int> Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;

        var rootCommand = new RootCommand
        {
            new DownloadCommand(),
            new ConfigCommand(),
            new CoverCommand(),
            new UpgradeCommand(),
            new SuggestCommand()
        };

        var commandLine = new CommandLineBuilder(rootCommand)
            .UseDefaults()
            .Build();

        return await commandLine.InvokeAsync(args);
    }
}