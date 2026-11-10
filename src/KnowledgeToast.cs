using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

public class KnowledgeToast : Form {
 readonly ClipForm app;
 public readonly KnowledgeItem Item;
 public readonly KnowledgePresentation Presentation;
 public readonly QuizAttempt Attempt;
 readonly Label title = new Label(), body = new Label(), detail = new Label(), feedback = new Label(), status = new Label();
 readonly FlowLayoutPanel options = new FlowLayoutPanel();
 readonly Panel detailPanel = new Panel();
 readonly TableLayoutPanel root = new TableLayoutPanel();
 readonly Timer timer = new Timer();
 readonly WordSpeaker speaker = new WordSpeaker();
 readonly Button pin;
 bool expanded, answerRevealed;
 protected override bool ShowWithoutActivation { get { return true; } }
 protected override CreateParams CreateParams { get { var p = base.CreateParams; p.ExStyle |= 0x8000000 | 0x80; return p; } }
 protected override void WndProc(ref Message m) { if (m.Msg == 0x21) { m.Result = new IntPtr(3); return; } base.WndProc(ref m); }
 public KnowledgeToast(ClipForm owner, KnowledgeItem item) {
  app = owner; Item = item; Presentation = new KnowledgePresentation(item, app.Settings, DateTime.Now);
  if (item.Category == "quiz") Attempt = new QuizAttempt(item);
  Text = "袋袋 · " + item.CategoryName; FormBorderStyle = FormBorderStyle.None; TopMost = true; ShowInTaskbar = false; StartPosition = FormStartPosition.Manual;
  BackColor = Skin.Cream; ForeColor = Skin.Ink; Font = Skin.Font(10); Padding = new Padding(20, 15, 20, 14); Width = 440;
  Height = item.Category == "quiz" ? 620 : item.Category == "poem" ? 330 : item.Category == "word" ? 420 : 465;
  root.Dock = DockStyle.Fill; root.ColumnCount = 1; root.RowCount = 9;
  foreach (int h in new[] { 36, 56, item.Category == "word" ? 48 : 82, item.Category == "word" ? 30 : 0, item.Category == "quiz" ? item.Options.Length * 43 : 0, 0, 0, 46, 24 }) root.RowStyles.Add(new RowStyle(SizeType.Absolute, h));
  root.RowStyles[6] = new RowStyle(SizeType.Percent, 100); Controls.Add(root);
  var header = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1 }; header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100));
  header.Controls.Add(new Label { Text = "袋袋的口袋电台  /  " + item.CategoryName, AutoSize = true, ForeColor = Skin.Green, Font = Skin.Font(9, true), Padding = new Padding(0, 5, 0, 0) });
  pin = Skin.Button("留在这里", () => { Presentation.Pinned = !Presentation.Pinned; pin.Text = Presentation.Pinned ? "取消保留" : "留在这里"; }); pin.MinimumSize = new Size(88, 28); pin.Height = 28; pin.Font = Skin.Font(8); header.Controls.Add(pin); root.Controls.Add(header, 0, 0);
  title.Text = item.Title; title.Font = Skin.Font(item.Category == "word" ? 24 : 17, true); title.Dock = DockStyle.Fill; title.AutoEllipsis = true; root.Controls.Add(title, 0, 1);
  body.Text = item.Body; body.Font = Skin.Font(item.Category == "poem" ? 17 : item.Category == "word" ? 13 : 11); body.Dock = DockStyle.Fill; body.AutoEllipsis = true; root.Controls.Add(body, 0, 2);
  var pronunciation = new Label { Text = item.Pronunciation ?? "", Dock = DockStyle.Fill, ForeColor = Skin.Green, Font = new Font("Segoe UI", 11) }; root.Controls.Add(pronunciation, 0, 3);
  options.Dock = DockStyle.Fill; options.FlowDirection = FlowDirection.TopDown; options.WrapContents = false;
  if (Attempt != null) for (int i = 0; i < Attempt.Options.Length; i++) {
   int choice = i; var b = Skin.Button(((char)('A' + i)) + "  " + Attempt.Options[i], () => SubmitAnswer(choice)); b.Width = 390; b.AutoSize = false; b.Height = 37; b.Margin = new Padding(0, 0, 0, 6); b.TextAlign = ContentAlignment.MiddleLeft; b.AutoEllipsis = true; options.Controls.Add(b);
  }
  root.Controls.Add(options, 0, 4);
  feedback.Dock = DockStyle.Fill; feedback.Font = Skin.Font(10, true); feedback.ForeColor = Skin.Green; root.Controls.Add(feedback, 0, 5);
  detailPanel.Dock = DockStyle.Fill; detailPanel.AutoScroll = true; detail.AutoSize = true; detail.MaximumSize = new Size(370, 0); detail.Padding = new Padding(0, 5, 0, 0); detail.Text = ""; detail.ForeColor = Skin.Ink; detailPanel.Controls.Add(detail); root.Controls.Add(detailPanel, 0, 6);
  var actions = new FlowLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(0, 7, 0, 0), WrapContents = false };
  if (item.Category == "word") actions.Controls.Add(Skin.Button("▶ 发音", Pronounce, true));
  if (item.Category == "poem") actions.Controls.Add(Skin.Button(item.DetailHeading == "原诗与阅读提示" ? "现在看原诗" : "现在看赏析", () => { Presentation.Reveal(DateTime.Now); RenderDetail(); }, true));
  if (item.Category == "quiz") actions.Controls.Add(Skin.Button("看答案", RevealAnswer));
  actions.Controls.Add(Skin.Button("换一条", () => { Close(); app.ShowKnowledgeNow(); })); actions.Controls.Add(Skin.Button("关闭", Close)); root.Controls.Add(actions, 0, 7);
  status.ForeColor = Skin.Muted; status.Font = Skin.Font(8); status.Dock = DockStyle.Fill; root.Controls.Add(status, 0, 8);
  RenderDetail(); PositionNearPet();
  timer.Interval = 200; timer.Tick += delegate {
   if (Presentation.Advance(DateTime.Now, Bounds.Contains(Cursor.Position))) { Close(); return; }
   if (Presentation.DetailVisible && !expanded) RenderDetail();
   if (!Presentation.DetailVisible && item.Category == "poem") status.Text = Math.Max(0, (int)Math.Ceiling((Presentation.DetailDue - DateTime.Now).TotalSeconds)) + " 秒后展开" + (item.DetailHeading == "原诗与阅读提示" ? "原诗与阅读提示" : "赏析") + " · 悬停暂停收起";
  };
  Shown += delegate { timer.Start(); if (item.Category == "word" && app.Settings.AutoPronounce) Pronounce(); };
 }
 void RenderDetail() {
  if (!Presentation.DetailVisible) { detail.Text = ""; status.Text = Item.Category == "poem" ? app.Settings.PoetryDelaySeconds + " 秒后展开" + (Item.DetailHeading == "原诗与阅读提示" ? "原诗与阅读提示" : "赏析") : "选一项试试吧 · " + app.Stats.Summary; return; }
  expanded = true;
  if (Item.Category == "poem") { Height = Item.DetailHeading == "原诗与阅读提示" ? 570 : 470; PositionNearPet(); }
  string label = Item.Category == "poem" ? "赏析" : Item.Category == "word" ? "这样用" : Item.Category == "quiz" ? "答案与解释" : "多知道一点";
  if (!string.IsNullOrWhiteSpace(Item.DetailHeading)) label = Item.DetailHeading;
  detail.Text = label + "\n" + Item.Detail + "\n\n" + Item.Attribution;
  status.Text = "悬停暂停收起 · 可点“留在这里”慢慢读";
  if (!string.IsNullOrWhiteSpace(Item.Source) && !detailPanel.Controls.OfType<LinkLabel>().Any()) {
   var link = new LinkLabel { Text = "查看内容来源 ↗", AutoSize = true, Top = detail.PreferredHeight + 10, LinkColor = Skin.Green, Font = Skin.Font(8) };
   link.LinkClicked += delegate { try { Process.Start(new ProcessStartInfo(Item.Source) { UseShellExecute = true }); } catch { status.Text = "暂时无法打开浏览器，来源地址已写在内容库中。"; } }; detailPanel.Controls.Add(link);
  }
 }
 public void ShowAnalysisNow() { Presentation.Reveal(DateTime.Now); RenderDetail(); }
 public void Pronounce() { string error = speaker.Speak(Item.SpeakText); if (error != null) status.Text = error; else status.Text = "正在播放英语发音 · 可再次点击重听"; }
 public void SubmitAnswer(int choice) {
  if (Attempt == null || answerRevealed || !Attempt.Answer(choice)) return;
  foreach (Button button in options.Controls) button.Enabled = false;
  options.Controls[Attempt.CorrectIndex].BackColor = Color.FromArgb(212, 235, 220);
  if (!Attempt.Correct) options.Controls[choice].BackColor = Color.FromArgb(250, 219, 196);
  feedback.Text = Personality.Answer(Attempt, app.Settings); feedback.ForeColor = Attempt.Correct ? Skin.Green : Color.FromArgb(159, 94, 41); root.RowStyles[5].Height = 58;
  app.RecordKnowledgeAnswer(Attempt.Correct); app.PetReact(Attempt.Correct ? "happy" : "wave", Attempt.Correct ? "答对了，给你一个口袋里的赞！" : Personality.Line("wrong", app.Settings), 6);
  Presentation.Reveal(DateTime.Now); RenderDetail(); status.Text = app.Stats.Summary;
 }
 public void RevealAnswer() {
  if (Attempt == null || Attempt.Answered || answerRevealed) return;
  answerRevealed = true;
  foreach (Button button in options.Controls) button.Enabled = false;
  options.Controls[Attempt.CorrectIndex].BackColor = Color.FromArgb(212, 235, 220);
  feedback.Text = "正确答案：" + Attempt.Options[Attempt.CorrectIndex] + "（本题未计分）"; root.RowStyles[5].Height = 45;
  // Mark revealed cards as read, without recording an attempted answer.
  for (int i = 0; i < options.Controls.Count; i++) options.Controls[i].Tag = "revealed";
  Presentation.Reveal(DateTime.Now); RenderDetail();
 }
 public void PositionNearPet() {
  var pet = app.PetBounds; var area = Screen.FromRectangle(pet).WorkingArea; int x = pet.Left - Width + 55; if (x < area.Left) x = pet.Right - 30;
  Height = Math.Min(Height, area.Height - 24); Location = new Point(Math.Max(area.Left + 8, Math.Min(x, area.Right - Width - 8)), Math.Max(area.Top + 8, Math.Min(pet.Top - 130, area.Bottom - Height - 8)));
 }
 protected override void OnPaint(PaintEventArgs e) { base.OnPaint(e); using (var p = new Pen(Skin.Gold, 2)) e.Graphics.DrawRectangle(p, 1, 1, Width - 2, Height - 2); }
 protected override void Dispose(bool disposing) { if (disposing) { timer.Dispose(); speaker.Dispose(); } base.Dispose(disposing); }
}
