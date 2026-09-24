using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using FakeNewsDetection.Models;

namespace FakeNewsDetection.Services
{
    public class AIService
    {
        private const string OllamaGenerateEndpoint =
            "http://localhost:11434/api/generate";

        private const string ModelName = "qwen3:4b";

        private static readonly HttpClient _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromMinutes(2)
        };

        private readonly ILogger<AIService> _logger;

        public AIService(ILogger<AIService> logger)
        {
            _logger = logger;
        }

        public async Task<PredictionResult> AnalyzeTextAsync(
            string articleText)
        {
            if (string.IsNullOrWhiteSpace(articleText))
            {
                _logger.LogWarning(
                    "AI analysis requested with empty article text.");

                return BuildErrorResult(
                    "Article text is empty.");
            }

            try
            {
                string prompt = BuildPrompt(articleText);

                var requestPayload = new OllamaGenerateRequest
                {
                    Model = ModelName,
                    Prompt = prompt,
                    Stream = false,
                    Think = false,
                    Format = "json",
                    Options = new OllamaGenerateOptions
                    {
                        Temperature = 0.1,
                        NumPredict = 600,
                        TopP = 0.9
                    }
                };

                using HttpResponseMessage response =
                    await _httpClient.PostAsJsonAsync(
                        OllamaGenerateEndpoint,
                        requestPayload);

                if (!response.IsSuccessStatusCode)
                {
                    string errorBody =
                        await response.Content.ReadAsStringAsync();

                    _logger.LogError(
                        "Ollama API failed. Status: {StatusCode}, Response: {Response}",
                        response.StatusCode,
                        errorBody);

                    return BuildUnavailableResult(
                        $"Ollama returned HTTP {(int)response.StatusCode}.");
                }

                var ollamaResponse =
                    await response.Content
                        .ReadFromJsonAsync<OllamaGenerateResponse>();

                if (ollamaResponse == null)
                {
                    _logger.LogWarning(
                        "Ollama returned a null response.");

                    return BuildErrorResult(
                        "The AI service returned an empty response.");
                }

                if (string.IsNullOrWhiteSpace(
                    ollamaResponse.Response))
                {
                    _logger.LogWarning(
                        "Ollama returned an empty response body.");

                    return BuildErrorResult(
                        "The AI service returned no analysis.");
                }

                _logger.LogInformation(
                    "OLLAMA RAW RESPONSE: {RawResponse}",
                    ollamaResponse.Response);

                return ParseModelOutput(
                    ollamaResponse.Response);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(
                    ex,
                    "Could not connect to Ollama at {Endpoint}.",
                    OllamaGenerateEndpoint);

                return BuildUnavailableResult(
                    "The local Ollama service is unavailable.");
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogError(
                    ex,
                    "Ollama request timed out.");

                return BuildUnavailableResult(
                    "The Ollama AI request timed out.");
            }
            catch (JsonException ex)
            {
                _logger.LogError(
                    ex,
                    "Could not deserialize the Ollama response.");

                return BuildErrorResult(
                    "The AI service returned an invalid response.");
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Unexpected error during AI analysis.");

                return BuildErrorResult(
                    "An unexpected error occurred during AI analysis.");
            }
        }

