using System.Globalization;
using System.Text.RegularExpressions;
using Thinker.Models;

namespace Thinker.Services;

public static partial class PowerSettingsParser
{
    public static string ParseActiveSchemeGuid(string output)
    {
        var match = GuidRegex().Match(output);
        if (!match.Success)
        {
            throw new InvalidOperationException("Unable to parse active power scheme GUID.");
        }

        return match.Groups["guid"].Value.ToLowerInvariant();
    }

    public static PowerSchemeState ParseLidActions(string schemeGuid, string output)
    {
        var ac = ParseHexIndex(output, "当前交流电源设置索引");
        var dc = ParseHexIndex(output, "当前直流电源设置索引");

        if (ac is null || dc is null)
        {
            throw new InvalidOperationException("Unable to parse AC/DC lid action indexes.");
        }

        return new PowerSchemeState(schemeGuid, (LidAction)ac.Value, (LidAction)dc.Value);
    }

    private static int? ParseHexIndex(string output, string label)
    {
        var pattern = Regex.Escape(label) + @":\s*0x(?<value>[0-9a-fA-F]+)";
        var match = Regex.Match(output, pattern);
        if (!match.Success)
        {
            return null;
        }

        return int.Parse(match.Groups["value"].Value, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
    }

    [GeneratedRegex(@"(?<guid>[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12})")]
    private static partial Regex GuidRegex();
}
