using StackExchange.Redis;

namespace Cleriq.Services;

public class LacatDistribuitRedis : ILacatDistribuit
{
    private const string Prefix = "cleriq:lock:";

    // Eliberare sigură: șterge cheia DOAR dacă token-ul e al nostru.
    // Fără comparație, am putea șterge lacătul altei instanțe (dacă al nostru a expirat).
    private const string ScriptEliberare = @"
if redis.call('GET', KEYS[1]) == ARGV[1] then
    return redis.call('DEL', KEYS[1])
else
    return 0
end";

    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<LacatDistribuitRedis> _logger;

    public LacatDistribuitRedis(IConnectionMultiplexer redis, ILogger<LacatDistribuitRedis> logger)
    {
        _redis = redis;
        _logger = logger;
    }

    public async Task<IAsyncDisposable?> IncearcaBlocareAsync(string cheie, TimeSpan durata)
    {
        var cheieCompleta = Prefix + cheie;
        var token = Guid.NewGuid().ToString("N");

        try
        {
            var dobandit = await _redis.GetDatabase()
                .StringSetAsync(cheieCompleta, token, durata, When.NotExists);

            return dobandit ? new HandleLacat(this, cheieCompleta, token) : null;
        }
        catch (Exception ex)
        {
            // Redis indisponibil → tratăm ca ne-dobândit: mai bine amânăm trimiterea
            // (o preia o tură viitoare) decât să riscăm duplicate în multi-instance.
            _logger.LogWarning(ex,
                "Redis indisponibil la blocarea {Cheie}; tratez ca ne-dobândit.", cheieCompleta);
            return null;
        }
    }

    private async Task ElibereazaAsync(string cheieCompleta, string token)
    {
        try
        {
            await _redis.GetDatabase().ScriptEvaluateAsync(
                ScriptEliberare,
                new RedisKey[] { cheieCompleta },
                new RedisValue[] { token });
        }
        catch (Exception ex)
        {
            // Necritic: TTL-ul curăță automat lacătul rămas.
            _logger.LogWarning(ex, "Eșec eliberare lacăt {Cheie}; expiră prin TTL.", cheieCompleta);
        }
    }

    private sealed class HandleLacat : IAsyncDisposable
    {
        private readonly LacatDistribuitRedis _parinte;
        private readonly string _cheie;
        private readonly string _token;
        private bool _eliberat;

        public HandleLacat(LacatDistribuitRedis parinte, string cheie, string token)
        {
            _parinte = parinte;
            _cheie = cheie;
            _token = token;
        }

        public async ValueTask DisposeAsync()
        {
            if (_eliberat) return;
            _eliberat = true;
            await _parinte.ElibereazaAsync(_cheie, _token);
        }
    }
}