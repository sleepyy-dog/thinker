# Thinker

Thinker 是一个 Windows 托盘工具，用来快速切换笔记本合盖后的行为。

它的目标很小：当你需要合盖移动电脑但任务不能中断时，临时把当前电源计划的 AC/DC 合盖动作切换为 `Do Nothing`；恢复时优先还原开启前记录的原始设置，缺失或恢复失败时兜底恢复为 `Sleep`。

## 当前能力

- 托盘常驻应用，基于 `.NET 8 + WinForms NotifyIcon`
- 左键托盘图标：按锁定模式开启，或在已开启时恢复正常
- 右键菜单：
  - 开启合盖继续运行
  - 恢复正常模式
  - 选择锁定模式：`30 分钟`、`2 小时`、`永久`
  - 开机自启动
  - 退出
- 默认锁定模式为 `30 分钟`
- 退出程序时自动恢复
- 状态保存到 `%LOCALAPPDATA%\Thinker\state.json`
- 不启用 keep-awake API，只处理合盖动作

## 运行

需要 Windows 和 .NET 8 Desktop Runtime。

发布版路径：

```powershell
.\src\Thinker.App\bin\Release\net8.0-windows\win-x64\publish\Thinker.exe
```

从源码构建：

```powershell
dotnet restore Thinker.sln
dotnet test Thinker.sln
dotnet publish .\src\Thinker.App\Thinker.App.csproj -c Release -r win-x64 --self-contained false
```

## 注意

Thinker 依赖 Windows `powercfg.exe` 和当前设备暴露的 `LIDACTION` 设置。某些台式机、虚拟机、企业管控设备或厂商定制环境可能没有该设置，开启时会失败并在托盘状态中显示错误。

合盖继续运行有散热和耗电风险，尤其不要在高负载时把电脑长时间放进包里。

## 文档

- 初始设想：[lid-awake-project-plan.zh-CN.md](./lid-awake-project-plan.zh-CN.md)
- MVP 设计：[docs/superpowers/specs/2026-05-13-thinker-tray-mvp-design.md](./docs/superpowers/specs/2026-05-13-thinker-tray-mvp-design.md)
- 实施计划：[docs/superpowers/plans/2026-05-13-thinker-tray-mvp-implementation.md](./docs/superpowers/plans/2026-05-13-thinker-tray-mvp-implementation.md)