        private static string BuildPrompt(string articleText)
        {
            string currentDate =
                DateTime.Now.ToString(
                    "yyyy-MM-dd",
                    CultureInfo.InvariantCulture);

            string currentYear =
                DateTime.Now.Year.ToString(
                    CultureInfo.InvariantCulture);

            return
                "You are an AI system that classifies news articles as REAL or FAKE.\n\n" +

                "CURRENT DATE CONTEXT:\n" +
                $"Today is {currentDate}.\n" +
                $"The current year is {currentYear}.\n\n" +

                "IMPORTANT DATE RULE:\n" +
                "- A future date in a news article does NOT automatically mean the article is fake.\n" +
                "- News articles can legitimately announce future events, planned visits, scheduled meetings, upcoming launches, elections, programmes, ceremonies, or government events.\n" +
                "- For example, an article published before an event can correctly say that a person WILL visit a location on a future date.\n" +
                "- Do NOT classify an article as FAKE merely because an event mentioned in it has not happened yet.\n" +
                "- Only treat a date as evidence of falsity when the article contains a genuine contradiction, impossible timeline, or clearly impossible historical claim.\n" +
                "- Carefully distinguish between an event date and the date on which the article was published or announced.\n\n" +

                "Analyze the complete article carefully.\n" +
                "Do not simply judge the writing style.\n" +
                "Do not assume an article is real just because it sounds professional.\n" +
                "Do not assume an article is fake just because it makes an unusual claim.\n\n" +

                "REAL indicators may include:\n" +
                "- Specific factual details.\n" +
                "- Plausible events.\n" +
                "- Consistent information.\n" +
                "- Reasonable claims.\n" +
                "- Specific organizations, locations, dates, numbers, or events.\n" +
                "- Official government or institutional announcements.\n" +
                "- Clearly structured factual reporting.\n\n" +

                "FAKE indicators may include:\n" +
                "- Impossible claims.\n" +
                "- Exaggerated or sensational statements.\n" +
                "- Absolute guarantees.\n" +
                "- Claims that contradict basic scientific knowledge.\n" +
                "- Suspicious or misleading statements.\n" +
                "- Unrealistic promises.\n" +
                "- Internal contradictions.\n" +
                "- Clearly impossible dates or timelines.\n\n" +

                "SOURCE AND EVIDENCE RULE:\n" +
                "- Evaluate the evidence available in the article itself.\n" +
                "- Do not invent facts that are not present in the article.\n" +
                "- Do not invent a publication year.\n" +
                "- Do not assume the article was written in an earlier year unless the article explicitly provides evidence for that conclusion.\n" +
                "- If the article contains an official organization, government department, named location, date, event, or programme, consider those details as evidence but do not automatically assume they prove the article is real.\n\n" +

                "IMPORTANT:\n" +
                "You cannot browse the internet.\n" +
                "Judge the article using the information available in the article.\n" +
                "Do not claim that you verified the article online.\n\n" +

                "SCORING:\n" +
                "- Give a REAL credibility score from 0 to 100.\n" +
                "- Give a FAKE credibility score from 0 to 100.\n" +
                "- The scores must represent your assessment of THIS article.\n" +
                "- Higher realScore means the article appears more credible based on the available evidence.\n" +
                "- Higher fakeScore means the article appears more misleading or unreliable based on the available evidence.\n" +
                "- Do not make fakeScore high merely because the article discusses a future event.\n" +
                "- Do not make realScore high merely because the article uses formal language.\n" +
                "- Consider multiple indicators before deciding the scores.\n" +
                "- Do not set both scores to 0.\n\n" +

                "EXPLANATION:\n" +
                "- Explain why you gave these scores.\n" +
                "- Discuss specific claims from THIS article.\n" +
                "- If dates are relevant, explain their context correctly.\n" +
                "- Do not say an article is fake simply because its event date is in the future.\n\n" +

                "You MUST return ONLY valid JSON in exactly this structure:\n" +
                "{\n" +
                "  \"realScore\": NUMBER,\n" +
                "  \"fakeScore\": NUMBER,\n" +
                "  \"explanation\": \"Your actual explanation of this article\",\n" +
                "  \"indicators\": [\n" +
                "    \"Actual indicator 1\",\n" +
                "    \"Actual indicator 2\",\n" +
                "    \"Actual indicator 3\"\n" +
                "  ]\n" +
                "}\n\n" +

                "ARTICLE:\n" +
                "-------------------------\n" +
                articleText +
                "\n-------------------------\n\n" +

                "Now classify this article.";
        }

        private PredictionResult ParseModelOutput(
            string rawOutput)
        {
            try
            {
                string jsonText =
                    ExtractJsonObject(rawOutput);

                if (string.IsNullOrWhiteSpace(jsonText))
                {
                    _logger.LogWarning(
                        "No JSON object found in Ollama response.");

                    return BuildErrorResult(
                        "The AI did not return structured JSON.");
                }

                using JsonDocument document =
                    JsonDocument.Parse(jsonText);

                JsonElement root =
                    document.RootElement;

                double realScore =
                    GetScore(root, "realScore");

                double fakeScore =
                    GetScore(root, "fakeScore");

                string explanation =
                    GetString(
                        root,
                        "explanation",
                        "The AI provided a credibility assessment.");

                var indicators =
                    new List<string>();

                if (root.TryGetProperty(
                    "indicators",
                    out JsonElement indicatorsProperty) &&
                    indicatorsProperty.ValueKind ==
                    JsonValueKind.Array)
                {
                    foreach (JsonElement item in
                             indicatorsProperty.EnumerateArray())
                    {
                        if (item.ValueKind !=
                            JsonValueKind.String)
                        {
                            continue;
                        }

                        string? indicator =
                            item.GetString();

                        if (!string.IsNullOrWhiteSpace(
                            indicator))
                        {
                            indicators.Add(
                                indicator);
                        }
                    }
                }

                if (indicators.Count == 0)
                {
                    indicators.Add(
                        "AI-assisted credibility analysis completed.");
                }

                realScore =
                    Math.Clamp(
                        realScore,
                        0,
                        100);

                fakeScore =
                    Math.Clamp(
                        fakeScore,
                        0,
                        100);

                PredictionType prediction =
                    CalculatePrediction(
                        realScore,
                        fakeScore);

                double confidence =
                    CalculateConfidence(
                        prediction,
                        realScore,
                        fakeScore);

                _logger.LogInformation(
                    "AI Scores -> REAL: {RealScore}, FAKE: {FakeScore}, FINAL: {Prediction}",
                    realScore,
                    fakeScore,
                    prediction);

                Console.WriteLine(
                    $"AI SCORES => REAL: {realScore}, " +
                    $"FAKE: {fakeScore}, " +
                    $"FINAL: {prediction}");

                return new PredictionResult
                {
                    Prediction = prediction,

                    Confidence =
                        Math.Round(
                            confidence,
                            2),

                    Explanation =
                        explanation +
                        " This is an AI-assisted assessment and does not guarantee factual accuracy.",

                    Indicators = indicators
                };
            }
            catch (JsonException ex)
            {
                _logger.LogError(
                    ex,
                    "Invalid JSON received from Ollama.");

                return BuildErrorResult(
                    "The AI response could not be read.");
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Unexpected error while processing AI output.");

                return BuildErrorResult(
                    "The AI response could not be processed.");
            }
        }

