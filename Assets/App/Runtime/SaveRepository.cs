using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using HabitGarden.Domain;
using UnityEngine;

namespace HabitGarden
{
    public sealed class SaveRepository : IStateStore
    {
        [Serializable] sealed class Envelope { public string payload, hash; }
        readonly string directory, snapshot, backup, journal;
        int sinceCheckpoint;
        public SaveRepository(string directory) {
            this.directory=directory;Directory.CreateDirectory(directory);
            snapshot=Path.Combine(directory,"state.json");backup=snapshot+".backup";journal=Path.Combine(directory,"journal.jsonl");
        }
        static string Hash(string s) {
            using(var sha=SHA256.Create()) return Convert.ToBase64String(sha.ComputeHash(Encoding.UTF8.GetBytes(s)));
        }
        public static string Export(AppState state) {
            HabitService.Validate(state); string json=JsonUtility.ToJson(state);
            return JsonUtility.ToJson(new Envelope {payload=json,hash=Hash(json)},true);
        }
        public static AppState Import(string text) {
            var e=JsonUtility.FromJson<Envelope>(text);
            if(e==null || e.payload==null || Hash(e.payload)!=e.hash) throw new InvalidDataException("Backup checksum failed");
            var s=JsonUtility.FromJson<AppState>(e.payload);
            if(s!=null&&s.schemaVersion>1)throw new NotSupportedException("This backup needs a newer version of Habit Garden.");
            HabitService.Validate(s);return s;
        }
        public AppState Load() {
            AppState best=null;bool any=File.Exists(snapshot)||File.Exists(backup)||File.Exists(journal);
            foreach(var path in new[]{snapshot,backup}) if(File.Exists(path)) {
                try {var s=Import(File.ReadAllText(path));if(best==null||s.revision>best.revision)best=s;}
                catch(Exception e) when(e is IOException || e is ArgumentException || e is InvalidOperationException || e is FormatException) { Debug.LogWarning("Save checkpoint was invalid; attempting journal recovery."); }
            }
            if(File.Exists(journal)) {
                string text=File.ReadAllText(journal);var lines=text.Split('\n');
                for(int i=0;i<lines.Length;i++) {
                    if(String.IsNullOrWhiteSpace(lines[i])) continue;
                    try {var s=Import(lines[i]);if(best==null||s.revision>best.revision)best=s;}
                    catch(Exception e) when(e is IOException || e is ArgumentException || e is InvalidOperationException || e is FormatException) {
                        if(i==lines.Length-1) {
                            int tail=text.LastIndexOf('\n');
                            using(var f=new FileStream(journal,FileMode.Open,FileAccess.Write,FileShare.Read)){f.SetLength(Encoding.UTF8.GetByteCount(tail<0?"":text.Substring(0,tail+1)));f.Flush(true);}
                            break;
                        } // Only an incomplete final append is recoverable.
                        throw new InvalidDataException("Save journal is damaged. Restore a backup before continuing.");
                    }
                }
            }
            if(best==null && any) throw new InvalidDataException("No valid save found. Restore a backup or explicitly reset.");
            return best??new AppState();
        }
        public void Commit(AppState state) {
            HabitService.Validate(state);string line=Export(state).Replace("\r","").Replace("\n","")+"\n";
            byte[] data=Encoding.UTF8.GetBytes(line);
            // A recovered JSON entry may be missing only its trailing newline.
            using(var stream=new FileStream(journal,FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.Read)) {
                long length=stream.Length;stream.Position=length;
                try {
                    if(length>0){stream.Position=length-1;int last=stream.ReadByte();stream.Position=length;if(last!='\n')stream.WriteByte((byte)'\n');}
                    stream.Write(data,0,data.Length);stream.Flush(true);
                }catch{stream.SetLength(length);stream.Flush(true);throw;}
            }
            // The journal commit is durable. Checkpoint failure must not turn a successful command into a rollback.
            if(++sinceCheckpoint>=32) {try{Checkpoint(state);}catch(Exception e) when(e is IOException || e is UnauthorizedAccessException || e is NotSupportedException){Debug.LogWarning("Save checkpoint delayed; journal remains intact.");}}
        }
        public void Checkpoint(AppState state) {
            string temp=snapshot+".tmp";byte[] bytes=Encoding.UTF8.GetBytes(Export(state));
            using(var f=new FileStream(temp,FileMode.Create,FileAccess.Write,FileShare.None)) {f.Write(bytes,0,bytes.Length);f.Flush(true);}
            Import(File.ReadAllText(temp));
            if(File.Exists(snapshot)) File.Replace(temp,snapshot,backup);else File.Move(temp,snapshot);
            using(var f=new FileStream(journal,FileMode.Create,FileAccess.Write,FileShare.Read))f.Flush(true);
            sinceCheckpoint=0;
        }
        public void ExplicitDelete() {
            foreach(var f in Directory.GetFiles(directory,"state.json*"))File.Delete(f);
            if(File.Exists(journal))File.Delete(journal);
        }
        public void PurgeHistory(AppState emptyState) {
            Checkpoint(emptyState);
            if(File.Exists(backup))File.Delete(backup);
            string temp=snapshot+".tmp";if(File.Exists(temp))File.Delete(temp);
        }
    }
}
