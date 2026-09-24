namespace FakeNewsDetection.Models
{
    public class DashboardViewModel
    {
        public int TotalArticles { get; set; }

        public int RealArticles { get; set; }

        public int FakeArticles { get; set; }

        public double AverageConfidence { get; set; }

        public List<NewsArticle> RecentArticles { get; set; } = new();
    }
}