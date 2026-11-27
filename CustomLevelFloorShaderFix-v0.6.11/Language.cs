using System;
using System.Collections;
using System.Collections.Generic;
using BepInEx.Configuration;
using UnityEngine;
public static class TextureRepairLanguage
{
    public static string Select(string language, string chinese, string english) { return language == "English" ? english : chinese; }
}
public sealed partial class CustomLevelFloorShaderFix
{
    ConfigEntry<string> uiLanguage;
    bool relabeling;
    string L(string chinese, string english) { return TextureRepairLanguage.Select(uiLanguage == null ? null : uiLanguage.Value, chinese, english); }
    void InitializeLanguage()
    {
        uiLanguage = Config.Bind("Interface", "Language", "简体中文", new ConfigDescription("补丁界面语言 / Plugin interface language", new AcceptableValueList<string>("简体中文", "English")));
        uiLanguage.SettingChanged += LanguageChanged;
    }
    void LanguageChanged(object sender, EventArgs e) { if (!relabeling) StartCoroutine(Relabel()); }
    IEnumerator Relabel()
    {
        relabeling = true; yield return null;
        if (settingsBridge != null) { settingsBridge.Dispose(); settingsBridge = null; }
        yield return ConnectSettings(); relabeling = false;
    }
    string Label(string key)
    {
        switch (key)
        {
            case "Language": return L("界面语言", "Interface language");
            case "状态": return L("检查状态", "Check status");
            case "StarCupCompatibility": return L("star 海豹小店：杯子、茶壶和蒸汽兼容", "Star teacup, kettle and steam compatibility");
            case "ForceYiWater": return L("宜：水面兼容规则", "Yi water compatibility");
            case "ForceGlFloor": return L("Gl：云朵地板兼容规则", "Gl cloud floor compatibility");
            case "RepairKnownVisualAssets": return L("大炮、图示和雨效兼容规则", "Cannon, signs and rain compatibility");
            case "ScanAllRenderers": return L("检查全部可视对象", "Inspect all visual objects");
            case "ForceShaderNames": return L("指定需要替换的着色器", "Force shader names");
            case "AdditionalScenes": return L("额外检查的场景", "Additional scenes");
            case "WatchSeconds": return L("动态对象检查时长（秒）", "Dynamic object observation (seconds)");
            case "VerboseMaterials": return L("显示每轮检查摘要", "Log every scan summary");
            case "ApplyUnplannedImmediately": return L("手动检查后立即应用新规则", "Apply proposals after manual inspection");
            case "UseLocalFloorFallback": return L("缺材质地板：使用 test_1 地板", "Use test_1 for missing floor materials");
            default: return key;
        }
    }
    string Help(string key, string original)
    {
        if (uiLanguage.Value == "English") return original;
        switch (key)
        {
            case "Language": return "可选择简体中文或 English，切换后刷新本补丁面板。";
            case "状态": return "显示目录检查和修复规则准备进度；未进入的关卡需要实际进入后观察。";
            case "StarCupCompatibility": return "仅针对 star 的指定杯子、茶壶、污渍和蒸汽材质，保留原贴图和模型；纯色勺子保留原颜色。";
            case "ForceYiWater": return "检查宜关卡的 blue_water 水面材质。";
            case "ForceGlFloor": return "检查 Gl 云朵壁纸地板。";
            case "RepairKnownVisualAssets": return "检查命名匹配的大炮、告示图示及雨效的通用着色器。";
            case "ScanAllRenderers": return "仅点击检查按钮时，检查网格、精灵、粒子、拖尾和界面图示；游戏中不自动检查。";
            case "ForceShaderNames": return "填写着色器精确名称，多个名称用英文分号分隔。通常留空。";
            case "AdditionalScenes": return "额外场景名用英文分号分隔，保留名称内部空格。";
            case "WatchSeconds": return "旧版设置，手动模式下不再使用。";
            case "VerboseMaterials": return "开启后每轮都输出检查摘要，日志会更多。";
            case "ApplyUnplannedImmediately": return "仅影响手动检查：关闭时先生成待应用规则，重启后生效；开启则在点击检查时立即尝试。";
            case "UseLocalFloorFallback": return "原材质缺失时，读取 test_level/info_test_level 中 test_1 的主地板和边条材质；保留 test_1 的贴图与缩放。";
            default: return original;
        }
    }
    void ConfigureLabels(TextureSettingsBridge bridge)
    {
        foreach (KeyValuePair<ConfigDefinition, ConfigEntryBase> pair in Config)
        {
            string section = pair.Key.Section;
            string category = section == "Repair" ? L("修复选项", "Repair") : section == "Discovery" ? L("场景检查", "Discovery") : section == "Logging" ? L("日志", "Logging") : section == "Interface" ? L("界面", "Interface") : L("贴图检查与更新", "Texture checks and updates");
            Dictionary<string,object> tags = new Dictionary<string,object> {
                { "DispName", Label(pair.Key.Key) }, { "Description", Help(pair.Key.Key, pair.Value.Description.Description) }, { "Category", category }
            };
            if (pair.Key.Key == "WatchSeconds" || pair.Key.Key == "VerboseMaterials") tags["Browsable"] = false;
            if (pair.Value == maintenanceStatus) { tags["ReadOnly"] = true; tags["HideDefaultButton"] = true; tags["Order"] = -110; tags["CategoryOrder"] = (int?) -100; }
            bridge.Configure(pair.Value, tags);
        }
    }
}
