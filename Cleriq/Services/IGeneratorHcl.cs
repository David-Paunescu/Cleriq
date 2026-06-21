using Cleriq.Models;

namespace Cleriq.Services;

public interface IGeneratorHcl
{
    /// <summary>
    /// Generează conținutul Markdown al unui HCL din datele structurate.
    /// Necesită navigările populate: PunctOrdineZi.Sedinta.Institutie, Semnatari (cu Persoana/Consilier),
    /// Documente, RelatiiSursa (cu HclTinta). Controllerul e responsabil pentru Include-uri.
    /// </summary>
    string GenereazaContinut(Hcl hcl);
}