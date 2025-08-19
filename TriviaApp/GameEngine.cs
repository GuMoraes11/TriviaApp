using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace TriviaGame
{
    public class GameEngine
    {
        private readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };

        private readonly Random _rng = new Random();

        public void StartGame()
        {
            Console.Clear();
            PrintHeader();

            while (true)
            {
                var difficulty = PromptForDifficulty();
                if (difficulty == null)
                {
                    // Quit selected
                    Console.WriteLine("\nThanks for playing! 👋");
                    return;
                }

                var questions = LoadQuestions(difficulty);
                if (questions.Count == 0)
                {
                    WriteWarn($"No questions found for '{difficulty}'. Make sure data/{difficulty.ToLower()}.json exists.");
                    PressAnyKey("\nPress any key to return to the menu...");
                    Console.Clear();
                    continue;
                }

                PlayQuestions(questions, difficulty);
                PressAnyKey("\nPress any key to return to the menu...");
                Console.Clear();
                PrintHeader();
            }
        }

        private static void PrintHeader()
        {
            Console.WriteLine("====================================");
            Console.WriteLine("           TERMINAL TRIVIA          ");
            Console.WriteLine("====================================\n");
        }

        private static void WriteWarn(string msg)
        {
            var old = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(msg);
            Console.ForegroundColor = old;
        }

        private static void WriteGood(string msg)
        {
            var old = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(msg);
            Console.ForegroundColor = old;
        }

        private static void WriteBad(string msg)
        {
            var old = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(msg);
            Console.ForegroundColor = old;
        }

        private static void PressAnyKey(string prompt)
        {
            Console.Write(prompt);
            Console.ReadKey(true);
        }

        private static string? PromptForDifficulty()
        {
            Console.WriteLine("Choose a difficulty:");
            Console.WriteLine("  [E] Easy");
            Console.WriteLine("  [M] Medium");
            Console.WriteLine("  [H] Hard");
            Console.WriteLine("  [Q] Quit");

            while (true)
            {
                Console.Write("\nYour choice: ");
                var input = Console.ReadLine()?.Trim().ToUpperInvariant();

                switch (input)
                {
                    case "E": return "easy";
                    case "M": return "medium";
                    case "H": return "hard";
                    case "Q": return null;
                    default:
                        WriteWarn("Please enter E, M, H, or Q.");
                        break;
                }
            }
        }

        public List<Question> LoadQuestions(string difficulty)
        {
            var fileName = $"{difficulty.ToLowerInvariant()}.json";
            var candidates = GetCandidatePaths(fileName);

            foreach (var path in candidates)
            {
                if (File.Exists(path))
                {
                    try
                    {
                        var json = File.ReadAllText(path);
                        var list = JsonSerializer.Deserialize<List<Question>>(json, _jsonOptions) ?? new List<Question>();
                        return NormalizeQuestions(list);
                    }
                    catch (Exception ex)
                    {
                        WriteWarn($"Failed to read '{path}': {ex.Message}");
                        return new List<Question>();
                    }
                }
            }

            // Not found anywhere
            WriteWarn($"Could not find '{fileName}' in any expected data folder.");
            return new List<Question>();
        }

        private static List<string> GetCandidatePaths(string fileName)
        {
            // We try several common locations so it "just works" whether you run with `dotnet run`
            // or from the compiled output folder.
            var cwd = Directory.GetCurrentDirectory();
            var baseDir = AppContext.BaseDirectory;

            var guess = new List<string>
            {
                Path.Combine(cwd, "data", fileName),
                Path.Combine(baseDir, "data", fileName),
                Path.Combine(cwd, "..", "..", "data", fileName),     // if running from bin/Debug/netX.Y
                Path.Combine(baseDir, "..", "..", "..", "data", fileName)
            };

            // Normalize
            return guess.Select(p => Path.GetFullPath(p)).Distinct().ToList();
        }

        private static List<Question> NormalizeQuestions(List<Question> list)
        {
            // Ensure option keys are standardized to uppercase A-D and trim whitespace
            foreach (var q in list)
            {
                var normalized = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (var kvp in q.Options)
                {
                    var key = (kvp.Key ?? "").Trim().ToUpperInvariant();
                    if (!string.IsNullOrEmpty(key))
                    {
                        normalized[key] = (kvp.Value ?? "").Trim();
                    }
                }
                q.Options = normalized;
                q.Answer = (q.Answer ?? "").Trim().ToUpperInvariant();
                q.Text = (q.Text ?? "").Trim();
            }
            return list;
        }

        private void PlayQuestions(List<Question> questions, string difficulty)
        {
            // Shuffle order for variety
            Shuffle(questions);

            int total = questions.Count;
            int score = 0;

            Console.Clear();
            Console.WriteLine($"Difficulty: {TitleCase(difficulty)}");
            Console.WriteLine($"Questions:  {total}\n");

            for (int i = 0; i < total; i++)
            {
                var q = questions[i];

                Console.WriteLine($"Q{i + 1}. {q.Text}");
                foreach (var key in new[] { "A", "B", "C", "D" })
                {
                    if (q.Options.TryGetValue(key, out var value))
                    {
                        Console.WriteLine($"   {key}) {value}");
                    }
                }

                var user = PromptAnswer();
                if (string.Equals(user, q.Answer, StringComparison.OrdinalIgnoreCase))
                {
                    WriteGood("Correct! ✔\n");
                    score++;
                }
                else
                {
                    var correctText = q.Options.TryGetValue(q.Answer, out var txt) ? txt : "(unknown)";
                    WriteBad($"Wrong! ✖  Correct answer was {q.Answer}) {correctText}\n");
                }
            }

            ShowResult(score, total, difficulty);
        }

        private static void ShowResult(int score, int total, string difficulty)
        {
            Console.WriteLine("====================================");
            Console.WriteLine("             GAME  OVER             ");
            Console.WriteLine("====================================");
            Console.WriteLine($"Difficulty: {TitleCase(difficulty)}");
            Console.WriteLine($"Score:      {score} / {total}");

            var pct = total > 0 ? (score * 100.0 / total) : 0;
            Console.WriteLine($"Accuracy:   {pct:0.##}%");
        }

        private static string PromptAnswer()
        {
            while (true)
            {
                Console.Write("\nYour answer (A/B/C/D): ");
                var input = Console.ReadLine()?.Trim().ToUpperInvariant();
                if (input is "A" or "B" or "C" or "D")
                    return input!;
                WriteWarn("Please enter A, B, C, or D.");
            }
        }

        private static void Shuffle<T>(IList<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = RandomShared.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        private static string TitleCase(string s) =>
            string.IsNullOrWhiteSpace(s) ? s : char.ToUpperInvariant(s[0]) + s.Substring(1).ToLowerInvariant();

        // Use a single shared RNG for Shuffle to avoid bias with multiple instances
        private static class RandomShared
        {
            private static readonly Random _r = new Random();
            public static int Next(int max) => _r.Next(max);
        }
    }
}
