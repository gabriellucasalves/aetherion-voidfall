using UnityEngine;

public class VoidImpactFx : MonoBehaviour
{
    static Sprite[] _frames;

    public static void EnsureSprites()
    {
        if (_frames != null && _frames.Length == 5) return;
        var tex = Resources.Load<Texture2D>("FX/F3/fx_impacto_vazio_5f_32");
        if (tex == null) return;
        tex.filterMode = FilterMode.Point;
        _frames = new Sprite[5];
        for (int i = 0; i < 5; i++)
        {
            _frames[i] = Sprite.Create(tex, new Rect(i * 32, 0, 32, 32), new Vector2(0.5f, 0.5f), 32f);
        }
    }

    public static VoidImpactFx Spawn(Vector3 position, float scale = 1f)
    {
        EnsureSprites();
        var go = new GameObject("VoidImpactFx");
        go.transform.position = position;
        go.transform.localScale = Vector3.one * scale;
        var fx = go.AddComponent<VoidImpactFx>();
        fx.Build();
        return fx;
    }

    SpriteRenderer _sr;
    float _timer;
    const float FrameDuration = 0.05f; // 20 fps

    void Build()
    {
        _sr = gameObject.AddComponent<SpriteRenderer>();
        _sr.sortingOrder = 22;
        if (_frames != null && _frames.Length > 0)
            _sr.sprite = _frames[0];
    }

    void Update()
    {
        _timer += Time.deltaTime;
        int frame = Mathf.FloorToInt(_timer / FrameDuration);
        if (_frames != null && frame < _frames.Length)
        {
            _sr.sprite = _frames[frame];
        }
        else
        {
            Destroy(gameObject);
        }
    }
}

