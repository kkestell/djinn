using System.CommandLine.Invocation;
using System.Text;
using System.Text.Json;
using Djinn.Configuration;
using Djinn.Models;
using Djinn.Services;
using Djinn.Utils;
using Wcwidth;

namespace Djinn.Commands.Handlers;

public class CheckCommandHandler : ICommandHandler
{
    private readonly DjinnConfig _config = DjinnConfig.Load();
    private bool _fix;

    public async Task<int> InvokeAsync(InvocationContext context)
    {
        ParseOptions(context);

        await CheckAlbumDirectories();
        await CheckTracks();
        return 0;
    }
    
    private void ParseOptions(InvocationContext context)
    {
        _config.Verbose = context.ParseResult.GetValueForOption(CheckCommand.Verbose);

        if (_config.Verbose)
            Log.Level = LogLevel.Verbose;
        
        _fix = context.ParseResult.GetValueForOption(CheckCommand.Fix);
    }
    
    public int Invoke(InvocationContext context)
    {
        return InvokeAsync(context).GetAwaiter().GetResult();
    }
    
    private static string FormatDictionary(Dictionary<string, string> dictionary)
    {
        if (dictionary.Count == 0)
            return string.Empty;

        var maxKeyWidth = dictionary.Keys.Max(GetDisplayWidth);

        var lines = dictionary.Select(kv => 
        {
            var paddedKey = PadLeftByDisplayWidth(kv.Key, maxKeyWidth);
            return $"{paddedKey}: {kv.Value}";
        });

        return string.Join("\n", lines);
    }

    private static int GetDisplayWidth(string str) => str.Sum(c => UnicodeCalculator.GetWidth(c));

    private static string PadLeftByDisplayWidth(string text, int totalWidth)
    {
        var displayWidth = GetDisplayWidth(text);
        if (displayWidth >= totalWidth)
            return text;
            
        return new string(' ', totalWidth - displayWidth) + text;
    }
    
