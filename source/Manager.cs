using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Web.Script.Serialization;
using System.Windows.Forms;
using System.Drawing;
using System.Diagnostics;
using System.Text.RegularExpressions;

[assembly: System.Reflection.AssemblyTitle("WH40K: Darktide - Mod แปลไทย")]
[assembly: System.Reflection.AssemblyProduct("WH40K: Darktide - Mod แปลไทย")]
[assembly: System.Reflection.AssemblyVersion("1.0.4.0")]
[assembly: System.Reflection.AssemblyFileVersion("1.0.4.0")]
public class Entry { public string path; public string hash; public bool existed; public string installedHash; }
public class Package { public string version; public string mod_version; public string updated_date; public string build; public string[] supported_builds; public List<Entry> files; }
public static class Engine {
 internal static Func<bool> GameRunning = ()=>Process.GetProcessesByName("Darktide").Length>0;
 public static JavaScriptSerializer Json = new JavaScriptSerializer { MaxJsonLength = 100000000 };
 public static string Sha(byte[] bytes) { using(var h=SHA256.Create()) return BitConverter.ToString(h.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant(); }
 public static string Hash(string p) { return Sha(File.ReadAllBytes(p)); }
 public static T Read<T>(string p) { return Json.Deserialize<T>(File.ReadAllText(p,Encoding.UTF8)); }
 public static void Save(string p,object o) { Atomic(p,Encoding.UTF8.GetBytes(Json.Serialize(o))); }
 public static string Safe(string root,string rel) {
  if(String.IsNullOrWhiteSpace(rel)||Path.IsPathRooted(rel)||rel.Contains(":")||rel.Split('/','\\').Any(x=>x==".."||x=="."||x==""||x.EndsWith(".")||x.EndsWith(" "))) throw new Exception("Unsafe relative path: "+rel);
  string r=Path.GetFullPath(root).TrimEnd('\\')+"\\";
  string p=Path.GetFullPath(Path.Combine(r,rel.Replace('/',Path.DirectorySeparatorChar)));
  if(!p.StartsWith(r,StringComparison.OrdinalIgnoreCase)) throw new Exception("Path escapes root");
  string probe=Path.GetFullPath(root);
  if(Directory.Exists(probe)&&(File.GetAttributes(probe)&FileAttributes.ReparsePoint)!=0) throw new Exception("Linked root is not supported");
  foreach(string part in rel.Split('/','\\')) { probe=Path.Combine(probe,part); if((Directory.Exists(probe)||File.Exists(probe))&&(File.GetAttributes(probe)&FileAttributes.ReparsePoint)!=0) throw new Exception("Linked path is not supported: "+probe); }
  return p;
 }
 public static void Atomic(string p,byte[] bytes) {
  if(p.Length>245)throw new Exception("เส้นทางยาวเกินรุ่นนี้รองรับ กรุณาใช้โฟลเดอร์ที่สั้นลง: "+p);
  Directory.CreateDirectory(Path.GetDirectoryName(p)); string temp=Path.Combine(Path.GetDirectoryName(p),".dt-"+Guid.NewGuid().ToString("N").Substring(0,8)+".tmp");
  using(var fs=new FileStream(temp,FileMode.CreateNew,FileAccess.Write,FileShare.None)){fs.Write(bytes,0,bytes.Length);fs.Flush(true);}
  if(File.Exists(p)) File.Replace(temp,p,null); else File.Move(temp,p);
 }
 public static bool Owned(string p) {
  if(p=="binaries/mod_loader"||p=="bundle/9ba626afa44a3aa3.patch_999"||p=="tools/dtkit-patch.exe")return true;
  string[] roots={"mods/base/","mods/dmf/","mods/SimpleAssets/","mods/WhatTheLocalization/","mods/thai_localization/"};
  return roots.Any(root=>p.StartsWith(root,StringComparison.Ordinal)) && !p.EndsWith(".exe",StringComparison.OrdinalIgnoreCase);
 }

 public static Package ValidatePackage(string package) {
  Package m=Read<Package>(Path.Combine(package,"package.json"));
  if(m==null||m.files==null||m.files.Count==0||m.files.Count>1000)throw new Exception("Invalid package manifest");
  var unique=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
  foreach(var e in m.files){if(!Owned(e.path)||!unique.Add(e.path)||e.hash==null||e.hash.Length!=64)throw new Exception("Invalid package entry"); if(Hash(Safe(Path.Combine(package,"payload"),e.path))!=e.hash)throw new Exception("Package hash mismatch: "+e.path);}
  return m;
 }
 public static string Build(string game) {
  string acf=Path.GetFullPath(Path.Combine(game,"..","..","appmanifest_1361210.acf"));
  if(!File.Exists(acf))return "unknown";var m=Regex.Match(File.ReadAllText(acf),"\"buildid\"\\s+\"(\\d+)\"");return m.Success?m.Groups[1].Value:"unknown";
 }
 public static string BuildWarning(string game,Package package) {
  string build=Build(game);
  string[] supported=package.supported_builds;
  if(supported==null||supported.Length==0) supported=new[]{package.build};
  if(supported.Contains(build))return null;
  return "เกม build "+(build=="unknown"?"อ่านเลขรุ่นไม่ได้":build)+" ยังไม่ได้ทดสอบกับแพ็กนี้\r\nรุ่นที่ทดสอบแล้ว: "+String.Join(", ",supported)+"\r\n\r\nตรวจไฟล์ส่วนประกอบและทดลอง Patch กับสำเนาฐานข้อมูลผ่านแล้ว\r\nการตรวจนี้ยังไม่ยืนยันว่า Loader ม็อด และฟอนต์จะทำงานในเกมรุ่นนี้\r\nคำแปลที่เกมเพิ่มหรือเปลี่ยนอาจยังแสดงอังกฤษ\r\n\r\nต้องการดำเนินการ Patch ต่อหรือไม่?";
 }
 public static void CheckGame(string game,bool mutation) {
  if(!File.Exists(Safe(game,"binaries/Darktide.exe"))||!Directory.Exists(Safe(game,"bundle"))) throw new Exception("เลือกโฟลเดอร์เกม Darktide ที่มี binaries/Darktide.exe และ bundle");
  if(mutation && GameRunning()) throw new Exception("กรุณาปิดเกมก่อนดำเนินการ");
 }
 public static string[] Requirements={"binaries/mod_loader","tools/dtkit-patch.exe","mods/base/mod_manager.lua","mods/dmf/dmf.mod","mods/SimpleAssets/SimpleAssets.mod","mods/WhatTheLocalization/WhatTheLocalization.mod"};
 public static List<string> Missing(string game){return Requirements.Where(p=>!File.Exists(Safe(game,p))).ToList();}
 public static byte[] LoadOrder(string game) {
  string p=Safe(game,"mods/mod_load_order.txt");string old=File.Exists(p)?File.ReadAllText(p,Encoding.UTF8):"";
  string[] required={"SimpleAssets","WhatTheLocalization","thai_localization"};
  var rest=Regex.Split(old,"\r?\n").Where(l=>!required.Contains(l.Trim())).ToList();
  return new UTF8Encoding(false).GetBytes(String.Join("\r\n",required)+"\r\n"+String.Join("\r\n",rest));
 }
}
public static class PatchEngine {
 public static string PatcherHash="6e2a6f04d6a7328ff9ae28b00fb281b4fd5d2bcb053a67bd31d8da78b679e48b";
 public static string[] LoaderFiles={"binaries/mod_loader","tools/dtkit-patch.exe","bundle/9ba626afa44a3aa3.patch_999","mods/base/mod_manager.lua","mods/dmf/dmf.mod","bundle/bundle_database.data"};
 public static bool Patched(byte[] data){return Encoding.ASCII.GetString(data).Contains("patch_999");}
 public static void VerifyPatcher(string exe){if(Engine.Hash(exe)!=PatcherHash)throw new Exception("รุ่น patcher ต่างจากรุ่นที่ทดสอบ กรุณาตรวจรุ่นก่อนใช้งาน");}
 public static string Status(string game){Engine.CheckGame(game,false);string state=Patched(File.ReadAllBytes(Engine.Safe(game,"bundle/bundle_database.data")))?"เปิดม็อดแล้ว":"ยังไม่ Patch";var missing=LoaderFiles.Where(p=>!File.Exists(Engine.Safe(game,p))).ToList();return missing.Count>0?"ไฟล์ Loader ไม่ครบ: "+String.Join(", ",missing):state;}
 public static byte[] StagePatch(string patcher,byte[] original,string tempRoot){return StagePatch(patcher,original,tempRoot,false);}
 public static byte[] StagePatch(string patcher,byte[] original,string tempRoot,bool verifyRoundTrip){
  VerifyPatcher(patcher);if(Patched(original)&&!verifyRoundTrip)return original;
  string dir=Path.Combine(tempRoot,"dtp-"+Guid.NewGuid().ToString("N").Substring(0,10));Directory.CreateDirectory(dir);string db=Path.Combine(dir,"bundle_database.data"),bak=Path.Combine(dir,"bundle_database.data.bak");
  try {
   File.WriteAllBytes(db,original);
   bool wasPatched=Patched(original);
   byte[] expected=null;int offset=-1;
   if(verifyRoundTrip){
    // dtkit-patch replaces one 84-byte boot entry; validate its actual bundled output.
    // https://github.com/ManShanko/dtkit-patch/blob/master/src/main.rs
    byte[] probe=new byte[128];Array.Copy(new byte[]{0xa3,0x3a,0x4a,0xa4,0xaf,0x26,0xa6,0x9b},0,probe,20,8);
    byte[] sample=StagePatch(patcher,probe,tempRoot,false);
    byte[] block=sample.Skip(20).Take(sample.Length-44).ToArray();
    if(Encoding.ASCII.GetString(original).Contains("9ba626afa44a3aa3.patch_001"))throw new Exception("โครงสร้างฐานข้อมูลเกมเปลี่ยน: พบ boot bundle รุ่นที่ Loader นี้ยังไม่รองรับ");
    if(wasPatched){
     if(FindUnique(original,block)<0)throw new Exception("โครงสร้าง Loader ที่ติดตั้งอยู่ไม่ตรงกับรุ่นที่ตรวจสอบได้");
     expected=original;
    }else{
     offset=FindUnique(original,new byte[]{0xa3,0x3a,0x4a,0xa4,0xaf,0x26,0xa6,0x9b});
     if(offset<0||offset+84>original.Length)throw new Exception("โครงสร้างฐานข้อมูลไม่รองรับ: ไม่พบตำแหน่ง Loader ที่ตรวจสอบได้เพียงตำแหน่งเดียว");
     expected=original.Take(offset).Concat(block).Concat(original.Skip(offset+84)).ToArray();
    }
   }
   RunPatcher(patcher,"--patch",dir);
   byte[] result=File.ReadAllBytes(db);if(!Patched(result))throw new Exception("ไม่พบ patch marker หลัง patch");
   if(verifyRoundTrip){
    if(!expected.SequenceEqual(result))throw new Exception("โครงสร้างฐานข้อมูลไม่รองรับ: สำเนาเปลี่ยนนอกตำแหน่ง Loader ที่ตรวจสอบไว้");
    if(!wasPatched){RunPatcher(patcher,"--unpatch",dir);if(!original.SequenceEqual(File.ReadAllBytes(db)))throw new Exception("ผลทดสอบสำเนาไม่ตรงกับต้นฉบับ");}
   }
   return result;
  } finally {if(File.Exists(bak))File.Delete(bak);if(File.Exists(db))File.Delete(db);if(Directory.Exists(dir))Directory.Delete(dir,false);}
 }
 static int FindUnique(byte[] data,byte[] value){
  if(value.Length==0)return -1;int found=-1;
  for(int i=0;i<=data.Length-value.Length;i++){bool equal=true;for(int j=0;j<value.Length;j++)if(data[i+j]!=value[j]){equal=false;break;}if(equal){if(found>=0)return -1;found=i;}}
  return found;
 }
 static void RunPatcher(string patcher,string option,string dir){
  var psi=new ProcessStartInfo(patcher,option+" \""+dir+"\""){UseShellExecute=false,CreateNoWindow=true,RedirectStandardError=true,RedirectStandardOutput=true,WorkingDirectory=dir};
  using(var p=Process.Start(psi)){var output=p.StandardOutput.ReadToEndAsync();var errors=p.StandardError.ReadToEndAsync();if(!p.WaitForExit(30000)){p.Kill();p.WaitForExit();throw new Exception("Patcher ใช้เวลานานเกินกำหนด");}System.Threading.Tasks.Task.WaitAll(output,errors);if(p.ExitCode!=0)throw new Exception("โครงสร้างฐานข้อมูลไม่รองรับ หรือ Patcher ทำงานไม่สำเร็จ: "+errors.Result+output.Result);}
 }
 static void VerifyDependencies(string game,Package manifest,Dictionary<string,byte[]> plan){
  foreach(var e in manifest.files.Where(e=>!e.path.StartsWith("mods/thai_localization/",StringComparison.Ordinal))){
   string path=Engine.Safe(game,e.path);
   if((!plan.ContainsKey(e.path)&&!File.Exists(path))||(File.Exists(path)&&Engine.Hash(path)!=e.hash))throw new Exception("ยังตรวจความเข้ากันได้กับ build ใหม่นี้ไม่ได้: ส่วนประกอบขาดหรือเป็นคนละรุ่น "+e.path+"\r\nกรุณาติดตั้งส่วนประกอบรุ่นที่ตรงกันก่อน ยังไม่ได้แก้ไฟล์เกม");
  }
 }
 public static string Apply(string game,string package,bool update,string tempRoot){return Apply(game,package,update,tempRoot,Categories.Keys);}
 public static string Apply(string game,string package,bool update,string tempRoot,string[] selected){return Apply(game,package,update,tempRoot,selected,null);}
 public static string Apply(string game,string package,bool update,string tempRoot,string[] selected,Func<string,bool> confirmUnknownBuild){
  Engine.CheckGame(game,true);Categories.Validate(selected);
  var plan=new Dictionary<string,byte[]>();Package m=Engine.ValidatePackage(package);string patcher;string build=Engine.Build(game);string warning=Engine.BuildWarning(game,m);
  if(update){
   foreach(var e in m.files){byte[] data=File.ReadAllBytes(Engine.Safe(Path.Combine(package,"payload"),e.path));string target=Engine.Safe(game,e.path);
    bool isThai=e.path.StartsWith("mods/thai_localization/",StringComparison.Ordinal);
    if(!isThai&&File.Exists(target)&&Engine.Hash(target)!=e.hash)throw new Exception("มีส่วนประกอบต่างรุ่นอยู่แล้ว ไม่เขียนทับ: "+e.path);
    plan.Add(e.path,data);
   }
   patcher=Engine.Safe(Path.Combine(package,"payload"),"tools/dtkit-patch.exe");
  }else{
   foreach(string rel in LoaderFiles)if(!File.Exists(Engine.Safe(game,rel)))throw new Exception("เครื่องนี้ยังติดตั้งไม่ครบ ให้เลือกติดตั้งครบชุดก่อน: "+rel);
   // Updating translations is part of every Patch; the checkbox controls dependency installation.
   foreach(var e in m.files.Where(e=>e.path.StartsWith("mods/thai_localization/",StringComparison.Ordinal))){
    plan.Add(e.path,File.ReadAllBytes(Engine.Safe(Path.Combine(package,"payload"),e.path)));
   }
   patcher=Engine.Safe(game,"tools/dtkit-patch.exe");
  }
  VerifyPatcher(patcher);
  string db=Engine.Safe(game,"bundle/bundle_database.data");byte[] before=File.ReadAllBytes(db);bool already=Patched(before);
  string runtimePath=Engine.Safe(game,Categories.RuntimePath);byte[] runtime=plan.ContainsKey(Categories.RuntimePath)?plan[Categories.RuntimePath]:File.Exists(runtimePath)?File.ReadAllBytes(runtimePath):null;
  if(runtime==null)throw new Exception("ยังไม่มีม็อดไทย ให้เลือกติดตั้งครบชุด");
  foreach(string key in selected){string rel="mods/thai_localization/scripts/mods/thai_localization/translations/"+key+".lua";if(!plan.ContainsKey(rel)&&!File.Exists(Engine.Safe(game,rel)))throw new Exception("ขาดคำแปล: "+key);}
  plan[Categories.RuntimePath]=Categories.Runtime(runtime);plan[Categories.ConfigPath]=Categories.Config(selected);plan["mods/mod_load_order.txt"]=Engine.LoadOrder(game);
  string[] checks={"binaries/mod_loader","tools/dtkit-patch.exe","bundle/9ba626afa44a3aa3.patch_999","mods/base/mod_manager.lua","mods/dmf/dmf.mod","mods/SimpleAssets/SimpleAssets.mod","mods/SimpleAssets/bin/simple-assets-runtime.dll","mods/WhatTheLocalization/WhatTheLocalization.mod","mods/thai_localization/assets/fonts/thai_test.slug"};
  foreach(string rel in checks)if(!plan.ContainsKey(rel)&&!File.Exists(Engine.Safe(game,rel)))throw new Exception("แพ็กยังไม่ครบ: "+rel);
  foreach(var e in plan){string target=Engine.Safe(game,e.Key);if(target.Length>245)throw new Exception("เส้นทางยาวเกินรองรับ กรุณาเลือกตำแหน่งเกมที่สั้นลง");}
  if(warning!=null)VerifyDependencies(game,m,plan);
  byte[] patched=StagePatch(patcher,before,tempRoot,warning!=null);
  if(warning!=null&&(confirmUnknownBuild==null||!confirmUnknownBuild(warning)))throw new OperationCanceledException("ยกเลิก Patch สำหรับ build ที่ยังไม่ได้ทดสอบ ยังไม่ได้แก้ไฟล์เกม");
  Engine.CheckGame(game,true);
  if(Engine.Build(game)!=build)throw new Exception("Steam เปลี่ยนรุ่นเกมระหว่างตรวจ ให้รออัปเดตจบแล้วเริ่มใหม่ ยังไม่ได้แก้ไฟล์เกม");
  if(warning!=null)VerifyDependencies(game,m,plan);
  if(Engine.Hash(db)!=Engine.Sha(before))throw new Exception("Steam เปลี่ยนฐานข้อมูลเกมระหว่างติดตั้ง ให้รออัปเดตจบก่อน");
  foreach(var e in plan){string p=Engine.Safe(game,e.Key);if(!File.Exists(p)||Engine.Hash(p)!=Engine.Sha(e.Value))Engine.Atomic(p,e.Value);if(Engine.Hash(p)!=Engine.Sha(e.Value))throw new Exception("ตรวจไฟล์หลังเขียนไม่ผ่าน: "+e.Key);}
  if(!already)Engine.Atomic(db,patched);if(!Patched(File.ReadAllBytes(db)))throw new Exception("ตรวจผล Patch ไม่ผ่าน");
  foreach(string rel in checks)if(!File.Exists(Engine.Safe(game,rel)))throw new Exception("ตรวจส่วนประกอบหลังติดตั้งไม่ผ่าน: "+rel);
  return (warning==null?"":"คำเตือน: build "+build+" ยังไม่ได้ทดสอบการทำงานในเกม\r\n")+"อัปเดตคำแปลและ Patch สำเร็จ — เปิดคำแปล "+selected.Length+" / "+Categories.Keys.Length+" หมวด\r\nรวม Loader, DMF, SimpleAssets, WhatTheLocalization และฟอนต์ไทย\r\nกรุณาเปิดเกมตรวจการแสดงผลภาษาไทย\r\nไม่มี Backup ถาวร";
 }

}
public static class Categories {
 public static readonly string[] Keys={"weapon_lore","blessings","item_lore","item_weapon_descriptions","talents","abilities","missions","circumstances","achievements_descriptions","story_events","subtitles"};
 public static readonly string[] Labels={"Weapon Lore","Blessings","Item Lore","Item / Weapon Descriptions","Talents","Abilities","Missions","Circumstances","Achievements","Story Events & Loading","Subtitles"};
 public static readonly string[][] Groups={new[]{"weapon_lore","blessings","item_lore","item_weapon_descriptions"},new[]{"talents","abilities"},new[]{"missions","circumstances","achievements_descriptions","story_events","subtitles"}};
 public const string ConfigPath="mods/thai_localization/scripts/mods/thai_localization/translation_categories.lua";
 public const string RuntimePath="mods/thai_localization/scripts/mods/thai_localization/thai_localization.lua";
 public const string Marker="-- NEXara category selection v1";
 public static void Validate(string[] selected){if(selected==null||selected.Distinct().Count()!=selected.Length||selected.Any(k=>!Keys.Contains(k)))throw new Exception("Invalid translation category selection");}
 public static byte[] Config(string[] selected){Validate(selected);return new UTF8Encoding(false).GetBytes(Marker+"\nreturn {\n"+String.Join("\n",Keys.Select(k=>"    "+k+" = "+(selected.Contains(k)?"true":"false")+","))+"\n}\n");}
 public static string[] Load(string game){string path=Engine.Safe(game,ConfigPath);if(!File.Exists(path))return Keys.ToArray();string t=File.ReadAllText(path,Encoding.UTF8);if(!t.StartsWith(Marker))throw new Exception("Unsupported category configuration");var values=Regex.Matches(t,"(?m)^    ([a-z_]+) = (true|false),$");bool legacy=values.Count==Keys.Length-1&&!values.Cast<Match>().Any(m=>m.Groups[1].Value=="subtitles");if((values.Count!=Keys.Length&&!legacy)||values.Cast<Match>().Select(m=>m.Groups[1].Value).Distinct().Count()!=values.Count||values.Cast<Match>().Any(m=>!Keys.Contains(m.Groups[1].Value)))throw new Exception("Invalid category configuration");return values.Cast<Match>().Where(m=>m.Groups[2].Value=="true").Select(m=>m.Groups[1].Value).ToArray();}
 public static byte[] Runtime(byte[] bytes){string t=Encoding.UTF8.GetString(bytes);if(t.Contains(Marker)){if(!t.Contains("register_translations(\"subtitles\")"))t+="\nregister_translations(\"subtitles\")\n";return new UTF8Encoding(false).GetBytes(t);}
  const string anchor="local function register_translations(filename)";if(t.Split(new[]{anchor},StringSplitOptions.None).Length!=2)throw new Exception("สคริปต์ม็อดรุ่นนี้ไม่รองรับการเลือกหมวด ให้เลือกอัปเดตไฟล์ภาษาไทยด้วย");
  string newline=t.Contains("\r\n")?"\r\n":"\n";
  string block=Marker+"\nlocal category_selection = {}\nlocal category_ok, category_data = pcall(function()\n    return mod:io_dofile(\"thai_localization/scripts/mods/thai_localization/translation_categories\")\nend)\nif category_ok and type(category_data) == \"table\" then\n    category_selection = category_data\nend\n\n"+anchor+"\n    if category_selection[filename] == false then\n        return\n    end";
  string updated=t.Replace(anchor,block.Replace("\n",newline));if(!updated.Contains("register_translations(\"subtitles\")"))updated+="\nregister_translations(\"subtitles\")\n";return new UTF8Encoding(false).GetBytes(updated);
 }
}
public class CategoryPanel:Panel {
 RadioButton grouped=new RadioButton(),custom=new RadioButton();FlowLayoutPanel options=new FlowLayoutPanel();HashSet<string> selected=new HashSet<string>(Categories.Keys);bool rebuilding;
 public CategoryPanel(){Size=new Size(505,202);grouped.Text="รวมหมวด";grouped.SetBounds(0,0,125,28);grouped.Checked=true;custom.Text="Custom · แยกทั้งหมด";custom.SetBounds(145,0,355,28);Controls.AddRange(new Control[]{grouped,custom});options.SetBounds(0,34,505,164);options.FlowDirection=FlowDirection.TopDown;options.WrapContents=true;Controls.Add(options);grouped.CheckedChanged+=(s,e)=>{if(grouped.Checked)Render();};custom.CheckedChanged+=(s,e)=>{if(custom.Checked)Render();};Render();}
 public string[] Selected(){return Categories.Keys.Where(k=>selected.Contains(k)).ToArray();}
 public void SetSelection(string[] values){Categories.Validate(values);selected=new HashSet<string>(values);Render();}
 public void SetCustom(bool value){custom.Checked=value;grouped.Checked=!value;Render();}

