using System;
using BepInEx.Configuration;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class LocalFloorPolicy
{
    public static bool IsFloorName(string name)
    {
        if (name == null) return false;
        string value = name.Trim().ToLowerInvariant();
        if (value.EndsWith("(clone)", StringComparison.Ordinal)) value = value.Substring(0, value.Length - 7).Trim();
        if (value == "floor") return true;
        if (!value.StartsWith("floor", StringComparison.Ordinal)) return false;
        string tail = value.Substring(5).Trim();
        if (tail.StartsWith("(", StringComparison.Ordinal) && tail.EndsWith(")", StringComparison.Ordinal)) tail = tail.Substring(1, tail.Length - 2);
        if (tail.Length > 1 && tail[0] >= 'a' && tail[0] <= 'h') tail = tail.Substring(1);
        int n; return int.TryParse(tail, out n) && n >= 0;
    }
    public static int SourceScore(string name)
    {
        string n = (name ?? "").ToLowerInvariant();
        if (n.Contains("burner") || n.Contains("collider") || n.Contains("glass") || n.Contains("shadow")) return 0;
        int score = n.Contains("floor") ? 100 : (n.Contains("path") ? 80 : (n.Contains("ground") ? 70 : (n.Contains("road") ? 60 : 0)));
        if (score > 0 && (n.Contains("side") || n.Contains("edge"))) score = 10;
        return score;
    }
}
public static class VisualAssetPolicy
{
    public static bool ContainsToken(string name, string token)
    {
        if (name == null || token == null || token.Length == 0) return false;
        int start = 0;
        while (start < name.Length)
        {
            int index = name.IndexOf(token, start, StringComparison.OrdinalIgnoreCase);
            if (index < 0) return false;
            if (index == 0 || !char.IsLetterOrDigit(name[index - 1]) || (char.IsUpper(name[index]) && char.IsLower(name[index - 1]))) return true;
            start = index + 1;
        }
        return false;
    }
}
public static class ParticleSlotPolicy
{
    public static bool IsUnusedTrailSlot(int slot, bool hasSystem, bool trailsEnabled) { return slot == 1 && hasSystem && !trailsEnabled; }
}
public sealed partial class CustomLevelFloorShaderFix
{
    ConfigEntry<bool> localFloorFallback;
    const string LocalMaterialPrefix = "local-material:";
    void RecordUnusedTrail(Scene scene, Transform transform, int slot)
    {
        Remember(new TextureRepairRecord { Scene = scene.name, ObjectPath = StablePath(transform), Material = "<null-slot:" + slot + ">", OriginalShader = "", Texture = "", Kind = "Particle", Fingerprint = FingerprintForScene(scene.name), Status = "optional_material" });
    }
    Material RepairMissingMaterial(Scene scene, State state, Transform transform, Kind kind, int slot, Material[] materials, Stats stats)
    {
        TextureRepairRecord record = new TextureRepairRecord {
            Scene = scene.name, ObjectPath = StablePath(transform), Material = "<null-slot:" + slot + ">",
            OriginalShader = "", Texture = "", Kind = kind.ToString(), Fingerprint = FingerprintForScene(scene.name), Status = "missing_material"
        };
        if (state.logged.Add("null:" + record.Key())) Logger.LogWarning("MISSING_MATERIAL scene=[" + scene.name + "] object=" + transform.name + " slot=" + slot);
        if (!localFloorFallback.Value || kind != Kind.Mesh || !LocalFloorPolicy.IsFloorName(transform.name)) { Remember(record); return null; }
        TextureRepairRecord rule = ActiveRule(record);
        if (state.applyOnly && rule == null) return null;
        string desired = TestMaterialPrefix + TestFloorPolicy.MaterialForObject(transform.name);
        if (rule != null && rule.TargetShader != desired) rule = null;
        if (state.applyOnly && rule == null) return null;
        Material source = TestFloorSource(transform.name);
        if (source == null) { record.Status = "missing_material_test1_unavailable"; Remember(record); return null; }
        record.Status = "candidate"; record.TargetShader = desired; Remember(record);
        if (!immediate.Value && rule == null) { stats.proposed++; return null; }
        string key = "local:" + source.GetInstanceID(); Material replacement;
        if (!state.cache.TryGetValue(key, out replacement) || replacement == null)
        {
            replacement = new Material(source); replacement.name = source.name + " [Local floor substitute]";
            state.cache[key] = replacement; owned.Add(replacement);
        }
        stats.changed++;
        Logger.LogWarning("LOCAL_FLOOR_SUBSTITUTE scene=[" + scene.name + "] object=" + transform.name + " source=" + TestFloorPolicy.MaterialForObject(transform.name) + "; substitute appearance, not the original missing texture.");
        return replacement;
    }
}
