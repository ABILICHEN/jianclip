using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
class Integration {
 static ClipForm app; static int stage; static string folder;static IDataObject original;static Timer timer;static Exception failure;
 [STAThread]static int Main(){
  folder=Path.Combine(Path.GetTempPath(),"JianClip-integration-"+Guid.NewGuid());
  Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);
  try{original=Clipboard.GetDataObject();app=new ClipForm(folder);timer=new Timer{Interval=600};timer.Tick+=Step;app.Shown+=delegate{timer.Start();};Application.Run(app);}
  catch(Exception ex){failure=ex;}
  finally{if(timer!=null)timer.Dispose();if(app!=null)app.Dispose();try{if(original!=null)Clipboard.SetDataObject(original,true);else Clipboard.Clear();}catch{Console.WriteLine("Clipboard restore unavailable");}if(Directory.Exists(folder))Directory.Delete(folder,true);}
  if(failure!=null){Console.WriteLine("FAIL: "+failure.Message);return 1;}Console.WriteLine("ALL INTEGRATION CHECKS PASSED");return 0;
 }
 static void Verify(bool condition,string message){if(!condition)throw new Exception(message);Console.WriteLine("PASS: "+message);}
 static void Step(object s,EventArgs e){try{switch(stage++){
  case 0:Clipboard.SetText("JianClip isolated clipboard test");break;
  case 1:Verify(app.History.Items.Count==1&&app.History.Items[0].Value=="JianClip isolated clipboard test","Native clipboard listener captures text");using(var b=new Bitmap(32,32)){using(var g=Graphics.FromImage(b))g.Clear(Color.Gold);Clipboard.SetImage(b);}break;
  case 2:Verify(app.History.Items.Count==2&&app.History.Items[1].Kind=="image","Native clipboard listener captures image");var d=new DataObject();d.SetText("excluded test");d.SetData("CanIncludeInClipboardHistory",new MemoryStream(new byte[4]));Clipboard.SetDataObject(d,true);break;
  case 3:Verify(app.History.Items.Count==2,"Excluded clipboard does not enter history");app.PasteClip(app.History.Items[0],false);break;
  case 4:Verify(app.History.Items.Count==2,"Own copy does not recapture");var pause=(CheckBox)typeof(ClipForm).GetField("pause",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(app);pause.Checked=true;Clipboard.SetText("paused test");break;
  case 5:Verify(app.History.Items.Count==2,"Pause suppresses capture");app.Hide();IntPtr fg=Native.GetForegroundWindow();using(var q=new QuickPanel(app)){q.OpenNear(app.PetBounds);Verify(Native.GetForegroundWindow()==fg,"Quick panel does not activate or steal focus");}app.Remind("water");Verify(app.Settings.WaterMinutes==45,"Reminder preserves configured interval");timer.Stop();app.ExitApp();break;
 }}catch(Exception ex){failure=ex;timer.Stop();app.ExitApp();}}
}
