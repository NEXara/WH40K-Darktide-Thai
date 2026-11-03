using System;
using System.IO;
using System.Text;
using System.Linq;
public static class CompatibilityProbe {
 [STAThread] public static int Main(string[] args) {
  try {
   string package=Path.GetFullPath(args[0]),root=Path.GetFullPath(args[1]),live=Path.GetFullPath(args[2]);
   string game=Path.Combine(root,"steamapps/common/g"),temp=Path.Combine(root,"stage");
   if(Directory.Exists(root))throw new Exception("Probe root already exists");
   byte[] original=File.ReadAllBytes(Engine.Safe(live,"bundle/bundle_database.data"));string originalHash=Engine.Sha(original);
   var m=Engine.ValidatePackage(package);if(Engine.Build(live)!="26101089")throw new Exception("Unexpected live build");
   if(PatchEngine.Patched(original))throw new Exception("Expected unpatched fresh game database");
   Engine.Atomic(Engine.Safe(game,"binaries/Darktide.exe"),new byte[]{0});
   Engine.Atomic(Engine.Safe(game,"bundle/bundle_database.data"),original);
   Engine.Atomic(Path.Combine(root,"steamapps/appmanifest_1361210.acf"),Encoding.UTF8.GetBytes("\"buildid\" \"26101089\""));
   Directory.CreateDirectory(temp);Engine.GameRunning=()=>false;
   PatchEngine.Apply(game,package,true,temp,Categories.Keys);
   byte[] patched=File.ReadAllBytes(Engine.Safe(game,"bundle/bundle_database.data"));
   if(!PatchEngine.Patched(patched)||patched.Length!=original.Length+100)throw new Exception("Unexpected patch result");
   if(!m.files.All(e=>File.Exists(Engine.Safe(game,e.path))&&Engine.Hash(Engine.Safe(game,e.path))==e.hash))throw new Exception("Installed package mismatch");
   PatchEngine.Apply(game,package,false,temp,new[]{"talents","abilities"});
   if(!patched.SequenceEqual(File.ReadAllBytes(Engine.Safe(game,"bundle/bundle_database.data"))))throw new Exception("Repeated Patch changed database");
   if(!Categories.Load(game).SequenceEqual(new[]{"talents","abilities"}))throw new Exception("Category selection failed");
   if(Engine.Hash(Engine.Safe(live,"bundle/bundle_database.data"))!=originalHash)throw new Exception("Live database changed during probe");
   if(Directory.GetFileSystemEntries(temp).Length!=0)throw new Exception("Staging left behind");
   Engine.Save(Path.Combine(root,"compatibility-results.json"),new{build="26101089",full_install="PASS",repeat_patch="PASS",category_selection="PASS",payload_hashes="PASS",live_database_unchanged=true,original_database_sha256=originalHash,original_bytes=original.Length,patched_bytes=patched.Length,gameplay_tested=false});
   Console.WriteLine("PASS: full installer on copied real build 26101089 database; repeat Patch; payload hashes; categories; live database unchanged");return 0;
  }catch(Exception e){Console.Error.WriteLine(e.ToString());return 1;}
 }
}