    private async Task CheckTracks()
    {
        Log.Information("Checking tracks...");
        
        var libraryDirectory = new DirectoryInfo(_config.LibraryPath);
        var artistDirectories = libraryDirectory.EnumerateDirectories().OrderBy(x => x.Name).ToList();
        
        foreach (var artistDirectory in artistDirectories)
        {
            var albumDirectories = artistDirectory.EnumerateDirectories()
                .OrderBy(x => x.Name)
                .ToList();
        
            foreach (var albumDirectory in albumDirectories)
            {
                var allFiles = albumDirectory.EnumerateFiles().ToList();
                var metadataFile = allFiles.FirstOrDefault(file => file.Name == ".metadata.json");
        
                if (metadataFile is null)
                {
                    Log.Warning($"No metadata file found in {artistDirectory.Name}/{albumDirectory.Name}");
                    continue;
                }
        
                var metadataAlbum = JsonSerializer.Deserialize<Album>(
                    await File.ReadAllTextAsync(metadataFile.FullName)
                );
        
                if (metadataAlbum is null)
                {
                    Log.Warning($"Unable to parse metadata file {metadataFile.FullName}");
                    continue;
                }
                
                // Get all audio files in the directory
                var audioFiles = allFiles.Where(file => file.Extension is ".mp3" or ".flac").ToList();
                
                if (!audioFiles.Any())
                {
                    Log.Warning($"No audio files found in {artistDirectory.Name}/{albumDirectory.Name}");
                    continue;
                }
                
                // Get the extension from the first audio file
                var audioExtension = audioFiles.First().Extension;
                
                // Generate expected track filenames with the same extension
                var expectedTrackFilenames = metadataAlbum.Tracks
                    .Select(track => PathUtils.SanitizePath(NameService.FormatTrack(track, metadataAlbum, _config.TrackFormat)) + audioExtension)
                    .ToHashSet();
                
                // Create a dictionary of actual filenames for quick lookup
                var actualAudioFilenamesDict = audioFiles
                    .ToDictionary(file => file.Name);
                
                // Get the actual audio filenames as a set for comparison
                var actualAudioFilenames = actualAudioFilenamesDict.Keys.ToHashSet();
                
                // Check for mismatches
                var missingFiles = expectedTrackFilenames.Except(actualAudioFilenames).ToList();
                var extraFiles = actualAudioFilenames.Except(expectedTrackFilenames).ToList();
                var hasAnyMismatches = missingFiles.Any() || extraFiles.Any();
                
                // Build a nice formatted output showing all tracks
                var trackListOutput = new StringBuilder();
                
                // Expected tracks first (in order)
                var orderedExpectedTracks = metadataAlbum.Tracks
                    .Select(track => PathUtils.SanitizePath(NameService.FormatTrack(track, metadataAlbum, _config.TrackFormat)) + audioExtension)
                    .OrderBy(filename => filename)
                    .ToList();
                
                // Format tracks with alignment
                var trackListItems = new List<string>();
                
                foreach (var expectedFilename in orderedExpectedTracks)
                {
                    if (actualAudioFilenames.Contains(expectedFilename))
                    {
                        // Track exists with correct name
                        trackListItems.Add(expectedFilename);
                    }
                    else
                    {
                        // Track with expected name doesn't exist
                        // Try to find a potential match among extra files (using track number prefix)
                        var trackNumberPrefix = expectedFilename.Split(' ').First();
                        var potentialMatch = extraFiles.FirstOrDefault(f => f.StartsWith(trackNumberPrefix));
                        
                        if (potentialMatch != null)
                        {
                            trackListItems.Add($"{potentialMatch} -> {expectedFilename}");
                            extraFiles.Remove(potentialMatch); // Remove from extra files since we've matched it
                        }
                        else
                        {
                            // No potential match found
                            trackListItems.Add($"[MISSING] -> {expectedFilename}");
                        }
                    }
                }
                
                // Add any remaining extra files
                foreach (var extraFile in extraFiles.OrderBy(f => f))
                {
                    trackListItems.Add($"{extraFile} -> [UNEXPECTED]");
                }
                
                // Join track list items with proper indentation
                trackListOutput.Append(string.Join("\n           ", trackListItems));
                
                // Create metadata dictionary for output
                var statusLine = hasAnyMismatches ? "MISMATCH" : "OK";
                var infoDictionary = new Dictionary<string, string>
                {
                    {"Artist(s)", string.Join(", ", metadataAlbum.ArtistNames)},
                    {"Album", metadataAlbum.Title},
                    {"Tracks", trackListOutput.ToString()},
                    {"Status", statusLine}
                };
                
                var outputMessage = FormatDictionary(infoDictionary);
                
                if (hasAnyMismatches)
                {
                    Log.Warning(outputMessage);
                    continue;
                }

                Log.Verbose(outputMessage);


                foreach (var track in metadataAlbum.Tracks)
                {
                    var trackFileInfo = new FileInfo(
                        Path.Combine(
                            _config.LibraryPath,
                            artistDirectory.Name,
                            albumDirectory.Name,
                            PathUtils.SanitizePath(NameService.FormatTrack(track, metadataAlbum, _config.TrackFormat)) +
                            audioExtension
                        )
                    );
                    
                    if (!trackFileInfo.Exists)
                    {
                        Log.Warning($"Track file {trackFileInfo.FullName} does not exist");
                        continue;
                    }
                }
            }
        }
    }
    
