using System.Security.Claims;

using FakeNewsDetection.Data;
using FakeNewsDetection.Models;
using FakeNewsDetection.Services;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FakeNewsDetection.Controllers
{
    [Authorize]
    public class NewsController : Controller
    {
        private readonly IFakeNewsDetectionService _detectionService;
        private readonly ApplicationDbContext _context;
        private readonly NewsUrlService _newsUrlService;
        private readonly NewsSearchService _newsSearchService;
        private readonly ILogger<NewsController> _logger;

        public NewsController(
            IFakeNewsDetectionService detectionService,
            ApplicationDbContext context,
            NewsUrlService newsUrlService,
            NewsSearchService newsSearchService,
            ILogger<NewsController> logger)
        {
            _detectionService = detectionService;
            _context = context;
            _newsUrlService = newsUrlService;
            _newsSearchService = newsSearchService;
            _logger = logger;
        }

        // =========================================================
        // NEWS INDEX
        // =========================================================

        [HttpGet]
        public IActionResult Index()
        {
            return RedirectToAction(nameof(Check));
        }

        // =========================================================
        // CHECK NEWS - GET
        // =========================================================

        [HttpGet]
        public IActionResult Check()
        {
            return View(new NewsCheckViewModel());
        }

        // =========================================================
        // CHECK NEWS - POST
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Check(
            NewsCheckViewModel model)
        {
            if (model == null)
            {
                return BadRequest();
            }

            // -----------------------------------------------------
            // STEP 1: Extract article from URL
            // -----------------------------------------------------

            if (!string.IsNullOrWhiteSpace(model.Url))
            {
                try
                {
                    var extracted =
                        await _newsUrlService.ExtractNewsAsync(
                            model.Url);

                    model.Headline =
                        extracted.Headline;

                    model.ArticleContent =
                        extracted.ArticleContent;
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Failed to extract article from URL: {Url}",
                        model.Url);

                    ModelState.AddModelError(
                        nameof(model.Url),
                        "Could not extract the article from this URL. " +
                        "The website may block automated requests.");
                }
            }

            // -----------------------------------------------------
            // STEP 2: Validate headline
            // -----------------------------------------------------

            if (string.IsNullOrWhiteSpace(model.Headline))
            {
                ModelState.AddModelError(
                    nameof(model.Headline),
                    "Headline is required.");
            }
            else if (model.Headline.Length < 3)
            {
                ModelState.AddModelError(
                    nameof(model.Headline),
                    "Headline must be at least 3 characters long.");
            }
            else if (model.Headline.Length > 300)
            {
                ModelState.AddModelError(
                    nameof(model.Headline),
                    "Headline must not exceed 300 characters.");
            }

            // -----------------------------------------------------
            // STEP 3: Validate article content
            // -----------------------------------------------------

            if (string.IsNullOrWhiteSpace(
                model.ArticleContent))
            {
                ModelState.AddModelError(
                    nameof(model.ArticleContent),
                    "Article content is required.");
            }
            else if (model.ArticleContent.Length < 20)
            {
                ModelState.AddModelError(
                    nameof(model.ArticleContent),
                    "Article content must be at least 20 characters long.");
            }

            // -----------------------------------------------------
            // STEP 4: Validate URL
            // -----------------------------------------------------

            if (!string.IsNullOrWhiteSpace(model.Url))
            {
                if (!Uri.TryCreate(
                        model.Url,
                        UriKind.Absolute,
                        out Uri? uri) ||
                    (uri.Scheme != Uri.UriSchemeHttp &&
                     uri.Scheme != Uri.UriSchemeHttps))
                {
                    ModelState.AddModelError(
                        nameof(model.Url),
                        "Please enter a valid HTTP or HTTPS URL.");
                }
            }

            // -----------------------------------------------------
            // If validation failed
            // -----------------------------------------------------

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // -----------------------------------------------------
            // STEP 5: Get logged-in user
            // -----------------------------------------------------

            string? userId =
                User.FindFirstValue(
                    ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                return Challenge();
            }

            // -----------------------------------------------------
            // STEP 6: Create article
            // -----------------------------------------------------

            var article = new NewsArticle
            {
                UserId = userId,

                Headline =
                    model.Headline!,

                ArticleContent =
                    model.ArticleContent!,

                Url =
                    model.Url,

                CreatedAt =
                    DateTime.UtcNow
            };

            // -----------------------------------------------------
            // STEP 7: AI Analysis
            // -----------------------------------------------------

            PredictionResult predictionResult;

            try
            {
                predictionResult =
                    await _detectionService.AnalyzeAsync(
                        article);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error while analyzing news article.");

                ModelState.AddModelError(
                    string.Empty,
                    "An error occurred while analyzing the article. " +
                    "Please try again.");

                return View(model);
            }

            // -----------------------------------------------------
            // STEP 8: Save AI result
            // -----------------------------------------------------

            article.Prediction =
                predictionResult.Prediction.ToString();

            article.Confidence =
                predictionResult.Confidence;

            article.Explanation =
                predictionResult.Explanation;

            // -----------------------------------------------------
            // STEP 9: Search matching news sources
            // -----------------------------------------------------

            List<NewsSearchResult> searchResults =
                new List<NewsSearchResult>();

            try
            {
                searchResults =
                    await _newsSearchService.SearchNewsAsync(
                        article.Headline);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error while searching news sources using GNews.");

                searchResults =
                    new List<NewsSearchResult>();
            }

            // -----------------------------------------------------
            // STEP 10: Save article to database
            // -----------------------------------------------------

            try
            {
                _context.NewsArticles.Add(article);

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error while saving news article.");

                ModelState.AddModelError(
                    string.Empty,
                    "An error occurred while saving the result. " +
                    "Please try again.");

                return View(model);
            }

            // -----------------------------------------------------
            // STEP 11: Store GNews results temporarily
            // -----------------------------------------------------

            TempData["NewsSearchResults"] =
                System.Text.Json.JsonSerializer.Serialize(
                    searchResults);

            // -----------------------------------------------------
            // STEP 12: Go to result page
            // -----------------------------------------------------

            return RedirectToAction(
                nameof(Result),
                new
                {
                    id = article.Id
                });
        }

        // =========================================================
        // RESULT
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Result(
            int id)
        {
            string? userId =
                User.FindFirstValue(
                    ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                return Challenge();
            }

            var article =
                await _context.NewsArticles
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        a =>
                            a.Id == id &&
                            a.UserId == userId);

            if (article == null)
            {
                return NotFound();
            }

            var searchResults =
                new List<NewsSearchResult>();

            if (TempData["NewsSearchResults"]
                is string json &&
                !string.IsNullOrWhiteSpace(json))
            {
                try
                {
                    searchResults =
                        System.Text.Json.JsonSerializer
                            .Deserialize<
                                List<NewsSearchResult>
                            >(json)
                        ?? new List<NewsSearchResult>();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(
                        ex,
                        "Could not deserialize GNews search results.");
                }
            }

            ViewBag.NewsSearchResults =
                searchResults;

            return View(article);
        }

        // =========================================================
        // HISTORY
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> History()
        {
            string? userId =
                User.FindFirstValue(
                    ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                return Challenge();
            }

            var articles =
                await _context.NewsArticles
                    .AsNoTracking()
                    .Where(
                        a =>
                            a.UserId == userId)
                    .OrderByDescending(
                        a =>
                            a.CreatedAt)
                    .ToListAsync();

            return View(articles);
        }

        // =========================================================
        // DELETE ANALYSIS
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(
            int id)
        {
            string? userId =
                User.FindFirstValue(
                    ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                return Challenge();
            }

            // -----------------------------------------------------
            // Get only the current user's article
            // -----------------------------------------------------

            var article =
                await _context.NewsArticles
                    .FirstOrDefaultAsync(
                        a =>
                            a.Id == id &&
                            a.UserId == userId);

            // -----------------------------------------------------
            // Article not found
            // -----------------------------------------------------

            if (article == null)
            {
                return NotFound();
            }

            // -----------------------------------------------------
            // Delete article
            // -----------------------------------------------------

            try
            {
                _context.NewsArticles.Remove(article);

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] =
                    "The analysis was deleted successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error deleting news article {ArticleId}",
                    id);

                TempData["ErrorMessage"] =
                    "Could not delete the analysis. Please try again.";
            }

            return RedirectToAction(nameof(History));
        }

        // =========================================================
        // DOWNLOAD PDF REPORT
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> DownloadPdf(
            int id)
        {
            string? userId =
                User.FindFirstValue(
                    ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                return Challenge();
            }

            // -----------------------------------------------------
            // Get only the current user's article
            // -----------------------------------------------------

            var article =
                await _context.NewsArticles
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        a =>
                            a.Id == id &&
                            a.UserId == userId);

            // -----------------------------------------------------
            // Article not found
            // -----------------------------------------------------

            if (article == null)
            {
                return NotFound();
            }

            try
            {
                // -------------------------------------------------
                // Generate PDF
                // -------------------------------------------------

                var pdfService =
                    new PdfReportService();

                byte[] pdf =
                    pdfService.GenerateReport(
                        article);

                // -------------------------------------------------
                // PDF file name
                // -------------------------------------------------

                string fileName =
                    $"FakeNews_Report_{article.Id}.pdf";

                // -------------------------------------------------
                // Return downloadable PDF
                // -------------------------------------------------

                return File(
                    pdf,
                    "application/pdf",
                    fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error generating PDF report for article {ArticleId}",
                    id);

                return Problem(
                    "Could not generate the PDF report.");
            }
        }
    }
}
