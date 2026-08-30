using System;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

class Program
{
    static void Main(string[] args)
    {
        if (args.Length < 1)
        {
            Console.WriteLine("Usage:");
            Console.WriteLine("mono patch_platform.exe /path/to/BepInEx.Preloader.dll");
            return;
        }

        string dll = args[0];
        string backup = dll + ".original";
        string patched = dll + ".patched";

        if (!System.IO.File.Exists(dll))
        {
            Console.WriteLine("ERROR: DLL not found:");
            Console.WriteLine(dll);
            return;
        }

        if (!System.IO.File.Exists(backup))
        {
            System.IO.File.Copy(dll, backup);
            Console.WriteLine("Backup created:");
            Console.WriteLine(backup);
        }

        var asm = AssemblyDefinition.ReadAssembly(dll);

        var type = asm.MainModule.Types
            .First(t => t.FullName == "BepInEx.Preloader.PlatformUtils");

        var method = type.Methods
            .First(m => m.Name == "SetPlatform");

        bool patchedOk = false;

        foreach (var ins in method.Body.Instructions)
        {
            if (ins.OpCode == OpCodes.Ldc_I4 &&
                ins.Operand is int &&
                (int)ins.Operand == 137)
            {
                var next = ins.Next;

                if (next != null &&
                    next.OpCode == OpCodes.Stloc_0)
                {
                    Console.WriteLine("Patching platform constant 137 -> 73");
                    ins.Operand = 73;
                    patchedOk = true;
                    break;
                }
            }
        }

        if (!patchedOk)
        {
            Console.WriteLine("ERROR: target instruction not found.");
            return;
        }

        asm.Write(patched);

        System.IO.File.Copy(patched, dll, true);

        Console.WriteLine("Patch complete.");
        Console.WriteLine("Patched DLL:");
        Console.WriteLine(dll);
    }
}
