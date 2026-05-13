using Thinker.Models;
using Thinker.Services;
using Xunit;

namespace Thinker.Tests;

public sealed class PowerSettingsServiceTests
{
    [Fact]
    public async Task GetCurrentAsync_QueriesHiddenButtonSettingsForLidAction()
    {
        var runner = new RecordingPowerCfgRunner();
        var service = new PowerSettingsService(runner);

        var state = await service.GetCurrentAsync();

        Assert.Equal(LidAction.Sleep, state.AcAction);
        Assert.Equal(LidAction.Sleep, state.DcAction);
        Assert.Contains("/qh SCHEME_CURRENT SUB_BUTTONS", runner.Commands);
    }

    private sealed class RecordingPowerCfgRunner : IPowerCfgRunner
    {
        public List<string> Commands { get; } = [];

        public Task<string> RunAsync(string arguments, CancellationToken cancellationToken = default)
        {
            Commands.Add(arguments);

            return arguments switch
            {
                "/getactivescheme" => Task.FromResult("电源方案 GUID: 381b4222-f694-41f0-9685-ff5bb260df2e  (平衡)"),
                "/q SCHEME_CURRENT SUB_BUTTONS" => Task.FromResult("""
                    电源设置 GUID: 7648efa3-dd9c-4e3e-b566-50f929386280  (电源按钮操作)
                      GUID 别名: UIBUTTON_ACTION
                      当前交流电源设置索引: 0x00000000
                      当前直流电源设置索引: 0x00000000
                    """),
                "/qh SCHEME_CURRENT SUB_BUTTONS" => Task.FromResult("""
                    电源设置 GUID: 5ca83367-6e45-459f-a27b-476b1d01c936  (合上盖子操作)
                      GUID 别名: LIDACTION
                      当前交流电源设置索引: 0x00000001
                      当前直流电源设置索引: 0x00000001
                    """),
                _ => throw new InvalidOperationException($"Unexpected powercfg command: {arguments}")
            };
        }
    }
}
