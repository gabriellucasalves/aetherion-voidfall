using UnityEngine;

// Partículas curtas da rajada (brasa, faísca de corte, flash da lâmina).
public class RajadaFx : MonoBehaviour
{
    static Sprite[] _ember, _impact, _flash;
    Sprite[] _frames;
    SpriteRenderer _renderer;
    float _life, _age;
    Vector3 _velocity;

    public static void Ember(Vector3 at)
    {
        if (_ember == null) _ember = TrioAnimationFrames.Vfx("rajada_brasa_4f_8x8", 8, 8, 4, new Vector2(0.5f, 0.5f));
        Make("Brasa", _ember, at, 0.45f, new Vector3(Random.Range(-0.2f, 0.2f), 0.6f, 0f), 21);
    }

    public static void Impact(Vector3 at)
    {
        if (_impact == null) _impact = TrioAnimationFrames.Vfx("rajada_impacto_4f_24x24", 24, 24, 4, new Vector2(0.5f, 0.5f));
        Make("CorteFagulha", _impact, at, 0.2f, Vector3.zero, 24);
    }

    public static void Flash(Vector3 at)
    {
        if (_flash == null) _flash = TrioAnimationFrames.Vfx("rajada_flash_final_4f_96x96", 96, 96, 4, new Vector2(0.5f, 0.5f));
        Make("LuaRubraFlash", _flash, at, 0.24f, Vector3.zero, 25);
    }

    static void Make(string name, Sprite[] frames, Vector3 at, float life, Vector3 velocity, int order)
    {
        if (frames == null)
            return;
        var go = new GameObject(name);
        go.transform.position = at;
        var fx = go.AddComponent<RajadaFx>();
        fx._frames = frames;
        fx._life = life;
        fx._velocity = velocity;
        fx._renderer = go.AddComponent<SpriteRenderer>();
        fx._renderer.sortingOrder = order;
        fx._renderer.sprite = frames[0];
    }

    void Update()
    {
        _age += Time.deltaTime;
        if (_age >= _life)
        {
            Destroy(gameObject);
            return;
        }
        transform.position += _velocity * Time.deltaTime;
        int f = Mathf.Min(_frames.Length - 1, (int)(_age / _life * _frames.Length));
        _renderer.sprite = _frames[f];
    }
}
