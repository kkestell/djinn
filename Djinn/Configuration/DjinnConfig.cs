using System.Runtime.InteropServices;
using System.Text.Json;

namespace Djinn.Configuration;

public class DjinnWatchdogConfig
{
    public int? TimeoutMinutes { get; set; }
    public int? DelaySeconds { get; set; }
    public int? MinimumSpeedBytes { get; set; }
    public bool QueuedRemotely { get; set; }
}

public class DjinnConfig
{
    public string LibraryPath { get; set; }
    public string LastFmApiKey { get; set; }
    public string LastFmApiSecret { get; set; }
    public string SoulseekUsername { get; set; }
    public string SoulseekPassword { get; set; }
    public string ArtistFormat { get; set; }
    public string AlbumFormat { get; set; }
    public string TrackFormat { get; set; }
    public DjinnWatchdogConfig? Watchdog { get; set; }

    public bool Verbose { get; set; }
    public bool NoProgress { get; set; }
    public bool StripExistingMetadata { get; set; }

    public DjinnConfig()
    {
    }

    public static DjinnConfig Load()
    {
        if (!File.Exists(ConfigPath))
            throw new FileNotFoundException($"Config file not found at path: {ConfigPath}");

        var jsonContent = File.ReadAllText(ConfigPath);

        var config = JsonSerializer.Deserialize<DjinnConfig>(jsonContent);

        if (config is null)
            throw new Exception($"Failed to deserialize config file at path: {ConfigPath}");

        return config;
    }

    public static string ConfigPath
    {
        get
        {
            string defaultConfigPath;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                defaultConfigPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "djinn", "config.json");
            }
            else
            {
                defaultConfigPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config", "djinn", "config.json");
            }
        
            var configPath = Environment.GetEnvironmentVariable("DJINN_CONFIG") ?? defaultConfigPath;
            return configPath;
        }
    }
}