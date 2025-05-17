using System.Diagnostics;
using System.Text.Json;
using Music.Configuration;
using Music.Models;

namespace Music.Services;

public class MetadataService
{
    private readonly MusicConfig _config;

    public MetadataService(MusicConfig config)
    {
        _config = config;
    }

    public void Update(Album album, Dictionary<Track, FileInfo> trackFiles, FileInfo? coverImageFile)
    {
        foreach (var (track, audioFile) in trackFiles)
        {
            if (_config.StripExistingMetadata)
                StripMetadata(audioFile);

            UpdateMetadata(audioFile, track, album);

            if (coverImageFile is not null)
                AddCoverImage(audioFile, coverImageFile);
        }
    }

    public Dictionary<string, string> GetMetadata(FileInfo trackFile)
    {
        Log.Verbose($"Retrieving metadata for {trackFile.Name}...");

        var startInfo = new ProcessStartInfo
        {
            FileName = _config.FfprobePath ?? Path.Combine(Path.GetDirectoryName(_config.FfmpegPath) ?? string.Empty, "ffprobe"),
            // Modified arguments to ensure we get format tags and ensure proper format specification
            Arguments = $"-v quiet -print_format json -show_entries format_tags -i \"{trackFile.FullName}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        Log.Verbose($"FFprobe command: {startInfo.FileName} {startInfo.Arguments}");

        var outputBuilder = new System.Text.StringBuilder();
        var errorBuilder = new System.Text.StringBuilder();

        using var process = new Process { StartInfo = startInfo };

        process.OutputDataReceived += (sender, args) =>
        {
            if (args.Data != null)
            {
                outputBuilder.AppendLine(args.Data);
                if (_config.Verbose)
                {
                    Log.Verbose($"FFprobe: {args.Data}");
                }
            }
        };

        process.ErrorDataReceived += (sender, args) =>
        {
            if (args.Data != null)
            {
                errorBuilder.AppendLine(args.Data);
                if (_config.Verbose)
                {
                    Log.Verbose($"FFprobe Error: {args.Data}");
                }
            }
        };

        try
        {
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            if (!process.WaitForExit(60000))
            {
                process.Kill();
                var error = $"FFprobe process timed out after 1 minute.\nOutput:\n{outputBuilder}\nError:\n{errorBuilder}";
                throw new Exception(error);
            }

            if (process.ExitCode != 0)
            {
                var error = $"FFprobe failed with exit code {process.ExitCode}.\nOutput:\n{outputBuilder}\nError:\n{errorBuilder}";
                throw new Exception(error);
            }

            var jsonOutput = outputBuilder.ToString();
            var result = new Dictionary<string, string>();

            if (string.IsNullOrWhiteSpace(jsonOutput))
                return result;

            using var jsonDoc = JsonDocument.Parse(jsonOutput);
            
            // Changed path to match ffprobe's output format when using -show_entries format_tags
            if (jsonDoc.RootElement.TryGetProperty("format", out var formatElement) && 
                formatElement.TryGetProperty("tags", out var tagsElement))
            {
                foreach (var property in tagsElement.EnumerateObject())
                {
                    result[property.Name.ToLowerInvariant()] = property.Value.GetString() ?? string.Empty;
                }
            }

            Log.Verbose($"Retrieved {result.Count} metadata tags from {trackFile.Name}");
            return result;
        }
        catch (Exception ex)
        {
            var error = $"Error retrieving metadata for {trackFile.Name}: {ex.Message}\nOutput:\n{outputBuilder}\nError:\n{errorBuilder}";
            throw new Exception(error, ex);
        }
    }
    
    private void RunFfmpegProcess(ProcessStartInfo startInfo, string operation)
    {
        Log.Verbose($"Running FFmpeg {operation}...");
        Log.Verbose($"Command: {startInfo.FileName} {startInfo.Arguments}");

        using var process = new Process { StartInfo = startInfo };

        var outputBuilder = new System.Text.StringBuilder();
        var errorBuilder = new System.Text.StringBuilder();

        process.OutputDataReceived += (sender, args) =>
        {
            if (args.Data != null)
            {
                outputBuilder.AppendLine(args.Data);
                if (_config.Verbose)
                {
                    Log.Verbose($"FFmpeg: {args.Data}");
                }
            }
        };

        process.ErrorDataReceived += (sender, args) =>
        {
            if (args.Data != null)
            {
                errorBuilder.AppendLine(args.Data);
                if (_config.Verbose)
                {
                    Log.Verbose($"FFmpeg Error: {args.Data}");
                }
            }
        };

        try
        {
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            if (!process.WaitForExit(300000))
            {
                process.Kill();
                var error = $"FFmpeg process timed out after 5 minutes during {operation}.\nOutput:\n{outputBuilder}\nError:\n{errorBuilder}";
                throw new Exception(error);
            }

            if (process.ExitCode != 0)
            {
                var error = $"FFmpeg failed during {operation} with exit code {process.ExitCode}.\nOutput:\n{outputBuilder}\nError:\n{errorBuilder}";
                throw new Exception(error);
            }
        }
        catch (Exception ex) when (ex is not TimeoutException)
        {
            var error = $"Error running FFmpeg during {operation}: {ex.Message}\nOutput:\n{outputBuilder}\nError:\n{errorBuilder}";
            throw new Exception(error, ex);
        }
    }

    public Dictionary<string, string> BuildMetadata(Album album, Track track)
    {
        // Join multiple artists with separator (standard is " / ")
        var artistNames = string.Join(", ", album.Artists.Select(a => a.Name));
        var artistIds = string.Join(" / ", album.Artists.Select(a => a.Id.ToString()));

        var metadata = new Dictionary<string, string>
        {
            {"title", track.Title},
            {"artist", artistNames},
            {"album", album.Title},
            {"track", $"{track.Number}/{album.Tracks.Count}"},
            {"date", album.Date.Year.ToString()},
            {"MUSICBRAINZ_ALBUMID", album.Id.ToString()},
            {"MUSICBRAINZ_ARTISTID", artistIds},
            {"MUSICBRAINZ_TRACKID", track.Id.ToString()}
        };
        
        // Add albumartist tag for player compatibility
        metadata.Add("albumartist", artistNames);
            
        // For better compatibility with players that support multiple values in different ways
        if (album.Artists.Count > 1)
        {
            metadata.Add("MUSICBRAINZ_ALBUMARTISTID", artistIds);
        }

        return metadata;
    }

    private void UpdateMetadata(FileInfo audioFile, Track track, Album album)
    {
        var tempFile = Path.Combine(
            Path.GetDirectoryName(audioFile.FullName)!,
            $"temp_{Path.GetFileName(audioFile.FullName)}");

        try
        {
            var metadata = BuildMetadata(album, track);

            var metadataArgs = string.Join(" ",
                metadata.Select(kv => $"-metadata {kv.Key}=\"{kv.Value}\""));

            var startInfo = new ProcessStartInfo
            {
                FileName = _config.FfmpegPath,
                Arguments = $"-i \"{audioFile.FullName}\" -c copy {metadataArgs} \"{tempFile}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            RunFfmpegProcess(startInfo, $"updating metadata for {audioFile.Name}");

            File.Delete(audioFile.FullName);
            File.Move(tempFile, audioFile.FullName);
        }
        catch
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
            throw;
        }
    }

    private void AddCoverImage(FileInfo audioFile, FileInfo coverFile)
    {
        var tempFile = Path.Combine(
            Path.GetDirectoryName(audioFile.FullName)!,
            $"temp_{Path.GetFileName(audioFile.FullName)}");

        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = _config.FfmpegPath,
                Arguments = $"-i \"{audioFile.FullName}\" -i \"{coverFile.FullName}\" -map 0 -map 1 -c copy " +
                           $"-disposition:v:0 attached_pic \"{tempFile}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            RunFfmpegProcess(startInfo, $"adding cover image to {audioFile.Name}");

            File.Delete(audioFile.FullName);
            File.Move(tempFile, audioFile.FullName);
        }
        catch
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
            throw;
        }
    }

    private void StripMetadata(FileInfo file)
    {
        var tempFile = Path.Combine(
            Path.GetDirectoryName(file.FullName)!,
            $"temp_{Path.GetFileName(file.FullName)}");

        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = _config.FfmpegPath,
                Arguments = $"-i \"{file.FullName}\" -map_metadata -1 -c copy \"{tempFile}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            RunFfmpegProcess(startInfo, $"stripping metadata from {file.Name}");

            File.Delete(file.FullName);
            File.Move(tempFile, file.FullName);
        }
        catch
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
            throw;
        }
    }
}