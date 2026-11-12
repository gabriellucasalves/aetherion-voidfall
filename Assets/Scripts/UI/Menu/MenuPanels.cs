using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

// Painéis de jogo no kit novo: PAUSA, VOCÊ CAIU, PARABÉNS (fim da demo T1) e FIM DA PARTE 1 (Devorador).
// Cada um devolve o GameObject raiz (overlay escuro em tela cheia) — os controllers continuam só ligando/desligando.
public static class MenuPanels
{
    public const string EndPart1Text = "Nazor foi derrotado. Aetherion resistiu.\nObrigado por jogar AETHERION: APOCALIPSE!";
    public const string DemoCompleteText = "Você concluiu a demo de AETHERION: APOCALIPSE.\nObrigado por jogar — novas fases e ajustes vêm a caminho.";

    public static GameObject Pause(Transform canvas, UnityAction onContinue, UnityAction onMenu, UnityAction onMap)
    {
        bool map = onMap != null;
        var go = MenuKit.Modal(canvas, "Pausa", "PAUSA", 210, map ? 148 : 128, out var box, 0.62f);
        var first = MenuKit.ModalButton(box, "CONTINUAR", 30, 0, onContinue);
        if (map) MenuKit.ModalButton(box, "MAPA", 30, 1, onMap);
        var last = MenuKit.ModalButton(box, "MENU", 30, map ? 2 : 1, onMenu);
        var music = AudioVolumeControls.Add(box, "MÚSICA", map ? 94 : 74, 210, true);
        var effects = AudioVolumeControls.Add(box, "EFEITOS", map ? 115 : 95, 210, false);
        Finish(go, box, first);
        AudioVolumeControls.Link(first, music, effects, last);
        return go;
    }

    // retry = botão TENTAR DE NOVO (o controller mostra/esconde e chama Restack)
    public static GameObject Defeat(Transform canvas, UnityAction onRetry, UnityAction onMenu, UnityAction onMap, out Button retry)
    {
        var go = MenuKit.Modal(canvas, "Derrota", "VOCÊ CAIU", 150, 116, out var box, 0.72f);
        MenuKit.Label(box, "O Vazio venceu desta vez.", MenuKit.BodyFont, 32, MenuKit.Dim, 10, 27, 130, 10, TextAnchor.MiddleCenter);
        retry = MenuKit.ModalButton(box, "TENTAR DE NOVO", 42, 0, onRetry, 108f);
        var menu = MenuKit.ModalButton(box, "MENU", 42, 1, onMenu, 108f);
        if (onMap != null) MenuKit.ModalButton(box, "MAPA", 42, 2, onMap, 108f);
        Finish(go, box, menu);
        return go;
    }

    public static GameObject DemoComplete(Transform canvas, UnityAction onMenu)
    {
        var go = MenuKit.Modal(canvas, "DemoFim", "PARABÉNS!", 250, 112, out var box, 0.78f);
        MenuKit.Label(box, DemoCompleteText, MenuKit.BodyFont, 34, MenuKit.Ivory, 12, 30, 226, 46, TextAnchor.MiddleCenter, true, 0.9f);
        var menu = MenuKit.ModalButton(box, "MENU", 84, 0, onMenu);
        Finish(go, box, menu);
        return go;
    }

    public static GameObject EndPart1(Transform canvas, UnityAction onMap, UnityAction onMenu)
    {
        var go = MenuKit.Modal(canvas, "EndPart1Panel", "VITÓRIA", 240, 128, out var box, 0.55f);
        MenuKit.Label(box, EndPart1Text, MenuKit.BodyFont, 36, MenuKit.Ivory, 12, 30, 216, 40, TextAnchor.MiddleCenter, true, 0.9f);
        var map = MenuKit.ModalButton(box, "MAPA", 78, 0, onMap);
        MenuKit.ModalButton(box, "MENU", 78, 1, onMenu);
        Finish(go, box, map);
        go.SetActive(true);   // este aparece na hora (fim da cutscene)
        return go;
    }

    static void Finish(GameObject go, RectTransform box, Selectable first)
    {
        Restack(box);
        go.AddComponent<MenuModalFocus>().First = first;
        go.SetActive(false);
    }

    // reempilha os botões ATIVOS do painel (ex.: TENTAR DE NOVO escondido não deixa buraco) e liga a navegação
    public static void Restack(RectTransform box)
    {
        var buttons = new System.Collections.Generic.List<Button>();
        foreach (Transform child in box)
        {
            var b = child.GetComponent<Button>();
            if (b != null && child.gameObject.activeSelf) buttons.Add(b);
        }
        if (buttons.Count == 0) return;
        float top = -buttons[0].GetComponent<RectTransform>().anchoredPosition.y;
        float bestTop = top;
        foreach (var b in buttons) bestTop = Mathf.Min(bestTop, -b.GetComponent<RectTransform>().anchoredPosition.y);
        for (int i = 0; i < buttons.Count; i++)
        {
            var rt = buttons[i].GetComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, -(bestTop + i * 20f * MenuKit.Px));
        }
        MainMenuController.ChainVertical(buttons);
    }

    // atalho para o controller: mostra/esconde o TENTAR DE NOVO e reempilha
    public static void SetRetryVisible(GameObject defeatPanel, Button retry, bool visible)
    {
        if (retry == null) return;
        retry.gameObject.SetActive(visible);
        Restack((RectTransform)retry.transform.parent);
        var focus = defeatPanel != null ? defeatPanel.GetComponent<MenuModalFocus>() : null;
        if (focus != null) focus.First = visible ? (Selectable)retry : focus.First;
    }
}
