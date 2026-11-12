"""Material-based original sound design (NumPy, no external recordings).
Unlike pitch-shifted beeps, these use modal metal, filtered air, plucked strings,
fracturing stone, granular bone rattles, bubbling liquid, and arcane bell clusters.
Run directly to regenerate only character/creature/UI sounds; music is untouched.
"""
from pathlib import Path
import wave
import numpy as np

ROOT = Path(__file__).resolve().parents[2] / 'Assets/Resources/Audio/Sfx'
SR = 22050
rng = np.random.default_rng(8523)

def clock(d): return np.arange(round(d * SR)) / SR

def env(t, attack=.004, decay=12):
    return np.minimum(t / attack, 1) * np.exp(-t * decay) * np.minimum((len(t)/SR-t)/.015,1)

def air(d, low=100, high=8000):
    x=rng.normal(0,1,round(d*SR))
    spectrum=np.fft.rfft(x)
    f=np.fft.rfftfreq(len(x),1/SR)
    spectrum *= (1-np.exp(-(f/max(low,1))**4)) * np.exp(-(f/high)**4)
    y=np.fft.irfft(spectrum,len(x))
    return y / max(np.std(y),.001)

def metal(d, fundamental=720, decay=16):
    t=clock(d)
    return sum(np.sin(2*np.pi*fundamental*r*t)*np.exp(-t*decay*(1+i*.25))/(1+i*.55)
        for i,r in enumerate([1,1.483,2.173,3.791,5.137])) * np.minimum(t/.0015,1)

def pluck(d, fundamental=180, bright=1):
    t=clock(d)
    return sum(np.sin(2*np.pi*fundamental*k*t)*np.exp(-t*(7+k*2.6))/(k**bright)
        for k in range(1,18)) * np.minimum(t/.002,1)

def sweep(d, lo, hi):
    t=clock(d)
    return np.sin(2*np.pi*(lo*t+(hi-lo)*t*t/(2*d)))

def mix(d, *events):
    out=np.zeros(round(d*SR))
    for at,clip,gain in events:
        start=round(at*SR); count=min(len(clip),len(out)-start)
        if count>0: out[start:start+count]+=clip[:count]*gain
    return out

def rock(d, weight=1):
    t=clock(d)
    boom=sweep(d,70*weight,26*weight)*env(t,.002,7)
    grit=air(d,100,2000)*env(t,.001,17)
    crack=air(d,1800,8500)*env(t,.001,65)
    return boom*.8+grit*.4+crack*.25

def fire(d):
    t=clock(d)
    noise=air(d,150,6000)*(0.6+.4*np.sin(2*np.pi*19*t)**2)
    return noise*env(t,.012,4)

def bell(d, freq):
    t=clock(d)
    return (np.sin(2*np.pi*freq*t)+.35*np.sin(2*np.pi*freq*2.76*t)+.15*np.sin(2*np.pi*freq*5.4*t))*env(t,.003,5)

def magic(d, base=520):
    return mix(d,(0,bell(d,base),.5),(.045,bell(d-.045,base*1.5),.3),
        (.09,bell(d-.09,base*2),.24),(0,air(d,2400,8000)*env(clock(d),.08,9),.1))

def claws(d):
    return mix(d,*[(i*.048,air(.15,600+i*350,8500)*np.sin(np.pi*np.minimum(clock(.15)/.15,1))**2,.65)
        for i in range(3)],(0,sweep(d,125,65)*env(clock(d),.02,10),.3))

def bone(d):
    return mix(d,*[(at,metal(.10,880+i*137,48)+air(.10,1700,7000)*env(clock(.10),.001,65),.45)
        for i,at in enumerate([0,.038,.095,.16])])

def bubbles(d):
    out=np.zeros(round(d*SR))
    for i,at in enumerate([0,.035,.09,.16,.21,.28]):
        c=sweep(.12,420-i*31,65+i*17)*env(clock(.12),.003,25)
        out+=mix(d,(at,c,.6))
    return out+air(d,120,2400)*env(clock(d),.02,8)*.23

def save(name, signal, peak=.78):
    ROOT.mkdir(parents=True,exist_ok=True)
    x=np.asarray(signal,dtype=float)
    x-=np.mean(x)
    # Gentle peak limiting, with clean edges and consistent headroom.
    x=np.tanh(x)
    x*=peak/max(np.max(np.abs(x)),.001)
    x*=np.minimum(np.arange(len(x))/44,1)*np.minimum(np.arange(len(x))[::-1]/220,1)
    with wave.open(str(ROOT/(name+'.wav')),'wb') as w:
        w.setnchannels(1);w.setsampwidth(2);w.setframerate(SR)
        w.writeframes((x*32767).astype('<i2').tobytes())
    return x

