# 合盖唤醒模式切换工具项目设想

## 1. 背景

在 Windows 笔记本上，用户常见的合盖行为通常只有一种全局策略：

- 合上盖子后睡眠
- 合上盖子后休眠
- 合上盖子后不执行任何操作

这个设置适合固定习惯，但不适合需要临时切换的场景。

当前需求不是“永远合盖不睡眠”，也不是“永远合盖休眠”，而是希望根据当前工作状态显式切换：

- 平时静置电脑时，合盖进入睡眠或休眠，节省电量并避免发热。
- 有长任务运行时，例如编译、下载、训练、批处理、远程会话、实验脚本，希望合盖移动电脑时任务不中断。
- 任务结束后，希望自动或手动恢复原来的合盖休眠策略。

PowerToys Awake 可以防止空闲睡眠，但它当前不适合作为“合盖动作覆盖开关”使用。更合理的项目方向是做一个专门的小工具，用来临时切换 Windows 的合盖动作，并结合 keep-awake 能力控制当前状态。

相关 PowerToys issue 已补充场景：

https://github.com/microsoft/PowerToys/issues/34479#issuecomment-4429236726

## 2. 目标

做一个轻量 Windows 工具，让用户可以一键切换笔记本合盖后的行为。

核心目标：

1. 用户平时保持 Windows 默认策略，例如合盖睡眠或休眠。
2. 当需要移动电脑但任务必须继续运行时，用户可以开启“合盖继续运行模式”。
3. 开启后，工具临时把合盖动作改为“不执行任何操作”，并可选开启系统防睡眠。
4. 关闭模式或超时后，工具恢复原来的合盖动作。
5. 工具必须明确显示当前状态，避免用户误以为电脑会休眠，实际却在包里持续运行。

## 3. 非目标

第一版不建议做以下能力：

- 不做复杂电源管理中心，避免替代 Windows 电源设置。
- 不做跨平台版本，先只支持 Windows。
- 不尝试拦截所有系统睡眠来源，例如手动点击睡眠、电源按钮、系统更新重启。
- 不绕过企业策略、组策略或设备厂商限制。
- 不默认允许合盖高负载运行，必须提供明显风险提示。

## 4. 用户场景

### 场景 A：平时合盖休眠

用户完成工作后合上盖子，电脑应按原 Windows 设置进入睡眠或休眠。

期望：

- 不需要每次手动打开 Windows 设置。
- 工具不应长期破坏用户的默认电源习惯。

### 场景 B：长任务运行中需要移动电脑

用户正在运行长任务，需要短距离移动电脑，例如从书桌移动到会议室或换一个位置。

期望：

- 用户开启“合盖继续运行模式”。
- 合上盖子后电脑不进入睡眠或休眠。
- 任务继续运行。
- 用户打开盖子后仍能看到模式处于开启状态。

### 场景 C：任务结束后恢复安全状态

用户开启合盖继续运行模式后，任务完成或计时结束。

期望：

- 工具自动恢复原来的合盖动作。
- 可选显示通知：“已恢复合盖睡眠/休眠”。

### 场景 D：忘记关闭模式

用户开启模式后忘记关闭，并把电脑放入包中。

期望：

- 工具不应让风险变得隐蔽。
- 第一版至少需要明显状态提示。
- 后续版本可以加入超时、低电量提醒、温度提醒、断电提醒等保护。

## 5. 推荐的 MVP 行为

第一版建议只做三个模式。

### 模式 1：正常模式

含义：

- 不干预系统。
- 合盖行为完全由 Windows 当前电源计划决定。

适用场景：

- 日常使用。
- 静置后希望合盖睡眠或休眠。

### 模式 2：合盖继续运行

含义：

- 记录当前电源计划下 AC 和 DC 的合盖动作。
- 把 AC 和 DC 的合盖动作都临时设为 `Do Nothing`。
- 可选调用系统 keep-awake API 防止空闲睡眠。

适用场景：

- 合盖移动电脑，但任务需要继续运行。

### 模式 3：定时合盖继续运行

含义：

- 在指定时长内启用“合盖继续运行”。
- 到期后恢复原来的合盖动作。

适用场景：

