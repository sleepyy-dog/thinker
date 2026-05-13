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
        电源设置 GUID: 5ca83367-6e45-459f-a27b-476b1d01c936  (盖子关闭操作)
          当前交流电源设置索引: 0x00000002
          当前直流电源设置索引: 0x00000001
        """;

        var state = PowerSettingsParser.ParseLidActions("381b4222-f694-41f0-9685-ff5bb260df2e", output);

        Assert.Equal(LidAction.Hibernate, state.AcAction);
        Assert.Equal(LidAction.Sleep, state.DcAction);
    }

    [Fact]
    public void ParseLidActions_ReadsIndexesFromLidActionBlockWhenOtherSettingsAppearFirst()
    {
        const string schemeGuid = "381b4222-f694-41f0-9685-ff5bb260df2e";
        const string output = """
        电源设置 GUID: 7648efa3-dd9c-4e3e-b566-50f929386280  (电源按钮操作)
          GUID 别名: UIBUTTON_ACTION
          当前交流电源设置索引: 0x00000000
          当前直流电源设置索引: 0x00000000

        电源设置 GUID: 5ca83367-6e45-459f-a27b-476b1d01c936  (盖子关闭操作)
          GUID 别名: LIDACTION
          当前交流电源设置索引: 0x00000002
          当前直流电源设置索引: 0x00000001
        """;

        var state = PowerSettingsParser.ParseLidActions(schemeGuid, output);

        Assert.Equal(schemeGuid, state.SchemeGuid);
        Assert.Equal(LidAction.Hibernate, state.AcAction);
        Assert.Equal(LidAction.Sleep, state.DcAction);
    }

    [Fact]
    public void ParseLidActions_IgnoresLidOpenWakeBlockBeforeLidAction()
    {
        const string schemeGuid = "381b4222-f694-41f0-9685-ff5bb260df2e";
        const string output = """
        电源设置 GUID: 99ff10e7-23b1-4c07-a9d1-5c3206d741b4  (打开盖子操作)
          GUID 别名: LIDOPENWAKE
          当前交流电源设置索引: 0x00000001
          当前直流电源设置索引: 0x00000001

        电源设置 GUID: 5ca83367-6e45-459f-a27b-476b1d01c936  (合盖操作)
          GUID 别名: LIDACTION
          当前交流电源设置索引: 0x00000002
          当前直流电源设置索引: 0x00000000
        """;

        var state = PowerSettingsParser.ParseLidActions(schemeGuid, output);

        Assert.Equal(LidAction.Hibernate, state.AcAction);
        Assert.Equal(LidAction.DoNothing, state.DcAction);
    }

    [Fact]
    public void ParseLidActions_ThrowsWhenIndexOutsideKnownLidActions()
    {
        const string output = """
        电源设置 GUID: 5ca83367-6e45-459f-a27b-476b1d01c936  (合盖操作)
          GUID 别名: LIDACTION
          当前交流电源设置索引: 0x00000004
          当前直流电源设置索引: 0x00000001
        """;

        var ex = Assert.Throws<InvalidOperationException>(() =>
            PowerSettingsParser.ParseLidActions("scheme", output));

        Assert.True(
            ex.Message.Contains("lid action", StringComparison.OrdinalIgnoreCase) ||
            ex.Message.Contains("LidAction", StringComparison.Ordinal) ||
            ex.Message.Contains("0..3", StringComparison.Ordinal),
            ex.Message);
    }

    [Fact]
    public void ParseLidActions_ThrowsWhenLidActionBlockMissingDcIndex()
    {
        const string output = """
        电源设置 GUID: 5ca83367-6e45-459f-a27b-476b1d01c936  (合盖操作)
          GUID 别名: LIDACTION
          当前交流电源设置索引: 0x00000001
        """;

        var ex = Assert.Throws<InvalidOperationException>(() =>
            PowerSettingsParser.ParseLidActions("scheme", output));

        Assert.Contains("AC/DC", ex.Message);
    }

    [Fact]
    public void ParseLidActions_ThrowsWhenOnlyLidOpenWakeExists()
    {
        const string output = """
        电源设置 GUID: 99ff10e7-23b1-4c07-a9d1-5c3206d741b4  (打开盖子操作)
          GUID 别名: LIDOPENWAKE
          当前交流电源设置索引: 0x00000001
          当前直流电源设置索引: 0x00000001
        """;

        var ex = Assert.Throws<InvalidOperationException>(() =>
            PowerSettingsParser.ParseLidActions("scheme", output));

        Assert.Contains("LIDACTION", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ParseLidActions_ThrowsWhenLidActionBlockMissingEvenIfOtherIndexesExist()
    {
        const string output = """
        电源设置 GUID: 7648efa3-dd9c-4e3e-b566-50f929386280  (电源按钮操作)
          GUID 别名: UIBUTTON_ACTION
          当前交流电源设置索引: 0x00000000
          当前直流电源设置索引: 0x00000000
        """;

        var ex = Assert.Throws<InvalidOperationException>(() =>
            PowerSettingsParser.ParseLidActions("scheme", output));

        Assert.True(
            ex.Message.Contains("LIDACTION", StringComparison.OrdinalIgnoreCase) ||
            ex.Message.Contains("AC/DC", StringComparison.OrdinalIgnoreCase),
            ex.Message);
    }

    [Fact]
    public void ParseLidActions_ThrowsWhenIndexesMissing()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            PowerSettingsParser.ParseLidActions("scheme", "no indexes"));

        Assert.Contains("AC/DC", ex.Message);
    }
}
