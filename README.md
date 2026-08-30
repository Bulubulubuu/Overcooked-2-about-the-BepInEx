# Overcooked! 2 BepInEx Fix for Apple Silicon macOS

This guide explains how to get **BepInEx working with Overcooked! 2 on an Apple Silicon Mac**.
AND HERE THE ZIP IS THE SAME WITH DIRECTORY!

The main problem is that BepInEx may incorrectly detect macOS as Linux and crash with:

```text
System.DllNotFoundException: libc.so.6
at BepInEx.Preloader.PlatformUtils:uname_linux(...)
```

The fix below patches `BepInEx.Preloader.dll` so that the platform is correctly treated as macOS.

---

# Tested Version

This setup was successfully tested with:

```text
Game: Overcooked! 2 (Steam)
CPU: Apple Silicon
Game architecture: x86_64
Rosetta 2: Yes
Unity version: 2017.4.8f1
Unity backend: Mono
BepInEx: 5.4.23.4
macOS: Apple Silicon macOS
```

Successful BepInEx output:

```text
[Message:   BepInEx] BepInEx 5.4.23.4 - Overcooked2
[Info   :   BepInEx] Running under Unity vUnknown (post-2017)
[Info   :   BepInEx] CLR runtime version: 2.0.50727.1433
[Info   :   BepInEx] Supports SRE: True
[Info   :   BepInEx] System platform: Bits64, MacOS
[Message:   BepInEx] Preloader started
[Info   :   BepInEx] Detected Unity version: v2017.4.8f1
[Message:   BepInEx] Chainloader ready
[Message:   BepInEx] Chainloader started
[Message:   BepInEx] Chainloader startup complete
```

---

# Step 1 — Find the Overcooked! 2 directory

The default Steam installation directory is:

```text
~/Library/Application Support/Steam/steamapps/common/Overcooked! 2
```

Open Terminal and run:

```bash
cd "$HOME/Library/Application Support/Steam/steamapps/common/Overcooked! 2"
```

Check the directory:

```bash
ls
```

You should see something similar to:

```text
BepInEx
doorstop_libs
Overcooked2.app
changelog.txt
libdoorstop.dylib
run_bepinex.sh
```

---

# Step 2 — Check the game architecture

Run:

```bash
file Overcooked2.app/Contents/MacOS/Overcooked2
```

Expected output:

```text
Overcooked2.app/Contents/MacOS/Overcooked2: Mach-O 64-bit executable x86_64
```

This means the macOS version of Overcooked! 2 is running as an Intel `x86_64` application.

On Apple Silicon Macs, it therefore runs through Rosetta 2.

---

# Step 3 — Check the Unity version

Run:

```bash
strings Overcooked2.app/Contents/MacOS/Overcooked2 | grep -E "20[0-9][0-9]\.[0-9]+\.[0-9]+f[0-9]+" | head
```

For the tested version, the Unity version is:

```text
2017.4.8f1
```

BepInEx also confirms this after a successful launch:

```text
[Info   :   BepInEx] Detected Unity version: v2017.4.8f1
```

---

# Step 4 — Check that this is a Mono Unity game

Run:

```bash
find Overcooked2.app -name "Assembly-CSharp.dll" -o -name "libmono*.dylib"
```

You should find files similar to:

```text
Overcooked2.app/Contents/Resources/Data/Managed/Assembly-CSharp.dll
Overcooked2.app/Contents/Frameworks/Mono/MonoEmbedRuntime/osx/libmono.0.dylib
```

This confirms that the game uses the Unity Mono runtime.

---

# Step 5 — Install Mono

The patch tool uses Mono and `mcs`.

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

Check the C# compiler:

```bash
mcs --version
```

You should get output similar to:

```text
Mono C# compiler version 6.x
```

---

# Step 6 — Install BepInEx 5

Install the macOS x64 version of BepInEx 5 into:

```text
~/Library/Application Support/Steam/steamapps/common/Overcooked! 2
```

After installation, the directory should look approximately like this:

