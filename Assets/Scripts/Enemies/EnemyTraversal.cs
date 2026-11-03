using UnityEngine;

// Consulta o chão físico dos modos clássico e híbrido; não depende de nomes de objetos.
public class EnemyTraversal : MonoBehaviour
{
    Rigidbody2D _body;
    BoxCollider2D _solid;
    float _airX;
    float _launchedAt = -10f;
    public bool Grounded => SurfaceAt(transform.position.x, transform.position.y + 0.12f, 0.25f, out _);

    void Awake()
    {
        _body = GetComponent<Rigidbody2D>();
        foreach (var c in GetComponents<BoxCollider2D>())
            if (!c.isTrigger) { _solid = c; break; }
    }

    public static bool IsSurface(Collider2D c)
    {
        return c != null && !c.isTrigger && c.GetComponentInParent<EnemyController>() == null
            && c.GetComponentInParent<PlayerController>() == null;
    }

    public static bool SurfaceAt(float x, float fromY, float distance, out RaycastHit2D best)
    {
        best = default;
        foreach (var hit in Physics2D.RaycastAll(new Vector2(x, fromY), Vector2.down, distance))
        {
            if (!IsSurface(hit.collider) || hit.normal.y < 0.7f)
                continue;
            if (best.collider == null || hit.distance < best.distance)
                best = hit;
        }
        return best.collider != null;
    }

    public bool SafeAhead(float dir, float lookAhead = 0.3f)
    {
        float half = _solid != null ? _solid.size.x * 0.5f : 0.5f;
        return SurfaceAt(transform.position.x + dir * (half + lookAhead), transform.position.y + 0.2f, 0.5f, out _);
    }

    public void Move(float dir, float speed, bool climb)
    {
        bool grounded = Grounded;
        if (!grounded || Time.time - _launchedAt < 0.15f)
        {
            SetX(_airX);
            return;
        }
        bool safe = SafeAhead(dir);
        if ((!safe || climb) && (TryJump(dir, climb) || (!safe && climb && TryJump(dir, false))))
            return;
        _airX = safe ? dir * speed : 0f;
        SetX(_airX);
    }

    bool TryJump(float dir, bool climb)
    {
        const float jump = 13.2f;
        float gravity = Mathf.Abs(Physics2D.gravity.y * _body.gravityScale);
        if (gravity < 1f) return false;
        float maxRise = jump * jump / (2f * gravity) - 0.15f;
        // O ponto de chegada precisa ter apoio para a largura inteira, não só um pixel.
        float half = _solid != null ? _solid.size.x * 0.5f : 0.5f;
        for (float distance = 1f; distance <= 5.25f; distance += 0.25f)
        {
            float x = transform.position.x + dir * distance;
            if (!SurfaceAt(x, transform.position.y + maxRise, maxRise + 4.5f, out var hit)) continue;
            float rise = hit.point.y - transform.position.y;
            if (climb && rise < 0.35f) continue;
            if (!climb && SafeAhead(dir) && rise < 0.35f) continue;
            if (!SurfaceAt(x - half, hit.point.y + 0.12f, 0.25f, out var left)
                || !SurfaceAt(x + half, hit.point.y + 0.12f, 0.25f, out var right)
                || Mathf.Abs(left.point.y - hit.point.y) > 0.1f || Mathf.Abs(right.point.y - hit.point.y) > 0.1f) continue;
            float disc = jump * jump - 2f * gravity * rise;
            if (disc <= 0f) continue;
            float flight = (jump + Mathf.Sqrt(disc)) / gravity;
            float vx = dir * distance / flight;
            if (Mathf.Abs(vx) > 6.5f) continue;
            Launch(vx, jump);
            return true;
        }
        return false;
    }

    public void Launch(float vx, float vy)
    {
        _airX = vx;
        _launchedAt = Time.time;
        _body.linearVelocity = new Vector2(vx, vy);
    }
    public void Stop()
    {
        _airX = 0f;
        SetX(0f);
    }
    void SetX(float x)
    {
        var v = _body.linearVelocity; v.x = x; _body.linearVelocity = v;
    }

    public static bool TrySpawnPoint(float x, StageData stage, Vector2 size, out Vector2 feet)
    {
        feet = default;
        // Prioriza a plataforma mais alta que comporte o corpo; senão, o chão.
        var hits = Physics2D.RaycastAll(new Vector2(x, stage.GroundTop + 7f), Vector2.down, 7.3f);
        foreach (var hit in hits)
        {
            if (!IsSurface(hit.collider) || hit.normal.y < 0.7f) continue;
            float half = size.x * 0.5f + 0.05f;
            if (x - half < hit.collider.bounds.min.x || x + half > hit.collider.bounds.max.x) continue;
            var center = hit.point + Vector2.up * (size.y * 0.5f + 0.04f);
            bool blocked = false;
            foreach (var c in Physics2D.OverlapBoxAll(center, size - Vector2.one * 0.04f, 0f))
                if (!c.isTrigger && c != hit.collider) { blocked = true; break; }
            if (blocked) continue;
            feet = hit.point + Vector2.up * 0.02f;
            return true;
        }
        return false;
    }
}