- 预计任务 30 分钟或 2 小时内完成。
- 降低忘记关闭模式的风险。

## 6. 技术可行性

Windows 提供了 `powercfg` 命令，可以修改当前电源计划中的合盖动作。

合盖动作设置项是：

- Setting: `Lid switch close action`
- Alias / GUID: `LIDACTION`
- Subgroup: `SUB_BUTTONS`

取值：

- `0`: Do Nothing
- `1`: Sleep
- `2`: Hibernate
- `3`: Shut Down

因此，工具可以通过命令行切换合盖策略。

示例命令：

```powershell
# 设置接通电源时合盖不执行任何操作
powercfg /setacvalueindex SCHEME_CURRENT SUB_BUTTONS LIDACTION 0

# 设置使用电池时合盖不执行任何操作
powercfg /setdcvalueindex SCHEME_CURRENT SUB_BUTTONS LIDACTION 0

# 应用当前电源方案
powercfg /setactive SCHEME_CURRENT
```

恢复睡眠：

```powershell
powercfg /setacvalueindex SCHEME_CURRENT SUB_BUTTONS LIDACTION 1
powercfg /setdcvalueindex SCHEME_CURRENT SUB_BUTTONS LIDACTION 1
powercfg /setactive SCHEME_CURRENT
```

恢复休眠：

```powershell
powercfg /setacvalueindex SCHEME_CURRENT SUB_BUTTONS LIDACTION 2
powercfg /setdcvalueindex SCHEME_CURRENT SUB_BUTTONS LIDACTION 2
powercfg /setactive SCHEME_CURRENT
```

官方参考：

- https://learn.microsoft.com/en-us/windows-hardware/customize/power-settings/power-button-and-lid-settings-lid-switch-close-action
- https://learn.microsoft.com/en-us/windows-hardware/design/device-experiences/powercfg-command-line-options

## 7. 为什么普通 Awake 类工具不够

PowerToys Awake、NoSleep、Keep-Alive、Caffeine 这类工具通常依赖系统 keep-awake API 或模拟用户活动，主要解决“系统空闲后自动睡眠”的问题。

这和“用户合上盖子后系统按电源策略睡眠/休眠”不是完全同一类行为。

微软 `SetThreadExecutionState` 文档说明，该 API 不能阻止用户主动让系统进入睡眠，例如通过电源按钮或关闭盖子触发的行为。

因此，如果目标是可靠控制合盖后是否休眠，直接切换 `LIDACTION` 比单纯调用 keep-awake API 更合适。

参考：

- https://learn.microsoft.com/en-us/windows/win32/api/winbase/nf-winbase-setthreadexecutionstate

## 8. 推荐实现路线

### 阶段 1：PowerShell 原型

目标：

- 快速验证 `powercfg` 切换是否满足个人需求。
- 不做 UI。
- 只做启用、恢复、查看状态三个命令。

建议命令：

```powershell
.\lid-awake.ps1 enable
.\lid-awake.ps1 restore
.\lid-awake.ps1 status
.\lid-awake.ps1 enable --duration 2h
```

状态文件：

```text
%LOCALAPPDATA%\LidAwake\state.json
```

记录内容：

```json
{
  "active": true,
  "schemeGuid": "SCHEME_CURRENT",
  "previousAcAction": 2,
  "previousDcAction": 2,
  "enabledAt": "2026-05-12T18:00:00+08:00",
  "expiresAt": "2026-05-12T20:00:00+08:00"
}
```

优点：

- 实现快。
- 易测试。
- 适合先自己用。

缺点：

- 没有托盘状态，容易忘记当前模式。
- 定时恢复需要计划任务或后台进程。

### 阶段 2：系统托盘小工具

目标：

- 做成常驻托盘应用。
- 左键切换模式。
- 右键显示菜单。
- 显示当前状态和风险提醒。

菜单建议：

```text
Lid Awake
--------------------------------
正常模式：跟随 Windows 合盖动作
合盖继续运行
合盖继续运行 30 分钟
合盖继续运行 2 小时
恢复原始合盖设置
--------------------------------
当前设置
开机启动
退出
```

托盘图标状态：

