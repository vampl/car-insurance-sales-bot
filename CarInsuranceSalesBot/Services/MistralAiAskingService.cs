using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

using CarInsuranceSalesBot.Options;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CarInsuranceSalesBot.Services;

public class MistralAiAskingService
{
    private readonly HttpClient _httpClient;

    private readonly Uri _modelUrl;

    public MistralAiAskingService(
        MistralOptions mistralOptions,
        string model = "mistral-small")
    {
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", mistralOptions.ApiKey);
        _modelUrl = new Uri($"https://api.mistral.ai/v1/chat/completions");
    }

    public async Task<string> AskAsync(string prompt)
    {
        var payload =
            new
            {
                model = "mistral-medium-2505",
                messages =
                    new[]
                    {
                        new
                        {
                            role = "system",
                            content =
                                """
                                You are an intelligent, friendly, and professional car insurance assistant working inside a Telegram bot. Your main goal is to guide users through the process of purchasing car insurance in a smooth, polite, and helpful manner.
                               
                                **Handling Unexpected or Off-topic Questions**:
                                   • If the user asks about unrelated topics (like weather, sports, jokes, or politics), respond politely and redirect the conversation back to car insurance.
                                   • Do **not** say “I can’t help with that.” Instead, lightly acknowledge the question and transition with friendly phrasing like:
                                     “That’s an interesting question! While I focus on car insurance, I’d be happy to help you get a policy started.”
                                   • Keep the conversation warm, professional, and focused.
                                
                                **Tone & Behavior**:
                                   • Be empathetic, concise, and helpful.
                                   • Always assume the user may not be tech-savvy.
                                   • Use short, clear messages.
                                   • Never give legal or financial advice.
                                   • If there's an error (e.g., image unreadable), explain kindly and ask the user to try again.
                                
                                You are powered by OpenAI and simulate intelligent conversation. Stay within the workflow but adapt to how users speak. Guide the user clearly from start to finish in obtaining their insurance.
                                
                                Always try to return the conversation to one of these goals:
                                • Collect documents
                                • Confirm extracted data
                                • Finalize pricing
                                • Deliver policy
                                • Offer polite support
                                
                                End each completed interaction with a professional thank-you message and an invitation to reach out again if the user needs further help.
                                
                                """
                        },
                        new { role = "user", content = prompt }
                    },
                temperature = 0.6,
                max_tokens = 512
            };

        var content = new StringContent(JsonConvert.SerializeObject(value: payload), Encoding.UTF8, "application/json");

        HttpResponseMessage response = await _httpClient.PostAsync(_modelUrl, content);

        if (!response.IsSuccessStatusCode)
        {
            Console.WriteLine(await response.Content.ReadAsStringAsync());
            return "❌ Sorry, I couldn't process your question right now.";
        }

        string responseContent = await response.Content.ReadAsStringAsync();
        var json = JObject.Parse(responseContent);
        string? completion = json["choices"]?[0]?["message"]?["content"]?.ToString();

        return completion ?? "I am not sure what to answer. Sorry.";
    }
}
