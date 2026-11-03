using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Collections.Generic;
public static class FreshTests {
 static List<string> checks=new List<string>();
 static void Assert(bool ok,string name){if(!ok)throw new Exception(name);checks.Add(name);}
 static void Reject(Action a,string name){bool fail=false;try{a();}catch{fail=true;}Assert(fail,name);}
 static void Canceled(Action a,string name){bool canceled=false;try{a();}catch(OperationCanceledException){canceled=true;}Assert(canceled,name);}
 public static void Run(string package,string root){var gameRunning=Engine.GameRunning;try{Engine.GameRunning=()=>false;RunFixture(package,root);}finally{Engine.GameRunning=gameRunning;}}
 static void RunFixture(string package,string root){Directory.CreateDirectory(root);string sandbox=Path.Combine(root,Guid.NewGuid().ToString("N").Substring(0,5));string game=Path.Combine(sandbox,"steamapps/common/g");string temp=Path.Combine(sandbox,"t");Directory.CreateDirectory(temp);
  var m=Engine.ValidatePackage(package);byte[] db=new byte[128];Array.Copy(new byte[]{0xa3,0x3a,0x4a,0xa4,0xaf,0x26,0xa6,0x9b},0,db,20,8);
  Engine.Atomic(Engine.Safe(game,"binaries/Darktide.exe"),new byte[]{0});Engine.Atomic(Engine.Safe(game,"bundle/bundle_database.data"),db);Engine.Atomic(Path.Combine(sandbox,"steamapps/appmanifest_1361210.acf"),Encoding.UTF8.GetBytes("\"buildid\" \""+m.build+"\""));
  Assert(!Directory.Exists(Engine.Safe(game,"mods"))&&!Directory.Exists(Engine.Safe(game,"tools")),"fixture starts with no mods or patcher installed");
  Engine.GameRunning=()=>true;Reject(()=>PatchEngine.Apply(game,package,false,temp,Categories.Keys),"running-game guard rejects Patch before any update");Engine.GameRunning=()=>false;
  Reject(()=>PatchEngine.Apply(game,package,false,temp,Categories.Keys),"patch-only reports missing first-install components");Assert(db.SequenceEqual(File.ReadAllBytes(Engine.Safe(game,"bundle/bundle_database.data"))),"missing-component rejection leaves game database unchanged");
  PatchEngine.Apply(game,package,true,temp,Categories.Keys);
  Assert(m.files.All(e=>File.Exists(Engine.Safe(game,e.path))&&Engine.Hash(Engine.Safe(game,e.path))==e.hash),"every payload file installed and hash-verified on clean fixture");
  Assert(File.Exists(Engine.Safe(game,"mods/SimpleAssets/bin/simple-assets-runtime.dll")),"font-loading native DLL present");Assert(File.Exists(Engine.Safe(game,"mods/thai_localization/assets/fonts/thai_test.slug")),"Thai Slug font present at runtime asset path");
  Assert(File.Exists(Engine.Safe(game,"mods/thai_localization/scripts/mods/thai_localization/font_fallbacks.lua")),"multilingual player-name font selector installed on a clean game");
  string runtime=File.ReadAllText(Engine.Safe(game,Categories.RuntimePath));Assert(runtime.Contains("SimpleAssets.load_font")&&runtime.Contains("font_for_text")&&runtime.Contains("script_draw_text")&&runtime.Contains("fonts/thai_test.slug"),"Thai font registration rendering and layout hooks preserved");
  Assert(PatchEngine.Patched(File.ReadAllBytes(Engine.Safe(game,"bundle/bundle_database.data"))),"clean fixture database patched using bundled patcher");
  string order=File.ReadAllText(Engine.Safe(game,"mods/mod_load_order.txt"));Assert(order.StartsWith("SimpleAssets\r\nWhatTheLocalization\r\nthai_localization"),"clean fixture receives dependency load order");
  Assert(!Directory.Exists(Engine.Safe(game,"mods/LocalizationExport")),"authoring-only LocalizationExport not installed");Assert(Categories.Load(game).Length==Categories.Keys.Length,"all categories enabled on first install");
  Engine.Atomic(Engine.Safe(game,"mods/OtherMod/data.txt"),Encoding.UTF8.GetBytes("keep"));File.AppendAllText(Engine.Safe(game,"mods/mod_load_order.txt"),"\r\nOtherMod\r\n-- comment\r\n");
  PatchEngine.Apply(game,package,true,temp,new[]{"talents"});Assert(Categories.Load(game).SequenceEqual(new[]{"talents"}),"custom talent-only selection installed");Assert(File.ReadAllText(Engine.Safe(game,"mods/OtherMod/data.txt"))=="keep"&&File.ReadAllText(Engine.Safe(game,"mods/mod_load_order.txt")).Contains("OtherMod"),"reinstall preserves unrelated mod files and load order");
  byte[] patched=File.ReadAllBytes(Engine.Safe(game,"bundle/bundle_database.data"));PatchEngine.Apply(game,package,false,temp,Categories.Groups[0]);Assert(patched.SequenceEqual(File.ReadAllBytes(Engine.Safe(game,"bundle/bundle_database.data"))),"repeat patch does not disable loader");Assert(Categories.Load(game).Length==4,"grouped Weapons and Items selection applies");
  Assert(Directory.GetFileSystemEntries(temp).Length==0&&!Directory.GetFiles(sandbox,"*.bak",SearchOption.AllDirectories).Any(),"no retained patch staging or backup");
  string legacySelection=Encoding.UTF8.GetString(Categories.Config(new[]{"talents","abilities"})).Replace("    subtitles = false,\n","");
  Engine.Atomic(Engine.Safe(game,Categories.ConfigPath),Encoding.UTF8.GetBytes(legacySelection));
  Assert(Categories.Load(game).SequenceEqual(new[]{"talents","abilities"}),"previous ten-module selection preserved without enabling subtitles");
  string legacyRuntime=File.ReadAllText(Engine.Safe(game,Categories.RuntimePath)).Replace("register_translations(\"subtitles\")","");
  byte[] migrated=Categories.Runtime(Encoding.UTF8.GetBytes(legacyRuntime));
  Assert(Encoding.UTF8.GetString(migrated).Split(new[]{"register_translations(\"subtitles\")"},StringSplitOptions.None).Length==2&&migrated.SequenceEqual(Categories.Runtime(migrated)),"existing category-gated runtime registers subtitles exactly once");
  PatchEngine.Apply(game,package,false,temp,new[]{"subtitles"});
  Assert(Categories.Load(game).SequenceEqual(new[]{"subtitles"})&&File.Exists(Engine.Safe(game,"mods/thai_localization/scripts/mods/thai_localization/translations/subtitles.lua")),"subtitles-only selection round trips with the module present");
  using(var panel=new CategoryPanel()){panel.SetSelection(new[]{"subtitles"});panel.SetCustom(true);panel.SetCustom(false);Assert(panel.Selected().SequenceEqual(new[]{"subtitles"}),"new category survives Custom and grouped mode switches");}
  string native=Engine.Safe(game,"mods/SimpleAssets/bin/simple-assets-runtime.dll");byte[] good=File.ReadAllBytes(native);Engine.Atomic(native,new byte[]{0});Reject(()=>PatchEngine.Apply(game,package,true,temp,Categories.Keys),"existing different dependency version rejected without overwriting");Assert(File.ReadAllBytes(native).Length==1,"dependency conflict file remains untouched");Engine.Atomic(native,good);
  string subtitleRel="mods/thai_localization/scripts/mods/thai_localization/translations/subtitles.lua";
  string talentRel="mods/thai_localization/scripts/mods/thai_localization/translations/talents.lua";
  File.Delete(Engine.Safe(game,subtitleRel));
  Engine.Atomic(Engine.Safe(game,talentRel),Encoding.UTF8.GetBytes("return {} -- old translation"));
  Engine.Atomic(Engine.Safe(game,Categories.RuntimePath),Encoding.UTF8.GetBytes(legacyRuntime));
  PatchEngine.Apply(game,package,false,temp,Categories.Keys);
  Assert(Engine.Hash(Engine.Safe(game,subtitleRel))==m.files.Single(e=>e.path==subtitleRel).hash,"Patch without full-install installs missing subtitles on an existing game");
  Assert(Engine.Hash(Engine.Safe(game,talentRel))==m.files.Single(e=>e.path==talentRel).hash&&File.ReadAllText(Engine.Safe(game,Categories.RuntimePath)).Contains("register_translations(\"subtitles\")"),"Patch refreshes old translation files and registers the new module");
  Engine.Atomic(native,new byte[]{0});
  PatchEngine.Apply(game,package,false,temp,Categories.Keys);
  Assert(File.ReadAllBytes(native).SequenceEqual(new byte[]{0})&&File.ReadAllText(Engine.Safe(game,"mods/OtherMod/data.txt"))=="keep","translation-only Patch preserves third-party dependencies and unrelated mods");
  Engine.Atomic(native,good);
  string corruptPackage=Path.Combine(sandbox,"cp");
  Engine.Atomic(Engine.Safe(Path.Combine(corruptPackage,"payload"),subtitleRel),Encoding.UTF8.GetBytes("corrupt"));
  Engine.Save(Path.Combine(corruptPackage,"package.json"),new Package{version="bad-fixture",build=m.build,files=new List<Entry>{new Entry{path=subtitleRel,hash=m.files.Single(e=>e.path==subtitleRel).hash}}});
  string beforeSub=Engine.Hash(Engine.Safe(game,subtitleRel)),beforeConfig=Engine.Hash(Engine.Safe(game,Categories.ConfigPath)),beforeDb=Engine.Hash(Engine.Safe(game,"bundle/bundle_database.data"));
  Reject(()=>PatchEngine.Apply(game,corruptPackage,false,temp,Categories.Keys),"corrupt translation package rejected when full-install is unchecked");
  Assert(Engine.Hash(Engine.Safe(game,subtitleRel))==beforeSub&&Engine.Hash(Engine.Safe(game,Categories.ConfigPath))==beforeConfig&&Engine.Hash(Engine.Safe(game,"bundle/bundle_database.data"))==beforeDb,"corrupt translation package leaves installed files selection and database unchanged");
  string acf=Path.Combine(sandbox,"steamapps/appmanifest_1361210.acf");
  foreach(string build in (m.supported_builds??new[]{m.build}).Where(b=>b!=m.build).Distinct()) {
   File.WriteAllText(acf,"\"buildid\" \""+build+"\"");
   PatchEngine.Apply(game,package,true,temp,Categories.Keys);
   Assert(PatchEngine.Patched(File.ReadAllBytes(Engine.Safe(game,"bundle/bundle_database.data"))),"explicitly supported previous game build installs");
  }
  File.WriteAllText(acf,"\"buildid\" \"99999999\"");
  string blockedDb=Engine.Hash(Engine.Safe(game,"bundle/bundle_database.data")),blockedConfig=Engine.Hash(Engine.Safe(game,Categories.ConfigPath));
  Canceled(()=>PatchEngine.Apply(game,package,true,temp,Categories.Keys),"untested build requires explicit confirmation in full-install mode");
  Canceled(()=>PatchEngine.Apply(game,package,false,temp,Categories.Keys),"untested build requires explicit confirmation in translation-only mode");
  Assert(blockedDb==Engine.Hash(Engine.Safe(game,"bundle/bundle_database.data"))&&blockedConfig==Engine.Hash(Engine.Safe(game,Categories.ConfigPath)),"no-confirmation rejection preserves database and selection in both modes");
  var snapshot=Directory.GetFiles(game,"*",SearchOption.AllDirectories).ToDictionary(p=>p,Engine.Hash);
  int prompts=0;
  Canceled(()=>PatchEngine.Apply(game,package,false,temp,new[]{"talents"},warning=>{prompts++;return false;}),"user can decline untested build after successful preflight");
  Assert(prompts==1&&snapshot.All(e=>File.Exists(e.Key)&&Engine.Hash(e.Key)==e.Value)&&Directory.GetFiles(game,"*",SearchOption.AllDirectories).Length==snapshot.Count,"declining confirmation leaves every game file unchanged");
  string result=PatchEngine.Apply(game,package,false,temp,new[]{"talents"},warning=>{prompts++;Assert(warning.Contains("99999999")&&warning.Contains("ยังไม่ยืนยัน"),"warning reports actual build and runtime limitations");return true;});
  Assert(prompts==2&&Categories.Load(game).SequenceEqual(new[]{"talents"})&&result.Contains("ยังไม่ได้ทดสอบ"),"confirmed untested build patches and reports untested status");
  Assert(blockedDb==Engine.Hash(Engine.Safe(game,"bundle/bundle_database.data")),"untested already-patched database passes exact loader-block verification without being changed");
  string unknownGame=Path.Combine(sandbox,"steamapps/common/newg");
  Engine.Atomic(Engine.Safe(unknownGame,"binaries/Darktide.exe"),new byte[]{0});Engine.Atomic(Engine.Safe(unknownGame,"bundle/bundle_database.data"),db);
  string unknownBefore=Engine.Hash(Engine.Safe(unknownGame,"bundle/bundle_database.data"));
  Canceled(()=>PatchEngine.Apply(unknownGame,package,true,temp,Categories.Keys,warning=>false),"first-install confirmation may be declined");
  Assert(!Directory.Exists(Engine.Safe(unknownGame,"mods"))&&!Directory.Exists(Engine.Safe(unknownGame,"tools"))&&Engine.Hash(Engine.Safe(unknownGame,"bundle/bundle_database.data"))==unknownBefore,"first-install cancellation installs no components");
  PatchEngine.Apply(unknownGame,package,true,temp,Categories.Keys,warning=>true);
  Assert(m.files.All(e=>Engine.Hash(Engine.Safe(unknownGame,e.path))==e.hash)&&PatchEngine.Patched(File.ReadAllBytes(Engine.Safe(unknownGame,"bundle/bundle_database.data"))),"new numeric build installs on clean fixture after copy round trip and confirmation");
  string dbPath=Engine.Safe(game,"bundle/bundle_database.data");byte[] validDb=File.ReadAllBytes(dbPath);
  byte[] duplicate=new byte[256];Array.Copy(db,duplicate,db.Length);Array.Copy(db,0,duplicate,128,db.Length);
  byte[] shortEntry=db.Take(50).ToArray();
  byte[] newBoot=db.Concat(Encoding.ASCII.GetBytes("9ba626afa44a3aa3.patch_001")).ToArray();
  byte[] changedPatched=validDb.Concat(Encoding.ASCII.GetBytes("9ba626afa44a3aa3.patch_001")).ToArray();
  foreach(byte[] invalid in new[]{new byte[128],Encoding.ASCII.GetBytes("unsupported patch_999 data"),duplicate,shortEntry,newBoot,changedPatched}){
   Engine.Atomic(dbPath,invalid);bool asked=false;
   Reject(()=>PatchEngine.Apply(game,package,false,temp,new[]{"abilities"},warning=>{asked=true;return true;}),"unsupported database rejected including forged patch marker");
   Assert(!asked&&Engine.Hash(dbPath)==Engine.Sha(invalid)&&Engine.Hash(Engine.Safe(game,Categories.ConfigPath))==Engine.Sha(Categories.Config(new[]{"talents"})),"bad database cannot reach confirmation or modify translation selection");
  }
  Engine.Atomic(dbPath,validDb);
  Engine.Atomic(native,new byte[]{0});bool dependencyPrompt=false;
  Reject(()=>PatchEngine.Apply(game,package,false,temp,Categories.Keys,warning=>{dependencyPrompt=true;return true;}),"different native font dependency blocks untested build even in translation-only mode");
  Assert(!dependencyPrompt&&Engine.Hash(dbPath)==Engine.Sha(validDb),"dependency failure occurs before confirmation and game writes");Engine.Atomic(native,good);
  byte[] acfBytes=File.ReadAllBytes(acf);File.Move(acf,acf+".held");
  PatchEngine.Apply(game,package,false,temp,new[]{"talents"},warning=>{Assert(warning.Contains("อ่านเลขรุ่นไม่ได้"),"missing manifest warns that build cannot be read");return true;});
  File.Move(acf+".held",acf);
  Reject(()=>PatchEngine.Apply(game,package,false,temp,new[]{"abilities"},warning=>{File.WriteAllText(acf,"\"buildid\" \"99999998\"");return true;}),"build change while user reviews warning stops installation");
  Assert(Categories.Load(game).SequenceEqual(new[]{"talents"}),"build-change rejection preserves selection");Engine.Atomic(acf,acfBytes);
  Reject(()=>PatchEngine.Apply(game,package,false,temp,new[]{"abilities"},warning=>{Engine.GameRunning=()=>true;return true;}),"game starting while confirmation is open stops installation");Engine.GameRunning=()=>false;
  Reject(()=>PatchEngine.Apply(game,package,false,temp,new[]{"abilities"},warning=>{Engine.Atomic(dbPath,db);return true;}),"database change while confirmation is open stops installation");
  Assert(Categories.Load(game).SequenceEqual(new[]{"talents"}),"race-condition failures preserve translation selection");Engine.Atomic(dbPath,validDb);
  foreach(string known in m.supported_builds??new[]{m.build}){
   File.WriteAllText(acf,"\"buildid\" \""+known+"\"");bool asked=false;
   PatchEngine.Apply(game,package,false,temp,Categories.Keys,warning=>{asked=true;return false;});
   Assert(!asked,"tested build installs without untested-build confirmation");
  }
  Assert(Directory.GetFileSystemEntries(temp).Length==0,"all compatibility staging cleaned after successes cancellations and failures");
  Assert(!m.files.Any(e=>e.path=="bundle/bundle_database.data"||e.path=="binaries/Darktide.exe"),"package excludes original game database and executable");
  Assert(System.Text.Encoding.ASCII.GetString(File.ReadAllBytes(Engine.Safe(game,"mods/thai_localization/assets/fonts/thai_test.slug"))).Contains("Noto Sans Thai"),"compiled font identifies Noto Sans Thai");
  RestoreTests.Run(game,package,temp,Assert,Reject);
  Engine.Save(Path.Combine(root,"fresh-results.json"),new {passed=checks.Count,tests=checks.ToArray(),payload_files=m.files.Count,scope="Real payload installation into clean synthetic game tree. Third-party mod DLL not executed; no Darktide gameplay or actual Thai rendering test. Real game directory not modified."});
 }
}
