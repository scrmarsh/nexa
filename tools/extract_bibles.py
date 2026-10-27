"""Build the offline verse index ONLY from Nexa's three supplied PDFs.
Run: uv run --with pymupdf python tools/extract_bibles.py
Font/layout rules exclude headings, running headers, footnotes and cross references.
Source page numbers and SHA256 fingerprints are retained for auditing.
"""
import gzip, hashlib, json, re, unicodedata
from pathlib import Path
import pymupdf

ROOT = Path(__file__).resolve().parents[1]
BOOKS = 'Genesis|Exodus|Leviticus|Numbers|Deuteronomy|Joshua|Judges|Ruth|1 Samuel|2 Samuel|1 Kings|2 Kings|1 Chronicles|2 Chronicles|Ezra|Nehemiah|Esther|Job|Psalms|Proverbs|Ecclesiastes|Song of Solomon|Isaiah|Jeremiah|Lamentations|Ezekiel|Daniel|Hosea|Joel|Amos|Obadiah|Jonah|Micah|Nahum|Habakkuk|Zephaniah|Haggai|Zechariah|Malachi|Matthew|Mark|Luke|John|Acts|Romans|1 Corinthians|2 Corinthians|Galatians|Ephesians|Philippians|Colossians|1 Thessalonians|2 Thessalonians|1 Timothy|2 Timothy|Titus|Philemon|Hebrews|James|1 Peter|2 Peter|1 John|2 John|3 John|Jude|Revelation'.split('|')
def clean(s):
    # Remove discretionary PDF hyphens; retain actual words/punctuation from the source.
    s=re.sub(r'\u00ad\s*\n\s*', '', s)
    s=s.replace('\u00ad','').replace('\u200b','')
    return re.sub(r'\s+', ' ', unicodedata.normalize('NFKC',s)).strip()
def spans(page):
    result=[]
    for b in page.get_text('dict')['blocks']:
        for l in b.get('lines',[]):
            for i,s in enumerate(l['spans']):
                s['_end']=i==len(l['spans'])-1
                center=(l['bbox'][0]+l['bbox'][2])/2
                s['_heading']=all('OblFTG' in n['font'] for n in l['spans']) and any(abs(center-c)<0.1 for c in [118.52,134.52,302.52,318.52])
                result.append(s)
    return result
