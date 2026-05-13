# Thinker Tray MVP Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build the first-stage Thinker Windows tray app that can switch lid-close behavior, restore prior settings, persist mode state, and expose a safe tray UI.

**Architecture:** The app is a .NET 8 WinForms tray process. Power setting access, persisted state, mode orchestration, startup registration, and tray UI are separate units so a future companion pet can subscribe to the same `ModeController` without duplicating `powercfg` logic.

**Tech Stack:** .NET 8 SDK, WinForms `NotifyIcon`, `System.Text.Json`, xUnit tests, Windows `powercfg.exe`, current-user registry `Run` key.

---

## Preconditions

Current machine state checked on 2026-05-13:

- `.NET 8` runtime is installed.
- `.NET SDK` is not installed, so `dotnet new`, `dotnet build`, and `dotnet test` will fail until the SDK exists.

Before implementation, install the .NET 8 SDK from Microsoft. Verification command:

```powershell
dotnet --list-sdks
```

Expected output includes a `8.0.x` SDK line.

---

## File Structure

Create this structure:

```text
Thinker/
  Thinker.sln
  src/
    Thinker.App/
      Thinker.App.csproj
      Program.cs
      TrayApplicationContext.cs
      Models/
        AppState.cs
        LidAction.cs
        ModeStatus.cs
        PowerSchemeState.cs
        RunMode.cs
      Services/
        IClock.cs
        IModeStatusSink.cs
        IPowerSettingsService.cs
        IStartupService.cs
        ModeController.cs
        PowerCfgRunner.cs
        PowerSettingsParser.cs
        PowerSettingsService.cs
        StateStore.cs
        StartupService.cs
        SystemClock.cs
        TrayIconFactory.cs
  tests/
    Thinker.Tests/
      Thinker.Tests.csproj
      ModeControllerTests.cs
      PowerSettingsParserTests.cs
      RestoreFallbackTests.cs
      StateStoreTests.cs
      TestDoubles.cs
```

Responsibilities:

- `Models/*`: enums and serializable state objects.
- `PowerSettingsParser`: pure parser for localized `powercfg` output.
- `PowerSettingsService`: command-facing Windows power setting service.
- `StateStore`: JSON persistence under `%LOCALAPPDATA%\Thinker\state.json`.
- `ModeController`: business rules for toggling, mode selection, timer expiry, startup, restore fallback.
- `TrayApplicationContext`: WinForms `NotifyIcon` wiring only.
- `StartupService`: current-user Run key management.
- `TrayIconFactory`: generated icons, avoiding checked-in binary icon files for MVP.

---

## Task 1: Scaffold Solution And Projects

**Files:**
- Create: `Thinker.sln`
- Create: `src/Thinker.App/Thinker.App.csproj`
- Create: `tests/Thinker.Tests/Thinker.Tests.csproj`
- Modify: `.gitignore`

- [ ] **Step 1: Verify SDK exists**

Run:

```powershell
dotnet --list-sdks
```

Expected: at least one `8.0.x` SDK. If none is listed, stop and install .NET 8 SDK before continuing.

- [ ] **Step 2: Create solution and projects**

Run:

```powershell
dotnet new sln -n Thinker
dotnet new winforms -n Thinker.App -o src/Thinker.App --framework net8.0-windows
dotnet new xunit -n Thinker.Tests -o tests/Thinker.Tests --framework net8.0
dotnet sln Thinker.sln add src/Thinker.App/Thinker.App.csproj
dotnet sln Thinker.sln add tests/Thinker.Tests/Thinker.Tests.csproj
dotnet add tests/Thinker.Tests/Thinker.Tests.csproj reference src/Thinker.App/Thinker.App.csproj
```

Expected: all commands exit `0`.

- [ ] **Step 3: Replace app project file**

Replace `src/Thinker.App/Thinker.App.csproj` with:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net8.0-windows</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <UseWindowsForms>true</UseWindowsForms>
    <AssemblyName>Thinker</AssemblyName>
    <RootNamespace>Thinker</RootNamespace>
  </PropertyGroup>
</Project>
```

- [ ] **Step 4: Replace test project file**

Replace `tests/Thinker.Tests/Thinker.Tests.csproj` with:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0-windows</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.11.1" />
    <PackageReference Include="xunit" Version="2.9.2" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.8.2" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\..\src\Thinker.App\Thinker.App.csproj" />
  </ItemGroup>
</Project>
```

- [ ] **Step 5: Update `.gitignore`**

Ensure `.gitignore` contains:

```gitignore
.superpowers/

# .NET build outputs
bin/
obj/

# Local IDE files
.vs/
.vscode/

# User-specific build files
*.user
```

- [ ] **Step 6: Build empty scaffold**

Run:

```powershell
dotnet build Thinker.sln
```

Expected: build succeeds.

- [ ] **Step 7: Commit scaffold**

Run:

```powershell
git add Thinker.sln src/Thinker.App/Thinker.App.csproj tests/Thinker.Tests/Thinker.Tests.csproj .gitignore
git commit -m "chore: scaffold Thinker solution"
```

---

## Task 2: Add Core Models

**Files:**
- Create: `src/Thinker.App/Models/LidAction.cs`
- Create: `src/Thinker.App/Models/RunMode.cs`
- Create: `src/Thinker.App/Models/ModeStatus.cs`
- Create: `src/Thinker.App/Models/PowerSchemeState.cs`
- Create: `src/Thinker.App/Models/AppState.cs`
- Test: `tests/Thinker.Tests/StateStoreTests.cs` in Task 4

- [ ] **Step 1: Create `LidAction.cs`**

```csharp
namespace Thinker.Models;

public enum LidAction
{
    DoNothing = 0,
    Sleep = 1,
    Hibernate = 2,
    ShutDown = 3
}
```

