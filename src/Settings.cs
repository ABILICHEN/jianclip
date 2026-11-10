using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
public class SettingsForm : Form {
 readonly ClipForm app;
 public SettingsForm(ClipForm owner) {
  app = owner; var s = app.Settings;
  Text = "袋袋 · 陪伴与知识电台"; Size = new Size(610, 740); MinimumSize = new Size(580, 690); MaximizeBox = false; MinimizeBox = false;
  StartPosition = FormStartPosition.CenterParent; BackColor = Skin.Cream; Font = Skin.Font(10);
  var root = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(20), ColumnCount = 1, RowCount = 3 };
  root.RowStyles.Add(new RowStyle(SizeType.Absolute, 48)); root.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); root.RowStyles.Add(new RowStyle(SizeType.Absolute, 56)); Controls.Add(root);
  root.Controls.Add(new Label { Text = "袋袋陪你，把日常过得有意思一点", Font = Skin.Font(15, true), AutoSize = true });
  var tabs = new TabControl { Dock = DockStyle.Fill }; var healthPage = new TabPage("桌宠与健康") { BackColor = Skin.Cream }; var knowledgePage = new TabPage("知识与互动") { BackColor = Skin.Cream }; tabs.TabPages.Add(healthPage); tabs.TabPages.Add(knowledgePage); var personalityPage = new TabPage("互动与人格") { BackColor = Skin.Cream }; tabs.TabPages.Add(personalityPage); var interaction = Grid(personalityPage); int ir = 0; Wide(interaction, Note("在“知识与互动”中勾选互动问答后，将按设定比例出现。\n查看答案不计分，同一道题只能计分一次。"), ir++, 68); root.Controls.Add(tabs);
  var health = Grid(healthPage); int r = 0;
  var pet = Check("显示桌面袋鼠", s.PetVisible); Wide(health, pet, r++);
  var animations = Check("启用肢体动作与入袋动画", s.Animations); Wide(health, animations, r++);
  var enabled = Check("开启喝水、远眺等健康提醒", s.RemindersEnabled); Wide(health, enabled, r++);
  var water = Check("喝点水  ·  每隔（分钟）", s.WaterEnabled); var wm = Number(s.WaterMinutes, 1, 240); Pair(health, water, wm, r++);
  var eyes = Check("远眺放松  ·  每隔（分钟）", s.EyesEnabled); var em = Number(s.EyesMinutes, 1, 240); Pair(health, eyes, em, r++);
  var stretch = Check("起身伸展  ·  每隔（分钟）", s.StretchEnabled); var sm = Number(s.StretchMinutes, 1, 240); Pair(health, stretch, sm, r++);
  var quiet = Check("22:00—08:00 安静陪伴（也用于知识推送）", s.QuietAtNight); Wide(health, quiet, r++);
  Wide(health, Note("连续 5 分钟无键鼠操作时暂停自动提醒与知识推送。\n健康提醒可一键完成，或稍后 10 分钟再提醒。"), r++, 65);
  Wide(health, Skin.Button("试试喝水提醒", () => app.Remind("water")), r++, 45);
  var knowledge = Grid(knowledgePage); r = 0;
  var knowledgeEnabled = Check("开启知识电台，定时弹出一条有趣的内容", s.KnowledgeEnabled); Wide(knowledge, knowledgeEnabled, r++);
  Wide(knowledge, Note("离线库共 " + app.Library.Total.ToString("N0") + " 条，分类随机推送，无需联网。"), r++, 36);
  var extended = Check("使用扩展诗词与英汉词库", s.UseExtendedCatalog); Wide(knowledge, extended, r++);
  var curated = Check("诗词优先推送有逐篇赏析的精选", s.PreferCuratedPoetry); Wide(knowledge, curated, r++);
  var commonWords = Check("英语优先常用词（关闭可抽取全部词库）", s.PreferCommonWords); Wide(knowledge, commonWords, r++);
  var interval = Number(s.KnowledgeMinutes, 1, 240); Pair(knowledge, new Label { Text = "推送间隔（分钟）", AutoSize = true, Anchor = AnchorStyles.Left }, interval, r++);
  var delay = Number(s.PoetryDelaySeconds, 1, 120); Pair(knowledge, new Label { Text = "诗句出现后，延迟几秒展开赏析", AutoSize = true, Anchor = AnchorStyles.Left }, delay, r++);
  var duration = Number(s.KnowledgeCardSeconds, 10, 180); Pair(knowledge, new Label { Text = "内容卡片默认停留（秒）", AutoSize = true, Anchor = AnchorStyles.Left }, duration, r++);
  var kinds = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, WrapContents = true };
  var poem = Check("诗词名句", s.PoetryEnabled); var quote = Check("名言金句", s.QuotesEnabled); var word = Check("英语单词", s.WordsEnabled); var fact = Check("有趣百科", s.FactsEnabled); var tip = Check("生活技巧", s.TipsEnabled); var quiz = Check("互动问答", s.QuizEnabled);
  foreach (var c in new[] { poem, quote, word, fact, tip, quiz }) { c.MinimumSize = new Size(135, 26); kinds.Controls.Add(c); } Wide(knowledge, kinds, r++, 70);
  var probability = Number(s.QuizPercent, 0, 100); Pair(interaction, new Label { Text = "混合推送中互动题的机会（%）", AutoSize = true, Anchor = AnchorStyles.Left }, probability, ir++);
  var voice = Check("英语单词出现时自动发音（也可手动点击发音）", s.AutoPronounce); Wide(knowledge, voice, r++);
  var playful = Check("答题时使用俏皮语气（不含毒舌）", s.PlayfulFeedback); Wide(interaction, playful, ir++);
  var sassy = Check("开启毒舌人格（默认关闭）", s.SassyMode); Wide(interaction, sassy, ir++);
  var sassyLevel = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 95, Anchor = AnchorStyles.Left }; sassyLevel.Items.AddRange(new object[] { "轻度", "适中", "较强" }); sassyLevel.SelectedIndex = s.SassyLevel - 1;
  Pair(interaction, new Label { Text = "毒舌强度（用于答题、收纳等台词）", AutoSize = true, Anchor = AnchorStyles.Left }, sassyLevel, ir++);
  Wide(interaction, Note("轻度：答案藏得不深，你绕得挺远。\n适中：答错得这么自信，袋袋差点都信了。\n较强：知识从脑袋路过，你连个招呼都没打。"), ir++, 85);
  Wide(interaction, Note(app.Stats.Summary), ir++, 42);
  Wide(interaction, Skin.Button("来道互动题", () => app.ShowKnowledgeNow("quiz")), ir++, 45);
  Wide(knowledge, Note("鼠标停在卡片上时暂停收起，也可选择“留在这里”。\n诗词赏析至少保留 15 秒；内容卡与健康提醒会错开。\n发音使用本机英语语音，不依赖联网。"), r++, 70);
  var demos = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = true };
  demos.Controls.Add(Skin.Button("试试诗词", () => app.ShowKnowledgeNow("poem"))); demos.Controls.Add(Skin.Button("试试单词", () => app.ShowKnowledgeNow("word"))); demos.Controls.Add(Skin.Button("来道互动题", () => app.ShowKnowledgeNow("quiz"))); Wide(knowledge, demos, r++, 44);
  var footer = new FlowLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(0, 15, 0, 0), WrapContents = false };
  footer.Controls.Add(Skin.Button("保存设置", () => {
   if (knowledgeEnabled.Checked && !new[] { poem, quote, word, fact, tip, quiz }.Any(c => c.Checked)) { MessageBox.Show("请至少选择一种知识内容，或关闭知识电台。", "袋袋"); tabs.SelectedTab = knowledgePage; return; }
   s.Animations = animations.Checked; s.RemindersEnabled = enabled.Checked; s.WaterEnabled = water.Checked; s.EyesEnabled = eyes.Checked; s.StretchEnabled = stretch.Checked; s.QuietAtNight = quiet.Checked;
   s.WaterMinutes = (int)wm.Value; s.EyesMinutes = (int)em.Value; s.StretchMinutes = (int)sm.Value;
   s.KnowledgeEnabled = knowledgeEnabled.Checked; s.KnowledgeMinutes = (int)interval.Value; s.PoetryDelaySeconds = (int)delay.Value; s.KnowledgeCardSeconds = (int)duration.Value; s.QuizPercent = (int)probability.Value;
   s.UseExtendedCatalog = extended.Checked; s.PreferCuratedPoetry = curated.Checked; s.PreferCommonWords = commonWords.Checked; s.PoetryEnabled = poem.Checked; s.QuotesEnabled = quote.Checked; s.WordsEnabled = word.Checked; s.FactsEnabled = fact.Checked; s.TipsEnabled = tip.Checked; s.QuizEnabled = quiz.Checked; s.AutoPronounce = voice.Checked; s.PlayfulFeedback = playful.Checked; s.SassyMode = sassy.Checked; s.SassyLevel = sassyLevel.SelectedIndex + 1;
   s.Validate(); app.ApplyCompanionSettings(); app.SetPetVisible(pet.Checked); app.SaveSettings(); Close();
  }, true)); footer.Controls.Add(Skin.Button("取消", Close)); root.Controls.Add(footer);
 }
 TableLayoutPanel Grid(TabPage page) { var grid = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(14), ColumnCount = 2 }; grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 76)); grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 24)); page.AutoScroll = true; page.Controls.Add(grid); return grid; }
 void Wide(TableLayoutPanel grid, Control control, int row, int height = 37) { grid.RowCount = row + 1; grid.RowStyles.Add(new RowStyle(SizeType.Absolute, height)); grid.Controls.Add(control, 0, row); grid.SetColumnSpan(control, 2); }
 void Pair(TableLayoutPanel grid, Control left, Control right, int row) { grid.RowCount = row + 1; grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 37)); grid.Controls.Add(left, 0, row); grid.Controls.Add(right, 1, row); }
 CheckBox Check(string text, bool value) { return new CheckBox { Text = text, Checked = value, AutoSize = true, ForeColor = Skin.Ink, Anchor = AnchorStyles.Left, Margin = new Padding(0, 4, 5, 4) }; }
 NumericUpDown Number(int n, int min, int max) { return new NumericUpDown { Minimum = min, Maximum = max, Value = n, Width = 82, Anchor = AnchorStyles.Left }; }
 Label Note(string text) { return new Label { Text = text, ForeColor = Skin.Muted, AutoSize = true, Dock = DockStyle.Fill, Font = Skin.Font(9), Padding = new Padding(0, 5, 0, 0) }; }
 public void SelectKnowledgeTab(int index = 1) { var tabs = Controls[0].Controls.OfType<TabControl>().First(); tabs.SelectedIndex = index; }
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
