using System;
using System.IO;
using System.Reflection;
class Tests
{
    static int count;
    static Type plugin, kind;
    static object K(string n) { return Enum.Parse(kind, n); }
    static object Call(string n, params object[] args) { return plugin.GetMethod(n, BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, args); }
    static void Check(bool ok, string name) { if (!ok) throw new Exception(name); count++; }
    static void Main(string[] args)
    {
        AppDomain.CurrentDomain.AssemblyResolve += delegate(object sender, ResolveEventArgs a) {
            string n = new AssemblyName(a.Name).Name + ".dll";
            foreach (string d in new string[] { args[1], args[2] }) { string p = Path.Combine(d,n); if (File.Exists(p)) return Assembly.LoadFrom(p); }
            return null;
        };
        plugin=Assembly.LoadFrom(args[0]).GetType("CustomLevelFloorShaderFix", true);
        kind=plugin.GetNestedType("Kind", BindingFlags.NonPublic);
        // Regression: the original Gl profile must retain its exact scene and sprite boundaries.
        Check((bool)Call("GlFloor","s_gl_level","【哲风壁纸】云朵-卡通-可爱",K("Sprite")),"GL asset");
        Check((bool)Call("GlFloor","s_gl_level 2","【哲风壁纸】云朵-卡通-可爱2",K("Sprite")),"GL copy");
        Check(!(bool)Call("GlFloor","s_young_8","【哲风壁纸】云朵-卡通-可爱",K("Sprite")),"GL scene isolation");
        Check(!(bool)Call("GlFloor","s_gl_level","【哲风壁纸】云朵-卡通-可爱",K("Mesh")),"GL type isolation");
        // Regression: alpha effects may never drop into the opaque game-mesh fallback.
        foreach(string k in new string[]{"Particle","Sprite","UI"})
            Check(!(bool)Call("OpaqueFallbackAllowed",K(k),2000,"Standard"),k+" opacity guard");
        Check(!(bool)Call("OpaqueFallbackAllowed",K("Mesh"),3000,"Standard"),"transparent render queue");
        Check(!(bool)Call("OpaqueFallbackAllowed",K("Mesh"),2000,"Overcooked_2/OC2_gloss_d_alpha"),"alpha shader guard");
        Check((bool)Call("OpaqueFallbackAllowed",K("Mesh"),2000,"Standard"),"opaque mesh accepted");
        string[] properties=(string[])plugin.GetField("TextureProperties",BindingFlags.NonPublic|BindingFlags.Static).GetValue(null);
        Check(Array.IndexOf(properties,"_ColourAlpha")>=0,"actual asset ColourAlpha");
        Check(Array.IndexOf(properties,"_DiffuseTexture")>=0,"actual asset DiffuseTexture");
        Console.WriteLine(count+" policy/regression checks passed. Rendering not tested.");
    }
}