- [ ] **Step 2: Create `RunMode.cs`**

```csharp
namespace Thinker.Models;

public enum RunMode
{
    Timed30Minutes,
    Timed2Hours,
    Permanent
}
```

- [ ] **Step 3: Create `ModeStatus.cs`**

```csharp
namespace Thinker.Models;

public enum ModeStatus
{
    Normal,
    ActiveTimed,
    ActivePermanent,
    Error
}
```

- [ ] **Step 4: Create `PowerSchemeState.cs`**

```csharp
namespace Thinker.Models;

public sealed record PowerSchemeState(
    string SchemeGuid,
    LidAction AcAction,
    LidAction DcAction);
```

- [ ] **Step 5: Create `AppState.cs`**

```csharp
using System.Text.Json.Serialization;

namespace Thinker.Models;

public sealed class AppState
{
    public bool Active { get; set; }
    public RunMode ActiveMode { get; set; } = RunMode.Timed30Minutes;
    public RunMode LockedMode { get; set; } = RunMode.Timed30Minutes;
    public string SchemeGuid { get; set; } = "SCHEME_CURRENT";
    public LidAction? PreviousAcAction { get; set; }
    public LidAction? PreviousDcAction { get; set; }
    public DateTimeOffset? EnabledAt { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }
    public string? LastError { get; set; }

    [JsonIgnore]
    public ModeStatus Status =>
        !string.IsNullOrWhiteSpace(LastError)
            ? ModeStatus.Error
            : ActiveMode == RunMode.Permanent && Active
                ? ModeStatus.ActivePermanent
                : Active
                    ? ModeStatus.ActiveTimed
                    : ModeStatus.Normal;

    public static AppState Default() => new()
    {
        Active = false,
        ActiveMode = RunMode.Timed30Minutes,
        LockedMode = RunMode.Timed30Minutes,
        SchemeGuid = "SCHEME_CURRENT"
    };
}
```

- [ ] **Step 6: Build**

Run:

```powershell
dotnet build Thinker.sln
```

Expected: build succeeds.

- [ ] **Step 7: Commit models**

Run:

```powershell
git add src/Thinker.App/Models
git commit -m "feat: add core mode models"
```

---

## Task 3: Add Power Settings Parsing

**Files:**
- Create: `src/Thinker.App/Services/PowerSettingsParser.cs`
- Test: `tests/Thinker.Tests/PowerSettingsParserTests.cs`

- [ ] **Step 1: Write failing parser tests**

Create `tests/Thinker.Tests/PowerSettingsParserTests.cs`:

```csharp
using Thinker.Models;
using Thinker.Services;

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
```

- [ ] **Step 2: Run parser tests and verify failure**

Run:

```powershell
dotnet test tests/Thinker.Tests/Thinker.Tests.csproj --filter PowerSettingsParserTests
```

Expected: fail because `PowerSettingsParser` does not exist.

- [ ] **Step 3: Implement parser**

Create `src/Thinker.App/Services/PowerSettingsParser.cs`:

```csharp
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
```

- [ ] **Step 4: Run parser tests and verify pass**

Run:

```powershell
dotnet test tests/Thinker.Tests/Thinker.Tests.csproj --filter PowerSettingsParserTests
```

Expected: pass.

- [ ] **Step 5: Commit parser**

Run:

```powershell
git add src/Thinker.App/Services/PowerSettingsParser.cs tests/Thinker.Tests/PowerSettingsParserTests.cs
git commit -m "feat: parse powercfg lid settings"
```

---

## Task 4: Add State Store

**Files:**
- Create: `src/Thinker.App/Services/StateStore.cs`
- Test: `tests/Thinker.Tests/StateStoreTests.cs`

- [ ] **Step 1: Write failing state store tests**

Create `tests/Thinker.Tests/StateStoreTests.cs`:

```csharp
using Thinker.Models;
using Thinker.Services;

namespace Thinker.Tests;

public sealed class StateStoreTests
{
    [Fact]
    public async Task LoadAsync_ReturnsDefaultWhenFileMissing()
    {
        using var dir = TempDir.Create();
        var store = new StateStore(Path.Combine(dir.Path, "state.json"));

        var state = await store.LoadAsync();

        Assert.False(state.Active);
        Assert.Equal(RunMode.Timed30Minutes, state.LockedMode);
    }

    [Fact]
    public async Task SaveAndLoadAsync_RoundTripsState()
    {
        using var dir = TempDir.Create();
        var store = new StateStore(Path.Combine(dir.Path, "state.json"));
        var expected = new AppState
        {
            Active = true,
            ActiveMode = RunMode.Timed2Hours,
            LockedMode = RunMode.Permanent,
            SchemeGuid = "abc",
            PreviousAcAction = LidAction.Sleep,
            PreviousDcAction = LidAction.Hibernate,
            EnabledAt = DateTimeOffset.Parse("2026-05-13T09:00:00+08:00"),
            ExpiresAt = DateTimeOffset.Parse("2026-05-13T11:00:00+08:00")
        };

        await store.SaveAsync(expected);
        var actual = await store.LoadAsync();

        Assert.True(actual.Active);
        Assert.Equal(RunMode.Timed2Hours, actual.ActiveMode);
        Assert.Equal(RunMode.Permanent, actual.LockedMode);
        Assert.Equal(LidAction.Sleep, actual.PreviousAcAction);
        Assert.Equal(LidAction.Hibernate, actual.PreviousDcAction);
    }

    [Fact]
    public async Task LoadAsync_RenamesCorruptFileAndReturnsErrorState()
    {
        using var dir = TempDir.Create();
        var statePath = Path.Combine(dir.Path, "state.json");
        await File.WriteAllTextAsync(statePath, "{ not json");
        var store = new StateStore(statePath);

        var state = await store.LoadAsync();

        Assert.False(state.Active);
        Assert.Equal(ModeStatus.Error, state.Status);
        Assert.True(Directory.GetFiles(dir.Path, "state.json.corrupt.*").Length == 1);
    }
}
```

