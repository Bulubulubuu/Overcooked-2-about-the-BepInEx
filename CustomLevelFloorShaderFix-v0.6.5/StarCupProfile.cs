using System;
using BepInEx.Configuration;

public static class StarCupPolicy
{
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
        if (kind == "Particle") return name.Equals("mat_web_pfx_steam", StringComparison.OrdinalIgnoreCase);
        if (kind != "Mesh") return false;
        return name.Equals("mat_web_teacup", StringComparison.OrdinalIgnoreCase) || name.Equals("mat_Kettle_Anniversary_01", StringComparison.OrdinalIgnoreCase) || name.Equals("mat_web_glassdirt", StringComparison.OrdinalIgnoreCase) || name.Equals("mat_Birthday_TeaCup_01_6", StringComparison.OrdinalIgnoreCase);
    }
    public static bool IsSolidColor(string material) { return BaseName(material).Equals("mat_Birthday_TeaCup_01_6", StringComparison.OrdinalIgnoreCase); }
}
public sealed partial class CustomLevelFloorShaderFix
{
    ConfigEntry<bool> starCups;
}
