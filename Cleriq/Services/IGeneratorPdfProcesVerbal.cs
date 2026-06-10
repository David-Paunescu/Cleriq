using Cleriq.Models;

namespace Cleriq.Services;

public interface IGeneratorPdfProcesVerbal
{
    // Randează PDF-ul din Markdown-ul stocat (pv.Continut) — inclusiv editările secretarului.
    // Status Draft → watermark „DRAFT"; Finalizat → document curat.
    byte[] Genereaza(ProcesVerbal pv, Institutie institutie);
}