- [ ] **Step 2: Add temp directory test helper**

Create `tests/Thinker.Tests/TestDoubles.cs` with this initial content:

```csharp
using Thinker.Models;
using Thinker.Services;

namespace Thinker.Tests;

public sealed class TempDir : IDisposable
{
    public string Path { get; } = System.IO.Path.Combine(
        System.IO.Path.GetTempPath(),
        "Thinker.Tests",
        Guid.NewGuid().ToString("N"));

    private TempDir()
    {
        Directory.CreateDirectory(Path);
    }

    public static TempDir Create() => new();

    public void Dispose()
    {
        if (Directory.Exists(Path))
        {
            Directory.Delete(Path, recursive: true);
        }
    }
}
```

- [ ] **Step 3: Run state store tests and verify failure**

Run:

```powershell
dotnet test tests/Thinker.Tests/Thinker.Tests.csproj --filter StateStoreTests
```

Expected: fail because `StateStore` does not exist.

- [ ] **Step 4: Implement `StateStore`**

Create `src/Thinker.App/Services/StateStore.cs`:

```csharp
using System.Text.Json;
using Thinker.Models;

namespace Thinker.Services;

public sealed class StateStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    public StateStore(string? statePath = null)
    {
        StatePath = statePath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Thinker",
            "state.json");
    }

    public string StatePath { get; }

    public async Task<AppState> LoadAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(StatePath))
        {
            return AppState.Default();
        }

        try
        {
            await using var stream = File.OpenRead(StatePath);
            return await JsonSerializer.DeserializeAsync<AppState>(stream, JsonOptions, cancellationToken)
                   ?? AppState.Default();
        }
        catch (JsonException ex)
        {
            RenameCorruptFile();
            var state = AppState.Default();
            state.LastError = "State file is corrupt: " + ex.Message;
            return state;
        }
    }

    public async Task SaveAsync(AppState state, CancellationToken cancellationToken = default)
    {
        var directory = Path.GetDirectoryName(StatePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var tempPath = StatePath + ".tmp";
        await using (var stream = File.Create(tempPath))
        {
            await JsonSerializer.SerializeAsync(stream, state, JsonOptions, cancellationToken);
        }

        File.Copy(tempPath, StatePath, overwrite: true);
        File.Delete(tempPath);
    }

    public Task ClearAsync(CancellationToken cancellationToken = default)
    {
        if (File.Exists(StatePath))
        {
            File.Delete(StatePath);
        }

        return Task.CompletedTask;
    }

    private void RenameCorruptFile()
    {
        var corruptPath = StatePath + ".corrupt." + DateTimeOffset.Now.ToString("yyyyMMddHHmmss");
        File.Move(StatePath, corruptPath, overwrite: true);
    }
}
```

- [ ] **Step 5: Run state store tests and verify pass**

Run:

```powershell
dotnet test tests/Thinker.Tests/Thinker.Tests.csproj --filter StateStoreTests
```

Expected: pass.

- [ ] **Step 6: Commit state store**

Run:

```powershell
git add src/Thinker.App/Services/StateStore.cs tests/Thinker.Tests/StateStoreTests.cs tests/Thinker.Tests/TestDoubles.cs
git commit -m "feat: persist Thinker state"
```

---

## Task 5: Add Power Settings Service

**Files:**
- Create: `src/Thinker.App/Services/IPowerSettingsService.cs`
- Create: `src/Thinker.App/Services/PowerCfgRunner.cs`
- Create: `src/Thinker.App/Services/PowerSettingsService.cs`
- Test: parser tests from Task 3 cover parsing; service is manually verified against `powercfg`.

- [ ] **Step 1: Create service interface**

Create `src/Thinker.App/Services/IPowerSettingsService.cs`:

```csharp
using Thinker.Models;

namespace Thinker.Services;

public interface IPowerSettingsService
{
    Task<PowerSchemeState> GetCurrentAsync(CancellationToken cancellationToken = default);
    Task SetLidActionsAsync(LidAction acAction, LidAction dcAction, CancellationToken cancellationToken = default);
    Task ApplyCurrentSchemeAsync(CancellationToken cancellationToken = default);
}
```

- [ ] **Step 2: Create `PowerCfgRunner`**

Create `src/Thinker.App/Services/PowerCfgRunner.cs`:

```csharp
using System.Diagnostics;
using System.Text;

namespace Thinker.Services;

public sealed class PowerCfgRunner
{
    public async Task<string> RunAsync(string arguments, CancellationToken cancellationToken = default)
    {
        using var process = new Process();
        process.StartInfo = new ProcessStartInfo
        {
            FileName = "powercfg.exe",
            Arguments = arguments,
            CreateNoWindow = true,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.Default,
            StandardErrorEncoding = Encoding.Default
        };

        process.Start();
        var stdout = await process.StandardOutput.ReadToEndAsync(cancellationToken);
        var stderr = await process.StandardError.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"powercfg {arguments} failed with exit code {process.ExitCode}: {stderr.Trim()}");
        }

        return stdout;
    }
}
```

- [ ] **Step 3: Create `PowerSettingsService`**

Create `src/Thinker.App/Services/PowerSettingsService.cs`:

