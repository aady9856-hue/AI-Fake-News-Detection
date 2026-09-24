using System.Security.Claims;
using FakeNewsDetection.Data;
using FakeNewsDetection.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FakeNewsDetection.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DashboardController(ApplicationDbContext context)
        {
            _context = context
                ?? throw new ArgumentNullException(nameof(context));
        }

        // GET: /Dashboard
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                return Challenge();
            }

            var articles = await _context.NewsArticles
                .AsNoTracking()
                .Where(a => a.UserId == userId)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();

            var model = new DashboardViewModel
            {
                TotalArticles = articles.Count,

                RealArticles = articles.Count(
                    a => a.Prediction == "REAL"),

                FakeArticles = articles.Count(
                    a => a.Prediction == "FAKE"),

                AverageConfidence = articles.Any()
                    ? articles.Average(a => a.Confidence ?? 0)
                    : 0,

                RecentArticles = articles
                    .Take(5)
                    .ToList()
            };

            return View(model);
        }
    }
}