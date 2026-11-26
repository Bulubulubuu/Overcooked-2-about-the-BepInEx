using System;
using System.IO;
using System.Reflection;
class LocalTests
{
    static int count;
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);count++;}
    static void Main(string[] args)
    {
        AppDomain.CurrentDomain.AssemblyResolve+=delegate(object sender,ResolveEventArgs e){string name=new AssemblyName(e.Name).Name+".dll";foreach(string d in new string[]{args[1],args[2]}){string f=Path.Combine(d,name);if(File.Exists(f))return Assembly.LoadFrom(f);}return null;};
        Assembly a=Assembly.LoadFrom(args[0]);Type policy=a.GetType("LocalFloorPolicy",true);MethodInfo floor=policy.GetMethod("IsFloorName"),score=policy.GetMethod("SourceScore");
        // Names taken from the four actual MeshRenderer owners in s_young_2.
        foreach(string n in new string[]{"Floor","Floor (1)","Floor (2)","Floor (3)","FloorA4","Floor(Clone)"})Check((bool)floor.Invoke(null,new object[]{n}),"recognize "+n);
        foreach(string n in new string[]{"Col_Floor","FloorBurner","Floor_fire","Counter","Rain","Ground"})Check(!(bool)floor.Invoke(null,new object[]{n}),"do not fabricate "+n);
        Check((int)score.Invoke(null,new object[]{"mat_city_road_01"})>0,"real loaded local road may be substituted");
        Check((int)score.Invoke(null,new object[]{"mat_city_road_01"})>(int)score.Invoke(null,new object[]{"mat_city_path_side_01"}),"prefer surface over side wall");
        Check((int)score.Invoke(null,new object[]{"mat_floorburner_01"})==0,"burner is not a floor template");
        Type visual=a.GetType("VisualAssetPolicy",true);MethodInfo token=visual.GetMethod("ContainsToken");
        Check(!(bool)token.Invoke(null,new object[]{"Design","sign"}),"Design is not a sign");
        Check(!(bool)token.Invoke(null,new object[]{"toy_train","rain"}),"train is not rain");
        Check((bool)token.Invoke(null,new object[]{"sushi_sign_03_day","sign"}),"actual sushi sign recognized");
        Check((bool)token.Invoke(null,new object[]{"PFX_ConfettiRain","rain"}),"actual ConfettiRain recognized");
        Check((bool)token.Invoke(null,new object[]{"dlc08_cannon","cannon"}),"actual cannon recognized");
        Type lang=a.GetType("TextureRepairLanguage",true);MethodInfo select=lang.GetMethod("Select");
        Check((string)select.Invoke(null,new object[]{"简体中文","检查","Inspect"})=="检查","simplified Chinese selection");
        Check((string)select.Invoke(null,new object[]{"English","检查","Inspect"})=="Inspect","English selection");
        Check((string)select.Invoke(null,new object[]{null,"检查","Inspect"})=="检查","default simplified Chinese");
        Type record=a.GetType("TextureRepairRecord",true);object r=Activator.CreateInstance(record);
        foreach(string[] v in new string[][]{new string[]{"Scene","s_young_2"},new string[]{"ObjectPath","Art#0/Ground#0/Floor#0"},new string[]{"Material","<null-slot:0>"},new string[]{"OriginalShader",""},new string[]{"Texture",""},new string[]{"Kind","Mesh"},new string[]{"TargetShader","local-material:mat_city_road_01"},new string[]{"Fingerprint","test"},new string[]{"Status","candidate"}})record.GetField(v[0]).SetValue(r,v[1]);
        Check((bool)record.GetProperty("Ready").GetValue(r,null),"local substitute may be staged without inventing original texture");
        Type document=a.GetType("TextureRepairDocument",true);object doc=Activator.CreateInstance(document);Array records=Array.CreateInstance(record,1);records.SetValue(r,0);document.GetField("Records").SetValue(doc,records);
        Type store=a.GetType("TextureRepairStore",true);string path=Path.Combine(args[3],"local.plan");store.GetMethod("Write").Invoke(null,new object[]{path,doc});object read=store.GetMethod("Read").Invoke(null,new object[]{path});object roundtrip=((Array)document.GetField("Records").GetValue(read)).GetValue(0);
        Check((string)record.GetField("TargetShader").GetValue(roundtrip)=="local-material:mat_city_road_01","local source survives restart format");
        Console.WriteLine(count+" local-floor/language checks passed. Actual rendering not tested.");
    }
}
