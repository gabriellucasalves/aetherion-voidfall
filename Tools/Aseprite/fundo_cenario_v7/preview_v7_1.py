from pathlib import Path
from PIL import Image
H=Path(__file__).parent
def L(d,n): return Image.open(H/d/f'{n}__FLAT_preview.png').convert('RGBA')
def comp(d):
    W,Hh=640,192
    im=Image.new('RGBA',(W,Hh)); im.alpha_composite(L(d,'sky').resize((W,Hh),Image.NEAREST))
    for n,(x,y) in {'clouds_a':(30,10),'clouds_b':(400,4),'clouds_c':(260,40),'clouds_d':(560,50)}.items():
        im.alpha_composite(L(d,n),(x,y))
    im.alpha_composite(L(d,'far'),(64,30)); im.alpha_composite(L(d,'city'),(0,16)); return im
x3=lambda i:i.resize((i.width*3,i.height*3),Image.NEAREST)
a,b=comp('out'),comp('out_v7_1')
x3(b).save(H/'preview_v7_1/v7_1_composito_x3.png')
s=Image.new('RGBA',(640,392),(40,40,40,255)); s.paste(a,(0,0)); s.paste(b,(0,200))
x3(s).save(H/'preview_v7_1/comparativo_v7_vs_v7_1_x3.png')