```text
Overcooked! 2/
├── BepInEx/
│   └── core/
├── doorstop_libs/
├── Overcooked2.app/
├── changelog.txt
├── libdoorstop.dylib
└── run_bepinex.sh
```

The tested working BepInEx runtime reports:

```text
BepInEx 5.4.23.4
```

---

# Step 7 — Remove macOS quarantine attributes if necessary

Downloaded files may have the macOS quarantine attribute.

From the Overcooked! 2 directory, run:

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

# Step 8 — First launch and identify the error

Configure the Steam launch option.

Steam:

```text
Overcooked! 2
→ Properties
→ General
→ Launch Options
```

Use:

```text
"/Users/YOUR_USERNAME/Library/Application Support/Steam/steamapps/common/Overcooked! 2/run_bepinex.sh" %command%
```

Replace:

```text
YOUR_USERNAME
```

with your own macOS username.

Launch Overcooked! 2 from Steam.

If BepInEx fails, a file such as this may appear:

```text
Overcooked2.app/Contents/MacOS/preloader_XXXXXXXX.log
```

Check the newest log:

```bash
latest=$(find Overcooked2.app/Contents/MacOS -name "preloader_*.log" -print | sort | tail -1)
echo "$latest"
cat "$latest"
```

The relevant error is:

```text
System.Reflection.TargetInvocationException:
Exception has been thrown by the target of an invocation.

---> System.DllNotFoundException: libc.so.6

at BepInEx.Preloader.PlatformUtils:uname_linux(...)
at BepInEx.Preloader.PlatformUtils.SetPlatform()
at BepInEx.Preloader.PreloaderRunner.PreloaderPreMain()
```

This is the problem fixed by this repository.

---

# Step 9 — Clone this repository

Run:

```bash
cd "$HOME/Desktop"
```

Then:

```bash
git clone https://github.com/Bulubulubuu/Overcooked-2-about-the-BepInEx.git
```

Enter the repository:

```bash
cd Overcooked-2-about-the-BepInEx
```

Check the files:

```bash
ls
```

You should see:

```text
README.md
patch_platform.cs
patch_bepinex.sh
examples
```

---

# Step 10 — Apply the BepInEx platform patch

Run:

```bash
unzip Overcooked2-BepInEx-macOS-fix.zip
cd Overcooked-2-about-the-BepInEx
./patch_bepinex.sh \
"$HOME/Library/Application Support/Steam/steamapps/common/Overcooked! 2"
```

The script patches:

```text
BepInEx/core/BepInEx.Preloader.dll
```

and keeps a backup:

```text
BepInEx/core/BepInEx.Preloader.dll.original
```

The patch changes the relevant platform value from:

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

The original IL contains:

```text
0091: ldloc.2
0092: ldstr unix
0097: callvirt System.Boolean System.String::Contains(System.String)
009C: brfalse.s IL_00a4
009E: ldc.i4 137
00A3: stloc.0
```

After patching:

```text
0091: ldloc.2
0092: ldstr unix
0097: callvirt System.Boolean System.String::Contains(System.String)
009C: brfalse.s IL_00a4
009E: ldc.i4 73
00A3: stloc.0
```

---

# Step 11 — Restart Steam

Completely quit Steam.

Then reopen Steam.

Launch Overcooked! 2 normally from the Steam client.

Do not start the game directly from Terminal for normal use.

Wait until the game reaches the main menu.

Then quit the game.

---

# Step 12 — Verify BepInEx initialization

Return to the game directory:

```bash
cd "$HOME/Library/Application Support/Steam/steamapps/common/Overcooked! 2"
```

Run:

```bash
find BepInEx -maxdepth 2 -print
```

A successful installation should now contain:

```text
BepInEx/cache
BepInEx/patchers
BepInEx/config
BepInEx/config/BepInEx.cfg
BepInEx/plugins
BepInEx/LogOutput.log
```

This is a very important sign that BepInEx initialized correctly.

---

