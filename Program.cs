using System.Text.Json;

namespace SteamUpdateTracker;

public class Program
{
    static readonly HttpClient http = new HttpClient();
    const string LAST_PATCH_FILE = "last_patch.txt";

    public static async Task Main()
    {
        string steamAppId = Environment.GetEnvironmentVariable("STEAM_APPID");
        string webhookUrl = Environment.GetEnvironmentVariable("WEBHOOK_URL");

        string lastPatchId = File.Exists(LAST_PATCH_FILE) ? await File.ReadAllTextAsync(LAST_PATCH_FILE) : "";

        string json = await http.GetStringAsync($"https://api.steampowered.com/ISteamNews/GetNewsForApp/v2/?appid={steamAppId}");
        using JsonDocument doc = JsonDocument.Parse(json);
        JsonElement latest = doc.RootElement.GetProperty("appnews").GetProperty("newsitems")[0];

        string gid = latest.GetProperty("gid").GetString();

        if (gid != lastPatchId && !string.IsNullOrWhiteSpace(webhookUrl) && !string.IsNullOrWhiteSpace(steamAppId))
        {
            string title = latest.GetProperty("title").GetString();
            string contents = latest.GetProperty("contents").GetString();
            long unixTime = long.Parse(latest.GetProperty("date").GetString());

            var payload = JsonSerializer.Serialize(new
            {
                content = $"# {title}\n{contents}\n**Date:** <t:{unixTime}:f> (<t:{unixTime}:R>)\r\n"
            });

            await http.PostAsync(webhookUrl, new StringContent(payload, System.Text.Encoding.UTF8, "application/json"));
            Console.WriteLine($"Posted update: {title}");

            await File.WriteAllTextAsync(LAST_PATCH_FILE, gid);
        }
    }
}