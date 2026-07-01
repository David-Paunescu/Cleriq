using Cleriq.Models;

namespace Cleriq.Services;

public interface IGeneratorDispozitie
{
    string GenereazaContinut(Dispozitie dispozitie);

    // Conținutul dispoziției de convocare (Pas 12) — temei art. 133/134 + proiectul ordinii de zi.
    string GenereazaConvocare(Dispozitie dispozitie, Sedinta sedinta);
}
