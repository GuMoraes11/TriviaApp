using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace TriviaGame
{
    // Simple data object that matches your JSON files
    public class Question
    {
        [JsonPropertyName("question")]
        public string Text { get; set; } = string.Empty;

        // Keys should be "A","B","C","D"
        [JsonPropertyName("options")]
        public Dictionary<string, string> Options { get; set; } = new();

        // The correct answer letter, e.g., "A"
        [JsonPropertyName("answer")]
        public string Answer { get; set; } = string.Empty;
    }
}
