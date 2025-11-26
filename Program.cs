using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace SteamUpdateTracker;

public static class Program
{
    public static string UserAgent { get; } = $"{nameof(SteamUpdateTracker)}/1.0";

    public static async Task Main()
    {
        string steamAppId = Environment.GetEnvironmentVariable("STEAM_APPID");
        string webhookUrl = Environment.GetEnvironmentVariable("WEBHOOK_URL");
        string gistToken = Environment.GetEnvironmentVariable("GIST_TOKEN");
        string gistId = Environment.GetEnvironmentVariable("GIST_ID");
        string prefix = Environment.GetEnvironmentVariable("MESSAGE_PREFIX");

        if (string.IsNullOrWhiteSpace(steamAppId) || string.IsNullOrWhiteSpace(webhookUrl) || string.IsNullOrWhiteSpace(gistToken) || string.IsNullOrWhiteSpace(gistId))
        {
            Console.WriteLine("Missing environment variables. Required: STEAM_APPID, WEBHOOK_URL, GIST_TOKEN, GIST_ID");
            return;
        }

        using HttpClient http = new HttpClient();
        http.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);

        string json = await http.GetStringAsync($"https://api.steampowered.com/ISteamNews/GetNewsForApp/v2/?appid={steamAppId}");
        using JsonDocument doc = JsonDocument.Parse(json);
        JsonElement latest = doc.RootElement.GetProperty("appnews").GetProperty("newsitems")[0];

        string lastPatchGid = await GetLastPatchGid(http, gistId, gistToken);
        string newGid = latest.GetProperty("gid").GetString();

        if (newGid != lastPatchGid && !latest.GetProperty("feedname").ValueEquals("PC Gamer"))
        {
            await SendMessage(http, webhookUrl, latest, prefix);
            await UpdateLastPatchGid(http, gistId, gistToken, newGid);
        }
    }

    private static async Task<string> GetLastPatchGid(HttpClient http, string gistId, string gistToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"https://api.github.com/gists/{gistId}");
        request.Headers.Authorization = new AuthenticationHeaderValue("token", gistToken);

        var response = await http.SendAsync(request);
        response.EnsureSuccessStatusCode();

        string gistResponse = await response.Content.ReadAsStringAsync();
        using var gistDoc = JsonDocument.Parse(gistResponse);

        return gistDoc.RootElement.GetProperty("files").GetProperty("last-patch-gid.txt").GetProperty("content").GetString() ?? "";
    }

    private static async Task UpdateLastPatchGid(HttpClient http, string gistId, string gistToken, string lastPatchGid)
    {
        var updatePayload = JsonSerializer.Serialize(new
        {
            files = new Dictionary<string, object>
            {
                ["last-patch-gid.txt"] = new
                {
                    content = lastPatchGid
                }
            }
        });

        var request = new HttpRequestMessage(HttpMethod.Patch, $"https://api.github.com/gists/{gistId}")
        {
            Content = new StringContent(updatePayload, Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("token", gistToken);

        var response = await http.SendAsync(request);
        response.EnsureSuccessStatusCode();
    }

    private static async Task SendMessage(HttpClient http, string webhookUrl, JsonElement latestPatch, string prefix)
    {
        string title = latestPatch.GetProperty("title").GetString().Trim();
        string url = latestPatch.GetProperty("url").GetString().Trim();
        string contents = latestPatch.GetProperty("contents").GetString().Trim();
        string author = latestPatch.GetProperty("author").GetString().Trim();
        long unixTime = latestPatch.GetProperty("date").GetInt64();

        string message = $"""
        {prefix}
        # {title}
        **Published by *{author}* on <t:{unixTime}:f> (<t:{unixTime}:R>)**
        **{url}**

        {Utility.BBCodeToMarkdown(contents).Trim()}
        """;

        foreach (var chunk in Utility.SplitString(message, 2000))
        {
            var payload = JsonSerializer.Serialize(new
            {
                content = chunk,
                flags = 4
            });

            var response = await http.PostAsync(webhookUrl, new StringContent(payload, Encoding.UTF8, "application/json"));
            response.EnsureSuccessStatusCode();
        }

        Console.WriteLine($"Posted update: {title}");
    }
}