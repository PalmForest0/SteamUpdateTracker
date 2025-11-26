using System.Text.RegularExpressions;

namespace SteamUpdateTracker;

public static class Utility
{
    public static List<string> SplitString(string str, int messageSize)
    {
        int startIndex = 0;
        List<string> result = new List<string>();

        while (startIndex < str.Length - 1)
        {
            string chunk = str.Substring(startIndex, Math.Min(messageSize, str.Length - startIndex));
            int lastNewLineIndex = chunk.LastIndexOf("\n");

            if (lastNewLineIndex == -1)
            {
                result.Add(chunk);
                startIndex += messageSize + 1;
            }
            else
            {
                result.Add(chunk.Substring(0, lastNewLineIndex + 1));
                startIndex += lastNewLineIndex + 1;
            }
        }

        return result;
    }

    public static string BBCodeToMarkdown(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        string output = input;

        // Headings: ensure blank line before heading so Discord renders it
        output = Regex.Replace(output, @"\[h1\](.*?)\[/h1\]", "\n## $1", RegexOptions.Singleline | RegexOptions.IgnoreCase);
        output = Regex.Replace(output, @"\[h2\](.*?)\[/h2\]", "\n## $1", RegexOptions.Singleline | RegexOptions.IgnoreCase);
        output = Regex.Replace(output, @"\[h3\](.*?)\[/h3\]", "\n## $1", RegexOptions.Singleline | RegexOptions.IgnoreCase);

        // Paragraphs: make sure paragraphs become separated blocks
        output = Regex.Replace(output, @"\[p\](.*?)\[/p\]", "\n$1", RegexOptions.Singleline | RegexOptions.IgnoreCase);

        // Text formatting: these are done before link conversion so nested formatting is preserved inside link text
        output = Regex.Replace(output, @"\[b\](.*?)\[/b\]", "**$1**", RegexOptions.Singleline | RegexOptions.IgnoreCase);
        output = Regex.Replace(output, @"\[i\](.*?)\[/i\]", "*$1*", RegexOptions.Singleline | RegexOptions.IgnoreCase);
        output = Regex.Replace(output, @"\[u\](.*?)\[/u\]", "__$1__", RegexOptions.Singleline | RegexOptions.IgnoreCase);
        output = Regex.Replace(output, @"\[strike\](.*?)\[/strike\]", "~~$1~~", RegexOptions.Singleline | RegexOptions.IgnoreCase);

        // Code blocks
        output = Regex.Replace(output, @"\[code\](.*?)\[/code\]", "```\n$1\n```", RegexOptions.Singleline | RegexOptions.IgnoreCase);

        // Lists
        output = Regex.Replace(output, @"\[list\](.*?)\[/list\]", m =>
        {
            var items = Regex.Split(m.Groups[1].Value, @"\[\*\]")
                             .Where(x => !string.IsNullOrWhiteSpace(x))
                             .Select(x => "- " + x.Trim());
            return $"\n{string.Join("\n", items)}\n";
        }, RegexOptions.Singleline | RegexOptions.IgnoreCase);

        // URL with optional quotes in attribute: [url="..."]text[/url] or [url='...']text[/url] or [url=...]text[/url]
        var urlWithTargetPattern = @"\[url=(?:""(?<url>.*?)""|'(?<url>.*?)'|(?<url>.*?))\](?<text>.*?)\[/url\]";
        output = Regex.Replace(output, urlWithTargetPattern, "[${text}](${url})", RegexOptions.Singleline | RegexOptions.IgnoreCase);

        // Bare url tag with no display text: [url]http://x[/url] -> <http://x> (Discord autolink)
        output = Regex.Replace(output, @"\[url\](.*?)\[/url\]", "<$1>", RegexOptions.Singleline | RegexOptions.IgnoreCase);

        // Cleanup: Remove any img tags entirely
        output = Regex.Replace(output, @"\[img\s+[^\]]*\]", "", RegexOptions.IgnoreCase);

        // Remove any leftover full BBCode tags (like unknown tags)
        // This pattern matches an opening or closing tag, optionally with an =arg, but removes the whole tag.
        // It will NOT remove text between tags because we've already converted the common tags above.
        output = Regex.Replace(output, @"\[(?:/?[A-Za-z0-9*]+)(?:=[^\]]+)?\]", "", RegexOptions.Singleline);

        // Normalize multiple blank lines to at most two
        output = Regex.Replace(output, @"\n{3,}", "\n\n", RegexOptions.Singleline);

        return output.Trim();
    }
}