using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
public static class CategoryTests {
 static List<string> checks=new List<string>();
 static void Assert(bool value,string name){if(!value)throw new Exception(name);checks.Add(name);}
 public static void Run(string game,string package,string temp,string root){
  Assert(Categories.Groups.SelectMany(g=>g).Distinct().Count()==Categories.Keys.Length&&Categories.Groups.Sum(g=>g.Length)==Categories.Keys.Length,"groups cover all categories exactly once");
  string[] weapons=Categories.Groups[0];PatchEngine.Apply(game,package,false,temp,weapons);Assert(Categories.Load(game).OrderBy(k=>k).SequenceEqual(weapons.OrderBy(k=>k)),"grouped Weapons enables four exact modules");
  string runtime=File.ReadAllText(Engine.Safe(game,Categories.RuntimePath));Assert(runtime.Contains("if category_selection[filename] == false then"),"runtime skips disabled categories before loading translations");
  var single=new[]{"talents"};PatchEngine.Apply(game,package,false,temp,single);Assert(Categories.Load(game).SequenceEqual(single),"Custom Talents only selection round trips");
  Assert(File.Exists(Engine.Safe(game,"mods/thai_localization/scripts/mods/thai_localization/translations/weapon_lore.lua")),"disabled translation file retained");
  PatchEngine.Apply(game,package,false,temp,new string[0]);Assert(Categories.Load(game).Length==0,"empty selection disables all translation modules");
  string rt=File.ReadAllText(Engine.Safe(game,Categories.RuntimePath));PatchEngine.Apply(game,package,false,temp,Categories.Keys);Assert(File.ReadAllText(Engine.Safe(game,Categories.RuntimePath))==rt,"repeated category update does not duplicate runtime injection");
  Assert(Categories.Load(game).Length==Categories.Keys.Length,"all categories can be enabled again");
  bool rejected=false;try{Categories.Config(new[]{"unknown_category"});}catch{rejected=true;}Assert(rejected,"unavailable category rejected");
  string before=Engine.Hash(Engine.Safe(game,Categories.ConfigPath));try{PatchEngine.Apply(game,package,false,temp,new[]{"../escape"});}catch{}Assert(Engine.Hash(Engine.Safe(game,Categories.ConfigPath))==before,"invalid category leaves existing selection unchanged");
  using(var panel=new CategoryPanel()){panel.SetSelection(new[]{"talents","blessings"});panel.SetCustom(true);panel.SetCustom(false);Assert(panel.Selected().SequenceEqual(new[]{"blessings","talents"}),"mode switches preserve partial Custom selections");}
  Engine.Save(Path.Combine(root,"category-test-results.json"),new{passed=checks.Count,tests=checks.ToArray(),limitations="Synthetic game engine tests and in-memory UI state only; Lua runtime not executed in Darktide"});
 }
}
