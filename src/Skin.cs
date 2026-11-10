using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;
public static class Skin {
 public static Color Ink=Color.FromArgb(54,45,37),Muted=Color.FromArgb(141,129,112),Gold=Color.FromArgb(242,185,68),Cream=Color.FromArgb(249,246,238),Line=Color.FromArgb(235,229,215),Green=Color.FromArgb(74,123,99);
 public static Font Font(float size, bool bold=false) { return new Font("Microsoft YaHei UI",size,bold?FontStyle.Bold:FontStyle.Regular); }
 public static GraphicsPath Round(RectangleF r,float radius) {
  var p=new GraphicsPath();float d=radius*2;p.AddArc(r.X,r.Y,d,d,180,90);p.AddArc(r.Right-d,r.Y,d,d,270,90);p.AddArc(r.Right-d,r.Bottom-d,d,d,0,90);p.AddArc(r.X,r.Bottom-d,d,d,90,90);p.CloseFigure();return p;
 }
 public static void Card(Graphics g, RectangleF r, Color fill, float radius) { using(var p=Round(r,radius))using(var b=new SolidBrush(fill))g.FillPath(b,p); }
 public static void Text(Graphics g,string text,Font font,Color color,RectangleF r,StringFormat format=null) {using(var b=new SolidBrush(color))g.DrawString(text,font,b,r,format??StringFormat.GenericDefault);}
 public static Button Button(string title,Action action,bool primary=false) {
  var b=new Button{Text=title,AutoSize=true,Height=36,MinimumSize=new Size(80,36),FlatStyle=FlatStyle.Flat,BackColor=primary?Gold:Color.White,ForeColor=Ink,Cursor=Cursors.Hand,Margin=new Padding(0,0,8,6),Padding=new Padding(8,2,8,2),Font=Font(9)};
  b.FlatAppearance.BorderColor=primary?Gold:Line;b.Click+=delegate{action();};return b;
 }
}
// Original vector character: all shapes are rendered at runtime, no external artwork required.
public static class KangarooArt {
 static Color Yellow=Color.FromArgb(248,198,65),Light=Color.FromArgb(255,227,114),Shade=Color.FromArgb(220,153,40),Brown=Color.FromArgb(112,61,32);
 static void Oval(Graphics g,RectangleF r,Color a,Color b,float angle=45) {using(var brush=new LinearGradientBrush(r,a,b,angle))g.FillEllipse(brush,r);}
 static void Path(Graphics g,PointF[] pts,Color a,Color b) {using(var p=new GraphicsPath()){p.AddBeziers(pts);p.CloseFigure();using(var br=new LinearGradientBrush(p.GetBounds(),a,b,55))g.FillPath(br,p);}}
 static PointF P(float x,float y){return new PointF(x,y);}
 public static void Draw(Graphics g,RectangleF bounds,double t,string pose) {
  var state=g.Save();g.SmoothingMode=SmoothingMode.AntiAlias;g.TextRenderingHint=TextRenderingHint.AntiAliasGridFit;g.TranslateTransform(bounds.X,bounds.Y);g.ScaleTransform(bounds.Width/260f,bounds.Height/290f);
  using(var b=new SolidBrush(Color.FromArgb(22,90,62,25)))g.FillEllipse(b,48,267,178,14);
  float lift=pose=="happy"?(float)Math.Abs(Math.Sin(t*7))*12:pose=="stretch"?(float)(Math.Sin(t*3)*3):0;
  g.TranslateTransform(0,(float)Math.Sin(t*2.2)*1.8f-lift);
  // Curved tail, large feet and rounded pear-shaped body.
  Path(g,new[]{P(170,212),P(224,241),P(226,190),P(244,185),P(258,194),P(238,263),P(175,253)},Shade,Yellow);
  Oval(g,new RectangleF(54,244,79,30),Light,Shade);Oval(g,new RectangleF(139,244,73,30),Yellow,Shade);
  Path(g,new[]{P(120,116),P(69,126),P(58,207),P(78,249),P(103,277),P(184,267),P(194,236),P(206,203),P(181,126),P(151,115)},Light,Yellow);
  Oval(g,new RectangleF(86,160,93,97),Color.FromArgb(255,235,151),Color.FromArgb(255,215,91));
  // Long ears, one angled outward.
  var ear=g.Save();g.TranslateTransform(104,67);g.RotateTransform((float)Math.Sin(t*1.4)*2-12);Oval(g,new RectangleF(-13,-65,30,79),Light,Shade);Oval(g,new RectangleF(-5,-54,14,50),Color.FromArgb(255,221,113),Color.FromArgb(236,167,63));g.Restore(ear);
  ear=g.Save();g.TranslateTransform(158,68);g.RotateTransform(31+(float)Math.Sin(t*1.2)*3);Oval(g,new RectangleF(-13,-65,31,79),Light,Shade);Oval(g,new RectangleF(-5,-53,14,49),Color.FromArgb(255,226,135),Color.FromArgb(236,167,63));g.Restore(ear);
  Path(g,new[]{P(105,49),P(73,53),P(83,91),P(60,112),P(29,143),P(56,171),P(92,168),P(130,170),P(182,154),P(181,108),P(183,75),P(164,49),P(142,50),P(128,49),P(116,47),P(105,49)},Light,Yellow);
  // Soft muzzle, white eyes with glossy brown pupils, oversized nose.
  Oval(g,new RectangleF(43,112,98,58),Color.FromArgb(255,229,119),Yellow);
  bool blink=pose=="sleep"||Math.Sin(t*.9)>.997;
  if(blink){using(var p=new Pen(Brown,3)){g.DrawArc(p,98,90,17,9,5,170);g.DrawArc(p,139,94,18,9,5,170);}}
  else {
   Oval(g,new RectangleF(96,79,28,34),Color.FromArgb(255,255,235),Color.FromArgb(255,241,185));Oval(g,new RectangleF(136,84,28,34),Color.FromArgb(255,255,235),Color.FromArgb(255,241,185));
   Oval(g,new RectangleF(105,88,16,23),Brown,Color.FromArgb(64,33,19));Oval(g,new RectangleF(145,93,16,23),Brown,Color.FromArgb(64,33,19));
   using(var b=new SolidBrush(Color.White)){g.FillEllipse(b,109,90,6,7);g.FillEllipse(b,149,95,6,7);}
  }
  Oval(g,new RectangleF(43,117,51,36),Color.FromArgb(159,85,35),Color.FromArgb(100,44,23));using(var b=new SolidBrush(Color.FromArgb(65,255,236,171)))g.FillEllipse(b,51,121,20,8);
  using(var p=new Pen(Color.FromArgb(164,105,37),2)){g.DrawArc(p,96,137,28,20,10,115);}
  using(var b=new SolidBrush(Color.FromArgb(70,245,145,79)))g.FillEllipse(b,148,122,20,11);
  // An actual pouch with a dark curved opening and stitched rim.
  Path(g,new[]{P(94,203),P(116,214),P(147,214),P(170,200),P(173,237),P(150,253),P(128,250),P(103,252),P(91,232),P(94,203)},Color.FromArgb(231,163,49),Color.FromArgb(249,197,76));
  using(var p=new Pen(Color.FromArgb(166,108,34),5)){p.StartCap=p.EndCap=LineCap.Round;g.DrawBezier(p,96,203,118,213,147,213,168,202);}
  using(var p=new Pen(Color.FromArgb(255,224,132),2)){g.DrawBezier(p,99,214,104,246,145,255,163,217);}
  using(var b=new SolidBrush(Color.FromArgb(255,236,160)))g.FillEllipse(b,127,227,8,8);
  // Left arm resting by the pouch; right arm waves, stretches or holds a mug.
  var arm=g.Save();g.TranslateTransform(83,168);g.RotateTransform(pose=="stretch"?-80:-13);Oval(g,new RectangleF(-14,0,27,59),Light,Shade);g.Restore(arm);
  arm=g.Save();g.TranslateTransform(174,169);float angle=-18;
  if(pose=="wave"||pose=="happy")angle=-140+(float)Math.Sin(t*10)*15;
  if(pose=="stretch")angle=-155;
  if(pose=="eyes")angle=-155+(float)Math.Sin(t*3)*8;
  if(pose=="water")angle=-105;
  g.RotateTransform(angle);Oval(g,new RectangleF(-14,0,29,58),Light,Shade);g.Restore(arm);
  if(pose=="water") {
   Skin.Card(g,new RectangleF(170,127,28,32),Color.FromArgb(215,237,229),6);
   using(var pen=new Pen(Skin.Green,3))g.DrawArc(pen,189,133,16,18,-100,230);
   using(var pen=new Pen(Color.FromArgb(116,171,183),2)){g.DrawLine(pen,177,119,179,111);g.DrawLine(pen,186,121,189,112);}
  }
  if(pose=="happy")using(var f=Skin.Font(16,true))Skin.Text(g,"✦",f,Shade,new RectangleF(203,78,35,35));
  g.Restore(state);
 }
 public static Bitmap Preview(string pose,int width=520,int height=580) {var b=new Bitmap(width,height,System.Drawing.Imaging.PixelFormat.Format32bppArgb);using(var g=Graphics.FromImage(b)){g.Clear(Color.Transparent);Draw(g,new RectangleF(0,0,width,height),1.2,pose);}return b;}
}