```csharp
using Thinker.Models;

namespace Thinker.Services;

public sealed class PowerSettingsService(PowerCfgRunner runner) : IPowerSettingsService
{
    public async Task<PowerSchemeState> GetCurrentAsync(CancellationToken cancellationToken = default)
    {
        var schemeOutput = await runner.RunAsync("/getactivescheme", cancellationToken);
        var schemeGuid = PowerSettingsParser.ParseActiveSchemeGuid(schemeOutput);

        var queryOutput = await runner.RunAsync("/q SCHEME_CURRENT SUB_BUTTONS", cancellationToken);
        return PowerSettingsParser.ParseLidActions(schemeGuid, queryOutput);
    }

    public async Task SetLidActionsAsync(
        LidAction acAction,
        LidAction dcAction,
        CancellationToken cancellationToken = default)
    {
        await runner.RunAsync($"/setacvalueindex SCHEME_CURRENT SUB_BUTTONS LIDACTION {(int)acAction}", cancellationToken);
        await runner.RunAsync($"/setdcvalueindex SCHEME_CURRENT SUB_BUTTONS LIDACTION {(int)dcAction}", cancellationToken);
    }

    public Task ApplyCurrentSchemeAsync(CancellationToken cancellationToken = default)
    {
        return runner.RunAsync("/setactive SCHEME_CURRENT", cancellationToken);
    }
}
```

- [ ] **Step 4: Build**

Run:

```powershell
dotnet build Thinker.sln
```

Expected: build succeeds.

- [ ] **Step 5: Manually verify read-only powercfg commands**

Run:

```powershell
powercfg /getactivescheme
powercfg /q SCHEME_CURRENT SUB_BUTTONS
```

Expected: both commands exit `0`; `/q` output includes current AC/DC setting indexes on a device that exposes lid settings. If the device does not expose `LIDACTION`, note the limitation before continuing UI testing.

- [ ] **Step 6: Commit service**

Run:

```powershell
git add src/Thinker.App/Services/IPowerSettingsService.cs src/Thinker.App/Services/PowerCfgRunner.cs src/Thinker.App/Services/PowerSettingsService.cs
git commit -m "feat: add power settings service"
```

---

## Task 6: Add Mode Controller With TDD

**Files:**
- Create: `src/Thinker.App/Services/IClock.cs`
- Create: `src/Thinker.App/Services/SystemClock.cs`
- Create: `src/Thinker.App/Services/IModeStatusSink.cs`
- Create: `src/Thinker.App/Services/ModeController.cs`
- Modify: `tests/Thinker.Tests/TestDoubles.cs`
- Test: `tests/Thinker.Tests/ModeControllerTests.cs`
- Test: `tests/Thinker.Tests/RestoreFallbackTests.cs`

- [ ] **Step 1: Add test doubles**

Append to `tests/Thinker.Tests/TestDoubles.cs`:

```csharp
public sealed class FakeClock(DateTimeOffset now) : IClock
{
    public DateTimeOffset UtcNow { get; private set; } = now;

    public void Advance(TimeSpan value) => UtcNow += value;
}

public sealed class FakePowerSettingsService : IPowerSettingsService
{
    public PowerSchemeState Current { get; set; } = new("scheme", LidAction.Sleep, LidAction.Sleep);
    public List<(LidAction Ac, LidAction Dc)> SetCalls { get; } = [];
    public bool FailRestoreOriginal { get; set; }
    public bool FailSleepFallback { get; set; }

    public Task<PowerSchemeState> GetCurrentAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Current);
    }

    public Task SetLidActionsAsync(LidAction acAction, LidAction dcAction, CancellationToken cancellationToken = default)
    {
        if (FailRestoreOriginal && (acAction, dcAction) != (LidAction.Sleep, LidAction.Sleep))
        {
            throw new InvalidOperationException("restore original failed");
        }

        if (FailSleepFallback && (acAction, dcAction) == (LidAction.Sleep, LidAction.Sleep))
        {
            throw new InvalidOperationException("sleep fallback failed");
        }

        SetCalls.Add((acAction, dcAction));
        Current = Current with { AcAction = acAction, DcAction = dcAction };
        return Task.CompletedTask;
    }

    public Task ApplyCurrentSchemeAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}
```

- [ ] **Step 2: Write failing mode controller tests**

Create `tests/Thinker.Tests/ModeControllerTests.cs`:

```csharp
using Thinker.Models;
using Thinker.Services;

namespace Thinker.Tests;

public sealed class ModeControllerTests
{
    [Fact]
    public async Task ToggleAsync_FromNormal_UsesDefaultThirtyMinuteLockedMode()
    {
        using var dir = TempDir.Create();
        var store = new StateStore(Path.Combine(dir.Path, "state.json"));
        var power = new FakePowerSettingsService
        {
            Current = new PowerSchemeState("scheme", LidAction.Sleep, LidAction.Hibernate)
        };
        var clock = new FakeClock(DateTimeOffset.Parse("2026-05-13T01:00:00Z"));
        var controller = new ModeController(power, store, clock);

        await controller.ToggleAsync();
        var state = await store.LoadAsync();

        Assert.True(state.Active);
        Assert.Equal(RunMode.Timed30Minutes, state.ActiveMode);
        Assert.Equal(RunMode.Timed30Minutes, state.LockedMode);
        Assert.Equal(LidAction.Sleep, state.PreviousAcAction);
        Assert.Equal(LidAction.Hibernate, state.PreviousDcAction);
        Assert.Equal(DateTimeOffset.Parse("2026-05-13T01:30:00Z"), state.ExpiresAt);
        Assert.Equal((LidAction.DoNothing, LidAction.DoNothing), power.SetCalls.Single());
    }

    [Fact]
    public async Task SelectModeAsync_WhileActive_RefreshesCurrentModeAndKeepsOriginalActions()
    {
        using var dir = TempDir.Create();
        var store = new StateStore(Path.Combine(dir.Path, "state.json"));
        var power = new FakePowerSettingsService
        {
            Current = new PowerSchemeState("scheme", LidAction.Sleep, LidAction.Hibernate)
        };
        var clock = new FakeClock(DateTimeOffset.Parse("2026-05-13T01:00:00Z"));
        var controller = new ModeController(power, store, clock);

        await controller.ToggleAsync();
        clock.Advance(TimeSpan.FromMinutes(5));
        await controller.SelectModeAsync(RunMode.Timed2Hours);
        var state = await store.LoadAsync();

        Assert.True(state.Active);
        Assert.Equal(RunMode.Timed2Hours, state.ActiveMode);
        Assert.Equal(RunMode.Timed2Hours, state.LockedMode);
        Assert.Equal(LidAction.Sleep, state.PreviousAcAction);
        Assert.Equal(LidAction.Hibernate, state.PreviousDcAction);
        Assert.Equal(DateTimeOffset.Parse("2026-05-13T03:05:00Z"), state.ExpiresAt);
    }

    [Fact]
    public async Task CheckExpiryAsync_RestoresWhenTimedModeExpired()
    {
        using var dir = TempDir.Create();
        var store = new StateStore(Path.Combine(dir.Path, "state.json"));
        var power = new FakePowerSettingsService();
        var clock = new FakeClock(DateTimeOffset.Parse("2026-05-13T01:00:00Z"));
        var controller = new ModeController(power, store, clock);

        await controller.ToggleAsync();
        clock.Advance(TimeSpan.FromMinutes(31));
        await controller.CheckExpiryAsync();
        var state = await store.LoadAsync();

        Assert.False(state.Active);
        Assert.Equal((LidAction.Sleep, LidAction.Sleep), power.SetCalls.Last());
    }
}
```

