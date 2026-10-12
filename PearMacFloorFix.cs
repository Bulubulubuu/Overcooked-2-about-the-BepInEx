using System;
using System.Collections;
using System.Collections.Generic;
using BepInEx;
using UnityEngine;
using UnityEngine.SceneManagement;

[BepInPlugin("local.pear.macfloorfix", "Pear Mac Floor Fix", "0.1.0")]
public sealed class PearMacFloorFix : BaseUnityPlugin
{
    readonly Dictionary<Material, Material> replacements = new Dictionary<Material, Material>();
    readonly List<Material> owned = new List<Material>();
    void Awake()
    {
        SceneManager.sceneLoaded += OnLoaded;
        Logger.LogInfo("Loaded. Only floor renderers in s_pear_* scenes are targeted.");
        foreach (Scene scene in LoadedScenes()) OnLoaded(scene, LoadSceneMode.Additive);
    }
    IEnumerable<Scene> LoadedScenes()
    {
        for (int i = 0; i < SceneManager.sceneCount; i++) yield return SceneManager.GetSceneAt(i);
    }
    void OnLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name.StartsWith("s_pear_", StringComparison.OrdinalIgnoreCase))
            StartCoroutine(Repair(scene));
    }
    static bool IsFloor(Renderer renderer)
    {
        for (Transform t = renderer.transform; t != null; t = t.parent)
        {
            string n = t.name.ToLowerInvariant();
            if (n.StartsWith("floor") || n.StartsWith("p_floor") || n.Contains("_floor_")) return true;
        }
        return false;
    }
    Shader FindCompatibleShader()
    {
        string[] names = { "Sprites/Default", "Unlit/Transparent", "Unlit/Texture", "UI/Default" };
        Shader[] loaded = Resources.FindObjectsOfTypeAll<Shader>();
        foreach (string name in names)
        {
            // Search every loaded copy: Shader.Find alone may return an unsupported bundle copy.
            foreach (Shader s in loaded)
                if (s != null && s.name == name && s.isSupported) return s;
            Shader found = Shader.Find(name);
            if (found != null && found.isSupported) return found;
        }
        return null;
    }
    IEnumerator Repair(Scene scene)
    {
        int total = 0;
        for (int pass = 0; pass < 3; pass++)
        {
            yield return new WaitForSeconds(pass == 0 ? 1f : 3f);
            if (!scene.isLoaded) yield break;
            Shader shader = FindCompatibleShader();
            int floors = 0, changed = 0;
            foreach (Renderer renderer in Resources.FindObjectsOfTypeAll<Renderer>())
            {
                if (renderer == null || !renderer.gameObject.scene.Equals(scene) || !IsFloor(renderer)) continue;
                floors++;
                Material[] materials = renderer.sharedMaterials;
                bool update = false;
                for (int i = 0; i < materials.Length; i++)
                {
                    Material source = materials[i];
                    if (source == null || owned.Contains(source)) continue;
                    Shader old = source.shader;
                    bool broken = old == null || !old.isSupported || old.name.Contains("InternalErrorShader") || old.name == "Sprites/Default";
                    Logger.LogInfo("Floor=" + renderer.name + " renderer=" + renderer.GetType().Name +
                        " shader=" + (old == null ? "null" : old.name) + " supported=" + (old != null && old.isSupported) +
                        " sprite=" + (renderer is SpriteRenderer && ((SpriteRenderer)renderer).sprite != null));
                    if (!broken || shader == null) continue;
                    Material replacement;
                    if (!replacements.TryGetValue(source, out replacement))
                    {
                        replacement = new Material(source);
                        replacement.name = source.name + " [PearMacFloorFix]";
                        replacement.shader = shader;
                        replacement.shaderKeywords = new string[0];
                        replacements[source] = replacement;
                        owned.Add(replacement);
                    }
                    materials[i] = replacement;
                    changed++;
                    update = true;
                    Logger.LogInfo("Replaced floor material: " + renderer.name + " -> " + shader.name);
                }
                if (update) renderer.sharedMaterials = materials;
            }
            total += changed;
            Logger.LogInfo("Scene=" + scene.name + " pass=" + (pass + 1) + " floors=" + floors +
                " changed=" + changed + " total=" + total + " replacement=" + (shader == null ? "NONE" : shader.name));
            if (shader == null) Logger.LogWarning("No supported sprite/unlit fallback is loaded. A macOS shader bundle is required.");
        }
    }
    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnLoaded;
        StopAllCoroutines();
        // Keep assigned materials alive until process exit; disabling this plugin during a scene
        // must not destroy materials still used by renderers. Removing the DLL takes effect on restart.
    }
}
