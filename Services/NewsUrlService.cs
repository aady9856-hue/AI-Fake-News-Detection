using System.Net;
using HtmlAgilityPack;

namespace FakeNewsDetection.Services
{
    public class NewsUrlService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<NewsUrlService> _logger;

        public NewsUrlService(
            HttpClient httpClient,
            ILogger<NewsUrlService> logger)
        {
            _httpClient = httpClient
                ?? throw new ArgumentNullException(nameof(httpClient));

            _logger = logger
                ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<(string Headline, string ArticleContent)> ExtractNewsAsync(
            string url)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                throw new ArgumentException(
                    "URL cannot be empty.",
                    nameof(url));
            }

            string normalizedUrl = url.Trim();

            normalizedUrl = normalizedUrl.Trim('`');

            if (normalizedUrl.StartsWith("```", StringComparison.Ordinal))
            {
                normalizedUrl =
                    normalizedUrl
                        .Replace("```csharp", string.Empty, StringComparison.OrdinalIgnoreCase)
                        .Replace("```", string.Empty, StringComparison.Ordinal)
                        .Trim();
            }

            if (!Uri.TryCreate(
                    normalizedUrl,
                    UriKind.Absolute,
                    out Uri? uri) ||
                (uri.Scheme != Uri.UriSchemeHttp &&
                 uri.Scheme != Uri.UriSchemeHttps))
            {
                throw new ArgumentException(
                    "Please provide a valid HTTP or HTTPS URL.",
                    nameof(url));
            }

            try
            {
                _logger.LogInformation(
                    "Attempting to extract news article from {Url}",
                    uri);

                using HttpResponseMessage response =
                    await _httpClient.GetAsync(
                        uri,
                        HttpCompletionOption.ResponseHeadersRead);

                if (response.StatusCode == HttpStatusCode.Forbidden)
                {
                    _logger.LogWarning(
                        "Website blocked the request. URL: {Url}",
                        uri);

                    throw new InvalidOperationException(
                        "This website is blocking automated requests. " +
                        "Please try another news website or enter the article manually.");
                }

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    _logger.LogWarning(
                        "Website requires authorization. URL: {Url}",
                        uri);

                    throw new InvalidOperationException(
                        "This website requires authorization and cannot be accessed automatically.");
                }

                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    _logger.LogWarning(
                        "Article page was not found. URL: {Url}",
                        uri);

