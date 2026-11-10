using System;
using System.Windows.Forms;
public partial class ClipForm {
 public KnowledgeLibrary Library { get; private set; }
 public KnowledgeDeck Deck { get; private set; }
 public KnowledgeClock KnowledgeSchedule { get; private set; }
 public KnowledgeStats Stats { get; private set; }
 public KnowledgeToast KnowledgeCard { get; private set; }
 DateTime lastCompanionPopup = DateTime.MinValue;
 ToolStripMenuItem learningToggle;
 bool syncingKnowledgeToggle;
 void SyncKnowledgeToggle() {
  if (learningToggle == null) return;
  syncingKnowledgeToggle = true;
  learningToggle.Checked = Settings.KnowledgeEnabled;
  syncingKnowledgeToggle = false;
 }
 public bool KnowledgeVisible { get { return KnowledgeCard != null && !KnowledgeCard.IsDisposed && KnowledgeCard.Visible; } }
 void InitializeCompanion() {
  Library = new KnowledgeLibrary(); Deck = new KnowledgeDeck(Library); KnowledgeSchedule = new KnowledgeClock(Settings, DateTime.Now); Stats = KnowledgeStats.Load(folder);
 }
 void TickCompanion() {
  DateTime now = DateTime.Now; bool idle = Native.Idle();
  if (KnowledgeVisible || toast != null && toast.Visible) return;
  string due = Clock.Next(Settings, now, idle);
  if (due != null) { Remind(due); return; }
  bool busy = QuickVisible || settingsWindow != null && settingsWindow.Visible || now < Clock.SnoozedUntil || (now - lastCompanionPopup).TotalSeconds < 60;
  if (KnowledgeSchedule.TryDue(Settings, now, idle, busy)) ShowKnowledgeNow();
 }
 public void ShowKnowledgeNow(string category = null) {
  var item = Deck.Next(Settings, category);
  if (item == null) { PetReact("wave", "先在设置里选择喜欢的内容吧。", 5); return; }
  HideQuick(); if (toast != null && !toast.IsDisposed) toast.Close(); CloseKnowledge();
  lastCompanionPopup = DateTime.Now; KnowledgeSchedule.Reset(Settings, DateTime.Now);
  KnowledgeCard = new KnowledgeToast(this, item);
  KnowledgeCard.FormClosed += delegate { lastCompanionPopup = DateTime.Now; };
  PetReact(item.Category == "quiz" ? "wave" : "idle", Settings.SassyMode ? Personality.Line("knowledge", Settings) : item.Category == "quiz" ? "开动脑筋，猜猜我的口袋里装了什么？" : item.Category == "poem" ? "送你一句诗，慢慢读。" : item.Category == "word" ? "今天也学一个新词吧。" : "给你一点有趣的新鲜事。", 6);
  KnowledgeCard.Show();
 }
 public void CloseKnowledge() { if (KnowledgeCard != null && !KnowledgeCard.IsDisposed) KnowledgeCard.Close(); }
 public void ApplyCompanionSettings() {
  Clock.Reset(Settings, DateTime.Now); KnowledgeSchedule.Reset(Settings, DateTime.Now); CloseKnowledge(); SyncKnowledgeToggle();
 }
 public void SetKnowledgeEnabled(bool enabled) {
  Settings.KnowledgeEnabled = enabled; KnowledgeSchedule.Reset(Settings, DateTime.Now); if (!enabled) CloseKnowledge(); SyncKnowledgeToggle(); SaveSettings();
 }
 public void RecordKnowledgeAnswer(bool correct) {
  Stats.Record(correct); try { Stats.Save(folder); } catch { PetReact("wave", "本题已答完，成绩暂时未能保存。", 4); }
 }
}
