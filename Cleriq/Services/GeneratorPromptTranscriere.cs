using Cleriq.Models;

namespace Cleriq.Services;

public class GeneratorPromptTranscriere : IGeneratorPromptTranscriere
{
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
            return new ContinutTranscriere(promptGol);
        }

        var listaPrompt = string.Join(", ", nume);
        var prompt = $"Ședință a Consiliului Local al instituției {sedinta.Institutie.Denumire}. Consilieri prezenți: {listaPrompt}.";

        return new ContinutTranscriere(prompt);
    }
}