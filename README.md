# 胡闹厨房 2：Mac 街机 MOD 更新工具 · 新手安装指南

这个分支提供 **OC2MacUpdater**，用于 Mac Steam 版 Overcooked! 2 的街机 MOD（OC2HostUtilities）。

它处理两个问题：街机 MOD 的部分联机代码与 Mac 游戏不兼容，以及更新程序只会调用 Windows PowerShell。工具把安装、备份和迁移交给外部 Python 脚本，并在安装新版 DLL 前检查已识别的兼容问题。

**不需要编程，不需要自己改 DLL。** 首次安装需要准备 Python 3 和 Mono；工具不包含这两个运行时，也不包含游戏和资源包。

> 已检查与测试的 MOD 版本：**1.9.3**。未来版本接口不兼容、资源不足或验证失败时，工具会停止安装，保留原版本。流程测试已通过，真实游戏内更新和联机仍需运行验证。

## 先看这里：你应该从哪一步开始？

| 你的情况 | 从哪里开始 |
| --- | --- |
| 已经能打开游戏和街机 MOD，只是更新失败或入房报错 | 直接看下面第 1 步 |
| 还没装 BepInEx，或者连街机 MOD 都没有 | 先完成[原版 BepInEx 安装教程](docs/BEPINEX_INSTALLATION.en.md)，再回到这里 |
| 游戏目录中没有 `OC2HostUtilities/Resources` 或资源清单 | 先安装完整资源包；这个小工具包本身不含音效等资源 |
| 通过 CrossOver/虚拟机运行 Windows 版游戏 | 本说明不适用 |

[下载 OC2MacUpdater.zip](./OC2MacUpdater.zip) · [详细技术说明](OC2MacUpdater/README.md) · [查看工具源码](./OC2MacUpdater)

## 第 1 步：完全退出游戏

在游戏中退出，然后确认 Steam 库里的 Overcooked! 2 按钮已经变回 **“开始游戏 / Play”**，而不是“停止”。

安装和迁移时不要让游戏保持运行。Steam 本身可以开着。

## 第 2 步：下载并解压工具

1. 点击上面的 **“下载 OC2MacUpdater.zip”**。
2. 如果打开的是 GitHub 文件预览页，点击右上角的 **Download raw file / 下载原始文件** 图标。
3. 下载后，在 Finder 的 **“下载 / Downloads”** 文件夹找到 `OC2MacUpdater.zip`。
4. 双击 ZIP 解压。
5. 确认得到的文件夹名字是 **`OC2MacUpdater`**。如果系统起名为 `OC2MacUpdater 2`，先改回 `OC2MacUpdater`。

文件夹里面应能看到：

```text
OC2MacUpdater/
├── install.command
├── update.command
├── update.py
├── CompatPatcher.exe
├── MacUpdateBridge.dll
└── README.md
```

其中的 `.exe` 由 Mono 运行，不是让你双击的 Windows 安装程序。**保留整个文件夹，不要只取出一个 DLL，也不要把它放进 plugins。**

## 第 3 步：打开终端

按 **⌘ Command + 空格**，输入 **终端** 或 **Terminal**，按回车。

接下来代码框里的命令可以整行复制。不要复制终端提示符或说明文字。

## 第 4 步：检查 Python 和 Mono

分别输入下面两条命令，每条输入后按回车：

```bash
python3 --version
```

```bash
mono --version
```

如果能看到 **Python 3.10 或更新版本**，以及 Mono 的版本信息，可以直接跳到第 5 步。

如果看到 `command not found`，或 Python 版本过旧，请按下面的方法安装依赖。

### 已经装过 Homebrew

先输入：

```bash
brew --version
```

能看到版本号，就继续：

```bash
brew install python3 mono
```

等待安装结束，然后重新检查 `python3 --version` 和 `mono --version`。

### 没有 Homebrew

