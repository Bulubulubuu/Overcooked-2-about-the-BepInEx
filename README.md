# Overcooked! 2 BepInEx Fix for Apple Silicon macOS

This guide explains how to install and fix **BepInEx for Overcooked! 2 on Apple Silicon Macs**.

The main issue addressed by this repository is a BepInEx preloader crash where macOS may be incorrectly detected as Linux, causing:

```text
System.DllNotFoundException: libc.so.6
at BepInEx.Preloader.PlatformUtils:uname_linux(...)
```

This repository provides a small patch for `BepInEx.Preloader.dll` so BepInEx can continue startup correctly on macOS.

---

# Tested Environment

This setup was successfully tested with:

```text
Game: Overcooked! 2 (Steam)
CPU: Apple Silicon
Game architecture: x86_64
Rosetta 2: Yes
Unity version: 2017.4.8f1
Unity backend: Mono
BepInEx: 5.4.23.4
```

Successful startup:

```text
[Message:   BepInEx] BepInEx 5.4.23.4 - Overcooked2
[Info   :   BepInEx] System platform: Bits64, MacOS
[Message:   BepInEx] Preloader started
[Message:   BepInEx] Preloader finished
[Info   :   BepInEx] Detected Unity version: v2017.4.8f1
[Message:   BepInEx] Chainloader ready
[Message:   BepInEx] Chainloader started
[Message:   BepInEx] Chainloader startup complete
```

> This guide is confirmed with **BepInEx 5.4.23.4**.  
> Other BepInEx versions may behave differently.

---

# Part 1 — Install BepInEx

## Step 1 — Download BepInEx

Go to the official BepInEx releases page:

```text
https://github.com/BepInEx/BepInEx/releases
```

Download the **macOS x64 / Unix x64 BepInEx 5 package** appropriate for the game.

For this guide, the confirmed working runtime is:

```text
BepInEx 5.4.23.4
```

Do not use BepInEx 6 for this guide.

---

## Step 2 — Open the Overcooked! 2 game directory

In Steam:

```text
Library
→ Right-click Overcooked! 2
→ Manage
→ Browse local files
```

The default game directory is usually:

```text
~/Library/Application Support/Steam/steamapps/common/Overcooked! 2
```

You can also open it from Terminal:

```bash
cd "$HOME/Library/Application Support/Steam/steamapps/common/Overcooked! 2"
```

---

## Step 3 — Extract BepInEx into the game directory

Extract the downloaded BepInEx ZIP.

Copy the extracted BepInEx files into the same directory as:

```text
Overcooked2.app
```

After installation, the game directory should look similar to:

```text
Overcooked! 2/
├── BepInEx/
├── doorstop_libs/
├── Overcooked2.app/
├── changelog.txt
├── libdoorstop.dylib
└── run_bepinex.sh
```

Check with:

```bash
ls
```

Expected output should contain:

```text
BepInEx
doorstop_libs
Overcooked2.app
changelog.txt
libdoorstop.dylib
run_bepinex.sh
```

---

## Step 4 — Give `run_bepinex.sh` execute permission

Run:

```bash
chmod +x "$HOME/Library/Application Support/Steam/steamapps/common/Overcooked! 2/run_bepinex.sh"
```

Verify:

```bash
ls -l "$HOME/Library/Application Support/Steam/steamapps/common/Overcooked! 2/run_bepinex.sh"
```

Expected permissions should contain `x`, for example:

```text
-rwxr-xr-x
```

---

## Step 5 — Remove macOS quarantine attributes

Downloaded files may be blocked by macOS quarantine.

Go to the game directory:

```bash
cd "$HOME/Library/Application Support/Steam/steamapps/common/Overcooked! 2"
```

Run:

```bash
xattr -dr com.apple.quarantine BepInEx
```

Then:

```bash
xattr -d com.apple.quarantine libdoorstop.dylib 2>/dev/null || true
```

And:

```bash
xattr -d com.apple.quarantine run_bepinex.sh 2>/dev/null || true
```

Check:

```bash
xattr -l libdoorstop.dylib
```

If there is no output, the quarantine attribute is no longer present.

---

## Step 6 — Set the executable name in `run_bepinex.sh`

Before configuring Steam, set the game executable name in:

```text
run_bepinex.sh
```

Run this command:

```bash
sed -i '' 's/^executable_name="".*/executable_name="Overcooked2.app"/' "$HOME/Library/Application Support/Steam/steamapps/common/Overcooked! 2/run_bepinex.sh"
```

Then verify:

