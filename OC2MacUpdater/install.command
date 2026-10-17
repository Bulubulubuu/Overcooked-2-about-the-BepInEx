#!/bin/bash
set -e
SOURCE_DIR="$(cd "$(dirname "$0")" && pwd)"
GAME_DIR="${1:-$HOME/Library/Application Support/Steam/steamapps/common/Overcooked! 2}"
OC2_PYTHON="$(command -v python3 || true)"
if [ -z "$OC2_PYTHON" ] || [ ! -x "$OC2_PYTHON" ]; then
    echo "需要 Python 3；请安装后重新运行。"
    exit 1
fi
if [ ! -f "$GAME_DIR/BepInEx/core/Mono.Cecil.dll" ]; then
    echo "未找到 BepInEx/core/Mono.Cecil.dll，请先安装 BepInEx，或传入正确的游戏路径。"
    exit 1
fi
DEST_DIR="$GAME_DIR/BepInEx/tools/OC2MacUpdater"
mkdir -p "$DEST_DIR"
if [ "$SOURCE_DIR" != "$(cd "$DEST_DIR" && pwd)" ]; then
    for OC2_FILE in update.py update.command CompatPatcher.exe MacUpdateBridge.dll README.md CompatPatcher.cs MacUpdateBridge.cs install.command; do
        cp "$SOURCE_DIR/$OC2_FILE" "$DEST_DIR/$OC2_FILE"
    done
fi
printf '%s\n' "$OC2_PYTHON" > "$DEST_DIR/python.path"
chmod +x "$DEST_DIR/update.command" "$DEST_DIR/install.command"
exec "$OC2_PYTHON" "$DEST_DIR/update.py" --game-dir "$GAME_DIR" --version latest
