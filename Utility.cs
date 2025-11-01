using System.Text.RegularExpressions;

namespace SteamUpdateTracker;

public static class Utility
{
    public static List<string> SplitString(string str, int messageSize) => Enumerable
        .Range(0, (str.Length + messageSize - 1) / messageSize)
        .Select(i => str.Substring(i * messageSize, Math.Min(messageSize, str.Length - i * messageSize)))
        .ToList();

    public static string BBCodeToMarkdown(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        string output = input;

        // Headings
        output = Regex.Replace(output, @"\[h2\](.*?)\[/h2\]", "## $1\n");
        output = Regex.Replace(output, @"\[h3\](.*?)\[/h3\]", "### $1\n");

        // Text formatting
        output = output.Replace("[b]", "**").Replace("[/b]", "**");
        output = output.Replace("[i]", "*").Replace("[/i]", "*");
        output = output.Replace("[u]", "__").Replace("[/u]", "__");

        // Lists
        output = Regex.Replace(output, @"\[list\](.*?)\[/list\]", m =>
        {
            var items = Regex.Split(m.Groups[1].Value, @"\[\*\]")
                             .Where(x => !string.IsNullOrWhiteSpace(x))
                             .Select(x => "- " + x.Trim());
            return string.Join("\n", items) + "\n";
        }, RegexOptions.Singleline);

        // URLs: [url=link]text[/url] → [text](link)
        output = Regex.Replace(output, @"\[url=(.*?)\](.*?)\[/url\]", "[$2]($1)");

        // Remove any leftover BBCode tags
        output = Regex.Replace(output, @"\[(.*?)\]", "");

        return output.Trim();
    }
}
