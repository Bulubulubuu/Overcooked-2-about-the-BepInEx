using System;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using Mono.Cecil;
class ManualTests
{
    static int count;
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);count++;}
    static IEnumerable<TypeDefinition> Types(TypeDefinition t){yield return t;foreach(TypeDefinition n in t.NestedTypes)foreach(TypeDefinition x in Types(n))yield return x;}
    static void Main(string[] args)
    {
        AppDomain.CurrentDomain.AssemblyResolve+=delegate(object sender,ResolveEventArgs e){string n=new AssemblyName(e.Name).Name+".dll";foreach(string d in new string[]{args[1],args[2]}){string p=Path.Combine(d,n);if(File.Exists(p))return Assembly.LoadFrom(p);}return null;};
        Assembly runtime=Assembly.LoadFrom(args[0]);Type path=runtime.GetType("SavedRulePath",true);MethodInfo decode=path.GetMethod("Decode");
        foreach(string[] v in new string[][]{new string[]{"Floor#0","Floor","0"},new string[]{"Floor%20(1)#2","Floor (1)","2"},new string[]{"%E5%9C%B0%E6%9D%BF#1","地板","1"},new string[]{"A%2FB%23C#0","A/B#C","0"}})
        {object[] input={v[0],null,0};Check((bool)decode.Invoke(null,input)&&((string)input[1])==v[1]&&(int)input[2]==int.Parse(v[2]),"saved path resolves "+v[0]);}
        foreach(string v in new string[]{null,"","Floor","Floor#-1","Floor#999999999999999999999","#0"}){object[] input={v,null,0};Check(!(bool)decode.Invoke(null,input),"invalid path rejected");}
        using(AssemblyDefinition a=AssemblyDefinition.ReadAssembly(args[0]))
        {
            Dictionary<string,List<string>> callers=new Dictionary<string,List<string>>();TypeDefinition plugin=null;MethodDefinition loaded=null,awake=null;bool observer=false,applyWalk=false;
            foreach(TypeDefinition root in a.MainModule.Types)foreach(TypeDefinition t in Types(root))
            {
                if(t.Name=="CustomLevelFloorShaderFix")plugin=t;
                foreach(MethodDefinition m in t.Methods)
                {
                    if(t.Name=="CustomLevelFloorShaderFix"&&m.Name=="Loaded")loaded=m;
                    if(t.Name=="CustomLevelFloorShaderFix"&&m.Name=="Awake")awake=m;
                    if(t.Name=="CustomLevelFloorShaderFix"&&m.Name=="Observe")observer=true;
                    if(!m.HasBody)continue;
                    foreach(var instruction in m.Body.Instructions)
                    {
                        MethodReference called=instruction.Operand as MethodReference;if(called==null)continue;
                        if(called.DeclaringType.Name=="CustomLevelFloorShaderFix")
                        {
                            List<string> list;if(!callers.TryGetValue(called.Name,out list)){list=new List<string>();callers[called.Name]=list;}list.Add(t.FullName+"::"+m.Name);
                        }
                        if(t.Name.Contains("ApplySavedOnce")&&called.Name.Contains("GetComponentsInChildren"))applyWalk=true;
                    }
                }
            }
            Check(plugin!=null&&loaded!=null&&awake!=null,"plugin entry points found");
            Check(!observer,"automatic observer loop removed");Check(!applyWalk,"saved-rule application has no full scene traversal");
            foreach(string target in new string[]{"ScanScene","Discover","SaveObservations"})
            {
                Check(callers.ContainsKey(target)&&callers[target].Count==1,target+" has one call site");
                Check(callers[target][0].Contains("PrepareWorker"),target+" is only called by the manual preparation worker");
            }
            bool loadedScan=false,awakeCatalog=false;
            foreach(var instruction in loaded.Body.Instructions){MethodReference m=instruction.Operand as MethodReference;if(m!=null&&(m.Name=="ScanScene"||m.Name=="Discover"))loadedScan=true;}
            foreach(var instruction in awake.Body.Instructions){MethodReference m=instruction.Operand as MethodReference;if(m!=null&&m.Name=="Discover")awakeCatalog=true;}
            Check(!loadedScan,"scene loading does not start full inspection");Check(!awakeCatalog,"startup does not read the full catalog");
        }
        Console.WriteLine(count+" manual-mode/path/performance call-site checks passed; no in-game frame-time benchmark.");
    }
}
