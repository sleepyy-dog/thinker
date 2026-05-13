using Thinker.Models;
using Thinker.Services;

namespace Thinker;

public sealed class TrayApplicationContext : ApplicationContext, IModeStatusSink
{
    private readonly ContextMenuStrip contextMenu;
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
        contextMenu = new ContextMenuStrip();
        contextMenu.Opening += (_, _) => RefreshMenu();
        notifyIcon = new NotifyIcon
        {
            Visible = true,
            Text = "Thinker",
            ContextMenuStrip = contextMenu
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
            contextMenu.Dispose();
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
        var menu = contextMenu;
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
