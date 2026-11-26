using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Security.Cryptography;

public sealed class TextureRepairRecord
{
    public string Scene, ObjectPath, Material, OriginalShader, Texture, Kind, TargetShader, Fingerprint, Status;
    public int Width, Height;
    public string Key() { return Part(Scene)+Part(ObjectPath)+Part(Material)+Part(OriginalShader)+Part(Texture)+Part(Kind); }
    static string Part(string s) { s=s??"";return s.Length+":"+s; }
    public bool Ready { get { return Status=="candidate"&&!string.IsNullOrEmpty(TargetShader)&&!string.IsNullOrEmpty(Fingerprint); } }
}
public sealed class TextureRepairDocument
{
    public int Schema=1;
    public string CreatedUtc=DateTime.UtcNow.ToString("o");
    public TextureRepairRecord[] Records=new TextureRepairRecord[0];
}
public sealed class TextureRepairStore
{
    const int MaximumBytes=64*1024*1024;
    static readonly byte[] Magic=Encoding.ASCII.GetBytes("OC2TRP1\0");
    static readonly Encoding Utf8=new UTF8Encoding(false,true);
    static readonly uint[] CrcTable=MakeCrcTable();
    public readonly string DirectoryPath;
    public string ActivePath { get { return Path.Combine(DirectoryPath,"active.plan"); } }
    public string PendingPath { get { return Path.Combine(DirectoryPath,"pending.plan"); } }
    public string ObservationsPath { get { return Path.Combine(DirectoryPath,"observations.plan"); } }
    public TextureRepairStore(string directory) { DirectoryPath=directory; }
    static uint[] MakeCrcTable()
    {
        uint[] table=new uint[256];
        for(uint i=0;i<256;i++) {uint c=i;for(int bit=0;bit<8;bit++)c=(c&1)!=0?0xedb88320U^(c>>1):c>>1;table[i]=c;}
        return table;
    }
    static uint Crc(byte[] bytes)
    {
        uint crc=0xffffffffU;foreach(byte b in bytes)crc=CrcTable[(crc^b)&255]^(crc>>8);return crc^0xffffffffU;
    }
    public static void Validate(TextureRepairDocument document)
    {
        if(document==null||document.Schema!=1||document.Records==null||document.Records.Length>50000)throw new IOException("Unsupported or oversized repair plan.");
        foreach(TextureRepairRecord r in document.Records)
        {
            if(r==null||string.IsNullOrEmpty(r.Scene)||string.IsNullOrEmpty(r.Material)||string.IsNullOrEmpty(r.Kind))throw new IOException("Incomplete repair record.");
            if(r.Kind!="Mesh"&&r.Kind!="Sprite"&&r.Kind!="Particle"&&r.Kind!="UI")throw new IOException("Unknown renderer kind.");
            if(r.Width<0||r.Height<0)throw new IOException("Invalid texture dimensions.");
            foreach(string s in new string[]{r.Scene,r.ObjectPath,r.Material,r.OriginalShader,r.Texture,r.TargetShader,r.Fingerprint,r.Status})
                if(s!=null&&s.Length>8192)throw new IOException("Repair field is too long.");
        }
    }
    static void WriteString(BinaryWriter writer,string value)
    {
        value=value??"";if(value.Length>8192)throw new IOException("String too long.");
        byte[] bytes=Utf8.GetBytes(value);writer.Write(bytes.Length);writer.Write(bytes);
    }
    static string ReadString(BinaryReader reader)
    {
        int length=reader.ReadInt32();if(length<0||length>32768||length>reader.BaseStream.Length-reader.BaseStream.Position)throw new IOException("Invalid string length.");
        byte[] bytes=reader.ReadBytes(length);if(bytes.Length!=length)throw new EndOfStreamException();
        string result=Utf8.GetString(bytes);if(result.Length>8192)throw new IOException("String too long.");return result;
    }
    static byte[] Serialize(TextureRepairDocument document)
    {
        Validate(document);
        using(MemoryStream stream=new MemoryStream())
        using(BinaryWriter writer=new BinaryWriter(stream))
        {
            writer.Write(document.Schema);WriteString(writer,document.CreatedUtc);writer.Write(document.Records.Length);
            foreach(TextureRepairRecord r in document.Records)
            {
                foreach(string value in new string[]{r.Scene,r.ObjectPath,r.Material,r.OriginalShader,r.Texture,r.Kind,r.TargetShader,r.Fingerprint,r.Status})WriteString(writer,value);
                writer.Write(r.Width);writer.Write(r.Height);
                if(stream.Length>MaximumBytes-16)throw new IOException("Repair file is too large.");
            }
            writer.Flush();return stream.ToArray();
        }
    }
    public static TextureRepairDocument Read(string path)
    {
        using(FileStream file=File.OpenRead(path))
        using(BinaryReader reader=new BinaryReader(file))
        {
            if(file.Length<16||file.Length>MaximumBytes)throw new IOException("Invalid repair file size.");
            byte[] magic=reader.ReadBytes(8);for(int i=0;i<8;i++)if(magic[i]!=Magic[i])throw new IOException("Unsupported repair format; legacy XML is not used.");
            int length=reader.ReadInt32();if(length<0||length>MaximumBytes-16||length!=file.Length-16)throw new IOException("Invalid repair payload length.");
            byte[] payload=reader.ReadBytes(length);if(payload.Length!=length||reader.ReadUInt32()!=Crc(payload))throw new IOException("Repair checksum mismatch.");
            using(MemoryStream stream=new MemoryStream(payload))
            using(BinaryReader fields=new BinaryReader(stream))
            {
                TextureRepairDocument document=new TextureRepairDocument();document.Schema=fields.ReadInt32();document.CreatedUtc=ReadString(fields);
                int count=fields.ReadInt32();if(count<0||count>50000)throw new IOException("Invalid repair record count.");
                document.Records=new TextureRepairRecord[count];
                for(int i=0;i<count;i++)
                {
                    TextureRepairRecord r=new TextureRepairRecord();
                    r.Scene=ReadString(fields);r.ObjectPath=ReadString(fields);r.Material=ReadString(fields);r.OriginalShader=ReadString(fields);r.Texture=ReadString(fields);r.Kind=ReadString(fields);r.TargetShader=ReadString(fields);r.Fingerprint=ReadString(fields);r.Status=ReadString(fields);r.Width=fields.ReadInt32();r.Height=fields.ReadInt32();document.Records[i]=r;
                }
                if(stream.Position!=stream.Length)throw new IOException("Unexpected trailing repair data.");
                Validate(document);return document;
            }
        }
    }
    public static void Write(string path,TextureRepairDocument document)
    {
        byte[] payload=Serialize(document);Directory.CreateDirectory(Path.GetDirectoryName(path));
        string temp=path+"."+Guid.NewGuid().ToString("N")+".tmp";
        try
        {
            using(FileStream file=new FileStream(temp,FileMode.CreateNew,FileAccess.Write,FileShare.None))
            using(BinaryWriter writer=new BinaryWriter(file))
            {writer.Write(Magic);writer.Write(payload.Length);writer.Write(payload);writer.Write(Crc(payload));writer.Flush();file.Flush();}
            Read(temp);
            if(File.Exists(path))File.Replace(temp,path,path+".bak");else File.Move(temp,path);
        }
        finally {if(File.Exists(temp))File.Delete(temp);}
    }
    public bool ActivatePending()
    {
        if(!File.Exists(PendingPath))return false;TextureRepairDocument pending=Read(PendingPath);
        foreach(TextureRepairRecord record in pending.Records)if(!record.Ready)throw new IOException("Pending plan contains an unresolved record.");
        Write(ActivePath,pending);File.Delete(PendingPath);return true;
    }
    public static string Fingerprint(string sceneFile)
    {
        if(string.IsNullOrEmpty(sceneFile)||!File.Exists(sceneFile))return "";
        using(SHA256 sha=SHA256.Create())
        using(FileStream stream=File.OpenRead(sceneFile))
        {
            string sceneHash=BitConverter.ToString(sha.ComputeHash(stream)).Replace("-","");List<string> dependencies=new List<string>();
            foreach(string f in Directory.GetFiles(Path.GetDirectoryName(sceneFile),"info_*")){FileInfo info=new FileInfo(f);dependencies.Add(info.Name+":"+info.Length+":"+info.LastWriteTimeUtc.Ticks);}
            dependencies.Sort(StringComparer.Ordinal);return sceneHash+"|"+string.Join("|",dependencies.ToArray());
        }
    }
    public static bool Matches(TextureRepairRecord record,string sceneFingerprint)
    {return record!=null&&record.Ready&&!string.IsNullOrEmpty(sceneFingerprint)&&record.Fingerprint==sceneFingerprint;}
}
