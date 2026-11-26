using System;
using System.IO;
using System.Text;
using System.Collections.Generic;

// Reads only the UnityFS directory, never loads scenes or instantiates level objects.
public static class TextureSceneCatalog
{
    const int MaximumInfo = 16 * 1024 * 1024;
    static uint U32(BinaryReader r) { byte[] b = r.ReadBytes(4); if (b.Length != 4) throw new EndOfStreamException(); return ((uint)b[0]<<24)|((uint)b[1]<<16)|((uint)b[2]<<8)|b[3]; }
    static ulong U64(BinaryReader r) { return ((ulong)U32(r)<<32)|U32(r); }
    static string Z(BinaryReader r)
    {
        List<byte> b = new List<byte>();
        for (int i = 0; i < 4096; i++) { byte v = r.ReadByte(); if (v == 0) return Encoding.UTF8.GetString(b.ToArray()); b.Add(v); }
        throw new IOException("UnityFS name exceeds the supported limit.");
    }
    static byte[] Lz4(byte[] input, int size)
    {
        byte[] output = new byte[size]; int src = 0, dst = 0;
        while (src < input.Length)
        {
            int token = input[src++], length = token >> 4;
            if (length == 15) { int v; do { if (src >= input.Length) throw new IOException("Truncated LZ4 length."); v = input[src++]; length += v; } while (v == 255); }
            if (length > input.Length - src || length > size - dst) throw new IOException("Invalid LZ4 literal length.");
            Buffer.BlockCopy(input, src, output, dst, length); src += length; dst += length;
            if (src == input.Length) break;
            if (src + 2 > input.Length) throw new IOException("Truncated LZ4 offset.");
            int offset = input[src] | input[src + 1] << 8; src += 2; length = (token & 15) + 4;
            if ((token & 15) == 15) { int v; do { if (src >= input.Length) throw new IOException("Truncated LZ4 match."); v = input[src++]; length += v; } while (v == 255); }
            if (offset <= 0 || offset > dst || length > size - dst) throw new IOException("Invalid LZ4 match.");
            for (int i = 0; i < length; i++) { output[dst] = output[dst - offset]; dst++; }
        }
        if (dst != size) throw new IOException("LZ4 directory size mismatch."); return output;
    }
    public static bool IsExcluded(string root, string file)
    {
        string relative = file.Substring(root.Length).TrimStart(Path.DirectorySeparatorChar);
        foreach (string part in relative.Split(Path.DirectorySeparatorChar)) if (part.StartsWith(".", StringComparison.Ordinal)) return true;
        return false;
    }
    public static string[] ReadSceneNames(string file)
    {
        using (FileStream stream = File.OpenRead(file))
        using (BinaryReader r = new BinaryReader(stream))
        {
            if (stream.Length < 48 || Encoding.ASCII.GetString(r.ReadBytes(8)) != "UnityFS\0") return new string[0];
            uint version = U32(r); if (version < 6 || version > 8) throw new IOException("Unsupported UnityFS format version.");
            Z(r); Z(r); ulong total = U64(r); uint packed = U32(r), size = U32(r), flags = U32(r);
            if (size > MaximumInfo || packed > MaximumInfo || total > (ulong)stream.Length) throw new IOException("Invalid UnityFS directory size.");
            if (version >= 7) stream.Position = (stream.Position + 15) & ~15L;
            if ((flags & 128) != 0) stream.Position = (long)total - packed;
            byte[] data = r.ReadBytes((int)packed); if (data.Length != packed) throw new EndOfStreamException();
            uint compression = flags & 63;
            if (compression == 2 || compression == 3) data = Lz4(data, (int)size);
            else if (compression != 0) throw new IOException("Unsupported UnityFS directory compression.");
            if (data.Length != size) throw new IOException("UnityFS directory length mismatch.");
            using (MemoryStream meta = new MemoryStream(data))
            using (BinaryReader directory = new BinaryReader(meta))
            {
                if (meta.Length < 20) throw new IOException("Truncated UnityFS directory.");
                meta.Position = 16; uint blocks = U32(directory);
                if (blocks > 65536 || blocks * 10L > meta.Length - meta.Position) throw new IOException("Invalid UnityFS block table.");
                meta.Position += blocks * 10L; uint nodes = U32(directory); if (nodes > 65536) throw new IOException("Invalid UnityFS node count.");
                List<string> scenes = new List<string>();
                for (uint i = 0; i < nodes; i++)
                {
                    U64(directory); U64(directory); U32(directory); string name = Z(directory);
                    const string prefix = "BuildPlayer-";
                    if (!name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) || name.EndsWith(".sharedAssets", StringComparison.OrdinalIgnoreCase)) continue;
                    string scene = name.Substring(prefix.Length);
                    if (scene.Length > 0 && scene.IndexOf('/') < 0 && scene.IndexOf('\\') < 0 && !scenes.Contains(scene)) scenes.Add(scene);
                }
                return scenes.ToArray();
            }
        }
    }
    public static Dictionary<string,string> Discover(string root, out HashSet<string> ambiguous, out List<string> warnings)
    {
        Dictionary<string,string> result = new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);
        ambiguous = new HashSet<string>(StringComparer.OrdinalIgnoreCase); warnings = new List<string>();
        if (!Directory.Exists(root)) return result;
        foreach (string file in Directory.GetFiles(root, "*", SearchOption.AllDirectories))
        {
            if (IsExcluded(root, file)) continue;
            string name = Path.GetFileName(file);
            if (name.IndexOf('.') >= 0 || name.StartsWith("info_", StringComparison.OrdinalIgnoreCase)) continue;
            try
            {
                foreach (string scene in ReadSceneNames(file))
                {
                    string previous;
                    if (result.TryGetValue(scene, out previous) && previous != file) ambiguous.Add(scene); else result[scene] = file;
                }
            }
            catch (Exception e) { warnings.Add(name + ": " + e.Message); }
        }
        return result;
    }
}
