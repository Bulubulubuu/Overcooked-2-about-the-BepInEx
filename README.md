# 🍳 Overcooked! 2 — BepInEx Fix for Apple Silicon macOS

This guide is for **Overcooked! 2 on macOS**, especially Apple Silicon Macs (M1 / M2 / M3 / M4).

No programming experience is required.

The required BepInEx files are already included in the ZIP file in this repository, so you do **not** need to download BepInEx separately.

---

# Before You Start

This guide is intended for:

```text
Game: Overcooked! 2 (Steam)
Computer: Apple Silicon Mac
Game architecture: x86_64
Unity version: 2017.4.8f1
Unity backend: Mono
BepInEx: 5.4.23.x
```

> ⚠️ This fix is specifically intended for the macOS version of Overcooked! 2 described above.
>
> Other game versions or BepInEx versions may behave differently.

---

# Step 1 — Download the ZIP file

At the top of this GitHub page, find:

```text
Overcooked2-BepInEx-macOS-fix.zip
```

Click the file and download it.

After downloading, double-click the ZIP file to extract it.

You should now have a folder containing the BepInEx files.

---

# Step 2 — Open the Overcooked! 2 game folder

Open Steam.

Go to:

```text
Library
→ Right-click Overcooked! 2
→ Manage
→ Browse local files
```

Finder should open the Overcooked! 2 folder.

You should see:

```text
Overcooked2.app
```

⚠️ Keep this Finder window open.

---

# Step 3 — Copy the files into the game folder

Open the folder you extracted in **Step 1**.

Copy the included BepInEx files into the same folder as:

```text
Overcooked2.app
```

After copying, the important files should look similar to:

```text
Overcooked! 2/
├── BepInEx/
├── Overcooked2.app/
├── libdoorstop.dylib
└── run_bepinex.sh
```

Depending on the package, you may also see files such as:

```text
doorstop_libs/
changelog.txt
```

If you do not see `doorstop_libs`, do not worry.

The important files are:

```text
BepInEx
Overcooked2.app
libdoorstop.dylib
run_bepinex.sh
```

---

# Step 4 — Open Terminal

Open:

```text
Finder
→ Applications
→ Utilities
→ Terminal
```

Or press:

```text
Command + Space
```

search for:

```text
Terminal
```

and press Enter.

---

# Step 5 — Go to the Overcooked! 2 folder

Copy the following command into Terminal:

```bash
cd "$HOME/Library/Application Support/Steam/steamapps/common/Overcooked! 2"
```

Press **Enter**.

Then type:

```bash
ls
```

You should be able to see:

```text
BepInEx
Overcooked2.app
libdoorstop.dylib
run_bepinex.sh
```

If you can see these files, continue to the next step.

---

# Step 6 — Check Homebrew

Copy this command:

```bash
brew --version
```

Press Enter.

## If you see something like:

```text
Homebrew 4.x.x
```

Homebrew is already installed.

👉 Skip to **Step 8**.

## If you see:

```text
command not found: brew
```

Homebrew is not installed.

Continue to **Step 7**.

---

# Step 7 — Install Homebrew

Go to the official Homebrew website:

https://brew.sh

Copy the installation command shown on the website and paste it into Terminal.

Press Enter.

During installation, macOS may ask for your computer password.

When typing your password in Terminal:

```text
Nothing will appear on the screen.
```

This is normal.

Type your password and press Enter.

When Homebrew finishes installing, close Terminal and open it again.

Then run:

```bash
brew --version
```

If you now see:

```text
Homebrew 4.x.x
```

continue to the next step.

---

# Step 8 — Check Mono

Run:

```bash
mono --version
```

## If you see something similar to:

```text
Mono JIT compiler version 6.x
```

Mono is already installed.

👉 Continue to **Step 9**.

## If you see:

```text
command not found: mono
```

install Mono with:

```bash
brew install mono
```

Wait for the installation to finish.

Then check again:

```bash
mono --version
```

---

# Step 9 — Give BepInEx permission to run

Copy and run:

```bash
chmod +x "$HOME/Library/Application Support/Steam/steamapps/common/Overcooked! 2/run_bepinex.sh"
```

There may be no message after running this command.

That is normal.

---

# Step 10 — Remove macOS quarantine

macOS may block downloaded BepInEx files.

First make sure you are inside the game folder:

```bash
cd "$HOME/Library/Application Support/Steam/steamapps/common/Overcooked! 2"
```

Then copy and run these commands:

```bash
xattr -dr com.apple.quarantine BepInEx 2>/dev/null || true
xattr -d com.apple.quarantine libdoorstop.dylib 2>/dev/null || true
xattr -d com.apple.quarantine run_bepinex.sh 2>/dev/null || true
```

