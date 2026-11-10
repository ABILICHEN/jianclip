using System;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
public static class Native {
 [DllImport("user32.dll")] public static extern bool AddClipboardFormatListener(IntPtr h);
 [DllImport("user32.dll")] public static extern bool RemoveClipboardFormatListener(IntPtr h);
 [DllImport("user32.dll")] public static extern bool RegisterHotKey(IntPtr h, int id, uint mod, uint key);
 [DllImport("user32.dll")] public static extern bool UnregisterHotKey(IntPtr h, int id);
 [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
 [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
 [DllImport("user32.dll")] public static extern uint GetClipboardSequenceNumber();
 [DllImport("user32.dll")] public static extern bool IsWindow(IntPtr h);
 [DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(IntPtr h, out uint pid);
 [DllImport("user32.dll")] static extern bool GetLastInputInfo(ref LASTINPUTINFO info);
 [DllImport("user32.dll")] public static extern short GetAsyncKeyState(int key);
 [DllImport("user32.dll")] public static extern bool SetProcessDPIAware();
 [DllImport("user32.dll", CharSet=CharSet.Unicode)] public static extern IntPtr SendMessage(IntPtr h, int msg, IntPtr w, string l);
 [StructLayout(LayoutKind.Sequential)] struct LASTINPUTINFO { public uint cbSize, dwTime; }
 public static bool External(IntPtr h) { uint pid; GetWindowThreadProcessId(h, out pid); return h != IntPtr.Zero && pid != Process.GetCurrentProcess().Id; }
 public static bool Idle() { var i = new LASTINPUTINFO(); i.cbSize = (uint)Marshal.SizeOf(i); return GetLastInputInfo(ref i) && unchecked((uint)Environment.TickCount - i.dwTime) > 300000; }
 public static bool ModifiersDown() { return (GetAsyncKeyState(0x11) & 0x8000) != 0 || (GetAsyncKeyState(0x12) & 0x8000) != 0 || (GetAsyncKeyState(0x10) & 0x8000) != 0; }
 public static bool Excluded(IDataObject d) {
  if (d.GetDataPresent("ExcludeClipboardContentFromMonitorProcessing")) return true;
  if (!d.GetDataPresent("CanIncludeInClipboardHistory")) return false;
  object v = d.GetData("CanIncludeInClipboardHistory"); byte[] b = v as byte[];
  var stream = v as System.IO.MemoryStream; if (stream != null) b = stream.ToArray();
  return (b != null && b.Length >= 4 && BitConverter.ToInt32(b, 0) == 0) || (v is int && (int)v == 0);
 }
 [StructLayout(LayoutKind.Sequential)] public struct POINT { public int x,y; public POINT(int a,int b){x=a;y=b;} }
 [StructLayout(LayoutKind.Sequential)] public struct SIZE { public int cx,cy; public SIZE(int a,int b){cx=a;cy=b;} }
 [StructLayout(LayoutKind.Sequential,Pack=1)] public struct BLEND { public byte op,flags,alpha,format; }
 [DllImport("user32.dll")] public static extern IntPtr GetDC(IntPtr h);
 [DllImport("user32.dll")] public static extern int ReleaseDC(IntPtr h,IntPtr dc);
 [DllImport("gdi32.dll")] public static extern IntPtr CreateCompatibleDC(IntPtr dc);
 [DllImport("gdi32.dll")] public static extern bool DeleteDC(IntPtr dc);
 [DllImport("gdi32.dll")] public static extern IntPtr SelectObject(IntPtr dc,IntPtr obj);
 [DllImport("gdi32.dll")] public static extern bool DeleteObject(IntPtr obj);
 [DllImport("user32.dll")] public static extern bool UpdateLayeredWindow(IntPtr h,IntPtr dc,ref POINT pt,ref SIZE size,IntPtr src,ref POINT origin,int key,ref BLEND blend,int flags);
}
public abstract class LayeredForm : Form {
 protected LayeredForm() { FormBorderStyle=FormBorderStyle.None; ShowInTaskbar=false; TopMost=true; StartPosition=FormStartPosition.Manual; }
 protected override bool ShowWithoutActivation { get { return true; } }
 protected override CreateParams CreateParams { get { var p=base.CreateParams;p.ExStyle|=0x80000|0x80|0x8000000;return p; } }
 protected override void WndProc(ref Message m) { if(m.Msg==0x21){m.Result=new IntPtr(3);return;}base.WndProc(ref m); }
 public void Present(Bitmap image) {
  IntPtr screen=Native.GetDC(IntPtr.Zero),mem=Native.CreateCompatibleDC(screen),bitmap=IntPtr.Zero,old=IntPtr.Zero;
  try { bitmap=image.GetHbitmap(Color.FromArgb(0));old=Native.SelectObject(mem,bitmap);var pt=new Native.POINT(Left,Top);var size=new Native.SIZE(image.Width,image.Height);var origin=new Native.POINT(0,0);var blend=new Native.BLEND{alpha=255,format=1};Native.UpdateLayeredWindow(Handle,screen,ref pt,ref size,mem,ref origin,0,ref blend,2); }
  finally { if(old!=IntPtr.Zero)Native.SelectObject(mem,old);if(bitmap!=IntPtr.Zero)Native.DeleteObject(bitmap);Native.DeleteDC(mem);Native.ReleaseDC(IntPtr.Zero,screen); }
 }
}
