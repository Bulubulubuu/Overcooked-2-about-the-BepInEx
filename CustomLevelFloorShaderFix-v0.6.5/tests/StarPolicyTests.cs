using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
class Tests
{
    static int count;
    static void Check(bool ok,string name){if(!ok)throw new Exception(name);count++;}
    static void Main(string[] args)
    {
        AppDomain.CurrentDomain.AssemblyResolve+=delegate(object sender,ResolveEventArgs e){string name=new AssemblyName(e.Name).Name+".dll";foreach(string d in new string[]{args[1],args[2]}){string p=Path.Combine(d,name);if(File.Exists(p))return Assembly.LoadFrom(p);}return null;};
        Assembly a=Assembly.LoadFrom(args[0]);Type policy=a.GetType("StarCupPolicy",true);MethodInfo match=policy.GetMethod("Matches"),solid=policy.GetMethod("IsSolidColor");
        foreach(string name in new string[]{"mat_web_teacup","mat_Kettle_Anniversary_01","mat_web_glassdirt","mat_Birthday_TeaCup_01_6"})Check((bool)match.Invoke(null,new object[]{"star_8","Mesh",name}),"actual shared material "+name);
        Check((bool)match.Invoke(null,new object[]{"star_8","Mesh","mat_web_teacup (Instance)"}),"instance material recognized");
        Check((bool)match.Invoke(null,new object[]{"star_8","Particle","mat_web_pfx_steam"}),"steam type recognized");
        Check(!(bool)match.Invoke(null,new object[]{"s_young_2","Mesh","mat_web_teacup"}),"other level families isolated");
        Check(!(bool)match.Invoke(null,new object[]{"star_8","UI","mat_web_teacup"}),"UI isolated");
        Check(!(bool)match.Invoke(null,new object[]{"star_8","Mesh","mat_web_teacup_other"}),"unrelated prefix material isolated");
        Check(!(bool)match.Invoke(null,new object[]{"star_8","Particle","mat_web_teacup"}),"mesh material not treated as steam");
        Check((bool)solid.Invoke(null,new object[]{"mat_Birthday_TeaCup_01_6"}),"known textureless spoon may keep solid color");
        Check(!(bool)solid.Invoke(null,new object[]{"mat_web_teacup"}),"cup must retain actual texture");
        Type catalog=a.GetType("TextureSceneCatalog",true);MethodInfo read=catalog.GetMethod("ReadSceneNames"),discover=catalog.GetMethod("Discover");
        string root=args[3];string star=Path.Combine(root,"star/star_8");
        string[] names=(string[])read.Invoke(null,new object[]{star});Check(Array.IndexOf(names,"star_8")>=0,"actual star bundle found without s_ prefix");
        names=(string[])read.Invoke(null,new object[]{Path.Combine(root,"test_level/s_test_level")});Check(Array.IndexOf(names,"s_test_level")>=0,"existing s_ bundles preserved");
        names=(string[])read.Invoke(null,new object[]{Path.Combine(root,"star/info_star")});Check(names.Length==0,"info bundle is not a scene");
        object[] dargs=new object[]{root,null,null};object result=discover.Invoke(null,dargs);IDictionary<string,string> scenes=(IDictionary<string,string>)result;
        Check(scenes.ContainsKey("star_8"),"star_8 included in full levels scan");Check(scenes.ContainsKey("star_1_1"),"star_1_1 included");
        Console.WriteLine("Current level catalog: "+scenes.Count+" scene names, "+((List<string>)dargs[2]).Count+" warnings.");
        string scratch=args[4];Directory.CreateDirectory(Path.Combine(scratch,"one"));Directory.CreateDirectory(Path.Combine(scratch,"two"));Directory.CreateDirectory(Path.Combine(scratch,".diylevel-fastinit"));
        File.Copy(star,Path.Combine(scratch,"one/bare_name"));File.Copy(star,Path.Combine(scratch,"two/also_bare"));File.Copy(star,Path.Combine(scratch,".diylevel-fastinit/cache"));
        dargs=new object[]{scratch,null,null};scenes=(IDictionary<string,string>)discover.Invoke(null,dargs);
        Check(scenes.Count==1,"scene identity from metadata, no filename guessing");Check(((HashSet<string>)dargs[1]).Contains("star_8"),"duplicate scene identity detected");
        string invalid=Path.Combine(scratch,"plain_data");File.WriteAllText(invalid,"{ this is not a Unity scene bundle but is long enough for a header }");Check(((string[])read.Invoke(null,new object[]{invalid})).Length==0,"plain data ignored");
        string truncated=Path.Combine(scratch,"truncated");File.WriteAllBytes(truncated,new byte[]{1,2,3});Check(((string[])read.Invoke(null,new object[]{truncated})).Length==0,"tiny data ignored");
        string old=args[5];Check(Array.IndexOf((string[])read.Invoke(null,new object[]{old}),"s_pear_1")>=0,"compressed LZ4 UnityFS directory supported");
        Console.WriteLine(count+" star/cup/catalog checks passed; actual rendering not tested.");
    }
}
