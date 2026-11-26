using System;
using System.Collections.Generic;
using System.Reflection;
public sealed class TextureSettingsBridge : IDisposable
{
    readonly object service;
    readonly List<IDisposable> leases = new List<IDisposable>();
    public TextureSettingsBridge(object instance) { service = instance; }
    object Call(string name, params object[] args)
    {
        MethodInfo m = service.GetType().GetMethod(name, BindingFlags.Public | BindingFlags.Instance);
        if (m == null) throw new MissingMethodException(name);
        return m.Invoke(service, args);
    }
    public bool Ready() { return (int)Call("GetApiVersionV1") == 1 && (string)Call("GetStatusV1") == "ready"; }
    public void Register(string owner, string name, string description, BepInEx.Configuration.ConfigFile config)
    { leases.Add((IDisposable)Call("RegisterConfigV1", owner, name, description, config)); }
    public static Dictionary<string, object> Descriptor(string name, string description, Action execute, Func<bool> enabled)
    {
        return new Dictionary<string, object> {
            { "Version", 1 }, { "DisplayName", name }, { "Description", description },
            { "Category", "贴图检查与更新" }, { "CategoryOrder", (int?) -100 },
            { "Order", -100 }, { "Execute", execute }, { "CanExecute", enabled }, { "ClosePage", false }
        };
    }
    public string Category = "贴图检查与更新";
    public void Configure(BepInEx.Configuration.ConfigEntryBase entry, IDictionary<string,object> metadata)
    { leases.Add((IDisposable)Call("ConfigureEntryV1", entry, metadata)); }
    public void Button(string owner, string id, string name, string description, Action execute, Func<bool> enabled)
    { Dictionary<string,object> d = Descriptor(name, description, execute, enabled); d["Category"] = Category; leases.Add((IDisposable)Call("RegisterActionV1", owner, id, d)); }
    public void ReadOnly(BepInEx.Configuration.ConfigEntryBase entry)
    {
        leases.Add((IDisposable)Call("ConfigureEntryV1", entry, new Dictionary<string, object> {
            { "ReadOnly", true }, { "HideDefaultButton", true }, { "Order", -110 }, { "CategoryOrder", (int?) -100 }
        }));
    }
    public void Refresh() { Call("RefreshV1"); }
    public void Dispose()
    {
        for (int i = leases.Count - 1; i >= 0; i--) try { if (leases[i] != null) leases[i].Dispose(); } catch (Exception) { }
        leases.Clear();
    }
}
