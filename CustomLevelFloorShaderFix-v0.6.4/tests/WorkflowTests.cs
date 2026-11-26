using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using BepInEx.Configuration;
class Lease : IDisposable { public static int disposed; public void Dispose() { disposed++; } }
public class SettingsServiceStub
{
    public IDictionary<string,object> descriptor;
    public int actions, registers, refreshes, configured;
    public int GetApiVersionV1() { return 1; }
    public string GetStatusV1() { return "ready"; }
    public IDisposable RegisterConfigV1(string owner,string name,string description,ConfigFile config) { registers++; return new Lease(); }
    public IDisposable RegisterActionV1(string owner,string id,IDictionary<string,object> d) { actions++; descriptor=d; return new Lease(); }
    public IDisposable ConfigureEntryV1(ConfigEntryBase e,IDictionary<string,object> d) { if (!(bool)d["ReadOnly"]) throw new Exception("not readonly"); configured++;return new Lease(); }
    public void RefreshV1() { refreshes++; }
}
class WorkflowTests
{
    static int count;
    static void Check(bool ok,string name) { if(!ok)throw new Exception(name);count++; }
    static TextureRepairRecord Record() { return new TextureRepairRecord { Scene="s_yi _level", ObjectPath="地板#0/水面#0", Material="mat_blue_water", OriginalShader="Standard", Texture="blue_water", Kind="Mesh", TargetShader="Unlit/Texture", Fingerprint="abc", Status="candidate",Width=1024,Height=1024 }; }
    static void Throws(Action action,string name) { bool threw=false;try{action();}catch(Exception){threw=true;}Check(threw,name); }
    static void Main(string[] args)
    {
        AppDomain.CurrentDomain.AssemblyResolve += delegate(object sender,ResolveEventArgs e) {
            string name=new AssemblyName(e.Name).Name+".dll";
            foreach(string dir in new string[]{args[2],args[3]}) {string path=Path.Combine(dir,name);if(File.Exists(path))return Assembly.LoadFrom(path);}
            return null;
        };
        string root=Path.GetFullPath(args[0]);Directory.CreateDirectory(root);
        TextureRepairStore store=new TextureRepairStore(Path.Combine(root,"rules"));
        TextureRepairRecord r=Record();TextureRepairDocument first=new TextureRepairDocument { Records=new TextureRepairRecord[]{r} };
        Check(r.Ready,"candidate ready");Check(!File.Exists(store.ActivePath),"no rule applied before stage");
        TextureRepairStore.Write(store.PendingPath,first);
        Check(!File.Exists(store.ActivePath),"stage does not modify active");
        Check(TextureRepairStore.Read(store.PendingPath).Records[0].Scene=="s_yi _level","scene spaces retained");
        Check(TextureRepairStore.Read(store.PendingPath).Records[0].ObjectPath==r.ObjectPath,"Chinese path roundtrip");
        Check(store.ActivatePending(),"next-start promotion");
        Check(File.Exists(store.ActivePath)&&!File.Exists(store.PendingPath),"pending consumed after promotion");
        Check(!store.ActivatePending(),"no repeat activation");
        Check(TextureRepairStore.Matches(r,"abc"),"fingerprint match");Check(!TextureRepairStore.Matches(r,"changed"),"stale fingerprint refused");
        string before=Convert.ToBase64String(File.ReadAllBytes(store.ActivePath));
        File.WriteAllText(store.PendingPath,"broken xml");Throws(delegate{store.ActivatePending();},"corrupt pending refused");
        Check(Convert.ToBase64String(File.ReadAllBytes(store.ActivePath))==before,"corrupt pending preserves previous active");
        r.Status="missing_texture";TextureRepairStore.Write(store.PendingPath,first);
        Throws(delegate{store.ActivatePending();},"unresolved record not activated");
        Check(Convert.ToBase64String(File.ReadAllBytes(store.ActivePath))==before,"unresolved pending preserves active");r.Status="candidate";
        TextureRepairStore.Write(store.PendingPath,first);Check(File.Exists(store.PendingPath+".bak"),"atomic overwrite backup");store.ActivatePending();
        Check(File.Exists(store.ActivePath+".bak"),"active backup exists");
        TextureRepairStore.Write(store.PendingPath,new TextureRepairDocument());store.ActivatePending();
        Check(TextureRepairStore.Read(store.ActivePath).Records.Length==0,"empty plan can clear old rules");
        Throws(delegate{TextureRepairStore.Write(store.ActivePath,new TextureRepairDocument{Schema=99});},"unknown schema refused");
        string hostile=Path.Combine(root,"hostile.xml");File.WriteAllText(hostile,"<!DOCTYPE x [<!ENTITY f SYSTEM 'file:///etc/passwd'>]><TextureRepairPlan>&f;</TextureRepairPlan>");
        Throws(delegate{TextureRepairStore.Read(hostile);},"XML input rejected without an XML parser");
        string scene=Path.Combine(root,"s_test_level"),info=Path.Combine(root,"info_test");File.WriteAllText(scene,"UnityFS test a");File.WriteAllText(info,"info");
        string fp=TextureRepairStore.Fingerprint(scene);Check(fp==TextureRepairStore.Fingerprint(scene),"unchanged assets stable");
        File.WriteAllText(scene,"UnityFS test b");Check(fp!=TextureRepairStore.Fingerprint(scene),"scene content invalidates fingerprint");fp=TextureRepairStore.Fingerprint(scene);
        File.WriteAllText(info,"info updated");Check(fp!=TextureRepairStore.Fingerprint(scene),"info changes invalidate fingerprint");
        Check(TextureRepairStore.Fingerprint(Path.Combine(root,"absent"))=="","missing file has no valid fingerprint");
        ConfigFile config=null;ConfigEntryBase status=null;
        SettingsServiceStub stub=new SettingsServiceStub();TextureSettingsBridge bridge=new TextureSettingsBridge(stub);
        Check(bridge.Ready(),"settings ready handshake");bridge.Register("test.owner","测试","description",config);bridge.ReadOnly(status);
        bool ran=false,enabled=true;bridge.Button("test.owner","scan","检查 levels 并准备修复","重启后生效",delegate{ran=true;},delegate{return enabled;});
        Check(stub.actions==1&&stub.registers==1&&stub.configured==1,"v1 registration contract");
        ((Action)stub.descriptor["Execute"])();Check(ran,"button callback executable");enabled=false;Check(!((Func<bool>)stub.descriptor["CanExecute"])(),"busy state can disable button");
        Assembly actual=Assembly.LoadFrom(args[1]);Type actionType=actual.GetType("OC2ModSettings.RegisteredAction",true);
        MethodInfo parse=actionType.GetMethod("Read",BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static);
        object parsed=parse.Invoke(null,new object[]{"test.owner","scan",stub.descriptor});
        Check(parsed!=null,"actual installed ModSettings accepts descriptor");
        string parsedName=(string)actionType.GetField("Name",BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance).GetValue(parsed);
        Check(parsedName=="检查 levels 并准备修复","actual settings button name");
        bridge.Refresh();Check(stub.refreshes==1,"refresh contract");bridge.Dispose();Check(Lease.disposed==3,"leases released");bridge.Dispose();Check(Lease.disposed==3,"dispose idempotent");

        string binary=Path.Combine(root,"binary.plan");TextureRepairStore.Write(binary,first);byte[] bytes=File.ReadAllBytes(binary);
        string bad=Path.Combine(root,"malformed.plan");
        byte[] corrupted=(byte[])bytes.Clone();corrupted[20]^=1;File.WriteAllBytes(bad,corrupted);Throws(delegate{TextureRepairStore.Read(bad);},"checksum catches payload damage");
        corrupted=(byte[])bytes.Clone();corrupted[0]^=1;File.WriteAllBytes(bad,corrupted);Throws(delegate{TextureRepairStore.Read(bad);},"format magic required");
        corrupted=new byte[bytes.Length-1];Array.Copy(bytes,corrupted,corrupted.Length);File.WriteAllBytes(bad,corrupted);Throws(delegate{TextureRepairStore.Read(bad);},"truncated file rejected");
        corrupted=(byte[])bytes.Clone();for(int i=8;i<12;i++)corrupted[i]=255;File.WriteAllBytes(bad,corrupted);Throws(delegate{TextureRepairStore.Read(bad);},"negative payload length rejected");
        corrupted=new byte[bytes.Length+1];Array.Copy(bytes,corrupted,bytes.Length);File.WriteAllBytes(bad,corrupted);Throws(delegate{TextureRepairStore.Read(bad);},"trailing data rejected");
        r.Width=-1;Throws(delegate{TextureRepairStore.Write(binary,first);},"negative dimensions rejected");r.Width=1024;
        r.Material=new string('x',8193);Throws(delegate{TextureRepairStore.Write(binary,first);},"oversized fields rejected");r.Material="mat_blue_water";
        TextureRepairStore legacy=new TextureRepairStore(Path.Combine(root,"legacy"));Directory.CreateDirectory(legacy.DirectoryPath);File.WriteAllText(Path.Combine(legacy.DirectoryPath,"pending.xml"),"old xml");
        Check(!legacy.ActivatePending()&&File.Exists(Path.Combine(legacy.DirectoryPath,"pending.xml")),"legacy XML retained and ignored");
        bool xmlLoaded=false;foreach(Assembly a in AppDomain.CurrentDomain.GetAssemblies())if(a.GetName().Name=="System.Xml")xmlLoaded=true;
        Check(!xmlLoaded,"workflow completed without loading System.Xml");
        Console.WriteLine(count+" persistence/workflow/API checks passed; in-game UI/rendering not tested.");
    }
}
