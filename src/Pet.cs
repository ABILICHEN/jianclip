using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using System.Windows.Forms;
using System.Collections.Generic;
public class PetForm : LayeredForm {
 readonly ClipForm app; readonly Timer tick=new Timer(); readonly Timer hover=new Timer();
 DateTime started=DateTime.Now,poseUntil=DateTime.MinValue,sayUntil=DateTime.MinValue,entered=DateTime.MinValue;
 public string Pose="idle"; string speech="我是袋袋，把灵感装进口袋。";
 Point anchor; bool dragging,moved; Point dragOrigin; public bool PopupVisible {get{return app.QuickVisible;}}
 public PetForm(ClipForm owner) {
  app=owner; Text="袋袋 · 桌面伙伴"; Size=new Size(260,362);AllowDrop=true;
  DragEnter+=(s,e)=>{if(e.Data.GetDataPresent(DataFormats.UnicodeText)||e.Data.GetDataPresent(DataFormats.Text))e.Effect=DragDropEffects.Copy;};
  DragDrop+=(s,e)=>{string value=e.Data.GetData(DataFormats.UnicodeText) as string ?? e.Data.GetData(DataFormats.Text) as string;app.CollectText(value);};
  var menu=new ContextMenuStrip();menu.Items.Add("打开口袋",null,delegate{app.ShowQuick();});menu.Items.Add("剪贴板管理",null,delegate{app.OpenPanel();});menu.Items.Add("陪伴与提醒设置",null,delegate{app.OpenSettings();});menu.Items.Add("稍后 10 分钟提醒",null,delegate{app.Snooze();});menu.Items.Add("隐藏袋袋",null,delegate{app.SetPetVisible(false);});menu.Items.Add("退出简贴",null,delegate{app.ExitApp();});ContextMenuStrip=menu;
  MouseDown+=(s,e)=>{if(e.Button!=MouseButtons.Left)return;dragging=true;moved=false;anchor=Cursor.Position;dragOrigin=Location;Capture=true;};
  MouseMove+=(s,e)=>{if(!dragging)return;Point d=Cursor.Position;int dx=d.X-anchor.X,dy=d.Y-anchor.Y;if(Math.Abs(dx)+Math.Abs(dy)>5)moved=true;if(moved){app.HideQuick();Location=new Point(dragOrigin.X+dx,dragOrigin.Y+dy);Render();}};
  MouseUp+=(s,e)=>{if(!dragging)return;dragging=false;Capture=false;if(moved){ClampPosition();app.Settings.PetX=Left;app.Settings.PetY=Top;app.SaveSettings();}else{React("wave","嗨！口袋里的内容随时可用。",4);app.ShowQuick();}};
  MouseEnter+=delegate{entered=DateTime.Now;}; MouseLeave+=delegate{entered=DateTime.MinValue;};
  tick.Interval=50;tick.Tick+=delegate{if(!Visible)return;if(DateTime.Now>poseUntil)Pose=Native.Idle()?"sleep":"idle";Render();};tick.Start();
  hover.Interval=100;hover.Tick+=delegate{
   if(!Visible||dragging)return;
   Point c=PointToClient(Cursor.Position);bool inside=c.X>=35&&c.X<245&&c.Y>80&&c.Y<340;
   if(inside){if(entered==DateTime.MinValue)entered=DateTime.Now;if((DateTime.Now-entered).TotalMilliseconds>500&&!PopupVisible)app.ShowQuick();}
   else entered=DateTime.MinValue;
  };hover.Start();sayUntil=DateTime.Now.AddSeconds(8);
 }
 public void RestorePosition(){var a=Screen.PrimaryScreen.WorkingArea;Location=app.Settings.PetX==int.MinValue?new Point(a.Right-Width-24,a.Bottom-Height-15):new Point(app.Settings.PetX,app.Settings.PetY);ClampPosition();}
 public void ClampPosition(){var a=Screen.FromPoint(new Point(Left+Width/2,Top+Height/2)).WorkingArea;Location=new Point(Math.Max(a.Left,Math.Min(Left,a.Right-Width)),Math.Max(a.Top,Math.Min(Top,a.Bottom-Height)));}
 public void React(string pose,string text,int seconds){Pose=pose;poseUntil=DateTime.Now.AddSeconds(seconds);speech=text;sayUntil=poseUntil;if(Visible)Render();}
 public Point Pocket {get{return new Point(Left+132,Top+280);}}
 public void CaptureClip(Clip clip){React("happy","收到啦，已经装进口袋！",3);if(Visible&&app.Settings.Animations){var fly=new ClipFlight(Cursor.Position,Pocket,clip.Kind=="image"?"一张图片":Clip.Shorten(clip.Value,22));fly.Show();}}
 public void Render(){if(IsDisposed)return;using(var b=new Bitmap(Width,Height,PixelFormat.Format32bppArgb))using(var g=Graphics.FromImage(b)){
  g.SmoothingMode=SmoothingMode.AntiAlias;g.Clear(Color.Transparent);
  if(DateTime.Now<sayUntil){Skin.Card(g,new RectangleF(12,6,236,62),Color.FromArgb(245,255,253,245),16);using(var br=new SolidBrush(Color.FromArgb(245,255,253,245)))g.FillPolygon(br,new[]{new Point(119,66),new Point(135,78),new Point(141,64)});using(var f=Skin.Font(10))using(var sf=new StringFormat{Alignment=StringAlignment.Center,LineAlignment=StringAlignment.Center})Skin.Text(g,speech,f,Skin.Ink,new RectangleF(23,14,213,45),sf);}
  KangarooArt.Draw(g,new RectangleF(0,66,260,290),app.Settings.Animations?(DateTime.Now-started).TotalSeconds:1.2,Pose);Present(b);
 }}
 protected override void Dispose(bool disposing){if(disposing){tick.Dispose();hover.Dispose();if(ContextMenuStrip!=null)ContextMenuStrip.Dispose();}base.Dispose(disposing);}
}
public class ClipFlight : LayeredForm {
 readonly Timer timer=new Timer();readonly Point from,to;readonly DateTime start=DateTime.Now;readonly string text;
 protected override CreateParams CreateParams{get{var p=base.CreateParams;p.ExStyle|=0x20;return p;}}
 public ClipFlight(Point source,Point destination,string value){from=source;to=destination;text=value;Size=new Size(170,56);Location=source;timer.Interval=25;timer.Tick+=delegate{double p=Math.Min(1,(DateTime.Now-start).TotalMilliseconds/850);double ease=1-Math.Pow(1-p,3);Location=new Point((int)(from.X+(to.X-from.X)*ease)-Width/2,(int)(from.Y+(to.Y-from.Y)*ease-70*Math.Sin(p*Math.PI))-Height/2);using(var b=new Bitmap(Width,Height,PixelFormat.Format32bppArgb))using(var g=Graphics.FromImage(b)){g.SmoothingMode=SmoothingMode.AntiAlias;float scale=(float)(1-p*.78);var r=new RectangleF((Width-Width*scale)/2,(Height-Height*scale)/2,Width*scale-1,Height*scale-1);Skin.Card(g,r,Color.FromArgb(255,252,230),Math.Max(2,9*scale));if(p<.65)using(var f=Skin.Font(9))Skin.Text(g,Clip.Shorten(text,16),f,Skin.Ink,new RectangleF(r.X+10,r.Y+11,r.Width-20,r.Height-12));Present(b);}if(p>=1)Close();};Shown+=delegate{timer.Start();};}
 protected override void Dispose(bool disposing){if(disposing)timer.Dispose();base.Dispose(disposing);}
}
public class QuickPanel : Form {
 readonly ClipForm app;readonly FlowLayoutPanel rows=new FlowLayoutPanel();readonly Timer leave=new Timer(); DateTime outside=DateTime.MinValue;
 protected override bool ShowWithoutActivation{get{return true;}}
 protected override CreateParams CreateParams{get{var p=base.CreateParams;p.ExStyle|=0x8000000|0x80;p.ClassStyle|=0x20000;return p;}}
 protected override void WndProc(ref Message m){if(m.Msg==0x21){m.Result=new IntPtr(3);return;}base.WndProc(ref m);}
 public QuickPanel(ClipForm owner){app=owner;Text="袋袋的口袋";FormBorderStyle=FormBorderStyle.None;ShowInTaskbar=false;TopMost=true;StartPosition=FormStartPosition.Manual;Size=new Size(350,405);BackColor=Skin.Cream;Padding=new Padding(14);Font=Skin.Font(9);
  var root=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=4};root.RowStyles.Add(new RowStyle(SizeType.Absolute,31));root.RowStyles.Add(new RowStyle(SizeType.Absolute,24));root.RowStyles.Add(new RowStyle(SizeType.Percent,100));root.RowStyles.Add(new RowStyle(SizeType.Absolute,40));Controls.Add(root);
  var header=new Label{Text="袋袋的口袋",Font=Skin.Font(13,true),ForeColor=Skin.Ink,Dock=DockStyle.Fill};root.Controls.Add(header);
  root.Controls.Add(new Label{Text="点选即粘贴到刚才的窗口",AutoSize=true,ForeColor=Skin.Muted});
  rows.Dock=DockStyle.Fill;rows.FlowDirection=FlowDirection.TopDown;rows.WrapContents=false;rows.AutoScroll=true;root.Controls.Add(rows);
  var footer=new FlowLayoutPanel{Dock=DockStyle.Fill};footer.Controls.Add(Skin.Button("全部历史",()=>{Hide();app.OpenPanel();}));footer.Controls.Add(Skin.Button("设置",()=>{Hide();app.OpenSettings();}));root.Controls.Add(footer);
  leave.Interval=120;leave.Tick+=delegate{if(!Visible)return;bool inside=Bounds.Contains(Cursor.Position)||app.PetBounds.Contains(Cursor.Position);if(inside)outside=DateTime.MinValue;else if(outside==DateTime.MinValue)outside=DateTime.Now;else if((DateTime.Now-outside).TotalMilliseconds>800)Hide();};leave.Start();
 }
 public void RefreshClips(IEnumerable<Clip> clips){rows.SuspendLayout();foreach(Control c in rows.Controls.Cast<Control>().ToArray()){rows.Controls.Remove(c);c.Dispose();}
  var chosen=clips.OrderByDescending(c=>c.Pinned).ThenByDescending(c=>c.Time).Take(5).ToList();
  foreach(var clip in chosen){var b=new Button{Text=(clip.Pinned?"★ ":"")+(clip.Kind=="image"?"[图片]":Clip.Shorten(clip.Value,42)),Width=300,Height=51,FlatStyle=FlatStyle.Flat,BackColor=Color.White,ForeColor=Skin.Ink,TextAlign=ContentAlignment.MiddleLeft,AutoEllipsis=true,Padding=new Padding(9,2,9,2),Margin=new Padding(0,0,0,6),Cursor=Cursors.Hand,AccessibleName="粘贴："+clip.Summary};b.FlatAppearance.BorderColor=Skin.Line;var selected=clip;b.Click+=delegate{Hide();app.PasteClip(selected,true);};rows.Controls.Add(b);}
  if(chosen.Count==0)rows.Controls.Add(new Label{Text="口袋还是空的。\n复制一些文字，让袋袋替你收好。",AutoSize=false,Size=new Size(300,70),ForeColor=Skin.Muted,Padding=new Padding(10)});rows.ResumeLayout();}
 public void OpenNear(Rectangle pet){RefreshClips(app.History.Items);var area=Screen.FromRectangle(pet).WorkingArea;int x=pet.Left-Width+35;if(x<area.Left)x=pet.Right-35;int y=pet.Top-120;Location=new Point(Math.Max(area.Left,Math.Min(x,area.Right-Width)),Math.Max(area.Top,Math.Min(y,area.Bottom-Height)));outside=DateTime.MinValue;Show();}
 protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);using(var p=new Pen(Skin.Line))e.Graphics.DrawRectangle(p,0,0,Width-1,Height-1);}
 protected override void Dispose(bool disposing){if(disposing)leave.Dispose();base.Dispose(disposing);}
}
