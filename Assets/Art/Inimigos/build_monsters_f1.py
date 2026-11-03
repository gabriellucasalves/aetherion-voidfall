"""Sprites nativos de integração F1, 16 PPU, 4 poses de 64 px; não altera o Guerreiro.
Execute a partir de qualquer pasta. Os briefs separam conceitos de aprovação de arte final.
"""
from pathlib import Path
from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'Resources' / 'Inimigos'
OUT.mkdir(parents=True, exist_ok=True)
INK = '#131524'; BONE = '#c1d4bf'; BSH = '#6d9697'; LIGHT = '#e3eed0'
TEAL = '#52d2d9'; DARK = '#28364b'; STONE = '#52637b'; HIGH = '#8297a8'
VIOLET = '#66516f'; SHADE = '#382c4b'; PINK = '#d18b9f'

def frame(kind, pose):
    im = Image.new('RGBA', (64,64)); d = ImageDraw.Draw(im)
    dy = 1 if pose in (1,3) else 0
    step = (-1 if pose == 2 else 1 if pose == 3 else 0)
    def poly(points, c): d.polygon([(x,y+dy) for x,y in points],fill=c)
    def box(r,c): d.rectangle((r[0],r[1]+dy,r[2],r[3]+dy),fill=c)
    def line(points,c,w=1):d.line([(x,y+dy) for x,y in points],fill=c,width=w)
    if kind=='esqueleto_alien':
        # Crânio alongado, apêndices ósseos, cavidades e espinha Aether.
        poly([(25,34),(24,30),(27,28),(29,30),(34,29),(38,31),(40,35),(38,39),(29,40)],INK)
        poly([(27,32),(29,31),(35,31),(38,33),(38,36),(35,38),(29,37)],BONE)
        poly([(25,32),(23,29),(22,25),(25,27),(28,31)],BSH)
        poly([(34,31),(35,27),(38,25),(37,30)],BONE)
        box((34,33,38,34),DARK);box((35,33,37,33),TEAL)
        line([(28,34),(29,36),(32,36)],LIGHT)
        box((29,39,34,48),INK);line([(32,39),(32,48)],TEAL)
        for y in (40,43,46):
            line([(27,y),(30,y+1),(35,y),(37,y-1)],BONE)
            line([(27,y+1),(30,y+2),(35,y+1)],BSH)
        line([(27,40),(24,44),(25,48),(29,48)],INK,3)
        line([(27,40),(25,44),(26,47),(29,47)],BSH)
        line([(35,40),(38,43),(41,40)],INK,3)
        line([(36,40),(38,42),(41,40)],BONE)
        box((39,39,43,40),TEAL)
        for x,dx in ((29,step),(34,-step)):
            line([(x,48),(x+dx,52),(x+dx,55)],INK,3)
            line([(x,49),(x+dx,52),(x+dx,55)],BONE)
            box((x+dx-1,55,x+dx+2,56-dy),BSH)
    elif kind=='bicho':
        # Quadrúpede arqueado, placas violetas, focinho e garras; sem ácido.
        poly([(20,45),(23,39),(28,36),(36,38),(40,42),(44,44),(44,49),(38,51),(26,51)],INK)
        poly([(22,44),(25,40),(30,38),(35,40),(39,44),(34,49),(25,49)],VIOLET)
        poly([(22,46),(27,45),(31,47),(36,45),(35,49),(26,50)],SHADE)
        for x,y in ((24,40),(28,38),(32,39)):
            poly([(x,y),(x-1,y-4),(x+3,y-1)],BSH)
        poly([(36,43),(40,41),(45,44),(44,48),(38,49)],INK)
        poly([(38,43),(41,43),(44,45),(42,47),(38,47)],HIGH)
        box((40,44,43,44),PINK);line([(39,47),(43,47)],INK)
        for x in (40,42):box((x,47,x,48),LIGHT)
        for x,dx in ((24,step),(31,-step),(38,step)):
            poly([(x,48),(x+3,49),(x+3+dx,54),(x+5+dx,55),(x-1+dx,56-dy),(x-1,52)],INK)
            line([(x+1,49),(x+1+dx,53),(x+3+dx,54)],VIOLET,2)
            for gx in (0,2):line([(x+2+dx+gx,54),(x+3+dx+gx,55)],LIGHT)
        poly([(22,45),(18,47),(17,43),(19,44)],SHADE)
    else:
        # Golem baixo e largo: 27 px de altura, camadas de ardósia e fissuras frias.
        for x,dx in ((25,step),(36,-step)):
            poly([(x,49),(x+5,49),(x+5+dx,55),(x+7+dx,56-dy),(x-1+dx,56-dy)],INK)
            box((x+dx,51,x+4+dx,54),STONE)
            line([(x+dx,51),(x+3+dx,51)],HIGH)
        poly([(22,37),(26,35),(38,35),(43,39),(41,49),(36,52),(25,50),(21,44)],INK)
        poly([(24,38),(28,36),(36,37),(40,40),(38,48),(33,50),(25,48),(23,43)],STONE)
        poly([(24,39),(28,37),(35,38),(32,41),(25,42)],HIGH)
        poly([(25,43),(32,42),(36,44),(33,49),(26,47)],DARK)
        line([(32,38),(30,42),(33,45),(31,48)],TEAL)
        for x in (19,40):
            poly([(x,39),(x+5,38),(x+7,44),(x+5,51),(x,51),(x-2,46)],INK)
            poly([(x,41),(x+4,40),(x+4,45),(x+3,49),(x,48)],STONE)
            line([(x,41),(x+3,41)],HIGH)
            line([(x+1,44),(x+3,45),(x+1,47)],TEAL)
        poly([(27,31),(30,29),(36,30),(39,33),(38,38),(28,38),(26,35)],INK)
        poly([(28,32),(31,31),(36,32),(37,35),(35,37),(29,36)],STONE)
        line([(29,32),(34,32)],HIGH);box((30,34,37,34),INK)
        box((31,34,32,34),TEAL);box((35,34,36,34),TEAL)
    return im

if __name__=='__main__':
    for kind in ('esqueleto_alien','bicho','golem_ardosia'):
        sheet=Image.new('RGBA',(256,64))
        for pose in range(4):sheet.alpha_composite(frame(kind,pose),(pose*64,0))
        sheet.save(OUT/f'{kind}.png')
    effects=ROOT/'Resources'/'Effects';effects.mkdir(exist_ok=True)
    fire=Image.new('RGBA',(112,32));d=ImageDraw.Draw(fire)
    for f in range(4):
        y=f*8
        for x in range(112):
            inset=1 if (x+f*5)%13<4 else 0
            d.line((x,y+1+inset,x,y+6-inset),fill='#da4223')
            d.line((x,y+2,x,y+5),fill='#ff8d24')
            if (x+f*3)%11<8:d.line((x,y+3,x,y+4),fill='#ffe9a3')
    fire.save(effects/'feixe_infernal.png')
    portal=Image.new('RGBA',(24,40));d=ImageDraw.Draw(portal)
    for inset,c in ((1,'#252940'),(3,'#5961a3'),(5,'#65d9dc'),(7,'#b1f1d5'),(8,'#282743')):
        d.ellipse((inset,2+inset,23-inset,39-inset),fill=c)
    for x,y in ((6,11),(17,28),(12,7),(8,33)):d.rectangle((x,y,x+1,y+1),fill='#fff0ad')
    portal.save(effects/'portal_f1.png')
    print('3 sheets F1 e efeitos exportados em Resources; Guerreiro preservado.')