 void Render(){rebuilding=true;options.Controls.Clear();options.AutoScroll=true;options.WrapContents=false;
 if(custom.Checked){
  custom.Text="Custom · แยกทั้งหมด";
  string[] help={"แปลประวัติและเรื่องราวเบื้องหลังอาวุธ","แปลคำอธิบายเอฟเฟกต์ของ Blessings","แปลเรื่องราว ภูมิหลัง และคำอธิบายรูปลักษณ์ไอเทม","แปลโบนัสอุปกรณ์และกลไกอาวุธ","แปลคำอธิบายและผลของ Talent","แปลสกิล บทฝึก และระบบต่อสู้","แปลเป้าหมายและคำอธิบายภารกิจ","แปลเงื่อนไขพิเศษและตัวปรับภารกิจ","แปลคำอธิบายความสำเร็จในเกม","แปลเนื้อเรื่อง กิจกรรม คติหน้าโหลด และข้อความช่วยเหลือ","แปลซับบทสนทนาตัวละครระหว่างเล่น: คุยกับเพื่อน เตือนศัตรู ร้องขอความช่วยเหลือ"};
  int rows=(Categories.Keys.Length+1)/2;
  for(int row=0;row<rows;row++){
   var holder=new Panel{Width=480,Height=79,Margin=new Padding(0)};
   for(int col=0;col<2;col++){int i=row+col*rows;if(i>=Categories.Keys.Length)continue;string key=Categories.Keys[i];
    var c=new CheckBox{UseMnemonic=false,Text=Categories.Labels[i],Location=new Point(col*242,0),Size=new Size(238,26),Checked=selected.Contains(key)};
    c.CheckedChanged+=(s,e)=>{if(!rebuilding){if(c.Checked)selected.Add(key);else selected.Remove(key);}};
    var description=new Label{Text=help[i],Location=new Point(col*242+19,27),Size=new Size(218,48),Font=new Font("Segoe UI",8.5f),ForeColor=Color.DimGray};
    holder.Controls.Add(c);holder.Controls.Add(description);
   }options.Controls.Add(holder);
  }
 }else{
  custom.Text="Custom · แยกทั้งหมด";
  string[] names={"Weapons & Items","Talents & Abilities","Missions & Events"};
  string[] help={"แปลประวัติอาวุธ ไอเทม เอฟเฟกต์ Blessings และโบนัสอุปกรณ์","แปล Talent สกิล ระบบต่อสู้ และบทฝึก","แปลภารกิจ กิจกรรม และซับบทพูดตัวละครระหว่างเล่น\r\nรวมคติหน้าโหลด คุยกับเพื่อน และเตือนศัตรู"};
  for(int i=0;i<Categories.Groups.Length;i++){
   string[] keys=Categories.Groups[i];int count=keys.Count(k=>selected.Contains(k));
   var c=new CheckBox{UseMnemonic=false,Text=names[i],Width=480,Height=25,ThreeState=true,CheckState=count==0?CheckState.Unchecked:count==keys.Length?CheckState.Checked:CheckState.Indeterminate,Margin=new Padding(0)};
   c.Click+=(s,e)=>{bool on=!keys.All(k=>selected.Contains(k));foreach(string k in keys){if(on)selected.Add(k);else selected.Remove(k);}c.CheckState=on?CheckState.Checked:CheckState.Unchecked;};options.Controls.Add(c);
   options.Controls.Add(new Label{Text=help[i],Width=460,Height=i==2?43:20,Font=new Font("Segoe UI",8.5f),ForeColor=Color.DimGray,Margin=new Padding(20,0,0,2)});
  }
 }rebuilding=false;
 }
}

public class MainForm:Form {
 TextBox game=new TextBox(),details=new TextBox();Label state=new Label();Button patch=new Button(),choose=new Button(),expand=new Button(),restore=new Button();CheckBox update=new CheckBox();CategoryPanel categories=new CategoryPanel();string package=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"package");
 public MainForm(){Text="WH40K: Darktide - Mod แปลไทย • Setup 1.0.4";ClientSize=new Size(550,585);FormBorderStyle=FormBorderStyle.FixedSingle;MaximizeBox=false;StartPosition=FormStartPosition.CenterScreen;Font=new Font("Segoe UI",10);BackColor=Color.FromArgb(246,247,249);AutoScaleMode=AutoScaleMode.Dpi;
  Icon=System.Drawing.Icon.ExtractAssociatedIcon(Application.ExecutablePath);
  var badge=new PictureBox{Image=Icon.ToBitmap(),Location=new Point(22,17),Size=new Size(32,32),SizeMode=PictureBoxSizeMode.Zoom};
  var title=new Label{Text="WH40K: Darktide - Mod แปลไทย",Font=new Font("Segoe UI",17,FontStyle.Bold),Location=new Point(64,15),AutoSize=true};
  var subtitle=new Label{Text="ฉบับเผยแพร่ 1.0.4 • คงชื่อและหัวข้อ UI เป็นอังกฤษ",Location=new Point(22,53),AutoSize=true,ForeColor=Color.FromArgb(80,87,98)};
  game.SetBounds(22,85,405,29);choose.Text="เลือกเกม";choose.SetBounds(435,83,92,32);choose.Click+=(s,e)=>Choose();
  var example=new Label{Text="ตัวอย่างโฟลเดอร์เกม:\r\nN:\\SteamLibrary\\steamapps\\common\\Warhammer 40,000 DARKTIDE\r\nเลือกโฟลเดอร์หลักนี้ ไม่ต้องเลือกไฟล์ .exe",Location=new Point(22,121),Size=new Size(510,57),Font=new Font("Segoe UI",8.5f),ForeColor=Color.FromArgb(95,103,115)};
  state.SetBounds(22,186,506,34);state.Text="ยังไม่ได้เลือกโฟลเดอร์เกม";state.ForeColor=Color.FromArgb(65,76,89);
  update.Text="ติดตั้งครบชุด พร้อมตัวโหลดและฟอนต์ไทย (สำหรับติดตั้งครั้งแรก)";update.SetBounds(22,224,495,28);update.Checked=true;
  patch.Text="เริ่มการ Patch";categories.SetBounds(22,258,505,202);patch.SetBounds(22,470,505,43);patch.BackColor=Color.FromArgb(28,97,192);patch.ForeColor=Color.White;patch.FlatStyle=FlatStyle.Flat;patch.Font=new Font("Segoe UI",12,FontStyle.Bold);patch.Click+=async(s,e)=>await Patch();
  expand.Text="รายละเอียด ▾";expand.SetBounds(19,519,125,27);expand.FlatStyle=FlatStyle.Flat;expand.FlatAppearance.BorderSize=0;expand.ForeColor=Color.DimGray;expand.Click+=(s,e)=>{details.Visible=!details.Visible;ClientSize=new Size(550,details.Visible?735:585);expand.Text=details.Visible?"ซ่อนรายละเอียด ▴":"รายละเอียด ▾";};
  var information=new Button{Text="",Image=InformationGlyph.Create(),Name="InformationButton",AccessibleName="ข้อมูลและเครดิต (Information)",AccessibleDescription="เปิดหน้าต่างเครดิตและคำขอบคุณ",Location=new Point(148,519),Size=new Size(28,28),FlatStyle=FlatStyle.Flat,Cursor=Cursors.Hand};information.FlatAppearance.BorderSize=0;information.FlatAppearance.MouseOverBackColor=Color.FromArgb(225,235,248);var informationTip=new ToolTip();informationTip.SetToolTip(information,"ข้อมูลและเครดิต (Information)");FormClosed+=(s,e)=>informationTip.Dispose();information.Disposed+=(s,e)=>information.Image.Dispose();information.Click+=(s,e)=>{using(var f=new InformationForm())f.ShowDialog(this);};
  string modVersion="ไม่ทราบ", updated="ไม่ทราบ";
  try{var meta=Engine.Read<Package>(Path.Combine(package,"package.json"));modVersion=meta.mod_version??meta.version??"ไม่ทราบ";DateTime date;if(DateTime.TryParseExact(meta.updated_date,"yyyy-MM-dd",System.Globalization.CultureInfo.InvariantCulture,System.Globalization.DateTimeStyles.None,out date))updated=date.ToString("dd/MM/yyyy",System.Globalization.CultureInfo.InvariantCulture);}catch{}
  var note=new Label{Text="v"+modVersion+" • อัปเดต "+updated,Location=new Point(321,524),Size=new Size(207,22),Font=new Font("Segoe UI",7.5f),TextAlign=ContentAlignment.MiddleRight,ForeColor=Color.DimGray};
  restore.Text="คืนภาษาอังกฤษ…";restore.SetBounds(181,519,137,28);restore.FlatStyle=FlatStyle.Flat;restore.FlatAppearance.BorderSize=0;restore.ForeColor=Color.FromArgb(28,97,192);restore.Click+=async(s,e)=>await RestoreEnglish();
  var credits=new Label{Text="Made with AI by NEXara (ใช้ AI แปลทั้งหมดเพราะงั้นไม่รับประกันคุณภาพการแปล)",Location=new Point(22,548),Size=new Size(510,34),Font=new Font("Segoe UI",7.5f),ForeColor=Color.DimGray,Anchor=AnchorStyles.Bottom|AnchorStyles.Left};
  details.SetBounds(22,545,505,128);details.Multiline=true;details.ReadOnly=true;details.ScrollBars=ScrollBars.Vertical;details.Visible=false;details.BackColor=Color.White;
  details.Text="ทุกครั้งที่กด Patch: อัปเดตไฟล์คำแปล บันทึกหมวด และเปิด Mod Loader\r\nติดตั้งครบชุด: เพิ่มตัวโหลดและส่วนประกอบสำหรับเครื่องใหม่\r\nSteam ซ่อมไฟล์เกมได้ แต่ไม่กู้ไฟล์ม็อดที่แก้เอง\r\nต้องปิดเกมก่อน Patch";
  Controls.AddRange(new Control[]{badge,title,subtitle,game,choose,example,state,update,categories,patch,expand,information,restore,note,credits,details});
  game.Leave+=(s,e)=>RefreshState();
  string settings=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"DarktideThaiManager","game-path.txt");try{if(File.Exists(settings))game.Text=File.ReadAllText(settings);}catch{}if(game.Text!="")RefreshState();
 }
 void RefreshState(){try{Engine.CheckGame(game.Text,false);state.Text=(File.Exists(Engine.Safe(game.Text,Categories.RuntimePath))?"พบม็อดไทย — พร้อมตรวจและอัปเดต":"เครื่องใหม่ — พร้อมติดตั้งส่วนประกอบทั้งหมด")+"\r\nBuild เกม: "+(Engine.Build(game.Text)=="unknown"?"อ่านไม่ได้":Engine.Build(game.Text))+" • ทดสอบเดิม: 26101089";categories.SetSelection(Categories.Load(game.Text));state.ForeColor=Color.FromArgb(37,113,72);}catch(Exception e){state.Text="เลือกโฟลเดอร์เกมเพื่อเริ่ม";details.Text=e.Message;state.ForeColor=Color.DimGray;}}
 async System.Threading.Tasks.Task RestoreEnglish(){
  string[] selected;
  try{Engine.CheckGame(game.Text,true);using(var dialog=new RestoreEnglishForm()){if(dialog.ShowDialog(this)!=DialogResult.OK)return;selected=dialog.Selected();}}
  catch(Exception e){MessageBox.Show(this,e.Message,"คืนภาษาอังกฤษไม่ได้",MessageBoxButtons.OK,MessageBoxIcon.Warning);return;}
  patch.Enabled=choose.Enabled=restore.Enabled=update.Enabled=game.Enabled=categories.Enabled=false;
  state.Text="กำลังคืนภาษาอังกฤษ…";
  try{
   string path=game.Text;
   string[] enabled=await System.Threading.Tasks.Task.Run(()=>EnglishRestore.Apply(path,selected));
   categories.SetSelection(enabled);state.Text="คืนภาษาอังกฤษสำเร็จ — เปิดเกมใหม่เพื่อใช้การตั้งค่า";state.ForeColor=Color.FromArgb(37,113,72);
   details.Text="คืนภาษาอังกฤษ: "+String.Join(", ",selected.Select(k=>Categories.Labels[Array.IndexOf(Categories.Keys,k)]))+"\r\nหมวดอื่นคงการตั้งค่าเดิม สามารถติ๊กกลับแล้วกดเริ่มการ Patch เพื่อใช้ไทยอีกครั้ง";
   MessageBox.Show(this,"คืนภาษาอังกฤษ "+selected.Length+" หมวดแล้ว\r\nเปิดเกมใหม่เพื่อใช้การตั้งค่า หมวดอื่นยังคงการตั้งค่าเดิม", "คืนภาษาอังกฤษสำเร็จ",MessageBoxButtons.OK,MessageBoxIcon.Information);
  }catch(Exception e){state.Text="คืนภาษาอังกฤษไม่สำเร็จ — ดูรายละเอียด";state.ForeColor=Color.Firebrick;details.Text=e.Message;MessageBox.Show(this,e.Message,"คืนภาษาอังกฤษไม่สำเร็จ",MessageBoxButtons.OK,MessageBoxIcon.Error);}
  finally{patch.Enabled=choose.Enabled=restore.Enabled=update.Enabled=game.Enabled=categories.Enabled=true;}
 }
 void Choose(){using(var f=new FolderBrowserDialog{Description="เลือกโฟลเดอร์เกม Darktide"})if(f.ShowDialog()==DialogResult.OK){game.Text=f.SelectedPath;RefreshState();}}
 async System.Threading.Tasks.Task Patch(){
  patch.Enabled=choose.Enabled=restore.Enabled=update.Enabled=game.Enabled=categories.Enabled=false;
  patch.Text="กำลัง Patch…";state.Text="กำลัง Patch…";state.ForeColor=Color.FromArgb(65,76,89);
  try{
   string path=game.Text;bool upd=update.Checked;string[] selected=categories.Selected();
   string result=await System.Threading.Tasks.Task.Run(()=>PatchEngine.Apply(path,package,upd,Path.GetTempPath(),selected,warning=>(bool)Invoke(new Func<bool>(()=>MessageBox.Show(this,warning,"เกมรุ่นที่ยังไม่ได้ทดสอบ",MessageBoxButtons.YesNo,MessageBoxIcon.Warning,MessageBoxDefaultButton.Button2)==DialogResult.Yes))));
   bool untested=result.StartsWith("คำเตือน:");
   state.Text=untested?"Patch สำเร็จ — build นี้ยังไม่ได้ทดสอบในเกม":"Patch สำเร็จ — เปิดเกมเพื่อตรวจภาษาไทยได้เลย";
   details.Text=result;state.ForeColor=untested?Color.DarkGoldenrod:Color.FromArgb(37,113,72);
   try{
    string settings=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"DarktideThaiManager","game-path.txt");
    Directory.CreateDirectory(Path.GetDirectoryName(settings));File.WriteAllText(settings,path);
   }catch{}
   patch.Text="เริ่มการ Patch";
   MessageBox.Show(this,"Patch สำเร็จ\r\n"+(untested?result.Split(new[]{"\r\n"},StringSplitOptions.None)[0]+"\r\n":"")+"\r\nเปิดคำแปล "+selected.Length+" / "+Categories.Keys.Length+" หมวด\r\nเปิดเกมเพื่อตรวจการแสดงผลภาษาไทยได้เลย", "Patch สำเร็จ", MessageBoxButtons.OK, MessageBoxIcon.Information);
  }catch(OperationCanceledException e){
   state.Text="ยกเลิก Patch — ยังไม่ได้แก้ไฟล์เกม";state.ForeColor=Color.DimGray;details.Text=e.Message;
  }catch(Exception e){
   state.Text="Patch ล้มเหลว — ดูสาเหตุในรายละเอียด";state.ForeColor=Color.Firebrick;
   details.Text=e.Message;details.Visible=true;ClientSize=new Size(550,735);expand.Text="ซ่อนรายละเอียด ▴";
   patch.Text="เริ่มการ Patch";
   MessageBox.Show(this,"Patch ล้มเหลว\r\n\r\n"+e.Message+"\r\n\r\nอ่านสาเหตุเพิ่มเติมได้ในช่องรายละเอียด", "Patch ล้มเหลว", MessageBoxButtons.OK, MessageBoxIcon.Error);
  }finally{
   patch.Text="เริ่มการ Patch";
   patch.Enabled=choose.Enabled=restore.Enabled=update.Enabled=game.Enabled=categories.Enabled=true;
  }
 }

}
public static class InformationGlyph {
 public static Bitmap Create(){var bitmap=new Bitmap(24,24);using(var g=Graphics.FromImage(bitmap))using(var pen=new Pen(Color.FromArgb(28,97,192),1.6f))using(var brush=new SolidBrush(Color.FromArgb(28,97,192)))using(var font=new Font("Segoe UI",11,FontStyle.Bold))using(var format=new StringFormat{Alignment=StringAlignment.Center,LineAlignment=StringAlignment.Center}){g.Clear(Color.Transparent);g.SmoothingMode=System.Drawing.Drawing2D.SmoothingMode.AntiAlias;g.DrawEllipse(pen,2.5f,2.5f,19,19);g.DrawString("i",font,brush,new RectangleF(0,0,24,24),format);}return bitmap;}
}
public class InformationForm:Form {
 public InformationForm(){Text="Information · เครดิตและคำขอบคุณ";ClientSize=new Size(650,610);MinimumSize=new Size(570,450);StartPosition=FormStartPosition.CenterParent;Font=new Font("Segoe UI",10);BackColor=Color.White;Icon=System.Drawing.Icon.ExtractAssociatedIcon(Application.ExecutablePath);
  var header=new Label{Text="เครดิตและคำขอบคุณ\r\nขอบคุณผู้สร้างม็อดและเครื่องมือที่ช่วยให้ชุดแปลไทยนี้ทำงานได้",Dock=DockStyle.Top,Height=66,Padding=new Padding(16,10,10,0),Font=new Font("Segoe UI",10)};
  var list=new FlowLayoutPanel{Dock=DockStyle.Fill,AutoScroll=true,FlowDirection=FlowDirection.TopDown,WrapContents=false,Padding=new Padding(14,4,0,8),BackColor=Color.FromArgb(246,247,249)};
  foreach(var entry in Credits()){
   var card=new Panel{Width=596,Height=125,Margin=new Padding(0,0,0,9),BackColor=Color.White};
   var title=new Label{Text=entry[0],Location=new Point(10,7),Size=new Size(575,23),Font=new Font("Segoe UI",10,FontStyle.Bold)};
   var author=new Label{Text=entry[1],Location=new Point(10,33),Size=new Size(575,20),Font=new Font("Segoe UI",9),ForeColor=Color.FromArgb(28,97,192)};
   var detail=new Label{Text=entry[2],Location=new Point(10,57),Size=new Size(575,38),Font=new Font("Segoe UI",9),ForeColor=Color.FromArgb(65,76,89)};
   var link=new LinkLabel{Text="เปิดแหล่งต้นฉบับ",Location=new Point(10,99),AutoSize=true,Font=new Font("Segoe UI",9)};string url=entry[3];link.LinkClicked+=(s,e)=>{try{Process.Start(new ProcessStartInfo(url){UseShellExecute=true});}catch(Exception error){MessageBox.Show(this,error.Message,"เปิดลิงก์ไม่ได้");}};
   card.Controls.AddRange(new Control[]{title,author,detail});if(!String.IsNullOrEmpty(url))card.Controls.Add(link);list.Controls.Add(card);
  }
  list.Resize+=(s,e)=>{int width=Math.Max(480,list.ClientSize.Width-45);foreach(Control card in list.Controls){card.Width=width;foreach(Control label in card.Controls)if(label is Label&&!(label is LinkLabel))label.Width=width-20;}};
  var bottom=new Panel{Dock=DockStyle.Bottom,Height=55,Padding=new Padding(0,10,14,10)};var note=new Label{Text="เครดิตตามแหล่งที่พบ • เจ้าของเดิมยังคงสิทธิ์ในผลงานของตน",Location=new Point(15,17),AutoSize=true,Font=new Font("Segoe UI",8.5f),ForeColor=Color.DimGray};var close=new Button{Text="ปิด",Dock=DockStyle.Right,Width=80,DialogResult=DialogResult.OK};bottom.Controls.AddRange(new Control[]{note,close});AcceptButton=close;CancelButton=close;
  Controls.Add(list);Controls.Add(header);Controls.Add(bottom);
 }
 public static string[][] Credits(){return new[]{
  new[]{"Darktide Mod Loader (DML)","ทีม Darktide-Mod-Framework และผู้ร่วมพัฒนา","ตัวโหลดม็อด รวม base และไฟล์ที่ทำให้เกมเริ่มโหลดม็อดได้\r\nไม่พบเลขเวอร์ชันที่ยืนยันได้จากไฟล์ในชุดนี้", "https://github.com/Darktide-Mod-Framework/Darktide-Mod-Loader"},
  new[]{"Darktide Mod Framework (DMF)","ทีม Darktide-Mod-Framework และผู้ร่วมพัฒนา","ระบบพื้นฐานสำหรับม็อด เช่น hooks และการตั้งค่า\r\nมีต้นทางจาก Vermintide Mod Framework ตาม README ของโครงการ", "https://github.com/Darktide-Mod-Framework/Darktide-Mod-Framework"},
  new[]{"SimpleAssets · 2.0.0","deluxghost — ยืนยันจาก info.json ในม็อด","ใช้โหลด asset และฟอนต์ไทยเข้าเกม รวม native runtime ที่ม็อดต้องใช้", "https://github.com/deluxghost/darktide-mods/tree/main/SimpleAssets"},
  new[]{"WhatTheLocalization","โครงการ deluxghost/darktide-mods","ระบบลงทะเบียนและนำ templates คำแปลไปแทนข้อความของเกม\r\nอ้างอิงเจ้าของ repository; ไม่ได้ยืนยันรายชื่อผู้เขียนทั้งหมด", "https://github.com/deluxghost/darktide-mods/tree/main/WhatTheLocalization"},
  new[]{"dtkit-patch · 0.1.7","ManShanko / manshanko • ขอบคุณ Aussiemon สำหรับสคริปต์ต้นทาง","เครื่องมือ Patch ฐานข้อมูลเพื่อเปิดตัวโหลดม็อด\r\nเลขรุ่นตรวจจาก --help ของไฟล์ที่บรรจุ", "https://github.com/manshanko/dtkit-patch"},
  new[]{"Noto Sans Thai","โครงการ Noto / notofonts","ชื่อ Noto Sans Thai พบฝังอยู่ใน thai_test.slug\r\nยังไม่ยืนยันผู้แปลงไฟล์ Slug และรุ่นฟอนต์ต้นฉบับที่ใช้", "https://github.com/notofonts/thai"},
  new[]{"LocalizationExport · เครื่องมือผู้แปล","ยังไม่พบข้อมูลผู้สร้างที่ยืนยันได้","ใช้ดึงข้อความอังกฤษมาช่วยทำคำแปล ไม่ได้รวมในชุดติดตั้งของผู้เล่น\r\nยังไม่มีลิงก์แหล่งต้นฉบับที่ยืนยันได้", ""}
 };}
}

