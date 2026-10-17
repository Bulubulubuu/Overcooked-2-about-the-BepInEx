#!/bin/bash
set -e
TOOL_DIR="$(cd "$(dirname "$0")" && pwd)"
if [ -f "$TOOL_DIR/python.path" ]; then
    OC2_PYTHON="$(cat "$TOOL_DIR/python.path")"
else
    OC2_PYTHON="$(command -v python3 || true)"
fi
if [ -z "$OC2_PYTHON" ] || [ ! -x "$OC2_PYTHON" ]; then
    echo "未找到 Python 3，请安装后重试。"
    exit 1
fi
exec "$OC2_PYTHON" "$TOOL_DIR/update.py" "$@"
