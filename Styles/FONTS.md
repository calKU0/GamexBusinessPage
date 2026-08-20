# Typefaces

The files in `wwwroot/fonts` are served from our own domain instead of Google
Fonts, so the browser does not have to connect to `fonts.googleapis.com` and
`fonts.gstatic.com` before it can paint the first line of text.

## What is here

**Barlow** and **Barlow Condensed**, by Jeremy Tribby.
Copyright 2017 The Barlow Project Authors — https://github.com/jpt/barlow

Licence: **SIL Open Font License 1.1** — full text in `OFL.txt`.
The licence requires copies of the fonts to be distributed together with that
text, so `OFL.txt` must stay in the directory and be deployed to the server.

## File set

12 `.woff2` files, two character subsets per weight:

| Family | Weights | Subsets |
|---|---|---|
| Barlow | 400, 500, 600, 700 | `latin`, `latin-ext` |
| Barlow Condensed | 600, 700 | `latin`, `latin-ext` |

The `latin-ext` subset carries the Polish diacritics (ą, ć, ę, ł, ń, ó, ś, ź, ż).
The `@font-face` declarations and their `unicode-range` values live in
`Styles/00-fonts.css`; they let the browser download only the files needed to
render a particular piece of text.

## Replacing a face or adding a weight

1. Download the file from Google Fonts (the `.woff2` URLs appear in the
   stylesheet returned by `https://fonts.googleapis.com/css2?family=...` when
   requested with a modern User-Agent).
2. Save it here following the `barlow[-condensed]-<weight>-<subset>.woff2`
   pattern.
3. Add the `@font-face` declaration to `Styles/00-fonts.css`, copying
   `unicode-range` from the Google stylesheet — without it the browser downloads
   the file unconditionally, even when it is not needed.
4. Run `dotnet build` to rebuild `wwwroot/css/site.css`.

The `<link rel="preload">` URLs in `_Layout.cshtml` deliberately avoid the `~/`
path: the tag helper would fingerprint them, while `@font-face` points at
unfingerprinted URLs, which would make every face download twice.
