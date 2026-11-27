using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class TestFloorPolicy
{
    public static string MaterialForObject(string name)
    {
        if (!LocalFloorPolicy.IsFloorName(name)) return null;
        string n = name.Trim();
        if (n.EndsWith("(Clone)", StringComparison.OrdinalIgnoreCase)) n = n.Substring(0, n.Length - 7).Trim();
        return n.Equals("Floor", StringComparison.OrdinalIgnoreCase) ? "mat_city_path_02" : "mat_city_path_01";
    }
}
public sealed partial class CustomLevelFloorShaderFix
{
    const string TestMaterialPrefix = "test1-material:";
    readonly Dictionary<string, Material> testFloors = new Dictionary<string, Material>();
    bool testFloorLoading, testFloorAttempted;
    const string TestAssetPrefix = "assets/levelsets/test/materials/";
    IEnumerator WarmTestFloorMaterials()
    {
        while (testFloorLoading) yield return null;
        if (testFloors.Count == 2 || testFloorAttempted || !localFloorFallback.Value) yield break;
        testFloorAttempted = true;
        testFloorLoading = true;
        IEnumerator work = LoadTestFloorWorker();
        while (true)
        {
            bool next;
            try { next = work.MoveNext(); }
            catch (Exception e) { Logger.LogWarning("test_1 floor source unavailable: " + e.Message); break; }
            if (!next) break;
            yield return work.Current;
        }
        testFloorLoading = false;
    }
    IEnumerator LoadTestFloorWorker()
    {
        AssetBundle bundle = null; bool ownBundle = false;
        foreach (AssetBundle loaded in AssetBundle.GetAllLoadedAssetBundles())
            if (loaded != null && (loaded.name == "test_level/info_test_level" || loaded.name == "info_test_level")) { bundle = loaded; break; }
        if (bundle == null)
        {
            string path = Path.Combine(Path.Combine(LevelsRoot, "test_level"), "info_test_level");
            if (!File.Exists(path)) throw new FileNotFoundException("test_level/info_test_level is required for the test_1 floor.", path);
            AssetBundleCreateRequest request = AssetBundle.LoadFromFileAsync(path);
            yield return request;
            bundle = request.assetBundle; ownBundle = bundle != null;
            if (bundle == null)
                foreach (AssetBundle loaded in AssetBundle.GetAllLoadedAssetBundles())
                    if (loaded != null && loaded.name == "test_level/info_test_level") { bundle = loaded; break; }
        }
        if (bundle == null) throw new IOException("Could not read test_1 resource bundle.");
        try
        {
            foreach (string name in new string[] { "mat_city_path_02", "mat_city_path_01" })
            {
                if (testFloors.ContainsKey(name) && testFloors[name] != null) continue;
                Material source = bundle.LoadAsset<Material>(TestAssetPrefix + name + ".mat");
                if (source == null || Broken(source.shader)) throw new IOException("test_1 material or compatible shader unavailable: " + name);
                Tex texture = ReadTexture(source, null, null, Kind.Mesh);
                if (texture.texture == null) throw new IOException("test_1 floor texture dependency is unavailable: " + name);
                Material pinned = new Material(source); pinned.name = name + " [Pinned test_1 source]";
                pinned.hideFlags = HideFlags.DontUnloadUnusedAsset;
                testFloors[name] = pinned; owned.Add(pinned);
                Logger.LogInfo("TEST1_SOURCE_READY material=" + name + " texture=" + texture.texture.name + " scale=" + texture.scale);
            }
        }
        finally
        {
            // A bundle loaded here is released so the level loader can load it normally later.
            // Cloned materials retain referenced textures; never unload a bundle borrowed from another plugin.
            if (ownBundle && bundle != null) bundle.Unload(false);
        }
    }
    Material TestFloorSource(string objectName)
    {
        string name = TestFloorPolicy.MaterialForObject(objectName); Material material;
        return name != null && testFloors.TryGetValue(name, out material) && material != null ? material : null;
    }
    void NormalizeTestFloorCandidates()
    {
        if (!localFloorFallback.Value) return;
        foreach (TextureRepairRecord record in observed.Values)
        {
            if (record.Kind != "Mesh" || !record.Material.StartsWith("<null-slot:", StringComparison.Ordinal)) continue;
            string segment = record.ObjectPath.Substring(record.ObjectPath.LastIndexOf('/') + 1);
            int suffix = segment.LastIndexOf('#'); if (suffix >= 0) segment = segment.Substring(0, suffix);
            string name = Uri.UnescapeDataString(segment); Material source = TestFloorSource(name);
            if (source == null) continue;
            record.Status = "candidate"; record.TargetShader = TestMaterialPrefix + TestFloorPolicy.MaterialForObject(name);
            observationsDirty = true;
        }
    }
    void ReleaseTestFloorMaterials()
    {
        foreach (Material material in testFloors.Values) if (material != null) { owned.Remove(material); Destroy(material); }
        testFloors.Clear();
    }
}
