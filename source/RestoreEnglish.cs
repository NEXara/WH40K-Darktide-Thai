using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Windows.Forms;
public static class EnglishRestore {
 public static string[] Apply(string game,string[] selected){
  Engine.CheckGame(game,true);Categories.Validate(selected);
  if(selected.Length==0)throw new Exception("กรุณาเลือกอย่างน้อยหนึ่งหมวดที่ต้องการคืนภาษาอังกฤษ");
  string runtime=Engine.Safe(game,Categories.RuntimePath),config=Engine.Safe(game,Categories.ConfigPath);
  if(!File.Exists(runtime))throw new Exception("ยังไม่พบม็อดแปลไทย กรุณาติดตั้งก่อนใช้โหมดคืนภาษาอังกฤษ");
  string script=File.ReadAllText(runtime,Encoding.UTF8);
  if(!script.Contains(Categories.Marker)||!script.Contains("local category_selection")||!script.Contains("category_selection[filename] == false"))throw new Exception("ม็อดรุ่นนี้ไม่รองรับการคืนแยกหมวด กรุณาอัปเดตม็อดก่อน");
  string runtimeHash=Engine.Hash(runtime);bool hadConfig=File.Exists(config);string configHash=hadConfig?Engine.Hash(config):null;
  string[] enabled=Categories.Load(game).Except(selected).ToArray();
  Engine.CheckGame(game,true);
  if(Engine.Hash(runtime)!=runtimeHash||File.Exists(config)!=hadConfig||(hadConfig&&Engine.Hash(config)!=configHash))throw new Exception("การตั้งค่าม็อดเปลี่ยนระหว่างตรวจ กรุณาลองอีกครั้ง");
  Engine.Atomic(config,Categories.Config(enabled));return enabled;
 }
}
public class RestoreEnglishForm:Form {
 CategoryPanel categories=new CategoryPanel();
 public string[] Selected(){return categories.Selected();}
 public RestoreEnglishForm(){
  Text="คืนภาษาอังกฤษ • เลือกหมวด";ClientSize=new Size(550,365);Font=new Font("Segoe UI",10);BackColor=Color.FromArgb(246,247,249);FormBorderStyle=FormBorderStyle.FixedDialog;MaximizeBox=false;MinimizeBox=false;StartPosition=FormStartPosition.CenterParent;
  var help=new Label{Text="ติ๊กเฉพาะหมวดที่ต้องการคืนภาษาอังกฤษดั้งเดิม\r\nหมวดที่ไม่ติ๊กจะคงการตั้งค่าเดิม ต้องปิดเกมก่อนทำรายการ",Location=new Point(22,12),Size=new Size(510,45)};
  var all=new Button{Text="เลือกทั้งหมด",Location=new Point(22,62),Size=new Size(110,27)};all.Click+=(s,e)=>categories.SetSelection(Categories.Keys);
  var none=new Button{Text="ไม่เลือกทั้งหมด",Location=new Point(140,62),Size=new Size(125,27)};none.Click+=(s,e)=>categories.SetSelection(new string[0]);
  categories.SetBounds(22,94,505,202);categories.SetCustom(true);categories.SetSelection(new string[0]);
  var apply=new Button{Text="คืนภาษาอังกฤษ",Location=new Point(337,315),Size=new Size(190,34),BackColor=Color.FromArgb(28,97,192),ForeColor=Color.White,FlatStyle=FlatStyle.Flat};
  apply.Click+=(s,e)=>{if(Selected().Length==0){MessageBox.Show(this,"กรุณาเลือกอย่างน้อยหนึ่งหมวด","ยังไม่ได้เลือกหมวด");return;}DialogResult=DialogResult.OK;};
  var cancel=new Button{Text="ยกเลิก",Location=new Point(220,315),Size=new Size(110,34),DialogResult=DialogResult.Cancel};CancelButton=cancel;
  Controls.AddRange(new Control[]{help,all,none,categories,apply,cancel});
 }
}
