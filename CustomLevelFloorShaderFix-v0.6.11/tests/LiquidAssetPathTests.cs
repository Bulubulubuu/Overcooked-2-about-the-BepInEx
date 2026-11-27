using System;


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
class Check { static void Main() { string[] names={"Water","Milk","Honeytea","Milktea"}; foreach(string n in names) { string a="assets/commonw5/kettle/textures/t_birthday_tea_"+n.ToLowerInvariant()+"_0.png"; if(!StarCupPolicy.AssetMatches(a,"t_Birthday_Tea_"+n+"_0")) throw new System.Exception(n); if(StarCupPolicy.AssetMatches(a,"noise_round")) throw new System.Exception("unrelated"); } System.Console.WriteLine("8 asset path checks passed"); } }