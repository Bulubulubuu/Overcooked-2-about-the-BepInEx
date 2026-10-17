# OC2 Mac 外部更新工具

适用于 macOS Steam 版 Overcooked! 2、BepInEx 5、Mono 游戏版本。已针对街机 MOD **1.9.3** 检查与测试；未来版本必须保持相同安装接口，未知接口或缺失依赖会停止安装，不会保证支持所有新版。

## 安装

需要 **Python 3 和 Mono**（此工具不自带运行时），以及游戏目录中已有的 BepInEx/Mono.Cecil。先退出游戏。

解压后在终端运行（把路径换成解压位置）：

```bash
bash "/path/to/OC2MacUpdater/install.command"
```

游戏不是默认安装位置时：

```bash
bash "/path/to/OC2MacUpdater/install.command" "/path/to/Overcooked! 2"
```

安装脚本会将工具复制到 `BepInEx/tools/OC2MacUpdater`，然后从官方 GitHub Release 下载最新稳定版，核对 GitHub SHA-256，检查兼容性并安装。证书校验不会被关闭。不要把工具放入 `plugins`。

## 以后如何更新

安装过此工具适配的 DLL 后，**游戏内更新按钮**会把安装任务转交给外部脚本。脚本等待游戏退出，然后备份、迁移、检查并安装；原程序要求重启时，再通过 Steam 打开游戏。每次安装新 DLL 都会重新进行游戏接口检查并加入转接代码。

也可退出游戏后手动运行：

```bash
bash "$HOME/Library/Application Support/Steam/steamapps/common/Overcooked! 2/BepInEx/tools/OC2MacUpdater/update.command" --version latest
```

指定版本用 `--version v1.9.3`。本地包安装需 `--archive /path/HostUtilities.zip --sha256 官方SHA256 --version v1.9.3`。

安装逻辑主要在 **update.py**。修正备份、迁移和安装行为时，更新外部工具即可；DLL 内的安装转接代码可以继续使用。若转接接口本身或游戏代码发生变化，则需更新兼容检查器和转接代码，不能保证永远免维护。

## 实际处理

- 不执行 Windows PowerShell/CMD 安装器。
- 检查游戏缺失字段和方法，仅兼容已识别的 `s_RoomSearch_NoneAvailable` 与 `SendDiagnosticEvent`；未知缺失接口拒绝安装。
- 等游戏退出后执行，使用安装锁避免并发操作。
- 新资源按清单、大小和 SHA-256 校验，核对 MOD 与资源版本依赖。
- 保存整个旧 `OC2HostUtilities`，再切换到准备好的新目录；安装失败会回退。
- 把仍在旧资源目录的 ConfigurationManager DLL 原样迁到独立插件目录；已有独立版本不覆盖。
- 旧资源与 `.DS_Store` 完整归档，不因 Finder 元数据变化而失败；实际用户文件变动仍会阻止无确认的清理。
- 安装包、工具与备份不会提交到任何远程仓库。仅从官方 GitHub API 下载 Release。

## 备份与日志

完整原件备份：`BepInEx/backups/OC2MacUpdater/时间-编号/OC2HostUtilities`。官方未修改 DLL 也单独保留。备份不在 plugins 中，不会被重复加载。

游戏内更新日志：`BepInEx/cache/HostUtilitiesUpdates/plan-*/mac-apply.log`。成功/失败另有 `mac-apply-success` / `mac-apply-failed` 标记。

手动更新暂存：`BepInEx/cache/OC2MacUpdater`。

回退时先退出游戏，把当前 `OC2HostUtilities` 移到 plugins 外，再将需要的备份文件夹移回 `BepInEx/plugins/OC2HostUtilities`。工具迁移出的 ConfigurationManager 应避免与回退版本的旧目录重复加载。

## 已执行的检查

1. 官方 1.9.3 ZIP 摘要核验；修复 2 处缺失字段读取、5 处缺失诊断方法调用；两个 Windows 安装入口转接。
2. 嵌入代码仅修改预期方法，无外部桥接程序集运行依赖；游戏接口审计无剩余缺失引用。
3. 8 项外部工具测试：正常安装/完整备份、原文件变化停止、损坏资源停止、安装失败回滚、符号链接拒绝、未知计划拒绝、Finder 元数据完整归档、真实用户改动保留。
4. 两个实际更新方法的外部进程就绪握手测试，含空格路径。

以上不能代替游戏内实际联机与完整更新的运行验证。工具安装后仍需启动游戏确认。
