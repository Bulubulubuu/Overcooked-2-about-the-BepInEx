# Overcooked-2-about-the-BepInEx
BepInEx macOS fix for Overcooked! 2 on Apple Silicon Macs.
# Overcooked! 2 BepInEx Fix for Apple Silicon macOS
A small workaround for running BepInEx with **Overcooked! 2** on Apple Silicon Macs under Rosetta.
This fixes a BepInEx preloader crash where macOS is incorrectly detected as Linux/Unix and BepInEx attempts to load:
```text
libc.so.6

which causes the preloader to fail before the BepInEx chainloader starts.

Tested environment

This workaround was tested with:

* Apple Silicon Mac
* macOS
* Steam version of Overcooked! 2
* Overcooked! 2 executable: x86_64
* Rosetta 2
* Unity 2017.4.8f1
* Mono runtime
* BepInEx 5.4.23.4

Successful startup was confirmed with:

[Info   :   BepInEx] System platform: Bits64, MacOS
[Message:   BepInEx] Preloader started
[Message:   BepInEx] Preloader finished
[Info   :   BepInEx] Detected Unity version: v2017.4.8f1
[Message:   BepInEx] Chainloader ready
[Message:   BepInEx] Chainloader started
[Message:   BepInEx] Chainloader startup complete

Problem

Without the patch, BepInEx may fail during preloader initialization with an error similar to:

System.Reflection.TargetInvocationException:
Exception has been thrown by the target of an invocation.
---> System.DllNotFoundException: libc.so.6
at BepInEx.Preloader.PlatformUtils:uname_linux(...)
at BepInEx.Preloader.PlatformUtils.SetPlatform()
at BepInEx.Preloader.PreloaderRunner.PreloaderPreMain()

In this configuration, Mono reports the operating system as:

Unix

BepInEx then classifies the platform as Linux and later executes:

uname_linux()

which tries to resolve Linux’s:

libc.so.6

On macOS this library does not exist, so the BepInEx preloader crashes.

The patch

Inside:

BepInEx.Preloader.dll

the relevant part of PlatformUtils.SetPlatform() originally contains:

0091: ldloc.2
0092: ldstr unix
0097: callvirt System.Boolean System.String::Contains(System.String)
009C: brfalse.s IL_00a4
009E: ldc.i4 137
00A3: stloc.0

Here:

137 = Linux
73  = macOS

The workaround changes:

009E: ldc.i4 137

to:

009E: ldc.i4 73

This causes the affected Unix platform-detection path to be treated as macOS instead of Linux.

After the patch, BepInEx correctly reports:

System platform: Bits64, MacOS

Important warning

This is a macOS-specific workaround.

The patch changes the Unix/Linux platform-detection branch to macOS, so the patched DLL should not be reused on Linux.

Keep the original DLL backup.

Requirements

You need:

* BepInEx 5.x installed in the Overcooked! 2 game directory
* Mono installed
* mcs
* mono
* Mono.Cecil.dll

You can check Mono with:

mono --version

and:

mcs --version

Installation

First install BepInEx normally into the Overcooked! 2 directory.

A typical Steam installation path is:

~/Library/Application Support/Steam/steamapps/common/Overcooked! 2

The directory should contain something similar to:

Overcooked! 2/
├── BepInEx/
│   └── core/
│       ├── BepInEx.Preloader.dll
│       └── Mono.Cecil.dll
├── Overcooked2.app
├── run_bepinex.sh
└── libdoorstop.dylib

Clone this repository:

git clone https://github.com/Bulubulubuu/Overcooked-2-about-the-BepInEx.git
cd Overcooked-2-about-the-BepInEx

Then run:

./patch_bepinex.sh \
"$HOME/Library/Application Support/Steam/steamapps/common/Overcooked! 2"

The script will:

1. Locate BepInEx.Preloader.dll.
2. Create a backup if one does not already exist.
3. Compile patch_platform.cs.
4. Patch the platform-detection constant.
5. Replace the active preloader DLL with the patched version.

Manual usage

You can also compile the patcher manually.

From this repository:

mcs \
-r:"/path/to/BepInEx/core/Mono.Cecil.dll" \
-out:"patch_platform.exe" \
patch_platform.cs

Then run:

MONO_PATH="/path/to/BepInEx/core" \
mono patch_platform.exe \
"/path/to/BepInEx/core/BepInEx.Preloader.dll"

Steam launch option

Configure the Steam launch option to use the BepInEx startup script.

Example:

"/Users/YOUR_USERNAME/Library/Application Support/Steam/steamapps/common/Overcooked! 2/run_bepinex.sh" %command%

Replace YOUR_USERNAME with your own macOS username.

Then fully restart Steam and launch Overcooked! 2 normally from Steam.

Verify that BepInEx works

After launching the game once, the following directories/files should be created:

BepInEx/cache
BepInEx/config
BepInEx/patchers
BepInEx/plugins
BepInEx/LogOutput.log

Check the log:

tail -100 BepInEx/LogOutput.log

A successful startup should contain:

System platform: Bits64, MacOS
Preloader started
Preloader finished
Detected Unity version: v2017.4.8f1
Chainloader ready
Chainloader started
Chainloader startup complete

If you see:

0 plugins to load

that is normal if the BepInEx/plugins directory is empty.

You can then place compatible BepInEx plugin DLLs inside:

BepInEx/plugins/

Restoring the original DLL

The patcher creates a backup named:

BepInEx.Preloader.dll.original

To restore it manually:

cp \
"BepInEx/core/BepInEx.Preloader.dll.original" \
"BepInEx/core/BepInEx.Preloader.dll"

Troubleshooting

libc.so.6 still appears

If the preloader log still contains:

BepInEx.Preloader.PlatformUtils:uname_linux
System.DllNotFoundException: libc.so.6

verify that the active preloader DLL was actually patched.

The patched IL should contain:

009E: ldc.i4 73

instead of:

009E: ldc.i4 137

Also make sure Steam is using the same BepInEx installation that you patched.

BepInEx starts but no mods load

Check:

BepInEx/LogOutput.log

If it says:

0 plugins to load

then BepInEx itself is working, but there are no compatible plugin DLLs in:

BepInEx/plugins/

HarmonyX warning about isBatchMode

A warning similar to:

AccessTools.Property: Could not find property for type UnityEngine.Application and name isBatchMode

may appear with this older Unity version.

In the tested setup, BepInEx continued successfully to:

Chainloader startup complete

so this warning was not fatal.

Repository contents

.
├── README.md
├── patch_platform.cs
├── patch_bepinex.sh
├── .gitignore
└── examples/
    ├── error-log.txt
    └── success-log.txt

What is not included

This repository does not include:

* Overcooked! 2 game files
* Overcooked2.app
* Unity assemblies
* BepInEx binaries
* Steam files
* Modified game assets

Install Overcooked! 2 and BepInEx separately.

Notes

This workaround was created specifically to address the BepInEx platform-detection issue observed with Overcooked! 2 running as an Intel x86_64 Unity Mono application under Rosetta on Apple Silicon macOS.

It may also be useful for investigating similar BepInEx issues in other older Unity Mono games, but compatibility with other games or BepInEx versions is not guaranteed.

Disclaimer

Use this patch at your own risk.

Always keep a backup of the original:

BepInEx.Preloader.dll

before applying modifications.
