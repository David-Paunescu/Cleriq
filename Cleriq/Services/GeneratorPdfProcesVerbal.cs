using Cleriq.Models;

namespace Cleriq.Services;

public class GeneratorPdfProcesVerbal : IGeneratorPdfProcesVerbal
{
    public byte[] Genereaza(ProcesVerbal pv, Institutie institutie)
    {
        var markdown = string.IsNullOrWhiteSpace(pv.Continut)
            ? "_(Procesul verbal nu are conținut.)_"
            : pv.Continut;

        // Draft → watermark „DRAFT"; Finalizat → document curat.
        WatermarkPdf? watermark = pv.Status == StatusProcesVerbal.Draft
            ? new WatermarkPdf("DRAFT", 110f)
            : null;

        return GeneratorPdfAct.Genereaza(markdown, institutie, watermark);
    }
}
