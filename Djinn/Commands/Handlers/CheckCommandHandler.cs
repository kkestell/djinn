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
    private readonly MusicConfig _config = MusicConfig.Load();
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
                if (artistDirectory.Name == expectedArtistDirectoryName &&
                    albumDirectory.Name == expectedAlbumDirectoryName)
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
                if (string.Equals(actualPath, expectedPath, StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(actualPath, expectedPath, StringComparison.Ordinal))
                {
                    Log.Error("Directory names differ only by case. This is not supported on Windows. Please rename the directories manually.");
                    continue;
                }
                
                // FIXME: Guard by --fix
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

// public class CheckCommandHandler : ICommandHandler
// {
//     public async Task<int> InvokeAsync(InvocationContext context)
//     {
//         var config = DjinnConfig.Load();
//         
//         var libraryDirectory = new DirectoryInfo(config.LibraryPath);
//         var artistDirectories = libraryDirectory.EnumerateDirectories().OrderBy(x => x.Name).ToList();
//         
//         var musicBrainzService = new MusicBrainzService();
//         var metadataService = new MetadataService(config);
//
//         foreach (var artistDirectory in artistDirectories)
//         {
//             var albumDirectories = artistDirectory.EnumerateDirectories().OrderBy(x => x.Name).ToList();
//             foreach (var albumDirectory in albumDirectories)
//             {
//                 var trackFiles = albumDirectory.EnumerateFiles()
//                     .ToList();
//
//                 var metadataFile = trackFiles.FirstOrDefault(file => file.Name.Equals(
//                         ".metadata.json",
//                         StringComparison.OrdinalIgnoreCase
//                     )
//                 );
//                 
//                 // var numAudioFiles = trackFiles.Count(file => file.Extension.Equals(".flac", StringComparison.OrdinalIgnoreCase) || 
//                 //                                                 file.Extension.Equals(".mp3", StringComparison.OrdinalIgnoreCase));
//                 if (metadataFile is null)
//                 {
//                     Console.WriteLine($"Error: No metadata file found for {artistDirectory.Name}/{albumDirectory.Name}");
//                     continue;
//
//                     // var album = await musicBrainzService.FindAlbum(albumDirectory.Name, artistDirectory.Name, numAudioFiles);
//                     //
//                     // if (album is null)
//                     // {
//                     //     Console.WriteLine($"Warning: No metadata file found and unable to find album {albumDirectory.Name}");
//                     //     continue;
//                     // }
//                     //
//                     // // var album = await musicBrainzService.FindAlbum(releaseId);
//                     // var newMetadataJson = JsonSerializer.Serialize(album, new JsonSerializerOptions
//                     // {
//                     //     WriteIndented = true,
//                     //     Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
//                     // });
//                     // metadataFile = new FileInfo(Path.Combine(albumDirectory.FullName, ".metadata.json"));
//                     // await File.WriteAllTextAsync(metadataFile.FullName, newMetadataJson);
//                     // Console.WriteLine($"Updated album metadata in {metadataFile.FullName}");
//                 }
//                 else
//                 {
//                     var metadataAlbum = JsonSerializer.Deserialize<Album>(
//                         await File.ReadAllTextAsync(metadataFile.FullName),
//                         new JsonSerializerOptions
//                         {
//                             PropertyNameCaseInsensitive = true
//                         }
//                     );
//                     
//                     if (metadataAlbum is null)
//                     {
//                         Console.WriteLine($"Warning: Unable to parse metadata file {metadataFile.FullName}");
//                         continue;
//                     }
//
//                     var expectedArtistDirectoryName = NameService.FormatArtist(metadataAlbum, config.ArtistFormat);
//                     var expectedAlbumDirectoryName = NameService.FormatAlbum(metadataAlbum, config.AlbumFormat);
//                     
//                     if (!artistDirectory.Name.Equals(expectedArtistDirectoryName, StringComparison.OrdinalIgnoreCase))
//                     {
//                         Console.WriteLine($"Warning: Artist directory name mismatch. Expected {expectedArtistDirectoryName}, found {artistDirectory.Name}");
//                     }
//                     
//                     if (!albumDirectory.Name.Equals(expectedAlbumDirectoryName, StringComparison.OrdinalIgnoreCase))
//                     {
//                         Console.WriteLine($"Warning: Album directory name mismatch. Expected {expectedAlbumDirectoryName}, found {albumDirectory.Name}");
//                     }
//                 }
//
//                 //
//                 // Guid? releaseId = null;
//                 //
//                 // if (metadataFile is null)
//                 // {
//                 //     Console.WriteLine($"No metadata file found for {artistDirectory.Name}/{albumDirectory.Name}");
//                 //     continue;
//                 //     
//                 //     var release = await musicBrainzService.FindAlbum(albumDirectory.Name);
//                 //     
//                 //     if (release is null)
//                 //     {
//                 //         Console.WriteLine($"Warning: No metadata file found and unable to find album {albumDirectory.Name}");
//                 //         continue;
//                 //     }
//                 //     
//                 //     // If we found by name, get the ID from the release
//                 //     releaseId = release.Id;
//                 // }
//                 // else
//                 // {
//                 //     var metadataJson = await File.ReadAllTextAsync(metadataFile.FullName);
//                 //     var parsedMetadata = JsonDocument.Parse(metadataJson);
//                 //
//                 //     // Extract the release ID regardless of metadata format
//                 //     Guid extractedId;
//                 //     if (!TryGetReleaseId(parsedMetadata.RootElement, out extractedId))
//                 //     {
//                 //         Console.WriteLine($"Warning: Could not extract release ID from {metadataFile.FullName}");
//                 //         continue;
//                 //     }
//                 //     
//                 //     releaseId = extractedId;
//                 // }
//                 //
//                 // // Ensure we have a release ID at this point
//                 // if (!releaseId.HasValue)
//                 // {
//                 //     Console.WriteLine($"Warning: No release ID found for {albumDirectory.Name}");
//                 //     continue;
//                 // }
//                 //
//                 // // Look up the album on MusicBrainz using the ID
//                 //
//                 // Console.WriteLine($"Looking up {artistDirectory.Name}/{albumDirectory.Name} on MusicBrainz");
//                 //
//                 // var album = await musicBrainzService.FindAlbum(releaseId.Value);
//                 //
//                 // if (album is null)
//                 // {
//                 //     Console.WriteLine($"Warning: Album not found in MusicBrainz for id {releaseId}");
//                 //     continue;
//                 // }
//                 //
//                 // // Create path for metadata file if it doesn't exist
//                 // if (metadataFile is null)
//                 // {
//                 //     metadataFile = new FileInfo(Path.Combine(albumDirectory.FullName, ".metadata.json"));
//                 // }
//                 //
//                 // // Recreate the metadata file with fresh data
//                 // var newMetadataJson = JsonSerializer.Serialize(album, new JsonSerializerOptions
//                 // {
//                 //     WriteIndented = true,
//                 //     Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
//                 // });
//                 //
//                 // await File.WriteAllTextAsync(metadataFile.FullName, newMetadataJson);
//                 //
//                 // Console.WriteLine($"Updated album metadata in {metadataFile.FullName}");
//             }
//         }
//         
//         return 0;
//     }
//     
//     private bool TryGetReleaseId(JsonElement root, out Guid releaseId)
//     {
//         releaseId = Guid.Empty;
//         
//         // Try to extract ID directly from root with lowercase "id" (new-style)
//         if (root.TryGetProperty("id", out var idProperty) && 
//             idProperty.ValueKind == JsonValueKind.String &&
//             !string.IsNullOrWhiteSpace(idProperty.GetString()) &&
//             Guid.TryParse(idProperty.GetString(), out releaseId))
//         {
//             return true;
//         }
//         
//         // Try to extract ID directly from root with uppercase "Id" (case in provided example)
//         if (root.TryGetProperty("Id", out idProperty) && 
//             idProperty.ValueKind == JsonValueKind.String &&
//             !string.IsNullOrWhiteSpace(idProperty.GetString()) &&
//             Guid.TryParse(idProperty.GetString(), out releaseId))
//         {
//             return true;
//         }
//         
//         return false;
//     }
//
//     public int Invoke(InvocationContext context)
//     {
//         return InvokeAsync(context).GetAwaiter().GetResult();
//     }
// }