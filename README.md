# 🍳 Overcooked! 2 — BepInEx Fix for macOS (Apple Silicon)

A simple, beginner-friendly guide to installing BepInEx for **Overcooked! 2 on Apple Silicon Macs (M1 / M2 / M3 / M4 and newer)**.

**No coding experience required!**

---

## 🛠 What Does This Fix?

Some Mac users have trouble getting BepInEx to work with Overcooked! 2.

Common problems include:

- BepInEx does not start.
- BepInEx fails to load on macOS.
- `libc.so.6` errors.
- The game starts, but BepInEx does not load.
- The `BepInEx/plugins` folder is missing.

This repository provides a prepared BepInEx installation package with a macOS compatibility fix.

**You do NOT need to:**

- ❌ Install Homebrew
- ❌ Install Mono
- ❌ Download BepInEx separately
- ❌ Compile or modify any files
- ❌ Run patch scripts

Just download, copy, and follow the steps below!

---

# 🚀 Installation Guide

## Step 1 — Download the Files

1. Open this repository.
2. Find the provided BepInEx installation ZIP file.
3. Click the ZIP file.
4. Click **Download**.
5. Double-click the downloaded ZIP to extract it.

You should now have a folder containing the prepared BepInEx files.

> ⚠️ Download the BepInEx installation ZIP provided in this repository, not the entire repository using **Code → Download ZIP**.

**Once extracted, continue to Step 2.**

---

## Step 2 — Open Your Game Folder

1. Open **Steam**.
2. Go to **Library**.
3. Right-click **Overcooked! 2**.
4. Select **Manage → Browse local files**.

Finder will open your game folder.

You should see:

```text
Overcooked2.app
```

**Keep this Finder window open.**

---

## Step 3 — Copy the Files

1. Open the folder you extracted in Step 1.
2. Select the BepInEx installation files inside it.
3. Copy them into the game folder opened in Step 2.

Your game folder should look similar to this:

```text
Overcooked! 2/
│
├── Overcooked2.app
├── BepInEx/
├── libdoorstop.dylib
└── run_bepinex.sh
```

**Important:**

- Copy the files **next to** `Overcooked2.app`.
- Do NOT put them inside `Overcooked2.app`.
- Do NOT delete `Overcooked2.app`.
- Do NOT create an extra folder containing all the BepInEx files.

If you already have BepInEx installed, back up your existing `BepInEx` folder before replacing it. In particular, save your `plugins` and `config` folders.

> 💡 Some packages may not contain a `doorstop_libs` folder. This is normal if you are using the complete package provided here.

**Files copied? Continue to Step 4.**

---

## Step 4 — Open Terminal

Don't worry! You only need to copy and paste one command.

1. Press **Command (⌘) + Space**.
2. Type `Terminal`.
3. Press **Enter**.

A Terminal window will open.

---

## Step 5 — Enable BepInEx

Copy the following command:

```bash
chmod +x "$HOME/Library/Application Support/Steam/steamapps/common/Overcooked! 2/run_bepinex.sh"
```

Paste it into Terminal and press **Enter**.

### ✅ If nothing happens

That's normal!

The command usually produces no output when successful.

**Continue to Step 6.**

### ❌ If you see:

```text
No such file or directory
```

The game may be installed in a different location, or the files were not copied correctly.

Go back to Step 2 and check your game folder.

---

## Step 6 — Get Your Steam Launch Command

Copy this command into Terminal:

```bash
echo "\"$HOME/Library/Application Support/Steam/steamapps/common/Overcooked! 2/run_bepinex.sh\" %command%"
```

Press **Enter**.

Terminal will display something similar to:

```text
"/Users/yourname/Library/Application Support/Steam/steamapps/common/Overcooked! 2/run_bepinex.sh" %command%
```

**Copy the entire line that Terminal displays.**

You do NOT need to change `yourname` manually.

The command automatically uses your Mac username.

---

## Step 7 — Set Steam Launch Options

1. Open **Steam**.
2. Go to **Library**.
3. Right-click **Overcooked! 2**.
4. Click **Properties**.
5. Select **General**.
6. Find **Launch Options**.
7. Paste the command you copied in Step 6.

It should look similar to:

```text
"/Users/yourname/Library/Application Support/Steam/steamapps/common/Overcooked! 2/run_bepinex.sh" %command%
```

⚠️ **Important:**

Make sure the command ends with:

```text
%command%
```

Keep the quotation marks around the file path.

**Done? Continue to Step 8.**

---

## Step 8 — Restart Steam and Launch the Game

