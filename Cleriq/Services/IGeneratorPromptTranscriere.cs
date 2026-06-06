using Cleriq.Models;

namespace Cleriq.Services;

public record ContinutTranscriere(string Prompt, string Hotwords);

public interface IGeneratorPromptTranscriere
{
    ContinutTranscriere Genereaza(Sedinta sedinta, IEnumerable<Consilier> consilieri);
}