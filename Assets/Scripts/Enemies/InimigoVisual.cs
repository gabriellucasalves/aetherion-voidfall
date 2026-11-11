using UnityEngine;

// Pixel art dos inimigos T1 (sheets geradas por Assets/Art/Inimigos/build_enemies.py).
// Cada sheet tem 4 células de 64px: idle A/B + passo A/B, com brilho dos olhos piscando.
public class InimigoVisual : MonoBehaviour
{
    const float Ppu = PixelArt.Ppu;      // mesma grade do herói e do cenário
    const float FootPivot = 7f / 64f;    // pés na linha 57 da célula
    static readonly int[] WalkCycle = { 2, 0, 3, 1 };

    static readonly System.Collections.Generic.Dictionary<string, Sprite[]> Cache =
        new System.Collections.Generic.Dictionary<string, Sprite[]>();

    SpriteRenderer _renderer;
    Rigidbody2D _body;
    EnemyController _controller;
    Sprite[] _frames;
    float _clock;
    float _hurtLeft;
    float _golemFxClock;
    SpriteRenderer _golemWarning;
    Sprite[] _golemWarningFrames;
    SpriteRenderer _slowMark;
    Sprite[] _slowFrames;

    public static bool Attach(GameObject owner, string id)
    {
        if (id == "cavaleiro_corrompido")
        {
            owner.AddComponent<CavaleiroCorrompido>();
            return true;
        }
        if (id == "cuspidor_vazio")
        {
            owner.AddComponent<CuspidorDoVazio>();
            return true;
        }

        var frames = Load(id);
        if (frames == null)
            return false;

        var go = new GameObject("Visual");
        go.transform.SetParent(owner.transform, false);
        var visual = go.AddComponent<InimigoVisual>();
        visual._frames = frames;
        visual._renderer = go.AddComponent<SpriteRenderer>();
        visual._renderer.sprite = frames[0];
        visual._renderer.sortingOrder = (T1AtmosScenery.Built || T2PalaceScenery.Built || T3RuinsScenery.Built) ? 9 : 8;
        visual._body = owner.GetComponent<Rigidbody2D>();
        visual._controller = owner.GetComponent<EnemyController>();
        visual._slowFrames = TrioAnimationFrames.Vfx("lentidao_marca", 8, 10, 3,
            new Vector2(.5f, 9f / 10f));
        if (visual._slowFrames != null)
        {
            var mark = new GameObject("MarcaLentidao");
            mark.transform.SetParent(go.transform, false);
            mark.transform.localPosition = new Vector3(0f, 1.85f, 0f);
            visual._slowMark = mark.AddComponent<SpriteRenderer>();
            visual._slowMark.sortingOrder = 13;
            visual._slowMark.enabled = false;
        }
        if (id == "golem_ardosia")
        {
            visual._golemWarningFrames = TrioAnimationFrames.Vfx("golem_carga_brilho", 16, 16, 3,
                new Vector2(.5f, .5f));
            if (visual._golemWarningFrames != null)
            {
                var warning = new GameObject("AvisoInvestida");
                warning.transform.SetParent(go.transform, false);
                warning.transform.localPosition = new Vector3(0f, 1.05f, 0f);
                visual._golemWarning = warning.AddComponent<SpriteRenderer>();
                visual._golemWarning.sortingOrder = 12;
                visual._golemWarning.enabled = false;
            }
        }
        visual._clock = Random.Range(0f, 10f); // dessincroniza a animação entre inimigos

        var health = owner.GetComponent<HealthSystem>();
        if (health != null)
            health.Damaged += _ => visual._hurtLeft = 0.14f;
        return true;
    }