1. Completely quit Steam using **Steam → Quit Steam**.
2. Open Steam again.
3. Go to your Library.
4. Click **Play** on Overcooked! 2.

**Always launch the game through Steam when using this setup.**

If the game opens successfully, continue to the next step.

---

# ✅ Check If BepInEx Is Working

## Step 9 — Check the BepInEx Log

Open your game folder again:

**Steam → Library → Overcooked! 2 → Manage → Browse local files**

Open:

```text
BepInEx/
```

Look for:

```text
LogOutput.log
```

Open it with a text editor.

If you see:

```text
Preloader started
Preloader finished
Chainloader ready
Chainloader started
Chainloader startup complete
```

🎉 **Congratulations! BepInEx is working!**

### What if you see `0 plugins to load`?

Don't worry!

```text
0 plugins to load
```

is NOT an error.

It simply means BepInEx is running, but you haven't installed any mods yet.

---

# 🎮 How to Install Mods

1. Download a mod that is compatible with your game and BepInEx version.
2. Open your Overcooked! 2 game folder.
3. Open:

```text
BepInEx/plugins/
```

4. Copy the mod files into this folder.
5. Restart Overcooked! 2 through Steam.

Some mods have additional installation instructions. Follow the instructions provided by the mod creator.

---

# ❓ Troubleshooting

## 1. I can't find `doorstop_libs`

This is usually not a problem.

Some BepInEx packages use a different folder structure.

Make sure you have:

```text
BepInEx/
libdoorstop.dylib
run_bepinex.sh
Overcooked2.app
```

If these files are present, continue with the installation.

---

## 2. Terminal says `No such file or directory`

Your game might not be installed in the default Steam location.

To find the correct location:

1. Open Steam.
2. Right-click Overcooked! 2.
3. Select **Manage → Browse local files**.
4. Check that `run_bepinex.sh` is in the same folder as `Overcooked2.app`.

If your game is installed elsewhere, replace the default path in the Terminal commands with the actual path.

---

## 3. macOS blocks the downloaded files

If macOS blocks BepInEx, first make sure the files came from a source you trust.

Then open Terminal and run:

```bash
cd "$HOME/Library/Application Support/Steam/steamapps/common/Overcooked! 2"
```

Next, run:

```bash
xattr -dr com.apple.quarantine BepInEx 2>/dev/null || true
xattr -d com.apple.quarantine libdoorstop.dylib 2>/dev/null || true
xattr -d com.apple.quarantine run_bepinex.sh 2>/dev/null || true
```

Restart Steam and try again.

---

## 4. The game opens, but BepInEx doesn't load

First, check that your Steam Launch Options are correct.

Next, open Terminal and run:

```bash
grep '^executable_name=' "$HOME/Library/Application Support/Steam/steamapps/common/Overcooked! 2/run_bepinex.sh"
```

You should see:

```text
executable_name="Overcooked2.app"
```

If you see something different, run:

```bash
sed -i '' 's/^executable_name=.*/executable_name="Overcooked2.app"/' "$HOME/Library/Application Support/Steam/steamapps/common/Overcooked! 2/run_bepinex.sh"
```

Then restart Steam and try again.

---

## 5. I see `0 plugins to load`

This is normal.

It means BepInEx started successfully, but no compatible plugins were found.

Check that your mods are installed in:

```text
BepInEx/plugins/
```

---

## 6. I get a `libc.so.6` error

This usually means the macOS compatibility fix did not work correctly.

Make sure you copied the complete BepInEx package provided in this repository.

Do not mix files from different BepInEx versions.

If the problem continues, please open a GitHub Issue and include your BepInEx log.

---

# 🔄 How to Disable BepInEx

If you want to launch Overcooked! 2 without BepInEx:

1. Open Steam.
2. Go to **Library**.
3. Right-click **Overcooked! 2**.
4. Select **Properties → General**.
5. Find **Launch Options**.
6. Remove the BepInEx launch command.

Steam will no longer use the BepInEx launch script.

You do not need to delete the game.

---

# 💬 Need Help?

If you encounter any problems, please open an Issue:

https://github.com/Bulubulubuu/Overcooked-2-about-the-BepInEx/issues

Please include:

- Your Mac model (M1 / M2 / M3 / M4, etc.).
- Your macOS version.
- A screenshot of the error.
- Your `BepInEx/LogOutput.log` file, if available.

Please remove personal information from logs before sharing them.

---

# ⚠️ Disclaimer

This is an unofficial community guide.

This project is not affiliated with Team17, Steam, Unity, or the BepInEx developers.

The game itself is not included.

You must own a legitimate copy of Overcooked! 2.

Use third-party mods at your own risk.
