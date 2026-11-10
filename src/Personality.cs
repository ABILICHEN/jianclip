using System;
public static class Personality {
 static readonly Random random = new Random();
 public static string Line(string context, AppSettings settings) {
  if (!settings.SassyMode) return context == "copy" ? "收到啦，已经装进口袋！" : context == "wrong" ? "没关系，看看解释再试下一题。" : "慢慢来，学到一点就是赚到。";
  int level = settings.SassyLevel;
  string[] lines;
  if (context == "copy") lines = level == 1 ? new[] { "又收好一条。你的记忆，我的口袋。", "你负责复制，我负责不让它失踪。", "口袋还没满，放心，再来点。" } : level == 2 ? new[] { "这段也记不住？行，袋袋替你保管。", "你复制的速度，已经超过消化的速度了。", "脑子先歇着，我的口袋上班。" } : new[] { "脑子负责放空，口袋负责存档，分工明确。", "又是复制粘贴的一天，键盘都快包浆了。", "你这知识不过脑，倒是很会过剪贴板。" };
  else if (context == "wrong") lines = level == 1 ? new[] { "噗，脑回路拐了个小弯。", "没猜中，想象力倒是挺在线。", "答案藏得不深，你绕得挺远。" } : level == 2 ? new[] { "你成功绕开了答案，反向导航满分。", "这脑回路，导航都得重新规划。", "答错得这么自信，袋袋差点都信了。" } : new[] { "知识从脑袋路过，你连个招呼都没打。", "正确答案就在旁边，你偏要走风景线。", "这题都能被你绕过去，迷宫见了也得拜师。" };
  else lines = level == 1 ? new[] { "知识来敲门啦，先别装不在家。", "学一点点，别让今天只剩复制粘贴。" } : level == 2 ? new[] { "脑袋别只当耳机支架，来装点知识。", "给脑子加点料，别只给口袋加库存。" } : new[] { "刷了这么久，脑袋总该缓存点有用的了。", "别光让电脑更新，脑袋也该升个级。" };
  return lines[random.Next(lines.Length)];
 }
 public static string Answer(QuizAttempt attempt, AppSettings settings) {
  if (attempt.Correct) return attempt.Feedback(settings.PlayfulFeedback);
  if (!settings.SassyMode) return settings.PlayfulFeedback ? "这题藏了个小转弯！看看解释，下次就会啦。" : attempt.Feedback(false);
  return Line("wrong", settings);
 }
}
