using Cleriq.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

// Alias necesar: „Document" există și în Cleriq.Models, și în QuestPDF.Fluent.
using PdfDocument = QuestPDF.Fluent.Document;

namespace Cleriq.Services;

// Watermark de fundal (text + mărime font). Text mai lung ⇒ font mai mic ca să încapă pe lățime.
public readonly record struct WatermarkPdf(string Text, float FontSize);

// Bază comună de randare PDF (A4, margini, header cu denumirea instituției, conținut Markdown, footer
// cu paginație, watermark opțional pe fundal). Extrasă la a 3-a folosire (PV + HCL + Dispoziție).
// Watermark-ul e singura divergență între acte → se calculează în fiecare generator și se pasează aici.
public static class GeneratorPdfAct
{
    public static byte[] Genereaza(string markdown, Institutie institutie, WatermarkPdf? watermark)
    {
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
                        .Text(watermark.Value.Text)
                        .FontSize(watermark.Value.FontSize).Bold()
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
