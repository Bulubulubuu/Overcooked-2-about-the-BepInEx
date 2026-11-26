using System;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Bootstrap;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed partial class CustomLevelFloorShaderFix
{
    const string OwnerGuid = "local.customlevel.floorshaderfix";
    const string SettingsGuid = "com.ch3ngyz.plugin.overcooked2.modsettings";
    TextureRepairStore store;
    readonly Dictionary<string, TextureRepairRecord> observed = new Dictionary<string, TextureRepairRecord>();
    readonly Dictionary<string, TextureRepairRecord> activeRules = new Dictionary<string, TextureRepairRecord>();
    readonly Dictionary<string, string> sceneFiles = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    readonly Dictionary<string, string> loadedFingerprints = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    readonly HashSet<string> ambiguousScenes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    TextureSettingsBridge settingsBridge;
    ConfigEntry<string> maintenanceStatus;
    ConfigEntry<bool> immediate;
    bool maintenanceBusy, observationsDirty;
    float lastObservationSave;
    string LevelsRoot { get { return Path.Combine(Path.Combine(Paths.PluginPath, "OC2DIYLevel"), "levels"); } }
    void LoadActive()
    {
        if (File.Exists(store.ActivePath)) foreach (TextureRepairRecord r in TextureRepairStore.Read(store.ActivePath).Records)
            if (r.Ready) activeRules[r.Key()] = r;
    }
    void InitializeMaintenance()
    {
        InitializeLanguage();
        localFloorFallback = Config.Bind("Repair", "UseLocalFloorFallback", true, "Use the floor materials from test_1 for missing-material Floor mesh renderers. This is a substitute when original resources are unavailable.");
        immediate = Config.Bind("Repair", "ApplyUnplannedImmediately", false, "False: new proposals require the Mod Settings button and a game restart. Active rules still apply automatically.");
        maintenanceStatus = Config.Bind("贴图检查与更新", "状态", "尚未检查", "目录检查不能代替进入关卡后的材质观察。");
        store = new TextureRepairStore(Path.Combine(Paths.ConfigPath, "CustomLevelTextureRepair"));
        try
        {
            bool promoted = store.ActivatePending(); LoadActive();
            maintenanceStatus.Value = (promoted ? L("已应用待更新规则：", "Pending rules activated: ") : L("当前有效规则：", "Current rules: ")) + activeRules.Count + L(" 条。进入关卡后可检查贴图。", " rules. Enter a level to inspect its materials.");
        }
        catch (Exception e)
        {
            Logger.LogWarning("Pending repair plan not applied: " + e.Message);
            try { LoadActive(); } catch (Exception) { }
            maintenanceStatus.Value = L("待更新文件读取失败，保留旧规则。详见日志。", "Pending rules could not be loaded; previous rules retained. See log.");
        }
        try { if (File.Exists(store.ObservationsPath)) foreach (TextureRepairRecord r in TextureRepairStore.Read(store.ObservationsPath).Records) observed[r.Key()] = r; }
        catch (Exception e) { Logger.LogWarning("Observation history ignored: " + e.Message); }
        StartCoroutine(ConnectSettings());
    }
    IEnumerator ConnectSettings()
    {
        for (int i = 0; i < 120; i++)
        {
            bool connected = false;
            PluginInfo info;
            if (Chainloader.PluginInfos.TryGetValue(SettingsGuid, out info) && info.Instance != null)
            {
                TextureSettingsBridge bridge = new TextureSettingsBridge(info.Instance);
                try
                {
                    if (bridge.Ready())
                    {
                        bridge.Register(OwnerGuid, L("自定义关卡贴图修复", "Custom Level Texture Repair"), L("检查 levels，准备兼容规则，重启后应用。", "Inspect levels, prepare compatibility rules, restart to apply."), Config);
                        bridge.Category = L("贴图检查与更新", "Texture checks and updates");
                        ConfigureLabels(bridge);
                        bridge.Button(OwnerGuid, "scan-and-stage", L("检查 levels 并准备修复", "Inspect levels and prepare repairs"), L("检查全部关卡文件及已观测材质，保存待更新规则；重启后生效。未进入的关卡标为待观察。", "Inspect files and observed materials, stage rules for the next restart. Unvisited levels remain unobserved."), BeginMaintenance, delegate { return !maintenanceBusy; });
                        bridge.Button(OwnerGuid, "discard-pending", L("撤销待更新", "Discard pending update"), L("只撤销尚未应用的规则，不影响当前规则或关卡文件。", "Discard pending rules without changing active rules or level files."), DiscardPending, delegate { return !maintenanceBusy && File.Exists(store.PendingPath); });
                        settingsBridge = bridge; connected = true;
                        Logger.LogInfo("Mod Settings v1 buttons registered; OC2ModSettings.dll unchanged.");
                    }
                    else bridge.Dispose();
                }
                catch (Exception e) { bridge.Dispose(); if (i == 119) Logger.LogWarning("Mod Settings integration: " + e.Message); }
            }
            if (connected) yield break;
            yield return new WaitForSecondsRealtime(1f);
        }
        Logger.LogWarning("Mod Settings buttons unavailable; no ready v1 service found.");
    }
    void RefreshSettings()
    { if (settingsBridge != null) try { settingsBridge.Refresh(); } catch (Exception e) { Logger.LogWarning("Settings refresh: " + e.Message); } }
    void DiscardPending()
    {
        try { if (File.Exists(store.PendingPath)) File.Delete(store.PendingPath); maintenanceStatus.Value = L("已撤销待更新，当前规则保留。", "Pending update discarded; active rules retained."); RefreshSettings(); }
        catch (Exception e) { maintenanceStatus.Value = L("撤销失败：", "Could not discard: ") + e.Message; }
    }
    static bool IsCache(string root, string path)
    {
        string relative = path.Substring(root.Length).TrimStart(Path.DirectorySeparatorChar);
        foreach (string part in relative.Split(Path.DirectorySeparatorChar)) if (part.StartsWith(".", StringComparison.Ordinal)) return true;
        return false;
    }
    string FingerprintForScene(string scene)
    {
        string value, file;
        if (ambiguousScenes.Contains(scene)) return "";
        if (loadedFingerprints.TryGetValue(scene, out value)) return value;
        if (ambiguousScenes.Contains(scene) || !sceneFiles.TryGetValue(scene, out file)) return "";
        try { value = TextureRepairStore.Fingerprint(file); loadedFingerprints[scene] = value; return value; }
        catch (Exception e) { Logger.LogWarning("Scene fingerprint: " + e.Message); return ""; }
    }
    static string StablePath(Transform transform)
    {
        List<string> parts = new List<string>();
        for (Transform t = transform; t != null; t = t.parent)
        {
            int ordinal = 0;
            if (t.parent != null) { for (int i = 0; i < t.GetSiblingIndex(); i++) if (t.parent.GetChild(i).name == t.name) ordinal++; }
            else foreach (GameObject root in t.gameObject.scene.GetRootGameObjects())
            { if (root.transform == t) break; if (root.name == t.name) ordinal++; }
            parts.Add(Uri.EscapeDataString(t.name) + "#" + ordinal);
        }
        parts.Reverse(); return string.Join("/", parts.ToArray());
    }
    TextureRepairRecord Record(Scene scene, Transform t, Material m, Kind kind, Tex texture)
    {
        return new TextureRepairRecord {
            Scene = scene.name, ObjectPath = StablePath(t), Material = m.name,
            OriginalShader = m.shader == null ? "" : m.shader.name,
            Texture = texture.texture == null ? "" : texture.texture.name,
            Width = texture.texture == null ? 0 : texture.texture.width,
            Height = texture.texture == null ? 0 : texture.texture.height,
            Kind = kind.ToString(), Fingerprint = FingerprintForScene(scene.name)
        };
    }
    void Remember(TextureRepairRecord record)
    {
        if (observed.Count >= 50000 && !observed.ContainsKey(record.Key())) return;
        TextureRepairRecord prior;
        bool changed = !observed.TryGetValue(record.Key(), out prior) || prior.Status != record.Status || prior.TargetShader != record.TargetShader || prior.Fingerprint != record.Fingerprint;
        observed[record.Key()] = record; observationsDirty |= changed;
    }
    TextureRepairRecord ActiveRule(TextureRepairRecord match)
    {
        TextureRepairRecord result;
        return activeRules.TryGetValue(match.Key(), out result) && result.Width == match.Width && result.Height == match.Height && TextureRepairStore.Matches(result, match.Fingerprint) ? result : null;
    }
    Target PlannedTarget(TextureRepairRecord rule, Material original, Shader[] shaders)
    {
        if (rule == null) return null;
        foreach (Shader shader in shaders)
        {
            if (shader == null || shader == original.shader || Broken(shader) || shader.name != rule.TargetShader) continue;
            if (shader.name == rule.OriginalShader) return new Target { shader = shader, sameShader = true };
            Material probe = new Material(shader); string property = Property(probe); Destroy(probe);
            if (property != null) return new Target { shader = shader, property = property };
        }
        return null;
    }
    void SaveObservations(bool force)
    {
        if (!observationsDirty || (!force && Time.realtimeSinceStartup - lastObservationSave < 10f)) return;
        try
        {
            TextureRepairStore.Write(store.ObservationsPath, new TextureRepairDocument { Records = new List<TextureRepairRecord>(observed.Values).ToArray() });
            observationsDirty = false; lastObservationSave = Time.realtimeSinceStartup;
        }
        catch (Exception e) { Logger.LogWarning("Observation save: " + e.Message); lastObservationSave = Time.realtimeSinceStartup; }
    }
    void BeginMaintenance()
    {
        if (maintenanceBusy) return;
        maintenanceBusy = true; maintenanceStatus.Value = L("正在检查 levels 和已观测材质…", "Inspecting levels and observed materials..."); RefreshSettings(); StartCoroutine(PrepareMaintenance());
    }
    IEnumerator PrepareMaintenance()
    {
        IEnumerator worker = PrepareWorker();
        while (true)
        {
            bool next;
            try { next = worker.MoveNext(); }
            catch (Exception e) { maintenanceStatus.Value = L("检查失败，原规则保留：", "Check failed; previous rules retained: ") + e.Message; Logger.LogError(e); break; }
            if (!next) break;
            yield return worker.Current;
        }
        maintenanceBusy = false; RefreshSettings();
    }
    IEnumerator PrepareWorker()
    {
        if (!Directory.Exists(LevelsRoot)) throw new DirectoryNotFoundException(L("未找到 OC2DIYLevel/levels", "OC2DIYLevel/levels was not found"));
        Discover();
        yield return WarmTestFloorMaterials();
        StringBuilder report = new StringBuilder();
        report.AppendLine("自定义关卡贴图维护 " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        report.AppendLine("扫描入口：" + LevelsRoot);
        report.AppendLine("检查文件及已加载/历史观测材质；不自动进入关卡，不修改关卡、存档、原贴图或 Mod Settings DLL。");
        Dictionary<string, string> current = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        int files = 0, invalidFiles = 0;
        foreach (string file in Directory.GetFiles(LevelsRoot, "*", SearchOption.AllDirectories))
        {
            if (IsCache(LevelsRoot, file)) continue;
            string name = Path.GetFileName(file);
            if ((!name.StartsWith("s_", StringComparison.OrdinalIgnoreCase) && !name.StartsWith("info_", StringComparison.OrdinalIgnoreCase)) || name.IndexOf('.') >= 0) continue;
            files++; bool valid;
            using (FileStream stream = File.OpenRead(file))
            { byte[] magic = new byte[8]; int n = stream.Read(magic, 0, 8); valid = n == 8 && Encoding.ASCII.GetString(magic) == "UnityFS\0"; }
            report.AppendLine((valid ? "文件头正常：" : "文件头异常：") + file.Substring(LevelsRoot.Length));
            if (!valid) invalidFiles++;
            if (name.StartsWith("s_", StringComparison.OrdinalIgnoreCase) && valid && !ambiguousScenes.Contains(name)) current[name] = TextureRepairStore.Fingerprint(file);
            yield return null;
        }
        foreach (KeyValuePair<Scene, State> entry in new List<KeyValuePair<Scene, State>>(states))
        { if (entry.Key.isLoaded && entry.Value.active) ScanScene(entry.Key, entry.Value); yield return null; }
        NormalizeTestFloorCandidates();
        Dictionary<string, TextureRepairRecord> nextRules = new Dictionary<string, TextureRepairRecord>(); int stale = 0, unresolved = 0;
        foreach (TextureRepairRecord rule in activeRules.Values)
        {
            string fp;
            if (current.TryGetValue(rule.Scene, out fp) && TextureRepairStore.Matches(rule, fp))
            {
                TextureRepairRecord latest;
                if (observed.TryGetValue(rule.Key(), out latest) && latest.Fingerprint == fp && latest.Status == "healthy") continue;
                nextRules[rule.Key()] = rule;
            }
        }
        HashSet<string> visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (TextureRepairRecord record in observed.Values)
        {
            string fp;
            if (!current.TryGetValue(record.Scene, out fp) || record.Fingerprint != fp) { stale++; continue; }
            visited.Add(record.Scene);
            if (record.Ready) nextRules[record.Key()] = record;
            else if (record.Status != "healthy" && record.Status != "optional_material") unresolved++;
            if (record.Status != "healthy" && record.Status != "optional_material") report.AppendLine(ReportStatus(record) + " | " + record.Scene + " | " + record.ObjectPath + " | " + record.Material + " | texture=" + record.Texture + " | shader=" + record.OriginalShader + " -> " + record.TargetShader);
        }
        report.AppendLine("\n未观测关卡（不能仅凭文件判定画面是否正常）：");
        foreach (string scene in current.Keys) if (!visited.Contains(scene)) report.AppendLine(scene);
        foreach (string scene in ambiguousScenes) report.AppendLine("重名场景，暂停持久规则：" + scene);
        report.AppendLine("\n文件=" + files + "，文件头异常=" + invalidFiles + "，磁盘场景=" + current.Count + "，已观测=" + visited.Count + "，待应用=" + nextRules.Count + "，未解决=" + unresolved + "，过期=" + stale);
        SaveObservations(true); Directory.CreateDirectory(store.DirectoryPath);
        File.WriteAllText(Path.Combine(store.DirectoryPath, "last-report.txt"), report.ToString(), Encoding.UTF8);
        TextureRepairStore.Write(store.PendingPath, new TextureRepairDocument { Records = new List<TextureRepairRecord>(nextRules.Values).ToArray() });
        maintenanceStatus.Value = L("检查完成：", "Check complete: ") + current.Count + L(" 关，已观测 ", " scenes; observed ") + visited.Count + L("；待应用 ", "; pending ") + nextRules.Count + L(" 条，未解决 ", "; unresolved ") + unresolved + L("。请退出并重启游戏。", ". Quit and restart to apply.");
        Logger.LogInfo(maintenanceStatus.Value + " Report=" + Path.Combine(store.DirectoryPath, "last-report.txt"));
    }
    string ReportStatus(TextureRepairRecord record)
    {
        if (record.TargetShader != null && record.TargetShader.StartsWith(TestMaterialPrefix, StringComparison.Ordinal)) return L("待应用：test_1 地板（替代缺失材质）", "Pending: test_1 floor for a missing material");
        if (record.Status == "missing_material_test1_unavailable") return L("缺少材质，test_1 地板资源尚未就绪", "Missing material; test_1 floor resource is unavailable");
        if (record.TargetShader != null && record.TargetShader.StartsWith(LocalMaterialPrefix, StringComparison.Ordinal)) return L("待应用：本地替代地板（非原贴图）", "Pending: local floor substitute (not original texture)");
        if (record.Status == "missing_material") return L("缺少材质", "Missing material");
        if (record.Status == "missing_material_no_local_source") return L("缺少材质，未找到本地替代来源", "Missing material; no local substitute found");
        if (record.Status == "missing_texture") return L("缺少贴图", "Missing texture");
        if (record.Status == "missing_shader") return L("缺少兼容着色器", "Missing compatible shader");
        if (record.Status == "candidate") return L("待应用：着色器兼容规则", "Pending: shader compatibility rule");
        return record.Status;
    }
    void ShutdownMaintenance()
    {
        if (uiLanguage != null) uiLanguage.SettingChanged -= LanguageChanged;
        if (store != null) SaveObservations(true);
        if (settingsBridge != null) settingsBridge.Dispose(); settingsBridge = null;
    }
}
