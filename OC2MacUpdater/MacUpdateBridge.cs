using System;using System.IO;using System.Reflection;using System.Collections;using System.Text;using System.Diagnostics;
public static class MacUpdateBridge {
 static object Get(object o,string name){var f=o.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public);if(object.ReferenceEquals(f,null))throw new MissingFieldException("Unsupported updater field: "+name);return f.GetValue(o);}
 static string Q(string s){if(s==null)return "null";var b=new StringBuilder("\"");foreach(char c in s){switch(c){case '\\':b.Append("\\\\");break;case '"':b.Append("\\\"");break;case '\n':b.Append("\\n");break;case '\r':b.Append("\\r");break;case '\t':b.Append("\\t");break;default:if(c<32)b.Append("\\u"+((int)c).ToString("x4"));else b.Append(c);break;}}return b.Append('"').ToString();}
 public static void Launch(object plan,bool cleanupOnly){
  if((bool)Get(plan,"Armed"))return;
  string plugins=(string)Get(plan,"PluginsRoot"),dir=(string)Get(plan,"Directory");string tools=Path.Combine(Path.Combine(Path.GetDirectoryName(plugins),"tools"),"OC2MacUpdater");
  string config=Path.Combine(tools,"python.path"),script=Path.Combine(tools,"update.py");
  if(!File.Exists(config)||!File.Exists(script))throw new IOException("缺少 Mac 更新工具。请安装 BepInEx/tools/OC2MacUpdater 后重试。");
  string python=File.ReadAllText(config).Trim();if(!File.Exists(python))throw new IOException("Mac 更新工具的 Python 路径失效，请重新安装工具。");
  var b=new StringBuilder("{\"format\":1,\"cleanupOnly\":"+(cleanupOnly?"true":"false"));
  foreach(string n in new[]{"Directory","InstallRoot","PluginsRoot","CurrentDll","Version","ModTarget","DllHash","ManifestHash","ConfigurationManagerHash","CurrentBetaVersion"})b.Append(',').Append(Q(n)).Append(':').Append(Q((string)Get(plan,n)));
  foreach(string n in new[]{"HasMod","HasResources","HasLegacyCleanup","AllowStableReturn"})b.Append(',').Append(Q(n)).Append(':').Append((bool)Get(plan,n)?"true":"false");
  b.Append(",\"ApprovedExtraFiles\":[");bool first=true;foreach(object item in (IEnumerable)Get(plan,"ApprovedExtraFiles")){if(!first)b.Append(',');first=false;b.Append(Q((string)item));}b.Append(']');
  b.Append(",\"ResourceBackups\":[");first=true;var backups=Get(plan,"ResourceBackups") as IEnumerable;if(backups!=null)foreach(object item in backups){if(!first)b.Append(',');first=false;b.Append('[').Append(Q((string)item.GetType().GetProperty("Key").GetValue(item,null))).Append(',').Append(Q((string)item.GetType().GetProperty("Value").GetValue(item,null))).Append(']');}b.Append("]}");
  string task=Path.Combine(dir,"mac-install-plan.json");File.WriteAllText(task,b.ToString(),new UTF8Encoding(false));
  string receipt=Path.Combine(dir,"mac-worker-ready");if(File.Exists(receipt))File.Delete(receipt);
  var psi=new ProcessStartInfo(python,"\""+script.Replace("\"","\\\"")+"\" --apply-plan \""+task.Replace("\"","\\\"")+"\" --wait-pid "+Process.GetCurrentProcess().Id){UseShellExecute=false,CreateNoWindow=true,WorkingDirectory=tools};
  // The external Python/Mono tools must not inherit Unity's injected native libraries.
  var remove=new System.Collections.Generic.List<string>();
  foreach(string key in psi.EnvironmentVariables.Keys)if(key.StartsWith("DYLD_",StringComparison.Ordinal)||key.StartsWith("DOORSTOP_",StringComparison.Ordinal)||key=="STEAM_DYLD_INSERT_LIBRARIES"||key=="LD_PRELOAD"||key=="LD_LIBRARY_PATH"||key=="MONO_PATH"||key=="MONO_ENV_OPTIONS")remove.Add(key);
  foreach(string key in remove)psi.EnvironmentVariables.Remove(key);
  using(Process worker=Process.Start(psi)){if(worker==null)throw new IOException("无法启动 Mac 更新工具");for(int i=0;i<50;i++){if(File.Exists(receipt))break;if(worker.HasExited)throw new IOException("Mac 更新工具启动失败，请查看更新计划目录 mac-apply.log");System.Threading.Thread.Sleep(100);}if(!File.Exists(receipt))throw new IOException("Mac 更新工具未就绪，请查看更新日志");}
  plan.GetType().GetField("Armed",BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic).SetValue(plan,true);
  Console.WriteLine("[OC2 Mac updater] 已转交外部 Mac 更新工具，退出游戏后执行；备份会保留。");
 }
}
