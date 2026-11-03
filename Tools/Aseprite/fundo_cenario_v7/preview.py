import sys
from pathlib import Path
from PIL import Image
o=Path('out'); T=Path('../../../Assets/Resources/T1_Hybrid')
def L(n): return Image.open(o/f'{n}__FLAT_preview.png').convert('RGBA')
def comp(get):
    W,H=640,192
    im=Image.new('RGBA',(W,H))
    sky=get('sky').resize((W,H),Image.NEAREST); im.alpha_composite(sky)
    for n,(x,y) in {'clouds_a':(30,10),'clouds_b':(400,4),'clouds_c':(260,40),'clouds_d':(560,50)}.items():
        im.alpha_composite(get(n),(x,y))
    im.alpha_composite(get('far'),(64,30)); im.alpha_composite(get('city'),(0,16))
    return im
x3=lambda i:i.resize((i.width*3,i.height*3),Image.NEAREST)
x3(comp(L)).save('preview/v7_composito_x3.png')
v6=Path(sys.argv[1]) if len(sys.argv)>1 else None
a=Image.open('backup_pre_v7_hybrid/city.png').convert('RGBA') if Path('backup_pre_v7_hybrid/city.png').exists() else Image.open(T/'city.png').convert('RGBA')
b=L('city')
bg=lambda i:Image.alpha_composite(Image.new('RGBA',i.size,(238,226,214,255)),i)
s=Image.new('RGBA',(1288,176),(40,40,40,255)); s.paste(bg(a),(0,0)); s.paste(bg(b),(648,0))
x3(s).save('preview/comparativo_v6_vs_v7_x3.png')
