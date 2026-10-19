# 🍳 Overcooked! 2 — BepInEx Fix for macOS (Apple Silicon)

A beginner-friendly guide to installing BepInEx for the **Steam macOS version of Overcooked! 2** on Apple Silicon Macs.

**Start with Method A. If it does not work, continue with Method B below.** You do not need to repeat the whole installation or reinstall the game.

## Which method should I use?

| Method | When to use it | What you need |
| --- | --- | --- |
| [Method A — Prepared package](#method-a) | Try this first. The macOS startup fix is already included in the prepared package. | The installation ZIP, Terminal, and Steam. Homebrew and a separate Mono installation are not required for this method. |
| [Method B — Manual fallback](#method-b) | Method A did not load BepInEx. Check the launch setup first, then apply the manual patch if the platform-detection error remains. | The existing BepInEx installation and, for the manual patch, Mono, `mcs`, and the two patch source files. |

> **If Method A works, stop there.** You do not need to install Mono or apply the manual startup patch again. Individual mods and external update tools may have their own requirements.

## What does this fix?

This repository addresses a BepInEx startup problem where macOS can be detected as Linux. A typical error is:

```text
System.DllNotFoundException: libc.so.6
BepInEx.Preloader.PlatformUtils:uname_linux(...)
```

The platform patch modifies **`BepInEx/core/BepInEx.Preloader.dll`**. It does not fix every game crash or every mod compatibility problem.

## Before you start

The reference setup used for this guide is:

```text
Game: Overcooked! 2 — Steam macOS version
Computer: Apple Silicon Mac
Game architecture: x86_64, running through Rosetta 2
Unity: 2017.4.8f1, Mono backend
BepInEx: 5.4.23.4
```

Other game builds or BepInEx versions may behave differently. The manual patch must match the installed BepInEx version; this guide is not for BepInEx 6.

- Completely quit Overcooked! 2 before copying or replacing files.
- Keep `Overcooked2.app`; the game itself is not included in this repository.
- If you already have BepInEx, back up your existing `BepInEx` folder, especially `plugins` and `config`.
- Keep plugin backups **outside `BepInEx/plugins`** so old DLLs are not loaded again.
- Commands below use Steam's default game location. If your game is elsewhere, use the location opened by **Steam → Manage → Browse local files** instead.

Default location:

```text
~/Library/Application Support/Steam/steamapps/common/Overcooked! 2
```

---

<a id="method-a"></a>

## Method A — Prepared package (recommended)

### A1. Download the prepared installation ZIP

1. Open [this repository](https://github.com/Bulubulubuu/Overcooked-2-about-the-BepInEx).
2. Find the prepared BepInEx installation ZIP in the file list. Depending on the revision, it may be named `放到和胡闹厨房app同级文件夹下.zip` or `Overcooked2-BepInEx-macOS-fix.zip`.
3. Open the ZIP file page and download the file.
4. Double-click the downloaded ZIP to extract it.

The extracted installation files should include `BepInEx`, `libdoorstop.dylib`, and `run_bepinex.sh`.

> The prepared **installation ZIP** and **Code → Download ZIP** are different downloads. The first installs BepInEx. The second downloads the repository source and any separately supplied mod folders.

### A2. Open the game folder

In Steam, open:

```text
Library → Overcooked! 2 → Manage → Browse local files
```

Finder should open the folder containing `Overcooked2.app`. Keep that window open.

### A3. Copy the prepared files

Copy the extracted installation files into the **same folder as `Overcooked2.app`**.

```text
Overcooked! 2/
├── Overcooked2.app/
├── BepInEx/
├── libdoorstop.dylib
└── run_bepinex.sh
```

Do not put them inside `Overcooked2.app` or leave them inside an extra wrapper folder. Back up an existing installation before replacing its files.

Some packages also include `doorstop_libs` or `changelog.txt`. A missing `doorstop_libs` folder alone does not mean that the prepared package is incomplete.

### A4. Replace the Arcade MOD folder, if you use it

If your installation includes the Arcade MOD, use the macOS-compatible `OC2HostUtilities` folder supplied for that mod version.

1. Completely quit the game.
2. On this repository's main page, choose **Code → Download ZIP** and extract it.
3. Find the top-level [OC2HostUtilities](./OC2HostUtilities) folder in the extracted repository.
4. Open your game's `BepInEx/plugins` folder.
5. Move the existing `OC2HostUtilities` folder to a backup location outside `plugins`.
6. Copy the complete replacement `OC2HostUtilities` folder into `plugins`, including its resource and version folders.

The destination is:

```text
Overcooked! 2/BepInEx/plugins/OC2HostUtilities/
```

The main DLL may be in this folder or in a version subfolder. Preserve the replacement package's layout. Do not combine DLLs from different releases or create a nested `OC2HostUtilities/OC2HostUtilities` folder.

Keep your other plugins and `BepInEx/config` unchanged. If the downloaded repository does not contain this folder, do not substitute an unrelated version; check the branch or package instructions.

> Updating the Arcade MOD can replace its Mac compatibility fixes. Use the fix for the version being installed. The BepInEx startup patch in Method B does not patch `HostUtilities.dll`.

### A5. Allow the launch script to run

Open Terminal: press **⌘ + Space**, type **Terminal**, and press Enter.

Copy and run:

```bash
chmod +x "$HOME/Library/Application Support/Steam/steamapps/common/Overcooked! 2/run_bepinex.sh"
```

No output normally means the command succeeded. If Terminal reports `No such file or directory`, check A2–A3 and the actual game location before continuing.

### A6. Set Steam Launch Options

Run this in Terminal:

```bash
echo "\"$HOME/Library/Application Support/Steam/steamapps/common/Overcooked! 2/run_bepinex.sh\" %command%"
```

Copy the **output**, which will look like:

```text
"/Users/yourname/Library/Application Support/Steam/steamapps/common/Overcooked! 2/run_bepinex.sh" %command%
```

Paste it into:

```text
Steam → Library → Overcooked! 2 → Properties → General → Launch Options
```

Keep the quotation marks and the space before `%command%`. Do not paste the `echo` command itself into Steam. The Terminal command automatically inserts your Mac username.

### A7. Restart Steam and check BepInEx

1. Completely quit Steam using **Steam → Quit Steam**.
2. Open Steam again and launch Overcooked! 2 from your Library.
3. Wait for the main menu, then quit the game.
4. Open `BepInEx/LogOutput.log` in the game folder, or run:

```bash
tail -100 "$HOME/Library/Application Support/Steam/steamapps/common/Overcooked! 2/BepInEx/LogOutput.log"
```

Successful startup includes messages similar to:

```text
Preloader started
Preloader finished
Chainloader ready
Chainloader started
Chainloader startup complete
```

Exact messages can vary. A normal startup reaches the chainloader and then loads installed plugins.

**If BepInEx works, installation is complete. Skip Method B and continue to [Installing mods](#installing-mods).**

**If it does not work, continue directly with Method B.** If `LogOutput.log` does not exist, Method B also explains where to look for earlier startup errors.

---

<a id="method-b"></a>

## Method B — Manual fallback if Method A fails

Use the files already installed by Method A. **Do not reinstall the game or overwrite all your plugins again.**

Follow B1–B3 first. These checks can fix launch-path and file-permission problems without installing Mono. B4–B7 are for applying the manual platform patch when the platform-detection error remains.

### B1. Quit the game and check the installed files

Completely quit Overcooked! 2. In Terminal, run:

```bash
ls -l "$HOME/Library/Application Support/Steam/steamapps/common/Overcooked! 2/run_bepinex.sh"
ls -l "$HOME/Library/Application Support/Steam/steamapps/common/Overcooked! 2/libdoorstop.dylib"
ls -lh "$HOME/Library/Application Support/Steam/steamapps/common/Overcooked! 2/BepInEx/core/BepInEx.Preloader.dll"
```

All three files should exist. The preloader DLL must have a nonzero size.

- If a file is missing, correct the location or copy the complete prepared installation files from A1–A3.
- If the preloader DLL is empty (`0B`), restore a known-good copy from the same BepInEx version before attempting a patch.
- If the game is installed elsewhere, replace the default location in all commands with its actual location.

### B2. Check the script target and permissions

Check the configured executable:

```bash
grep '^executable_name=' "$HOME/Library/Application Support/Steam/steamapps/common/Overcooked! 2/run_bepinex.sh"
```

Expected:

```text
executable_name="Overcooked2.app"
```

If it is empty or different, set it and check again:

```bash
sed -i '' 's/^executable_name=.*/executable_name="Overcooked2.app"/' "$HOME/Library/Application Support/Steam/steamapps/common/Overcooked! 2/run_bepinex.sh"
grep '^executable_name=' "$HOME/Library/Application Support/Steam/steamapps/common/Overcooked! 2/run_bepinex.sh"
```

Then allow the script to run:

```bash
chmod +x "$HOME/Library/Application Support/Steam/steamapps/common/Overcooked! 2/run_bepinex.sh"
```

If macOS has blocked downloaded files, and you trust their source, remove quarantine from the BepInEx files:

```bash
(
    set -e
    cd "$HOME/Library/Application Support/Steam/steamapps/common/Overcooked! 2"
    xattr -dr com.apple.quarantine BepInEx 2>/dev/null || true
    xattr -d com.apple.quarantine libdoorstop.dylib 2>/dev/null || true
    xattr -d com.apple.quarantine run_bepinex.sh 2>/dev/null || true
)
```

Recheck Steam Launch Options using **A6**, then restart Steam and test using **A7**.

**If this fixes startup, stop here. You do not need the manual patch.**

### B3. Confirm whether the platform patch is needed

If startup still fails, inspect `BepInEx/LogOutput.log` when available.

For errors that occur before that log is created, list the preloader logs:

```bash
find "$HOME/Library/Application Support/Steam/steamapps/common/Overcooked! 2/Overcooked2.app/Contents/MacOS" -type f -name 'preloader_*.log' -print
```

Open the newest relevant log in a text editor. Look for:

```text
System.DllNotFoundException: libc.so.6
```

or:

```text
BepInEx.Preloader.PlatformUtils:uname_linux
```

**If you see this platform-detection error, continue with B4.** If BepInEx already reaches the chainloader, or the error is different, use [Troubleshooting](#troubleshooting) instead of repeatedly applying this patch.

### B4. Install the tools needed for the manual patch

The patch tool uses **Mono** and the **`mcs` C# compiler**. The Mono runtime embedded inside the game is not a replacement for these Terminal commands.

First check:

```bash
mono --version
mcs --version
```

If both print version information, skip to **B5**.

If either command is missing, check Homebrew:

```bash
brew --version
```

If Homebrew works, install Mono:

```bash
brew install mono
```

If `brew` is missing, follow the [official Homebrew installation instructions](https://brew.sh/). The official Terminal installation command is:

```bash
/bin/bash -c "$(curl -fsSL https://raw.githubusercontent.com/Homebrew/install/HEAD/install.sh)"
```

Wait for installation to finish, then follow the installer's **Next steps** to add Homebrew to your shell. If it asks for your Mac password, no characters are displayed while you type; that is normal.

On an Apple Silicon Mac with Homebrew installed at `/opt/homebrew`, this enables it in the current Terminal window:

```bash
eval "$(/opt/homebrew/bin/brew shellenv)"
```

Then run:

```bash
brew install mono
mono --version
mcs --version
```

Continue only when both Mono and `mcs` are available. If a tool installation fails, resolve that error before running the patch.

### B5. Get the manual patch files

You need these **two files from the same repository revision**:

```text
patch_bepinex.sh
patch_platform.cs
```

If you already downloaded the repository in A4, check that folder first. Otherwise, download the repository with **Code → Download ZIP** and extract it.

If the current branch no longer includes the manual patch files, use the [manual-patch source snapshot](https://github.com/Bulubulubuu/Overcooked-2-about-the-BepInEx/tree/332c885b02af97f18e0afd677a8b63850acd1d40) and choose **Code → Download ZIP** there. These are the source files for the historical BepInEx 5 startup patch, not the Arcade MOD updater.

Open Terminal and enter the extracted repository folder. For a current `main` download extracted into Downloads, the usual command is:

```bash
cd "$HOME/Downloads/Overcooked-2-about-the-BepInEx-main"
```

If the folder has a different name or location, type `cd` followed by a space, drag the extracted repository folder into Terminal, then press Enter.

Confirm the two files are present:

```bash
ls -l patch_bepinex.sh patch_platform.cs
```

If either file is missing, correct the source folder before continuing.

### B6. Back up the preloader, then apply the patch

Keep the game closed. From the repository folder selected in B5, run this backup command:

```bash
(
    set -e
    OC2_PRELOADER="$HOME/Library/Application Support/Steam/steamapps/common/Overcooked! 2/BepInEx/core/BepInEx.Preloader.dll"
    if [ ! -s "$OC2_PRELOADER" ]; then
        echo "Preloader DLL is missing or empty. Restore a known-good copy first."
        exit 1
    fi
    cp -p "$OC2_PRELOADER" "$OC2_PRELOADER.manual-backup-$(date +%Y%m%d-%H%M%S)"
)
```

If the backup command reports an error, stop and fix that error first.

Make the script executable:

```bash
chmod +x patch_bepinex.sh
```

Then run the patch as **one complete command**:

```bash
./patch_bepinex.sh "$HOME/Library/Application Support/Steam/steamapps/common/Overcooked! 2"
```

Wait for it to finish. Do not run the game folder path by itself; the command must start with `./patch_bepinex.sh`.

A successful patch should report completion. It may also create a `BepInEx.Preloader.dll.original` backup. Keep the backup made above as well.

If the script reports a missing dependency, an unsupported pattern, no matching patch target, or another failure, **stop and keep the output**. Do not repeatedly patch or manually change platform constants from another BepInEx version.

### B7. Verify the patched DLL and test again

Check the target file:

```bash
ls -lh "$HOME/Library/Application Support/Steam/steamapps/common/Overcooked! 2/BepInEx/core/BepInEx.Preloader.dll"
file "$HOME/Library/Application Support/Steam/steamapps/common/Overcooked! 2/BepInEx/core/BepInEx.Preloader.dll"
```

The DLL must not be empty. `file` should identify a managed .NET/Mono assembly; it may describe it as PE32 even though the game is a 64-bit Mac application.

> A nonzero file size alone does not prove that the patch worked. The patch output and the next game startup log must also be checked. If the DLL is empty or invalid, restore a known-good backup before launching the game. Do not blindly copy an unverified `.patched` file over it.

1. Recheck Steam Launch Options using **A6**.
2. Completely quit and reopen Steam.
3. Launch Overcooked! 2 from Steam, wait for the main menu, then quit.
4. Check `BepInEx/LogOutput.log` as described in **A7**.

**If the chainloader starts normally, the BepInEx startup fix is complete.** You can keep using the installed package; there is no need to copy it again after patching.

If it still fails, keep the patch output and logs and see [Need help?](#need-help).

---

<a id="installing-mods"></a>

## Installing mods

After BepInEx starts successfully, install compatible mods in:

```text
Overcooked! 2/
└── BepInEx/
    └── plugins/
        └── ExamplePlugin.dll
```

Keep any resource folders required by the mod. Completely quit the game before replacing a DLL, then restart through Steam. Follow the mod creator's instructions for additional dependencies.

`0 plugins to load` is not a preloader error: BepInEx found no compatible plugins to load. If you expected installed mods, check their paths, dependencies, and compatibility.

---

<a id="troubleshooting"></a>

## Troubleshooting

### Terminal says `No such file or directory`

Open the real game folder through Steam and confirm the files are next to `Overcooked2.app`. Adjust the path in the commands if the game is on another drive or in another library. Quote paths that contain spaces.

If the missing file is `patch_bepinex.sh`, enter the repository source folder from B5. The patch script and the game folder are different locations.

### The game opens, but BepInEx does not load

Check A6 and B1–B3: Steam Launch Options, the script's `executable_name`, execute permissions, quarantine, and startup logs. Launch through Steam for this test.

### `libc.so.6` or `uname_linux`

Use **Method B**. This is the startup platform-detection problem addressed by the manual preloader patch.

### `BadImageFormatException`, an empty DLL, or an invalid assembly

Keep the game closed. Check the preloader file using B7 and restore a known-good copy from the same BepInEx installation. Do not mix BepInEx core files from different releases. Include the `file` output, file size, and patch output when reporting the problem.

### Harmony warnings

A warning such as a missing `isBatchMode` property is not, by itself, proof that startup failed. Check whether the log subsequently reaches the chainloader and loads plugins. Investigate any actual exceptions that follow.

### BepInEx reports `Bits64, iOS`

Do not treat that as a confirmed correct platform result on a Mac. Save the log and report the BepInEx version and patch output. Platform enum values can differ across dependency versions; repeatedly applying the historical patch is not a reliable fix.

### Arcade MOD updates try to run Windows PowerShell or CMD

This is a separate update-installer compatibility problem inside `HostUtilities.dll`. Reapplying the BepInEx preloader patch will not fix it.

Use the instructions for the [Mac external updater branch](https://github.com/Bulubulubuu/Overcooked-2-about-the-BepInEx/tree/mac-external-updater), or a Mac-compatible DLL for the same mod version. The external updater has separate Python/Mono requirements and must be used with a DLL that delegates updates to it. Merely copying the tool folder does not patch an existing DLL.

### The Arcade MOD has network, chat, or rendering problems

First confirm BepInEx completed startup. Then check the affected mod's version, configuration, dependencies, and logs. This README's manual platform patch is not a general repair for all plugin features.

---

## Restoring the original preloader

Completely quit the game. Use a known-good backup made from the **same BepInEx installation**.

List available preloader copies:

```bash
find "$HOME/Library/Application Support/Steam/steamapps/common/Overcooked! 2/BepInEx/core" -maxdepth 1 -type f -name 'BepInEx.Preloader.dll*' -exec ls -lh {} +
```

If `BepInEx.Preloader.dll.original` is the correct, nonempty backup, restore it with:

```bash
(
    set -e
    cd "$HOME/Library/Application Support/Steam/steamapps/common/Overcooked! 2/BepInEx/core"
    if [ ! -s BepInEx.Preloader.dll.original ]; then
        echo "Original backup is missing or empty. Nothing was restored."
        exit 1
    fi
    cp -p BepInEx.Preloader.dll.original BepInEx.Preloader.dll
    ls -lh BepInEx.Preloader.dll
)
```

To use the timestamped `.manual-backup-…` from B6 instead, copy that chosen file back to `BepInEx.Preloader.dll` in Finder. Restoring an unpatched original can also restore the platform-detection error; it is a rollback, not an additional fix.

## Disabling BepInEx temporarily

In **Steam → Overcooked! 2 → Properties → General → Launch Options**, save a copy of the launch line, then remove it. Launch through Steam to test without the BepInEx script. Paste the saved line back to enable BepInEx again.

You do not need to delete the game or your mods.

<a id="need-help"></a>

## Need help?

Open an [Issue](https://github.com/Bulubulubuu/Overcooked-2-about-the-BepInEx/issues) and include:

- Your Mac model and macOS version.
- The BepInEx version and whether you used Method A or Method B.
- A screenshot or the complete error text.
- The manual patch output, if you ran it.
- `BepInEx/LogOutput.log`, or the relevant preloader log if startup stopped earlier.
- The preloader file size and `file` output from B7.

Remove personal information from logs before posting them.

## Useful links

- [Repository](https://github.com/Bulubulubuu/Overcooked-2-about-the-BepInEx)
- [Historical manual-patch instructions and source](https://github.com/Bulubulubuu/Overcooked-2-about-the-BepInEx/tree/332c885b02af97f18e0afd677a8b63850acd1d40)
- [Homebrew installation](https://brew.sh/)
- [Homebrew Mono formula](https://formulae.brew.sh/formula/mono)
- [BepInEx releases](https://github.com/BepInEx/BepInEx/releases)

## Disclaimer

This is an unofficial community guide. It is not affiliated with Team17, Steam, Unity, or the BepInEx developers. The game itself is not included. You must own a legitimate copy of Overcooked! 2. Compatibility outside the reference setup is not guaranteed.
