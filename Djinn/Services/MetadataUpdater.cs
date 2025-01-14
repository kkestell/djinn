using System.Diagnostics;
using Djinn.Configuration;
using Djinn.Models;

namespace Djinn.Services;

public class MetadataUpdater
{
    private readonly DjinnConfig _config;

    public MetadataUpdater(DjinnConfig config)
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
                var error = $"FFmpeg process timed out after 5 minutes during {operation}.\n" +
                           $"Output:\n{outputBuilder}\n" +
                           $"Error:\n{errorBuilder}";
                throw new Exception(error);
            }

            if (process.ExitCode != 0)
            {
                var error = $"FFmpeg failed during {operation} with exit code {process.ExitCode}.\n" +
                           $"Output:\n{outputBuilder}\n" +
                           $"Error:\n{errorBuilder}";
                throw new Exception(error);
            }
        }
        catch (Exception ex) when (ex is not TimeoutException)
        {
            var error = $"Error running FFmpeg during {operation}: {ex.Message}\n" +
                       $"Output:\n{outputBuilder}\n" +
                       $"Error:\n{errorBuilder}";
            throw new Exception(error, ex);
        }
    }

    private void UpdateMetadata(FileInfo audioFile, Track track, Album album)
    {
        var tempFile = Path.Combine(
            Path.GetDirectoryName(audioFile.FullName)!,
            $"temp_{Path.GetFileName(audioFile.FullName)}");

        try
        {
            var metadata = new Dictionary<string, string>
            {
                {"title", track.Title},
                {"artist", album.Artist.Name},
                {"album", album.Title},
                {"track", track.Number.ToString()},
                {"date", album.Date.Year.ToString()},
                {"MUSICBRAINZ_ALBUMID", album.Id.ToString()},
                {"MUSICBRAINZ_ARTISTID", album.Artist.Id.ToString()},
                {"MUSICBRAINZ_TRACKID", track.Id.ToString()}
            };

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