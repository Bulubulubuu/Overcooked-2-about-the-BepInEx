using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using UnityEngine;
using UnityEngine.SceneManagement;

[BepInPlugin("local.customlevel.floorshaderfix", "Custom Level Floor Shader Fix", "0.4.0")]
[BepInIncompatibility("local.pear.macfloorfix")]
public sealed class CustomLevelFloorShaderFix : BaseUnityPlugin
{
    readonly HashSet<string> scenes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    readonly HashSet<Material> owned = new HashSet<Material>();
    ConfigEntry<bool> forceYi;
    ConfigEntry<bool> forceGl;
    ConfigEntry<string> extraScenes;
    static readonly string[] TextureProperties = {
        "_MainTex", "_Diffuse", "_DiffuseTex", "_AlbedoTex", "_BaseMap", "_Texture"
    };
    sealed class TextureInfo
    {
        public Texture texture;
        public Vector2 scale = Vector2.one, offset = Vector2.zero;
        public Color color = Color.white;
    }
    sealed class Fallback
    {
        public Shader shader;
        public Material template;
        public string property;
    }
    void Awake()
    {
        forceYi = Config.Bind("Repair", "ForceYiWater", true,
            "Replace blue_water materials in s_yi _level even if the imported shader reports support. Only affects Yi water.");
        forceGl = Config.Bind("Repair", "ForceGlFloor", true,
            "Repair the cloud wallpaper floor SpriteRenderer in s_gl_level and s_gl_level 2 even when the imported Windows shader reports support.");
        extraScenes = Config.Bind("Discovery", "AdditionalScenes", "s_yi _level",
            "Additional custom scene names separated by semicolons. Spaces inside scene names are preserved.");
        Discover();
        SceneManager.sceneLoaded += Loaded;
        Logger.LogInfo("v0.4.0 active; custom scenes=" + scenes.Count + "; ForceYiWater=" + forceYi.Value + "; ForceGlFloor=" + forceGl.Value);
        for (int i = 0; i < SceneManager.sceneCount; i++) Loaded(SceneManager.GetSceneAt(i), LoadSceneMode.Additive);
    }
    void Discover()
    {
        // Built-in profiles stay active even when an existing config lists only older scenes.
        scenes.Add("s_gl_level");
        scenes.Add("s_gl_level 2");
        foreach (string name in extraScenes.Value.Split(';'))
            if (name.Trim().Length > 0) scenes.Add(name.Trim());
        string root = Path.Combine(Path.Combine(Paths.PluginPath, "OC2DIYLevel"), "levels");
        try
        {
            if (Directory.Exists(root))
                foreach (string file in Directory.GetFiles(root, "*", SearchOption.AllDirectories))
                {
                    string name = Path.GetFileName(file);
                    if (name.StartsWith("s_", StringComparison.OrdinalIgnoreCase) && name.IndexOf('.') < 0)
                        scenes.Add(name);
                }
        }
        catch (Exception ex) { Logger.LogWarning("Scene discovery: " + ex.Message); }
    }
    void Loaded(Scene scene, LoadSceneMode mode)
    {
        Discover();
        if (!scenes.Contains(scene.name)) return;
        Logger.LogInfo("Inspecting custom scene: [" + scene.name + "]");
        StartCoroutine(Repair(scene));
    }
    static bool IsGlFloor(string sceneName, string objectName, bool sprite)
    {
        if (!sprite || string.IsNullOrEmpty(objectName)) return false;
        if (!string.Equals(sceneName, "s_gl_level", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(sceneName, "s_gl_level 2", StringComparison.OrdinalIgnoreCase)) return false;
        const string prefix = "【哲风壁纸】云朵-卡通-可爱";
        // Unity may add a numbered copy or a Clone suffix to the actual asset name.
        if (!objectName.StartsWith(prefix, StringComparison.Ordinal)) return false;
        string suffix = objectName.Substring(prefix.Length).Trim();
        if (suffix == "" || suffix == "(Clone)") return true;
        int copy;
        return int.TryParse(suffix, out copy) && copy >= 0;
    }
    static bool WaterName(string name)
    {
        return name != null && name.IndexOf("blue_water", StringComparison.OrdinalIgnoreCase) >= 0;
    }
    static bool IsWater(Renderer renderer, Material material)
    {
        if (material != null && WaterName(material.name)) return true;
        for (Transform t = renderer.transform; t != null; t = t.parent)
            if (WaterName(t.name)) return true;
        return false;
    }
    static bool IsFloor(Renderer renderer)
    {
        foreach (Material material in renderer.sharedMaterials)
            if (material != null && (WaterName(material.name) || material.name.IndexOf("floor", StringComparison.OrdinalIgnoreCase) >= 0)) return true;
        for (Transform t = renderer.transform; t != null; t = t.parent)
        {
            string n = t.name.ToLowerInvariant();
            if (n == "ground" || WaterName(n) || n.StartsWith("floor") || n.StartsWith("p_floor") || n.Contains("_floor_") || n.Contains("_flooredge")) return true;
        }
        return false;
    }
    static TextureInfo ReadTexture(Renderer renderer, Material material, bool preferSprite)
    {
        TextureInfo result = new TextureInfo();
        SpriteRenderer sprite = renderer as SpriteRenderer;
        if (preferSprite && sprite != null && sprite.sprite != null)
            result.texture = sprite.sprite.texture;
        foreach (string property in TextureProperties)
        {
            if (result.texture != null) break;
            if (!material.HasProperty(property)) continue;
            Texture texture = material.GetTexture(property);
            if (texture == null) continue;
            result.texture = texture;
            result.scale = material.GetTextureScale(property);
            result.offset = material.GetTextureOffset(property);
            break;
        }
        if (result.texture == null && sprite != null && sprite.sprite != null)
            result.texture = sprite.sprite.texture;
        if (material.HasProperty("_Color")) result.color = material.GetColor("_Color");
        return result;
    }
    Fallback FindFallback(Shader old, bool sprite)
    {
        // Enumerate every copy. Shader.Find can return the incompatible imported copy.
        string[] preferred = sprite
            ? new string[] { "Sprites/Default", "Unlit/Transparent", "Unlit/Texture", "UI/Default" }
            : new string[] { "Unlit/Texture", "Unlit/Transparent", "Sprites/Default", "UI/Default" };
        Shader[] shaders = Resources.FindObjectsOfTypeAll<Shader>();
        foreach (string name in preferred)
            foreach (Shader shader in shaders)
                if (shader != null && shader != old && shader.isSupported && shader.name == name)
                {
                    Material probe = new Material(shader);
                    bool textured = probe.HasProperty("_MainTex");
                    Destroy(probe);
                    if (textured) return new Fallback { shader = shader, property = "_MainTex" };
                }
        // If built-in unlit shaders are unavailable, reuse a supported game material.
        // Copy its complete material state rather than applying a shader with unknown defaults.
        foreach (Material material in Resources.FindObjectsOfTypeAll<Material>())
        {
            if (material == null || owned.Contains(material) || material.shader == null) continue;
            Shader shader = material.shader;
            if (shader == old || !shader.isSupported || !shader.name.StartsWith("Overcooked_2/", StringComparison.Ordinal)) continue;
            foreach (string property in TextureProperties)
                if (material.HasProperty(property) && material.GetTexture(property) != null)
                    return new Fallback { shader = shader, template = material, property = property };
        }
        return null;
    }
    IEnumerator Repair(Scene scene)
    {
        // Realtime delays also run during the ready screen and pause menu.
        for (int pass = 0; pass < 5; pass++)
        {
            yield return new WaitForSecondsRealtime(pass == 0 ? 0.5f : 3f);
            if (!scene.isLoaded) yield break;
            int floors = 0, changed = 0, missingTexture = 0, missingShader = 0;
            foreach (Renderer renderer in Resources.FindObjectsOfTypeAll<Renderer>())
            {
                if (renderer == null || !renderer.gameObject.scene.Equals(scene)) continue;
                bool glFloor = IsGlFloor(scene.name, renderer.name, renderer is SpriteRenderer);
                if (!glFloor && !IsFloor(renderer)) continue;
                floors++;
                try
                {
                    Material[] materials = renderer.sharedMaterials;
                    bool update = false;
                    for (int i = 0; i < materials.Length; i++)
                    {
                        Material original = materials[i];
                        if (original == null || owned.Contains(original)) continue;
                        Shader old = original.shader;
                        bool forced = (forceGl.Value && glFloor) ||
                            (forceYi.Value && scene.name.Equals("s_yi _level", StringComparison.OrdinalIgnoreCase) && IsWater(renderer, original));
                        bool broken = old == null || !old.isSupported || old.name.IndexOf("InternalErrorShader", StringComparison.OrdinalIgnoreCase) >= 0;
                        TextureInfo texture = ReadTexture(renderer, original, glFloor);
                        if (pass == 0 || broken || forced)
                            Logger.LogInfo("Object=" + renderer.name + " material=" + original.name + " shader=" + (old == null ? "null" : old.name) +
                                " supported=" + (old != null && old.isSupported) + " forced=" + forced + " texture=" + (texture.texture == null ? "NONE" : texture.texture.name));
                        if (!broken && !forced) continue;
                        if (texture.texture == null) { missingTexture++; continue; }
                        Fallback fallback = FindFallback(old, renderer is SpriteRenderer);
                        if (fallback == null) { missingShader++; continue; }
                        Material replacement = fallback.template != null ? new Material(fallback.template) : new Material(fallback.shader);
                        replacement.name = original.name + " [CustomLevelFloorShaderFix]";
                        replacement.SetTexture(fallback.property, texture.texture);
                        replacement.SetTextureScale(fallback.property, texture.scale);
                        replacement.SetTextureOffset(fallback.property, texture.offset);
                        if (replacement.HasProperty("_Color")) replacement.SetColor("_Color", texture.color);
                        if (replacement.HasProperty("_RendererColor")) replacement.SetColor("_RendererColor", Color.white);
                        owned.Add(replacement);
                        materials[i] = replacement;
                        changed++;
                        update = true;
                        Logger.LogInfo("REPAIRED " + renderer.name + " -> " + fallback.shader.name + " textureProperty=" + fallback.property +
                            " texture=" + texture.texture.name + " scale=" + texture.scale + " offset=" + texture.offset +
                            " nativeMaterial=" + (fallback.template != null));
                    }
                    if (update) renderer.sharedMaterials = materials;
                }
                catch (Exception ex) { Logger.LogWarning("Skipped renderer [" + renderer.name + "]: " + ex.Message); }
            }
            Logger.LogInfo("Scene=[" + scene.name + "] pass=" + (pass + 1) + " floors=" + floors + " changed=" + changed +
                " missingTexture=" + missingTexture + " missingShader=" + missingShader);
        }
    }
    void OnDestroy()
    {
        SceneManager.sceneLoaded -= Loaded;
        StopAllCoroutines();
        // Assigned materials live until process exit so they are not destroyed under active renderers.
    }
}
