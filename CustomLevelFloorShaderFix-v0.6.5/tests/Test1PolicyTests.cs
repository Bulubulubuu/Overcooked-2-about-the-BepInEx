using System;
using System.IO;
using System.Reflection;
class Test1Tests
{
    static int count;
    static void Check(bool ok,string name){if(!ok)throw new Exception(name);count++;}
    static void Main(string[] args)
    {
        AppDomain.CurrentDomain.AssemblyResolve+=delegate(object sender,ResolveEventArgs e){string name=new AssemblyName(e.Name).Name+".dll";foreach(string d in new string[]{args[1],args[2]}){string f=Path.Combine(d,name);if(File.Exists(f))return Assembly.LoadFrom(f);}return null;};
        Assembly a=Assembly.LoadFrom(args[0]);MethodInfo select=a.GetType("TestFloorPolicy",true).GetMethod("MaterialForObject");
        Check((string)select.Invoke(null,new object[]{"Floor"})=="mat_city_path_02","main floor matches test_1 main source");
        foreach(string name in new string[]{"Floor (1)","Floor (2)","Floor (3)"})Check((string)select.Invoke(null,new object[]{name})=="mat_city_path_01","strip matches test_1 "+name);
        Check((string)select.Invoke(null,new object[]{"Floor(Clone)"})=="mat_city_path_02","main clone uses main texture");
        Check(select.Invoke(null,new object[]{"FloorBurner"})==null,"burner excluded");
        Check(select.Invoke(null,new object[]{"Rain"})==null,"rain excluded");
        MethodInfo optional=a.GetType("ParticleSlotPolicy",true).GetMethod("IsUnusedTrailSlot");
        Check((bool)optional.Invoke(null,new object[]{1,true,false}),"disabled trail is optional");
        Check(!(bool)optional.Invoke(null,new object[]{1,true,true}),"enabled trail still inspected");
        Check(!(bool)optional.Invoke(null,new object[]{0,true,false}),"main particle slot not suppressed");
        Check(!(bool)optional.Invoke(null,new object[]{1,false,false}),"unknown particle component not suppressed");
        Type record=a.GetType("TextureRepairRecord",true);object r=Activator.CreateInstance(record);
        foreach(string[] v in new string[][]{new string[]{"Scene","s_young_2"},new string[]{"ObjectPath","Art#0/Ground#0/Floor#0"},new string[]{"Material","<null-slot:0>"},new string[]{"OriginalShader",""},new string[]{"Texture",""},new string[]{"Kind","Mesh"},new string[]{"TargetShader","test1-material:mat_city_path_02"},new string[]{"Fingerprint","test"},new string[]{"Status","candidate"}})record.GetField(v[0]).SetValue(r,v[1]);
        Check((bool)record.GetProperty("Ready").GetValue(r,null),"test_1 rule can be prepared");
        Type document=a.GetType("TextureRepairDocument",true);object doc=Activator.CreateInstance(document);Array records=Array.CreateInstance(record,1);records.SetValue(r,0);document.GetField("Records").SetValue(doc,records);
        Type store=a.GetType("TextureRepairStore",true);string path=Path.Combine(args[3],"test1.plan");store.GetMethod("Write").Invoke(null,new object[]{path,doc});object read=store.GetMethod("Read").Invoke(null,new object[]{path});object rt=((Array)document.GetField("Records").GetValue(read)).GetValue(0);
        Check((string)record.GetField("TargetShader").GetValue(rt)=="test1-material:mat_city_path_02","test_1 source persists across restart");
        Console.WriteLine(count+" test_1 mapping/trail/persistence checks passed; no GPU rendering test.");
    }
}