在 [Homebrew 官方网站](https://brew.sh/) 查看安装方法。官方提供 `.pkg` 安装器，也可以在终端执行官网给出的命令：

```bash
/bin/bash -c "$(curl -fsSL https://raw.githubusercontent.com/Homebrew/install/HEAD/install.sh)"
```

按屏幕提示完成。如果要求输入 Mac 登录密码，输入时不会显示字符，这是正常的。

安装末尾如果显示 **Next steps**，先执行它列出的命令，让终端能够找到 `brew`。Apple Silicon Mac 通常也可以用下面这条命令为当前终端加载 Homebrew：

```bash
eval "$(/opt/homebrew/bin/brew shellenv)"
```

然后安装依赖：

```bash
brew install python3 mono
```

依赖安装说明来源：[Homebrew](https://brew.sh/)、[Mono 软件包](https://formulae.brew.sh/formula/mono)、[Python 软件包说明](https://formulae.brew.sh/formula/python@3.14)。无需安装 pip 软件包。

## 第 5 步：安装 Mac 更新工具

### 游戏装在 Steam 默认位置

确认 `OC2MacUpdater` 文件夹在“下载”文件夹里，然后复制这条命令到终端：

```bash
bash "$HOME/Downloads/OC2MacUpdater/install.command"
```

**不要把 `$HOME` 改成你的用户名**，系统会自动展开成你的主目录。

这一步会：

1. 把工具安装到游戏的 `BepInEx/tools/OC2MacUpdater`。
2. 从官方 GitHub Release 下载最新稳定版 MOD，并核对 SHA-256。
3. 检查游戏接口、MOD 版本和资源依赖。
4. 完整备份旧 `OC2HostUtilities`。
5. 迁移旧资源里的 ConfigurationManager，安装带 Mac 兼容处理的新 DLL。

工具需要联网。如果验证失败会停止，不会通过关闭证书检查来继续下载。

### 游戏装在外置硬盘或其他位置

1. 在 Steam 中右键 Overcooked! 2。
2. 点击 **管理 → 浏览本地文件**。
3. 找到同时包含 `Overcooked2.app` 和 `BepInEx` 的那个游戏文件夹。
4. 在终端先输入下面的内容，末尾保留一个空格：

```bash
bash "$HOME/Downloads/OC2MacUpdater/install.command"
```

5. 把 **整个游戏文件夹**从 Finder 拖进终端，它会自动填入路径。
6. 按回车执行。

最终类似这样（以你实际拖入的路径为准）：

```bash
bash "$HOME/Downloads/OC2MacUpdater/install.command" "/Volumes/Games/Overcooked! 2"
```

## 第 6 步：确认安装成功

如果终端最后出现类似：

```text
安装成功: 1.9.3.0 完整备份: .../BepInEx/backups/OC2MacUpdater/...
```

就说明文件安装完成了。安装当时的最新版本可能不同。

从 **Steam 库**重新启动游戏，先检查 MOD 能否加载，再试联机。

默认位置的关键文件如下：

```text
Overcooked! 2/
└── BepInEx/
    ├── plugins/
    │   ├── OC2HostUtilities/
    │   │   ├── Resources/
    │   │   └── v1.9.3/
    │   │       └── HostUtilities.dll
    │   └── ConfigurationManager/
    │       └── ConfigurationManager.dll
    ├── tools/
    │   └── OC2MacUpdater/
    │       ├── update.py
    │       └── ...
    └── backups/
        └── OC2MacUpdater/
            └── 时间-编号/
                └── OC2HostUtilities/
```

具体版本目录会随更新变化。ConfigurationManager 如果原本在独立位置，会保留已有版本。

## 以后怎么更新？

### 方法 A：使用游戏里的更新按钮

安装了这个工具适配过的 DLL 后，游戏内的更新请求会转交给外部脚本。

脚本先等待游戏退出，再核对文件、备份和安装。游戏要求自动重启时，会通过 Steam 启动游戏。

**不要等待游戏在运行中替换自己的 DLL。** 如果长时间没有完成，先确认游戏已经退出，再检查下面的日志。

### 方法 B：退出游戏，手动更新

默认游戏路径：

```bash
bash "$HOME/Library/Application Support/Steam/steamapps/common/Overcooked! 2/BepInEx/tools/OC2MacUpdater/update.command" --version latest
```

非默认路径，例如：

```bash
bash "/Volumes/Games/Overcooked! 2/BepInEx/tools/OC2MacUpdater/update.command" --game-dir "/Volumes/Games/Overcooked! 2" --version latest
```

每次安装的新 DLL 都会重新检查并添加已识别的兼容处理。安装、备份和迁移逻辑在外部 `update.py` 中；如果上游改变安装接口，仍需更新工具。

**不要用官方原始 DLL 手动覆盖适配版后，再指望游戏内转接仍然存在。** 如果已经覆盖，退出游戏，重新运行外部更新命令即可。

## 常见问题

### 找不到 `install.command`

检查是否已解压、文件夹是否在 Downloads、名字是否为 `OC2MacUpdater`。如果放在桌面，将命令中的 `Downloads` 改为 `Desktop`。

### 未找到 `Mono.Cecil.dll`

通常是游戏路径不对，或还没装 BepInEx。正确路径应包含 `BepInEx/core/Mono.Cecil.dll`。先检查 Steam 的“浏览本地文件”，或按[基础安装教程](docs/BEPINEX_INSTALLATION.en.md)完成安装。

### 找不到资源清单，或提示“资源版本不足”

工具不包含完整音效资源。需要先准备完整、满足该 MOD 版本要求的 `OC2HostUtilities/Resources`。不要删掉已有资源来尝试绕过检查。

### SSL certificate、HTTP 403、网络下载失败

这些是下载连接问题，工具不会关闭 HTTPS 证书验证。确认网络正常后重试；不要设置 `sslVerify false`。如果仍然失败，可以从官方 Release 网页下载 ZIP，再按[技术说明](OC2MacUpdater/README.md)使用本地安装包和官方 SHA-256。

### 提示“拒绝覆盖较新的版本”或“Unsupported”

工具拒绝了降级或无法识别的接口。当前安装会保留。请保留终端输出和日志，等待工具适配，不要强制替换 DLL。

### 更新时又出现 PowerShell 错误

可能当前加载的是未经适配的原版 DLL，或没有安装外部工具。退出游戏，重新运行第 5 步。若仍报错，请提供 `BepInEx/LogOutput.log` 和外部更新日志。

### `.DS_Store` 导致旧资源清理失败

此工具会把旧资源及 Finder 元数据一起归档，不需要手动删除 `.DS_Store`。如果真正的资源内容与备份不一致，清理会保留旧目录。

### 去哪里看更新日志？

游戏内发起的更新：

```text
BepInEx/cache/HostUtilitiesUpdates/plan-某个编号/mac-apply.log
```

同目录的 `mac-apply-success` / `mac-apply-failed` 表示成功 / 失败。手动更新的结果直接显示在终端里。

## 原文件在哪里？怎么恢复？

每次安装会把整个旧插件目录保留在：

```text
BepInEx/backups/OC2MacUpdater/时间-编号/OC2HostUtilities
```

恢复时先退出游戏。把当前 `BepInEx/plugins/OC2HostUtilities` 移到 plugins 外，再把选中的备份目录移回原位置。

如果旧备份里也有 ConfigurationManager DLL，要避免它与新迁移到独立目录的同一个插件重复加载。拿不准时先保留所有备份，并附上文件夹截图咨询。

## 说明

这是社区兼容工具，与 Team17、Steam 和 MOD 原作者没有官方关联。代码和流程已测试，但不能保证未来所有版本都适用。实际游戏内更新、联机结果仍需用户验证。源码保留在 [OC2MacUpdater](./OC2MacUpdater)，维护说明见[技术文档](OC2MacUpdater/README.md)。
