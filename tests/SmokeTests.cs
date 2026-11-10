using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using System.Windows.Forms;
class Tests {
 [STAThread] static void Main(){
 var tmp=Path.Combine(Path.GetTempPath(),"JianClip-test-"+Guid.NewGuid());Directory.CreateDirectory(tmp);
 try{using(var f=new ClipForm()){
 var flags=BindingFlags.NonPublic|BindingFlags.Instance;
 typeof(ClipForm).GetField("folder",flags).SetValue(f,tmp);
 var items=(List<Clip>)typeof(ClipForm).GetField("clips",flags).GetValue(f);items.Clear();
 items.Add(new Clip{Kind="text",Value="测试文本 encryption probe",Time=DateTime.Now,Pinned=true});
 typeof(ClipForm).GetMethod("Save",flags).Invoke(f,null);
 var bytes=File.ReadAllBytes(Path.Combine(tmp,"history.dat"));if(System.Text.Encoding.UTF8.GetString(bytes).Contains("encryption probe"))throw new Exception("Plaintext persisted");
 items.Clear();typeof(ClipForm).GetMethod("LoadHistory",flags).Invoke(f,null);
 items=(List<Clip>)typeof(ClipForm).GetField("clips",flags).GetValue(f);if(items.Count!=1||!items[0].Pinned||items[0].Value!="测试文本 encryption probe")throw new Exception("Roundtrip failed");
 var d=new DataObject();d.SetData("CanIncludeInClipboardHistory",new MemoryStream(new byte[4]));if(!(bool)typeof(ClipForm).GetMethod("Excluded",flags).Invoke(f,new object[]{d}))throw new Exception("Privacy flag failed");
 Console.WriteLine("PASS: encrypted history roundtrip, Unicode, favorites, clipboard exclusion flag");
 }}finally{Directory.Delete(tmp,true);}
 }
}
