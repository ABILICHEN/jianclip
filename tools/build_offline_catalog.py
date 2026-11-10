"""Build the bundled, chunked offline library from upstream MIT datasets.
Input cache contains original source JSON files, Windows-simplified .simple copies,
ecdict.csv and poetry-tree.json. This developer tool is not needed to run JianClip.
"""
import argparse,csv,hashlib,json,re,zipfile
from pathlib import Path
from collections import Counter
parser=argparse.ArgumentParser();parser.add_argument('--cache',required=True);args=parser.parse_args();cache=Path(args.cache)
root=Path(__file__).resolve().parents[1];manifest={'Format':1,'Blocks':[],'Sources':[]};seen=set();poems=[];common=[];other=[]
tree=json.loads((cache/'poetry-tree.json').read_text(encoding='utf-8-sig'));hashes={e['path']:e['sha'] for e in tree}
for f in sorted(cache.glob('*.json.simple')):
 name=f.name[:-7];source_path=('全唐诗/' if name.startswith('poet.tang.') else '宋词/')+name
 raw=(cache/name).read_bytes();git_hash=hashlib.sha1(b'blob '+str(len(raw)).encode()+b'\0'+raw).hexdigest()
 if git_hash!=hashes.get(source_path):raise ValueError('Upstream integrity mismatch: '+name)
 records=json.loads(f.read_text(encoding='utf-8-sig'))
 for p in records:
  lines=p.get('paragraphs',[]);title=p.get('title') or p.get('rhythmic');author=p.get('author')
  if not title or not author or not lines or not all(isinstance(x,str) for x in lines):continue
  full='\n'.join(x.strip() for x in lines if x.strip());key=author+'|'+full
  if not full or len(full)>6000 or '□' in full or '\ufffd' in full or key in seen:continue
  seen.add(key);summary=''.join(lines[:2]);summary=summary[:100]+'…' if len(summary)>100 else summary
  poems.append(dict(Id='ext-poem-'+hashlib.sha1(key.encode()).hexdigest()[:20],Category='poem',Title=title[:90],Body=summary,Detail='原诗全文\n'+full+'\n\n阅读提示：这首作品暂未配备逐篇赏析。可以先读出节奏，再留心意象、情绪变化和结尾。以下内容不冒充逐篇解析。',DetailHeading='原诗与阅读提示',Attribution=author+'《'+title+'》 · chinese-poetry 开源古籍库 · 简体转写',Source='https://github.com/chinese-poetry/chinese-poetry',Collection='唐诗' if name.startswith('poet.tang.') else '宋词'))
seen_words=set();curated=json.loads((root/'data/knowledge.json').read_text(encoding='utf-8'));seen_words.update(x['Title'] for x in curated if x['Category']=='word')
with (cache/'ecdict.csv').open(encoding='utf-8-sig',newline='') as stream:
 for row in csv.DictReader(stream):
  word=row.get('word','');ipa=row.get('phonetic','').strip();translation=row.get('translation','').replace('\\n','\n').strip()
  if not re.fullmatch('[a-z]{2,25}',word) or word in seen_words or not ipa or not re.search('[\u4e00-\u9fff]',translation):continue
  if re.search(r'fuck|asshole|bullshit|nigger|cunt|motherfucker',word) or any(mark in translation for mark in ['粗俗','粗鲁','猥亵','辱骂','脏话','性交','色情','阴茎','阴道','肛交','妓女','婊']):continue
  seen_words.add(word)
  def number(s):
   try:return int(float(s or '0'))
   except:return 0
  tags=(row.get('tag') or '').split();bnc=number(row.get('bnc'));frq=number(row.get('frq'))
  usual=bool(number(row.get('oxford')) or number(row.get('collins'))>=1 or set(tags)&{'zk','gk','cet4','cet6'} or 0<bnc<=15000 or 0<frq<=15000)
  first=translation.splitlines()[0];first=first[:105]+'…' if len(first)>105 else first
  definition=row.get('definition','').replace('\\n','\n').strip()
  details=translation[:2000]+(('\n\nEnglish definition\n'+definition[:1200]) if definition else '')
  item=dict(Id='ext-word-'+word,Category='word',Title=word,Body=first,Detail=details,DetailHeading='词典释义',Attribution='ECDICT 开源词典 · 原库音标，英美口音可能有差异',Source='https://github.com/skywind3000/ECDICT',Pronunciation='/'+ipa.strip('/')+'/',SpeakText=word,CommonWord=usual)
  (common if usual else other).append(item)
archive=root/'data/offline-catalog.zip'
with zipfile.ZipFile(archive,'w',compression=zipfile.ZIP_DEFLATED,compresslevel=9) as z:
 for category,pool,usual in [('poem',poems,False),('word',common,True),('word',other,False)]:
  for n in range(0,len(pool),500):
   batch=pool[n:n+500];name=f'{category}-'+('common-' if usual else 'all-')+str(n//500)+'.json'
   z.writestr(name,json.dumps(batch,ensure_ascii=False,separators=(',',':')))
   manifest['Blocks'].append(dict(Name=name,Category=category,Count=len(batch),CommonWord=usual))
 manifest['Sources']=[{'Project':'chinese-poetry/chinese-poetry','License':'MIT','Poems':len(poems),'Files':len(list(cache.glob('*.json.simple')))}, {'Project':'skywind3000/ECDICT','License':'MIT','Words':len(common)+len(other),'CommonWords':len(common),'CsvSHA256':hashlib.sha256((cache/'ecdict.csv').read_bytes()).hexdigest()}]
 manifest['Total']=len(poems)+len(common)+len(other)
 z.writestr('manifest.json',json.dumps(manifest,ensure_ascii=False,indent=2))
 for name in ['ECDICT-LICENSE.txt','chinese-poetry-LICENSE.txt']:z.write(root/'licenses'/name,'licenses/'+name)
(root/'data/catalog-manifest.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps({'Poems':len(poems),'Words':len(common)+len(other),'CommonWords':len(common),'ExtendedTotal':manifest['Total'],'ArchiveBytes':archive.stat().st_size},ensure_ascii=False))
