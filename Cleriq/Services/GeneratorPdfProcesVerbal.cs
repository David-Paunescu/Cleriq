using System.Text;
using Cleriq.Models;
using Markdig;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

// Alias necesar: „Document" există și în Cleriq.Models (entitatea de fișiere atașate)
// și în QuestPDF.Fluent — fără alias, referința ar fi ambiguă.
using PdfDocument = QuestPDF.Fluent.Document;

namespace Cleriq.Services;

public class GeneratorPdfProcesVerbal : IGeneratorPdfProcesVerbal
{
    // Pipeline CommonMark standard — acoperă exact subsetul emis de generatorul de PV
    // (heading-uri, bold, italic, liste, link-uri). Imutabil după Build → thread-safe.
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder().Build();

    public byte[] Genereaza(ProcesVerbal pv, Institutie institutie)
    {
        var markdown = string.IsNullOrWhiteSpace(pv.Continut)
            ? "_(Procesul verbal nu are conținut.)_"
            : pv.Continut;

        var documentMd = Markdown.Parse(markdown, Pipeline);
        var esteDraft = pv.Status == StatusProcesVerbal.Draft;

        var pdf = PdfDocument.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2.2f, Unit.Centimetre);
                page.DefaultTextStyle(s => s.FontSize(10.5f).LineHeight(1.35f));