- [ ] **Step 3: Write failing restore fallback tests**

Create `tests/Thinker.Tests/RestoreFallbackTests.cs`:

```csharp
using Thinker.Models;
using Thinker.Services;

namespace Thinker.Tests;

public sealed class RestoreFallbackTests
{
    [Fact]
    public async Task RestoreAsync_FallsBackToSleepWhenOriginalRestoreFails()
    {
        using var dir = TempDir.Create();
        var store = new StateStore(Path.Combine(dir.Path, "state.json"));
        await store.SaveAsync(new AppState
        {
            Active = true,
            ActiveMode = RunMode.Permanent,
            LockedMode = RunMode.Permanent,
            SchemeGuid = "scheme",
            PreviousAcAction = LidAction.Hibernate,
            PreviousDcAction = LidAction.Hibernate
        });
        var power = new FakePowerSettingsService { FailRestoreOriginal = true };
        var controller = new ModeController(power, store, new FakeClock(DateTimeOffset.UtcNow));

        await controller.RestoreAsync();

        Assert.Contains(power.SetCalls, call => call == (LidAction.Sleep, LidAction.Sleep));
        Assert.False((await store.LoadAsync()).Active);
    }

    [Fact]
    public async Task RestoreAsync_RecordsErrorWhenSleepFallbackFails()
    {
        using var dir = TempDir.Create();
        var store = new StateStore(Path.Combine(dir.Path, "state.json"));
        await store.SaveAsync(new AppState
        {
            Active = true,
            ActiveMode = RunMode.Permanent,
            LockedMode = RunMode.Permanent,
            SchemeGuid = "scheme",
            PreviousAcAction = LidAction.Hibernate,
            PreviousDcAction = LidAction.Hibernate
        });
        var power = new FakePowerSettingsService
        {
            FailRestoreOriginal = true,
            FailSleepFallback = true
        };
        var controller = new ModeController(power, store, new FakeClock(DateTimeOffset.UtcNow));

        await controller.RestoreAsync();

        var state = await store.LoadAsync();
        Assert.Equal(ModeStatus.Error, state.Status);
        Assert.Contains("sleep fallback failed", state.LastError);
    }
}
```

- [ ] **Step 4: Run controller tests and verify failure**

Run:

```powershell
dotnet test tests/Thinker.Tests/Thinker.Tests.csproj --filter "ModeControllerTests|RestoreFallbackTests"
```

Expected: fail because `ModeController`, `IClock`, and `IModeStatusSink` do not exist.

- [ ] **Step 5: Create clock and status sink interfaces**

Create `src/Thinker.App/Services/IClock.cs`:

```csharp
namespace Thinker.Services;

public interface IClock
{
    DateTimeOffset UtcNow { get; }
}
```

Create `src/Thinker.App/Services/SystemClock.cs`:

```csharp
namespace Thinker.Services;

public sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
```

Create `src/Thinker.App/Services/IModeStatusSink.cs`:

```csharp
using Thinker.Models;

namespace Thinker.Services;

public interface IModeStatusSink
{
    Task OnStateChangedAsync(AppState state, CancellationToken cancellationToken = default);
}
```

- [ ] **Step 6: Implement `ModeController`**

Create `src/Thinker.App/Services/ModeController.cs`:

```csharp
using Thinker.Models;

namespace Thinker.Services;

public sealed class ModeController(
    IPowerSettingsService powerSettings,
    StateStore stateStore,
    IClock clock,
    IModeStatusSink? statusSink = null)
{
    public async Task<AppState> LoadAsync(CancellationToken cancellationToken = default)
    {
        var state = await stateStore.LoadAsync(cancellationToken);
        if (state.Active && state.ExpiresAt is not null && state.ExpiresAt <= clock.UtcNow)
        {
            return await RestoreAsync(cancellationToken);
        }

        await NotifyAsync(state, cancellationToken);
        return state;
    }

    public async Task<AppState> ToggleAsync(CancellationToken cancellationToken = default)
    {
        var state = await stateStore.LoadAsync(cancellationToken);
        return state.Active
            ? await RestoreAsync(cancellationToken)
            : await EnableAsync(state.LockedMode, cancellationToken);
    }

    public async Task<AppState> SelectModeAsync(RunMode mode, CancellationToken cancellationToken = default)
    {
        var state = await stateStore.LoadAsync(cancellationToken);
        state.LockedMode = mode;
        if (state.Active)
        {
            state.ActiveMode = mode;
            state.ExpiresAt = CalculateExpiry(mode);
        }

        await stateStore.SaveAsync(state, cancellationToken);
        await NotifyAsync(state, cancellationToken);
        return state;
    }

    public async Task<AppState> EnableAsync(RunMode mode, CancellationToken cancellationToken = default)
    {
        var existing = await stateStore.LoadAsync(cancellationToken);
        var current = await powerSettings.GetCurrentAsync(cancellationToken);

        var state = existing.Active
            ? existing
            : new AppState
            {
                PreviousAcAction = current.AcAction,
                PreviousDcAction = current.DcAction,
                SchemeGuid = current.SchemeGuid
            };

        state.Active = true;
        state.ActiveMode = mode;
        state.LockedMode = mode;
        state.EnabledAt = clock.UtcNow;
        state.ExpiresAt = CalculateExpiry(mode);
        state.LastError = null;

        await powerSettings.SetLidActionsAsync(LidAction.DoNothing, LidAction.DoNothing, cancellationToken);
        await powerSettings.ApplyCurrentSchemeAsync(cancellationToken);
        await stateStore.SaveAsync(state, cancellationToken);
        await NotifyAsync(state, cancellationToken);
        return state;
    }

    public async Task<AppState> RestoreAsync(CancellationToken cancellationToken = default)
    {
        var state = await stateStore.LoadAsync(cancellationToken);
        var targetAc = state.PreviousAcAction ?? LidAction.Sleep;
        var targetDc = state.PreviousDcAction ?? LidAction.Sleep;

        try
        {
            await powerSettings.SetLidActionsAsync(targetAc, targetDc, cancellationToken);
            await powerSettings.ApplyCurrentSchemeAsync(cancellationToken);
            state.Active = false;
            state.ActiveMode = state.LockedMode;
            state.PreviousAcAction = null;
            state.PreviousDcAction = null;
            state.EnabledAt = null;
            state.ExpiresAt = null;
            state.LastError = null;
        }
        catch (Exception originalRestoreError)
        {
            try
            {
                await powerSettings.SetLidActionsAsync(LidAction.Sleep, LidAction.Sleep, cancellationToken);
                await powerSettings.ApplyCurrentSchemeAsync(cancellationToken);
                state.Active = false;
                state.ActiveMode = state.LockedMode;
                state.PreviousAcAction = null;
                state.PreviousDcAction = null;
                state.EnabledAt = null;
                state.ExpiresAt = null;
                state.LastError = null;
            }
            catch (Exception fallbackError)
            {
                state.LastError = originalRestoreError.Message + " | " + fallbackError.Message;
            }
        }

        await stateStore.SaveAsync(state, cancellationToken);
        await NotifyAsync(state, cancellationToken);
        return state;
    }

    public async Task<AppState> CheckExpiryAsync(CancellationToken cancellationToken = default)
    {
        var state = await stateStore.LoadAsync(cancellationToken);
        if (state.Active && state.ExpiresAt is not null && state.ExpiresAt <= clock.UtcNow)
        {
            return await RestoreAsync(cancellationToken);
        }

        return state;
    }

    private DateTimeOffset? CalculateExpiry(RunMode mode) => mode switch
    {
        RunMode.Timed30Minutes => clock.UtcNow.AddMinutes(30),
        RunMode.Timed2Hours => clock.UtcNow.AddHours(2),
        RunMode.Permanent => null,
        _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)
    };

    private Task NotifyAsync(AppState state, CancellationToken cancellationToken)
    {
        return statusSink?.OnStateChangedAsync(state, cancellationToken) ?? Task.CompletedTask;
    }
}
```

- [ ] **Step 7: Run controller tests and verify pass**

Run:

```powershell
dotnet test tests/Thinker.Tests/Thinker.Tests.csproj --filter "ModeControllerTests|RestoreFallbackTests"
```

Expected: pass.

- [ ] **Step 8: Run all tests**

Run:

```powershell
dotnet test Thinker.sln
```

Expected: all tests pass.

- [ ] **Step 9: Commit mode controller**

Run:

```powershell
git add src/Thinker.App/Services/IClock.cs src/Thinker.App/Services/SystemClock.cs src/Thinker.App/Services/IModeStatusSink.cs src/Thinker.App/Services/ModeController.cs tests/Thinker.Tests/ModeControllerTests.cs tests/Thinker.Tests/RestoreFallbackTests.cs tests/Thinker.Tests/TestDoubles.cs
git commit -m "feat: orchestrate lid mode transitions"
```

---

## Task 7: Add Startup Service

**Files:**
- Create: `src/Thinker.App/Services/IStartupService.cs`
- Create: `src/Thinker.App/Services/StartupService.cs`

- [ ] **Step 1: Create startup interface**

Create `src/Thinker.App/Services/IStartupService.cs`:

```csharp
namespace Thinker.Services;

public interface IStartupService
{
    bool IsEnabled();
    void SetEnabled(bool enabled);
}
```

- [ ] **Step 2: Implement registry-backed startup service**

Create `src/Thinker.App/Services/StartupService.cs`:

