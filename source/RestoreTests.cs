using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Windows.Forms;
public static class RestoreTests {
 public static void Run(string game,string package,string temp,Action<bool,string> check,Action<Action,string> reject){
  string cfg=Engine.Safe(game,Categories.ConfigPath),runtime=Engine.Safe(game,Categories.RuntimePath);
  var snapshot=Directory.GetFiles(game,"*",SearchOption.AllDirectories).ToDictionary(p=>Path.GetFullPath(p),p=>Engine.Hash(p));
  string[] before=Categories.Load(game);
  var enabled=EnglishRestore.Apply(game,new[]{"subtitles"});
  check(!enabled.Contains("subtitles")&&enabled.SequenceEqual(before.Except(new[]{"subtitles"})),"restore Subtitles disables only subtitle translations");
  check(snapshot.All(e=>e.Key==cfg||File.Exists(e.Key)&&Engine.Hash(e.Key)==e.Value),"restore changes no database loader fonts translation payload or unrelated mod file");
  check(Directory.GetFiles(game,"*",SearchOption.AllDirectories).Length==snapshot.Count,"restore neither removes nor adds game or mod payload files");
  byte[] one=File.ReadAllBytes(cfg);EnglishRestore.Apply(game,new[]{"subtitles"});
  check(one.SequenceEqual(File.ReadAllBytes(cfg)),"restoring an already-English category is idempotent");
  EnglishRestore.Apply(game,new[]{"talents","abilities"});
  check(Categories.Load(game).SequenceEqual(before.Except(new[]{"subtitles","talents","abilities"})),"restore grouped categories preserves every unselected category");
  EnglishRestore.Apply(game,Categories.Keys);
  check(Categories.Load(game).Length==0,"restore all categories leaves no Thai translation category enabled");
  one=File.ReadAllBytes(cfg);
  reject(()=>EnglishRestore.Apply(game,new string[0]),"empty restore selection rejected");
  reject(()=>EnglishRestore.Apply(game,new[]{"not_a_category"}),"unknown restore category rejected");
  reject(()=>EnglishRestore.Apply(game,new[]{"subtitles","subtitles"}),"duplicate restore selection rejected");
  Engine.GameRunning=()=>true;try{reject(()=>EnglishRestore.Apply(game,new[]{"subtitles"}),"restore while game is running rejected");}finally{Engine.GameRunning=()=>false;}
  check(one.SequenceEqual(File.ReadAllBytes(cfg)),"invalid requests and running-game rejection preserve category configuration");
  byte[] rt=File.ReadAllBytes(runtime);Engine.Atomic(runtime,System.Text.Encoding.UTF8.GetBytes("return {} -- legacy"));
  try{reject(()=>EnglishRestore.Apply(game,new[]{"subtitles"}),"legacy runtime without category gating rejected");}finally{Engine.Atomic(runtime,rt);}
  check(one.SequenceEqual(File.ReadAllBytes(cfg)),"legacy-runtime rejection preserves category configuration");
  Engine.Atomic(cfg,System.Text.Encoding.UTF8.GetBytes("unsupported user configuration"));
  try{reject(()=>EnglishRestore.Apply(game,new[]{"subtitles"}),"unsupported category configuration rejected without replacement");check(File.ReadAllText(cfg)=="unsupported user configuration","unrecognized user configuration remains untouched");}finally{Engine.Atomic(cfg,one);}
  string acf=Path.GetFullPath(Path.Combine(game,"..","..","appmanifest_1361210.acf"));byte[] acfBytes=File.ReadAllBytes(acf);File.WriteAllText(acf,"\"buildid\" \"99999999\"");
  try{EnglishRestore.Apply(game,new[]{"subtitles"});check(one.SequenceEqual(File.ReadAllBytes(cfg)),"restoration works on an untested build without patching its database");}finally{Engine.Atomic(acf,acfBytes);}
  PatchEngine.Apply(game,package,false,temp,new[]{"talents","subtitles"});
  check(Categories.Load(game).SequenceEqual(new[]{"talents","subtitles"}),"normal Patch can re-enable Thai after English restoration");
  using(var form=new RestoreEnglishForm()){check(form.Selected().Length==0&&((Button)form.CancelButton).DialogResult==DialogResult.Cancel,"restore dialog starts with nothing selected and offers cancellation");}
  check(snapshot.All(e=>e.Key==cfg||File.Exists(e.Key)&&Engine.Hash(e.Key)==e.Value),"restore and re-enable round trip preserve original synthetic game payload");
 }
}