                // Watermark mare, pal, în spatele textului — doar pe Draft.
                if (esteDraft)
                {
                    page.Background()
                        .AlignCenter()
                        .AlignMiddle()
                        .Text("DRAFT")
                        .FontSize(110).Bold()
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
                        foreach (var bloc in documentMd)
                            RandeazaBloc(col, bloc);
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

    // ============ Blocuri (nivel document) ============
    // Cazurile specifice ÎNAINTEA celor generice (ContainerBlock/LeafBlock) —
    // ordinea contează la pattern matching.

    private static void RandeazaBloc(ColumnDescriptor col, Block bloc)
    {
        switch (bloc)
        {
            case HeadingBlock heading:
                RandeazaHeading(col, heading);
                break;

            case ParagraphBlock paragraf:
                col.Item().Text(text => RandeazaInlines(text, paragraf.Inline));
                break;

            case ListBlock lista:
                RandeazaLista(col, lista);
                break;

            case QuoteBlock citat:
                col.Item()
                   .BorderLeft(2).BorderColor(Colors.Grey.Lighten1)
                   .PaddingLeft(10)
                   .Column(sub =>
                   {
                       sub.Spacing(6);
                       foreach (var copil in citat)
                           RandeazaBloc(sub, copil);
                   });
                break;

            case ThematicBreakBlock:
                col.Item().PaddingVertical(4)
                   .LineHorizontal(0.75f).LineColor(Colors.Grey.Lighten2);
                break;

            // Degradare grațioasă pentru orice alt bloc: containerele își randează
            // copiii, frunzele își randează textul. Niciodată crash.
            case ContainerBlock containerNecunoscut:
                foreach (var copil in containerNecunoscut)
                    RandeazaBloc(col, copil);
                break;

            case LeafBlock frunza:
                if (frunza.Inline is not null)
                    col.Item().Text(text => RandeazaInlines(text, frunza.Inline));
                else if (frunza.Lines.Count > 0)
                    col.Item().Text(frunza.Lines.ToString());
                break;
        }
    }

    private static void RandeazaHeading(ColumnDescriptor col, HeadingBlock heading)
    {
        var (fontSize, paddingTop) = heading.Level switch
        {
            1 => (18f, 0f),
            2 => (14.5f, 10f),
            3 => (12f, 6f),
            _ => (11f, 4f)
        };

        col.Item()
           .PaddingTop(paddingTop)
           .Text(text =>
           {
               text.DefaultTextStyle(s => s.FontSize(fontSize).Bold());
               RandeazaInlines(text, heading.Inline);
           });
    }

    private static void RandeazaLista(ColumnDescriptor col, ListBlock lista)
    {
        var contor = 1;
        if (lista.IsOrdered && int.TryParse(lista.OrderedStart, out var start))
            contor = start;

        col.Item().Column(colLista =>
        {
            colLista.Spacing(2);

            foreach (var item in lista.OfType<ListItemBlock>())
            {
                var marcaj = lista.IsOrdered ? $"{contor}." : "•";
                contor++;

                colLista.Item().Row(row =>
                {
                    row.ConstantItem(16).Text(marcaj);
                    row.RelativeItem().Column(colItem =>
                    {
                        colItem.Spacing(2);
                        // Liste imbricate trec tot prin RandeazaBloc → indentare
                        // naturală prin structura de coloane.
                        foreach (var copil in item)
                            RandeazaBloc(colItem, copil);
                    });
                });
            }
        });
    }

    // ============ Inlines (text în interiorul blocurilor) ============

    private static void RandeazaInlines(TextDescriptor text, ContainerInline? container)
    {
        if (container is null) return;
        RandeazaInlineCopii(text, container, bold: false, italic: false);
    }

    private static void RandeazaInlineCopii(
        TextDescriptor text, ContainerInline container, bool bold, bool italic)
    {
        foreach (var inline in container)
        {
            switch (inline)
            {
                case LiteralInline literal:
                    AplicaStil(text.Span(literal.Content.ToString()), bold, italic);
                    break;

                case EmphasisInline emfaza:
                    // ** = bold (DelimiterCount 2), * sau _ = italic (1).
                    // *** se parsează ca emfaze imbricate — recursivitatea acoperă.
                    RandeazaInlineCopii(text, emfaza,
                        bold || emfaza.DelimiterCount >= 2,
                        italic || emfaza.DelimiterCount == 1);
                    break;

                case LinkInline link when !link.IsImage:
                    var eticheta = ExtrageTextSimplu(link);
                    var url = link.Url ?? string.Empty;
                    var spanLink = string.IsNullOrEmpty(url)
                        ? text.Span(eticheta)
                        : text.Hyperlink(eticheta, url);
                    AplicaStil(spanLink, bold, italic);
                    spanLink.FontColor(Colors.Blue.Darken2).Underline();
                    break;

                case LinkInline imagine:
                    // Imaginile nu au sens în PV — degradăm la alt-text.
                    AplicaStil(text.Span(ExtrageTextSimplu(imagine)), bold, italic);
                    break;

                case LineBreakInline:
                    // DECIZIE: break-urile soft din sursă = linii separate în PDF.
                    // Generatorul de PV scrie câmpurile metadate („**Instituția:** X")
                    // pe linii consecutive în același paragraf — intenția vizuală e
                    // o linie per câmp, nu text curgător.
                    text.Span("\n");
                    break;

                case CodeInline cod:
                    AplicaStil(text.Span(cod.Content), bold, italic);
                    break;

                case HtmlEntityInline entitate:
                    AplicaStil(text.Span(entitate.Transcoded.ToString()), bold, italic);
                    break;

                case AutolinkInline autolink:
                    text.Hyperlink(autolink.Url, autolink.Url)
                        .FontColor(Colors.Blue.Darken2).Underline();
                    break;

                case ContainerInline altContainer:
                    RandeazaInlineCopii(text, altContainer, bold, italic);
                    break;

                    // Alte frunze (ex: HtmlInline cu tag-uri brute) — ignorate silențios.
            }
        }
    }

    private static void AplicaStil(TextSpanDescriptor span, bool bold, bool italic)
    {
        if (bold) span.Bold();
        if (italic) span.Italic();
    }

    // Textul simplu al unui container inline (pentru etichete de link).
    private static string ExtrageTextSimplu(ContainerInline container)
    {
        var sb = new StringBuilder();
        foreach (var inline in container)
        {
            switch (inline)
            {
                case LiteralInline lit: sb.Append(lit.Content.ToString()); break;
                case CodeInline cod: sb.Append(cod.Content); break;
                case ContainerInline c: sb.Append(ExtrageTextSimplu(c)); break;
            }
        }
        return sb.ToString();
    }
}