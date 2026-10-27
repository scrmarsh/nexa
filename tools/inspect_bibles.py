"""Render selected source pages and write extracted passage text for visual QA.
Run after extract_bibles.py. This reads the supplied PDFs without modifying them.
"""
import gzip, json
from pathlib import Path
import pymupdf
root=Path(__file__).resolve().parents[1]
out=root/'test-output/bible-source'; out.mkdir(parents=True,exist_ok=True)
references=[('Genesis',1,1),('Psalms',23,1),('John',3,16),('Acts',2,17),('Revelation',22,21)]
report={}
for version in ['NLT','CSB','NKJV']:
    data=json.load(gzip.open(root/'assets/Bible'/f'{version}.json.gz'))
    doc=pymupdf.open(root/'assets/Bible'/f'{version}.pdf')
    report[version]=[]
    for book,chapter,verse in references:
        item=next(v for v in data['Verses'] if (v['Book'],v['Chapter'],v['Verse'])==(book,chapter,verse))
        report[version].append(item)
        doc[item['Page']-1].get_pixmap(matrix=pymupdf.Matrix(1.5,1.5)).save(out/f'{version}-{book}-{chapter}-{verse}.png')
(out/'spot-checks.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
print('Source QA written to',out)
