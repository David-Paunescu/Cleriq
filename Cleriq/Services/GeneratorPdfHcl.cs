using Cleriq.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

using PdfDocument = QuestPDF.Fluent.Document;

namespace Cleriq.Services;

public class GeneratorPdfHcl : IGeneratorPdfHcl
{
    public byte[] Genereaza(Hcl hcl, Institutie institutie)
    {
        var markdown = string.IsNullOrWhiteSpace(hcl.Continut)
            ? "_(Hotărârea nu are conținut generat.)_"
            : hcl.Continut;

        // Watermark 3 stări. INVALIDAT prioritar (un act invalidat poate fi și Semnat,
        // dar invaliditatea primează vizual). „INVALIDAT" e mai lung → font mai mic ca să încapă.
        string? watermark = null;
        var watermarkFontSize = 110f;
        if (hcl.DataInvalidare != null)
        {
            watermark = "INVALIDAT";
            watermarkFontSize = 75f;
        }
        else if (hcl.Status != StatusActRedactional.Semnat)
        {
            watermark = "DRAFT";
        }

        var pdf = PdfDocument.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2.2f, Unit.Centimetre);
                page.DefaultTextStyle(s => s.FontSize(10.5f).LineHeight(1.35f));

                if (watermark != null)
                {
                    page.Background()
                        .AlignCenter()
                        .AlignMiddle()
                        .Text(watermark)
                        .FontSize(watermarkFontSize).Bold()
                        .FontColor(Colors.Red.Lighten3);
                }

                page.Header()
                    .PaddingBottom(6)
                    .BorderBottom(0.75f)
                    .BorderColor(Colors.Grey.Lighten2)
                    .Text(institutie.Denumire)
                    .FontSize(9).FontColor(Colors.Grey.Darken1);

                page.Content()
                    .PaddingTop(12)
                    .Column(col =>
                    {
                        col.Spacing(6);
                        RandareMarkdownPdf.RandeazaIn(col, markdown);
                    });

                page.Footer()
                    .AlignCenter()
                    .Text(t =>
                    {
                        t.DefaultTextStyle(s => s.FontSize(9).FontColor(Colors.Grey.Darken1));
                        t.Span("Pagina ");
                        t.CurrentPageNumber();
                        t.Span(" din ");
                        t.TotalPages();
                    });
            });
        });

        return pdf.GeneratePdf();
    }
}