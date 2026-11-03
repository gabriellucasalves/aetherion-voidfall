using UnityEngine;

// A demo termina ao atravessar o portal após as três ondas. Não simula uma Fase II inexistente.
public class F1ExitPortal : MonoBehaviour
{
    GroundT1Controller _stage;
    bool _used;
    public static void Spawn(GroundT1Controller stage, Vector2 feet)
    {
        var go = new GameObject("PortalFimFaseI");
        go.transform.position = feet;
        var portal = go.AddComponent<F1ExitPortal>();
        portal._stage = stage;
        var texture = Resources.Load<Texture2D>("Effects/portal_f1");
        if (texture != null)
        {
            var r = go.AddComponent<SpriteRenderer>();
            r.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0f), 16f);
            r.sortingOrder = 10;
        }
        var trigger = go.AddComponent<BoxCollider2D>();
        trigger.isTrigger = true;
        trigger.size = new Vector2(1.2f, 2.3f);
        trigger.offset = Vector2.up * 1.2f;
    }
    void OnTriggerEnter2D(Collider2D other)
    {
        if (_used || other.GetComponentInParent<PlayerController>() == null) return;
        _used = true;
        if (_stage != null) _stage.OnDemoComplete();
    }
}
