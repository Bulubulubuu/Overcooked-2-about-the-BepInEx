using System;
using System.IO;
using System.Collections.Generic;
using Mono.Cecil;
sealed class GameResolver : IAssemblyResolver
{
    string[] dirs; Dictionary<string,AssemblyDefinition> cache=new Dictionary<string,AssemblyDefinition>();
    public GameResolver(string[] d){dirs=d;}
    public AssemblyDefinition Resolve(AssemblyNameReference n){return Resolve(n,new ReaderParameters());}
    public AssemblyDefinition Resolve(AssemblyNameReference n,ReaderParameters p)
    {
        AssemblyDefinition a;if(cache.TryGetValue(n.Name,out a))return a;
        foreach(string d in dirs){string file=Path.Combine(d,n.Name+".dll");if(File.Exists(file)){p.AssemblyResolver=this;a=AssemblyDefinition.ReadAssembly(file,p);cache[n.Name]=a;return a;}}
        throw new FileNotFoundException(n.Name);
    }
    public void Dispose(){foreach(AssemblyDefinition a in cache.Values)a.Dispose();cache.Clear();}
}
class Audit
{
    static void Main(string[] args)
    {
        int types=0,methods=0,fields=0,errors=0;
        using(GameResolver resolver=new GameResolver(new string[]{args[1],args[2],Path.GetDirectoryName(Path.GetFullPath(args[0]))}))
        using(AssemblyDefinition a=AssemblyDefinition.ReadAssembly(args[0],new ReaderParameters{AssemblyResolver=resolver}))
        {
            foreach(TypeReference t in a.MainModule.GetTypeReferences())
            {types++;try{if(t.Resolve()==null)throw new Exception("unresolved");}catch(Exception e){errors++;Console.WriteLine("TYPE "+t+" "+e.Message);}}
            foreach(MemberReference m in a.MainModule.GetMemberReferences())
            {
                try
                {
                    MethodReference method=m as MethodReference;
                    FieldReference field=m as FieldReference;
                    if(method!=null){methods++;if(method.Resolve()==null)throw new Exception("unresolved method");}
                    else if(field!=null){fields++;if(field.Resolve()==null)throw new Exception("unresolved field");}
                }
                catch(Exception e){errors++;Console.WriteLine("MEMBER "+m+" "+e.Message);}
            }
            foreach(AssemblyNameReference n in a.MainModule.AssemblyReferences)
                if(n.Name=="System.Xml") {errors++;Console.WriteLine("Forbidden System.Xml reference");}
        }
        Console.WriteLine("Game-only reference audit: "+types+" types, "+methods+" methods, "+fields+" fields, "+errors+" failures.");
        Environment.ExitCode=errors==0?0:1;
    }
}
