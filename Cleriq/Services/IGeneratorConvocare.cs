using Cleriq.Models;

namespace Cleriq.Services;

public record ContinutConvocare(string Subiect, string EmailHtml, string SmsText);

public interface IGeneratorConvocare
{
    ContinutConvocare Genereaza(Sedinta sedinta, Consilier consilier);
}