                    throw new InvalidOperationException(
                        "The requested article page could not be found. " +
                        "Please check the URL and try again.");
                }

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning(
                        "Website returned HTTP {StatusCode}. URL: {Url}",
                        (int)response.StatusCode,
                        uri);

                    throw new InvalidOperationException(
                        $"The website returned an HTTP error ({(int)response.StatusCode}). " +
                        "Please try another URL.");
                }

                string html =
                    await response.Content.ReadAsStringAsync();

                if (string.IsNullOrWhiteSpace(html))
                {
                    _logger.LogWarning(
                        "Website returned empty HTML. URL: {Url}",
                        uri);

                    throw new InvalidOperationException(
                        "The webpage returned empty content.");
                }

                var document = new HtmlDocument();

                try
                {
                    document.LoadHtml(html);
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "HTML parsing failed for URL: {Url}",
                        uri);

                    throw new InvalidOperationException(
                        "The webpage could not be processed.",
                        ex);
                }

                /*
                 * ============================================================
                 * PIB
                 * ============================================================
                 */

                if (uri.Host.Contains(
                        "pib.gov.in",
                        StringComparison.OrdinalIgnoreCase))
                {
                    return ExtractPibArticle(document);
                }

                /*
                 * ============================================================
                 * GENERIC WEBSITE
                 * ============================================================
                 */

                return ExtractGenericArticle(document);
            }
            catch (ArgumentException)
            {
                throw;
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogError(
                    ex,
                    "News URL request timed out. URL: {Url}",
                    uri);

                throw new InvalidOperationException(
                    "The website took too long to respond. " +
                    "Please try again or use another news website.",
                    ex);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(
                    ex,
                    "Could not connect to news website. URL: {Url}",
                    uri);

                throw new InvalidOperationException(
                    "Could not connect to the news website. " +
                    "Please check your internet connection or try another URL.",
                    ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Unexpected error while extracting news article. URL: {Url}",
                    uri);

                throw new InvalidOperationException(
                    "An unexpected error occurred while reading the news article. " +
                    "Please try again or enter the article manually.",
                    ex);
            }
        }

        private static (
            string Headline,
            string ArticleContent) ExtractPibArticle(
                HtmlDocument document)
        {
            /*
             * PIB HEADLINE
             *
             * Actual PIB HTML:
             *
             * <h2 id="Titleh2">
             *     Article headline
             * </h2>
             */

            string headline = string.Empty;

            var titleNode =
                document.DocumentNode.SelectSingleNode(
                    "//h2[@id='Titleh2']");

            if (titleNode != null)
            {
                headline =
                    CleanText(titleNode.InnerText);
            }

            /*
             * Fallback to Open Graph title.
             */

            if (string.IsNullOrWhiteSpace(headline))
            {
                var ogTitle =
                    document.DocumentNode.SelectSingleNode(
                        "//meta[@property='og:title']");

                if (ogTitle != null)
                {
                    headline =
                        CleanText(
                            ogTitle.GetAttributeValue(
                                "content",
                                ""));
                }
            }

            /*
             * ============================================================
             * PIB ARTICLE CONTENT
             * ============================================================
             */

            string articleContent = string.Empty;

            var pdfDiv =
                document.DocumentNode.SelectSingleNode(
                    "//div[@id='PdfDiv']");

            if (pdfDiv != null)
            {
                var paragraphs =
                    pdfDiv.SelectNodes(".//p");

                if (paragraphs != null)
                {
                    articleContent =
                        string.Join(
                            Environment.NewLine +
                            Environment.NewLine,
                            paragraphs
                                .Select(p => CleanText(p.InnerText))
                                .Where(text =>
                                    !string.IsNullOrWhiteSpace(text))
                                .Where(text =>
                                    text.Length > 20));
                }
            }

            /*
             * Fallback to main PIB content area.
             */

            if (string.IsNullOrWhiteSpace(articleContent))
            {
                var contentContainer =
                    document.DocumentNode.SelectSingleNode(
                        "//div[contains(@style,'text-align: left') and " +
                        "contains(@style,'font-size: 16px')]");

                if (contentContainer != null)
                {
                    var paragraphs =
                        contentContainer.SelectNodes(".//p");

                    if (paragraphs != null)
                    {
                        articleContent =
                            string.Join(
                                Environment.NewLine +
                                Environment.NewLine,
                                paragraphs
                                    .Select(p => CleanText(p.InnerText))
                                    .Where(text =>
                                        !string.IsNullOrWhiteSpace(text))
                                    .Where(text =>
                                        text.Length > 20));
                    }
                }
            }

            /*
             * Final fallback: all meaningful paragraphs.
             */

            if (string.IsNullOrWhiteSpace(articleContent))
            {
                var paragraphs =
                    document.DocumentNode.SelectNodes("//p");

                if (paragraphs != null)
                {
                    articleContent =
                        string.Join(
                            Environment.NewLine +
                            Environment.NewLine,
                            paragraphs
                                .Select(p => CleanText(p.InnerText))
                                .Where(text =>
                                    !string.IsNullOrWhiteSpace(text))
                                .Where(text =>
                                    text.Length > 30));
                }
            }

            if (string.IsNullOrWhiteSpace(headline))
            {
                headline =
                    "Headline could not be extracted.";
            }

            if (string.IsNullOrWhiteSpace(articleContent))
            {
                throw new InvalidOperationException(
                    "PIB page was opened successfully, but the press release content " +
                    "could not be extracted.");
            }

            if (articleContent.Length > 30000)
            {
                articleContent =
                    articleContent.Substring(0, 30000);
            }

            return (
                headline,
                articleContent
            );
        }

        private static (
            string Headline,
            string ArticleContent) ExtractGenericArticle(
                HtmlDocument document)
        {
            string headline = string.Empty;

            /*
             * Open Graph title.
             */

            var ogTitle =
                document.DocumentNode.SelectSingleNode(
                    "//meta[@property='og:title']");

            if (ogTitle != null)
            {
                headline =
                    CleanText(
                        ogTitle.GetAttributeValue(
                            "content",
                            ""));
            }

            /*
             * Twitter title.
             */

            if (string.IsNullOrWhiteSpace(headline))
            {
                var twitterTitle =
                    document.DocumentNode.SelectSingleNode(
                        "//meta[@name='twitter:title']");

                if (twitterTitle != null)
                {
                    headline =
                        CleanText(
                            twitterTitle.GetAttributeValue(
                                "content",
                                ""));
                }
            }

            /*
             * H1.
             */

            if (string.IsNullOrWhiteSpace(headline))
            {
                var h1 =
                    document.DocumentNode.SelectSingleNode(
                        "//h1");

                if (h1 != null)
                {
                    headline =
                        CleanText(h1.InnerText);
                }
            }

            /*
             * Page title.
             */

            if (string.IsNullOrWhiteSpace(headline))
            {
                var title =
                    document.DocumentNode.SelectSingleNode(
                        "//title");

                if (title != null)
                {
                    headline =
                        CleanText(title.InnerText);
                }
            }

            RemoveUnwantedNodes(document);

            string articleContent = string.Empty;

            /*
             * <article>
             */

            var articleNode =
                document.DocumentNode.SelectSingleNode(
                    "//article");

            if (articleNode != null)
            {
                articleContent =
                    ExtractParagraphs(articleNode);
            }

            /*
             * Common article containers.
             */

            if (string.IsNullOrWhiteSpace(articleContent))
            {
                string[] selectors =
                {
                    "//main",
                    "//div[contains(@class,'article-body')]",
                    "//div[contains(@class,'article-content')]",
                    "//div[contains(@class,'story-content')]",
                    "//div[contains(@class,'story-body')]",
                    "//div[contains(@class,'post-content')]",
                    "//div[contains(@class,'entry-content')]"
                };

                foreach (string selector in selectors)
                {
                    var container =
                        document.DocumentNode
                            .SelectSingleNode(selector);

                    if (container == null)
                    {
                        continue;
                    }

                    string extracted =
                        ExtractParagraphs(container);

                    if (!string.IsNullOrWhiteSpace(extracted))
                    {
                        articleContent = extracted;
                        break;
                    }
                }
            }

            /*
             * Generic fallback.
             */

            if (string.IsNullOrWhiteSpace(articleContent))
            {
                var paragraphs =
                    document.DocumentNode.SelectNodes("//p");

                if (paragraphs != null)
                {
                    articleContent =
                        string.Join(
                            Environment.NewLine +
                            Environment.NewLine,
                            paragraphs
                                .Select(p => CleanText(p.InnerText))
                                .Where(text =>
                                    !string.IsNullOrWhiteSpace(text))
                                .Where(text =>
                                    text.Length > 30));
                }
            }

            if (string.IsNullOrWhiteSpace(headline))
            {
                headline =
                    "Headline could not be extracted.";
            }

            if (string.IsNullOrWhiteSpace(articleContent))
            {
                throw new InvalidOperationException(
                    "Could not extract article content from this URL.");
            }

            if (articleContent.Length > 30000)
            {
                articleContent =
                    articleContent.Substring(0, 30000);
            }

            return (
                headline,
                articleContent
            );
        }

        private static string ExtractParagraphs(
            HtmlNode container)
        {
            var paragraphs =
                container.SelectNodes(".//p");

            if (paragraphs == null)
            {
                return string.Empty;
            }

            return string.Join(
                Environment.NewLine +
                Environment.NewLine,
                paragraphs
                    .Select(p => CleanText(p.InnerText))
                    .Where(text =>
                        !string.IsNullOrWhiteSpace(text))
                    .Where(text =>
                        text.Length > 20));
        }

        private static void RemoveUnwantedNodes(
            HtmlDocument document)
        {
            string[] unwantedTags =
            {
                "script",
                "style",
                "nav",
                "header",
                "footer",
                "aside",
                "form",
                "noscript",
                "iframe",
                "svg"
            };

            foreach (string tag in unwantedTags)
            {
                var nodes =
                    document.DocumentNode.SelectNodes(
                        $"//{tag}");

                if (nodes == null)
                {
                    continue;
                }

                foreach (var node in nodes)
                {
                    node.Remove();
                }
            }
        }

        private static string CleanText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return string.Empty;
            }

            string cleaned =
                HtmlEntity
                    .DeEntitize(text)
                    .Trim();

            cleaned =
                string.Join(
                    " ",
                    cleaned.Split(
                        new[]
                        {
                            ' ',
                            '\r',
                            '\n',
                            '\t'
                        },
                        StringSplitOptions.RemoveEmptyEntries));

            return cleaned;
        }
    }
}
