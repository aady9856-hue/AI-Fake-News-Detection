using System.ComponentModel.DataAnnotations;

namespace FakeNewsDetection.Models
{
    public enum PredictionType
    {
        REAL,
        FAKE,
        UNCERTAIN
    }

    public class PredictionResult
    {
        [Required(ErrorMessage = "Prediction is required.")]
        public PredictionType Prediction { get; set; }

        [Range(0.0, 100.0, ErrorMessage = "Confidence must be between 0 and 100.")]
        public double Confidence { get; set; }

        [DataType(DataType.MultilineText)]
        public string? Explanation { get; set; }

        public List<string> Indicators { get; set; } = new List<string>();
    }
}