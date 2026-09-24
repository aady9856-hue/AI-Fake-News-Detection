using FakeNewsDetection.Models;

namespace FakeNewsDetection.Services
{
    /// <summary>
    /// Delegates AI-assisted fake-news analysis to <see cref="AIService"/>, which calls
    /// the configured OpenAI Responses API. The prediction, confidence, explanation, and
    /// indicators returned by this service are an AI-assisted analysis only and are not
    /// a guarantee that a given article is objectively true or false.
    /// </summary>
    public class FakeNewsDetectionService : IFakeNewsDetectionService
    {
        private readonly AIService _aiService;

        public FakeNewsDetectionService(AIService aiService)
        {
            _aiService = aiService ?? throw new ArgumentNullException(nameof(aiService));
        }

        public async Task<PredictionResult> AnalyzeAsync(NewsArticle article)
        {
            if (article == null)
            {
                throw new ArgumentNullException(nameof(article));
            }

            string headline = article.Headline ?? string.Empty;
            string content = article.ArticleContent ?? string.Empty;
            string combinedText = $"{headline}\n\n{content}";

            PredictionResult result = await _aiService.AnalyzeTextAsync(combinedText);

            return result;
        }
    }
}