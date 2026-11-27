#!/bin/bash
set -euo pipefail
cd "$(dirname "$0")"
GAME="${OC2_GAME_DIR:-$HOME/Library/Application Support/Steam/steamapps/common/Overcooked! 2}"
MANAGED="$GAME/Overcooked2.app/Contents/Resources/Data/Managed"
mcs -target:library -nostdlib -warnaserror -out:CustomLevelFloorShaderFix.dll \
  -r:"$MANAGED/mscorlib.dll" \
  -r:"$MANAGED/System.dll" \
  -r:"$MANAGED/System.Core.dll" \
  -r:"$GAME/BepInEx/core/BepInEx.dll" \
  -r:"$MANAGED/UnityEngine.dll" \
  -r:"$MANAGED/UnityEngine.CoreModule.dll" \
  -r:"$MANAGED/UnityEngine.SpriteMaskModule.dll" \
  -r:"$MANAGED/UnityEngine.ParticleSystemModule.dll" \
  -r:"$MANAGED/UnityEngine.AssetBundleModule.dll" \
  -r:"$MANAGED/UnityEngine.UI.dll" \
  CustomLevelFloorShaderFix.cs Maintenance.cs RepairPlanStore.cs SettingsBridge.cs LocalFloorFallback.cs Language.cs TestFloorSource.cs SceneCatalog.cs StarCupProfile.cs SavedRulesOnly.cs
