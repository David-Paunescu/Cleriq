using Cleriq.Models;

namespace Cleriq.Services;

public class GeneratorPromptTranscriere : IGeneratorPromptTranscriere
{
    public string Genereaza(Sedinta sedinta, IEnumerable<Consilier> consilieri)
    {
        var nume = consilieri
            .Select(c => c.NumeComplet.Trim())
            .Where(n => !string.IsNullOrEmpty(n))
            .Distinct()
            .ToList();

        if (nume.Count == 0)
            return $"Ședință a Consiliului Local al instituției {sedinta.Institutie.Denumire}.";

        var listaNume = string.Join(", ", nume);
        return $"Ședință a Consiliului Local al instituției {sedinta.Institutie.Denumire}. Consilieri prezenți: {listaNume}.";
    }
}