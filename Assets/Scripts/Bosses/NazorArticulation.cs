using UnityEngine;

public enum NazorPose { Hover, Focus, Release, Roar, Conjure, Windup, Dive, Recover, Transform, Dying }

// A continuous articulated mesh reuses the exact sprite UVs. Wings rotate about
// their roots, arms about shoulders, and tentacles/robe ripple below the waist.
// There are no cutout seams or newly painted pixels. Gameplay colliders stay fixed.
public sealed class NazorArticulation : MonoBehaviour
{
    const int Columns = 72, Rows = 48;
    Mesh _mesh;
    MeshRenderer _renderer;
    Material _material;
    Vector3[] _rest, _vertices;
    Vector2[] _uv, _coordinates;
    Sprite _sprite;
    int _form;
    float _clock, _nextFrame;
    Vector2 _size;
    public int VertexCount => _vertices != null ? _vertices.Length : 0;
    public MeshRenderer Renderer => _renderer;
    public float Clock => _clock;

    public void Setup()
    {
        var child = new GameObject("NazorArticulado");
        child.transform.SetParent(transform, false);
        _renderer = child.AddComponent<MeshRenderer>();
        _material = new Material(Shader.Find("Sprites/Default")) { name = "NazorArteOriginal" };
        _renderer.sharedMaterial = _material;
        _mesh = new Mesh { name = "AsasBracosTentaculosNazor" };
        _mesh.MarkDynamic();
        child.AddComponent<MeshFilter>().sharedMesh = _mesh;
        int n = (Columns + 1) * (Rows + 1);
        _rest = new Vector3[n]; _vertices = new Vector3[n];
        _uv = new Vector2[n]; _coordinates = new Vector2[n];
        var colors = new Color32[n];
        var triangles = new int[Columns * Rows * 6];
        for (int y = 0; y <= Rows; y++) for (int x = 0; x <= Columns; x++)
        {
            int i = y * (Columns + 1) + x;
            _coordinates[i] = new Vector2((float)x / Columns, (float)y / Rows);
            colors[i] = new Color32(255, 255, 255, 255);
            if (x == Columns || y == Rows) continue;
            int t = (y * Columns + x) * 6;
            triangles[t] = i; triangles[t + 1] = i + Columns + 1; triangles[t + 2] = i + 1;
            triangles[t + 3] = i + 1; triangles[t + 4] = i + Columns + 1; triangles[t + 5] = i + Columns + 2;
        }
        _mesh.vertices = _vertices; _mesh.uv = _uv; _mesh.colors32 = colors; _mesh.triangles = triangles;
    }

    public void Animate(Sprite source, int form, NazorPose pose, float poseTime, Color color, bool frozen)
    {
        if (source == null || _mesh == null) return;
        if (frozen && _sprite != null) { _material.color = color; return; }
        bool changed = _sprite != source || _form != form;
        if (changed)
        {
            _sprite = source; _form = form;
            _size = source.bounds.size;
            Rect rect = source.textureRect;
            var texture = source.texture;
            _material.mainTexture = texture;
            for (int i = 0; i < _rest.Length; i++)
            {
                Vector2 p = _coordinates[i];
                _rest[i] = new Vector3((p.x - .5f) * _size.x, (p.y - .5f) * _size.y, 0f);
                _uv[i] = new Vector2((rect.x + p.x * rect.width) / texture.width, (rect.y + p.y * rect.height) / texture.height);
            }
            _mesh.uv = _uv;
        }
        _material.color = color;
        if (!frozen) _clock += Time.deltaTime;
        if (!changed && (_clock < _nextFrame || frozen)) return;
        _nextFrame = _clock + 1f / 24f;
        for (int i = 0; i < _vertices.Length; i++)
            _vertices[i] = EvaluatePoint(_coordinates[i], _clock, pose, poseTime);
        _mesh.vertices = _vertices;
        // Fixed generous bounds avoid culling the tips during a downstroke.
        _mesh.bounds = new Bounds(Vector3.zero, new Vector3(_size.x + 2f, _size.y + 2f, 1f));
    }

