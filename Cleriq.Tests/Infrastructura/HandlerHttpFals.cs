using System.Net;
using System.Text;

namespace Cleriq.Tests.Infrastructura;

// Capturează request-ul ÎN SendAsync — wrapper-ul face dispose pe content imediat după apel.
public class HandlerHttpFals : HttpMessageHandler
{
    private readonly HttpResponseMessage _raspuns;

    public Uri? Url { get; private set; }
    public Dictionary<string, string> CampuriForm { get; } = new();
    public string? NumeFisierAudio { get; private set; }

    public HandlerHttpFals(HttpStatusCode status, string corp)
    {
        _raspuns = new HttpResponseMessage(status)
        {
            Content = new StringContent(corp, Encoding.UTF8, "application/json")
        };
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken ct)
    {
        Url = request.RequestUri;

        if (request.Content is MultipartFormDataContent multipart)
        {
            foreach (var parte in multipart)
            {
                var nume = parte.Headers.ContentDisposition?.Name?.Trim('"') ?? "";
                if (parte is StringContent)
                    CampuriForm[nume] = await parte.ReadAsStringAsync(ct);
                else
                    NumeFisierAudio = parte.Headers.ContentDisposition?.FileName?.Trim('"');
            }
        }

        return _raspuns;
    }
}