using System.ComponentModel.DataAnnotations;

namespace FakeNewsDetection.Models
{
    public class NewsCheckViewModel
    {
        [Url(ErrorMessage = "Please enter a valid URL.")]
        [StringLength(2048)]
        public string? Url { get; set; }

        [StringLength(
            300,
            MinimumLength = 3,
            ErrorMessage = "Headline must be between 3 and 300 characters.")]
        public string? Headline { get; set; }

        [MinLength(
            20,
            ErrorMessage = "Article content must be at least 20 characters long.")]
        [DataType(DataType.MultilineText)]
        public string? ArticleContent { get; set; }
    }
}