def generate():
    sounds={}
    d=.32;t=clock(d)
    sounds['Sword']=mix(d,(0,air(.24,300,8000)*np.sin(np.pi*clock(.24)/.24)**2,.62),(.085,metal(.22,1250,26),.18))
    d=.85;t=clock(d)
    sounds['BowDraw']=(air(d,90,1700)*(.5+.5*np.sin(2*np.pi*34*t))*.24+np.sin(2*np.pi*(78*t+48*t*t))*.14)*np.sin(np.pi*t/d)**.6
    sounds['Bow']=mix(.32,(0,pluck(.30,165,.9),.65),(.018,air(.24,1600,8500)*env(clock(.24),.012,19),.26))
    sounds['Magic']=magic(.66,620)
    sounds['WarriorSpecial']=mix(1.1,(0,air(.22,120,3200)*np.linspace(0,1,len(clock(.22)))**2,.3),(.14,fire(.9),.6),(.14,rock(.8,.8),.4))
    sounds['ArcherSpecial']=mix(.85,(0,pluck(.55,235,.7),.6),(.03,air(.7,2800,9500)*env(clock(.7),.02,5),.28),(.1,bell(.65,1600),.12))
    sounds['MageSpecial']=mix(1.5,(0,magic(1.3,280),.55),(.12,magic(1.2,420),.3),(.3,sweep(1.1,80,140)*env(clock(1.1),.12,3),.35))
    sounds['SupremeWarrior']=mix(1.8,(0,metal(.8,320,5),.5),(.12,fire(1.6),.44),(.24,rock(1.4,.7),.65),(.32,metal(1.3,490,4),.3))
    sounds['SupremeArcher']=mix(1.7,(0,pluck(.8,330),.4),(.09,bell(1.5,880),.4),(.18,bell(1.4,1320),.3),(.27,bell(1.3,1760),.2),(.25,air(1.4,4000,9000)*env(clock(1.4),.15,3),.14))
    reverse=magic(1.25,760)[::-1]
    sounds['SupremeMage']=mix(1.85,(0,reverse,.48),(1.1,magic(.7,240),.5),(.1,sweep(1.6,95,48)*env(clock(1.6),.15,1.5),.32))
    sounds['WarriorCrescent']=mix(.4,(0,claws(.26),.4),(.02,fire(.35),.4))
    sounds['WarriorFinal']=mix(1.,(0,metal(.7,410,6),.4),(0,rock(.95,.7),.6),(.03,fire(.8),.4))
    sounds['ArcherStar']=mix(.38,(0,pluck(.28,640),.4),(.02,bell(.35,2100),.24))
    sounds['MageTimeBreak']=mix(1.3,(0,magic(.95,1400)[::-1],.5),(.75,metal(.5,2100,18),.15))
    sounds['Claws']=claws(.32)
    sounds['Bone']=bone(.35)
    sounds['Charge']=mix(.9,*[(i*.19,rock(.30,.65),.42) for i in range(4)],(0,air(.9,30,360)*env(clock(.9),.04,2),.3))
    sounds['Slam']=mix(.9,(0,rock(.8,1.3),.7),(.055,bone(.35),.15),(.14,air(.65,500,3000)*env(clock(.65),.004,7),.25))
    sounds['KnightCharge']=mix(.85,*[(i*.13,metal(.2,480+i*45,24),.35) for i in range(5)],(0,air(.8,120,1300)*env(clock(.8),.05,3),.15))
    sounds['KnightSlam']=mix(.9,(0,metal(.8,265,5),.7),(.015,metal(.7,960,11),.3),(.01,rock(.6,.85),.35))
    sounds['Spit']=mix(.55,(0,bubbles(.45),.65),(.14,air(.35,300,2900)*env(clock(.35),.008,17),.4))
    sounds['Ember']=mix(.6,(0,fire(.58),.6),(0,air(.08,1200,9500)*env(clock(.08),.001,60),.4))
    sounds['EmberDive']=air(.72,650,7600)*np.sin(np.pi*clock(.72)/.72)**2
    sounds['Tentacle']=mix(.8,(0,bubbles(.5),.55),(.1,rock(.65,.7),.6))
    sounds['SelectWarrior']=mix(.6,(0,metal(.5,650,10),.4),(.065,metal(.5,975,12),.22))
    sounds['SelectMage']=mix(.65,(0,bell(.6,740),.5),(.08,bell(.5,1110),.3))
    sounds['SelectArcher']=mix(.6,(0,pluck(.55,330),.6),(.055,pluck(.5,495),.35))
    # Material-specific hit feedback, quieter and shorter than attack releases.
    sounds['WarriorHurt']=metal(.25,310,22)*.6+rock(.25,.8)*.2
    sounds['MageHurt']=magic(.32,310)
    sounds['ArcherHurt']=pluck(.25,110)
    sounds['SkeletonHit']=bone(.26)
    sounds['BeastHit']=air(.20,100,2300)*env(clock(.20),.002,25)
    sounds['GolemHit']=rock(.32,.7)
    sounds['SentinelHit']=mix(.28,(0,rock(.28,1.5),.6),(.04,bone(.2),.2))
    sounds['EmberHit']=fire(.24)
    sounds['KnightHit']=metal(.34,820,16)
    sounds['SpitterHit']=bubbles(.3)
    sounds['BossHit']=sweep(.3,62,32)*env(clock(.3),.005,16)+air(.3,50,700)*env(clock(.3),.005,20)*.3
    from cinematic_sfx import character_overrides
    sounds.update(character_overrides())
    for name,x in sounds.items(): save(name,x,.7 if 'Hit' in name or 'Hurt' in name else .8)
    return sounds

if __name__=='__main__':
    sounds=generate()
    print(f'Generated {len(sounds)} distinct character, enemy and selection cues; music unchanged')