```csharp
using Microsoft.Win32;

namespace Thinker.Services;

public sealed class StartupService(string executablePath) : IStartupService
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "Thinker";

    public bool IsEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: false);
        return string.Equals(key?.GetValue(ValueName) as string, Quote(executablePath), StringComparison.OrdinalIgnoreCase);
    }

    public void SetEnabled(bool enabled)
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true)
                      ?? Registry.CurrentUser.CreateSubKey(RunKeyPath, writable: true);

        if (enabled)
        {
            key.SetValue(ValueName, Quote(executablePath), RegistryValueKind.String);
        }
        else
        {
            key.DeleteValue(ValueName, throwOnMissingValue: false);
        }
    }

    private static string Quote(string path) => "\"" + path + "\"";
}
```

- [ ] **Step 3: Build**

Run:

```powershell
dotnet build Thinker.sln
```

Expected: build succeeds.

- [ ] **Step 4: Commit startup service**

Run:

```powershell
git add src/Thinker.App/Services/IStartupService.cs src/Thinker.App/Services/StartupService.cs
git commit -m "feat: manage startup registration"
```

---

## Task 8: Add Tray UI

**Files:**
- Modify: `src/Thinker.App/Program.cs`
- Create: `src/Thinker.App/TrayApplicationContext.cs`
- Create: `src/Thinker.App/Services/TrayIconFactory.cs`

- [ ] **Step 1: Replace `Program.cs`**

```csharp
using Thinker.Services;

namespace Thinker;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();

        var runner = new PowerCfgRunner();
        var powerSettings = new PowerSettingsService(runner);
        var stateStore = new StateStore();
        var clock = new SystemClock();
        var startupService = new StartupService(Environment.ProcessPath ?? Application.ExecutablePath);
        using var context = new TrayApplicationContext(powerSettings, stateStore, clock, startupService);

        Application.Run(context);
    }
}
```

- [ ] **Step 2: Create tray icon factory**

Create `src/Thinker.App/Services/TrayIconFactory.cs`:

```csharp
using System.Drawing;
using Thinker.Models;

namespace Thinker.Services;

public static class TrayIconFactory
{
    public static Icon Create(ModeStatus status)
    {
        var color = status switch
        {
            ModeStatus.Normal => Color.Gray,
            ModeStatus.ActiveTimed => Color.Goldenrod,
            ModeStatus.ActivePermanent => Color.SeaGreen,
            ModeStatus.Error => Color.Firebrick,
            _ => Color.Gray
        };

        using var bitmap = new Bitmap(32, 32);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.Clear(Color.Transparent);
        using var brush = new SolidBrush(color);
        graphics.FillEllipse(brush, 4, 4, 24, 24);
        using var pen = new Pen(Color.White, 3);
        graphics.DrawEllipse(pen, 4, 4, 24, 24);
        return Icon.FromHandle(bitmap.GetHicon());
    }
}
```

- [ ] **Step 3: Create tray application context**

Create `src/Thinker.App/TrayApplicationContext.cs`:

```csharp
using Thinker.Models;
using Thinker.Services;

namespace Thinker;

public sealed class TrayApplicationContext : ApplicationContext, IModeStatusSink
{
    private readonly NotifyIcon notifyIcon;
    private readonly ModeController controller;
    private readonly IStartupService startupService;
    private readonly System.Windows.Forms.Timer timer;
    private AppState currentState = AppState.Default();

    public TrayApplicationContext(
        IPowerSettingsService powerSettings,
        StateStore stateStore,
        IClock clock,
        IStartupService startupService)
    {
        this.startupService = startupService;
        controller = new ModeController(powerSettings, stateStore, clock, this);
        notifyIcon = new NotifyIcon
        {
            Visible = true,
            Text = "Thinker",
            ContextMenuStrip = new ContextMenuStrip()
        };
        notifyIcon.MouseUp += NotifyIconOnMouseUp;

        timer = new System.Windows.Forms.Timer { Interval = 30_000 };
        timer.Tick += async (_, _) => await RunUiAsync(() => controller.CheckExpiryAsync());
        timer.Start();

        _ = RunUiAsync(() => controller.LoadAsync());
    }

    public Task OnStateChangedAsync(AppState state, CancellationToken cancellationToken = default)
    {
        currentState = state;
        RefreshTray();
        return Task.CompletedTask;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            timer.Dispose();
            notifyIcon.Dispose();
        }

        base.Dispose(disposing);
    }

    private async void NotifyIconOnMouseUp(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            await RunUiAsync(() => controller.ToggleAsync());
        }
        else if (e.Button == MouseButtons.Right)
        {
            RefreshMenu();
        }
    }

    private void RefreshTray()
    {
        var oldIcon = notifyIcon.Icon;
        notifyIcon.Icon = TrayIconFactory.Create(currentState.Status);
        oldIcon?.Dispose();
        notifyIcon.Text = BuildTooltip(currentState);
        RefreshMenu();
    }

    private void RefreshMenu()
    {
        var menu = notifyIcon.ContextMenuStrip!;
        menu.Items.Clear();
        menu.Items.Add(BuildDisabledItem(BuildStatusText(currentState)));
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("开启合盖继续运行", null, async (_, _) => await RunUiAsync(() => controller.EnableAsync(currentState.LockedMode)));
        menu.Items.Add("恢复正常模式", null, async (_, _) => await RunUiAsync(() => controller.RestoreAsync()));
        menu.Items.Add(new ToolStripSeparator());
        AddModeItem(menu, "30 分钟", RunMode.Timed30Minutes);
        AddModeItem(menu, "2 小时", RunMode.Timed2Hours);
        AddModeItem(menu, "永久", RunMode.Permanent);
        menu.Items.Add(new ToolStripSeparator());
        var startupItem = new ToolStripMenuItem("开机自启动")
        {
            Checked = startupService.IsEnabled(),
            CheckOnClick = true
        };
        startupItem.Click += (_, _) => startupService.SetEnabled(startupItem.Checked);
        menu.Items.Add(startupItem);
        menu.Items.Add("退出", null, async (_, _) => await ExitAsync());
    }

    private void AddModeItem(ContextMenuStrip menu, string text, RunMode mode)
    {
        var item = new ToolStripMenuItem(text)
        {
            Checked = currentState.LockedMode == mode
        };
        item.Click += async (_, _) => await RunUiAsync(() => controller.SelectModeAsync(mode));
        menu.Items.Add(item);
    }

    private static ToolStripMenuItem BuildDisabledItem(string text) => new(text) { Enabled = false };

    private static string BuildTooltip(AppState state)
    {
        var text = BuildStatusText(state);
        return text.Length <= 63 ? text : text[..63];
    }

    private static string BuildStatusText(AppState state)
    {
        if (state.Status == ModeStatus.Error)
        {
            return "Thinker：错误 - " + state.LastError;
        }

        if (!state.Active)
        {
            return $"Thinker：正常模式 · 锁定 {FormatMode(state.LockedMode)}";
        }

        var suffix = state.ExpiresAt is null
            ? "永久"
            : "到期 " + state.ExpiresAt.Value.ToLocalTime().ToString("HH:mm");
        return $"Thinker：合盖继续运行 · {suffix} · 锁定 {FormatMode(state.LockedMode)}";
    }

    private static string FormatMode(RunMode mode) => mode switch
    {
        RunMode.Timed30Minutes => "30 分钟",
        RunMode.Timed2Hours => "2 小时",
        RunMode.Permanent => "永久",
        _ => mode.ToString()
    };

    private async Task RunUiAsync(Func<Task<AppState>> action)
    {
        try
        {
            await action();
        }
        catch (Exception ex)
        {
            currentState.LastError = ex.Message;
            RefreshTray();
            notifyIcon.ShowBalloonTip(5000, "Thinker", ex.Message, ToolTipIcon.Error);
        }
    }

    private async Task ExitAsync()
    {
        if (currentState.Active)
        {
            await RunUiAsync(() => controller.RestoreAsync());
        }

        notifyIcon.Visible = false;
        ExitThread();
    }
}
```

