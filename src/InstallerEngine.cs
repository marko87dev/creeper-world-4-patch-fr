using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace Cw4French {
public static class InstallerEngine {
    const string BackupName="CW4_Patch_FR_Backup";
    const string NormalizedHash="5394a6baa78d7cf8784fa4b53c34ef52ddfac4a474e13ec810eda8a0e5bc8391";
    sealed class Entry {
        public string Relative,Original,French,Resource;
        public bool Normalize;
        public Entry(string p,string a,string b,string r,bool n){Relative=p;Original=a;French=b;Resource=r;Normalize=n;}
    }
    static readonly Entry[] Entries={
        new Entry(@"CW4_Data\data.unity3d","763bd1138439ca062065e2d8beaa8f2d70332f75f0ac2f50191760caf296f61b","453dc93829d96a69408d3314bc08cc2bef5d4151f6e721a840d18be141f84446","PayloadAssets",true),
        new Entry(@"CW4_Data\il2cpp_data\Metadata\global-metadata.dat","0175276df7e2489f35a742a5b06f8e8827ddfac4750aa1d5053b4986834da0da","40eaf17fea10662321098d74d7cee6a820b495dc6350a12d118bd601d885a7a1","PayloadMetadata",false)
    };
    static string Hash(string file){using(FileStream f=File.OpenRead(file))using(SHA256 h=SHA256.Create())return BitConverter.ToString(h.ComputeHash(f)).Replace("-","").ToLowerInvariant();}
    static string Root(string directory){
        if(String.IsNullOrWhiteSpace(directory))throw new IOException("Choisissez le dossier contenant CW4.exe.");
        string root=Path.GetFullPath(directory.Trim().Trim('"')).TrimEnd(Path.DirectorySeparatorChar);
        if(!File.Exists(Path.Combine(root,"CW4.exe")))throw new IOException("CW4.exe est introuvable. Choisissez le dossier d’installation du jeu.");
        return root;
    }
    static string Backup(string root,Entry e){return Path.Combine(root,BackupName,Path.GetFileName(e.Relative));}
    static void Report(Action<string,int> r,string text,int n){if(r!=null)r(text,n);}
    static string[] Inspect(string root){
        string[] hashes=new string[Entries.Length];
        for(int i=0;i<Entries.Length;i++){
            string file=Path.Combine(root,Entries[i].Relative);
            if(!File.Exists(file))throw new IOException("Fichier du jeu introuvable : "+Entries[i].Relative);
            hashes[i]=Hash(file);
            if(hashes[i]!=Entries[i].Original&&hashes[i]!=Entries[i].French)
                throw new IOException("Les fichiers de cette installation ne sont pas reconnus par ce patch.\r\n\r\nVersion visée : Creeper World 4 — 2.5.1. Aucune boutique n’est imposée ; les fichiers sont vérifiés pour garantir un résultat correct.\r\nFichier différent : "+Entries[i].Relative+"\r\n\r\nAucun fichier du jeu n’a été remplacé. Une autre version ou une édition dont les fichiers diffèrent nécessite un patch adapté.");
        }
        return hashes;
    }
    static void CheckClosed(string root){
        Process[] processes=Process.GetProcessesByName("CW4");bool running=false;
        foreach(Process p in processes){try{if(String.Equals(p.MainModule.FileName,Path.Combine(root,"CW4.exe"),StringComparison.OrdinalIgnoreCase))running=true;}catch{running=true;}finally{p.Dispose();}}
        if(running)throw new IOException("Fermez Creeper World 4 avant de modifier sa langue.");
    }
    static void SafeClean(string root,string stage){
        if(String.IsNullOrEmpty(stage)||!Directory.Exists(stage))return;
        string full=Path.GetFullPath(stage);
        if(Path.GetDirectoryName(full)!=root||!Path.GetFileName(full).StartsWith("CW4_FR_Stage_",StringComparison.Ordinal))return;
        if((File.GetAttributes(full)&FileAttributes.ReparsePoint)!=0)return;
        try{Directory.Delete(full,true);}catch{}
    }
    static void CheckBackupDirectory(string root){
        string dir=Path.Combine(root,BackupName);
        if(Directory.Exists(dir)&&(File.GetAttributes(dir)&FileAttributes.ReparsePoint)!=0)
            throw new IOException("Le dossier de sauvegarde est un lien. Choisissez une installation avec un dossier de sauvegarde normal.");
        if(File.Exists(dir))throw new IOException("Un fichier porte déjà le nom du dossier de sauvegarde : "+BackupName);
    }
    static void EnsureBackups(string root,string[] hashes){
        CheckBackupDirectory(root);
        // Validate all existing backups before writing anything.
        for(int i=0;i<Entries.Length;i++){
            string backup=Backup(root,Entries[i]);
            if(File.Exists(backup)&&Hash(backup)!=Entries[i].Original)
                throw new IOException("La sauvegarde anglaise ne correspond pas à la version attendue : "+Path.GetFileName(backup)+". Elle n’a pas été écrasée.");
            if(!File.Exists(backup)&&hashes[i]!=Entries[i].Original)
                throw new IOException("La sauvegarde anglaise est absente. Impossible de modifier cette installation déjà traduite sans ses originaux.");
        }
        Directory.CreateDirectory(Path.Combine(root,BackupName));
        for(int i=0;i<Entries.Length;i++){
            string backup=Backup(root,Entries[i]);
            if(File.Exists(backup))continue;
            string temporary=backup+"."+Guid.NewGuid().ToString("N")+".tmp";
            try{
                File.Copy(Path.Combine(root,Entries[i].Relative),temporary,false);
                if(Hash(temporary)!=Entries[i].Original)throw new IOException("La copie de sauvegarde n’a pas pu être vérifiée.");
                File.Move(temporary,backup);
            }finally{if(File.Exists(temporary))File.Delete(temporary);}
        }
    }
    static void Commit(string root,string stage,string[] before,string[] prepared,bool restore){
        CheckClosed(root);
        for(int i=0;i<Entries.Length;i++)if(Hash(Path.Combine(root,Entries[i].Relative))!=before[i])throw new IOException("Un fichier du jeu a changé pendant la préparation. Aucun fichier n’a été remplacé.");
        List<int> committed=new List<int>();
        try{
            for(int i=0;i<Entries.Length;i++){
                string target=Path.Combine(root,Entries[i].Relative),rollback=Path.Combine(stage,"rollback"+i);
                File.Replace(prepared[i],target,rollback);committed.Add(i);
            }
            for(int i=0;i<Entries.Length;i++)if(Hash(Path.Combine(root,Entries[i].Relative))!=(restore?Entries[i].Original:Entries[i].French))throw new IOException("La vérification finale a échoué.");
        }catch(Exception failure){
            List<string> rollbackErrors=new List<string>();
            for(int j=committed.Count-1;j>=0;j--){int i=committed[j];try{File.Replace(Path.Combine(stage,"rollback"+i),Path.Combine(root,Entries[i].Relative),null);}catch(Exception e){rollbackErrors.Add(e.Message);}}
            if(rollbackErrors.Count>0){IOException problem=new IOException("L’opération a échoué et le retour automatique est incomplet. Les originaux sont conservés dans "+Path.Combine(root,BackupName)+". Les fichiers de récupération restent dans "+stage+".\r\n"+String.Join("\r\n",rollbackErrors.ToArray()),failure);problem.Data["KeepRecovery"]=true;throw problem;}
            throw new IOException("L’opération a échoué. Les fichiers précédents ont été rétablis.\r\n"+failure.Message,failure);
        }
    }
    static void Execute(string directory,bool restore,Action<string,int> report){
        string root=Root(directory),stage=null;bool recoveryNeeded=false;
        CheckClosed(root);Report(report,"Vérification de la version du jeu…",2);
        string[] before=Inspect(root);bool already=true;
        for(int i=0;i<Entries.Length;i++)if(before[i]!=(restore?Entries[i].Original:Entries[i].French))already=false;
        if(already){Report(report,restore?"Le jeu est déjà en anglais.":"Le patch français est déjà installé.",100);return;}
        CheckBackupDirectory(root);
        if(restore){for(int i=0;i<Entries.Length;i++)if(!File.Exists(Backup(root,Entries[i]))||Hash(Backup(root,Entries[i]))!=Entries[i].Original)throw new IOException("Sauvegarde anglaise absente ou modifiée. Restauration annulée sans remplacer les fichiers.");}
        DriveInfo drive=new DriveInfo(Path.GetPathRoot(root));
        if(drive.AvailableFreeSpace<3L*1024*1024*1024)throw new IOException("Prévoyez au moins 3 Go d’espace libre pour préparer le patch et sauvegarder les originaux.");
        try{
            stage=Path.Combine(root,"CW4_FR_Stage_"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(stage);
            if(!restore){Report(report,"Sauvegarde des fichiers anglais…",8);EnsureBackups(root,before);}
            string[] prepared=new string[Entries.Length];
            for(int i=0;i<Entries.Length;i++){
                Entry e=Entries[i];prepared[i]=Path.Combine(stage,"prepared"+i);
                if(restore){Report(report,"Préparation de la version anglaise…",20+i*25);File.Copy(Backup(root,e),prepared[i],false);}
                else{
                    string source=Backup(root,e);
                    if(e.Normalize){string normalized=Path.Combine(stage,"source.normalized");Report(report,"Préparation des textes du jeu…",12);Bundle.Normalize(source,normalized,delegate(int n){Report(report,"Préparation des textes du jeu…",12+n*18/100);});
                        if(Hash(normalized)!=NormalizedHash)throw new IOException("Le contenu du jeu ne correspond pas à la version attendue.");source=normalized;}
                    int index=i;using(Stream payload=Assembly.GetExecutingAssembly().GetManifestResourceStream(e.Resource)){
                        if(payload==null)throw new IOException("Installateur incomplet. Téléchargez à nouveau le patch.");
                        Delta.Apply(source,payload,prepared[i],delegate(int n){Report(report,index==0?"Installation de la traduction…":"Préparation des libellés…",index==0?32+n*35/100:70+n*10/100);});
                    }
                }
                Report(report,"Vérification des fichiers préparés…",85+i*3);
                if(Hash(prepared[i])!=(restore?e.Original:e.French))throw new IOException("Le résultat du patch est invalide. Aucun fichier du jeu n’a été remplacé.");
            }
            Report(report,"Mise en place des fichiers vérifiés…",92);
            recoveryNeeded=true;Commit(root,stage,before,prepared,restore);recoveryNeeded=false;
            Report(report,restore?"Version anglaise restaurée. Vous pouvez relancer le jeu.":"Patch français installé. Vous pouvez relancer le jeu.",100);
        }catch(UnauthorizedAccessException e){recoveryNeeded=false;throw new IOException("Accès refusé au dossier du jeu. Fermez le jeu et relancez cet installateur avec « Exécuter en tant qu’administrateur ».",e);}
        catch(Exception e){recoveryNeeded=e.Data.Contains("KeepRecovery")&&(bool)e.Data["KeepRecovery"];throw;}
        finally{if(!recoveryNeeded)SafeClean(root,stage);}
    }
    public static void Install(string directory,Action<string,int> report){Execute(directory,false,report);}
    public static void Restore(string directory,Action<string,int> report){Execute(directory,true,report);}
    public static string FindGameDirectory(){
        List<string> dirs=new List<string>();string own=Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        dirs.Add(own);if(Directory.GetParent(own)!=null)dirs.Add(Directory.GetParent(own).FullName);
        try{using(RegistryKey k=Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\GOG.com\Games\1229872913"))if(k!=null)dirs.Add(Convert.ToString(k.GetValue("path")));}catch{}
        try{using(RegistryKey k=Registry.LocalMachine.OpenSubKey(@"SOFTWARE\GOG.com\Games\1229872913"))if(k!=null)dirs.Add(Convert.ToString(k.GetValue("path")));}catch{}
        try{using(RegistryKey k=Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam"))if(k!=null){
            string steam=Convert.ToString(k.GetValue("SteamPath"));
            if(!String.IsNullOrWhiteSpace(steam)){
                dirs.Add(Path.Combine(steam,@"steamapps\common\Creeper World 4"));
                string libraries=Path.Combine(steam,@"steamapps\libraryfolders.vdf");
                if(File.Exists(libraries))foreach(Match m in Regex.Matches(File.ReadAllText(libraries),"\"path\"\\s*\"([^\"]+)\""))
                    dirs.Add(Path.Combine(m.Groups[1].Value.Replace(@"\\",@"\"),@"steamapps\common\Creeper World 4"));
            }
        }}catch{}
        foreach(string d in dirs)if(!String.IsNullOrWhiteSpace(d)&&File.Exists(Path.Combine(d,"CW4.exe")))return d;
        return "";
    }
    public static int RunCli(string[] args){
        List<string> logs=new List<string>();string log=null;
        for(int i=0;i+1<args.Length;i++)if(args[i]=="--log")log=args[i+1];
        try{
            if(args.Length<2)throw new ArgumentException("Utilisation : --install DOSSIER ou --restore DOSSIER [--log FICHIER]");
            Action<string,int> report=delegate(string s,int n){string line=n+"% "+s;logs.Add(line);Console.WriteLine(line);};
            if(args[0]=="--install")Install(args[1],report);
            else if(args[0]=="--restore")Restore(args[1],report);
            else if(args[0]=="--check"){Inspect(Root(args[1]));logs.Add("Installation reconnue.");}
            else throw new ArgumentException("Commande inconnue.");
            return 0;
        }catch(Exception e){logs.Add("ERREUR : "+e.Message);Console.Error.WriteLine(e.Message);return 1;}
        finally{if(log!=null)try{File.WriteAllLines(log,logs.ToArray(),Encoding.UTF8);}catch{}}
    }
}
}