        private static PredictionType CalculatePrediction(
            double realScore,
            double fakeScore)
        {
            if (realScore >= fakeScore)
            {
                return PredictionType.REAL;
            }

            return PredictionType.FAKE;
        }

        private static double CalculateConfidence(
            PredictionType prediction,
            double realScore,
            double fakeScore)
        {
            double winningScore =
                prediction == PredictionType.REAL
                    ? realScore
                    : fakeScore;

            double losingScore =
                prediction == PredictionType.REAL
                    ? fakeScore
                    : realScore;

            double difference =
                Math.Abs(
                    winningScore -
                    losingScore);

            double confidence =
                winningScore +
                (difference * 0.20);

            return Math.Clamp(
                confidence,
                50,
                99);
        }

        private static double GetScore(
            JsonElement root,
            string propertyName)
        {
            if (!root.TryGetProperty(
                propertyName,
                out JsonElement property))
            {
                return 0;
            }

            if (property.ValueKind ==
                JsonValueKind.Number)
            {
                if (property.TryGetDouble(
                    out double number))
                {
                    return number;
                }

                return 0;
            }

            if (property.ValueKind ==
                JsonValueKind.String)
            {
                string? text =
                    property.GetString();

                if (double.TryParse(
                    text,
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out double value))
                {
                    return value;
                }
            }

            return 0;
        }

        private static string GetString(
            JsonElement root,
            string propertyName,
            string defaultValue)
        {
            if (root.TryGetProperty(
                propertyName,
                out JsonElement property) &&
                property.ValueKind ==
                JsonValueKind.String)
            {
                string? value =
                    property.GetString();

                if (!string.IsNullOrWhiteSpace(
                    value))
                {
                    return value;
                }
            }

            return defaultValue;
        }

        private static string ExtractJsonObject(
            string text)
        {
            int firstBrace =
                text.IndexOf('{');

            int lastBrace =
                text.LastIndexOf('}');

            if (firstBrace >= 0 &&
                lastBrace > firstBrace)
            {
                return text.Substring(
                    firstBrace,
                    lastBrace -
                    firstBrace +
                    1);
            }

            return string.Empty;
        }

        private static PredictionResult BuildUnavailableResult(
            string message)
        {
            return new PredictionResult
            {
                Prediction =
                    PredictionType.FAKE,

                Confidence = 0,

                Explanation =
                    message +
                    " The system could not complete the AI analysis.",

                Indicators =
                    new List<string>
                    {
                        "Ollama service unavailable.",
                        "AI analysis could not be completed."
                    }
            };
        }

        private static PredictionResult BuildErrorResult(
            string message)
        {
            return new PredictionResult
            {
                Prediction =
                    PredictionType.FAKE,

                Confidence = 0,

                Explanation =
                    message +
                    " The system could not complete the AI analysis.",

                Indicators =
                    new List<string>
                    {
                        "AI response processing failed.",
                        "Analysis could not be completed."
                    }
            };
        }

        private class OllamaGenerateRequest
        {
            [JsonPropertyName("model")]
            public string Model { get; set; } =
                string.Empty;

            [JsonPropertyName("prompt")]
            public string Prompt { get; set; } =
                string.Empty;

            [JsonPropertyName("stream")]
            public bool Stream { get; set; }

            [JsonPropertyName("think")]
            public bool Think { get; set; }

            [JsonPropertyName("format")]
            public string? Format { get; set; }

            [JsonPropertyName("options")]
            public OllamaGenerateOptions? Options { get; set; }
        }

        private class OllamaGenerateOptions
        {
            [JsonPropertyName("temperature")]
            public double Temperature { get; set; }

            [JsonPropertyName("num_predict")]
            public int NumPredict { get; set; }

            [JsonPropertyName("top_p")]
            public double TopP { get; set; }
        }

        private class OllamaGenerateResponse
        {
            [JsonPropertyName("response")]
            public string? Response { get; set; }

            [JsonPropertyName("thinking")]
            public string? Thinking { get; set; }

            [JsonPropertyName("done")]
            public bool Done { get; set; }
        }
    }
}