    private async Task CheckAlbumDirectories()
    {
        Log.Information("Checking album directories...");
        
        var libraryDirectory = new DirectoryInfo(_config.LibraryPath);
        var artistDirectories = libraryDirectory.EnumerateDirectories().OrderBy(x => x.Name).ToList();

        foreach (var artistDirectory in artistDirectories)
        {
            var albumDirectories = artistDirectory.EnumerateDirectories()
                .OrderBy(x => x.Name).ToList();

            foreach (var albumDirectory in albumDirectories)
            {
                var albumDirectoryFiles = albumDirectory.EnumerateFiles()
                    .ToList();

                var metadataFile = albumDirectoryFiles.FirstOrDefault(file => file.Name == ".metadata.json");

                if (metadataFile is null)
                {
                    Log.Error($"No metadata file found in {artistDirectory.Name}/{albumDirectory.Name}");
                    continue;
                }

                var metadataAlbum = JsonSerializer.Deserialize<Album>(
                    await File.ReadAllTextAsync(metadataFile.FullName)
                );

                if (metadataAlbum is null)
                {
                    Log.Error($"Unable to parse metadata file {metadataFile.FullName}");
                    continue;
                }

                var expectedArtistDirectoryName = PathUtils.SanitizePath(
                    NameService.FormatArtist(metadataAlbum, _config.ArtistFormat)
                );

                var expectedAlbumDirectoryName = PathUtils.SanitizePath(
                    NameService.FormatAlbum(metadataAlbum, _config.AlbumFormat)
                );

                var actualPath = Path.Combine(
                    libraryDirectory.FullName,
                    artistDirectory.Name,
                    albumDirectory.Name
                );

                var expectedPath = Path.Combine(
                    libraryDirectory.FullName,
                    PathUtils.SanitizePath(expectedArtistDirectoryName),
                    PathUtils.SanitizePath(expectedAlbumDirectoryName)
                );

                // Create info dictionary for output
                var infoDictionary = new Dictionary<string, string>
                {
                    {"Artist(s)", string.Join(", ", metadataAlbum.ArtistNames)},
                    {"Album", metadataAlbum.Title},
                    {"Current Path", actualPath}
                };

                // If the directory names are already correct, skip
                if (PathUtils.NormalizePath(artistDirectory.Name) == PathUtils.NormalizePath(expectedArtistDirectoryName) &&
                    PathUtils.NormalizePath(albumDirectory.Name) == PathUtils.NormalizePath(expectedAlbumDirectoryName))
                {
                    infoDictionary.Add("Status", "OK");
                    Log.Verbose(FormatDictionary(infoDictionary));
                    continue;
                }

                // Add expected path for mismatches
                infoDictionary.Add("New Path", expectedPath);
                infoDictionary.Add("Status", "MISMATCH");
                
                Log.Warning(FormatDictionary(infoDictionary));

                // Avoid gnarly case-sensitivity issues on Windows
                if (string.Equals(PathUtils.NormalizePath(actualPath), PathUtils.NormalizePath(expectedPath), StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(PathUtils.NormalizePath(actualPath), PathUtils.NormalizePath(expectedPath), StringComparison.Ordinal))
                {
                    Log.Error("Directory names differ only by case. This is not supported on Windows. Please rename the directories manually.");
                    continue;
                }
                
                if (_fix)
                {
                    var expectedDirectory = new DirectoryInfo(expectedPath);

                    // Check if the expected directory already exists
                    if (expectedDirectory.Exists)
                    {
                        Log.Warning(
                            $"Expected directory {expectedDirectory.FullName} already exists. Skipping rename. Please resolve this manually."
                        );
                        continue;
                    }

                    // Create the expected directory
                    Log.Information($"Creating {expectedDirectory.FullName}");
                    expectedDirectory.Create();

                    // Move the files to the new directory
                    foreach (var file in albumDirectoryFiles)
                    {
                        var newFilePath = Path.Combine(expectedDirectory.FullName, file.Name);
                        Log.Information(
                            $"Moving {artistDirectory.Name}/{albumDirectory.Name}/{file.Name} to {expectedArtistDirectoryName}/{expectedAlbumDirectoryName}/{file.Name}"
                        );
                        File.Move(file.FullName, newFilePath);
                    }

                    // Delete the old directory if it's empty
                    if (!albumDirectory.EnumerateFiles().Any())
                    {
                        Log.Information($"Deleting empty directory {albumDirectory.FullName}");
                        albumDirectory.Delete();
                    }
                }
            }
        }
    }
}