- 灰色：正常模式
- 绿色：合盖继续运行
- 黄色：定时模式
- 红色或警告叠加：恢复失败、权限不足、状态不一致

推荐技术栈：

- .NET 8 + WinForms NotifyIcon：最简单，适合托盘工具。
- .NET 8 + WPF：UI 稍复杂但体验更好。
- Rust + tray-icon：体积小，但 Windows 电源管理和安装体验需要更多处理。
- Go + systray：实现快，但 Windows 桌面体验和签名发布需要额外处理。

推荐第一版使用 `.NET 8 + WinForms NotifyIcon`。

理由：

- Windows 原生集成成本低。
- 托盘菜单容易做。
- 调用 `powercfg` 简单。
- 后续可以逐步加入设置窗口。

### 阶段 3：更安全的自动化能力

可以考虑增加：

- 计时器到期自动恢复。
- 低电量自动恢复。
- 拔掉电源后提醒。
- 温度过高提醒。
- 只在指定进程运行时启用，例如 `python.exe`、`node.exe`、`docker.exe`。
- 长时间合盖继续运行时重复提醒。
- 退出程序时恢复原设置。

这些能力不建议第一版全做，否则项目会变成完整电源管理器。

## 9. 架构设计

### 核心模块

#### PowerSettingsService

职责：

- 查询当前电源方案。
- 查询当前 AC/DC 合盖动作。
- 修改 AC/DC 合盖动作。
- 应用当前电源方案。

依赖：

- `powercfg.exe`

#### StateStore

职责：

- 保存工具启用前的原始设置。
- 保存当前模式、启用时间、过期时间。
- 在程序重启后恢复状态。

存储位置：

```text
%LOCALAPPDATA%\LidAwake\state.json
```

#### AwakeService

职责：

- 可选调用 `SetThreadExecutionState` 防止空闲睡眠。
- 在“合盖继续运行模式”开启期间保持系统活跃。

注意：

- 它不能代替 `LIDACTION` 修改。
- 它只能作为辅助能力。

#### TrayApp

职责：

- 显示当前模式。
- 提供切换菜单。
- 触发恢复、定时、退出等操作。

#### SafetyGuard

职责：

- 检测恢复失败。
- 检测电源状态变化。
- 检测定时到期。
- 发出通知。

## 10. 数据流

### 启用“合盖继续运行”

1. 用户点击托盘菜单。
2. 工具读取当前电源方案。
3. 工具读取当前 AC/DC 的 `LIDACTION`。
4. 工具写入状态文件。
5. 工具把 AC/DC 的 `LIDACTION` 改为 `0`。
6. 工具执行 `powercfg /setactive SCHEME_CURRENT`。
7. 工具更新托盘图标。
8. 工具显示通知。

### 恢复正常模式

1. 用户点击恢复，或定时器到期。
2. 工具读取状态文件。
3. 工具恢复之前记录的 AC/DC `LIDACTION`。
4. 工具执行 `powercfg /setactive SCHEME_CURRENT`。
5. 工具停止 keep-awake。
6. 工具清理或更新状态文件。
7. 工具显示通知。

### 程序异常退出后重新打开

1. 工具启动时读取状态文件。
2. 如果发现之前处于启用状态，托盘显示“仍处于合盖继续运行”。
3. 用户可以选择继续保持或立即恢复。

## 11. 风险与边界

### 散热风险

合盖后继续运行可能导致散热变差。尤其是把电脑放进包里时，长时间高负载运行存在发热和电池消耗风险。

应对：

- 开启模式时显示明确提示。
- 推荐默认使用定时模式。
- 后续版本加入温度、低电量、断电提醒。

### 权限风险

某些机器或企业环境可能限制电源设置修改。

应对：

- 检测 `powercfg` 返回码。
- 失败时明确提示“可能被策略限制或权限不足”。

### 状态恢复风险

如果程序崩溃或用户强制结束，可能无法自动恢复。

应对：

- 状态文件必须记录原始设置。
- 程序重新启动时检测未恢复状态。
- 提供命令行 `restore` 作为兜底。

### 多电源计划风险

用户可能在启用后切换了电源计划。

应对：