No output is normal.

---

# Step 11 — Tell BepInEx which game to launch

This step is important.

Copy the entire command below:

```bash
sed -i '' 's/^executable_name=.*/executable_name="Overcooked2.app"/' "$HOME/Library/Application Support/Steam/steamapps/common/Overcooked! 2/run_bepinex.sh"
```

Press Enter.

Now check it:

```bash
grep '^executable_name=' "$HOME/Library/Application Support/Steam/steamapps/common/Overcooked! 2/run_bepinex.sh"
```

You should see:

```text
executable_name="Overcooked2.app"
```

✅ If you see this, continue.

---

# Step 12 — Apply the macOS BepInEx fix

Now we need to apply the compatibility fix included in this repository.

Go to the folder where you downloaded this repository.

If you downloaded the repository to your Desktop, open Terminal and use:

```bash
cd "$HOME/Desktop/Overcooked-2-about-the-BepInEx"
```

If your folder has a different name or is somewhere else, you can also type:

```bash
cd 
```

with a space after `cd`, then drag the repository folder into the Terminal window and press Enter.

Now run:

```bash
chmod +x patch_bepinex.sh
```

Then run:

```bash
./patch_bepinex.sh "$HOME/Library/Application Support/Steam/steamapps/common/Overcooked! 2"
```

Wait for the patch to finish.

⚠️ Do not close Terminal while the patch is running.

---

# Step 13 — Check the patch result

After the patch finishes, run:

```bash
ls -lh "$HOME/Library/Application Support/Steam/steamapps/common/Overcooked! 2/BepInEx/core/BepInEx.Preloader.dll"*
```

The important thing is:

```text
BepInEx.Preloader.dll
```

must **NOT** show:

```text
0B
```

If it shows a normal file size such as:

```text
42K
```

you can continue.

### If `BepInEx.Preloader.dll` shows `0B`

Do **not** start the game yet.

If this file exists:

```text
BepInEx.Preloader.dll.patched
```

you can restore it with:

```bash
cp "$HOME/Library/Application Support/Steam/steamapps/common/Overcooked! 2/BepInEx/core/BepInEx.Preloader.dll.patched" \
"$HOME/Library/Application Support/Steam/steamapps/common/Overcooked! 2/BepInEx/core/BepInEx.Preloader.dll"
```

Then check the file size again.

---

# Step 14 — Configure Steam Launch Options

Now open Steam.

Go to:

```text
Library
→ Right-click Overcooked! 2
→ Properties
→ General
→ Launch Options
```

We need your macOS username.

In Terminal, run:

```bash
whoami
```

For example, if Terminal prints:

```text
jennylyu
```

your Steam Launch Option should be:

```text
"/Users/jennylyu/Library/Application Support/Steam/steamapps/common/Overcooked! 2/run_bepinex.sh" %command%
```

Replace `jennylyu` with **your own username**.

You can also generate the correct line automatically by running:

```bash
echo "\"$HOME/Library/Application Support/Steam/steamapps/common/Overcooked! 2/run_bepinex.sh\" %command%"
```

Copy the output and paste it into Steam Launch Options.

⚠️ Important:

```text
Keep the quotation marks.
Keep the space before %command%.
```

The end must be:

```text
%command%
```

NOT:

```text
%command/%
```

---

# Step 15 — Restart Steam

Completely quit Steam.

Do not only close the Steam window.

Use:

```text
Steam
→ Quit Steam
```

Then reopen Steam.

---

# Step 16 — Start Overcooked! 2

Launch **Overcooked! 2 from Steam normally**.

Do not launch `Overcooked2.app` directly from Finder for this test.

Wait until the game reaches the main menu.

Then quit the game.

---

# Step 17 — Check whether BepInEx worked

Open Terminal and run:

```bash
cd "$HOME/Library/Application Support/Steam/steamapps/common/Overcooked! 2"
```

Then:

```bash
ls BepInEx
```

A successful first launch may create folders/files such as:

```text
cache
config
patchers
plugins
LogOutput.log
```

Now check the log:

```bash
tail -100 BepInEx/LogOutput.log
```

A successful startup should contain lines similar to:

```text
Preloader started
Preloader finished
Chainloader ready
Chainloader started
Chainloader startup complete
```

If you see:

```text
0 plugins to load
```

that is **NOT an error**.

It simply means BepInEx is working but you have not installed any mods yet.

🎉 BepInEx is now running.

---

# Step 18 — Install Mods

BepInEx plugins normally go into:

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

After adding a plugin, restart Overcooked! 2 from Steam.

Then check:

```bash
tail -100 "$HOME/Library/Application Support/Steam/steamapps/common/Overcooked! 2/BepInEx/LogOutput.log"
```

---

# Troubleshooting

## `doorstop_libs` is missing

Some packages may not contain a separate:

```text
doorstop_libs/
```

folder.

Check whether your game folder contains:

```text
BepInEx/
libdoorstop.dylib
run_bepinex.sh
Overcooked2.app/
```

If these files are present, continue with the guide.

---

## `0 plugins to load`

This is normal:

```text
[Info   : BepInEx] 0 plugins to load
```

It means BepInEx started successfully but there are currently no plugins inside:

```text
BepInEx/plugins/
```

---

## HarmonyX `isBatchMode` warning

You may see:

```text
[Warning: HarmonyX] AccessTools.Property:
Could not find property for type UnityEngine.Application
and name isBatchMode
```

If the log later reaches:

```text
Chainloader startup complete
```

BepInEx has completed startup.

---

## `libc.so.6` error

You may see:

```text
System.DllNotFoundException: libc.so.6
```

or:

```text
BepInEx.Preloader.PlatformUtils:uname_linux
```

This is the compatibility problem that this repository attempts to fix.

Do not repeatedly run the patch if it fails.

Check the size of:

```bash
ls -lh "$HOME/Library/Application Support/Steam/steamapps/common/Overcooked! 2/BepInEx/core/BepInEx.Preloader.dll"*
```

and open an Issue with the output.

---

## `BadImageFormatException`

If you see:

```text
System.BadImageFormatException:
Format of the executable (.exe) or library (.dll) is invalid.
```

check:

```bash
file "$HOME/Library/Application Support/Steam/steamapps/common/Overcooked! 2/BepInEx/core/BepInEx.Preloader.dll"
```

and:

```bash
ls -lh "$HOME/Library/Application Support/Steam/steamapps/common/Overcooked! 2/BepInEx/core/BepInEx.Preloader.dll"*
```

This may mean the DLL is damaged, empty, or the installed BepInEx version is different from the version expected by the patch.

---

## BepInEx reports `Bits64, iOS`

Some BepInEx / MonoMod.Utils versions may use different platform enum values.

If your log says:

```text
System platform: Bits64, iOS
```

but still reaches:

```text
Preloader finished
Chainloader started
Chainloader startup complete
```

please save your:

```text
BepInEx/LogOutput.log
```

and report the BepInEx version when opening an Issue.

Do not repeatedly patch the DLL.

---

# How to Restore the Original Preloader

The patch tool may create backup files such as:

```text
BepInEx.Preloader.dll.original
BepInEx.Preloader.dll.patched
```

If you need to restore the original:

```bash
cd "$HOME/Library/Application Support/Steam/steamapps/common/Overcooked! 2/BepInEx/core"
```

Then:

```bash
cp BepInEx.Preloader.dll.original BepInEx.Preloader.dll
```

Check:

```bash
ls -lh BepInEx.Preloader.dll*
```

---

# Important Notes

- This project is for the **Steam macOS version of Overcooked! 2**.
- The game itself is **not included**.
- You must own and install Overcooked! 2 through Steam.
- Do not delete `Overcooked2.app`.
- Do not repeatedly run the patch when an error occurs.
- Always keep a backup of the original `BepInEx.Preloader.dll`.
- Different BepInEx versions may behave differently.
- If `Chainloader startup complete` appears, BepInEx has completed its startup process.

---

# Repository Files

```text
Overcooked2-BepInEx-macOS-fix.zip
patch_bepinex.sh
patch_platform.cs
README.md
examples/
```

`Overcooked2-BepInEx-macOS-fix.zip`

Contains the files prepared for this installation guide.

`patch_bepinex.sh`

Runs the macOS compatibility patch.

`patch_platform.cs`

Contains the patch logic used by the shell script.

`examples/`

Contains example logs for troubleshooting.

---

# Need Help?

If the installation does not work, please open a GitHub Issue.

Please include the output of:

```bash
file "$HOME/Library/Application Support/Steam/steamapps/common/Overcooked! 2/Overcooked2.app/Contents/MacOS/Overcooked2"
```

and:

```bash
ls -lh "$HOME/Library/Application Support/Steam/steamapps/common/Overcooked! 2/BepInEx/core/BepInEx.Preloader.dll"*
```

If this file exists, also include:

```text
BepInEx/LogOutput.log
```

This makes troubleshooting much easier.

---

# Disclaimer

This is an unofficial community fix.

Overcooked! 2, Team17, BepInEx, Unity, Steam, and other mentioned projects belong to their respective owners.

Use this project at your own risk.
