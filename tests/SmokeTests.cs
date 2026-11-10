using System;
using System.IO;
using System.Linq;
using System.Drawing;
using System.Windows.Forms;
class Tests {
 static int passed;
 static void Check(bool b,string name){if(!b)throw new Exception(name);passed++;Console.WriteLine("PASS: "+name);}
 static Clip Text(string s,int minutes,bool pinned=false){return new Clip{Kind="text",Value=s,Time=new DateTime(2026,11,10,10,0,0).AddMinutes(minutes),Pinned=pinned};}
 [STAThread] static void Main(){string tmp=Path.Combine(Path.GetTempPath(),"JianClip-test-"+Guid.NewGuid());Directory.CreateDirectory(tmp);try{
  var h=new HistoryStore(tmp);h.Add(Text("中文 encryption probe",0,true));h.Save();Check(!System.Text.Encoding.UTF8.GetString(File.ReadAllBytes(Path.Combine(tmp,"history.dat"))).Contains("encryption probe"),"DPAPI encrypted at rest");var loaded=new HistoryStore(tmp);loaded.Load();Check(loaded.Items.Count==1&&loaded.Items[0].Pinned&&loaded.Items[0].Value=="中文 encryption probe","Unicode + favorites encrypted roundtrip");
  h.Add(Text("中文 encryption probe",1));Check(h.Items.Count==1&&h.Items[0].Pinned&&h.Items[0].Time.Minute==1,"Duplicate keeps favorite and updates time");
  h.Items.Clear();for(int i=0;i<200;i++)h.Add(Text("pinned "+i,i,true));Check(!h.Add(Text("overflow",201))&&h.Items.Count==200&&h.Items.All(c=>c.Pinned),"Full favorites rejects incoming clip without data loss");
  h.Items[0].Pinned=false;Check(h.Add(Text("new",202))&&h.Items.Count==200&&!h.Items.Any(c=>c.Value=="pinned 0"),"Capacity evicts oldest non-favorite");
  var d=new DataObject();d.SetData("CanIncludeInClipboardHistory",new MemoryStream(new byte[4]));Check(Native.Excluded(d),"Privacy DWORD stream exclusion");d=new DataObject();d.SetData("ExcludeClipboardContentFromMonitorProcessing","1");Check(Native.Excluded(d),"Password manager exclusion");
  var s=new AppSettings{WaterMinutes=5,EyesMinutes=2,StretchMinutes=3,StretchEnabled=true,PetX=-400,PetY=100};s.Save(tmp);var settings=AppSettings.Load(tmp);Check(settings.WaterMinutes==5&&settings.StretchEnabled&&settings.PetX==-400,"Settings including negative monitor coordinates roundtrip");
  DateTime now=new DateTime(2026,11,10,10,0,0);var clock=new ReminderClock(s,now);Check(clock.Next(s,now.AddMinutes(2),false)=="eyes","Eye reminder schedule");clock.Snooze(now.AddMinutes(2));Check(clock.Next(s,now.AddMinutes(6),false)==null,"10 minute snooze suppresses reminders");Check(clock.Next(s,now.AddMinutes(13),true)==null,"Idle suppresses reminders");Check(clock.Next(s,new DateTime(2026,11,10,23,0,0),false)==null,"Night quiet hours suppress reminders");var snoozeClock=new ReminderClock(s,now);snoozeClock.Next(s,now.AddMinutes(5),false);snoozeClock.Snooze(now.AddMinutes(5),"water");Check(snoozeClock.Next(s,now.AddMinutes(14),false)==null,"Snoozed water waits full 10 minutes");Check(snoozeClock.Next(s,now.AddMinutes(15),false)=="water","Same reminder returns after snooze");s.RemindersEnabled=false;Check(clock.Next(s,now.AddMinutes(15),false)==null,"Disabled reminders stay silent");
  foreach(string pose in new[]{"idle","wave","happy","water","eyes","stretch","sleep"})using(var b=KangarooArt.Preview(pose,260,290)){Check(b.GetPixel(0,0).A==0&&b.GetPixel(130,220).A>0,"Transparent artwork renders "+pose);}
  passed += KnowledgeTests.Run(tmp);
  Console.WriteLine("ALL "+passed+" CHECKS PASSED");
 }finally{Directory.Delete(tmp,true);}}
}
