using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace HabitGarden.Editor
{
    internal static class TmpResources
    {
        // Package imports can be deferred in batch mode. Extract only the trusted TMP asset subtree,
        // then synchronously import the files before asking FontEngine to read a font face.
        internal static void Ensure(string packageRoot)
        {
            if(AssetDatabase.LoadAssetAtPath<Font>("Assets/TextMesh Pro/Fonts/LiberationSans.ttf")!=null && Shader.Find("TextMeshPro/Distance Field")!=null)return;
            string archive=Directory.GetFiles(packageRoot,"*.unitypackage",SearchOption.AllDirectories)
                .FirstOrDefault(path=>Path.GetFileName(path).IndexOf("Essential",StringComparison.OrdinalIgnoreCase)>=0);
            if(archive!=null)Extract(archive);
            else {
                string font=Directory.GetFiles(packageRoot,"LiberationSans.ttf",SearchOption.AllDirectories).FirstOrDefault();
                if(font!=null) {
                    Directory.CreateDirectory("Assets/TextMesh Pro/Fonts");File.Copy(font,"Assets/TextMesh Pro/Fonts/LiberationSans.ttf",true);
                    foreach(string license in Directory.GetFiles(Path.GetDirectoryName(font),"*OFL*"))File.Copy(license,"Assets/TextMesh Pro/Fonts/"+Path.GetFileName(license),true);
                }
            }
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            if(AssetDatabase.LoadAssetAtPath<Font>("Assets/TextMesh Pro/Fonts/LiberationSans.ttf")==null) {
                string candidates=String.Join(" | ",Directory.GetFiles(packageRoot,"*",SearchOption.AllDirectories)
                    .Where(path=>path.EndsWith(".unitypackage",StringComparison.OrdinalIgnoreCase)||path.EndsWith(".ttf",StringComparison.OrdinalIgnoreCase)).Take(8));
                throw new InvalidOperationException("TMP resource font was not found. Package: "+packageRoot+"; candidates: "+candidates);
            }
        }
        static void Extract(string archive)
        {
            var entries=new Dictionary<string,Dictionary<string,byte[]>>();
            using(var file=File.OpenRead(archive))using(var gzip=new GZipStream(file,CompressionMode.Decompress)) {
                var header=new byte[512];
                while(Read(gzip,header)) {
                    if(header.All(value=>value==0))break;
                    string name=Encoding.UTF8.GetString(header,0,100).TrimEnd('\0');
                    string sizeText=Encoding.ASCII.GetString(header,124,12).Trim('\0',' ');
                    long size=Convert.ToInt64(sizeText.Length==0?"0":sizeText,8);
                    if(size<0 || size>64*1024*1024)throw new InvalidDataException("Unexpected TMP package entry size.");
                    var bytes=new byte[(int)size];if(size>0 && !Read(gzip,bytes))throw new EndOfStreamException("Incomplete TMP package entry.");
                    int padding=(int)((512-size%512)%512);if(padding>0 && !Read(gzip,new byte[padding]))throw new EndOfStreamException("Incomplete TMP package padding.");
                    int separator=name.LastIndexOf('/');if(separator<0)continue;
                    string key=name.Substring(0,separator),role=name.Substring(separator+1);
                    if(role!="asset" && role!="asset.meta" && role!="pathname")continue;
                    if(!entries.TryGetValue(key,out var entry)){entry=new Dictionary<string,byte[]>();entries[key]=entry;}
                    entry[role]=bytes;
                }
            }
            string allowed=Path.GetFullPath("Assets/TextMesh Pro");
            foreach(var entry in entries.Values) {
                if(!entry.TryGetValue("pathname",out var name))continue;
                string relative=Encoding.UTF8.GetString(name).Trim('\0','\r','\n',' ');
                if(relative!="Assets/TextMesh Pro" && !relative.StartsWith("Assets/TextMesh Pro/",StringComparison.Ordinal))continue;
                string target=Path.GetFullPath(relative);
                if(target!=allowed && !target.StartsWith(allowed+Path.DirectorySeparatorChar,StringComparison.Ordinal))throw new InvalidDataException("TMP package path escaped its asset directory.");
                if(entry.TryGetValue("asset",out var bytes)) {
                    Directory.CreateDirectory(Path.GetDirectoryName(target));File.WriteAllBytes(target,bytes);
                }else Directory.CreateDirectory(target);
                if(entry.TryGetValue("asset.meta",out var meta))File.WriteAllBytes(target+".meta",meta);
            }
        }
        static bool Read(Stream stream,byte[] buffer)
        {
            int offset=0;
            while(offset<buffer.Length){int count=stream.Read(buffer,offset,buffer.Length-offset);if(count==0){if(offset==0)return false;throw new EndOfStreamException("Incomplete TMP package data.");}offset+=count;}
            return true;
        }
    }
}
