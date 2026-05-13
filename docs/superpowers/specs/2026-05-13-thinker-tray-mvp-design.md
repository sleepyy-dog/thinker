# Thinker 托盘 MVP 设计

日期：2026-05-13

## 1. 背景与目标

Thinker 是一个 Windows 托盘工具，用来快速切换“合盖后是否进入睡眠”的行为。它不替代 Windows 电源设置，也不做完整电源管理中心，只解决一个明确问题：当用户需要合盖移动电脑但任务不能中断时，可以临时把 AC/DC 合盖动作切到 `Do Nothing`，并在恢复后尽量还原原本设置。

第一阶段采用“托盘 MVP + 桌宠预留接口”路线。第一阶段只交付托盘工具；第二阶段再加入桌宠，作为状态可视化和快捷控制层。

## 2. 阶段范围

### 第一阶段：托盘 MVP

- 使用 `.NET 8 + WinForms NotifyIcon`。
- 左键托盘图标执行快速切换：
  - 当前为正常模式时，按锁定模式开启“合盖继续运行”。
  - 当前已开启时，恢复正常模式。
- 右键菜单分为两块：
  - 开关模式：开启合盖继续运行、恢复正常模式。
  - 模式选择：30 分钟、2 小时、永久。
- 右键选择模式后保存为锁定模式。计时模式到期后自动恢复，但锁定模式保留。
- 开启后同时修改 AC/DC 的 `LIDACTION` 为 `Do Nothing`。
- 恢复时优先还原开启前记录的 AC/DC 原值；缺失可信原值时兜底恢复为 `Sleep`。
- 程序退出时自动恢复；如果恢复原值失败，则 fallback 到 `Sleep`。如果 fallback 也失败，允许退出并通知用户。
- 支持开机自启动。
- 状态通过托盘图标颜色、tooltip、右键菜单顶部展示。
- 不启用 keep-awake API。
- 异常退出或系统重启后，启动时按状态文件继续显示当前开启状态，不自动恢复、不额外警告；如果计时模式已过期，则执行恢复。

### 第二阶段：桌宠

- 桌宠不进入第一阶段交付范围。
- 桌宠只在“合盖继续运行”开启后出现。
- 左键点击桌宠直接恢复正常模式，取消当前合盖动作覆盖。
- 右键点击桌宠打开与托盘右键一致的菜单，复用第一阶段的控制接口。
- 桌宠只做 UI 和状态提示，不直接调用 `powercfg`。

## 3. 非目标

- 不做跨平台。
- 不绕过企业策略、组策略或厂商限制。
- 不默认启用防空闲睡眠。
- 不拦截用户手动点击睡眠、电源按钮、系统更新重启等行为。
- 第一阶段不做温度、低电量、断电、进程监控等高级安全策略。

## 4. 用户交互

### 托盘左键

- `Normal` -> 按当前锁定模式开启。
- `ActiveTimed` / `ActivePermanent` -> 恢复正常模式。
- `Error` -> 不做盲目切换，提示用户查看菜单状态。

### 托盘右键菜单

菜单顶部显示当前状态，例如：

```text
当前：合盖继续运行 · 2 小时
剩余：38 分钟
锁定模式：2 小时
```

菜单主体：

```text
开关模式
  开启合盖继续运行
  恢复正常模式

模式选择
  30 分钟
  2 小时
  永久

设置
  开机自启动
  退出
```

右键切换模式时：

- 未开启时，只更新锁定模式。
- 已开启时，更新锁定模式，并切换当前开启模式；计时模式会刷新过期时间。

## 5. 状态模型

核心状态：

- `Normal`：正常模式，不干预系统。
- `ActiveTimed`：合盖继续运行中，有过期时间。
- `ActivePermanent`：合盖继续运行中，无过期时间。
- `Error`：`powercfg` 调用失败、恢复失败、状态文件损坏且无法判断安全动作时进入。

状态文件建议放在：

```text
%LOCALAPPDATA%\Thinker\state.json
```

建议字段：

```json
{
  "active": true,
  "activeMode": "Timed2Hours",
  "lockedMode": "Timed2Hours",
  "schemeGuid": "SCHEME_CURRENT",
  "previousAcAction": 1,
  "previousDcAction": 1,
  "enabledAt": "2026-05-13T09:00:00+08:00",
  "expiresAt": "2026-05-13T11:00:00+08:00"
}
```

## 6. 模块设计

### PowerSettingsService

职责：

- 查询当前电源方案。
- 查询当前 AC/DC 合盖动作。
- 设置 AC/DC 合盖动作。
- 应用当前电源方案。
- 解析 `powercfg` 输出。

依赖：

- `powercfg.exe`。

### StateStore

职责：

- 读写 `%LOCALAPPDATA%\Thinker\state.json`。
- 保存当前状态、锁定模式、开启前 AC/DC 原值、启用时间、过期时间。
- 处理缺失文件、损坏文件、版本兼容。

### ModeController

职责：

- 执行开启、恢复、左键切换、右键模式切换、计时到期恢复。
- 确保重复开启不会覆盖第一次记录的原始 AC/DC 值。
- 恢复原值失败时 fallback 到 `Sleep`。
- 对外发布状态变化事件，供托盘 UI 和后续桌宠订阅。

