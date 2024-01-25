using System.Diagnostics;
using Djinn.Configuration;
using Djinn.Metadata;
using Djinn.Models;

namespace Djinn.Services;

// public class MetadataUpdater
// {
//     private readonly DjinnConfig _config;
//
//     public MetadataUpdater(DjinnConfig config)
//     {
//         _config = config;
//     }
//
//     public void Update(Album album, Dictionary<Track, FileInfo> trackFiles, FileInfo? coverImageFile)
//     {
//         foreach (var (track, file) in trackFiles)
//         {
//             var audioFile = AudioFileLoader.Load(file.FullName);
//
//             if (_config.StripExistingMetadata)
//                 audioFile.Clear();
//
//             audioFile.Set(Field.Title, track.Title);
//             audioFile.Set(Field.Album, album.Title);
//             audioFile.Set(Field.Artist, album.Artist.Name);
//             audioFile.Set(Field.TrackNumber, track.Number);
//             audioFile.Set(Field.TotalTracks, album.Tracks.Count);
//             audioFile.Set(Field.Date, album.Date.Year);
//             audioFile.Set(Field.MusicbrainzTrackId, track.Id);
//             audioFile.Set(Field.MusicbrainzReleaseGroupId, album.Id);
//             audioFile.Set(Field.MusicbrainzArtistId, album.Artist.Id);
//
//             audioFile.Save();
//         }
//     }
// }

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

            var tagFile = TagLib.File.Create(audioFile.FullName);

            tagFile.Tag.MusicBrainzReleaseId = album.Id.ToString();
            tagFile.Tag.MusicBrainzArtistId = album.Artist.Id.ToString();
            tagFile.Tag.MusicBrainzTrackId = track.Id.ToString();
            tagFile.Tag.Track = (uint)track.Number;
            tagFile.Tag.TrackCount = (uint)album.Tracks.Count;
            tagFile.Tag.Title = track.Title;
            tagFile.Tag.TitleSort = track.Title;
            tagFile.Tag.Album = album.Title;
            tagFile.Tag.AlbumSort = album.Title;
            tagFile.Tag.AlbumArtists = [album.Artist.Name];
            tagFile.Tag.AlbumArtistsSort = [album.Artist.SortName];
            tagFile.Tag.Artists = [album.Artist.Name];
            tagFile.Tag.Year = (uint)album.Date.Year;

            tagFile.Save();

            if (coverImageFile is not null)
                AddCoverImage(audioFile, coverImageFile);
        }
    }

    private static void AddCoverImage(FileInfo audioFile, FileInfo coverFile)
    {
        string filename;
        string arguments;

        switch (audioFile.Extension)
        {
            case ".mp3":
                filename = "eyeD3";
                arguments = $"--add-image={coverFile.FullName}:FRONT_COVER \"{audioFile.FullName}\"";
                break;
            case ".flac":
                filename = "metaflac";
                arguments = $"--import-picture-from=\"3||||{coverFile.FullName}\" \"{audioFile.FullName}\"";
                break;
            default:
                throw new ArgumentException($"Unsupported file type: {audioFile.Extension}",nameof(audioFile));
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = filename,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        var process = new Process
        {
            StartInfo = startInfo
        };

        process.Start();
        process.WaitForExit();

        // var output = process.StandardOutput.ReadToEnd();
        //
        // Console.WriteLine($"{filename} {arguments}");
        // Console.WriteLine($"Exit code: {process.ExitCode}");
        // Console.WriteLine(output);
    }

    private static void StripMetadata(FileInfo file)
    {
        string filename;
        string arguments;

        switch (file.Extension)
        {
            case ".mp3":
                filename = "eyeD3";
                arguments = $"--remove-all \"{file.FullName}\"";
                break;
            case ".flac":
                filename = "metaflac";
                arguments = $"--remove-all \"{file.FullName}\"";
                break;
            default:
                throw new ArgumentException($"Unsupported file type: {file.Extension}",nameof(file));
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = filename,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        var process = new Process
        {
            StartInfo = startInfo
        };

        process.Start();
        process.WaitForExit();

        // var output = process.StandardOutput.ReadToEnd();
        //
        // Console.WriteLine($"{filename} {arguments}");
        // Console.WriteLine($"Exit code: {process.ExitCode}");
        // Console.WriteLine(output);
    }
}