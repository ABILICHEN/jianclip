using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Web.Script.Serialization;
public static class KnowledgeTests {
 static int passed;
 static void Check(bool ok, string name) { if (!ok) throw new Exception(name); passed++; Console.WriteLine("PASS: " + name); }
 static AppSettings Only(string type) { return new AppSettings { UseExtendedCatalog=false, PoetryEnabled=type=="poem", QuotesEnabled=type=="quote", WordsEnabled=type=="word", FactsEnabled=type=="fact", TipsEnabled=type=="tip", QuizEnabled=type=="quiz", QuietAtNight=false }; }
 public static int Run(string folder) {
  passed=0;
  using(var library=new KnowledgeLibrary()) {
   Check(library.Items.Count==82 && library.Items.Select(x=>x.Category).Distinct().Count()==6,"Curated offline collection covers six categories");
   Check(library.Total>=250000 && library.Catalog.Count("poem")>76000 && library.Catalog.Count("word")>170000,"Bundled offline collection exceeds 250k entries");
   Check(library.Catalog.Count("word",true)>16000,"Common vocabulary pool exceeds 16k words");
   var rng=new Random(19);
   var poem=library.Catalog.Sample("poem",rng,null);
   Check(poem.DetailHeading=="原诗与阅读提示"&&poem.Detail.Contains("暂未配备逐篇赏析")&&poem.Source.StartsWith("https://"),"Bulk poetry labels reading guides without fabricating analysis");
   var word=library.Catalog.Sample("word",rng,null,true);
   Check(word.CommonWord&&!string.IsNullOrEmpty(word.Pronunciation)&&!string.IsNullOrEmpty(word.SpeakText),"Common dictionary sample contains Chinese and pronunciation");
   foreach(var type in new[]{"poem","quote","word","fact","tip","quiz"}) {
    var deck=new KnowledgeDeck(library,new Random(3));var settings=Only(type);int count=library.Items.Count(x=>x.Category==type);var ids=new HashSet<string>();
    for(int i=0;i<count;i++){var item=deck.Next(settings);if(item.Category!=type)throw new Exception("Category filter failed");ids.Add(item.Id);}Check(ids.Count==count,"Shuffle bag avoids repeats within "+type+" cycle");
   }
   var none=Only("none");var d=new KnowledgeDeck(library,new Random(9));Check(d.Next(none)==null,"No selected categories means no content");
   var settingsAll=new AppSettings{UseExtendedCatalog=false,QuizPercent=0};Check(Enumerable.Range(0,100).All(i=>d.Next(settingsAll).Category!="quiz"),"Zero quiz chance excludes quizzes in mixed mode");settingsAll.QuizPercent=100;Check(Enumerable.Range(0,20).All(i=>d.Next(settingsAll).Category=="quiz"),"100 percent quiz chance selects quizzes");
   var quiz=library.Items.First(x=>x.Category=="quiz");var attempt=new QuizAttempt(quiz,new Random(7));Check(attempt.Options[attempt.CorrectIndex]==quiz.Options[quiz.AnswerIndex],"Shuffled quiz preserves correct answer");Check(attempt.Answer(attempt.CorrectIndex)&&attempt.Correct&&!attempt.Answer(0),"Quiz accepts only the first submitted answer");
   var wrong=new QuizAttempt(quiz,new Random(7));wrong.Answer((wrong.CorrectIndex+1)%wrong.Options.Length);var friendly=new AppSettings{SassyMode=false};Check(Personality.Answer(wrong,friendly).Contains("看看解释"),"Sassy mode is opt-in and friendly by default");friendly.SassyMode=true;friendly.SassyLevel=3;Check(!string.IsNullOrEmpty(Personality.Answer(wrong,friendly)),"Enabled sassy mode provides level-specific feedback");
   DateTime now=new DateTime(2026,11,10,10,0,0);var timing=new AppSettings{KnowledgeMinutes=5,PoetryDelaySeconds=8,KnowledgeCardSeconds=10};var clock=new KnowledgeClock(timing,now);
   Check(!clock.TryDue(timing,now.AddMinutes(4),false,false)&&clock.TryDue(timing,now.AddMinutes(5),false,false),"User-defined five-minute schedule fires at deadline");
   Check(!clock.TryDue(timing,now.AddMinutes(5),false,false),"Schedule does not fire repeatedly at same deadline");
   Check(!clock.TryDue(timing,now.AddMinutes(11),true,false)&&!clock.TryDue(timing,now.AddMinutes(11),false,true),"Idle or busy desktop suppresses scheduled knowledge");
   Check(clock.TryDue(timing,now.AddMinutes(20),false,false)&&clock.NextDue==now.AddMinutes(25),"Resume schedules one card without catch-up storm");
   Check(!clock.TryDue(timing,new DateTime(2026,11,10,23,0,0),false,false),"Knowledge respects night quiet hours");timing.KnowledgeEnabled=false;Check(!clock.TryDue(timing,now.AddHours(1),false,false),"Disabling knowledge stops automatic pushes");
   timing.KnowledgeEnabled=true;var display=new KnowledgePresentation(library.Items.First(x=>x.Category=="poem"),timing,now);display.Advance(now.AddSeconds(7),false);Check(!display.DetailVisible,"Poem keeps analysis hidden before configured delay");display.Advance(now.AddSeconds(8),false);Check(display.DetailVisible&&display.ExpiresAt>=now.AddSeconds(23),"Eight-second reveal leaves at least fifteen seconds for analysis");
   var hover=new KnowledgePresentation(library.Items.First(x=>x.Category=="word"),timing,now);Check(!hover.Advance(now.AddSeconds(50),true),"Hover prevents card dismissal");hover.Pinned=true;Check(!hover.Advance(now.AddHours(1),false),"Pinned card remains until user closes it");
   var stats=new KnowledgeStats();stats.Record(true);stats.Record(true);stats.Record(false);stats.Save(folder);stats=KnowledgeStats.Load(folder);Check(stats.Answered==3&&stats.Correct==2&&stats.Streak==0&&stats.BestStreak==2,"Quiz progress persists and resets streak after mistake");
   timing.KnowledgeMinutes=7;timing.PoetryDelaySeconds=9;timing.SassyMode=true;timing.SassyLevel=2;timing.Save(folder);var loaded=AppSettings.Load(folder);Check(loaded.KnowledgeMinutes==7&&loaded.PoetryDelaySeconds==9&&loaded.SassyMode&&loaded.SassyLevel==2,"Knowledge timing and sassy preference roundtrip");
   File.WriteAllText(Path.Combine(folder,"settings.json"),"{\"WaterMinutes\":45,\"PetVisible\":true}");loaded=AppSettings.Load(folder);Check(loaded.KnowledgeMinutes==5&&loaded.PoetryDelaySeconds==8&&!loaded.SassyMode,"Legacy settings migrate with safe knowledge defaults");
   using(var speaker=new WordSpeaker()){Check(speaker.Speak("")!=null,"Empty speech input is handled without audio");speaker.Dispose();Check(speaker.Speak("word")!=null,"Disposed speaker fails gracefully");}
  }
  return passed;
 }
}
