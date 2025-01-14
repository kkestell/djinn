using System.Runtime.InteropServices;
using System.Text.Json;
using Djinn.Services;

namespace Djinn.Configuration;

public class ConfigurationError : Exception
{
    public ConfigurationError(string message) : base(message) { }

    public ConfigurationError(string message, Exception innerException) : base(message, innerException) { }
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
    public string FfmpegPath { get; set; }
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
        // Initialize with default values
        ArtistFormat = "%S";
        AlbumFormat = "%Y %T";
        TrackFormat = "%n %t";
    }

    private static string GetDefaultLibraryPath()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "Music"
            );
        }
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "Music"
        );
    }

    private static string GetDefaultFfmpegPath()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return @"C:\Program Files\ffmpeg\bin\ffmpeg.exe";
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return "/usr/local/bin/ffmpeg";
        }
        return "/usr/bin/ffmpeg";  // Linux default
    }

    private static string PromptForValue(string prompt, string defaultValue = "", bool isRequired = false, bool isPassword = false)
    {
        Console.Write($"{prompt} [{defaultValue}]: ");

        string value = "";
        if (isPassword)
        {
            while (true)
            {
                var key = Console.ReadKey(true);
                if (key.Key == ConsoleKey.Enter)
                {
                    Console.WriteLine();
                    break;
                }
                if (key.Key == ConsoleKey.Backspace && value.Length > 0)
                {
                    value = value[..^1];
                    Console.Write("\b \b");
                }
                else if (key.Key != ConsoleKey.Backspace)
                {
                    value += key.KeyChar;
                    Console.Write("*");
                }
            }
        }
        else
        {
            value = Console.ReadLine() ?? "";
        }

        if (string.IsNullOrWhiteSpace(value))
        {
            value = defaultValue;
        }

        if (isRequired && string.IsNullOrWhiteSpace(value))
        {
            Console.WriteLine("This field is required. Please enter a value.");
            return PromptForValue(prompt, defaultValue, isRequired, isPassword);
        }

        return value;
    }

    public static DjinnConfig CreateInteractive()
    {
        Console.WriteLine("Welcome to Djinn Configuration Setup!");
        Console.WriteLine("Press Enter to accept the default value (shown in brackets) or type your own.");
        Console.WriteLine();

        var config = new DjinnConfig
        {
            LibraryPath = PromptForValue("Library Path", GetDefaultLibraryPath(), true),
            FfmpegPath = PromptForValue("FFmpeg Path", GetDefaultFfmpegPath(), true),

            LastFmApiKey = PromptForValue("Last.fm API Key", ""),
            LastFmApiSecret = PromptForValue("Last.fm API Secret", ""),

            SpotifyClientId = PromptForValue("Spotify Client ID", ""),
            SpotifyClientSecret = PromptForValue("Spotify Client Secret", ""),

            SoulseekUsername = PromptForValue("Soulseek Username", "", true),
            SoulseekPassword = PromptForValue("Soulseek Password", "", true, true),

            ArtistFormat = PromptForValue("Artist Format", "%S"),
            AlbumFormat = PromptForValue("Album Format", "%Y %T"),
            TrackFormat = PromptForValue("Track Format", "%n %t")
        };

        // Ensure the config directory exists
        var configDir = Path.GetDirectoryName(ConfigPath);
        if (!string.IsNullOrEmpty(configDir))
        {
            Directory.CreateDirectory(configDir);
        }

        // Save the config
        var jsonOptions = new JsonSerializerOptions { WriteIndented = true };
        File.WriteAllText(ConfigPath, JsonSerializer.Serialize(config, jsonOptions));

        Console.WriteLine($"\nConfiguration saved to: {ConfigPath}");

        return config;
    }

    public static void Validate()
    {
        var config = Load();

        if (string.IsNullOrEmpty(config.LibraryPath))
            throw new ConfigurationError("Library path is required");

        if (string.IsNullOrEmpty(config.FfmpegPath))
            throw new ConfigurationError("Ffmpeg path is required");

        if (!Directory.Exists(config.LibraryPath))
            throw new ConfigurationError($"Library path does not exist: {config.LibraryPath}");

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
        if (!File.Exists(ConfigPath))
        {
            Console.WriteLine($"No configuration file found at {ConfigPath}");
            return CreateInteractive();
        }

        try
        {
            var jsonContent = File.ReadAllText(ConfigPath);
            var config = JsonSerializer.Deserialize<DjinnConfig>(jsonContent)
                ?? throw new ConfigurationError($"Failed to deserialize config file at path: {ConfigPath}");

            return config;
        }
        catch (JsonException e)
        {
            throw new ConfigurationError($"Failed to deserialize config file at path: {ConfigPath}", e);
        }
    }

    public static string ConfigPath
    {
        get
        {
            string defaultConfigPath;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                defaultConfigPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Djinn", "config.json");
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