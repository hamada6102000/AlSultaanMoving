# Font Awesome 6.5.2 — subset

`fontawesome-subset.css` and the three files in `webfonts/` are **generated**, not
vendored as-is. They contain only the icons this site actually uses.

| | upstream | here |
|---|---|---|
| CSS | 103,009 B (from cdnjs) | 3,162 B (local) |
| fa-solid-900.woff2 | 156,400 B | 5,344 B |
| fa-regular-400.woff2 | 25,392 B | 2,164 B |
| fa-brands-400.woff2 | 117,852 B | 692 B |
| **total** | **~402 KB + a third-party connection** | **11.4 KB, same origin** |

Also added here and missing upstream: `font-display:swap`, so icons never block text.

## Adding or changing an icon

The font only contains the 41 glyphs that were in use when it was generated. A new
`fa-*` class in the markup **will render as a blank box** until the font is rebuilt.

1. Use the icon normally in a view or in `App_Data/content.json`.
2. Re-run the four steps below.
3. Rebuild and check the icon appears.

```bash
# 0. one-off tooling
python -m pip install fonttools brotli

# 1. upstream CSS (source of the codepoint table)
curl -o fa.css https://cdnjs.cloudflare.com/ajax/libs/font-awesome/6.5.2/css/all.min.css

# 2. upstream fonts (full versions, used as subsetting input)
for f in fa-solid-900 fa-regular-400 fa-brands-400; do
  curl -o "orig-$f.woff2" "https://cdnjs.cloudflare.com/ajax/libs/font-awesome/6.5.2/webfonts/$f.woff2"
done

# 3. collect every fa-* class used in the app and map it to a codepoint,
#    then subset each font to exactly that set
python -m fontTools.subset "orig-fa-solid-900.woff2" \
  --unicodes="U+f015,U+f095,..." \
  --flavor=woff2 --no-hinting --desubroutinize \
  --output-file=webfonts/fa-solid-900.woff2
```

The codepoint list is derived by scanning `Views/**/*.cshtml`, `App_Data/content.json`
and `wwwroot/css/site.css` for `fa-*` classes, then looking each one up in the upstream
CSS `content:"\fXXX"` rules. The same unicode list is passed to all three fonts —
`fontTools` silently skips codepoints a given font does not contain, which is why the
solid font ends up with 40 glyphs, regular with 12 and brands with 1.

## Licence

Font Awesome Free 6.5.2 — see `LICENSE.txt`.
Icons CC BY 4.0, fonts SIL OFL 1.1. Subsetting is permitted under both.