    static Sprite[] Load(string id)
    {
        if (Cache.TryGetValue(id, out var cached) && cached != null)
            return cached;

        var texture = Resources.Load<Texture2D>("Inimigos/" + id);
        if (texture == null)
            return null;

        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;

        int count = texture.width / 64;
        var frames = new Sprite[count];
        for (int i = 0; i < count; i++)
        {
            frames[i] = Sprite.Create(
                texture,
                new Rect(i * 64, 0, 64, 64),
                new Vector2(0.5f, FootPivot),
                Ppu,
                0,
                SpriteMeshType.FullRect);
        }

        Cache[id] = frames;
        return frames;
    }

    void LateUpdate()
    {
        if (_renderer == null || _frames == null)
            return;

        if (_controller != null && _controller.IsFrozen)
        {
            _renderer.color = new Color(0.55f, 0.55f, 0.62f);
            return;
        }

        _clock += Time.deltaTime;
        if (_slowMark != null)
        {
            _slowMark.enabled = _controller != null && _controller.IsSlowed;
            if (_slowMark.enabled)
                _slowMark.sprite = _slowFrames[Mathf.FloorToInt(_clock * 8f) % _slowFrames.Length];
        }
        if (_golemWarning != null)
        {
            bool winding = _controller != null && _controller.IsWindingUp;
            _golemWarning.enabled = winding;
            if (winding)
                _golemWarning.sprite = _golemWarningFrames[Mathf.FloorToInt(_clock * 10f) % 3];
            if (_controller != null && _controller.IsCharging)
            {
                _golemFxClock += Time.deltaTime;
                if (_golemFxClock >= .12f)
                {
                    _golemFxClock = 0f;
                    TrioVfxPlayer.Spawn("golem_investida_poeira", transform.position + Vector3.up * .1f,
                        32, 14, 5, new Vector2(.5f, .5f), 14f);
                }
            }
        }

        // flash de dano > telegraph de ataque > normal
        if (_hurtLeft > 0f)
        {
            _hurtLeft -= Time.deltaTime;
            _renderer.color = new Color(1f, 0.42f, 0.42f);
        }
        else if (_controller != null && _controller.IsWindingUp)
        {
            bool charging = _controller.Data != null && _controller.Data.AttackKind == "charge";
            float speed = charging ? 10f : 7f;
            var flash = charging ? new Color(1f, 0.66f, 0.28f) : new Color(0.55f, 1f, 0.95f);
            bool on = (int)(Time.time * speed) % 2 == 0;
            _renderer.color = on ? flash : Color.white;
        }
        else if (_controller != null && _controller.VitalMarks > 0)
        {
            bool on = (int)(Time.time * 12f) % 2 == 0;
            _renderer.color = on ? new Color(1f, 0.84f, 0.25f) : Color.white;
        }
        else
        {
            _renderer.color = Color.white;
        }

        if (_controller != null && _controller.Data != null)
        {
            string kind = _controller.Data.Id;
            if (kind == "sentinela_escombro" && _frames.Length >= 9)
            {
                if (_controller.BurrowStage == 1)
                {
                    _renderer.sprite = _frames[4];
                    return;
                }
                if (_controller.BurrowStage == 2)
                {
                    _renderer.sprite = _frames[5];
                    return;
                }
                if (_controller.IsWindingUp)
                {
                    _renderer.sprite = _frames[6];
                    return;
                }
                if (_controller.IsAttackingActive)
                {
                    _renderer.sprite = _frames[7];
                    return;
                }
            }
            else if (kind == "arauto_brasa" && _frames.Length >= 8)
            {
                if (_controller.IsWindingUp)
                {
                    _renderer.sprite = _frames[4];
                    return;
                }
                if (_controller.IsDiving)
                {
                    _renderer.sprite = _frames[5];
                    return;
                }
                if (_controller.IsShootingEmber)
                {
                    _renderer.sprite = _frames[6];
                    return;
                }
            }
        }

        bool moving = _body != null && Mathf.Abs(_body.linearVelocity.x) > 0.15f;
        if (moving)
            _renderer.sprite = _frames[WalkCycle[(int)(_clock * 7f) % WalkCycle.Length]];
        else
            _renderer.sprite = _frames[(int)(_clock * 2.4f) % 2];
    }
}
