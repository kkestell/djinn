using System.Diagnostics;
using System.Runtime.InteropServices;
using Djinn.Configuration;
using Djinn.Models;
using Djinn.Utils;
using Soulseek;

namespace Djinn.Services;

public class SourceDownloader
{
    private const string SpinnerFrames = "⠁⠂⠄⡀⡈⡐⡠⣀⣁⣂⣄⣌⣔⣤⣥⣦⣮⣶⣷⣿⡿⠿⢟⠟⡛⠛⠫⢋⠋⠍⡉⠉⠑⠡⢁";
    private static int _spinnerFrameIndex;
    private static DateTime _lastProgressUpdate = DateTime.MinValue;
    private readonly MusicConfig _config;
    private readonly TransferOptions _transferOptions;
    private CancellationTokenSource _transferStoppingTokenSource = new();
    private Stopwatch _transferStopWatch = new();
    private CancellationTokenSource _globalCancellationSource = new();

    public SourceDownloader(MusicConfig config)
    {
        _config = config;

        if (!config.NoProgress)
            _transferOptions = new TransferOptions(
                stateChanged: args => PrintProgress(args.Transfer),
                progressUpdated: args => PrintProgress(args.Transfer));
        else
            _transferOptions = new TransferOptions();
    }

    public async Task<DownloadResult?> DownloadFiles(SoulseekClient soulseekClient, Album album,
        IReadOnlyList<SourceLocator.DownloadSource> downloadSources)
    {
        var idx = 1;

        foreach (var source in downloadSources)
        {
            if (_globalCancellationSource.Token.IsCancellationRequested)
            {
                Log.Information("SourceDownloader: Global cancellation requested, stopping downloads.");
                break;
            }

            Log.Information($"Downloading from {source.Username} ({idx++}/{downloadSources.Count})…");

            var tempDirectory = CreateTempDirectory();

            var cts = CancellationTokenSource.CreateLinkedTokenSource(_globalCancellationSource.Token);
            Log.Debug("SourceDownloader: Initialized CancellationTokenSource");

            if (_config.Watchdog?.TimeoutMinutes != null)
                cts.CancelAfter(TimeSpan.FromMinutes(_config.Watchdog.TimeoutMinutes.Value));

            // Register SIGQUIT handler for Unix-like systems to cancel all transfers
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                PosixSignalRegistration.Create(PosixSignal.SIGQUIT, sig =>
                {
                    Log.Information("SourceDownloader: Received SIGQUIT, cancelling all transfers.");
                    _globalCancellationSource.Cancel();
                });
            }

            try
            {
                Console.CancelKeyPress += CancelSourceHandler;
                Log.Debug("SourceDownloader: Registered CancelKeyPress handler");

                var downloadedFiles =
                    await DownloadFilesFromSource(soulseekClient, album, source, tempDirectory, cts.Token);

                if (downloadedFiles is not null)
                    return new DownloadResult(tempDirectory, downloadedFiles);

                Log.Verbose($"Download from {source.Username} failed");

                tempDirectory.Delete(true);
                Log.Verbose($"Deleted temporary directory {tempDirectory.FullName}");
            }
            catch (Exception e)
            {
                if (_config.Verbose)
                    Log.Error(e, $"Error downloading from {source.Username}");

                tempDirectory.Delete(true);
                Log.Verbose($"Deleted temporary directory {tempDirectory.FullName}");
            }
            finally
            {
                Console.CancelKeyPress -= CancelSourceHandler;
                Log.Debug("SourceDownloader: Cleared CancelKeyPress handler");
            }

            continue;

            void CancelSourceHandler(object? sender, ConsoleCancelEventArgs e)
            {
                e.Cancel = true;
                cts.Cancel();
                Log.Debug("SourceDownloader: Called cts.Cancel()");
            }
        }