# Step 13 — Check the BepInEx log

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
Preloader started
Preloader finished
Detected Unity version: v2017.4.8f1
Chainloader ready
Chainloader started
Chainloader startup complete
```

If these lines appear, BepInEx is working.

---

# Step 14 — Install BepInEx plugins

Once BepInEx is working, plugin DLL files should normally be placed in:

```text
BepInEx/plugins/
```

For example:

```text
Overcooked! 2/
└── BepInEx/
    └── plugins/
        └── ExamplePlugin.dll
```

Restart the game from Steam.

Then check:

```bash
tail -100 BepInEx/LogOutput.log
```

Before adding any plugins, this is normal:

```text
[Info   :   BepInEx] 0 plugins to load
```

With a valid plugin installed, BepInEx should report that one or more plugins are being loaded.

---

# Why does this happen?

The relevant BepInEx method is:

```text
BepInEx.Preloader.PlatformUtils.SetPlatform()
```

On this particular Apple Silicon + Rosetta + old Unity Mono configuration, the runtime may report the operating system as:

```text
Unix
```

The BepInEx platform-detection code then classifies it as Linux:

```text
Unix
↓
Linux
↓
uname_linux()
↓
libc.so.6
```

But macOS does not provide Linux's:

```text
libc.so.6
```

so the preloader crashes.

The workaround changes this affected platform path to macOS:

```text
Unix
↓
MacOS
↓
uname_osx()
```

After the patch, BepInEx correctly reports:

```text
System platform: Bits64, MacOS
```

---

# Warning

This patch is intended for this macOS configuration.

It changes an affected Unix/Linux platform branch to macOS.

**Do not copy the patched `BepInEx.Preloader.dll` to a Linux machine.**

The patch script keeps the original DLL as:

```text
BepInEx.Preloader.dll.original
```

---

# Restore the original BepInEx Preloader

If you need to undo the patch, go to the game directory:

```bash
cd "$HOME/Library/Application Support/Steam/steamapps/common/Overcooked! 2"
```

Then run:

```bash
cp \
BepInEx/core/BepInEx.Preloader.dll.original \
BepInEx/core/BepInEx.Preloader.dll
```

The original preloader is now restored.

---

# Troubleshooting

## Error: `libc.so.6`

If you still see:

```text
System.DllNotFoundException: libc.so.6
```

and:

```text
BepInEx.Preloader.PlatformUtils:uname_linux
```

the active `BepInEx.Preloader.dll` is probably not patched, or Steam is using a different BepInEx installation.

---

## `BepInEx/plugins` does not exist

If these do not exist:

```text
BepInEx/plugins
BepInEx/config
BepInEx/LogOutput.log
```

the BepInEx chainloader probably did not finish initialization.

Check:

```bash
find Overcooked2.app/Contents/MacOS -name "preloader_*.log" -print
```

---

## `0 plugins to load`

This message:

```text
[Info   :   BepInEx] 0 plugins to load
```

is not an error.

It simply means:

```text
BepInEx/plugins/
```

does not currently contain a compatible plugin.

---

## HarmonyX `isBatchMode` warning

You may see:

```text
[Warning: HarmonyX] AccessTools.Property: Could not find property for type UnityEngine.Application and name isBatchMode
```

With the tested Unity `2017.4.8f1` setup, this warning was not fatal.

BepInEx still successfully reached:

```text
Chainloader startup complete
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

It does **not** include:

```text
Overcooked2.app
game assets
Steam game files
BepInEx binaries
Unity DLLs
modified game files
```

You must install Overcooked! 2 and BepInEx separately.

---

# Confirmed Working Result

The final tested log was:

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
[Warning:  HarmonyX] AccessTools.Property: Could not find property for type UnityEngine.Application and name isBatchMode
[Info   :   BepInEx] Detected Unity version: v2017.4.8f1
[Message:   BepInEx] Chainloader ready
[Message:   BepInEx] Chainloader started
[Info   :   BepInEx] 0 plugins to load
[Message:   BepInEx] Chainloader startup complete
```

If your log reaches:

```text
Chainloader startup complete
```

the BepInEx installation is working.
