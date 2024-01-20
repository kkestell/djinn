using System.CommandLine.Invocation;
using Djinn.Configuration;
using Djinn.Services;
using IF.Lastfm.Core.Api;

namespace Djinn.Commands.Handlers;

public class SuggestCommandHandler : ICommandHandler
{
    public async Task<int> InvokeAsync(InvocationContext context)
    {
        var limit = 30;
        
        var config = DjinnConfig.Load();

        var artistName = context.ParseResult.GetValueForOption(SuggestCommand.ArtistName);

        if (artistName is not null)
        {
            var musicBrainzService = new MusicBrainzService();
            var artist = await musicBrainzService.FindArtist(artistName);
            
            if (artist is null)
            {
                Log.Error("Unable to locate artist");
                return 1;
            }

            var client = new LastfmClient(config.LastFmApiKey, config.LastFmApiSecret);
            var response = await client.Artist.GetSimilarByMbidAsync(artist.Id.ToString());
            
            var similarArtists = response.Content.Where(lastArtist => lastArtist.Mbid is not null).ToList();

            if (similarArtists.Any())
            {
                Console.WriteLine("Similar artists:");
                foreach (var lastArtist in similarArtists.Take(limit))
                    Console.WriteLine($"  * {lastArtist.Name}");
            }
        }
        else
        {
            // TODO
            return 1;
        }
        
        return 0;
    }
    
    public int Invoke(InvocationContext context)
    {
        throw new NotImplementedException();
    }
}