using System.Globalization;
using System.Text.RegularExpressions;
using Thinker.Models;

namespace Thinker.Services;

public static partial class PowerSettingsParser
{
    private const string LidActionSettingGuid = "5ca83367-6e45-459f-a27b-476b1d01c936";

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
        var lidActionBlock = FindLidActionBlock(output);
        if (lidActionBlock is null)
        {
            throw new InvalidOperationException("Unable to find LIDACTION power setting block for AC/DC lid action indexes.");
        }

        var ac = ParseHexIndex(lidActionBlock, "当前交流电源设置索引");
        var dc = ParseHexIndex(lidActionBlock, "当前直流电源设置索引");

        if (ac is null || dc is null)
        {
            throw new InvalidOperationException("Unable to parse AC/DC lid action indexes from LIDACTION power setting block.");
        }

        return new PowerSchemeState(schemeGuid, (LidAction)ac.Value, (LidAction)dc.Value);
    }

    private static string? FindLidActionBlock(string output)
    {
        foreach (Match blockStart in PowerSettingBlockStartRegex().Matches(output))
        {
            var nextBlockStart = PowerSettingBlockStartRegex().Match(output, blockStart.Index + blockStart.Length);
            var end = nextBlockStart.Success ? nextBlockStart.Index : output.Length;
            var block = output[blockStart.Index..end];

            if (IsLidActionBlock(block))
            {
                return block;
            }
        }

        return null;
    }

    private static bool IsLidActionBlock(string block)
    {
        return LidActionAliasRegex().IsMatch(block) ||
            block.Contains(LidActionSettingGuid, StringComparison.OrdinalIgnoreCase) ||
            block.Contains("盖子", StringComparison.OrdinalIgnoreCase) ||
            block.Contains("Lid", StringComparison.OrdinalIgnoreCase);
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

    [GeneratedRegex(@"电源设置\s+GUID:", RegexOptions.IgnoreCase)]
    private static partial Regex PowerSettingBlockStartRegex();

    [GeneratedRegex(@"GUID\s*别名:\s*LIDACTION", RegexOptions.IgnoreCase)]
    private static partial Regex LidActionAliasRegex();
}