- [ ] **Step 4: Build**

Run:

```powershell
dotnet build Thinker.sln
```

Expected: build succeeds.

- [ ] **Step 5: Commit tray UI**

Run:

```powershell
git add src/Thinker.App/Program.cs src/Thinker.App/TrayApplicationContext.cs src/Thinker.App/Services/TrayIconFactory.cs
git commit -m "feat: add tray app shell"
```

---

## Task 9: End-To-End Verification

**Files:**
- Modify only if verification exposes defects.

- [ ] **Step 1: Run all automated tests**

Run:

```powershell
dotnet test Thinker.sln
```

Expected: all tests pass.

- [ ] **Step 2: Build release**

Run:

```powershell
dotnet publish src/Thinker.App/Thinker.App.csproj -c Release -r win-x64 --self-contained false
```

Expected: publish succeeds and creates `src/Thinker.App/bin/Release/net8.0-windows/win-x64/publish/Thinker.exe`.

- [ ] **Step 3: Read current power settings before manual test**

Run:

```powershell
powercfg /q SCHEME_CURRENT SUB_BUTTONS
```

Record current AC/DC indexes before launching Thinker.

- [ ] **Step 4: Launch app manually**

Run:

```powershell
.\src\Thinker.App\bin\Release\net8.0-windows\win-x64\publish\Thinker.exe
```

Expected:

- Tray icon appears.
- Tooltip says normal mode and locked `30 分钟`.
- Right-click menu shows current status, open/restore commands, mode choices, startup toggle, exit.

- [ ] **Step 5: Verify left-click enable**

Left-click tray icon, then run:

```powershell
powercfg /q SCHEME_CURRENT SUB_BUTTONS
```

Expected: AC/DC `LIDACTION` indexes are `0x00000000` on systems where lid action is exposed.

- [ ] **Step 6: Verify restore**

Left-click tray icon again, then run:

```powershell
powercfg /q SCHEME_CURRENT SUB_BUTTONS
```

Expected: AC/DC indexes return to recorded original values, or `Sleep` if original values were unavailable.

- [ ] **Step 7: Verify mode menu**

Right-click tray icon, select `2 小时`, then left-click tray icon.

Expected:

- Tooltip/menu show active 2-hour mode.
- State file under `%LOCALAPPDATA%\Thinker\state.json` has `"lockedMode": "Timed2Hours"`.

- [ ] **Step 8: Verify startup toggle**

Right-click tray icon, toggle `开机自启动`, then run:

```powershell
reg query HKCU\Software\Microsoft\Windows\CurrentVersion\Run /v Thinker
```

Expected: enabled state creates `Thinker` value; disabled state removes it.

- [ ] **Step 9: Commit verification fixes if needed**

If defects were found and fixed:

```powershell
git add src tests
git commit -m "fix: address tray MVP verification issues"
```

---

## Self-Review

Spec coverage:

- Tray MVP: covered by Tasks 1, 8, 9.
- AC/DC `LIDACTION` modification: covered by Tasks 3, 5, 6, 9.
- 30 minutes / 2 hours / permanent locked modes: covered by Tasks 2, 6, 8.
- Default locked mode `30 分钟`: covered by Tasks 2 and 6.
- Restore original values with `Sleep` fallback: covered by Task 6.
- Startup registration: covered by Task 7 and Task 9.
- No keep-awake API: no task adds it.
- Companion pet reserved only: `IModeStatusSink` and `ModeController` state events cover future integration without implementing pet UI.

Placeholder scan:

- No unfinished placeholder instructions were found.
- Each implementation task names files, code shape, commands, expected result, and commit.

Type consistency:

- `RunMode`, `ModeStatus`, `AppState`, `PowerSchemeState`, `IPowerSettingsService`, `IClock`, and `ModeController` names are consistent across tasks.
