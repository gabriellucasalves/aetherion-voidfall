using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Controles virtuais para jogar no celular (WebGL incluso). No PC ficam ocultos.
public class MobileControls : MonoBehaviour
{
    public static float Move { get; private set; }
    public static bool JumpHeld { get; private set; }
    public static bool AttackHeld { get; private set; }
    public static bool BlockHeld { get; private set; }

    static bool _jumpPressed;
    static bool _dashPressed;

    public static bool ConsumeJumpDown()
    {
        if (!_jumpPressed)
            return false;
        _jumpPressed = false;
        return true;
    }

    public static bool ConsumeDashDown()
    {
        if (!_dashPressed)
            return false;
        _dashPressed = false;
        return true;
    }

    public static bool ShouldShow()
    {
        if (Application.isMobilePlatform)
            return true;
        return Input.touchSupported && SystemInfo.deviceType == DeviceType.Handheld;
    }

    public static void Attach(Transform hudCanvas)
    {
        if (!ShouldShow() || hudCanvas == null)
            return;
        if (hudCanvas.GetComponentInChildren<MobileControls>() != null)
            return;

        var root = new GameObject("MobilePad");
        root.transform.SetParent(hudCanvas, false);
        var pad = root.AddComponent<MobileControls>();
        pad.Build(root.transform);
    }

    void OnDisable()
    {
        Move = 0f;
        JumpHeld = false;
        AttackHeld = false;
        BlockHeld = false;
    }

    void Build(Transform parent)
    {
        var rect = parent.gameObject.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        BuildStick(parent);
        HoldButton(parent, "PULAR", new Vector2(620f, -320f), new Vector2(170f, 170f),
            () => { JumpHeld = true; _jumpPressed = true; },
            () => JumpHeld = false);
        HoldButton(parent, "ATACAR", new Vector2(790f, -170f), new Vector2(140f, 140f),
            () => AttackHeld = true,
            () => AttackHeld = false);
        HoldButton(parent, "ESCUDO", new Vector2(470f, -170f), new Vector2(130f, 130f),
            () => BlockHeld = true,
            () => BlockHeld = false);
        HoldButton(parent, "DASH", new Vector2(790f, -360f), new Vector2(110f, 80f),
            () => _dashPressed = true,
            () => { });
        ClickButton(parent, "II", new Vector2(880f, 430f), new Vector2(72f, 72f), () =>
        {
            var stage = FindAnyObjectByType<GroundT1Controller>();
            if (stage != null)
                stage.RequestPause();
        });
    }

    void BuildStick(Transform parent)
    {
        var baseGo = Circle(parent, "Stick", new Vector2(-720f, -280f), 220f, new Color(1f, 1f, 1f, 0.12f));
        var knob = Circle(baseGo.transform, "Knob", Vector2.zero, 90f, new Color(0.93f, 0.78f, 0.38f, 0.85f));
        var stick = baseGo.gameObject.AddComponent<VirtualStick>();
        stick.Setup(knob.rectTransform, 90f);
    }

    static void HoldButton(Transform parent, string label, Vector2 position, Vector2 size, System.Action down, System.Action up)
    {
        var image = UiKit.Panel(parent, label, size, position, new Color(0.18f, 0.1f, 0.28f, 0.72f));
        UiKit.Label(image.transform, label, 18, Vector2.zero, MenuTheme.SoftIvory, size);
        var trigger = image.gameObject.AddComponent<EventTrigger>();
        Add(trigger, EventTriggerType.PointerDown, _ => down?.Invoke());
        Add(trigger, EventTriggerType.PointerUp, _ => up?.Invoke());
        Add(trigger, EventTriggerType.PointerExit, _ => up?.Invoke());
    }

    static void ClickButton(Transform parent, string label, Vector2 position, Vector2 size, System.Action click)
    {
        UiKit.Button(parent, label, position, size, () => click?.Invoke());
    }

    static Image Circle(Transform parent, string name, Vector2 position, float diameter, Color color)
    {
        var image = UiKit.Panel(parent, name, new Vector2(diameter, diameter), position, color);
        return image;
    }

    static void Add(EventTrigger trigger, EventTriggerType type, UnityEngine.Events.UnityAction<BaseEventData> action)
    {
        var entry = new EventTrigger.Entry { eventID = type };
        entry.callback.AddListener(action);
        trigger.triggers.Add(entry);
    }

    class VirtualStick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        RectTransform _knob;
        float _range;

        public void Setup(RectTransform knob, float range)
        {
            _knob = knob;
            _range = range;
        }

        public void OnPointerDown(PointerEventData eventData) => OnDrag(eventData);

        public void OnDrag(PointerEventData eventData)
        {
            if (_knob == null)
                return;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                transform as RectTransform, eventData.position, eventData.pressEventCamera, out var local);
            local = Vector2.ClampMagnitude(local, _range);
            _knob.anchoredPosition = local;
            Move = Mathf.Abs(local.x) < 12f ? 0f : Mathf.Clamp(local.x / _range, -1f, 1f);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (_knob != null)
                _knob.anchoredPosition = Vector2.zero;
            Move = 0f;
        }
    }
}
