using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class SavedRulePath
{
    public static bool Decode(string segment, out string name, out int ordinal)
    {
        name = ""; ordinal = -1;
        if (string.IsNullOrEmpty(segment)) return false;
        int marker = segment.LastIndexOf('#');
        if (marker < 0 || !int.TryParse(segment.Substring(marker + 1), out ordinal) || ordinal < 0) return false;
        try { name = Uri.UnescapeDataString(segment.Substring(0, marker)); return name.Length > 0; }
        catch (Exception) { return false; }
    }
}
public sealed partial class CustomLevelFloorShaderFix
{
    bool suppressCapture, historyLoaded;
    bool menuPreparationStarted, menuPreparationFinished;
    IEnumerator PrepareSavedResourcesAtStartup()
    {
        yield return null;
        PrepareStarSteamTexture();
        HashSet<string> prepared = new HashSet<string>(StringComparer.Ordinal);
        bool needsFloor = false;
        foreach (TextureRepairRecord rule in activeRules.Values)
        {
            if (!rule.Ready) continue;
            if (prepared.Add(rule.Scene))
            {
                if (LocateSavedSceneFile(rule.Scene) != null) FingerprintForScene(rule.Scene);
                yield return null;
            }
            if (rule.TargetShader != null && rule.TargetShader.StartsWith(TestMaterialPrefix, StringComparison.Ordinal)) needsFloor = true;
        }
        if (needsFloor) yield return WarmTestFloorMaterials();
        menuPreparationFinished = true;
        Logger.LogInfo("MENU_RESOURCES_PREPARED_ONCE; automatic resource loading disabled until manual check or restart.");
    }
    bool HasSavedRules(string scene)
    {
        foreach (TextureRepairRecord rule in activeRules.Values) if (rule.Scene == scene && rule.Ready) return true;
        return false;
    }
    string LocateSavedSceneFile(string scene)
    {
        string file;
        if (sceneFiles.TryGetValue(scene, out file) && File.Exists(file)) return file;
        if (!Directory.Exists(LevelsRoot) || scene.IndexOfAny(new char[] { '/', '\\', '*', '?' }) >= 0) return null;
        string found = null;
        // Locate only the file named by an existing rule, without reading the full bundle catalog.
        foreach (string candidate in Directory.GetFiles(LevelsRoot, scene, SearchOption.AllDirectories))
        {
            if (TextureSceneCatalog.IsExcluded(LevelsRoot, candidate)) continue;
            if (found != null) { Logger.LogWarning("Saved scene name is ambiguous: " + scene); return null; }
            found = candidate;
        }
        if (found != null) sceneFiles[scene] = found;
        return found;
    }
    static Transform ResolveSavedTransform(Scene scene, string path)
    {
        if (string.IsNullOrEmpty(path)) return null;
        Transform current = null; string[] parts = path.Split('/');
        for (int level = 0; level < parts.Length; level++)
        {
            string name; int ordinal;
            if (!SavedRulePath.Decode(parts[level], out name, out ordinal)) return null;
            Transform next = null; int seen = 0;
            if (level == 0)
            {
                foreach (GameObject root in scene.GetRootGameObjects())
                    if (root.name == name && seen++ == ordinal) { next = root.transform; break; }
            }
            else
                for (int i = 0; i < current.childCount; i++)
                {
                    Transform child = current.GetChild(i);
                    if (child.name == name && seen++ == ordinal) { next = child; break; }
                }
            if (next == null) return null; current = next;
        }
        return current;
    }
    IEnumerator ApplySavedOnce(Scene scene, State state)
    {
        yield return new WaitForSecondsRealtime(0.5f);
        if (!state.active || !scene.isLoaded) yield break;
        while (menuPreparationStarted && !menuPreparationFinished) yield return null;
        if (!state.active || !scene.isLoaded) yield break;
        if (!sceneFiles.ContainsKey(scene.name)) { Logger.LogWarning("Saved rules not applied: scene file not uniquely located; use manual levels check. " + scene.name); yield break; }
        string fingerprint = FingerprintForScene(scene.name);
        List<TextureRepairRecord> rules = new List<TextureRepairRecord>(); bool needsTestFloor = false;
        foreach (TextureRepairRecord rule in activeRules.Values)
            if (rule.Scene == scene.name && TextureRepairStore.Matches(rule, fingerprint))
            { rules.Add(rule); if (rule.TargetShader.StartsWith(TestMaterialPrefix, StringComparison.Ordinal)) needsTestFloor = true; }
        if (rules.Count == 0) yield break;
        if (needsTestFloor && testFloors.Count < 2) Logger.LogWarning("Cached test_1 material unavailable; use manual levels check. " + scene.name);
        if (!state.active || !scene.isLoaded) yield break;
        Shader[] shaders = Resources.FindObjectsOfTypeAll<Shader>();
        Material[] noMaterialSearch = new Material[0]; Stats stats = new Stats();
        HashSet<string> handled = new HashSet<string>(StringComparer.Ordinal);
        suppressCapture = true; state.applyOnly = true;
        try
        {
            foreach (TextureRepairRecord rule in rules)
            {
                if (!handled.Add(rule.ObjectPath)) continue;
                Transform target = ResolveSavedTransform(scene, rule.ObjectPath);
                if (target == null) continue;
                // Only access the recorded object, not all renderers in the level.
                foreach (Renderer renderer in target.GetComponents<Renderer>())
                {
                    try
                    {
                        Kind kind = renderer is ParticleSystemRenderer ? Kind.Particle : renderer is SpriteRenderer ? Kind.Sprite : Kind.Mesh;
                        Material[] a = renderer.sharedMaterials; bool changed = false;
                        if (a.Length == 0 && kind == Kind.Mesh && LocalFloorPolicy.IsFloorName(renderer.name)) a = new Material[1];
                        for (int i = 0; i < a.Length; i++)
                        {
                            Material original = a[i];
                            Material replacement = original == null
                                ? RepairMissingMaterial(scene, state, target, kind, i, noMaterialSearch, stats)
                                : RepairMaterial(scene, state, target, renderer, null, original, kind, shaders, noMaterialSearch, stats);
                            if (replacement == null) continue;
                            state.assignments.Add(new Assignment { renderer = renderer, slot = i, original = original, replacement = replacement });
                            a[i] = replacement; changed = true;
                        }
                        if (changed) renderer.sharedMaterials = a;
                        ParticleSystemRenderer particle = renderer as ParticleSystemRenderer;
                        if (particle != null && particle.trailMaterial != null)
                        {
                            Material original = particle.trailMaterial;
                            Material replacement = RepairMaterial(scene, state, target, renderer, null, original, Kind.Particle, shaders, noMaterialSearch, stats);
                            if (replacement != null) { state.assignments.Add(new Assignment { renderer = renderer, trail = true, original = original, replacement = replacement }); particle.trailMaterial = replacement; }
                        }
                    }
                    catch (Exception e) { Logger.LogWarning("Saved renderer rule skipped: " + e.Message); }
                }
                foreach (Graphic graphic in target.GetComponents<Graphic>())
                {
                    try
                    {
                        Material original = graphic.material;
                        Material replacement = RepairMaterial(scene, state, target, null, graphic, original, Kind.UI, shaders, noMaterialSearch, stats);
                        if (replacement != null) { state.assignments.Add(new Assignment { graphic = graphic, original = original, replacement = replacement }); graphic.material = replacement; }
                    }
                    catch (Exception e) { Logger.LogWarning("Saved UI rule skipped: " + e.Message); }
                }
            }
        }
        finally { state.applyOnly = false; suppressCapture = false; }
        Logger.LogInfo("SAVED_RULES_APPLIED_ONCE scene=[" + scene.name + "] rules=" + rules.Count + " changed=" + stats.changed + "; automatic inspection disabled.");
    }
    void LoadHistoryForManualCheck()
    {
        if (historyLoaded) return; historyLoaded = true;
        try
        {
            if (File.Exists(store.ObservationsPath))
                foreach (TextureRepairRecord record in TextureRepairStore.Read(store.ObservationsPath).Records)
                    if (!observed.ContainsKey(record.Key())) observed[record.Key()] = record;
        }
        catch (Exception e) { Logger.LogWarning("Observation history ignored: " + e.Message); }
    }
    void IncludeLoadedScenesForManualCheck()
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            if (!scene.isLoaded || !scenes.Contains(scene.name) || states.ContainsKey(scene)) continue;
            FingerprintForScene(scene.name); states.Add(scene, new State());
        }
    }
}
