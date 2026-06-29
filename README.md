# Thinker

Thinker 是一个 Windows 托盘工具，用来快速切换笔记本合盖后的行为。

为了让电脑不长时间处于运行，多数人设置关闭机盖这个动作后自动进入睡眠，但是 agent (例如cc,cx)往往有长时间的任务，我们的关闭机盖行为会导致任务中断。**Thinker** 在不修改windows 默认睡眠操作设置的前提下，让其即使合上机盖也能够保持自定义时长的不睡眠，维持任务运行的状态。

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

下载 GitHub Releases 里的 `Thinker-win-x64.exe` 后直接运行即可。发行版是 Windows x64 自包含单文件，不需要额外安装 .NET Runtime。

绿色版建议先放到固定目录再运行，例如 `D:\APP\Thinker\Thinker-win-x64.exe`。如果开启“开机自启动”，Thinker 会记录当前 exe 路径；后续移动 exe 后需要重新关闭并开启一次自启动。

从源码构建绿色单文件发行版：

```powershell
dotnet restore Thinker.sln
dotnet test Thinker.sln --configuration Release --no-restore
dotnet publish .\src\Thinker.App\Thinker.App.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true -p:DebugType=none -p:DebugSymbols=false
```

发布版路径：

```powershell
.\src\Thinker.App\bin\Release\net8.0-windows\win-x64\publish\Thinker.exe
```

## 注意

Thinker 依赖 Windows `powercfg.exe` 和当前设备暴露的 `LIDACTION` 设置。某些台式机、虚拟机、企业管控设备或厂商定制环境可能没有该设置，开启时会失败并在托盘状态中显示错误。

合盖继续运行有散热和耗电风险，尤其不要在高负载时把电脑长时间放进包里。

## 文档

- 初始设想：[lid-awake-project-plan.zh-CN.md](./lid-awake-project-plan.zh-CN.md)
- MVP 设计：[docs/superpowers/specs/2026-05-13-thinker-tray-mvp-design.md](./docs/superpowers/specs/2026-05-13-thinker-tray-mvp-design.md)
- 实施计划：[docs/superpowers/plans/2026-05-13-thinker-tray-mvp-implementation.md](./docs/superpowers/plans/2026-05-13-thinker-tray-mvp-implementation.md)
