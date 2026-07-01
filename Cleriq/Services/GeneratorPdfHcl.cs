using Cleriq.Models;

namespace Cleriq.Services;

public class GeneratorPdfHcl : IGeneratorPdfHcl
{
    public byte[] Genereaza(Hcl hcl, Institutie institutie)
    {
        var markdown = string.IsNullOrWhiteSpace(hcl.Continut)
            ? "_(Hotărârea nu are conținut generat.)_"
            : hcl.Continut;

        // Watermark: INVALIDAT prioritar (un act invalidat poate fi și Semnat, dar invaliditatea
        // primează vizual; „INVALIDAT" e mai lung → font mai mic). Altfel „DRAFT" până la semnare.
        WatermarkPdf? watermark = null;
        if (hcl.DataInvalidare != null)
            watermark = new WatermarkPdf("INVALIDAT", 75f);
        else if (hcl.Status != StatusActRedactional.Semnat)
            watermark = new WatermarkPdf("DRAFT", 110f);

        return GeneratorPdfAct.Genereaza(markdown, institutie, watermark);
    }
}
