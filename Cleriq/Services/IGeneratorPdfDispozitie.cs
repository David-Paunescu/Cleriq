using Cleriq.Models;

namespace Cleriq.Services;

public interface IGeneratorPdfDispozitie
{
    byte[] Genereaza(Dispozitie dispozitie, Institutie institutie);
}
