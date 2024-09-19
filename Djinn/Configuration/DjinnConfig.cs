using System.Runtime.InteropServices;
using System.Text.Json;
using Djinn.Services;

namespace Djinn.Configuration;

public class ConfigurationError : Exception
{
    public ConfigurationError(string message) : base(message)
    {
    }
    
    public ConfigurationError(string message, Exception innerException) : base(message, innerException)
    {
    }
}

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
    public string SpotifyClientId { get; set; }
    public string SpotifyClientSecret { get; set; }
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

    public static void Validate()
    {
        var config = Load();
        
        if (string.IsNullOrEmpty(config.LibraryPath))
            throw new ConfigurationError("Library path is required");

        if (!Directory.Exists(config.LibraryPath))
            throw new ConfigurationError($"Library path does not exist: {config.LibraryPath}");

        if (string.IsNullOrEmpty(config.LastFmApiKey))
            throw new ConfigurationError("Last.fm API key is required");

        if (string.IsNullOrEmpty(config.LastFmApiSecret))
            throw new ConfigurationError("Last.fm API secret is required");
        
        if (string.IsNullOrEmpty(config.SpotifyClientId))
            throw new ConfigurationError("Spotify client ID is required");
        
        if (string.IsNullOrEmpty(config.SpotifyClientSecret))
            throw new ConfigurationError("Spotify client secret is required");

        if (string.IsNullOrEmpty(config.SoulseekUsername))
            throw new ConfigurationError("Soulseek username is required");

        if (string.IsNullOrEmpty(config.SoulseekPassword))
            throw new ConfigurationError("Soulseek password is required");

        if (string.IsNullOrEmpty(config.ArtistFormat))
            throw new ConfigurationError("Artist format is required");

        if (string.IsNullOrEmpty(config.AlbumFormat))
            throw new ConfigurationError("Album format is required");

        if (string.IsNullOrEmpty(config.TrackFormat))
            throw new ConfigurationError("Track format is required");
    }
    
    public static DjinnConfig Load()
    {
        DjinnConfig config;

        if (!File.Exists(ConfigPath))
            throw new ConfigurationError($"Config file not found at path: {ConfigPath}");

        try
        {
            var jsonContent = File.ReadAllText(ConfigPath);
            config = JsonSerializer.Deserialize<DjinnConfig>(jsonContent) ?? throw new ConfigurationError($"Failed to deserialize config file at path: {ConfigPath}");
        }
        catch (JsonException e)
        {
            throw new ConfigurationError($"Failed to deserialize config file at path: {ConfigPath}", e);
        }

        if (config is null)
            throw new ConfigurationError($"Failed to deserialize config file at path: {ConfigPath}");

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