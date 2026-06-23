using Cleriq.Models;

namespace Cleriq.Services;

public interface IGeneratorPdfHcl
{
    byte[] Genereaza(Hcl hcl, Institutie institutie);
}