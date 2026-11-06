using UnityEngine;

// Portal rúnico 16-bit que abre ao derrotar as ondas da Fase I.
public class F1ExitPortal : MonoBehaviour
{
    GroundT1Controller _stage;
    bool _used;
    SpriteRenderer _renderer;
    Sprite[] _frames;
    float _clock;

    public static void Spawn(GroundT1Controller stage, Vector2 feet)
    {
        var go = new GameObject("PortalFimFaseI");
        go.transform.position = feet;
        var portal = go.AddComponent<F1ExitPortal>();
        portal._stage = stage;
        var texture = Resources.Load<Texture2D>("Effects/portal_f1");
        if (texture != null)
        {
            texture.filterMode = FilterMode.Point;
            var r = go.AddComponent<SpriteRenderer>();
            int frameW = texture.height == 64 && texture.width >= 192 ? 48 : texture.width;
            int count = Mathf.Max(1, texture.width / frameW);
            portal._frames = new Sprite[count];
            for (int i = 0; i < count; i++)
                portal._frames[i] = Sprite.Create(texture, new Rect(i * frameW, 0, frameW, texture.height), new Vector2(0.5f, 0f), 16f);
            r.sprite = portal._frames[0];
            r.sortingOrder = 10;
            portal._renderer = r;
        }
        var trigger = go.AddComponent<BoxCollider2D>();
        trigger.isTrigger = true;
        trigger.size = new Vector2(1.8f, 3.2f);
        trigger.offset = Vector2.up * 1.6f;
    }

    void Update()
    {
        if (_renderer != null && _frames != null && _frames.Length > 1)
        {
            _clock += Time.deltaTime * 8f;
            _renderer.sprite = _frames[(int)_clock % _frames.Length];
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (_used || other.GetComponentInParent<PlayerController>() == null) return;
        _used = true;
        if (_stage != null) _stage.OnDemoComplete();
    }
}
