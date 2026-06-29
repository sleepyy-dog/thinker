# Thinker

Thinker 是一个 Windows 托盘工具，用于快速切换笔记本合盖后的运行状态。

多数人会把“合上机盖”设置为自动睡眠，以免电脑长时间运行；但 agent 任务（例如 cc、cx）经常需要持续执行，合盖睡眠会直接中断任务。**Thinker** 在不改动 Windows 默认睡眠设置的前提下，让电脑在合盖后仍能按自定义时长保持运行。

## 使用

从 [GitHub Releases](https://github.com/sleepyy-dog/thinker/releases) 下载 `Thinker-win-x64.exe`，放到固定目录后直接运行。

- 左键托盘图标：开启或恢复。
- 右键托盘图标：选择 `30 分钟`、`2 小时`、`永久`，或开启自启动。

## 注意

- 自启动会记录当前 exe 路径，移动文件后需要重新开关一次自启动。
- 依赖 Windows `powercfg.exe` 和设备的 `LIDACTION` 设置；不支持时会在托盘提示错误。
- 合盖继续运行有散热和耗电风险。