public static class Program {
 [STAThread] public static void Main(string[] args){
  if(args.Length==2&&args[0]=="--render-restore"){Application.EnableVisualStyles();using(var f=new RestoreEnglishForm())using(var panel=new Panel{Size=f.ClientSize,BackColor=f.BackColor,Font=f.Font}){foreach(Control c in f.Controls.Cast<Control>().ToArray())panel.Controls.Add(c);panel.CreateControl();foreach(Control c in panel.Controls)c.CreateControl();using(var b=new Bitmap(panel.Width,panel.Height)){panel.DrawToBitmap(b,new Rectangle(Point.Empty,panel.Size));b.Save(args[1],System.Drawing.Imaging.ImageFormat.Png);}}return;}
  if(args.Length==2&&args[0]=="--render-info"){Application.EnableVisualStyles();using(var f=new InformationForm())using(var panel=new Panel{Size=f.ClientSize,BackColor=f.BackColor,Font=f.Font}){foreach(Control c in f.Controls.Cast<Control>().ToArray())panel.Controls.Add(c);panel.CreateControl();panel.PerformLayout();foreach(Control c in panel.Controls)c.CreateControl();using(var b=new Bitmap(panel.Width,panel.Height)){panel.DrawToBitmap(b,new Rectangle(Point.Empty,panel.Size));b.Save(args[1],System.Drawing.Imaging.ImageFormat.Png);}}return;}
  if(args.Length==2&&(args[0]=="--render-preview"||args[0]=="--render-custom")){Application.EnableVisualStyles();using(var f=new MainForm())using(var panel=new Panel{Size=f.ClientSize,BackColor=f.BackColor,Font=f.Font}){if(args[0]=="--render-custom")foreach(var cp in f.Controls.OfType<CategoryPanel>())cp.SetCustom(true);foreach(Control c in f.Controls.Cast<Control>().ToArray())panel.Controls.Add(c);panel.CreateControl();foreach(Control c in panel.Controls)c.CreateControl();using(var b=new Bitmap(panel.Width,panel.Height)){panel.DrawToBitmap(b,new Rectangle(Point.Empty,panel.Size));b.Save(args[1],System.Drawing.Imaging.ImageFormat.Png);}}return;}
  if(args.Length==3&&args[0]=="--legacy-self-test"){try{CompactTests.Run(args[1],args[2]);}catch(Exception e){File.WriteAllText(Path.Combine(args[2],"failure.txt"),e.ToString());Environment.ExitCode=1;}return;}
  if(args.Length==3&&args[0]=="--fresh-test"){try{FreshTests.Run(args[1],args[2]);}catch(Exception e){File.WriteAllText(Path.Combine(args[2],"fresh-failure.txt"),e.ToString());Environment.ExitCode=1;}return;}
  bool first;using(var mutex=new System.Threading.Mutex(true,"Local\\DarktideThaiManager",out first)){if(!first)return;Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);Application.Run(new MainForm());}
 }
}