- 第一版只支持启用时的当前计划。
- 检测当前计划变化并提示。
- 后续可支持所有电源计划同步切换。

### 休眠不可用

有些设备关闭了休眠功能。

应对：

- 恢复时只恢复之前读取到的数值。
- 不主动假设用户一定支持休眠。

## 12. 测试要点

### 基础功能测试

- 启用后，AC 合盖动作变为 `Do Nothing`。
- 启用后，DC 合盖动作变为 `Do Nothing`。
- 恢复后，AC 合盖动作恢复原值。
- 恢复后，DC 合盖动作恢复原值。
- 重复启用不会覆盖第一次记录的原始值。
- 未启用时执行恢复不会破坏当前设置。

### 定时功能测试

- 设置 1 分钟后自动恢复。
- 程序重启后仍能识别剩余时间。
- 到期恢复失败时显示错误。

### 异常测试

- `powercfg` 不存在或调用失败。
- 权限不足。
- 状态文件损坏。
- 当前电源计划在启用期间变化。
- 用户手动修改了合盖设置。

### 真实设备测试

- 接通电源时合盖。
- 使用电池时合盖。
- 合盖后短距离移动再打开。
- 长任务运行期间合盖。
- 低电量时行为是否符合预期。

## 13. MVP 文件结构建议

如果使用 .NET 8 + WinForms：

```text
LidAwake/
  LidAwake.sln
  src/
    LidAwake.App/
      Program.cs
      TrayApplicationContext.cs
      PowerSettingsService.cs
      AwakeService.cs
      StateStore.cs
      SafetyGuard.cs
      Models/
        LidAction.cs
        AppState.cs
        PowerSchemeState.cs
  tests/
    LidAwake.Tests/
      PowerSettingsParserTests.cs
      StateStoreTests.cs
      ModeTransitionTests.cs
  docs/
    README.md
```

如果先做 PowerShell 原型：

```text
lid-awake/
  lid-awake.ps1
  README.md
  tests/
    lid-awake.tests.ps1
```

## 14. 推荐渐进开发顺序

1. 验证 `powercfg` 是否能在你的机器上正确读写 `LIDACTION`。
2. 写 PowerShell 原型，只支持 `enable`、`restore`、`status`。
3. 加状态文件，确保恢复逻辑可靠。
4. 加定时恢复。
5. 做托盘应用，复用已经验证过的核心逻辑。
6. 加通知和明显状态提示。
7. 加安全增强，例如低电量、断电、温度提醒。
8. 再考虑进程监控、自动模式、开机启动。

## 15. 可以先做的最小原型

第一天可以只做一个脚本，证明方案可行：

```powershell
.\lid-awake.ps1 enable
```

效果：

- 保存当前 AC/DC 合盖动作。
- 设置 AC/DC 合盖动作为 `Do Nothing`。

```powershell
.\lid-awake.ps1 restore
```

效果：

- 从状态文件恢复原来的 AC/DC 合盖动作。

只要这个原型稳定，后面的托盘 UI 就是体验层问题。

## 16. 待决策问题

这些问题后续真正开始写项目前需要确定：

1. 默认恢复目标是恢复原值，还是固定恢复为休眠？
2. 是否 AC 和 DC 都一起切换，还是允许分别配置？
3. 是否默认开启 keep-awake API，还是只切换合盖动作？
4. 是否强制使用定时模式，避免忘记关闭？
5. 是否要做开机自启？
6. 是否需要检测指定进程，例如只有 `python.exe` 或 `docker.exe` 运行时才保持开启？
7. 是否接受 .NET 8 运行时依赖，还是希望单文件发布？

## 17. 初步结论

这个项目是可行的，而且不需要一开始写得很复杂。

最可靠的核心不是“模拟活动”或“阻止空闲睡眠”，而是显式切换 Windows 的合盖动作 `LIDACTION`。PowerToys Awake 可以作为参考，但你的需求更适合一个专门的“合盖模式切换器”。

推荐路线：

1. 先做 PowerShell 原型验证。
2. 再做 .NET 托盘应用。
3. 默认提供“定时合盖继续运行”，降低误用风险。
4. 所有状态切换都必须可见、可恢复、可解释。

