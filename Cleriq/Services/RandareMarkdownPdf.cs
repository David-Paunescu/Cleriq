using System.Text;
using Markdig;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Cleriq.Services;

// Renderer Markdown→PDF partajat între generatoarele de acte (PV, HCL, viitor dispoziții).
// Subset CommonMark emis de generatoare: heading-uri, bold, italic, liste, link-uri,
// citate, thematic breaks. Page chrome (antet, watermark, footer) rămâne la fiecare
// generator; aici doar corpul documentului.
public static class RandareMarkdownPdf
{
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder().Build();

    // Punct unic de intrare. Caller-ul setează Spacing pe coloană înainte de apel.
    public static void RandeazaIn(ColumnDescriptor col, string markdown)
    {
        var documentMd = Markdown.Parse(markdown, Pipeline);
        foreach (var bloc in documentMd)
            RandeazaBloc(col, bloc);
    }

    // Cazurile specifice ÎNAINTEA celor generice (ContainerBlock/LeafBlock).
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
                        foreach (var copil in item)
                            RandeazaBloc(colItem, copil);
                    });
                });
            }
        });
    }

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
                    // ** = bold (DelimiterCount 2), * sau _ = italic (1). *** = imbricate.
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
                    AplicaStil(text.Span(ExtrageTextSimplu(imagine)), bold, italic);
                    break;

                case LineBreakInline:
                    // Break soft = linie separată în PDF (câmpurile metadate pe linii
                    // consecutive = o linie per câmp).
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
            }
        }
    }

    private static void AplicaStil(TextSpanDescriptor span, bool bold, bool italic)
    {
        if (bold) span.Bold();
        if (italic) span.Italic();
    }

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