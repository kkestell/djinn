using System.CommandLine;
using Music.Commands.Handlers;

namespace Music.Commands;

public class DownloadCommand : Command
{
    public static readonly Option<Guid?> ReleaseId = new("--release-id", "Release ID");
    public static readonly Option<string> ReleaseTitle = new("--release-title", "Release title");
    public static readonly Option<Guid?> ArtistId = new("--artist-id", "Artist ID");
    public static readonly Option<string> ArtistName = new("--artist-name", "Artist name");
    
    public static readonly Option<bool> Replace = new("--replace", "Replace existing album(s)");
    public static readonly Option<List<string>> FileTypes = new("--file-types", () => new List<string> { ".flac", ".mp3" }, "File types to download");
    public static readonly Option<bool> NoProgress = new("--no-progress", "Disable realtime download progress");
    public static readonly Option<DateOnly?> Date = new("--date", "Release date");
    public static readonly Option<bool> StripExistingMetadata = new("--strip-existing-metadata", () => true, "Strip existing metadata from downloaded files");

    public static readonly Option<bool> Verbose = new("--verbose", "Verbose output");

    public DownloadCommand() : base("download", "Download one or more releases")
    {
        AddOption(ReleaseId);
        AddOption(ReleaseTitle);
        AddOption(ArtistId);
        AddOption(ArtistName);
        AddOption(Replace);
        AddOption(FileTypes);
        AddOption(Verbose);
        AddOption(NoProgress);
        AddOption(Date);
        AddOption(StripExistingMetadata);

        Handler = new DownloadCommandHandler();
        
        AddValidator(result =>
        {
            if (result.Children.Any(x => x.Symbol is IdentifierSymbol id && id.HasAlias("--release-id")) &&
                result.Children.Any(x => x.Symbol is IdentifierSymbol id && id.HasAlias("--release-title")))
            {
                result.ErrorMessage = "Cannot specify both --release-id and --release-title";
            }
            
            if (result.Children.Any(x => x.Symbol is IdentifierSymbol id && id.HasAlias("--artist-id")) &&
                result.Children.Any(x => x.Symbol is IdentifierSymbol id && id.HasAlias("--artist-name")))
            {
                result.ErrorMessage = "Cannot specify both --artist-id and --artist-name";
            }
            
            if ((result.Children.Any(x => x.Symbol is IdentifierSymbol id && id.HasAlias("--release-id")) ||
                 result.Children.Any(x => x.Symbol is IdentifierSymbol id && id.HasAlias("--release-title"))) &&
                (result.Children.Any(x => x.Symbol is IdentifierSymbol id && id.HasAlias("--artist-id")) ||
                 result.Children.Any(x => x.Symbol is IdentifierSymbol id && id.HasAlias("--artist-name"))))
            {
                result.ErrorMessage = "Cannot specify both a release and an artist";
            }
            
            if (!result.Children.Any(x => x.Symbol is IdentifierSymbol id && id.HasAlias("--release-id")) && 
                !result.Children.Any(x => x.Symbol is IdentifierSymbol id && id.HasAlias("--release-title")) &&
                !result.Children.Any(x => x.Symbol is IdentifierSymbol id && id.HasAlias("--artist-id")) &&
                !result.Children.Any(x => x.Symbol is IdentifierSymbol id && id.HasAlias("--artist-name")))
            {
                result.ErrorMessage = "Either --release-id, --release-title, --artist-id, or --artist-name must be specified";
            }
        });
    }
}