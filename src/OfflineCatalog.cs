using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Web.Script.Serialization;
public class CatalogBlock {
 public string Name { get; set; }
 public string Category { get; set; }
 public int Count { get; set; }
 public bool CommonWord { get; set; }
}
public class CatalogManifest {
 public int Format { get; set; }
 public int Total { get; set; }
 public List<CatalogBlock> Blocks { get; set; }
}
public sealed class OfflineCatalog : IDisposable {
 readonly ZipArchive archive;
 readonly Dictionary<string, KnowledgeItem[]> cached = new Dictionary<string, KnowledgeItem[]>();
 readonly Dictionary<string, string> cachedName = new Dictionary<string, string>();
 public readonly CatalogManifest Manifest;
 public int Total { get { return Manifest.Total; } }
 public int Count(string category, bool commonOnly = false) { return Manifest.Blocks.Where(b => b.Category == category && (!commonOnly || b.CommonWord)).Sum(b => b.Count); }
 public OfflineCatalog() {
  var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("JianClip.Catalog.zip");
  if (stream == null) throw new InvalidDataException("扩展离线库未找到。");
  archive = new ZipArchive(stream, ZipArchiveMode.Read, false);
  using (var reader = new StreamReader(archive.GetEntry("manifest.json").Open())) Manifest = new JavaScriptSerializer().Deserialize<CatalogManifest>(reader.ReadToEnd());
  if (Manifest == null || Manifest.Format != 1 || Manifest.Blocks == null || Manifest.Total != Manifest.Blocks.Sum(b => b.Count)) throw new InvalidDataException("扩展离线库目录损坏。");
 }
 public KnowledgeItem Sample(string category, Random random, string lastId, bool commonOnly = false) {
  var blocks = Manifest.Blocks.Where(b => b.Category == category && (!commonOnly || b.CommonWord)).ToArray();
  if (blocks.Length == 0) return null;
  int index = random.Next(blocks.Sum(b => b.Count)); CatalogBlock selected = blocks[0];
  foreach (var block in blocks) { if (index < block.Count) { selected = block; break; } index -= block.Count; }
  KnowledgeItem[] items; string name;
  if (!cached.TryGetValue(category, out items) || !cachedName.TryGetValue(category, out name) || name != selected.Name) {
   var entry = archive.GetEntry(selected.Name); if (entry == null) throw new InvalidDataException("扩展离线库分块未找到。");
   using (var reader = new StreamReader(entry.Open())) items = new JavaScriptSerializer { MaxJsonLength = 16000000 }.Deserialize<KnowledgeItem[]>(reader.ReadToEnd());
   cached[category] = items; cachedName[category] = selected.Name;
  }
  if (items.Length == 0) return null;
  var item = items[random.Next(items.Length)];
  if (items.Length > 1 && item.Id == lastId) item = items[(Array.IndexOf(items, item) + 1) % items.Length];
  return item;
 }
 public void Dispose() { archive.Dispose(); cached.Clear(); }
}
