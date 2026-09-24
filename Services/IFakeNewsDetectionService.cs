using FakeNewsDetection.Models;

namespace FakeNewsDetection.Services
{
    public interface IFakeNewsDetectionService
    {
        Task<PredictionResult> AnalyzeAsync(NewsArticle article);
    }
}