using System.Reflection;
using System;using System.Linq;using System.Collections.Generic;using Mono.Cecil;using Mono.Cecil.Cil;
class CompatPatcher {
 static MethodDefinition AddReader(ModuleDefinition mod, TypeDefinition owner, TypeReference gameUtils) {
  var m = new MethodDefinition("ReadRoomSearchNoneAvailableCompat", Mono.Cecil.MethodAttributes.Private | Mono.Cecil.MethodAttributes.Static, mod.TypeSystem.Boolean);
  owner.Methods.Add(m);
  m.Body.InitLocals = true;
  var field = new VariableDefinition(mod.ImportReference(typeof(FieldInfo))); m.Body.Variables.Add(field);
  var il=m.Body.GetILProcessor(); var missing=Instruction.Create(OpCodes.Ldc_I4_0);
  il.Emit(OpCodes.Ldtoken,gameUtils);
  il.Emit(OpCodes.Call,mod.ImportReference(typeof(Type).GetMethod("GetTypeFromHandle")));
  il.Emit(OpCodes.Ldstr,"s_RoomSearch_NoneAvailable");
  il.Emit(OpCodes.Ldc_I4,(int)(BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic));
  il.Emit(OpCodes.Callvirt,mod.ImportReference(typeof(Type).GetMethod("GetField",new[]{typeof(string),typeof(BindingFlags)})));
  il.Emit(OpCodes.Stloc,field); il.Emit(OpCodes.Ldloc,field); il.Emit(OpCodes.Brfalse,missing);
  il.Emit(OpCodes.Ldloc,field); il.Emit(OpCodes.Callvirt,mod.ImportReference(typeof(FieldInfo).GetProperty("FieldType").GetGetMethod()));
  il.Emit(OpCodes.Ldtoken,mod.TypeSystem.Boolean); il.Emit(OpCodes.Call,mod.ImportReference(typeof(Type).GetMethod("GetTypeFromHandle")));
  il.Emit(OpCodes.Ceq); il.Emit(OpCodes.Brfalse,missing);
  il.Emit(OpCodes.Ldloc,field); il.Emit(OpCodes.Ldnull);
  il.Emit(OpCodes.Callvirt,mod.ImportReference(typeof(FieldInfo).GetMethod("GetValue")));
  il.Emit(OpCodes.Unbox_Any,mod.TypeSystem.Boolean); il.Emit(OpCodes.Ret);
  il.Append(missing); il.Emit(OpCodes.Ret); return m;
 }
 static System.Collections.Generic.IEnumerable<TypeDefinition> Types(TypeDefinition t){yield return t;foreach(var n in t.NestedTypes)foreach(var x in Types(n))yield return x;}
 static void Main(string[] args){
  if(args.Length==2 && args[0]=="--inspect"){
   var inspect=AssemblyDefinition.ReadAssembly(args[1]);var resource=inspect.MainModule.Resources.OfType<EmbeddedResource>().FirstOrDefault(r=>r.Name.EndsWith("current-mod-info.json"));string info="null";
   if(resource!=null)using(var sr=new System.IO.StreamReader(resource.GetResourceStream()))info=sr.ReadToEnd().TrimStart('\ufeff');
   Console.WriteLine("{\"name\":\""+inspect.Name.Name+"\",\"version\":\""+inspect.Name.Version+"\",\"modInfo\":"+info+"}");return;
  }

  var resolver=new DefaultAssemblyResolver();resolver.AddSearchDirectory(args[3]);resolver.AddSearchDirectory(args[4]);
  var a=AssemblyDefinition.ReadAssembly(args[0],new ReaderParameters{AssemblyResolver=resolver});var mod=a.MainModule;
  var src=AssemblyDefinition.ReadAssembly(args[1]).MainModule.GetType("MacUpdateBridge");
  if(mod.Types.Any(t=>t.Name==src.Name))throw new Exception("Already patched");
  var dst=new TypeDefinition("HostUtilities",src.Name,src.Attributes,mod.TypeSystem.Object);mod.Types.Add(dst);
  var methodMap=new Dictionary<MethodDefinition,MethodDefinition>();var fieldMap=new Dictionary<FieldDefinition,FieldDefinition>();
  foreach(var f in src.Fields){var n=new FieldDefinition(f.Name,f.Attributes,mod.ImportReference(f.FieldType));if(f.HasConstant)n.Constant=f.Constant;dst.Fields.Add(n);fieldMap.Add(f,n);}
  foreach(var m in src.Methods){var n=new MethodDefinition(m.Name,m.Attributes,mod.ImportReference(m.ReturnType));foreach(var p in m.Parameters)n.Parameters.Add(new ParameterDefinition(p.Name,p.Attributes,mod.ImportReference(p.ParameterType)));dst.Methods.Add(n);methodMap.Add(m,n);}
  foreach(var m in src.Methods.Where(m=>m.HasBody)){
   var n=methodMap[m];n.Body.InitLocals=m.Body.InitLocals;n.Body.MaxStackSize=m.Body.MaxStackSize;
   foreach(var v in m.Body.Variables)n.Body.Variables.Add(new VariableDefinition(mod.ImportReference(v.VariableType)));
   var ins=new Dictionary<Instruction,Instruction>();foreach(var i in m.Body.Instructions){var j=Instruction.Create(OpCodes.Nop);j.OpCode=i.OpCode;n.Body.Instructions.Add(j);ins.Add(i,j);}
   foreach(var i in m.Body.Instructions){object v=i.Operand;
    if(v is Instruction)v=ins[(Instruction)v];else if(v is Instruction[])v=((Instruction[])v).Select(x=>ins[x]).ToArray();
    else if(v is VariableDefinition)v=n.Body.Variables[((VariableDefinition)v).Index];else if(v is ParameterDefinition)v=n.Parameters[((ParameterDefinition)v).Index];
    else if(v is MethodReference){var r=(MethodReference)v;v=r.DeclaringType.FullName==src.FullName?(object)methodMap[r.Resolve()]:mod.ImportReference(r);}
    else if(v is FieldReference){var r=(FieldReference)v;v=r.DeclaringType.FullName==src.FullName?(object)fieldMap[r.Resolve()]:mod.ImportReference(r);}
    else if(v is TypeReference)v=mod.ImportReference((TypeReference)v);
    ins[i].Operand=v;
   }
   foreach(var e in m.Body.ExceptionHandlers)n.Body.ExceptionHandlers.Add(new ExceptionHandler(e.HandlerType){TryStart=ins[e.TryStart],TryEnd=e.TryEnd==null?null:ins[e.TryEnd],HandlerStart=ins[e.HandlerStart],HandlerEnd=e.HandlerEnd==null?null:ins[e.HandlerEnd],CatchType=e.CatchType==null?null:mod.ImportReference(e.CatchType),FilterStart=e.FilterStart==null?null:ins[e.FilterStart]});
  }
  var plan=mod.GetType("HostUtilities.UpdateInstallPlan");
  if(plan==null)throw new Exception("Unsupported updater: UpdateInstallPlan missing");
  foreach(string required in new[]{"Armed","Directory","InstallRoot","PluginsRoot","CurrentDll","Version","ModTarget","DllHash","ManifestHash","ConfigurationManagerHash","HasMod","HasResources","HasLegacyCleanup","AllowStableReturn","CurrentBetaVersion","ApprovedExtraFiles","ResourceBackups"})if(!plan.Fields.Any(f=>f.Name==required))throw new Exception("Unsupported updater field: "+required);
  var platform=mod.ImportReference(typeof(Environment).GetProperty("OSVersion").GetGetMethod());var getPlatform=mod.ImportReference(typeof(OperatingSystem).GetProperty("Platform").GetGetMethod());
  var allowed=new HashSet<string>();
  foreach(string methodName in new[]{"Arm","ArmResourceCleanup"}){
   var arm=plan.Methods.Single(m=>m.Name==methodName&&m.Parameters.Count==0);if(arm.ReturnType.FullName!="System.Void")throw new Exception("Unsupported updater method");allowed.Add(arm.FullName);
   var il=arm.Body.GetILProcessor();var first=arm.Body.Instructions[0];
   foreach(var i in new[]{Instruction.Create(OpCodes.Call,platform),Instruction.Create(OpCodes.Callvirt,getPlatform),Instruction.Create(OpCodes.Ldc_I4_2),Instruction.Create(OpCodes.Beq,first),Instruction.Create(OpCodes.Ldarg_0),Instruction.Create(methodName=="ArmResourceCleanup"?OpCodes.Ldc_I4_1:OpCodes.Ldc_I4_0),Instruction.Create(OpCodes.Call,dst.Methods.Single(m=>m.Name=="Launch")),Instruction.Create(OpCodes.Ret)})il.InsertBefore(first,i);
  }
  MethodDefinition reader=null;int fieldCount=0,diagnosticCount=0;
  foreach(var type in mod.Types.SelectMany(Types).ToArray())foreach(var method in type.Methods.Where(m=>m.HasBody).ToArray())foreach(var i in method.Body.Instructions){
   var f=i.Operand as FieldReference;var c=i.Operand as MethodReference;
   if(f!=null&&f.DeclaringType.Scope.Name=="Assembly-CSharp"&&f.Resolve()==null){
    if(f.FullName!="System.Boolean GameUtils::s_RoomSearch_NoneAvailable"||i.OpCode!=OpCodes.Ldsfld)throw new Exception("Unsupported missing field: "+f.FullName);
    if(reader==null)reader=AddReader(mod,type,f.DeclaringType);i.OpCode=OpCodes.Call;i.Operand=reader;allowed.Add(method.FullName);fieldCount++;
   }
   if(c!=null&&c.DeclaringType.Scope.Name=="Assembly-CSharp"&&c.Resolve()==null){
    if(c.FullName!="System.Void GameUtils::SendDiagnosticEvent(System.String)"||i.OpCode!=OpCodes.Call||c.HasThis)throw new Exception("Unsupported missing game method: "+c.FullName);
    i.OpCode=OpCodes.Pop;i.Operand=null;allowed.Add(method.FullName);diagnosticCount++;
   }
  }
  a.Write(args[2]);var check=AssemblyDefinition.ReadAssembly(args[2]);int changed=0;
  var original=AssemblyDefinition.ReadAssembly(args[0]);
  foreach(var t in original.MainModule.Types)foreach(var m in t.Methods.Where(m=>m.HasBody)){var n=check.MainModule.Types.Single(x=>x.FullName==t.FullName).Methods.Single(x=>x.FullName==m.FullName);if(string.Join("\n",m.Body.Instructions.Select(i=>i.ToString()).ToArray())!=string.Join("\n",n.Body.Instructions.Select(i=>i.ToString()).ToArray())){changed++;if(!allowed.Contains(m.FullName))throw new Exception("Unexpected method change "+m.FullName);}}
  if(changed!=allowed.Count)throw new Exception("Unexpected changed-method count");
  if(check.MainModule.AssemblyReferences.Any(r=>r.Name=="MacUpdateBridge"))throw new Exception("Unexpected bridge dependency");
  Console.WriteLine("PASS HostUtilities "+a.Name.Version+": field fixes="+fieldCount+", diagnostic fixes="+diagnosticCount+", Windows installer methods bridged=2, changed existing methods="+changed);
 }
}
