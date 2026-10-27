# Integrated Bible sources

The reader uses only `NLT.pdf`, `CSB.pdf` and `NKJV.pdf` supplied with this project. The original PDFs are preserved. No online verse service, substitute translation or generated verse text is used.

`NLT.json.gz`, `CSB.json.gz` and `NKJV.json.gz` are embedded read-only indexes. Each records its translation, source filename, SHA-256 source fingerprint and source page number for each verse. They cover all 66 books and 1,189 chapters. The index removes PDF layout whitespace, discretionary line-break hyphens, running headers, section headings, cross-references, footnote markers and appendices; it preserves Scripture wording from the supplied edition.

The supplied NLT has combined passages without separate numbers in parts of Numbers 1–2 and Hebrews 13:20–21. CSB's Acts 24 passage has no separate printed 8 marker. NKJV's numbering differs from NLT in several New Testament passages, including the end of 3 John and Revelation 12. These source differences are retained, not filled with another translation. Searches return the verse numbers actually present in the selected index.

Rebuild the indexes on a development machine:

```powershell
uv run --with pymupdf python tools/extract_bibles.py
uv run --with pymupdf python tools/inspect_bibles.py
dotnet run --project SessionDashboard.csproj -c Release -- --verify-startup verification/bible-startup
```

The extraction script checks chapter coverage and duplicate addresses, and writes a cross-edition numbering audit to `test-output/bible-source/audit.json`. The inspection script renders sample source pages and exports passage text for visual comparison. PyMuPDF is a development-only extraction dependency; the Windows app reads the embedded indexes using .NET and works offline.

The supplied CSB PDF identifies the Christian Standard Bible, copyright 2017 Holman Bible Publishers. The supplied NKJV PDF identifies the New King James Version, copyright 1982 Thomas Nelson. Original source notices remain in the PDFs. NLT wording is taken from the supplied file rather than replaced with wording from a different NLT edition.