```bash
grep '^executable_name=' "$HOME/Library/Application Support/Steam/steamapps/common/Overcooked! 2/run_bepinex.sh"
```

Expected output:

```text
executable_name="Overcooked2.app"
```

This step is important.

If `executable_name` is left empty, BepInEx may fail to locate the game and folders such as:

```text
BepInEx/plugins
BepInEx/config
```

may not be created.

---

# Part 2 — Configure Steam

## Step 7 — Add the Steam launch option

In Steam:

```text
Library
→ Right-click Overcooked! 2
→ Properties
→ General
→ Launch Options
```

Enter:

```text
"/Users/YOUR_USERNAME/Library/Application Support/Steam/steamapps/common/Overcooked! 2/run_bepinex.sh" %command%
```

Replace:

```text
YOUR_USERNAME
```

with your own macOS username.

Example:

```text
"/Users/sonia/Library/Application Support/Steam/steamapps/common/Overcooked! 2/run_bepinex.sh" %command%
```

Important:

```text
Keep the quotation marks.
Keep the space before %command%.
The correct syntax is %command%.
```

Do not write:

```text
%command/%
```

---

## Step 8 — Start the game once from Steam

Completely quit Steam.

Then reopen Steam and launch Overcooked! 2 normally.

If BepInEx initializes correctly, it may generate:

```text
BepInEx/config
BepInEx/plugins
BepInEx/cache
BepInEx/LogOutput.log
```

If macOS blocks a downloaded component, go to:

```text
System Settings
→ Privacy & Security
```

and allow it if macOS provides an option.

---

# Part 3 — Check the game environment

## Step 9 — Check the game architecture

Go to the game directory:

```bash
cd "$HOME/Library/Application Support/Steam/steamapps/common/Overcooked! 2"
```

Run:

```bash
file Overcooked2.app/Contents/MacOS/Overcooked2
```

Expected output:

```text
Overcooked2.app/Contents/MacOS/Overcooked2: Mach-O 64-bit executable x86_64
```

This means the game is an Intel `x86_64` application and runs through Rosetta on Apple Silicon.

---

## Step 10 — Check the Unity version

Run:

```bash
strings Overcooked2.app/Contents/MacOS/Overcooked2 | grep -E "20[0-9][0-9]\.[0-9]+\.[0-9]+f[0-9]+" | head
```

For the tested game version:

```text
2017.4.8f1
```

---

## Step 11 — Check that the game uses Unity Mono

Run:

```bash
find Overcooked2.app -name "Assembly-CSharp.dll" -o -name "libmono*.dylib"
```

Expected files include:

```text
Overcooked2.app/Contents/Resources/Data/Managed/Assembly-CSharp.dll
Overcooked2.app/Contents/Frameworks/Mono/MonoEmbedRuntime/osx/libmono.0.dylib
```

This confirms the game uses the Unity Mono backend.

---

# Part 4 — Install Mono for the patch tool

## Step 12 — Install Mono

The patch utility uses Mono and `mcs`.

If Homebrew is installed:

```bash
brew install mono
```

Check Mono:

```bash
mono --version
```

Example:

```text
Mono JIT compiler version 6.x
```

Check the compiler:

```bash
mcs --version
```

Expected output:

```text
Mono C# compiler version 6.x
```

---

# Part 5 — Identify the BepInEx crash

## Step 13 — Check the preloader error

Launch Overcooked! 2 from Steam.

If BepInEx fails before creating:

```text
BepInEx/LogOutput.log
```

go back to the game directory:

```bash
cd "$HOME/Library/Application Support/Steam/steamapps/common/Overcooked! 2"
```

Check for preloader logs:

```bash
find Overcooked2.app/Contents/MacOS -name "preloader_*.log" -print
```

Read the newest log:

```bash
latest=$(find Overcooked2.app/Contents/MacOS -name "preloader_*.log" -print | sort | tail -1)
echo "$latest"
[ -n "$latest" ] && cat "$latest"
```

The specific error fixed by this repository looks like:

```text
System.Reflection.TargetInvocationException:
Exception has been thrown by the target of an invocation.

---> System.DllNotFoundException: libc.so.6

at BepInEx.Preloader.PlatformUtils:uname_linux(...)
at BepInEx.Preloader.PlatformUtils.SetPlatform()
at BepInEx.Preloader.PreloaderRunner.PreloaderPreMain()
```

If you do not see this error, your issue may be different.

---

# Part 6 — Download and apply this patch

## Step 14 — Clone this repository

Go to Desktop:

