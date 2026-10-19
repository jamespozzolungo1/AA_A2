using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace AIStudyPlannerApp.Backend
{
    // sends chat to deepseek and gets the reply back
    public class AIService : IAIService
    {
        // one shared httpclient, making a new one every request can run out of connections
        private static readonly HttpClient client = new()
        {
            Timeout = TimeSpan.FromSeconds(60)
        };

        private const string ApiUrl =
            "https://api.deepseek.com/chat/completions";

        private const string Model = "deepseek-flash";

        public async Task<string> GetReplyAsync(
            List<(string Role, string Content)> history,
            string? studyContext = null)
        {
            // api key comes from env 
            string? apiKey =
                Environment.GetEnvironmentVariable("DEEPSEEK_API_KEY");

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                throw new InvalidOperationException(
                    "DEEPSEEK_API_KEY is missing. Please configure the environment variable and restart Visual Studio.");
            }

            // tells the ai what it's suppose to be/do
            string systemPrompt =
                    "You are an AI study assistant. Help students " +
                    "with C#, .NET, programming, databases, and study " +
                    "planning. Explain concepts clearly and provide " +
                    "practical examples when useful.";

            // if we have user data, add them to advice so its personalised to them
            if (!string.IsNullOrWhiteSpace(studyContext))
            {
                systemPrompt += "\n\nHere is the student's current study data. " +
                    "Use this information to provide personalized advice when it is relevant" +
                    "(for example, what to study next or upcoming deadlines). " + 
                    "Do not mention it if the question is unrelated. \n\n" +
                    studyContext;
            }

            var messages = new List<Dictionary<string, string>>
            {
                new()
                {
                    ["role"] = "system",
                    ["content"] = systemPrompt
                }
            };


            // only send user/assistant messages and skip empty ones
            foreach (var item in history)
            {
                if (item.Role != "user" &&
                    item.Role != "assistant")
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(item.Content))
                {
                    continue;
                }

                messages.Add(new Dictionary<string, string>
                {
                    ["role"] = item.Role,
                    ["content"] = item.Content
                });
            }

            var requestBody = new
            {
                model = Model,
                messages = messages
            };

            string requestJson =
                JsonSerializer.Serialize(requestBody);

            using var request = new HttpRequestMessage(
                HttpMethod.Post, ApiUrl);

            // api key goes in auth header
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", apiKey);

            request.Content = new StringContent(
                requestJson,
                Encoding.UTF8,
                "application/json");

            using HttpResponseMessage response =
                await client.SendAsync(request);

            string responseJson =
                await response.Content.ReadAsStringAsync();

            // if the api errors, throw with the status and body so we see whats wrong
            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException(
                    $"DeepSeek API returned HTTP " +
                    $"{(int)response.StatusCode} " +
                    $"{response.ReasonPhrase}.\n{responseJson}");
            }

            // parses successful response
            using JsonDocument document =
                JsonDocument.Parse(responseJson);

            JsonElement root = document.RootElement;

            if (!root.TryGetProperty(
                    "choices", out JsonElement choices) ||
                choices.GetArrayLength() == 0)
            {
                throw new InvalidOperationException(
                    "DEEPSEEK returned no response choices.");
            }

            JsonElement message =
                choices[0].GetProperty("message");

            // ensures the message contains text 
            if (!message.TryGetProperty(
                    "content", out JsonElement content) ||
                content.ValueKind != JsonValueKind.String)
            {
                throw new InvalidOperationException(
                    "DEEPSEEK response did not contain text content.");
            }

            string reply = content.GetString() ?? "";

            if (string.IsNullOrWhiteSpace(reply))
            {
                throw new InvalidOperationException(
                    "DEEPSEEK returned an empty response.");
            }

            return reply.Trim();
        }
    }
}