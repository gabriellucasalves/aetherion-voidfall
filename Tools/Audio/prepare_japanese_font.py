"""Embed a small static OFL Japanese font for the exact authored supreme text.
Requires fonttools. Source: https://github.com/google/fonts/tree/main/ofl/notosansjp
"""
from pathlib import Path
import re
import urllib.request
from fontTools.ttLib import TTFont
from fontTools.varLib.instancer import instantiateVariableFont
from fontTools import subset

ROOT=Path(__file__).resolve().parents[2]
DEST=ROOT/'Assets/Resources/Menu/fontes'
SOURCE='https://raw.githubusercontent.com/google/fonts/main/ofl/notosansjp/'
cache=Path('/tmp/aetherion-japanese-font.ttf')
if not cache.exists():urllib.request.urlretrieve(SOURCE+'NotoSansJP%5Bwght%5D.ttf',cache)
font=TTFont(cache)
font=instantiateVariableFont(font,{'wght':600},inplace=True)
text=(ROOT/'Assets/Scripts/Abilities/SpecialJapanese.cs').read_text() + (ROOT/'Assets/Scripts/Bosses/BossDialogue.cs').read_text()
strings=re.findall(r'public const string \w+ = "([^"]+)";',text)
chars=set(''.join(strings))
chars.update(chr(c) for c in range(32,256))
chars.update(chr(c) for c in range(0x3040,0x3100))
options=subset.Options();options.name_IDs=['*'];options.name_languages=['*'];options.name_legacy=True
sub=subset.Subsetter(options=options);sub.populate(text=''.join(chars));sub.subset(font)
for name in font['name'].names:
 if name.nameID in [1,3,4,6,16,17]:
  value='AetherionSupremeJP' if name.nameID!=17 else 'SemiBold'
  name.string=value.encode(name.getEncoding())
output=DEST/'AetherionSupremeJP.ttf';font.save(output)
if not (DEST/'OFL_NotoSansJP.txt').exists(): urllib.request.urlretrieve(SOURCE+'OFL.txt',DEST/'OFL_NotoSansJP.txt')
print('Embedded Japanese font:',output.stat().st_size,'bytes;',len(font.getBestCmap()),'glyph mappings')
