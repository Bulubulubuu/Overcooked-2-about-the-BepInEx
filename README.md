# Overcooked! 2 BepInEx Fix for Apple Silicon macOS

This guide explains how to install and fix **BepInEx for Overcooked! 2 on Apple Silicon Macs**.

The issue addressed by this repository is a BepInEx preloader crash where macOS may be incorrectly treated as Linux, causing:

```text
System.DllNotFoundException: libc.so.6
at BepInEx.Preloader.PlatformUtils:uname_linux(...)
```

This repository provides a small patch for `BepInEx.Preloader.dll` so BepInEx can correctly continue startup on macOS.

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

Open Terminal.

Run:

```bash
chmod +x "$HOME/Library/Application Support/Steam/steamapps/common/Overcooked! 2/run_bepinex.sh"
```

If macOS asks for your password, enter your Mac login password and press Enter.

> When entering a password in Terminal, no characters or dots are shown.  
> This is normal.

You can verify the permission with:

```bash
ls -l "$HOME/Library/Application Support/Steam/steamapps/common/Overcooked! 2/run_bepinex.sh"
```

You should see execute permissions such as:

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

No output means the quarantine attribute is no longer present.

---

# Part 2 — Configure Steam

## Step 6 — Add the Steam launch option

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

For example:

```text
"/Users/sonia/Library/Application Support/Steam/steamapps/common/Overcooked! 2/run_bepinex.sh" %command%
```

Important:

- Keep the quotation marks.
- Keep the space before `%command%`.
- Do not write `%command/%`.
- The correct syntax is:

```text
%command%
```

---

## Step 7 — Start the game once from Steam

Completely quit Steam first.

Then reopen Steam and launch Overcooked! 2 normally.

If BepInEx starts correctly, it will generate folders such as:

```text
BepInEx/config
BepInEx/plugins
BepInEx/cache
```

and:

```text
BepInEx/LogOutput.log
```

If macOS blocks a downloaded file, go to:

```text
System Settings
→ Privacy & Security
```

and allow the blocked application/file if macOS provides that option.

---

# Part 3 — Check the game before applying the patch

## Step 8 — Check the game architecture

Run:

```bash
cd "$HOME/Library/Application Support/Steam/steamapps/common/Overcooked! 2"
```

Then:

```bash
file Overcooked2.app/Contents/MacOS/Overcooked2
```

Expected output:

```text
Overcooked2.app/Contents/MacOS/Overcooked2: Mach-O 64-bit executable x86_64
```

This means the game is an Intel `x86_64` application and runs through Rosetta on Apple Silicon.

---

## Step 9 — Check the Unity version

Run:

```bash
strings Overcooked2.app/Contents/MacOS/Overcooked2 | grep -E "20[0-9][0-9]\.[0-9]+\.[0-9]+f[0-9]+" | head
```

For the tested game version:

```text
2017.4.8f1
```

---

## Step 10 — Check that the game uses Unity Mono

Run:

```bash
find Overcooked2.app -name "Assembly-CSharp.dll" -o -name "libmono*.dylib"
```

Expected files include:

```text
Overcooked2.app/Contents/Resources/Data/Managed/Assembly-CSharp.dll
Overcooked2.app/Contents/Frameworks/Mono/MonoEmbedRuntime/osx/libmono.0.dylib
```

---

# Part 4 — Install Mono for the patch tool

## Step 11 — Install Mono

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

Expected:

```text
Mono C# compiler version 6.x
```

---

# Part 5 — Identify the BepInEx crash

## Step 12 — Check the preloader error

Launch Overcooked! 2 from Steam.

If BepInEx fails before creating `BepInEx/LogOutput.log`, check for a preloader log:

```bash
cd "$HOME/Library/Application Support/Steam/steamapps/common/Overcooked! 2"
```

Run:

```bash
find Overcooked2.app/Contents/MacOS -name "preloader_*.log" -print
```

To read the newest one:

```bash
latest=$(find Overcooked2.app/Contents/MacOS -name "preloader_*.log" -print | sort | tail -1)
echo "$latest"
[ -n "$latest" ] && cat "$latest"
```

The error fixed by this repository looks like:

```text
System.Reflection.TargetInvocationException:
Exception has been thrown by the target of an invocation.

---> System.DllNotFoundException: libc.so.6

at BepInEx.Preloader.PlatformUtils:uname_linux(...)
at BepInEx.Preloader.PlatformUtils.SetPlatform()
at BepInEx.Preloader.PreloaderRunner.PreloaderPreMain()
```

If you do not see this error, your problem may be different.

---

# Part 6 — Apply this repository's patch

## Step 13 — Clone this repository

Go to the Desktop:

```bash
cd "$HOME/Desktop"
```

Clone:

```bash
git clone https://github.com/Bulubulubuu/Overcooked-2-about-the-BepInEx.git
```

Enter the repository:

```bash
cd Overcooked-2-about-the-BepInEx
```

Check:

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

## Step 14 — Make the patch script executable

Run:

```bash
chmod +x patch_bepinex.sh
```

Check:

```bash
ls -l patch_bepinex.sh
```

Expected permissions include:

```text
-rwxr-xr-x
```

---

## Step 15 — Apply the platform patch

Run this as **one complete command**:

```bash
./patch_bepinex.sh "$HOME/Library/Application Support/Steam/steamapps/common/Overcooked! 2"
```

Do not run the game directory path by itself.

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

# Step 16 — What the patch changes

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

## Step 17 — Restart Steam and launch the game

Completely quit Steam.

Reopen Steam.

Launch Overcooked! 2 normally from Steam.

Wait until the main menu appears, then quit the game.

---

## Step 18 — Check whether BepInEx initialized

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

## Step 19 — Check the final log

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

If these appear, the fix worked.

---

# Part 8 — Install Mods

## Step 20 — Put BepInEx plugins in the plugins folder

Plugin DLL files normally go into:

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

Then restart the game through Steam.

Check:

```bash
tail -100 BepInEx/LogOutput.log
```

This message is normal when no mods are installed:

```text
[Info   :   BepInEx] 0 plugins to load
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

# Important Notes

- This workaround was tested with **BepInEx 5.4.23.4**.
- Do not assume BepInEx 6 works with this guide.
- Do not copy the patched `BepInEx.Preloader.dll` to Linux.
- Keep `BepInEx.Preloader.dll.original`.
- The patch does not modify Overcooked! 2 gameplay.
- The patch only changes BepInEx platform detection.
