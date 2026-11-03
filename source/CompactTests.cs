using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Collections.Generic;
public static class CompactTests {
 static List<string> passed=new List<string>();
 static void Check(bool ok,string name){if(!ok)throw new Exception(name);passed.Add(name);}
 static void Reject(Action a,string name){bool fail=false;try{a();}catch{fail=true;}Check(fail,name);}
 public static void Run(string patcher,string root){Directory.CreateDirectory(root);string sandbox=Path.Combine(root,"r"+Guid.NewGuid().ToString("N").Substring(0,6));Directory.CreateDirectory(sandbox);string temp=Path.Combine(sandbox,"temp");Directory.CreateDirectory(temp);
  byte[] marker={0xa3,0x3a,0x4a,0xa4,0xaf,0x26,0xa6,0x9b};byte[] clean=new byte[128];Array.Copy(marker,0,clean,20,8);
  byte[] result=PatchEngine.StagePatch(patcher,clean,temp);Check(PatchEngine.Patched(result),"real installed dtkit 0.1.7 patches synthetic database");Check(Directory.GetFileSystemEntries(temp).Length==0,"temporary patcher database and bak removed");Check(result.SequenceEqual(PatchEngine.StagePatch(patcher,result,temp)),"repeat Patch does not unpatch");Reject(()=>PatchEngine.StagePatch(patcher,new byte[128],temp),"unsupported database rejected");Check(Directory.GetFileSystemEntries(temp).Length==0,"failed patch leaves no staging backup");
  string game=Path.Combine(sandbox,"steamapps/common/Game"),package=Path.Combine(sandbox,"package");Directory.CreateDirectory(game);
  foreach(string rel in PatchEngine.LoaderFiles)Engine.Atomic(Engine.Safe(game,rel),rel=="bundle/bundle_database.data"?clean:new byte[]{1});Engine.Atomic(Engine.Safe(game,"binaries/Darktide.exe"),new byte[]{0});File.Copy(patcher,Engine.Safe(game,"tools/dtkit-patch.exe"),true);
  Engine.Atomic(Engine.Safe(game,"mods/mod_load_order.txt"),Encoding.UTF8.GetBytes("OtherMod\r\n"));byte[] order=File.ReadAllBytes(Engine.Safe(game,"mods/mod_load_order.txt"));
  Engine.Atomic(Engine.Safe(game,Categories.RuntimePath),Encoding.UTF8.GetBytes("local mod = get_mod(\"thai_localization\")\nlocal function register_translations(filename)\n    return filename\nend\n"));
  File.AppendAllText(Engine.Safe(game,Categories.RuntimePath),String.Join("\n",Categories.Keys.Select(k=>"register_translations(\""+k+"\")"))+"\n");
  foreach(string key in Categories.Keys)Engine.Atomic(Engine.Safe(game,"mods/thai_localization/scripts/mods/thai_localization/translations/"+key+".lua"),Encoding.UTF8.GetBytes("return {}"));
  PatchEngine.Apply(game,package,false,temp);Check(PatchEngine.Patched(File.ReadAllBytes(Engine.Safe(game,"bundle/bundle_database.data"))),"Patch-only writes synthetic game database");Check(order.SequenceEqual(File.ReadAllBytes(Engine.Safe(game,"mods/mod_load_order.txt"))),"Patch-only preserves mod load order");
  byte[] first=File.ReadAllBytes(Engine.Safe(game,"bundle/bundle_database.data"));PatchEngine.Apply(game,package,false,temp);Check(first.SequenceEqual(File.ReadAllBytes(Engine.Safe(game,"bundle/bundle_database.data"))),"repeated Apply is idempotent");
  string relfile="mods/thai_localization/scripts/mods/thai_localization/translations/test.lua";byte[] data=Encoding.UTF8.GetBytes("return {} -- updated");Engine.Atomic(Engine.Safe(Path.Combine(package,"payload"),relfile),data);Engine.Save(Path.Combine(package,"package.json"),new Package{version="test",build="123",files=new List<Entry>{new Entry{path=relfile,hash=Engine.Sha(data)}}});
  foreach(string rel in Engine.Requirements)if(!File.Exists(Engine.Safe(game,rel)))Engine.Atomic(Engine.Safe(game,rel),new byte[]{1});Engine.Atomic(Path.GetFullPath(Path.Combine(game,"../../appmanifest_1361210.acf")),Encoding.UTF8.GetBytes("\"buildid\" \"123\""));
  PatchEngine.Apply(game,package,true,temp);Check(File.ReadAllBytes(Engine.Safe(game,relfile)).SequenceEqual(data),"optional Thai update writes payload");Check(File.ReadAllText(Engine.Safe(game,"mods/mod_load_order.txt")).Contains("OtherMod"),"optional update preserves unrelated mods");Check(!Directory.GetFiles(sandbox,"*.bak",SearchOption.AllDirectories).Any()&&!Directory.GetDirectories(sandbox,"backups",SearchOption.AllDirectories).Any(),"no retained backups");
  Engine.Atomic(Engine.Safe(Path.Combine(package,"payload"),relfile),new byte[]{0});Reject(()=>PatchEngine.Apply(game,package,true,temp),"corrupt optional package rejected");Check(File.ReadAllBytes(Engine.Safe(game,relfile)).SequenceEqual(data),"corrupt package leaves installed file unchanged");
  CategoryTests.Run(game,package,temp,root);
  Engine.Save(Path.Combine(root,"test-results.json"),new {passed=passed.Count,tests=passed.ToArray(),scope="Synthetic database and fake game only, real installed patcher used; no real game database written"});
 }
}
