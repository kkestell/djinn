using System.CommandLine.Invocation;
using Djinn.Collections;
using Djinn.Configuration;
using Djinn.Models;
using Djinn.Services;

namespace Djinn.Commands.Handlers;

public class ConfigCommandHandler : ICommandHandler
{
    public Task<int> InvokeAsync(InvocationContext context)
    {
        var configPath = DjinnConfig.ConfigPath;
        var config = DjinnConfig.Load();
        
        Log.Information($"Configuration loaded from {configPath}");
        Log.Information($"Library path:       {config.LibraryPath}");
        Log.Information($"Last.fm API key:    {config.LastFmApiKey}");
        Log.Information($"Last.fm API secret: {config.LastFmApiSecret}");
        Log.Information($"Soulseek username:  {config.SoulseekUsername}");
        Log.Information($"Soulseek password:  {config.SoulseekPassword}");
        Log.Information($"Artist format:      {config.ArtistFormat}");
        Log.Information($"Album format:       {config.AlbumFormat}");
        Log.Information($"Track format:       {config.TrackFormat}");
        
        if (config.Watchdog is not null)
        {
            Log.Information($"Watchdog timeout:   {config.Watchdog.TimeoutMinutes} minutes");
            Log.Information($"Watchdog delay:     {config.Watchdog.DelaySeconds} seconds");
            Log.Information($"Watchdog minimum:   {config.Watchdog.MinimumSpeedBytes} bytes/second");
            Log.Information($"Watchdog remote:    {config.Watchdog.QueuedRemotely}");
        }

        return Task.FromResult(0);
    }

    public int Invoke(InvocationContext context)
    {
        throw new NotImplementedException();
    }
}