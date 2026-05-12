using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Midori.Utils.Extensions;

namespace fluxel.Utils;

public static class StringValidator
{
    public static bool IsBlacklisted(this string input)
    {
        var path = Path.Combine("username-blacklist.txt");

        if (!File.Exists(path))
            return false;

        var file = File.ReadAllLines(path);
        var split = file.Select(s => s.Trim())
                        .Where(s => !string.IsNullOrWhiteSpace(s))
                        .ToArray();

        var isInSplit = split.Any(input.ContainsLower);
        return isInSplit;
    }

    public static bool ValidateArtistID(string id)
        => Regex.IsMatch(id, "^[a-z0-9-]{1,32}$");
}
