using Cleriq.Models;

namespace Cleriq.Services;

public class GeneratorPromptTranscriere : IGeneratorPromptTranscriere
{
    private readonly bool _hotwordsEnabled;

    public GeneratorPromptTranscriere(IConfiguration config)
    {
        _hotwordsEnabled = config.GetValue<bool>("Whisper:HotwordsEnabled", true);
    }

    public ContinutTranscriere Genereaza(Sedinta sedinta, IEnumerable<Consilier> consilieri)
    {
        var nume = consilieri
            .Select(c => c.NumeComplet.Trim())
            .Where(n => !string.IsNullOrEmpty(n))
            .Distinct()
            .ToList();

        if (nume.Count == 0)
        {
            var promptGol = $"Ședință a Consiliului Local al instituției {sedinta.Institutie.Denumire}.";
            return new ContinutTranscriere(promptGol, "");
        }

        var listaPrompt = string.Join(", ", nume);
        var prompt = $"Ședință a Consiliului Local al instituției {sedinta.Institutie.Denumire}. Consilieri prezenți: {listaPrompt}.";

        var hotwords = _hotwordsEnabled ? string.Join(",", nume) : "";

        return new ContinutTranscriere(prompt, hotwords);
    }
}