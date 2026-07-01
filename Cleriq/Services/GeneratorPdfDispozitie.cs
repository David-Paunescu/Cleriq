using Cleriq.Models;

namespace Cleriq.Services;

// Doar watermark pe stare peste baza comună (GeneratorPdfAct) — semnăturile „Primar / Secretar
// general" stau deja în Markdown (GeneratorDispozitie, Pas 5), nu în PDF.
public class GeneratorPdfDispozitie : IGeneratorPdfDispozitie
{
    public byte[] Genereaza(Dispozitie dispozitie, Institutie institutie)
    {
        var markdown = string.IsNullOrWhiteSpace(dispozitie.Continut)
            ? "_(Dispoziția nu are conținut generat.)_"
            : dispozitie.Continut;

        // Watermark: INVALIDAT prioritar (invaliditatea primează vizual), altfel „DRAFT" până la semnare.
        WatermarkPdf? watermark = null;
        if (dispozitie.DataInvalidare != null)
            watermark = new WatermarkPdf("INVALIDAT", 75f);
        else if (dispozitie.Status != StatusActRedactional.Semnat)
            watermark = new WatermarkPdf("DRAFT", 110f);

        return GeneratorPdfAct.Genereaza(markdown, institutie, watermark);
    }
}
