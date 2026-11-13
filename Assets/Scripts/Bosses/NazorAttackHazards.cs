using UnityEngine;

// These actors only position/animate the supplied sheets, never manufacture art.
public sealed class NazorFallingDebris : MonoBehaviour
{
    VoidDevourerBoss _owner;
    float _age, _warning, _surface, _sky, _x;
    bool _shard, _released, _hit, _landed;
    GameObject _visual;
    public bool DamageActive => _released && !_landed;
    public float WarningDuration => _warning;
    public static NazorFallingDebris Spawn(VoidDevourerBoss owner, Transform parent, float x, float surface, float sky, float warning, bool shard)
    {
        var go = new GameObject(shard ? "NazorCacoVitral" : "NazorColunaPalacio");
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(x,surface,0);
        var actor = go.AddComponent<NazorFallingDebris>();
        actor._owner=owner; actor._warning=warning; actor._surface=surface; actor._sky=sky; actor._x=x; actor._shard=shard;
        NazorVfx.Play("marcador_chao_magenta_48x12_4f", go.transform.position, scale: shard ? .5f : 1f, loop:true, duration:warning, sortingOrder:17, parent:go.transform);
        NazorVfx.Mark(shard ? "caco" : "desabamento",false);
        return actor;
    }
    void Update()
    {
        if (_owner == null || _owner.CurrentState != VoidDevourerBoss.State.Attacking) { Destroy(gameObject); return; }
        _age += Time.deltaTime;
        if (_age < _warning) return;
        float previous = _visual != null ? _visual.transform.position.y : _sky;
        if (!_released)
        {
            _released=true;
            _visual=NazorVfx.Play(_shard ? "desabamento_caco_vitral_16x16_8f" : "desabamento_coluna_40x120_4f", new Vector3(_x,_sky,0),loop:true,parent:transform);
            NazorVfx.Mark(_shard ? "caco" : "desabamento",true);
        }
        if (_landed) { if (_age > _warning + .9f) Destroy(gameObject); return; }
        float t=Mathf.Clamp01((_age-_warning)/.45f), y=Mathf.Lerp(_sky,_surface,t*t);
        if (_visual != null) _visual.transform.position=new Vector3(_x,y,0);
        Vector2 size=_shard ? new Vector2(.5f,.5f) : new Vector2(1.2f,2.2f);
        Vector2 oldCenter=new Vector2(_x,previous+(_shard ? 0f : 1.1f));
        float distance=Mathf.Abs(previous-y);
        if (!_hit) foreach(var hit in Physics2D.BoxCastAll(oldCenter,size,0,Vector2.down,distance))
        {
            var hero=hit.collider.GetComponentInParent<PlayerController>();
            if(hero==null)continue;
            _hit=true;
            NazorVfx.DamagePlayer(hero.transform,new Vector3(_x,y,0),(_shard ? 14f : 22f)*_owner.AttackDamageMultiplier,55);
            break;
        }
        if(t>=1f)
        {
            _landed=true;
            if(_visual!=null)Destroy(_visual);
            var fx=NazorVfx.Play("desabamento_impacto_64x40_7f",new Vector3(_x,_surface,0),scale:_shard ? .5f : 1f,duration:.6f);
            if(fx!=null)fx.GetComponent<NazorVfxInstance>().FadeSeconds=.2f;
            if(_shard)GameAudio.Emit(GameAudio.Cue.BossVoidImpact,transform.position,.5f);
        }
    }
}

public sealed class NazorClawStrike : MonoBehaviour
{
    VoidDevourerBoss _owner;
    float _age;
    bool _hit,_struck;
    GameObject _visual;
    SpriteRenderer _renderer;
    Sprite[] _frames;
    public bool DamageActive => _age >= .9f && _age < 1.15f;
    public static NazorClawStrike Spawn(VoidDevourerBoss owner, GameObject root, Vector3 position)
    {
        root.transform.position=position;
        var actor=root.AddComponent<NazorClawStrike>();
        actor._owner=owner;
        actor._frames=NazorSheets.Get("garra_vazio_64x112_9f");
        NazorVfx.Play("garra_aviso_marcas_64x16_4f",position,loop:true,duration:.9f,sortingOrder:17,parent:root.transform);
        NazorVfx.Mark("garras",false);
        return actor;
    }
    void Update()
    {
        if(_owner==null || _owner.CurrentState!=VoidDevourerBoss.State.Attacking){Destroy(gameObject);return;}
        _age+=Time.deltaTime;
        if(_age>=.9f-5f/14f && _visual==null)
        {
            _visual=new GameObject("GarraAnimada");_visual.transform.SetParent(transform,false);
            _renderer=_visual.AddComponent<SpriteRenderer>();_renderer.sortingOrder=20;
        }
        if(_renderer!=null)
        {
            int frame=_age<.9f ? Mathf.Clamp(Mathf.FloorToInt((_age-(.9f-5f/14f))*14f),0,4)
                : _age<1.15f ? Mathf.Min(6,5+Mathf.FloorToInt((_age-.9f)*14f))
                : Mathf.Min(8,7+Mathf.FloorToInt((_age-1.15f)*14f));
            _renderer.sprite=_frames[frame];
        }
        if(DamageActive)
        {
            if(!_struck)
            {
                _struck=true;_owner.NotifyClawStrike();NazorVfx.Mark("garras",true);NazorVfx.Juice("garras");
                GameAudio.Emit(GameAudio.Cue.Claws,transform.position);
                GameAudio.Emit(GameAudio.Cue.BossVoidImpact,transform.position,.6f);
                NazorVfx.Play("impacto_ciano_48x48_6f",transform.position+Vector3.up*.7f,parent:transform);
                NazorVfx.Play("mergulho_poeira_64x32_8f",transform.position,scale:.8f,parent:transform);
            }
            if(!_hit)foreach(var col in Physics2D.OverlapBoxAll(transform.position+Vector3.up*1.5f,new Vector2(1.6f,3f),0))
            {
                var player=col.GetComponentInParent<PlayerController>();if(player==null)continue;
                _hit=true;NazorVfx.DamagePlayer(player.transform,transform.position,28f*_owner.AttackDamageMultiplier,60);break;
            }
        }
        if(_age>=1.55f)Destroy(gameObject);
    }
}