def extract(version):
    path=ROOT/'assets/Bible'/f'{version}.pdf'
    doc=pymupdf.open(path); verses=[]; book=-1; chapter=0; verse=0; parts=[]; start_page=0; warnings=[]
    def flush():
        nonlocal parts
        text=clean(''.join(parts)); parts=[]
        if text and book>=0 and chapter and verse:
            verses.append(dict(Book=BOOKS[book],Chapter=chapter,Verse=verse,Text=text,Page=start_page))
    if version=='NLT':
        abbreviations=[]
        for pi,p in enumerate(doc):
            for line in p.get_text().splitlines():
                match=re.match(r'^(\d?\s*[A-Za-z]+)\s+(\d+):(\d+)\s+(.*)$',line)
                if match:
                    flush(); ab,c,v,t=match.groups()
                    if ab not in abbreviations: abbreviations.append(ab)
                    book=abbreviations.index(ab); chapter=int(c); verse=int(v);start_page=pi+1;parts=[t+'\n']
                elif book>=0: parts.append(line+'\n')
        flush()
    else:
        starts={}
        if version=='NKJV':
            for line in doc[11].get_text().splitlines():
                m=re.match(r'^(.*?)\s*\.(?:\s*\.)*\s*(\d+)$',line)
                if m and m[1].strip() in BOOKS: starts[int(m[2])+14]=BOOKS.index(m[1].strip())
            assert len(starts)==66
        pending_chapter=None
        waiting_psalm_text=False
        for pi,p in enumerate(doc):
            if version=='NKJV' and pi>=1111: break
            if book==65 and chapter==22 and verse==21 and pi+1>start_page: break
            allspans=spans(p)
            # Book headings identify canonical transitions (not page running headers).
            for s in allspans if version=='CSB' else []:
                name=clean(s['text']).upper().replace('SONG OF SONGS','SONG OF SOLOMON')
                if s['size']>(20 if version=='CSB' else 12) and name in [b.upper() for b in BOOKS]:
                    candidate=[b.upper() for b in BOOKS].index(name)
                    if candidate==book+1:
                        flush();book=candidate;chapter=1;verse=1;start_page=pi+1
            title=None
            if pi in starts:
                title=next(s for s in allspans if 13<s['size']<14)
                title['_book']=starts[pi]
            if book<0 and title is None: continue
            if version=='NKJV':
                # PDF content stream order occasionally puts drop caps before the book title.
                title_y=title['bbox'][1] if title else -100
                def order(s):
                    zone=0 if s['bbox'][1]<title_y-1 else 1 if '_book' in s else 2
                    y=s['bbox'][1] if s['size']>15 else s['origin'][1]-5.65
                    return (zone,0 if s['bbox'][0]<210 else 1,round(y,0),s['bbox'][0])
                allspans.sort(key=order)
            previous_body=None
            for si,s in enumerate(allspans):
                t=s['text']; f=s['font']; z=s['size'];x,y,xx,yy=s['bbox']; stripped=clean(t)
                if '_book' in s:
                    flush();book=s['_book'];chapter=1;verse=1;start_page=pi+1;continue
                if book<0: continue
                if version=='CSB':
                    if y<45 or 'Bible' not in f: continue
                    if 'BibleSerif' in f and z>18 and stripped.isdigit():
                        following=next((clean(n['text']) for n in allspans[si+1:] if n['font']=='BibleSans3-Bold' and 8<n['size']<12 and clean(n['text']).isdigit()),'2')
                        if following!='2': pending_chapter=int(stripped)
                        else: flush();chapter=int(stripped);verse=1;start_page=pi+1
                        continue
                    if 'BibleSans3-Bold'==f and 8<z<12 and stripped.isdigit():
                        flush()
                        if stripped=='1' and pending_chapter: chapter=pending_chapter;pending_chapter=None
                        verse=int(stripped);start_page=pi+1;continue
                    if 'BibleSerif' in f and 8<z<12:
                        parts.append(t+('\n' if s['_end'] else ''))
                else:
                    # Main text sits in two 130-point columns; center references use smaller fonts.
                    if y<66 or z<5.9 or z>=22: continue
                    if re.fullmatch(r'PSALM \d+',stripped) and z>7:
                        flush();chapter=int(stripped.split()[1]);verse=1;start_page=pi+1;waiting_psalm_text=True;continue
                    if stripped in ['BOOK ONE','BOOK TWO','BOOK THREE','BOOK FOUR','BOOK FIVE']: continue
                    if 15<z<20:
                        if stripped.isdigit():
                            flush();chapter=int(stripped);verse=1;start_page=pi+1
                        elif any(c.isalpha() for c in stripped): waiting_psalm_text=False;parts.append(t)
                        continue
                    if waiting_psalm_text:
                        if stripped=='1' and z>7: waiting_psalm_text=False
                        else: continue
                    if not (7.3<z<7.8 or stripped in ['ORD','OD']) or 'Bold' in f and not stripped.isdigit(): continue
                    # Section headings in oblique type are centered; Scripture quotations in
                    # the same face are left-aligned. Keep the quotations, omit headings.
                    if s['_heading']: continue
                    inline=re.match(r'^(\d{1,3})\s+(.+)$',t)
                    if inline and int(inline[1])==verse+1 and any(abs(x-left)<1 for left in [53.52,69.52,237.52,253.52]):
                        flush();verse=int(inline[1]);start_page=pi+1;t=inline[2];stripped=clean(t)
                    if stripped.isdigit() and (int(stripped)==verse+1 or abs(x-53.5)<1 or abs(x-69.5)<1 or abs(x-237.5)<1 or abs(x-253.5)<1):
                        flush();verse=int(stripped);start_page=pi+1;continue
                    # Body line breaks are physical wraps, not word boundaries inside spans.
                    if previous_body and parts and not parts[-1].endswith((' ','\n')) and not t.startswith(' '):
                        if abs(previous_body['origin'][1]-s['origin'][1])<1 and x-previous_body['bbox'][2]>0.7:
                            parts.append(' ')
                    parts.append(t)
                    previous_body=s
                    if s['_end']:
                        if parts[-1].endswith('-'): parts[-1]=parts[-1][:-1]
                        else: parts.append(' ')
        flush()
    keys=set()
    for v in verses:
        key=(v['Book'],v['Chapter'],v['Verse'])
        if key in keys: warnings.append(f'Duplicate {key}')
        keys.add(key)
    assert not warnings, warnings[:10]
    assert len(set(v['Book'] for v in verses))==66
    chapters=set((v['Book'],v['Chapter']) for v in verses)
    if len(chapters)!=1189:
        baseline=json.load(gzip.open(ROOT/'assets/Bible/NLT.json.gz'))['Verses']
        raise ValueError('Missing chapters: '+str(sorted(set((v['Book'],v['Chapter']) for v in baseline)-chapters)))
    output=dict(Translation=version,Source=path.name,SourceSha256=hashlib.sha256(path.read_bytes()).hexdigest(),Verses=verses)
    dest=ROOT/'assets/Bible'/f'{version}.json.gz'
    with gzip.open(dest,'wt',encoding='utf-8') as stream: json.dump(output,stream,ensure_ascii=False,separators=(',',':'))
    print(version, 'verses',len(verses),'books',len(set(v['Book'] for v in verses)), 'warnings',len(warnings),flush=True)
    return output,warnings
if __name__=='__main__':
    outputs={};audit={}
    for v in ['NLT','CSB','NKJV']:
        outputs[v],audit[v]=extract(v)
    baseline={(v['Book'],v['Chapter'],v['Verse']) for v in outputs['NLT']['Verses']}
    for name,data in outputs.items():
        keys={(v['Book'],v['Chapter'],v['Verse']) for v in data['Verses']}
        audit[name]+= ['Missing '+str(k) for k in sorted(baseline-keys)]
        audit[name]+= ['Extra '+str(k) for k in sorted(keys-baseline)]
    dest=ROOT/'test-output/bible-source';dest.mkdir(parents=True,exist_ok=True)
    (dest/'audit.json').write_text(json.dumps(audit,indent=2),encoding='utf-8')
    print({k:len(v) for k,v in audit.items()})
