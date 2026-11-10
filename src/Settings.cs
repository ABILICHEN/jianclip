using System;
using System.Drawing;
using System.Windows.Forms;
public class SettingsForm : Form {
 public SettingsForm(ClipForm app){
  Text="袋袋 · 陪伴与提醒";Size=new Size(490,590);FormBorderStyle=FormBorderStyle.FixedDialog;MaximizeBox=false;MinimizeBox=false;StartPosition=FormStartPosition.CenterParent;BackColor=Skin.Cream;Font=Skin.Font(10);
  var s=app.Settings;var root=new TableLayoutPanel{Dock=DockStyle.Fill,Padding=new Padding(24),ColumnCount=2,RowCount=12};root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,65));root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,35));Controls.Add(root);
  var title=new Label{Text="让袋袋陪你，好好工作也好好休息",Font=Skin.Font(13,true),AutoSize=true};root.Controls.Add(title,0,0);root.SetColumnSpan(title,2);
  var pet=Check("显示桌面袋鼠",s.PetVisible);root.Controls.Add(pet,0,1);root.SetColumnSpan(pet,2);
  var animations=Check("启用肢体动作与入袋动画",s.Animations);root.Controls.Add(animations,0,2);root.SetColumnSpan(animations,2);
  var enabled=Check("开启陪伴提醒",s.RemindersEnabled);root.Controls.Add(enabled,0,3);root.SetColumnSpan(enabled,2);
  var water=Check("喝点水  ·  每隔（分钟）",s.WaterEnabled);var wm=Number(s.WaterMinutes);root.Controls.Add(water,0,4);root.Controls.Add(wm,1,4);
  var eyes=Check("远眺放松  ·  每隔（分钟）",s.EyesEnabled);var em=Number(s.EyesMinutes);root.Controls.Add(eyes,0,5);root.Controls.Add(em,1,5);
  var stretch=Check("起身伸展  ·  每隔（分钟）",s.StretchEnabled);var sm=Number(s.StretchMinutes);root.Controls.Add(stretch,0,6);root.Controls.Add(sm,1,6);
  var quiet=Check("22:00—08:00 安静陪伴",s.QuietAtNight);root.Controls.Add(quiet,0,7);root.SetColumnSpan(quiet,2);
  var note=new Label{Text="连续 5 分钟无键鼠操作时暂停提醒。\n提醒可一键完成，或稍后 10 分钟再提醒。",ForeColor=Skin.Muted,AutoSize=true};root.Controls.Add(note,0,8);root.SetColumnSpan(note,2);
  var buttons=new FlowLayoutPanel{AutoSize=true,Dock=DockStyle.Fill};
  buttons.Controls.Add(Skin.Button("保存设置",()=>{s.Animations=animations.Checked;s.RemindersEnabled=enabled.Checked;s.WaterEnabled=water.Checked;s.EyesEnabled=eyes.Checked;s.StretchEnabled=stretch.Checked;s.QuietAtNight=quiet.Checked;s.WaterMinutes=(int)wm.Value;s.EyesMinutes=(int)em.Value;s.StretchMinutes=(int)sm.Value;app.Clock.Reset(s,DateTime.Now);app.SetPetVisible(pet.Checked);app.SaveSettings();Close();},true));
  buttons.Controls.Add(Skin.Button("试试喝水提醒",()=>app.Remind("water")));root.Controls.Add(buttons,0,10);root.SetColumnSpan(buttons,2);
  for(int i=0;i<12;i++)root.RowStyles.Add(new RowStyle(SizeType.Absolute,i==0?54:i==8?62:i==10?48:36));
 }
 CheckBox Check(string text,bool value){return new CheckBox{Text=text,Checked=value,AutoSize=true,ForeColor=Skin.Ink,Anchor=AnchorStyles.Left};}
 NumericUpDown Number(int n){return new NumericUpDown{Minimum=1,Maximum=240,Value=n,Width=85,Anchor=AnchorStyles.Left};}
}
public class ReminderToast : Form {
 readonly ClipForm app;readonly Timer timeout=new Timer();
 protected override bool ShowWithoutActivation{get{return true;}}
 protected override CreateParams CreateParams{get{var p=base.CreateParams;p.ExStyle|=0x8000000|0x80;return p;}}
 protected override void WndProc(ref Message m){if(m.Msg==0x21){m.Result=new IntPtr(3);return;}base.WndProc(ref m);}
 public ReminderToast(ClipForm owner,string key){app=owner;FormBorderStyle=FormBorderStyle.None;ShowInTaskbar=false;TopMost=true;StartPosition=FormStartPosition.Manual;Size=new Size(340,170);BackColor=Skin.Cream;Font=Skin.Font(10);Padding=new Padding(18);
  var root=new TableLayoutPanel{Dock=DockStyle.Fill,RowCount=3,ColumnCount=1};root.RowStyles.Add(new RowStyle(SizeType.Absolute,30));root.RowStyles.Add(new RowStyle(SizeType.Percent,100));root.RowStyles.Add(new RowStyle(SizeType.Absolute,40));Controls.Add(root);
  string title=key=="water"?"喝口水，补充一点元气":key=="eyes"?"抬起头，让眼睛看看远处":"站起来，和袋袋伸个懒腰";
  string note=key=="water"?"袋袋陪你一起喝水。慢慢来，休息片刻。":key=="eyes"?"看向窗外或远处，放松眼睛约 20 秒。":"轻轻活动肩颈和手臂，给身体一点空间。";
  root.Controls.Add(new Label{Text=title,Font=Skin.Font(12,true),AutoSize=true,ForeColor=Skin.Ink});root.Controls.Add(new Label{Text=note,Dock=DockStyle.Fill,ForeColor=Skin.Muted});
  var actions=new FlowLayoutPanel{Dock=DockStyle.Fill};actions.Controls.Add(Skin.Button("好，休息一下",()=>{app.PetReact("happy","真棒，照顾好自己！",4);Close();},true));actions.Controls.Add(Skin.Button("稍后 10 分钟",()=>{app.Snooze();Close();}));root.Controls.Add(actions);
  var area=Screen.FromRectangle(app.PetBounds).WorkingArea;var pet=app.PetBounds;Location=new Point(Math.Max(area.Left,Math.Min(pet.Left-Width+60,area.Right-Width-12)),Math.Max(area.Top,Math.Min(pet.Top-120,area.Bottom-Height-12)));
  timeout.Interval=25000;timeout.Tick+=delegate{Close();};timeout.Start();
 }
 protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);using(var p=new Pen(Skin.Gold,2))e.Graphics.DrawRectangle(p,1,1,Width-2,Height-2);}
 protected override void Dispose(bool disposing){if(disposing)timeout.Dispose();base.Dispose(disposing);}
}
