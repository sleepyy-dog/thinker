using Thinker.Models;
using Thinker.Services;
using Xunit;

namespace Thinker.Tests;

public sealed class PowerSettingsParserTests
{
    [Fact]
    public void ParseActiveSchemeGuid_ReadsChinesePowercfgOutput()
    {
        const string output = "电源方案 GUID: 381b4222-f694-41f0-9685-ff5bb260df2e  (平衡)";

        var guid = PowerSettingsParser.ParseActiveSchemeGuid(output);

        Assert.Equal("381b4222-f694-41f0-9685-ff5bb260df2e", guid);
    }

    [Fact]
    public void ParseLidActions_ReadsAcAndDcIndexes()
    {
        const string output = """
        电源方案 GUID: 381b4222-f694-41f0-9685-ff5bb260df2e  (平衡)
          当前交流电源设置索引: 0x00000002
          当前直流电源设置索引: 0x00000001
        """;

        var state = PowerSettingsParser.ParseLidActions("381b4222-f694-41f0-9685-ff5bb260df2e", output);

        Assert.Equal(LidAction.Hibernate, state.AcAction);
        Assert.Equal(LidAction.Sleep, state.DcAction);
    }

    [Fact]
    public void ParseLidActions_ThrowsWhenIndexesMissing()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            PowerSettingsParser.ParseLidActions("scheme", "no indexes"));

        Assert.Contains("AC/DC", ex.Message);
    }
}
