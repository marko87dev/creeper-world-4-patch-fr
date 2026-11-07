using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace Cw4French {
public static class Delta {
    const int Block=4096, Mod=65521;
    static readonly byte[] Magic=Encoding.ASCII.GetBytes("CWFRD001");
    static uint Check(byte[] b,int p,int n) {
        int a=1,c=0; for(int i=0;i<n;i++){a=(a+b[p+i])%Mod;c=(c+a)%Mod;}
        return ((uint)c<<16)|(uint)a;
    }
    static bool Equal(byte[] a,int p,byte[] b,int q,int n) {
        for(int i=0;i<n;i++)if(a[p+i]!=b[q+i])return false;return true;
    }
    static void Literal(BinaryWriter w,byte[] b,int p,int n) {
        if(n==0)return;w.Write((byte)2);w.Write(n);w.Write(b,p,n);
    }
    public static void Build(string source,string target,string output) {
        byte[] a=File.ReadAllBytes(source),b=File.ReadAllBytes(target);
        Dictionary<uint,List<int>> blocks=new Dictionary<uint,List<int>>();
        for(int i=0;i+Block<=a.Length;i+=Block) {
            uint key=Check(a,i,Block);List<int> offsets;
            if(!blocks.TryGetValue(key,out offsets)){offsets=new List<int>();blocks.Add(key,offsets);}
            if(offsets.Count<16)offsets.Add(i);
        }
        long copied=0,literals=0;int cmds=0;
        using(FileStream f=new FileStream(output,FileMode.Create,FileAccess.Write))
        using(GZipStream zip=new GZipStream(f,CompressionMode.Compress))
        using(BinaryWriter w=new BinaryWriter(zip)) {
            w.Write(Magic);w.Write((long)b.Length);
            int p=0,pending=0;uint hash=b.Length>=Block?Check(b,0,Block):0;
            while(p+Block<=b.Length) {
                List<int> options;int found=-1;
                if(blocks.TryGetValue(hash,out options))foreach(int q in options)if(Equal(a,q,b,p,Block)){found=q;break;}
                if(found>=0) {
                    int q=found,start=p;
                    while(start>pending&&q>0&&b[start-1]==a[q-1]){start--;q--;}
                    Literal(w,b,pending,start-pending);literals+=start-pending;
                    int n=p-start+Block;
                    while(q+n<a.Length&&start+n<b.Length&&a[q+n]==b[start+n])n++;
                    w.Write((byte)1);w.Write((long)q);w.Write(n);copied+=n;cmds++;
                    p=start+n;pending=p;
                    if(p+Block<=b.Length)hash=Check(b,p,Block);
                    continue;
                }
                if(p+Block<b.Length) {
                    int aa=(int)(hash&65535),bb=(int)(hash>>16);
                    aa=(aa-b[p]+b[p+Block])%Mod;if(aa<0)aa+=Mod;
                    bb=(bb-Block*b[p]+aa-1)%Mod;if(bb<0)bb+=Mod;
                    hash=((uint)bb<<16)|(uint)aa;
                }
                p++;
            }
            Literal(w,b,pending,b.Length-pending);literals+=b.Length-pending;
            w.Write((byte)0);
        }
        Console.WriteLine("Delta: {0} bytes copied, {1} new bytes, {2} copy commands; payload {3} bytes",copied,literals,cmds,new FileInfo(output).Length);
    }
    static void Exact(Stream s,byte[] buffer,int n) {
        int p=0;while(p<n){int r=s.Read(buffer,p,n-p);if(r==0)throw new InvalidDataException("Patch incomplet.");p+=r;}
    }
    public static void Apply(string source,Stream patch,string output,Action<int> progress) {
        using(FileStream input=new FileStream(source,FileMode.Open,FileAccess.Read,FileShare.Read))
        using(GZipStream zip=new GZipStream(patch,CompressionMode.Decompress))
        using(BinaryReader r=new BinaryReader(zip))
        using(FileStream dest=new FileStream(output,FileMode.CreateNew,FileAccess.Write,FileShare.None)) {
            byte[] magic=r.ReadBytes(Magic.Length);if(magic.Length!=Magic.Length||!Equal(magic,0,Magic,0,Magic.Length))throw new InvalidDataException("Format de patch invalide.");
            long target=r.ReadInt64(),written=0;
            if(target<0||target>2147483647)throw new InvalidDataException("Taille de patch invalide.");
            byte[] buffer=new byte[1024*1024];int last=-1;
            for(;;) {
                byte op=r.ReadByte();if(op==0)break;
                long offset=0;int n;
                if(op==1){offset=r.ReadInt64();n=r.ReadInt32();}
                else if(op==2)n=r.ReadInt32();
                else throw new InvalidDataException("Instruction de patch invalide.");
                if(n<=0||written+n>target)throw new InvalidDataException("Limites du patch invalides.");
                if(op==1){if(offset<0||offset+n>input.Length)throw new InvalidDataException("Source du patch invalide.");input.Position=offset;}
                int remaining=n;
                while(remaining>0){int take=Math.Min(buffer.Length,remaining);Exact(op==1?(Stream)input:(Stream)zip,buffer,take);dest.Write(buffer,0,take);remaining-=take;}
                written+=n;int percent=target==0?100:(int)(written*100/target);
                if(percent!=last&&progress!=null){progress(percent);last=percent;}
            }
            if(written!=target)throw new InvalidDataException("Le fichier reconstruit est incomplet.");
            if(zip.ReadByte()!=-1)throw new InvalidDataException("Données supplémentaires dans le patch.");
            dest.Flush(true);
        }
    }
}
}
