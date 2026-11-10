using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Web.Script.Serialization;
using Microsoft.Win32;

public class Clip {
 public string Kind {get;set;} public string Value {get;set;} public DateTime Time {get;set;} public bool Pinned {get;set;}
 public override string ToString(){return (Pinned?"★  ":"")+(Kind=="image"?"[图片]":Value.Replace("\r"," ").Replace("\n","  ")) + "    · " + Time.ToString("MM-dd HH:mm");}
}
public class ClipForm:Form {
 [DllImport("user32.dll")] static extern bool AddClipboardFormatListener(IntPtr h);
 [DllImport("user32.dll")] static extern bool RemoveClipboardFormatListener(IntPtr h);
 [DllImport("user32.dll")] static extern bool RegisterHotKey(IntPtr h,int id,uint mod,uint key);
 [DllImport("user32.dll")] static extern bool UnregisterHotKey(IntPtr h,int id);
 [DllImport("user32.dll")] static extern IntPtr GetForegroundWindow();
 [DllImport("user32.dll")] static extern bool SetForegroundWindow(IntPtr h);
 [DllImport("user32.dll")] static extern uint GetClipboardSequenceNumber();
 const string RunKey=@"Software\Microsoft\Windows\CurrentVersion\Run";
 readonly string folder=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"JianClip");
 List<Clip> clips=new List<Clip>(); ListBox list=new ListBox(); TextBox search=new TextBox(); TextBox preview=new TextBox(); PictureBox picture=new PictureBox(); Label status=new Label(); CheckBox startup=new CheckBox(); CheckBox pause=new CheckBox(); NotifyIcon tray; bool exiting; IntPtr target; uint ownSequence; int retries; Timer retry=new Timer();
 public ClipForm(){
 Text="简贴 · 剪贴板历史"; Size=new Size(840,620); MinimumSize=new Size(680,460); StartPosition=FormStartPosition.CenterScreen; Font=new Font("Microsoft YaHei UI",10); BackColor=Color.FromArgb(246,248,252);
 var root=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=6,Padding=new Padding(20)}; root.RowStyles.Add(new RowStyle(SizeType.Absolute,48));root.RowStyles.Add(new RowStyle(SizeType.Absolute,38));root.RowStyles.Add(new RowStyle(SizeType.Percent,60));root.RowStyles.Add(new RowStyle(SizeType.Percent,40));root.RowStyles.Add(new RowStyle(SizeType.Absolute,45));root.RowStyles.Add(new RowStyle(SizeType.Absolute,28)); Controls.Add(root);
 root.Controls.Add(new Label{Text="简贴  /  让复制的内容随时可用",AutoSize=true,Font=new Font(Font.FontFamily,17,FontStyle.Bold)});
 search.Dock=DockStyle.Fill; search.TextChanged+=delegate{RefreshList();};root.Controls.Add(search);
 list.Dock=DockStyle.Fill;list.HorizontalScrollbar=true;list.IntegralHeight=false; list.SelectedIndexChanged+=delegate{ShowPreview();};list.DoubleClick+=delegate{UseClip(true);};list.KeyDown+=(s,e)=>{if(e.KeyCode==Keys.Enter){e.SuppressKeyPress=true;UseClip(true);} if(e.KeyCode==Keys.Delete)Delete();};root.Controls.Add(list);
 var panel=new Panel{Dock=DockStyle.Fill,Padding=new Padding(0,8,0,0)};preview.Multiline=true;preview.ReadOnly=true;preview.ScrollBars=ScrollBars.Vertical;preview.Dock=DockStyle.Fill;picture.Dock=DockStyle.Fill;picture.SizeMode=PictureBoxSizeMode.Zoom;panel.Controls.Add(preview);panel.Controls.Add(picture);root.Controls.Add(panel);
 var actions=new FlowLayoutPanel{Dock=DockStyle.Fill};root.Controls.Add(actions); AddButton(actions,"粘贴",()=>UseClip(true));AddButton(actions,"仅复制",()=>UseClip(false));AddButton(actions,"收藏 / 取消",()=>{var c=Selected();if(c!=null){c.Pinned=!c.Pinned;Save();RefreshList();}});AddButton(actions,"删除",Delete);AddButton(actions,"清空",()=>{if(MessageBox.Show("清空全部历史（含收藏）？","简贴",MessageBoxButtons.YesNo)==DialogResult.Yes){clips.Clear();Save();RefreshList();}});
 startup.Text="开机自启";startup.AutoSize=true;using(var k=Registry.CurrentUser.OpenSubKey(RunKey)){startup.Checked=k!=null&&k.GetValue("JianClip")!=null;} startup.CheckedChanged+=delegate{try{using(var k=Registry.CurrentUser.CreateSubKey(RunKey)){if(startup.Checked)k.SetValue("JianClip","\""+Application.ExecutablePath+"\" --background");else k.DeleteValue("JianClip",false);}}catch(Exception ex){MessageBox.Show(ex.Message);}};actions.Controls.Add(startup);pause.Text="暂停记录";pause.AutoSize=true;actions.Controls.Add(pause);
 status.Dock=DockStyle.Fill;root.Controls.Add(status);
 Directory.CreateDirectory(folder);LoadHistory();RefreshList();
 tray=new NotifyIcon{Icon=SystemIcons.Application,Text="简贴 · Ctrl+Alt+V",Visible=true};var menu=new ContextMenuStrip();menu.Items.Add("打开剪贴板",null,delegate{OpenPanel();});menu.Items.Add("退出",null,delegate{exiting=true;Close();});tray.ContextMenuStrip=menu;tray.DoubleClick+=delegate{OpenPanel();};
 retry.Interval=100;retry.Tick+=delegate{retry.Stop();CaptureClipboard();};
 FormClosing+=(s,e)=>{if(!exiting){e.Cancel=true;Hide();}else{RemoveClipboardFormatListener(Handle);UnregisterHotKey(Handle,1);tray.Dispose();}};
 Shown+=delegate{if(Environment.GetCommandLineArgs().Contains("--background"))Hide();};
 }
 void AddButton(Control p,string t,Action a){var b=new Button{Text=t,AutoSize=true,Height=32};b.Click+=delegate{a();};p.Controls.Add(b);}
 protected override void OnHandleCreated(EventArgs e){base.OnHandleCreated(e);if(!AddClipboardFormatListener(Handle))MessageBox.Show("剪贴板监听注册失败。");if(!RegisterHotKey(Handle,1,0x4003,0x56))MessageBox.Show("Ctrl+Alt+V 已被其他程序占用，请从托盘打开简贴。");}
 protected override void WndProc(ref Message m){if(m.Msg==0x312){if(Visible&&GetForegroundWindow()==Handle)Hide();else OpenPanel();}if(m.Msg==0x31D){retries=0;retry.Stop();retry.Start();}base.WndProc(ref m);}
 void OpenPanel(){var fg=GetForegroundWindow();if(fg!=Handle)target=fg;Show();if(WindowState==FormWindowState.Minimized)WindowState=FormWindowState.Normal;Activate();search.Focus();search.SelectAll();}
 Clip Selected(){return list.SelectedItem as Clip;}
 void RefreshList(){var selected=Selected();list.BeginUpdate();list.Items.Clear();foreach(var c in clips.Where(c=>c.Kind=="image"?search.Text.Length==0||"图片".Contains(search.Text):c.Value.IndexOf(search.Text,StringComparison.OrdinalIgnoreCase)>=0).OrderByDescending(c=>c.Pinned).ThenByDescending(c=>c.Time))list.Items.Add(c);list.EndUpdate();if(selected!=null&&list.Items.Contains(selected))list.SelectedItem=selected;else if(list.Items.Count>0)list.SelectedIndex=0;else ShowPreview();status.Text="Ctrl+Alt+V 呼出 · 双击 / Enter 粘贴 · "+clips.Count+" 条 · 本机加密保存";}
 void ShowPreview(){var c=Selected();var old=picture.Image;picture.Image=null;if(old!=null)old.Dispose();picture.Visible=c!=null&&c.Kind=="image";preview.Visible=!picture.Visible;preview.Text=c==null?"复制文本或图片后，历史记录会出现在这里。":c.Kind=="text"?c.Value:"";if(picture.Visible)using(var ms=new MemoryStream(Convert.FromBase64String(c.Value)))using(var im=Image.FromStream(ms))picture.Image=new Bitmap(im);}
 void Delete(){var c=Selected();if(c!=null){clips.Remove(c);Save();RefreshList();}}
 bool Excluded(IDataObject d){if(d.GetDataPresent("ExcludeClipboardContentFromMonitorProcessing"))return true;if(d.GetDataPresent("CanIncludeInClipboardHistory")){var v=d.GetData("CanIncludeInClipboardHistory");var stream=v as MemoryStream;if(stream!=null){var b=stream.ToArray();if(b.Length>=4&&BitConverter.ToInt32(b,0)==0)return true;}var bytes=v as byte[];if(bytes!=null&&bytes.Length>=4&&BitConverter.ToInt32(bytes,0)==0)return true;if(v is int&&(int)v==0)return true;}return false;}
 void CaptureClipboard(){if(pause.Checked||GetClipboardSequenceNumber()==ownSequence)return;try{var d=Clipboard.GetDataObject();if(d==null||Excluded(d))return;Clip c=null;if(Clipboard.ContainsImage()){using(var im=Clipboard.GetImage())using(var ms=new MemoryStream()){im.Save(ms,System.Drawing.Imaging.ImageFormat.Png);if(ms.Length>4*1024*1024){status.Text="图片超过 4 MB，未记录";return;}c=new Clip{Kind="image",Value=Convert.ToBase64String(ms.ToArray())};}}else if(Clipboard.ContainsText()){var t=Clipboard.GetText();if(string.IsNullOrWhiteSpace(t)||t.Length>500000)return;c=new Clip{Kind="text",Value=t};}if(c==null)return;var existing=clips.FirstOrDefault(x=>x.Kind==c.Kind&&x.Value==c.Value);if(existing!=null){existing.Time=DateTime.Now;}else{c.Time=DateTime.Now;clips.Add(c);}while(clips.Count>200 || clips.Sum(x=>(long)x.Value.Length)>16000000){var last=clips.Where(x=>!x.Pinned).OrderBy(x=>x.Time).FirstOrDefault();if(last==null){clips.Remove(c);break;}clips.Remove(last);}Save();RefreshList();}catch(ExternalException){if(++retries<8)retry.Start();}catch(Exception ex){status.Text="记录失败："+ex.Message;}}
 void UseClip(bool paste){var c=Selected();if(c==null)return;try{if(c.Kind=="text")Clipboard.SetText(c.Value);else using(var ms=new MemoryStream(Convert.FromBase64String(c.Value)))using(var im=Image.FromStream(ms))Clipboard.SetImage(im);ownSequence=GetClipboardSequenceNumber();if(paste){Hide();if(target!=IntPtr.Zero&&SetForegroundWindow(target)){var t=new Timer{Interval=180};t.Tick+=delegate{t.Stop();t.Dispose();if(GetForegroundWindow()==target)SendKeys.SendWait("^v");};t.Start();}else tray.ShowBalloonTip(2500,"已复制","切换到目标窗口后按 Ctrl+V 粘贴。",ToolTipIcon.Info);}}catch(Exception ex){MessageBox.Show("复制失败："+ex.Message);}}
 void Save(){try{var json=new JavaScriptSerializer{MaxJsonLength=32000000}.Serialize(clips);var data=ProtectedData.Protect(Encoding.UTF8.GetBytes(json),null,DataProtectionScope.CurrentUser);var path=Path.Combine(folder,"history.dat");File.WriteAllBytes(path+".tmp",data);if(File.Exists(path))File.Replace(path+".tmp",path,null);else File.Move(path+".tmp",path);}catch(Exception ex){status.Text="保存失败："+ex.Message;}}
 void LoadHistory(){var p=Path.Combine(folder,"history.dat");if(!File.Exists(p))return;try{clips=new JavaScriptSerializer{MaxJsonLength=32000000}.Deserialize<List<Clip>>(Encoding.UTF8.GetString(ProtectedData.Unprotect(File.ReadAllBytes(p),null,DataProtectionScope.CurrentUser)))??new List<Clip>();}catch{MessageBox.Show("历史文件无法读取，原文件已保留备份。");File.Copy(p,p+".unreadable-"+DateTime.Now.ToString("yyyyMMddHHmmss"),true);}}
 [STAThread]public static void Main(){bool created;using(var mutex=new System.Threading.Mutex(true,"Local\\JianClip.Desktop",out created)){if(!created){MessageBox.Show("简贴已在运行，请在系统托盘打开，或按 Ctrl+Alt+V。");return;}Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);Application.Run(new ClipForm());}}
}

