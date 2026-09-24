using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace FakeNewsDetection.Services
{
    public class NewsSearchService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<NewsSearchService> _logger;

        public NewsSearchService(
            HttpClient httpClient,
            IConfiguration configuration,
            ILogger<NewsSearchService> logger)
        {
            _httpClient = httpClient
                ?? throw new ArgumentNullException(nameof(httpClient));

            _configuration = configuration
                ?? throw new ArgumentNullException(nameof(configuration));

            _logger = logger
                ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<List<NewsSearchResult>> SearchNewsAsync(
            string headline)
        {
            var results = new List<NewsSearchResult>();

            if (string.IsNullOrWhiteSpace(headline))
            {
                _logger.LogWarning(
                    "GNews search requested with an empty headline.");

                return results;
            }

            string? apiKey =
                _configuration["GNews:ApiKey"];

            string? baseUrl =
                _configuration["GNews:BaseUrl"];

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                _logger.LogError(
                    "GNews API key is not configured.");

                throw new InvalidOperationException(
                    "GNews API key is not configured.");
            }

            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                _logger.LogError(
                    "GNews base URL is not configured.");

                throw new InvalidOperationException(
                    "GNews base URL is not configured.");
            }

            if (!Uri.TryCreate(
                    baseUrl,
                    UriKind.Absolute,
                    out Uri? baseUri) ||
                (baseUri.Scheme != Uri.UriSchemeHttp &&
                 baseUri.Scheme != Uri.UriSchemeHttps))
            {
                _logger.LogError(
                    "Invalid GNews base URL configured: {BaseUrl}",
                    baseUrl);

                throw new InvalidOperationException(
                    "GNews base URL is invalid.");
            }

            string encodedHeadline =
                Uri.EscapeDataString(headline.Trim());

            string requestUrl =
                $"{baseUrl}" +
                $"?q={encodedHeadline}" +
                $"&lang=en" +
                $"&country=in" +
                $"&max=10" +
                $"&apikey={Uri.EscapeDataString(apiKey)}";

            try
            {
                _logger.LogInformation(
                    "Searching GNews for headline: {Headline}",
                    headline);

                using HttpResponseMessage response =
                    await _httpClient.GetAsync(requestUrl);

                string json =
                    await response.Content.ReadAsStringAsync();

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    _logger.LogError(
                        "GNews API rejected the API key.");

                    throw new InvalidOperationException(
                        "GNews API authentication failed. " +
                        "Please check the configured API key.");
                }

                if (response.StatusCode == HttpStatusCode.Forbidden)
                {
                    _logger.LogError(
                        "GNews API request was forbidden.");

                    throw new InvalidOperationException(
                        "GNews API access was denied. " +
                        "Please check your API plan and API key.");
                }

                if (response.StatusCode == HttpStatusCode.TooManyRequests)
                {
                    _logger.LogWarning(
                        "GNews API rate limit reached.");

                    throw new InvalidOperationException(
                        "GNews API rate limit has been reached. " +
                        "Please try again later.");
                }

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError(
                        "GNews API request failed. Status: {StatusCode}, Response: {Response}",
                        (int)response.StatusCode,
                        json);

                    throw new HttpRequestException(
                        $"GNews API request failed with HTTP {(int)response.StatusCode}.");
                }

                if (string.IsNullOrWhiteSpace(json))
                {
                    _logger.LogWarning(
                        "GNews API returned an empty response.");

                    return results;
                }

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                GNewsResponse? data;

                try
                {
                    data =
                        JsonSerializer.Deserialize<GNewsResponse>(
                            json,
                            options);
                }
                catch (JsonException ex)
                {
                    _logger.LogError(
                        ex,
                        "Could not parse GNews API response.");

                    throw new InvalidOperationException(
                        "GNews returned an invalid response.",
                        ex);
                }

                if (data?.Articles == null)
                {
                    _logger.LogInformation(
                        "GNews returned no articles.");

                    return results;
                }

                foreach (GNewsArticle? article in data.Articles)
                {
                    if (article == null)
                    {
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(article.Title) &&
                        string.IsNullOrWhiteSpace(article.Url))
                    {
                        continue;
                    }

                    results.Add(
                        new NewsSearchResult
                        {
                            Title =
                                article.Title?.Trim()
                                ?? string.Empty,

                            Description =
                                article.Description?.Trim()
                                ?? string.Empty,

                            Url =
                                article.Url?.Trim()
                                ?? string.Empty,

                            SourceName =
                                string.IsNullOrWhiteSpace(
                                    article.Source?.Name)
                                    ? "Unknown Source"
                                    : article.Source.Name.Trim(),

                            PublishedAt =
                                article.PublishedAt?.Trim()
                                ?? string.Empty
                        });
                }

                _logger.LogInformation(
                    "GNews search completed. Results found: {Count}",
                    results.Count);

                return results;
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogError(
                    ex,
                    "GNews API request timed out.");

                throw new InvalidOperationException(
                    "The GNews search request timed out. " +
                    "Please try again later.",
                    ex);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(
                    ex,
                    "Could not connect to GNews API.");

                throw new InvalidOperationException(
                    "Could not connect to the GNews service. " +
                    "Please check your internet connection and try again.",
                    ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Unexpected error during GNews search.");

                throw new InvalidOperationException(
                    "An unexpected error occurred while searching for related news.",
                    ex);
            }
        }

        private class GNewsResponse
        {
            public List<GNewsArticle>? Articles { get; set; }
        }

        private class GNewsArticle
        {
            public string? Title { get; set; }

            public string? Description { get; set; }

            public string? Url { get; set; }

            public string? PublishedAt { get; set; }

            public GNewsSource? Source { get; set; }
        }

        private class GNewsSource
        {
            public string? Name { get; set; }
        }
    }

    public class NewsSearchResult
    {
        public string Title { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public string Url { get; set; } = string.Empty;

        public string SourceName { get; set; } = string.Empty;

        public string PublishedAt { get; set; } = string.Empty;
    }
}