        return null;
    }

    private async Task<Dictionary<Track, FileInfo>?> DownloadFilesFromSource(ISoulseekClient soulseekClient,
        Album album, SourceLocator.DownloadSource downloadSource, FileSystemInfo tempDirectory,
        CancellationToken stoppingToken)
    {
        Log.Verbose($"Downloading {downloadSource.Files.Count} files from {downloadSource.Username}…");
        var downloadedFiles = new Dictionary<Track, FileInfo>();
        foreach (var (track, file) in downloadSource.Files)
        {
            if (stoppingToken.IsCancellationRequested)
            {
                Log.Verbose("SourceDownloader: Transfer cancelled before it began.");
                return null;
            }

            _transferStoppingTokenSource = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
            _transferStopWatch = new Stopwatch();
            _transferStopWatch.Start();
            var trackFilename =
                $"{NameService.FormatTrack(track, album, _config.TrackFormat)}{Path.GetExtension(file.Filename)}";
            var trackPath = Path.Combine(tempDirectory.FullName, PathUtils.SanitizePath(trackFilename));
            var downloadFile = new FileInfo(trackPath);
            if (downloadFile.Exists)
            {
                Log.Verbose($"Skipping {track} (already exists)");
                return null;
            }

            try
            {
                Console.CursorVisible = false;
                var transfer = await soulseekClient.DownloadAsync(downloadSource.Username,
                    file.Filename, downloadFile.FullName, file.Size, options: _transferOptions,
                    cancellationToken: _transferStoppingTokenSource.Token);
                PrintProgress(transfer);
                Console.WriteLine();
                if (transfer.State.HasFlag(TransferStates.Aborted) ||
                    transfer.State.HasFlag(TransferStates.Cancelled) ||
                    transfer.State.HasFlag(TransferStates.Errored) ||
                    transfer.State.HasFlag(TransferStates.Rejected) ||
                    transfer.State.HasFlag(TransferStates.TimedOut))
                {
                    Log.Verbose($"Transfer failed: {transfer.State}");
                    return null;
                }

                var downloadFileSize = new FileInfo(downloadFile.FullName).Length;
                if (downloadFileSize != file.Size)
                {
                    Log.Verbose($"Transfer failed: file size mismatch ({downloadFileSize} != {file.Size})");
                    return null;
                }

                downloadedFiles.Add(track, downloadFile);
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine();
                Log.Verbose("Transfer cancelled");
                return null;
            }
            finally
            {
                Console.CursorVisible = true;
            }
        }

        return downloadedFiles;
    }

    private void PrintProgress(Transfer transfer)
    {
        if (_config.Watchdog?.TimeoutMinutes is not null &&
            _transferStopWatch.Elapsed.Seconds >= _config.Watchdog.DelaySeconds)
        {
            if (_config.Watchdog.MinimumSpeedBytes.HasValue &&
                transfer.AverageSpeed < _config.Watchdog.MinimumSpeedBytes.Value) _transferStoppingTokenSource.Cancel();
            if (_config.Watchdog.QueuedRemotely && transfer.State.HasFlag(TransferStates.Queued) &&
                transfer.State.HasFlag(TransferStates.Remotely)) _transferStoppingTokenSource.Cancel();
        }

        var filename = Path.GetFileName(transfer.Filename.Replace("\\", "/"));
        if (filename.Length > 53) filename = $"{filename[..50]}…";
        filename = filename.PadRight(53);
        var percentComplete = $"{Math.Round(transfer.PercentComplete)}%".PadLeft(4);
        var transferSpeed = $"{Math.Round(transfer.AverageSpeed / 1000)}".PadLeft(5);
        var state = TransferState(transfer).PadRight(17);
        var spinnerFrame = SpinnerFrames[_spinnerFrameIndex];
        if (DateTime.Now - _lastProgressUpdate >= TimeSpan.FromMilliseconds(80))
        {
            _spinnerFrameIndex = (_spinnerFrameIndex + 1) % SpinnerFrames.Length;
            _lastProgressUpdate = DateTime.Now;
        }

        if (Math.Round(transfer.PercentComplete) >= 100)
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write("⣿ ");
            Console.ResetColor();

            Console.ForegroundColor = ConsoleColor.Gray;
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.Write($"{spinnerFrame} ");
            Console.ResetColor();
            Console.ForegroundColor = ConsoleColor.White;
        }

        Console.Write($"{filename}  {percentComplete}  {state}  {transferSpeed} KB/s");
        Console.CursorLeft = 0;
        Console.ResetColor();
    }

    private DirectoryInfo CreateTempDirectory()
    {
        var tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var tempDirectory = new DirectoryInfo(tempPath);
        tempDirectory.Create();
        Log.Verbose($"Created temporary directory {tempDirectory.FullName}");
        return tempDirectory;
    }

    private static string TransferState(Transfer transfer)
    {
        if (transfer.State.HasFlag(TransferStates.Aborted))
            return "Aborted";
        if (transfer.State.HasFlag(TransferStates.Initializing))
            return "Initializing";
        if (transfer.State.HasFlag(TransferStates.Completed) && transfer.State.HasFlag(TransferStates.Succeeded))
            return "Completed";
        if (transfer.State.HasFlag(TransferStates.Completed) && transfer.State.HasFlag(TransferStates.Errored))
            return "Errored";
        if (transfer.State.HasFlag(TransferStates.Queued) && transfer.State.HasFlag(TransferStates.Remotely))
            return "Queued (Remotely)";
        if (transfer.State.HasFlag(TransferStates.Queued) && transfer.State.HasFlag(TransferStates.Locally))
            return "Queued (Locally)";
        if (transfer.State.HasFlag(TransferStates.Cancelled))
            return "Cancelled";
        if (transfer.State.HasFlag(TransferStates.InProgress))
            return "Downloading";
        if (transfer.State.HasFlag(TransferStates.Requested))
            return "Requested";
        if (transfer.State.HasFlag(TransferStates.Rejected))
            return "Rejected";
        if (transfer.State.HasFlag(TransferStates.TimedOut))
            return "Timed Out";
        return "Unknown";
    }

    public class DownloadResult
    {
        public DirectoryInfo TempDirectory { get; }
        public Dictionary<Track, FileInfo> Files { get; }

        public DownloadResult(DirectoryInfo tempDirectory, Dictionary<Track, FileInfo> files)
        {
            TempDirectory = tempDirectory;
            Files = files;
        }
    }
}
