using UnityEngine;

// Provided Nazor designs replace the complete body in forms two and three.
// The complete original textures drive a local articulated mesh in all three forms.
[DefaultExecutionOrder(160)]
public sealed class BossFormVisual : MonoBehaviour
{
    VoidDevourerBoss _boss;
    SpriteRenderer _body;
    NazorArticulation _rig;
    public NazorArticulation Rig => _rig;
    static readonly Sprite[] Forms = new Sprite[2];
    static readonly Sprite[] Portraits = new Sprite[2];
    int _form = 1;
    public int Form => _form;
    public Sprite FormSprite => _form > 1 ? Forms[_form - 2] : null;
    public Sprite PortraitSprite => _form > 1 ? Portraits[_form - 2] : null;

    public void Setup(VoidDevourerBoss boss)
    {
        _boss = boss;
        _body = GetComponent<SpriteRenderer>();
        _rig = gameObject.AddComponent<NazorArticulation>();
        _rig.Setup();
        _body.forceRenderingOff = true;
        for (int i = 0; i < Forms.Length; i++)
        {
            if (Forms[i] != null) continue;
            var texture = Resources.Load<Texture2D>("Bosses/DevoradorDoVazio/nazor/form" + (i + 2));
            if (texture == null) { Debug.LogError("Missing Nazor form " + (i + 2)); continue; }
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
            Forms[i] = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(.5f, .5f), texture.width / 7.4f, 0, SpriteMeshType.FullRect);
            Forms[i].name = "NazorForma" + (i + 2);
            // Head/chest crop reuses the exact supplied texture for dialogue portraits.
            Portraits[i] = Sprite.Create(texture, new Rect(texture.width * .32f, texture.height * .30f, texture.width * .36f, texture.height * .64f), new Vector2(.5f, .5f), 100f, 0, SpriteMeshType.FullRect);
        }
        SetForm(1);
    }

    public void SetForm(int form)
    {
        _form = Mathf.Clamp(form, 1, 3);
        float size = _form == 1 ? 1f : _form == 2 ? 1.08f : 1.18f;
        transform.localScale = new Vector3(Mathf.Sign(transform.localScale.x) * size, size, 1f);
        var core = GetComponentInChildren<BossNucleus>(true);
        if (core != null)
            core.transform.localPosition = _form == 1 ? new Vector3(-.3125f, -.9375f, 0f) : new Vector3(0f, _form == 2 ? .25f : .50f, 0f);
    }

    void LateUpdate()
    {
        if (_body == null || _rig == null) return;
        bool dying = _boss.CurrentState == VoidDevourerBoss.State.Defeated;
        if (_form > 1 && FormSprite != null)
        {
            _body.sprite = FormSprite;
            if (!dying) _body.color = _boss.IsDamageFlashing ? new Color(1f, .60f, .70f) : Color.white;
        }
        transform.localRotation = Quaternion.identity;
        _rig.Renderer.sortingOrder = _body.sortingOrder;
        _rig.Renderer.sortingLayerID = _body.sortingLayerID;
        _rig.Animate(_body.sprite, _form, _boss.AnimationPose, _boss.PoseElapsed, _body.color, _boss.EnemyCtrl != null && _boss.EnemyCtrl.IsFrozen);
    }
}