```bash
cd "$HOME/Desktop"
```

Clone this repository:

```bash
git clone https://github.com/Bulubulubuu/Overcooked-2-about-the-BepInEx.git
```

Enter it:

```bash
cd Overcooked-2-about-the-BepInEx
```

Check the files:

```bash
ls
```

Expected files:

```text
README.md
patch_platform.cs
patch_bepinex.sh
examples
```

---

## Step 15 — Make the patch script executable

Run:

```bash
chmod +x patch_bepinex.sh
```

Verify:

```bash
ls -l patch_bepinex.sh
```

Expected permissions should include:

```text
-rwxr-xr-x
```

---

## Step 16 — Apply the BepInEx platform patch

Run this as one complete command:

```bash
./patch_bepinex.sh "$HOME/Library/Application Support/Steam/steamapps/common/Overcooked! 2"
```

Do not run the game directory by itself.

Wrong:

```bash
"$HOME/Library/Application Support/Steam/steamapps/common/Overcooked! 2"
```

That may produce:

```text
zsh: permission denied
```

Correct:

```bash
./patch_bepinex.sh "$HOME/Library/Application Support/Steam/steamapps/common/Overcooked! 2"
```

A successful patch should show output similar to:

```text
Compiling patcher...
Applying patch...

Backup created:
.../BepInEx.Preloader.dll.original

Patching platform constant 137 -> 73

Patch complete.

Done.
Now start Overcooked! 2 through Steam.
```

The script patches:

```text
BepInEx/core/BepInEx.Preloader.dll
```

and keeps a backup:

```text
BepInEx/core/BepInEx.Preloader.dll.original
```

---

## Step 17 — What the patch changes

The relevant platform value is changed from:

```text
137
```

to:

```text
73
```

where:

```text
137 = Linux
73  = macOS
```

Before:

```text
0091: ldloc.2
0092: ldstr unix
0097: callvirt System.Boolean System.String::Contains(System.String)
009C: brfalse.s IL_00a4
009E: ldc.i4 137
00A3: stloc.0
```

After:

```text
0091: ldloc.2
0092: ldstr unix
0097: callvirt System.Boolean System.String::Contains(System.String)
009C: brfalse.s IL_00a4
009E: ldc.i4 73
00A3: stloc.0
```

---

# Part 7 — Verify the fix

## Step 18 — Restart Steam

Completely quit Steam.

Then reopen Steam.

Launch Overcooked! 2 normally from Steam.

Wait until the main menu appears.

Then quit the game.

---

## Step 19 — Check whether BepInEx initialized

Return to the game directory:

```bash
cd "$HOME/Library/Application Support/Steam/steamapps/common/Overcooked! 2"
```

Run:

```bash
find BepInEx -maxdepth 2 -print
```

A successful installation should contain:

```text
BepInEx/cache
BepInEx/patchers
BepInEx/config
BepInEx/config/BepInEx.cfg
BepInEx/plugins
BepInEx/LogOutput.log
```

---

## Step 20 — Check the final BepInEx log

Run:

```bash
tail -100 BepInEx/LogOutput.log
```

Expected successful output:

```text
[Message:   BepInEx] BepInEx 5.4.23.4 - Overcooked2
[Info   :   BepInEx] Running under Unity vUnknown (post-2017)
[Info   :   BepInEx] CLR runtime version: 2.0.50727.1433
[Info   :   BepInEx] Supports SRE: True
[Info   :   BepInEx] System platform: Bits64, MacOS
[Message:   BepInEx] Preloader started
[Info   :   BepInEx] Loaded 1 patcher method from [BepInEx.Preloader 5.4.23.4]
[Info   :   BepInEx] 1 patcher plugin loaded
[Info   :   BepInEx] Patching [UnityEngine.CoreModule] with [BepInEx.Chainloader]
[Message:   BepInEx] Preloader finished
[Info   :   BepInEx] Detected Unity version: v2017.4.8f1
[Message:   BepInEx] Chainloader ready
[Message:   BepInEx] Chainloader started
[Info   :   BepInEx] 0 plugins to load
[Message:   BepInEx] Chainloader startup complete
```

The most important lines are:

```text
System platform: Bits64, MacOS
Preloader finished
Chainloader started
Chainloader startup complete
```

If these appear, BepInEx is working.

---

# Part 8 — Install Mods

## Step 21 — Put plugins into `BepInEx/plugins`

Compatible BepInEx plugin DLLs normally go into:

```text
BepInEx/plugins/
```

Example:

