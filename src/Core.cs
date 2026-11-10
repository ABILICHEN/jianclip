using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web.Script.Serialization;

public class Clip {
 public string Kind { get; set; }
 public string Value { get; set; }
 public DateTime Time { get; set; }
 public bool Pinned { get; set; }
 public override string ToString() { return (Pinned ? "★ " : "") + (Kind == "image" ? "图片" : Value); }
 public string Summary { get { return Kind == "image" ? "一张图片" : Shorten(Value, 100); } }
 public static string Shorten(string value, int length) {
  string s = (value ?? "").Replace("\r", " ").Replace("\n", "  ").Replace("\t", " ");
  return s.Length > length ? s.Substring(0, length) + "…" : s;
 }
}
public class HistoryStore {
 public List<Clip> Items = new List<Clip>();
 public readonly string Folder;
 public string Warning;
 public HistoryStore(string folder) { Folder = folder; Directory.CreateDirectory(folder); }
 public bool Add(Clip item) {
  var old = Items.FirstOrDefault(x => x.Kind == item.Kind && x.Value == item.Value);
  if (old != null) { old.Time = item.Time; return true; }
  // Keep favorites intact. Reject a new item when the protected collection is full.
  var remaining = Items.ToList(); remaining.Add(item);
  while (remaining.Count > 200 || remaining.Sum(x => (long)x.Value.Length) > 16000000) {
   var victim = remaining.Where(x => !x.Pinned).OrderBy(x => x.Time).FirstOrDefault();
   if (victim == null || victim == item) return false;
   remaining.Remove(victim);
  }
  Items = remaining; return true;
 }
 public void Save() {
  string json = new JavaScriptSerializer { MaxJsonLength = 64000000 }.Serialize(Items);
  byte[] bytes = ProtectedData.Protect(Encoding.UTF8.GetBytes(json), null, DataProtectionScope.CurrentUser);
  string path = Path.Combine(Folder, "history.dat");
  File.WriteAllBytes(path + ".tmp", bytes);
  if (File.Exists(path)) File.Replace(path + ".tmp", path, null); else File.Move(path + ".tmp", path);
 }
 public void Load() {
  string path = Path.Combine(Folder, "history.dat"); if (!File.Exists(path)) return;
  try {
   byte[] bytes = ProtectedData.Unprotect(File.ReadAllBytes(path), null, DataProtectionScope.CurrentUser);
   var loaded = new JavaScriptSerializer { MaxJsonLength = 64000000 }.Deserialize<List<Clip>>(Encoding.UTF8.GetString(bytes));
   Items = (loaded ?? new List<Clip>()).Where(x => x != null && (x.Kind == "text" || x.Kind == "image") && x.Value != null).ToList();
  } catch (Exception) {
   string backup = path + ".unreadable-" + DateTime.Now.ToString("yyyyMMddHHmmssfff");
   File.Copy(path, backup, false); Warning = "历史文件无法读取，已保留备份：" + backup;
  }
 }
}
public class AppSettings {
 public bool PetVisible = true, RemindersEnabled = true, WaterEnabled = true, EyesEnabled = true, StretchEnabled = false;
 public bool QuietAtNight = true, Animations = true;
 public int WaterMinutes = 45, EyesMinutes = 20, StretchMinutes = 60;
 public int PetX = int.MinValue, PetY = int.MinValue;
 public void Validate() { WaterMinutes = Clamp(WaterMinutes); EyesMinutes = Clamp(EyesMinutes); StretchMinutes = Clamp(StretchMinutes); }
 static int Clamp(int n) { return Math.Max(1, Math.Min(240, n)); }
 public static AppSettings Load(string folder) {
  try { var s = new JavaScriptSerializer().Deserialize<AppSettings>(File.ReadAllText(Path.Combine(folder, "settings.json"))); s.Validate(); return s; }
  catch { return new AppSettings(); }
 }
 public void Save(string folder) {
  Validate(); string path = Path.Combine(folder, "settings.json");
  File.WriteAllText(path + ".tmp", new JavaScriptSerializer().Serialize(this), Encoding.UTF8);
  if (File.Exists(path)) File.Replace(path + ".tmp", path, null); else File.Move(path + ".tmp", path);
 }
}
public class ReminderClock {
 readonly Dictionary<string, DateTime> due = new Dictionary<string, DateTime>();
 public DateTime SnoozedUntil = DateTime.MinValue;
 DateTime nextAllowed = DateTime.MinValue;
 public ReminderClock(AppSettings s, DateTime now) { Reset(s, now); }
 public void Reset(AppSettings s, DateTime now) { due["water"] = now.AddMinutes(s.WaterMinutes); due["eyes"] = now.AddMinutes(s.EyesMinutes); due["stretch"] = now.AddMinutes(s.StretchMinutes); }
 public void Snooze(DateTime now, string key = null) { SnoozedUntil = now.AddMinutes(10); if (key != null && due.ContainsKey(key)) due[key] = SnoozedUntil; }
 public string Next(AppSettings s, DateTime now, bool idle) {
  if (!s.RemindersEnabled || idle || now < SnoozedUntil || now < nextAllowed || (s.QuietAtNight && (now.Hour < 8 || now.Hour >= 22))) return null;
  foreach (string key in new[] { "water", "eyes", "stretch" }) {
   bool enabled = key == "water" ? s.WaterEnabled : key == "eyes" ? s.EyesEnabled : s.StretchEnabled;
   int minutes = key == "water" ? s.WaterMinutes : key == "eyes" ? s.EyesMinutes : s.StretchMinutes;
   if (now >= due[key]) { due[key] = now.AddMinutes(minutes); if (enabled) { nextAllowed = now.AddMinutes(2); return key; } }
  }
  return null;
 }
}
