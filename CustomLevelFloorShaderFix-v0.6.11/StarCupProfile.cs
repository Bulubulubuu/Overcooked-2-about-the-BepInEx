using System;
using BepInEx.Configuration;

public static class StarCupPolicy
{
    public static bool AssetMatches(string asset, string name) { return System.IO.Path.GetFileNameWithoutExtension(asset).Equals(name, StringComparison.OrdinalIgnoreCase); }
    static string BaseName(string material)
    {
        string name = (material ?? "").Trim();
        const string instance = " (Instance)";
        if (name.EndsWith(instance, StringComparison.OrdinalIgnoreCase)) name = name.Substring(0, name.Length - instance.Length);
        return name;
    }
    public static bool Matches(string scene, string kind, string material)
    {
        if (scene == null || !scene.StartsWith("star_", StringComparison.OrdinalIgnoreCase)) return false;
        string name = BaseName(material);
        if (kind == "Particle") return name.Equals("mat_web_pfx_steam", StringComparison.OrdinalIgnoreCase) || name.Equals("mat_web_pfx_drip", StringComparison.OrdinalIgnoreCase);
        if (kind != "Mesh") return false;
        if (kind == "Mesh" && (name == "mat_web_pfx_fire" || name == "mat_web_liquid_water" || name == "mat_web_liquid_milk" || name == "mat_web_liquid_honeytea" || name == "mat_web_liquid_milktea")) return true;
        return name.Equals("mat_web_teacup", StringComparison.OrdinalIgnoreCase) || name.Equals("mat_Kettle_Anniversary_01", StringComparison.OrdinalIgnoreCase) || name.Equals("mat_web_glassdirt", StringComparison.OrdinalIgnoreCase) || name.Equals("mat_Birthday_TeaCup_01_6", StringComparison.OrdinalIgnoreCase);
    }
    public static bool IsSolidColor(string material) { return BaseName(material).Equals("mat_Birthday_TeaCup_01_6", StringComparison.OrdinalIgnoreCase); }
}
public sealed partial class CustomLevelFloorShaderFix
{
    ConfigEntry<bool> starCups;
    readonly System.Collections.Generic.Dictionary<string, UnityEngine.Texture> starTextures = new System.Collections.Generic.Dictionary<string, UnityEngine.Texture>(StringComparer.Ordinal);
    static readonly System.Collections.Generic.Dictionary<string,string> StarTextureNames = new System.Collections.Generic.Dictionary<string,string>(StringComparer.Ordinal) {
        {"mat_web_pfx_steam", "noise_round"}, {"mat_web_pfx_drip", "t_pfx_water_drop_01_d"},
        {"mat_web_liquid_water", "t_Birthday_Tea_Water_0"}, {"mat_web_liquid_milk", "t_Birthday_Tea_Milk_0"},
        {"mat_web_liquid_honeytea", "t_Birthday_Tea_Honeytea_0"}, {"mat_web_liquid_milktea", "t_Birthday_Tea_Milktea_0"}
    };
    static string StarMaterialName(string name) { return name.EndsWith(" (Instance)", StringComparison.Ordinal) ? name.Substring(0, name.Length - 11) : name; }
    void PrepareStarSteamTexture()
    {
        foreach (UnityEngine.AssetBundle bundle in UnityEngine.AssetBundle.GetAllLoadedAssetBundles())
        {
            if (bundle == null || bundle.name.IndexOf("commonw5", StringComparison.OrdinalIgnoreCase) < 0) continue;
            foreach (string asset in bundle.GetAllAssetNames())
                foreach (System.Collections.Generic.KeyValuePair<string,string> entry in StarTextureNames)
                {
                    if (starTextures.ContainsKey(entry.Key) || !StarCupPolicy.AssetMatches(asset, entry.Value)) continue;
                    UnityEngine.Texture texture = bundle.LoadAsset<UnityEngine.Texture>(asset);
                    if (texture == null) continue;
                    texture.hideFlags |= UnityEngine.HideFlags.DontUnloadUnusedAsset;
                    starTextures[entry.Key] = texture;
                    Logger.LogInfo("STAR_TEXTURE_READY material=" + entry.Key + " texture=" + texture.name);
                }
            // Fix the four shared liquid templates once, so recipe-created instances inherit the compatible shader.
            UnityEngine.Shader liquidShader = UnityEngine.Shader.Find("Unlit/Texture");
            if (!starCups.Value || Broken(liquidShader)) continue;
            foreach (string asset in bundle.GetAllAssetNames())
                foreach (System.Collections.Generic.KeyValuePair<string,string> entry in StarTextureNames)
                {
                    UnityEngine.Texture texture;
                    if (!entry.Key.StartsWith("mat_web_liquid_", StringComparison.Ordinal) || !StarCupPolicy.AssetMatches(asset, entry.Key) || !starTextures.TryGetValue(entry.Key, out texture)) continue;
                    UnityEngine.Material source = bundle.LoadAsset<UnityEngine.Material>(asset);
                    if (source == null) continue;
                    source.shader = liquidShader;
                    source.SetTexture("_MainTex", texture);
                    source.hideFlags |= UnityEngine.HideFlags.DontUnloadUnusedAsset;
                    Logger.LogInfo("STAR_LIQUID_TEMPLATE_READY material=" + entry.Key + " texture=" + texture.name);
                }
        }
        if (!starTextures.ContainsKey("mat_web_pfx_fire"))
            foreach (UnityEngine.Material source in UnityEngine.Resources.FindObjectsOfTypeAll<UnityEngine.Material>())
            {
                if (source == null || source.name != "mat_pfx_fire_small_01" || Broken(source.shader)) continue;
                Tex original = ReadTexture(source, null, null, Kind.Particle);
                if (original.texture == null) continue;
                original.texture.hideFlags |= UnityEngine.HideFlags.DontUnloadUnusedAsset;
                starTextures["mat_web_pfx_fire"] = original.texture;
                Logger.LogInfo("STAR_FIRE_FALLBACK_READY texture=" + original.texture.name);
                break;
            }
    }
    void RecoverStarSteamTexture(string scene, Kind kind, UnityEngine.Material material, Tex texture)
    {
        if (!starCups.Value || texture.texture != null || !StarCupPolicy.Matches(scene, kind.ToString(), material.name)) return;
        UnityEngine.Texture cached;
        if (!starTextures.TryGetValue(StarMaterialName(material.name), out cached) || cached == null) return;
        texture.texture = cached; texture.property = "_MainTex";
    }
}