```text
Overcooked! 2/
└── BepInEx/
    └── plugins/
        └── ExamplePlugin.dll
```

Then restart Overcooked! 2 through Steam.

Check the log:

```bash
tail -100 BepInEx/LogOutput.log
```

If no plugins are installed, this is normal:

```text
[Info   :   BepInEx] 0 plugins to load
```

---

# Why does this happen?

The relevant BepInEx method is:

```text
BepInEx.Preloader.PlatformUtils.SetPlatform()
```

On this Apple Silicon + Rosetta + old Unity Mono configuration, the runtime may report the operating system as:

```text
Unix
```

BepInEx then classifies the platform as Linux:

```text
Unix
↓
Linux
↓
uname_linux()
↓
libc.so.6
↓
Crash
```

But macOS does not provide Linux's:

```text
libc.so.6
```

The patch changes this affected platform path to macOS:

```text
Unix
↓
MacOS
↓
uname_osx()
↓
BepInEx continues startup
```

After the patch, BepInEx reports:

```text
System platform: Bits64, MacOS
```

---

# Troubleshooting

## `BadImageFormatException`

If the patch script produces:

```text
System.BadImageFormatException:
Format of the executable (.exe) or library (.dll) is invalid.
```

first check:

```bash
file "$HOME/Library/Application Support/Steam/steamapps/common/Overcooked! 2/BepInEx/core/BepInEx.Preloader.dll"
```

Also check:

```bash
ls -lh "$HOME/Library/Application Support/Steam/steamapps/common/Overcooked! 2/BepInEx/core/BepInEx.Preloader.dll"
```

This may mean that:

```text
the installed BepInEx version is different
the DLL is damaged
the wrong BepInEx package was installed
the Preloader assembly is not compatible with this patch
```

This guide was tested with:

```text
BepInEx 5.4.23.4
```

---

## `libc.so.6` still appears

If the preloader log still contains:

```text
BepInEx.Preloader.PlatformUtils:uname_linux
System.DllNotFoundException: libc.so.6
```

the active `BepInEx.Preloader.dll` may not have been patched.

Also make sure Steam is using the same BepInEx installation you modified.

---

## `BepInEx/plugins` does not appear

First check:

```bash
grep '^executable_name=' "$HOME/Library/Application Support/Steam/steamapps/common/Overcooked! 2/run_bepinex.sh"
```

Expected:

```text
executable_name="Overcooked2.app"
```

If it is empty:

```text
executable_name=""
```

set it with:

```bash
sed -i '' 's/^executable_name="".*/executable_name="Overcooked2.app"/' "$HOME/Library/Application Support/Steam/steamapps/common/Overcooked! 2/run_bepinex.sh"
```

Then restart Steam and launch the game again.

---

## `0 plugins to load`

This is not an error:

```text
[Info   :   BepInEx] 0 plugins to load
```

It means:

```text
BepInEx/plugins/
```

currently contains no compatible plugins.

---

## HarmonyX `isBatchMode` warning

You may see:

```text
[Warning: HarmonyX] AccessTools.Property: Could not find property for type UnityEngine.Application and name isBatchMode
```

With Unity `2017.4.8f1`, this warning was not fatal in the tested setup.

BepInEx still successfully reached:

```text
Chainloader startup complete
```

---

# Restore the original Preloader

To undo the patch:

```bash
cd "$HOME/Library/Application Support/Steam/steamapps/common/Overcooked! 2"
```

Then:

```bash
cp \
BepInEx/core/BepInEx.Preloader.dll.original \
BepInEx/core/BepInEx.Preloader.dll
```

---

# Repository Files

This repository contains:

```text
Overcooked-2-about-the-BepInEx/
├── README.md
├── patch_platform.cs
├── patch_bepinex.sh
├── .gitignore
└── examples/
    ├── error-log.txt
    └── success-log.txt
```

It does not include:

```text
Overcooked2.app
game assets
Steam game files
BepInEx binaries
Unity DLLs
modified game assets
```

Overcooked! 2 and BepInEx must be installed separately.

---

# Important Notes

- This workaround was tested with **BepInEx 5.4.23.4**.
- Do not assume BepInEx 6 works with this guide.
- The patch does not modify Overcooked! 2 gameplay.
- The patch only changes BepInEx platform detection.
- Keep `BepInEx.Preloader.dll.original`.
- Do not copy the patched Preloader DLL to a Linux machine.

---

# Disclaimer

Use this patch at your own risk.

Always keep a backup of:

```text
BepInEx.Preloader.dll
```

before modifying it.