    static float Smooth(float a, float b, float value) => Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(a, b, value));
    static Vector2 Rotate(Vector2 point, Vector2 pivot, float angle)
    {
        float sine = Mathf.Sin(angle), cosine = Mathf.Cos(angle);
        Vector2 d = point - pivot;
        return pivot + new Vector2(d.x * cosine - d.y * sine, d.x * sine + d.y * cosine);
    }

    public Vector3 EvaluatePoint(Vector2 uv, float clock, NazorPose pose, float poseTime)
    {
        float u = uv.x, v = uv.y, side = u < .5f ? -1f : 1f;
        float distance = Mathf.Abs(u - .5f);
        Vector2 point = new Vector2((u - .5f) * _size.x, (v - .5f) * _size.y);
        float preparation = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(poseTime / .35f));
        float release = pose == NazorPose.Release ? Mathf.Exp(-poseTime * 7f) : 0f;
        float winding = pose == NazorPose.Windup ? preparation : 0f;
        float dive = pose == NazorPose.Dive ? 1f : 0f;
        float roar = pose == NazorPose.Roar ? preparation : 0f;
        float conjure = pose == NazorPose.Conjure ? preparation : 0f;
        float recovering = pose == NazorPose.Recover ? Mathf.Exp(-poseTime * 7f) : 0f;
        float dying = pose == NazorPose.Dying ? Mathf.Min(1f, poseTime * .8f) : 0f;
        float transform = pose == NazorPose.Transform ? Mathf.Sin(Mathf.Min(poseTime, 1f) * Mathf.PI) : 0f;

        // Soft skin weights meet at shoulders, never across the central face/chest.
        float arms = _form == 1 ? 0f : (1f - Smooth(.055f, .14f, Mathf.Abs(distance - .20f)))
            * Smooth(.32f, .44f, v) * (1f - Smooth(.64f, .72f, v));
        float wing = Smooth(.14f, .31f, distance) * Smooth(_form == 3 ? .16f : .49f, _form == 3 ? .38f : .65f, v) * (1f - arms * .88f);
        float wingBeat = Mathf.Sin(clock * (_form == 1 ? 4.8f : 4.0f) + (_form == 3 ? v * 3.5f : 0f));
        float wingAngle = wingBeat * .20f * (1f - dying * .85f) - winding * .33f - dive * .48f - recovering * .45f + release * .38f + transform * .32f - dying * .42f;
        float hingeY = _form == 3 ? Mathf.Lerp(.32f, .72f, Smooth(.2f, .85f, v)) : .64f;
        Vector2 hinge = new Vector2(side * .16f * _size.x, (hingeY - .5f) * _size.y);
        point = Vector2.Lerp(point, Rotate(point, hinge, -side * wingAngle), wing);
        float armAngle = Mathf.Sin(clock * 2.5f + side * .3f) * .035f + conjure * .38f + roar * .19f - release * .27f - dying * .22f;
        Vector2 shoulder = new Vector2(side * .13f * _size.x, (_form == 3 ? .03f : -.02f) * _size.y);
        point = Vector2.Lerp(point, Rotate(point, shoulder, side * armAngle), arms);

        // Independent tip phases make the lower silhouette curl, rather than sway as one card.
        float tail = 1f - Smooth(.08f, _form == 1 ? .47f : .38f, v);
        point.x += tail * (Mathf.Sin(clock * 3.5f + u * 17f + v * 9f) * .17f + dive * side * .24f);
        point.y += tail * (Mathf.Sin(clock * 4.2f + u * 21f) * .09f + roar * .13f - dying * .10f);
        float chest = (1f - Smooth(.07f, .18f, distance)) * Smooth(.30f, .48f, v) * (1f - Smooth(.66f, .80f, v));
        point.x += (u - .5f) * chest * (Mathf.Sin(clock * 2.3f) * .08f + roar * .22f + conjure * .10f);
        float jawLow = _form == 1 ? .30f : _form == 2 ? .57f : .68f;
        float jawHigh = _form == 1 ? .45f : _form == 2 ? .74f : .77f;
        float jaw = (1f - Smooth(.035f, .085f, distance)) * Smooth(jawLow, jawLow + .07f, v) * (1f - Smooth(jawHigh, jawHigh + .05f, v));
        point.y -= jaw * (roar * .16f + release * .08f + dive * .07f + conjure * .035f);
        if (_form == 3)
        {
            float halo = (1f - Smooth(.10f, .19f, distance)) * Smooth(.82f, .90f, v);
            point = Vector2.Lerp(point, Rotate(point, new Vector2(0f, .38f * _size.y), Mathf.Sin(clock * 1.7f) * .065f), halo);
            point.y += halo * (Mathf.Sin(clock * 2.5f) * .06f + conjure * .15f);
        }
        // Recoil affects the upper body, while the lower extremities lag behind it.
        point.x += Smooth(.24f, .65f, v) * (-release * .16f + winding * .10f);
        point.y -= dying * Smooth(.35f, .8f, v) * .18f;
        // The torso leans into the strike, then returns over several recovery poses.
        point = Rotate(point, Vector2.zero, winding * .07f - dive * .16f - recovering * .16f + release * .045f);
        return new Vector3(point.x, point.y, 0f);
    }

    void OnDestroy()
    {
        if (_mesh != null) Destroy(_mesh);
        if (_material != null) Destroy(_material);
    }
}
