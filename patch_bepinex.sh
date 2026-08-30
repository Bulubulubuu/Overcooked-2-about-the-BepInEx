#!/bin/bash

set -e

if [ -z "$1" ]; then
    echo "Usage:"
    echo "./patch_bepinex.sh \"/path/to/Overcooked! 2\""
    exit 1
fi

GAME_DIR="$1"
CORE_DIR="$GAME_DIR/BepInEx/core"
PRELOADER="$CORE_DIR/BepInEx.Preloader.dll"
CECIL="$CORE_DIR/Mono.Cecil.dll"

if [ ! -f "$PRELOADER" ]; then
    echo "ERROR: BepInEx.Preloader.dll not found."
    exit 1
fi

if [ ! -f "$CECIL" ]; then
    echo "ERROR: Mono.Cecil.dll not found."
    exit 1
fi

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"

echo "Compiling patcher..."

mcs \
-r:"$CECIL" \
-out:"$SCRIPT_DIR/patch_platform.exe" \
"$SCRIPT_DIR/patch_platform.cs"

echo "Applying patch..."

MONO_PATH="$CORE_DIR" \
mono "$SCRIPT_DIR/patch_platform.exe" "$PRELOADER"

echo
echo "Done."
echo "Now start Overcooked! 2 through Steam."
