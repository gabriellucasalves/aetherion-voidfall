using UnityEngine;

// Provided Nazor designs replace the complete body in forms two and three.
// The complete original textures drive a local articulated mesh in all three forms.
[DefaultExecutionOrder(160)]
public sealed class BossFormVisual : MonoBehaviour
{
    public const bool UseFinalSheets = true;

    VoidDevourerBoss _boss;
    SpriteRenderer _body;
    NazorArticulation _rig;
    public NazorArticulation Rig => _rig;
    static readonly Sprite[] Forms = new Sprite[2];
    static readonly Sprite[] Portraits = new Sprite[2];
    int _form = 1;
    public int Form => _form;
    public Sprite FormSprite => UseFinalSheets ? NazorSheets.GetFrame($"forma{_form}_idle_8f", 0) : _form > 1 ? Forms[_form - 2] : null;
    public Sprite PortraitSprite => UseFinalSheets ? FormSprite : _form > 1 ? Portraits[_form - 2] : null;

    public void Setup(VoidDevourerBoss boss)
    {
        _boss = boss;
        _body = GetComponent<SpriteRenderer>();
        _rig = gameObject.AddComponent<NazorArticulation>();
        _rig.Enabled = !UseFinalSheets;
        _rig.Setup();

        if (UseFinalSheets)
        {
            _body.forceRenderingOff = false;
            // Preload das folhas
            NazorSheets.Get("forma1_idle_8f");
            NazorSheets.Get("forma2_idle_8f");
            NazorSheets.Get("forma3_idle_8f");
        }
        else
        {
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
                Portraits[i] = Sprite.Create(texture, new Rect(texture.width * .32f, texture.height * .30f, texture.width * .36f, texture.height * .64f), new Vector2(.5f, .5f), 100f, 0, SpriteMeshType.FullRect);
            }
        }
        SetForm(1);
    }

    public void SetForm(int form)
    {
        _form = Mathf.Clamp(form, 1, 3);
        if (UseFinalSheets)
        {
            transform.localScale = Vector3.one;
            if (_boss != null)
                _boss.SetFormHurtbox(_form);
        }
        else
        {
            float size = _form == 1 ? 1f : _form == 2 ? 1.08f : 1.18f;
            transform.localScale = new Vector3(Mathf.Sign(transform.localScale.x) * size, size, 1f);
            var core = GetComponentInChildren<BossNucleus>(true);
            if (core != null)
                core.transform.localPosition = _form == 1 ? new Vector3(-.3125f, -.9375f, 0f) : new Vector3(0f, _form == 2 ? .25f : .50f, 0f);
        }
    }

    void LateUpdate()
    {
        if (_body == null) return;

        if (UseFinalSheets)
        {
            transform.localRotation = Quaternion.identity;
            if (_boss == null || (_boss.EnemyCtrl != null && _boss.EnemyCtrl.IsFrozen)) return;

            Sprite s = null;
            if (_boss.IsDamageFlashing)
            {
                int dFrame = Mathf.Clamp(Mathf.FloorToInt((0.17f - _boss.FlashTimer) * 12f), 0, 1);
                s = NazorSheets.GetFrame($"forma{_form}_dano_2f", dFrame);
            }
            else
            {
                var pose = _boss.AnimationPose;
                float elapsed = _boss.PoseElapsed;
                switch (pose)
                {
                    case NazorPose.Hover:
                    case NazorPose.Recover:
                        s = NazorSheets.GetFrame($"forma{_form}_idle_8f", Mathf.FloorToInt(elapsed * 8f) % 8);
                        break;
                    case NazorPose.Focus:
                    case NazorPose.Windup:
                    case NazorPose.Conjure:
                    case NazorPose.Roar:
                        s = NazorSheets.GetFrame($"forma{_form}_ataque_4f", Mathf.Min(1, Mathf.FloorToInt(elapsed * 10f)));
                        break;
                    case NazorPose.Release:
                    case NazorPose.Dive:
                        s = NazorSheets.GetFrame($"forma{_form}_ataque_4f", Mathf.Min(3, 2 + Mathf.FloorToInt(elapsed * 10f)));
                        break;
                    case NazorPose.Transform:
                        int source = Mathf.Max(1, _form - 1);
                        s = elapsed < BossData.TransitionDuration
                            ? NazorSheets.GetFrame($"forma{source}_transformacao_f{source + 1}_8f", Mathf.Clamp(Mathf.FloorToInt(elapsed / BossData.TransitionDuration * 8f), 0, 7))
                            : NazorSheets.GetFrame($"forma{_form}_idle_8f", Mathf.FloorToInt(elapsed * 8f) % 8);
                        break;
                    case NazorPose.Dying:
                        if (_form == 3)
                            s = NazorSheets.GetFrame("forma3_morte_8f", Mathf.Clamp(Mathf.FloorToInt(elapsed * 8f), 0, 7));
                        else
                            s = NazorSheets.GetFrame($"forma{_form}_dano_2f", 0);
                        break;
                    default:
                        s = NazorSheets.GetFrame($"forma{_form}_idle_8f", Mathf.FloorToInt(elapsed * 8f) % 8);
                        break;
                }
            }

            if (s != null) _body.sprite = s;
            _body.color = new Color(1f, 1f, 1f, _body.color.a);
            return;
        }

        if (_rig == null) return;
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