### TrayApp

职责：

- 使用 `NotifyIcon` 展示图标、tooltip 和右键菜单。
- 派发左键、右键菜单操作到 `ModeController`。
- 展示当前状态、剩余时间、锁定模式。
- 展示错误或恢复失败通知。

### StartupService

职责：

- 管理开机自启动。
- 第一阶段建议使用当前用户注册表 `Run` 项。

### 第二阶段预留：CompanionView

职责：

- 订阅 `ModeController` 状态。
- 在开启模式时显示桌宠。
- 左键触发 `ModeController` 恢复正常模式，取消当前状态覆盖。
- 右键显示与托盘一致的上下文菜单。
- 不直接依赖 `powercfg`。

## 7. 数据流

### 开启合盖继续运行

1. 用户左键托盘或右键选择开启。
2. `ModeController` 读取当前状态。
3. 如果当前未开启，读取当前电源方案与 AC/DC `LIDACTION`，写入状态文件。
4. 设置 AC/DC `LIDACTION` 为 `Do Nothing`。
5. 应用当前电源方案。
6. 更新状态为 `ActiveTimed` 或 `ActivePermanent`。
7. 托盘图标、tooltip、菜单顶部刷新。

### 恢复正常模式

1. 用户左键托盘、右键恢复、计时到期或程序退出触发恢复。
2. `ModeController` 读取状态文件中的原始 AC/DC 值。
3. 优先恢复原始 AC/DC 值。
4. 如果原值缺失或恢复失败，fallback 到 `Sleep`。
5. 应用当前电源方案。
6. 清理或更新状态文件。
7. 托盘状态刷新；必要时显示通知。

### 启动

1. 读取状态文件。
2. 如果为正常状态，显示正常模式。
3. 如果为开启状态且未过期，继续显示开启状态，不自动恢复。
4. 如果为计时状态且已过期，执行恢复。
5. 如果状态文件损坏，重命名为 `.corrupt`，进入安全处理流程；需要恢复时 fallback 到 `Sleep`。

## 8. 风险与处理

- 散热风险：开启状态必须通过托盘图标、tooltip、菜单顶部持续可见。
- 权限或策略限制：`powercfg` 返回失败时进入 `Error`，不假装已切换成功。
- 状态恢复风险：状态文件保存原值；恢复失败时 fallback 到 `Sleep`。
- 多电源计划风险：第一阶段只支持启用时的当前计划；如果启用期间用户切换计划，菜单顶部显示检测到的当前值。
- 自启动可靠性：开机自启动是第一阶段范围内能力，减少状态不可见窗口。

## 9. 文件结构建议

```text
Thinker/
  Thinker.sln
  src/
    Thinker.App/
      Program.cs
      TrayApplicationContext.cs
      Services/
        ModeController.cs
        PowerSettingsService.cs
        StateStore.cs
        StartupService.cs
      Models/
        AppState.cs
        LidAction.cs
        RunMode.cs
        PowerSchemeState.cs
  tests/
    Thinker.Tests/
      PowerSettingsParserTests.cs
      StateStoreTests.cs
      ModeControllerTests.cs
      RestoreFallbackTests.cs
  docs/
    superpowers/
      specs/
        2026-05-13-thinker-tray-mvp-design.md
```

## 10. 渐进式开发顺序

1. 建立 .NET 解决方案、项目和测试项目。
2. 实现 `PowerSettingsService` 的解析层与命令执行抽象。
3. 实现 `StateStore`。
4. 实现 `ModeController` 的开启、恢复、模式锁定、计时到期逻辑。
5. 实现托盘 UI：左键切换、右键菜单、tooltip、图标状态。
6. 实现开机自启动。
7. 补齐异常处理和通知。
8. 做真实设备手动验证。

## 11. 测试策略

自动化测试：

- `PowerSettingsParserTests`：解析当前计划和 AC/DC `LIDACTION`。
- `StateStoreTests`：状态文件读写、缺失文件、损坏文件处理、缺失原值兜底。
- `ModeControllerTests`：左键切换、右键模式选择、计时到期恢复、重复开启不覆盖原始值。
- `RestoreFallbackTests`：恢复原值失败时 fallback 到 `Sleep`。

手动测试：

- AC/DC 都能切到 `Do Nothing`。
- 恢复原值成功。
- 恢复失败时 fallback 到 `Sleep`。
- 30 分钟和 2 小时计时逻辑可用。
- 开机自启动项创建和关闭。
- 托盘左键和右键行为符合预期。
- 程序退出时恢复。
- 异常退出或系统重启后按状态文件继续显示。

## 12. 决策记录

- 第一阶段不做桌宠，只预留接口。
- 使用 `.NET 8 + WinForms NotifyIcon`。
- 不启用 keep-awake API。
- AC/DC 一起切换。
- 恢复策略为优先恢复原值，缺失时兜底 `Sleep`。
- 默认锁定模式可由右键菜单选择，左键按锁定模式快速切换。
- 计时模式到期后恢复正常，但锁定模式保留。
- 异常退出或重启后不自动恢复，按状态文件继续显示当前开启状态。
- 第二阶段桌宠左键直接恢复正常模式，右键与托盘右键菜单一致。
