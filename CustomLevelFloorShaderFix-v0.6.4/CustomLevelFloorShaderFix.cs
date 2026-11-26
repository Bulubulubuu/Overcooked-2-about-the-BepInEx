using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[BepInPlugin("local.customlevel.floorshaderfix", "Custom Level Floor Shader Fix", "0.6.4")]
[BepInDependency("com.ch3ngyz.plugin.overcooked2.modsettings", BepInDependency.DependencyFlags.SoftDependency)]
[BepInIncompatibility("local.pear.macfloorfix")]
public sealed partial class CustomLevelFloorShaderFix : BaseUnityPlugin
{
    readonly HashSet<string> scenes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    readonly Dictionary<Scene, State> states = new Dictionary<Scene, State>();
    readonly HashSet<Material> owned = new HashSet<Material>();
    ConfigEntry<bool> forceYi, forceGl, knownVisuals, allRenderers, verbose;
    ConfigEntry<string> extraScenes, forcedShaders;
    ConfigEntry<float> watchSeconds;
    enum Kind { Mesh, Sprite, Particle, UI }
    sealed class Assignment
    {
        public Renderer renderer; public Graphic graphic; public int slot; public bool trail;
        public Material original, replacement;
        public void Restore()
        {
            if (graphic != null) { if (graphic.material == replacement) graphic.material = original; return; }
            if (renderer == null) return;
            if (trail) { ParticleSystemRenderer p = renderer as ParticleSystemRenderer; if (p != null && p.trailMaterial == replacement) p.trailMaterial = original; return; }
            Material[] a = renderer.sharedMaterials;
            if (slot < a.Length && a[slot] == replacement) { a[slot] = original; renderer.sharedMaterials = a; }
        }
    }
    sealed class State
    {
        public bool active = true;
        public readonly List<Assignment> assignments = new List<Assignment>();
        public readonly Dictionary<string, Material> cache = new Dictionary<string, Material>();
        public readonly HashSet<string> logged = new HashSet<string>();
    }
    sealed class Tex
    {
        public Texture texture; public string property;
        public Vector2 scale = Vector2.one, offset = Vector2.zero;
        public Color color = Color.white; public bool hasColor;
    }
    sealed class Target { public Shader shader; public Material template; public string property; public bool sameShader; }
    sealed class Stats { public int objects, materials, invalid, forced, changed, missingTexture, missingShader, missingMaterial, proposed, errors; }
    static readonly string[] TextureProperties = { "_MainTex", "_ColourAlpha", "_DiffuseTexture", "_Diffuse", "_DiffuseTex", "_AlbedoTex", "_BaseMap", "_Texture" };
    void Awake()
    {
        forceYi = Config.Bind("Repair", "ForceYiWater", true, "Retain the Yi blue_water profile.");
        forceGl = Config.Bind("Repair", "ForceGlFloor", true, "Retain the Gl cloud wallpaper profile.");
        knownVisuals = Config.Bind("Repair", "RepairKnownVisualAssets", true, "Also try portable shader replacements for cannon, sign/icon and rain objects with imported Standard/Sprites/Particles/Legacy shaders. Supported Overcooked_2 shaders are retained.");
        allRenderers = Config.Bind("Repair", "ScanAllRenderers", true, "Inspect all custom-scene renderers and UI, including particle trails. False restricts scanning to named floors.");
        forcedShaders = Config.Bind("Repair", "ForceShaderNames", "", "Optional exact shader names separated by semicolons. Only custom scenes are affected.");
        extraScenes = Config.Bind("Discovery", "AdditionalScenes", "s_yi _level", "Extra scene names separated by semicolons.");
        watchSeconds = Config.Bind("Discovery", "WatchSeconds", 60f, "Real-time observation window for dynamically spawned objects. Limited to 12.5-300 seconds.");
        verbose = Config.Bind("Logging", "VerboseMaterials", false, "Log healthy material details as well as failures and replacements.");
        InitializeMaintenance();
        Discover();
        SceneManager.sceneLoaded += Loaded; SceneManager.sceneUnloaded += Unloaded;
        Logger.LogInfo("v0.6.4 active; custom scenes=" + scenes.Count + "; allRenderers=" + allRenderers.Value + "; knownVisuals=" + knownVisuals.Value);
        for (int i = 0; i < SceneManager.sceneCount; i++) Loaded(SceneManager.GetSceneAt(i), LoadSceneMode.Additive);
    }
    void Discover()
    {
        sceneFiles.Clear(); ambiguousScenes.Clear();
        scenes.Add("s_yi _level"); scenes.Add("s_gl_level"); scenes.Add("s_gl_level 2");
        foreach (string n in extraScenes.Value.Split(';')) if (n.Trim().Length > 0) scenes.Add(n.Trim());
        string root = Path.Combine(Path.Combine(Paths.PluginPath, "OC2DIYLevel"), "levels");
        try
        {
            if (!Directory.Exists(root)) return;
            foreach (string file in Directory.GetFiles(root, "s_*", SearchOption.AllDirectories))
            {
                if (IsCache(root, file)) continue;
                string n = Path.GetFileName(file);
                if (n.IndexOf('.') < 0)
                {
                    scenes.Add(n);
                    string existing;
                    if (sceneFiles.TryGetValue(n, out existing) && existing != file) ambiguousScenes.Add(n);
                    else sceneFiles[n] = file;
                }
            }
        }
        catch (Exception e) { Logger.LogWarning("Discovery: " + e.Message); }
    }
    void Loaded(Scene scene, LoadSceneMode mode)
    {
        Discover();
        if (!scenes.Contains(scene.name) || states.ContainsKey(scene)) return;
        FingerprintForScene(scene.name);
        State state = new State(); states.Add(scene, state);
        StartCoroutine(WarmTestFloorMaterials());
        Logger.LogInfo("Inspecting custom scene: [" + scene.name + "]"); StartCoroutine(Observe(scene, state));
    }
    void Release(State state, bool restore)
    {
        state.active = false;
        if (restore) foreach (Assignment a in state.assignments) try { a.Restore(); } catch (Exception) { }
        foreach (Material m in state.cache.Values) if (m != null) { owned.Remove(m); Destroy(m); }
        state.cache.Clear(); state.assignments.Clear(); state.logged.Clear();
    }
    void Unloaded(Scene scene) { SaveObservations(true); loadedFingerprints.Remove(scene.name); State s; if (states.TryGetValue(scene, out s)) { Release(s, false); states.Remove(scene); } }
    static bool Contains(string name, string token) { return name != null && name.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0; }
    static bool Hierarchy(Transform t, string token)
    {
        for (; t != null; t = t.parent) if (Contains(t.name, token)) return true;
        return false;
    }
    static bool GlFloor(string scene, string name, Kind kind)
    {
        if (kind != Kind.Sprite || (scene != "s_gl_level" && scene != "s_gl_level 2")) return false;
        const string prefix = "【哲风壁纸】云朵-卡通-可爱";
        if (name == null || !name.StartsWith(prefix, StringComparison.Ordinal)) return false;
        string tail = name.Substring(prefix.Length).Trim(); int copy;
        return tail == "" || tail == "(Clone)" || (int.TryParse(tail, out copy) && copy >= 0);
    }
    static bool Floor(Transform t, Material m)
    {
        return Contains(m.name, "floor") || Contains(m.name, "blue_water") || Hierarchy(t, "floor") || Hierarchy(t, "ground") || Hierarchy(t, "blue_water");
    }
    static bool Portable(string shader)
    {
        return shader == "Standard" || shader.StartsWith("Sprites/", StringComparison.Ordinal) || shader.StartsWith("Particles/", StringComparison.Ordinal) || shader.StartsWith("Legacy Shaders/", StringComparison.Ordinal) || shader.StartsWith("Unlit/", StringComparison.Ordinal);
    }
    static bool Visual(Transform t, Material m, Kind kind)
    {
        string[] tokens = kind == Kind.Particle
            ? new string[] { "rain", "雨" }
            : new string[] { "cannon", "sign", "tutorial", "icon", "diagram", "大炮", "图示", "告示" };
        foreach (string token in tokens)
        {
            if (VisualAssetPolicy.ContainsToken(m.name, token)) return true;
            for (Transform parent = t; parent != null; parent = parent.parent)
                if (VisualAssetPolicy.ContainsToken(parent.name, token)) return true;
        }
        return false;
    }
    static bool Broken(Shader s) { return s == null || !s.isSupported || Contains(s.name, "InternalErrorShader"); }
    bool Forced(string scene, Transform t, Material m, Kind kind)
    {
        if (forceYi.Value && scene == "s_yi _level" && (Hierarchy(t, "blue_water") || Contains(m.name, "blue_water"))) return true;
        if (forceGl.Value && GlFloor(scene, t.name, kind)) return true;
        if (m.shader != null)
        {
            foreach (string name in forcedShaders.Value.Split(';')) if (name.Trim().Length > 0 && m.shader.name == name.Trim()) return true;
            if (knownVisuals.Value && Portable(m.shader.name) && Visual(t, m, kind)) return true;
        }
        return false;
    }
    static Tex ReadTexture(Material m, Renderer renderer, Graphic graphic, Kind kind)
    {
        Tex t = new Tex(); SpriteRenderer sr = renderer as SpriteRenderer;
        if (kind == Kind.Sprite && sr != null && sr.sprite != null) t.texture = sr.sprite.texture;
        foreach (string key in TextureProperties)
        {
            if (t.texture != null) break;
            if (!m.HasProperty(key)) continue;
            Texture tx = m.GetTexture(key); if (tx == null) continue;
            t.texture = tx; t.property = key; t.scale = m.GetTextureScale(key); t.offset = m.GetTextureOffset(key);
        }
        if (t.texture == null && graphic != null) t.texture = graphic.mainTexture;
        string color = m.HasProperty("_TintColor") ? "_TintColor" : (m.HasProperty("_Color") ? "_Color" : null);
        if (color != null) { t.color = m.GetColor(color); t.hasColor = true; }
        return t;
    }
    static string Property(Material m)
    {
        foreach (string p in TextureProperties) if (m.HasProperty(p)) return p;
        return null;
    }
    static bool OpaqueFallbackAllowed(Kind kind, int queue, string shaderName)
    {
        return kind == Kind.Mesh && queue < 2450 && !Contains(shaderName, "alpha") && !Contains(shaderName, "transparent");
    }
    Target FindTarget(Material original, Kind kind, Shader[] shaders, Material[] materials, bool forced)
    {
        Shader old = original.shader;
        // An alternative supported copy of the same shader preserves its semantics and all slots.
        if (!forced && old != null)
            foreach (Shader shader in shaders)
                if (shader != null && shader != old && !Broken(shader) && shader.name == old.name)
                    return new Target { shader = shader, sameShader = true };
        string[] names;
        if (kind == Kind.Particle)
        {
            bool additive = old != null && Contains(old.name, "Additive");
            names = additive ? new string[] { "Particles/Additive", "Legacy Shaders/Particles/Additive", "Particles/Alpha Blended", "Sprites/Default" }
                : new string[] { "Particles/Alpha Blended", "Legacy Shaders/Particles/Alpha Blended", "Particles/Alpha Blended Premultiply", "Sprites/Default", "Unlit/Transparent" };
        }
        else if (kind == Kind.UI) names = new string[] { "UI/Default", "Sprites/Default", "Unlit/Transparent" };
        else if (kind == Kind.Sprite) names = new string[] { "Sprites/Default", "Unlit/Transparent", "UI/Default" };
        else
        {
            bool alpha = original.renderQueue >= 2450 || (old != null && (Contains(old.name, "alpha") || Contains(old.name, "transparent")));
            names = alpha ? new string[] { "Unlit/Transparent", "Sprites/Default", "Legacy Shaders/Transparent/Diffuse" }
                : new string[] { "Unlit/Texture", "Legacy Shaders/Diffuse", "Sprites/Default", "Unlit/Transparent" };
        }
        foreach (string name in names)
            foreach (Shader shader in shaders)
                if (shader != null && shader != old && !Broken(shader) && shader.name == name)
                {
                    Material probe = new Material(shader); string property = Property(probe); Destroy(probe);
                    if (property != null) return new Target { shader = shader, property = property };
                }
        // Never replace rain, signs with transparency, or UI using an opaque game mesh material.
        bool opaque = OpaqueFallbackAllowed(kind, original.renderQueue, old == null ? "" : old.name);
        if (opaque)
            foreach (Material m in materials)
            {
                if (m == null || owned.Contains(m) || Broken(m.shader) || m.shader == old || m.renderQueue >= 2450) continue;
                if (!m.shader.name.StartsWith("Overcooked_2/OC2_gloss_d", StringComparison.Ordinal) || Contains(m.shader.name, "alpha")) continue;
                string property = Property(m);
                if (property != null && m.GetTexture(property) != null) return new Target { shader = m.shader, template = m, property = property };
            }
        return null;
    }
    Material RepairMaterial(Scene scene, State state, Transform transform, Renderer renderer, Graphic graphic, Material original, Kind kind, Shader[] shaders, Material[] materials, Stats stats)
    {
        if (original == null || owned.Contains(original)) return null;
        stats.materials++;
        if (!allRenderers.Value && !Floor(transform, original) && !GlFloor(scene.name, transform.name, kind)) return null;
        Tex texture = ReadTexture(original, renderer, graphic, kind);
        TextureRepairRecord observation = Record(scene, transform, original, kind, texture);
        TextureRepairRecord rule = ActiveRule(observation);
        bool invalid = Broken(original.shader), force = rule != null || Forced(scene.name, transform, original, kind);
        if (invalid) stats.invalid++; if (force) stats.forced++;
        string key = original.GetInstanceID() + ":" + kind + ":" + (texture.texture == null ? 0 : texture.texture.GetInstanceID());
        if (!invalid && !force) { observation.Status = "healthy"; Remember(observation); return null; }
        string shaderName = original.shader == null ? "null" : original.shader.name;
        string detail = " object=" + transform.name + " kind=" + kind + " material=" + original.name + " shader=" + shaderName + " forced=" + force;
        Target target = PlannedTarget(rule, original, shaders) ?? FindTarget(original, kind, shaders, materials, force);
        if (target == null) { observation.Status = "missing_shader"; Remember(observation); stats.missingShader++; if (state.logged.Add(key + ":shader")) Logger.LogWarning("NO_COMPATIBLE_SHADER" + detail); return null; }
        if (!target.sameShader && texture.texture == null) { observation.Status = "missing_texture"; Remember(observation); stats.missingTexture++; if (state.logged.Add(key + ":texture")) Logger.LogWarning("NO_SOURCE_TEXTURE" + detail); return null; }
        observation.Status = "candidate"; observation.TargetShader = target.shader.name; Remember(observation);
        if (!immediate.Value && rule == null) { stats.proposed++; return null; }
        Material cached;
        if (state.cache.TryGetValue(key, out cached) && cached != null) { stats.changed++; return cached; }
        Material replacement;
        if (target.sameShader) { replacement = new Material(original); replacement.shader = target.shader; }
        else
        {
            replacement = target.template != null ? new Material(target.template) : new Material(target.shader);
            replacement.SetTexture(target.property, texture.texture);
            replacement.SetTextureScale(target.property, texture.scale); replacement.SetTextureOffset(target.property, texture.offset);
            if (texture.hasColor)
            {
                if (replacement.HasProperty("_Color")) replacement.SetColor("_Color", texture.color);
                if (replacement.HasProperty("_TintColor")) replacement.SetColor("_TintColor", texture.color);
            }
            if (replacement.HasProperty("_RendererColor")) replacement.SetColor("_RendererColor", Color.white);
            if (kind == Kind.UI && original.HasProperty("_StencilComp") && replacement.HasProperty("_StencilComp"))
                foreach (string p in new string[] { "_StencilComp", "_Stencil", "_StencilOp", "_StencilWriteMask", "_StencilReadMask", "_ColorMask", "_UseUIAlphaClip" })
                    if (original.HasProperty(p) && replacement.HasProperty(p)) replacement.SetFloat(p, original.GetFloat(p));
        }
        replacement.name = original.name + " [CustomLevelFloorShaderFix v0.6.4]";
        state.cache[key] = replacement; owned.Add(replacement); stats.changed++;
        Logger.LogInfo("REPAIRED" + detail + " -> " + target.shader.name + " texture=" + (texture.texture == null ? "preserved" : texture.texture.name) + " sameShader=" + target.sameShader);
        return replacement;
    }
    Stats ScanScene(Scene scene, State state)
    {
        if (testFloors.Count < 2 && !testFloorLoading && localFloorFallback.Value) StartCoroutine(WarmTestFloorMaterials());
            Stats stats = new Stats(); Shader[] shaders = Resources.FindObjectsOfTypeAll<Shader>(); Material[] materials = Resources.FindObjectsOfTypeAll<Material>();
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
                {
                    stats.objects++;
                    try
                    {
                        Kind kind = renderer is ParticleSystemRenderer ? Kind.Particle : (renderer is SpriteRenderer ? Kind.Sprite : Kind.Mesh);
                        Material[] a = renderer.sharedMaterials; bool changed = false;
                        if (a.Length == 0 && kind == Kind.Mesh && LocalFloorPolicy.IsFloorName(renderer.name)) a = new Material[1];
                        for (int i = 0; i < a.Length; i++)
                        {
                            if (a[i] == null)
                            {
                                if (kind == Kind.Particle)
                                {
                                    ParticleSystem system = renderer.GetComponent<ParticleSystem>();
                                    if (ParticleSlotPolicy.IsUnusedTrailSlot(i, system != null, system != null && system.trails.enabled)) { RecordUnusedTrail(scene, renderer.transform, i); continue; }
                                }
                                stats.missingMaterial++;
                                Material substitute = RepairMissingMaterial(scene, state, renderer.transform, kind, i, materials, stats);
                                if (substitute != null) { state.assignments.Add(new Assignment { renderer = renderer, slot = i, original = null, replacement = substitute }); a[i] = substitute; changed = true; }
                                continue;
                            }
                            Material replacement = RepairMaterial(scene, state, renderer.transform, renderer, null, a[i], kind, shaders, materials, stats);
                            if (replacement == null) continue;
                            state.assignments.Add(new Assignment { renderer = renderer, slot = i, original = a[i], replacement = replacement }); a[i] = replacement; changed = true;
                        }
                        if (changed) renderer.sharedMaterials = a;
                        ParticleSystemRenderer particle = renderer as ParticleSystemRenderer;
                        if (particle != null && particle.trailMaterial != null)
                        {
                            Material original = particle.trailMaterial;
                            Material replacement = RepairMaterial(scene, state, renderer.transform, renderer, null, original, Kind.Particle, shaders, materials, stats);
                            if (replacement != null) { state.assignments.Add(new Assignment { renderer = renderer, trail = true, original = original, replacement = replacement }); particle.trailMaterial = replacement; }
                        }
                    }
                    catch (Exception e) { stats.errors++; Logger.LogWarning("Renderer [" + renderer.name + "]: " + e.Message); }
                }
                if (allRenderers.Value)
                    foreach (Graphic graphic in root.GetComponentsInChildren<Graphic>(true))
                    {
                        try
                        {
                            Material original = graphic.material;
                            Material replacement = RepairMaterial(scene, state, graphic.transform, null, graphic, original, Kind.UI, shaders, materials, stats);
                            if (replacement != null) { state.assignments.Add(new Assignment { graphic = graphic, original = original, replacement = replacement }); graphic.material = replacement; }
                        }
                        catch (Exception e) { stats.errors++; Logger.LogWarning("Graphic [" + graphic.name + "]: " + e.Message); }
                    }
            }
        SaveObservations(false);
        return stats;
    }
    IEnumerator Observe(Scene scene, State state)
    {
        float end = Time.realtimeSinceStartup + Mathf.Clamp(watchSeconds.Value, 12.5f, 300f); int pass = 0;
        while (state.active && scene.isLoaded)
        {
            yield return new WaitForSecondsRealtime(pass == 0 ? 0.5f : (pass < 5 ? 3f : 5f));
            if (!state.active || !scene.isLoaded) yield break;
            Stats stats = ScanScene(scene, state);
            pass++;
            if (pass == 1 || stats.changed > 0 || stats.errors > 0 || Time.realtimeSinceStartup >= end || verbose.Value)
                Logger.LogInfo("AUDIT scene=[" + scene.name + "] pass=" + pass + " renderers=" + stats.objects + " materials=" + stats.materials + " invalid=" + stats.invalid + " forced=" + stats.forced + " changed=" + stats.changed + " proposed=" + stats.proposed + " missingTexture=" + stats.missingTexture + " missingShader=" + stats.missingShader + " missingMaterial=" + stats.missingMaterial + " errors=" + stats.errors);
            if (Time.realtimeSinceStartup >= end) break;
        }
    }
    void OnDestroy()
    {
        ShutdownMaintenance();
        StopAllCoroutines(); SceneManager.sceneLoaded -= Loaded; SceneManager.sceneUnloaded -= Unloaded;
        foreach (State s in states.Values) Release(s, true); states.Clear(); ReleaseTestFloorMaterials();
    }
}
