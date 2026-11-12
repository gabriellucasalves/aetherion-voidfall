using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Map-only touch UI: independent of combat controls and destroyed with the map.
public sealed class WorldMapTouchControls : MonoBehaviour
{
    GameObject _root;
    WorldMapDirectionButton _left, _right;
    Button _enter;
    bool _blocked;
    bool _touchSeen;
    bool _focused = true;
    bool _portrait;
    bool _layoutReady;
    public bool Visible => _root != null && _root.activeSelf;
    public float Move => !_blocked && _focused && Visible ? (_right.Held ? 1f : 0f) - (_left.Held ? 1f : 0f) : 0f;

    public static WorldMapTouchControls Attach(Transform owner, UnityAction enter, UnityAction menu)
    {
        var canvas = UiKit.CreateCanvas(owner, "MapTouchCanvas");
        canvas.sortingOrder = 30;
        var scaler = canvas.GetComponent<CanvasScaler>();
        // At 390px wide each 148-unit button is 45px high; landscape keeps large targets.
        scaler.referenceResolution = new Vector2(1280f, 720f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
        var controls = canvas.gameObject.AddComponent<WorldMapTouchControls>();
        controls.Build(enter, menu);
        return controls;
    }

    void Build(UnityAction enter, UnityAction menu)
    {
        UiKit.EnsureEventSystem();
        Input.multiTouchEnabled = true;
        _root = new GameObject("MapTouchButtons", typeof(RectTransform));
        _root.transform.SetParent(transform, false);
        var rt = (RectTransform)_root.transform;
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        var left = MakeButton("MapWalkLeft", "<", new Vector2(0, 0), new Vector2(112, 106), 148, () => { });
        var right = MakeButton("MapWalkRight", ">", new Vector2(0, 0), new Vector2(286, 106), 148, () => { });
        _left = left.gameObject.AddComponent<WorldMapDirectionButton>();
        _right = right.gameObject.AddComponent<WorldMapDirectionButton>();
        _enter = MakeButton("MapEnter", "ENTRAR", new Vector2(1, 0), new Vector2(-326, 106), 244, enter);
        MakeButton("MapMenu", "MENU", new Vector2(1, 0), new Vector2(-112, 106), 150, menu);
        ApplyButtonLayout();
        ApplyVisibility();
    }

    Button MakeButton(string name, string label, Vector2 anchor, Vector2 position, float width, UnityAction action)
    {
        var button = UiKit.Button(_root.transform, label, position, new Vector2(width, 148), action);
        button.name = name;
        var rect = button.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = anchor;
        button.navigation = new Navigation { mode = Navigation.Mode.None };
        var text = button.GetComponentInChildren<Text>();
        text.fontSize = label.Length == 1 ? 64 : 36;
        text.raycastTarget = false;
        if (label.Length == 1)
        {
            // Draw the chevron directly: mobile font atlases may omit arrow glyphs.
            text.enabled = false;
            bool right = label == ">";
            for (int i = 0; i < 2; i++)
            {
                var stroke = UiKit.Panel(button.transform, "ArrowStroke" + i, new Vector2(68f, 12f),
                    new Vector2(0f, i == 0 ? 20f : -20f), MenuTheme.SoftIvory);
                stroke.raycastTarget = false;
                float angle = (i == 0 ? 45f : -45f) * (right ? -1f : 1f);
                stroke.rectTransform.localRotation = Quaternion.Euler(0f, 0f, angle);
            }
        }
        return button;
    }

    public void SetCanEnter(bool allowed) { _enter.interactable = allowed && !_blocked; }

    public void SetBlocked(bool blocked)
    {
        _blocked = blocked;
        if (blocked) ResetHeld();
        ApplyVisibility();
    }

    void Update()
    {
        if (Input.touchCount > 0) _touchSeen = true;
        ApplyButtonLayout();
        ApplyVisibility();
    }

    void ApplyButtonLayout()
    {
        bool portrait = Screen.height > Screen.width;
        if (_layoutReady && portrait == _portrait) return;
        _layoutReady = true;
        _portrait = portrait;
        ResetHeld();
        foreach (RectTransform button in _root.transform)
        {
            var position = button.anchoredPosition;
            // Keep portrait controls above the browser's existing rotate-phone hint.
            position.y = portrait ? 220f : 106f;
            button.anchoredPosition = position;
        }
    }

    void ApplyVisibility()
    {
        bool show = !_blocked && (MobileControls.ShouldShow() || _touchSeen);
        if (_root != null && _root.activeSelf != show) _root.SetActive(show);
    }

    void ResetHeld()
    {
        if (_left != null) _left.Release();
        if (_right != null) _right.Release();
    }

    void OnApplicationFocus(bool focused) { _focused = focused; if (!focused) ResetHeld(); }
    void OnApplicationPause(bool paused) { if (paused) ResetHeld(); }
    void OnDisable() { ResetHeld(); }
}

// Pointer ownership prevents a second finger's release from cancelling a held direction.
public sealed class WorldMapDirectionButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    const int NoPointer = int.MinValue;
    int _pointer = NoPointer;
    public bool Held => _pointer != NoPointer;
    public void OnPointerDown(PointerEventData data) { if (_pointer == NoPointer) _pointer = data.pointerId; }
    public void OnPointerUp(PointerEventData data) { if (_pointer == data.pointerId) Release(); }
    public void OnPointerExit(PointerEventData data) { if (_pointer == data.pointerId) Release(); }
    public void Release() { _pointer = NoPointer; }
    void OnDisable() { Release(); }
}
