using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Speech.Synthesis;
using System.Web.Script.Serialization;

public class KnowledgeItem {
 public string Id { get; set; }
 public string Category { get; set; }
 public string Title { get; set; }
 public string Body { get; set; }
 public string Detail { get; set; }
 public string DetailHeading { get; set; }
 public bool CommonWord { get; set; }
 public string Attribution { get; set; }
 public string Source { get; set; }
 public string Pronunciation { get; set; }
 public string SpeakText { get; set; }
 public string[] Options { get; set; }
 public int AnswerIndex { get; set; }
 public string CategoryName { get { return Category == "poem" ? "一口诗意" : Category == "quote" ? "名言金句" : Category == "word" ? "每天一个词" : Category == "fact" ? "有趣百科" : Category == "tip" ? "生活小妙招" : "脑袋转个弯"; } }
}
public class KnowledgeLibrary : IDisposable {
 public readonly OfflineCatalog Catalog;
 public int Total { get { return Items.Count + Catalog.Total; } }
 public readonly List<KnowledgeItem> Items;
 public KnowledgeLibrary() {
  using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("JianClip.Knowledge.json")) {
   if (stream == null) throw new InvalidDataException("内置知识库未找到。");
   using (var reader = new StreamReader(stream)) Items = new JavaScriptSerializer().Deserialize<List<KnowledgeItem>>(reader.ReadToEnd());
  }
  Validate(Items); Catalog = new OfflineCatalog();
 }
 public void Dispose() { Catalog.Dispose(); }
 public static void Validate(List<KnowledgeItem> items) {
  if (items == null || items.Count == 0) throw new InvalidDataException("知识库为空。");
  var ids = new HashSet<string>();
  foreach (var item in items) {
   if (item == null || string.IsNullOrWhiteSpace(item.Id) || !ids.Add(item.Id)) throw new InvalidDataException("知识条目 ID 重复或为空。");
   if (!new[] { "poem", "quote", "word", "fact", "tip", "quiz" }.Contains(item.Category)) throw new InvalidDataException("未知内容分类。");
   if (string.IsNullOrWhiteSpace(item.Title) || string.IsNullOrWhiteSpace(item.Body) || string.IsNullOrWhiteSpace(item.Detail)) throw new InvalidDataException("知识条目缺少标题、内容或解释。");
   if (item.Category == "word" && (string.IsNullOrWhiteSpace(item.SpeakText) || string.IsNullOrWhiteSpace(item.Pronunciation))) throw new InvalidDataException("单词缺少发音信息。");
   if (item.Category == "quiz" && (item.Options == null || item.Options.Length < 2 || item.Options.Length > 4 || item.Options.Any(string.IsNullOrWhiteSpace) || item.AnswerIndex < 0 || item.AnswerIndex >= item.Options.Length || item.Options.Distinct().Count() != item.Options.Length)) throw new InvalidDataException("互动题选项不完整。");
   Uri uri;
   if (!string.IsNullOrWhiteSpace(item.Source) && (!Uri.TryCreate(item.Source, UriKind.Absolute, out uri) || uri.Scheme != "https")) throw new InvalidDataException("来源链接必须为 HTTPS。");
  }
 }
}
public class KnowledgeDeck {
 readonly KnowledgeLibrary library; readonly Random random;
 readonly Dictionary<string, Queue<KnowledgeItem>> bags = new Dictionary<string, Queue<KnowledgeItem>>();
 string lastId, lastCategory;
 public KnowledgeDeck(KnowledgeLibrary library, Random random = null) { this.library = library; this.random = random ?? new Random(); }
 public KnowledgeItem Next(AppSettings settings, string forcedCategory = null) {
  string category = forcedCategory;
  if (category == null) {
   var types = new List<string>();
   if (settings.PoetryEnabled) types.Add("poem"); if (settings.QuotesEnabled) types.Add("quote"); if (settings.WordsEnabled) types.Add("word");
   if (settings.FactsEnabled) types.Add("fact"); if (settings.TipsEnabled) types.Add("tip");
   if (settings.QuizEnabled && (types.Count == 0 || random.Next(100) < settings.QuizPercent)) category = "quiz";
   else {
    if (types.Count == 0) return null;
    var alternatives = types.Where(t => t != lastCategory).ToArray();
    category = alternatives.Length == 0 ? types[0] : alternatives[random.Next(alternatives.Length)];
   }
  }
  if (settings.UseExtendedCatalog && (category == "poem" || category == "word") && random.Next(100) < (category == "poem" && settings.PreferCuratedPoetry ? 30 : 90)) {
   var extended = library.Catalog.Sample(category, random, lastId, category == "word" && settings.PreferCommonWords);
   if (extended != null) { lastId = extended.Id; lastCategory = category; return extended; }
  }
  Queue<KnowledgeItem> bag;
  if (!bags.TryGetValue(category, out bag) || bag.Count == 0) {
   var entries = library.Items.Where(x => x.Category == category).ToList();
   for (int i = entries.Count - 1; i > 0; i--) { int j = random.Next(i + 1); var a = entries[i]; entries[i] = entries[j]; entries[j] = a; }
   if (entries.Count > 1 && entries[0].Id == lastId) { var a = entries[0]; entries[0] = entries[1]; entries[1] = a; }
   bag = new Queue<KnowledgeItem>(entries); bags[category] = bag;
  }
  if (bag.Count == 0) return null;
  var item = bag.Dequeue(); lastId = item.Id; lastCategory = category; return item;
 }
}
public class KnowledgeClock {
 public DateTime NextDue { get; private set; }
 public KnowledgeClock(AppSettings settings, DateTime now) { Reset(settings, now); }
 public void Reset(AppSettings settings, DateTime now) { NextDue = now.AddMinutes(settings.KnowledgeMinutes); }
 public bool TryDue(AppSettings settings, DateTime now, bool idle, bool busy) {
  if (!settings.KnowledgeEnabled || !settings.HasKnowledgeTypes || idle || busy || now < NextDue || (settings.QuietAtNight && (now.Hour < 8 || now.Hour >= 22))) return false;
  Reset(settings, now); return true;
 }
}
public class QuizAttempt {
 public readonly KnowledgeItem Item;
 public readonly string[] Options;
 public readonly int CorrectIndex;
 public bool Answered { get; private set; }
 public int SelectedIndex { get; private set; }
 public bool Correct { get { return Answered && SelectedIndex == CorrectIndex; } }
 public QuizAttempt(KnowledgeItem item, Random random = null) {
  if (item.Category != "quiz") throw new ArgumentException("不是互动题。");
  Item = item; var rng = random ?? new Random(); var order = Enumerable.Range(0, item.Options.Length).ToArray();
  for (int i = order.Length - 1; i > 0; i--) { int j = rng.Next(i + 1); int a = order[i]; order[i] = order[j]; order[j] = a; }
  Options = order.Select(i => item.Options[i]).ToArray(); CorrectIndex = Array.IndexOf(order, item.AnswerIndex); SelectedIndex = -1;
 }
 public bool Answer(int choice) { if (Answered || choice < 0 || choice >= Options.Length) return false; SelectedIndex = choice; Answered = true; return true; }
 public string Feedback(bool playful, Random random = null) {
  if (!Answered) return "先选一个答案吧。";
  if (Correct) return "答对啦！你的脑袋比我的口袋还灵光。";
  if (!playful) return "差一点点！换个角度想想，答案在下面。";
  string[] lines = { "噗，脑袋刚才是不是偷偷去喝奶茶了？", "这题你成功绕开了正确答案，反向导航满分！", "我把答案装进口袋了，你把脑回路打了个结。", "差一点就对了——袋袋决定给你的想象力加一分。" };
  return lines[(random ?? new Random()).Next(lines.Length)];
 }
}
public class KnowledgeStats {
 public int Answered, Correct, Streak, BestStreak;
 public void Record(bool correct) { Answered++; if (correct) { Correct++; Streak++; BestStreak = Math.Max(BestStreak, Streak); } else Streak = 0; }
 public string Summary { get { return "已答 " + Answered + " 题 · 答对 " + Correct + " 题 · 最佳连对 " + BestStreak; } }
 public static KnowledgeStats Load(string folder) { try { return new JavaScriptSerializer().Deserialize<KnowledgeStats>(File.ReadAllText(Path.Combine(folder, "knowledge-progress.json"))) ?? new KnowledgeStats(); } catch { return new KnowledgeStats(); } }
 public void Save(string folder) { string p = Path.Combine(folder, "knowledge-progress.json"); File.WriteAllText(p + ".tmp", new JavaScriptSerializer().Serialize(this)); if (File.Exists(p)) File.Replace(p + ".tmp", p, null); else File.Move(p + ".tmp", p); }
}
public sealed class WordSpeaker : IDisposable {
 SpeechSynthesizer voice; bool disposed;
 public string Speak(string word) {
  if (disposed || string.IsNullOrWhiteSpace(word)) return "当前无法发音。";
  try {
   if (voice == null) {
    voice = new SpeechSynthesizer();
    var english = voice.GetInstalledVoices().FirstOrDefault(v => v.Enabled && v.VoiceInfo.Culture.TwoLetterISOLanguageName == "en");
    if (english == null) { voice.Dispose(); voice = null; return "未找到英语语音，请在 Windows 语音设置中安装英语语音。"; }
    voice.SelectVoice(english.VoiceInfo.Name); voice.SetOutputToDefaultAudioDevice(); voice.Rate = -1;
   }
   voice.SpeakAsyncCancelAll(); voice.SpeakAsync(word); return null;
  } catch (Exception) { return "暂时无法发音，请检查英语语音与音频设备。"; }
 }
 public void Stop() { if (voice != null) try { voice.SpeakAsyncCancelAll(); } catch (ObjectDisposedException) { } }
 public void Dispose() { if (disposed) return; disposed = true; if (voice != null) { Stop(); voice.Dispose(); voice = null; } }
}
public class KnowledgePresentation {
 public readonly KnowledgeItem Item;
 public bool DetailVisible { get; private set; }
 public bool Pinned;
 public System.DateTime DetailDue { get; private set; }
 public System.DateTime ExpiresAt { get; private set; }
 System.DateTime lastTick;
 public KnowledgePresentation(KnowledgeItem item, AppSettings settings, System.DateTime now) {
  Item = item; lastTick = now; DetailDue = now.AddSeconds(settings.PoetryDelaySeconds);
  DetailVisible = item.Category != "poem" && item.Category != "quiz";
  ExpiresAt = now.AddSeconds(System.Math.Max(settings.KnowledgeCardSeconds, item.Category == "poem" ? settings.PoetryDelaySeconds + 15 : 10));
 }
 public bool Advance(System.DateTime now, bool hovered) {
  double elapsed = System.Math.Max(0, (now - lastTick).TotalSeconds); lastTick = now;
  if (hovered || Pinned) ExpiresAt = ExpiresAt.AddSeconds(elapsed);
  if (Item.Category == "poem" && now >= DetailDue) DetailVisible = true;
  return now >= ExpiresAt && !hovered && !Pinned;
 }
 public void Reveal(System.DateTime now) { DetailVisible = true; ExpiresAt = now.AddSeconds(20); }
}
