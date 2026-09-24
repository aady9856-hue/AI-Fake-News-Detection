using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FakeNewsDetection.Models
{
    public class NewsArticle
    {
        [Key]
        public int Id { get; set; }

        // Logged-in user's Identity ID
        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Headline is required.")]
        [StringLength(300, MinimumLength = 3, ErrorMessage = "Headline must be between 3 and 300 characters.")]
        public string Headline { get; set; } = string.Empty;

        [Required(ErrorMessage = "Article content is required.")]
        [MinLength(20, ErrorMessage = "Article content must be at least 20 characters long.")]
        [DataType(DataType.MultilineText)]
        public string ArticleContent { get; set; } = string.Empty;

        [Url(ErrorMessage = "Please enter a valid URL.")]
        [StringLength(2048)]
        public string? Url { get; set; }

        [StringLength(50)]
        public string? Prediction { get; set; }

        [Range(0.0, 100.0, ErrorMessage = "Confidence must be between 0 and 100.")]
        [Column(TypeName = "decimal(5,2)")]
        public double? Confidence { get; set; }

        [DataType(DataType.MultilineText)]
        public string? Explanation { get; set; }

        [Required]
        [DataType(DataType.DateTime)]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}