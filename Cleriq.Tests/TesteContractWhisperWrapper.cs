using System.Net;
using System.Text;
using Cleriq.Services;
using Cleriq.Tests.Infrastructura;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cleriq.Tests;

public class TesteContractWhisperWrapper
{
    private const string RaspunsJsonValid =
        """{"text": [{"start": 0.0, "end": 79.5, "text": "Buna ziua.", "speaker": "SPEAKER_00"}]}""";

    private static (TranscriereWhisperWrapper Wrapper, HandlerHttpFals Handler) CreeazaWrapper(
        HttpStatusCode status = HttpStatusCode.OK, string corp = RaspunsJsonValid)
    {
        var handler = new HandlerHttpFals(status, corp);
        var http = new HttpClient(handler) { BaseAddress = new Uri("http://whisper-test") };
        var wrapper = new TranscriereWhisperWrapper(
            http, NullLogger<TranscriereWhisperWrapper>.Instance);
        return (wrapper, handler);
    }

    private static async Task<RezultatTranscriere> TrimiteAsync(
        TranscriereWhisperWrapper wrapper, string prompt = "Prompt test")
    {
        using var audio = new MemoryStream(Encoding.UTF8.GetBytes("audio-fals"));
        return await wrapper.TrimiteAsync(audio, "sedinta.mp3", new ContinutTranscriere(prompt));
    }

    // Contractul wrapper-ului learnedmachine (bug s25): task/language/initial_prompt
    // sunt Form(...) în FastAPI — prin query ar fi ignorate silențios.
    [Fact]
    public async Task TrimiteAsync_ParametriiPleacaCaFormData_NuCaQuery()
    {
        var (wrapper, handler) = CreeazaWrapper();
        const string prompt = "Ședință Consiliu. Consilieri prezenți: Ion Popescu.";

        await TrimiteAsync(wrapper, prompt);

        var query = handler.Url!.Query;
        Assert.Contains("enable_diarization=true", query);
        Assert.Contains("output=json", query);
        Assert.DoesNotContain("task=", query);
        Assert.DoesNotContain("language=", query);
        Assert.DoesNotContain("initial_prompt", query);

        Assert.Equal(3, handler.CampuriForm.Count);
        Assert.Equal("transcribe", handler.CampuriForm["task"]);
        Assert.Equal("ro", handler.CampuriForm["language"]);
        Assert.Equal(prompt, handler.CampuriForm["initial_prompt"]);
        Assert.Equal("sedinta.mp3", handler.NumeFisierAudio);
    }

    [Fact]
    public async Task TrimiteAsync_RaspunsOk_SuccesCuDurataExtrasa()
    {
        var (wrapper, _) = CreeazaWrapper();

        var rezultat = await TrimiteAsync(wrapper);

        Assert.True(rezultat.Succes);
        Assert.Equal(RaspunsJsonValid, rezultat.ContinutJson);
        Assert.Equal(80, rezultat.DurataAudioSecunde);
    }

    [Fact]
    public async Task TrimiteAsync_Raspuns500_EsuatRetriable()
    {
        var (wrapper, _) = CreeazaWrapper(HttpStatusCode.InternalServerError, "eroare interna");

        var rezultat = await TrimiteAsync(wrapper);

        Assert.False(rezultat.Succes);
        Assert.True(rezultat.EsteRetriable);
    }

    [Fact]
    public async Task TrimiteAsync_Raspuns400_EsuatNonRetriable()
    {
        var (wrapper, _) = CreeazaWrapper(HttpStatusCode.BadRequest, "parametri invalizi");

        var rezultat = await TrimiteAsync(wrapper);

        Assert.False(rezultat.Succes);
        Assert.False(rezultat.EsteRetriable);
    }
}