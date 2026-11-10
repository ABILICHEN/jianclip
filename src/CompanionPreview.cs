using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
public static class CompanionPreview {
 static void Save(Form form, string name) { using (var b = new Bitmap(form.Width, form.Height)) { form.DrawToBitmap(b, new Rectangle(Point.Empty, form.Size)); b.Save("docs/" + name + ".png"); } }
 public static void Export(ClipForm app) {
  bool auto = app.Settings.AutoPronounce, sassy = app.Settings.SassyMode; int level = app.Settings.SassyLevel;
  app.Settings.AutoPronounce = false; app.Settings.SassyMode = true; app.Settings.SassyLevel = 2;
  using (var settings = new SettingsForm(app)) { settings.Show(); settings.SelectKnowledgeTab(); Save(settings, "knowledge-settings"); settings.SelectKnowledgeTab(2); Save(settings, "personality-settings"); }
  using (var poem = new KnowledgeToast(app, app.Library.Items.First(i => i.Category == "poem"))) { poem.Show(); Save(poem, "poem-card"); poem.ShowAnalysisNow(); Save(poem, "poem-analysis"); }
  using (var word = new KnowledgeToast(app, app.Library.Items.First(i => i.Category == "word"))) { word.Show(); Save(word, "word-card"); }
  using (var quiz = new KnowledgeToast(app, app.Library.Items.First(i => i.Category == "quiz"))) { quiz.Show(); Save(quiz, "quiz-card"); quiz.SubmitAnswer((quiz.Attempt.CorrectIndex + 1) % quiz.Attempt.Options.Length); Save(quiz, "quiz-feedback"); }
  app.Settings.AutoPronounce = auto; app.Settings.SassyMode = sassy; app.Settings.SassyLevel = level;
 }
}
