using FakeNewsDetection.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace FakeNewsDetection.Services
{
    public class PdfReportService
    {
        public byte[] GenerateReport(NewsArticle article)
        {
            if (article == null)
            {
                throw new ArgumentNullException(nameof(article));
            }

            QuestPDF.Settings.License =
                LicenseType.Community;

            var document =
                Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A4);
                        page.Margin(40);

                        page.DefaultTextStyle(
                            x => x.FontSize(10));

                        page.Header()
                            .Element(ComposeHeader);

                        page.Content()
                            .PaddingVertical(20)
                            .Element(content =>
                                ComposeContent(
                                    content,
                                    article));

                        page.Footer()
                            .AlignCenter()
                            .Text(text =>
                            {
                                text.Span(
                                    "AI Fake News Detection • ");

                                text.Span(
                                    $"Generated on {DateTime.Now:dd MMM yyyy, hh:mm tt}");
                            });
                    });
                });

            return document.GeneratePdf();
        }

        private static void ComposeHeader(
            IContainer container)
        {
            container.Column(column =>
            {
                column.Item()
                    .Text("AI Fake News Detection")
                    .FontSize(22)
                    .Bold();

                column.Item()
                    .PaddingTop(4)
                    .Text("News Credibility Analysis Report")
                    .FontSize(12);

                column.Item()
                    .PaddingTop(10)
                    .LineHorizontal(1);
            });
        }

        private static void ComposeContent(
            IContainer container,
            NewsArticle article)
        {
            container.Column(column =>
            {
                column.Spacing(15);

                column.Item()
                    .Text("Analysis Result")
                    .FontSize(16)
                    .Bold();

                column.Item()
                    .Border(1)
                    .Padding(15)
                    .Column(result =>
                    {
                        result.Spacing(8);

                        result.Item()
                            .Text(text =>
                            {
                                text.Span("Prediction: ")
                                    .Bold();

                                text.Span(
                                    article.Prediction
                                    ?? "N/A");
                            });

                        result.Item()
                            .Text(text =>
                            {
                                text.Span("Confidence: ")
                                    .Bold();

                                text.Span(
                                    article.Confidence.HasValue
                                        ? $"{article.Confidence.Value:F2}%"
                                        : "N/A");
                            });

                        result.Item()
                            .Text(text =>
                            {
                                text.Span("Analysis Date: ")
                                    .Bold();

                                text.Span(
                                    article.CreatedAt
                                        .ToLocalTime()
                                        .ToString(
                                            "dd MMM yyyy, hh:mm tt"));
                            });
                    });

                column.Item()
                    .Text("Headline")
                    .FontSize(14)
                    .Bold();

                column.Item()
                    .Text(article.Headline);

                if (!string.IsNullOrWhiteSpace(article.Url))
                {
                    column.Item()
                        .Text("Source URL")
                        .FontSize(14)
                        .Bold();

                    column.Item()
                        .Text(article.Url);
                }

                column.Item()
                    .Text("AI Explanation")
                    .FontSize(14)
                    .Bold();

                column.Item()
                    .Text(
                        article.Explanation
                        ?? "No explanation available.");

                column.Item()
                    .Text("Article Content")
                    .FontSize(14)
                    .Bold();

                column.Item()
                    .Text(
                        article.ArticleContent);

                column.Item()
                    .PaddingTop(10)
                    .Border(1)
                    .Padding(10)
                    .Text(
                        "Important: This report contains an " +
                        "AI-assisted assessment. The prediction " +
                        "should not be treated as absolute proof " +
                        "that an article is true or false. " +
                        "Important claims should always be " +
                        "verified using reliable sources.");
            });
        }
    }
}