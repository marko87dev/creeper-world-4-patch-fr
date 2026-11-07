using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
namespace Cw4French {
// Read UnityFS/LZ4 containers and emit a deterministic uncompressed UnityFS
// container. Inner serialized files are copied byte for byte, never interpreted.
public static class Bundle {
    static uint U32(BinaryReader r){byte[] b=r.ReadBytes(4);if(b.Length!=4)throw new EndOfStreamException();return ((uint)b[0]<<24)|((uint)b[1]<<16)|((uint)b[2]<<8)|b[3];}
    static ulong U64(BinaryReader r){return ((ulong)U32(r)<<32)|U32(r);}
    static ushort U16(BinaryReader r){return (ushort)((r.ReadByte()<<8)|r.ReadByte());}
    static void W32(BinaryWriter w,uint x){w.Write(new byte[]{(byte)(x>>24),(byte)(x>>16),(byte)(x>>8),(byte)x});}
    static void W64(BinaryWriter w,ulong x){W32(w,(uint)(x>>32));W32(w,(uint)x);}
    static string Z(BinaryReader r){List<byte> b=new List<byte>();byte c;while((c=r.ReadByte())!=0){if(b.Count>1024)throw new InvalidDataException();b.Add(c);}return Encoding.UTF8.GetString(b.ToArray());}
    static void ZW(BinaryWriter w,string s){w.Write(Encoding.UTF8.GetBytes(s));w.Write((byte)0);}
    static byte[] Decode(byte[] src,int expected,int mode){
        if(mode==0){if(src.Length!=expected)throw new InvalidDataException("Bloc Unity invalide.");return src;}
        if(mode!=2&&mode!=3)throw new InvalidDataException("Compression de cette version du jeu non prise en charge.");
        byte[] dst=new byte[expected];int s=0,d=0;
        while(s<src.Length){
            byte token=src[s++];int literals=token>>4;
            if(literals==15){int n;do{if(s>=src.Length)throw new InvalidDataException();n=src[s++];literals+=n;}while(n==255);}
            if(s+literals>src.Length||d+literals>dst.Length)throw new InvalidDataException("Bloc LZ4 invalide.");
            Buffer.BlockCopy(src,s,dst,d,literals);s+=literals;d+=literals;if(s==src.Length)break;
            if(s+2>src.Length)throw new InvalidDataException();int offset=src[s]|(src[s+1]<<8);s+=2;
            int count=(token&15)+4;
            if((token&15)==15){int n;do{if(s>=src.Length)throw new InvalidDataException();n=src[s++];count+=n;}while(n==255);}
            if(offset<=0||offset>d||d+count>dst.Length)throw new InvalidDataException("Référence LZ4 invalide.");
            for(int i=0;i<count;i++)dst[d+i]=dst[d-offset+i];d+=count;
        }
        if(d!=expected)throw new InvalidDataException("Bloc Unity incomplet.");return dst;
    }
    public static void Normalize(string input,string output,Action<int> progress){
        using(FileStream file=File.OpenRead(input))using(BinaryReader r=new BinaryReader(file)){
            string signature=Z(r);uint version=U32(r);string player=Z(r),engine=Z(r);
            if(signature!="UnityFS"||version!=7)throw new InvalidDataException("Conteneur du jeu incompatible.");
            ulong size=U64(r);int compressed=checked((int)U32(r)),uncompressed=checked((int)U32(r));uint flags=U32(r);
            if(size!=(ulong)file.Length||compressed<=0||uncompressed<=0||uncompressed>32*1024*1024)throw new InvalidDataException("En-tête Unity invalide.");
            file.Position=(file.Position+15)&~15L;long dataStart=file.Position;
            if((flags&128)!=0)file.Position=file.Length-compressed;
            byte[] packed=r.ReadBytes(compressed);if(packed.Length!=compressed)throw new EndOfStreamException();
            byte[] info=Decode(packed,uncompressed,(int)(flags&63));
            if((flags&128)==0)dataStart=file.Position;
            if((flags&512)!=0)dataStart=(dataStart+15)&~15L;
            int[] rawSizes,packedSizes,modes;byte[] directory;long total=0;
            using(MemoryStream inf=new MemoryStream(info))using(BinaryReader ir=new BinaryReader(inf)){
                ir.ReadBytes(16);int count=checked((int)U32(ir));if(count<=0||count>100000)throw new InvalidDataException();
                rawSizes=new int[count];packedSizes=new int[count];modes=new int[count];
                for(int i=0;i<count;i++){rawSizes[i]=checked((int)U32(ir));packedSizes[i]=checked((int)U32(ir));modes[i]=U16(ir)&63;total+=rawSizes[i];}
                if(total<=0||total>2147483647)throw new InvalidDataException();
                directory=ir.ReadBytes(checked((int)(inf.Length-inf.Position)));
            }
            byte[] newInfo;
            using(MemoryStream inf=new MemoryStream())using(BinaryWriter iw=new BinaryWriter(inf)){
                iw.Write(new byte[16]);W32(iw,1);W32(iw,(uint)total);W32(iw,(uint)total);iw.Write((byte)0);iw.Write((byte)0);iw.Write(directory);iw.Flush();newInfo=inf.ToArray();
            }
            using(FileStream dest=new FileStream(output,FileMode.CreateNew,FileAccess.Write,FileShare.None))using(BinaryWriter w=new BinaryWriter(dest)){
                ZW(w,signature);W32(w,version);ZW(w,player);ZW(w,engine);long sizePos=dest.Position;W64(w,0);W32(w,(uint)newInfo.Length);W32(w,(uint)newInfo.Length);W32(w,64);
                while((dest.Position&15)!=0)w.Write((byte)0);
                ulong finalSize=(ulong)(dest.Position+newInfo.Length+total);long endHeader=dest.Position;dest.Position=sizePos;W64(w,finalSize);dest.Position=endHeader;w.Write(newInfo);
                file.Position=dataStart;
                for(int i=0;i<rawSizes.Length;i++){
                    byte[] block=r.ReadBytes(packedSizes[i]);if(block.Length!=packedSizes[i])throw new EndOfStreamException();
                    w.Write(Decode(block,rawSizes[i],modes[i]));if(progress!=null)progress((i+1)*100/rawSizes.Length);
                }
                w.Flush();dest.Flush(true);if((ulong)dest.Length!=finalSize)throw new InvalidDataException("Conteneur reconstruit incomplet.");
            }
        }
    }
}
}
