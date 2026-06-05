using Cleriq.Models;

namespace Cleriq.Services;

public interface IGeneratorPromptTranscriere
{
    string Genereaza(Sedinta sedinta, IEnumerable<Consilier> consilieri);
}