using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class UIManager : MonoBehaviour
{
    [Header("Цвета игроков")]
    public Color player1Color = new Color(1f, 0.5f, 0f);
    public Color player2Color = new Color(0.6f, 0.4f, 0.2f);
    public Color player3Color = new Color(0.6f, 0.6f, 0.6f);
    public Color player4Color = new Color(0.2f, 0.4f, 1f);

    [Header("Имена")]
    public string[] playerNames = { "Тигр", "Собака", "Кот", "Пингвин" };

    [Header("Спавн")]
    public Vector3 spawn1 = new Vector3(8.33f, 2.6f, -8.12f);
    public Vector3 spawn2 = new Vector3(9.07f, 2.6f, -8.2f);
    public Vector3 spawn3 = new Vector3(9.76f, 2.7f, -7.9f);
    public Vector3 spawn4 = new Vector3(10.43f, 2.7f, -8f);

    private Canvas canvas;
    private Text timerText, eliminationText, winText, scoresText, controlsText;
    private GameObject winPanel;
    private GameObject[] spinInd = new GameObject[4];
    private Image[] spinFill = new Image[4];
    private Text[] spinLbl = new Text[4];
    private PlayerController[] players;
    private Sprite circle, rounded;

    void Awake() { circle = MakeCircle(); rounded = MakeRounded(); CreateUI(); }
    void Start() { var gm = FindObjectOfType<GameManager>(); if (gm != null) players = gm.players; }

    void CreateUI()
    {
        var co = new GameObject("GameCanvas");
        canvas = co.AddComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 100;
        var sc = co.AddComponent<CanvasScaler>(); sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; sc.referenceResolution = new Vector2(1920, 1080);
        co.AddComponent<GraphicRaycaster>();

        MkText(co.transform, "ПОЛ — ЭТО ЛАВА!", 28, new Color(1, 0.3f, 0.1f), new Vector2(0.5f, 1), new Vector2(0, -120), FontStyle.Bold);
        timerText = MkText(co.transform, "0", 54, Color.white, new Vector2(0.5f, 1), new Vector2(0, -40), FontStyle.Bold);
        var bg = new GameObject("BG"); bg.transform.SetParent(timerText.transform, false); bg.transform.SetAsFirstSibling();
        var bgi = bg.AddComponent<Image>(); bgi.sprite = rounded; bgi.color = new Color(0.1f, 0.1f, 0.2f, 0.85f); bgi.type = Image.Type.Sliced;
        var bgr = bg.GetComponent<RectTransform>(); bgr.anchorMin = Vector2.zero; bgr.anchorMax = Vector2.one; bgr.offsetMin = new Vector2(-20, -10); bgr.offsetMax = new Vector2(20, 10);

        eliminationText = MkText(co.transform, "", 42, Color.red, new Vector2(0.5f, 0.5f), new Vector2(0, 150), FontStyle.Bold);
        controlsText = MkText(co.transform, "Тигр:WASD+F+R+T+V+Z/X | Собака:Стрелки+RShift+RCtrl+RAlt+Enter+,/.\nКот:IJKL+H+Y+G+B+N/M | Пингвин:Num8456+1+3+7+9+////*", 16, new Color(1, 1, 1, 0.7f), new Vector2(0.5f, 0), new Vector2(0, 20), FontStyle.Normal);

        Color[] clrs = { player1Color, player2Color, player3Color, player4Color };
        Vector2[] anch = { new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 0), new Vector2(1, 0) };
        Vector2[] offs = { new Vector2(100, -100), new Vector2(-100, -100), new Vector2(100, 100), new Vector2(-100, 100) };
        for (int i = 0; i < 4; i++) MkSpinInd(i, co.transform, anch[i], offs[i], clrs[i]);

        MkWinPanel(co.transform);
    }

    void MkWinPanel(Transform p)
    {
        winPanel = new GameObject("WinPanel"); winPanel.transform.SetParent(p, false);
        var pi = winPanel.AddComponent<Image>(); pi.color = new Color(0, 0, 0, 0.7f);
        var pr = winPanel.GetComponent<RectTransform>(); pr.anchorMin = Vector2.zero; pr.anchorMax = Vector2.one; pr.offsetMin = pr.offsetMax = Vector2.zero;

        var card = new GameObject("Card"); card.transform.SetParent(winPanel.transform, false);
        var ci = card.AddComponent<Image>(); ci.sprite = rounded; ci.color = new Color(1, 0.95f, 0.85f); ci.type = Image.Type.Sliced;
        var cr2 = card.GetComponent<RectTransform>(); cr2.anchorMin = new Vector2(0.2f, 0.15f); cr2.anchorMax = new Vector2(0.8f, 0.85f); cr2.offsetMin = cr2.offsetMax = Vector2.zero;

        var hdr = new GameObject("Hdr"); hdr.transform.SetParent(card.transform, false);
        var hi = hdr.AddComponent<Image>(); hi.sprite = rounded; hi.color = new Color(1, 0.8f, 0.2f); hi.type = Image.Type.Sliced;
        var hr = hdr.GetComponent<RectTransform>(); hr.anchorMin = new Vector2(0, 0.8f); hr.anchorMax = new Vector2(1, 1); hr.offsetMin = new Vector2(15, 0); hr.offsetMax = new Vector2(-15, -15);

        MkText(hdr.transform, "ПОСЛЕДНИЙ НА МЕБЕЛИ", 32, new Color(0.5f, 0.25f, 0), new Vector2(0.5f, 0.7f), Vector2.zero, FontStyle.Bold);
        winText = MkText(hdr.transform, "", 58, Color.white, new Vector2(0.5f, 0.3f), Vector2.zero, FontStyle.Bold);
        MkText(card.transform, "✦ Порядок падения ✦", 28, new Color(0.4f, 0.2f, 0.1f), new Vector2(0.5f, 0.72f), Vector2.zero, FontStyle.Bold);
        scoresText = MkText(card.transform, "", 28, new Color(0.2f, 0.1f, 0), new Vector2(0.5f, 0.4f), Vector2.zero, FontStyle.Bold);
        scoresText.lineSpacing = 1.4f;

        var btn = new GameObject("Btn"); btn.transform.SetParent(card.transform, false);
        var bi = btn.AddComponent<Image>(); bi.sprite = rounded; bi.color = new Color(0.3f, 0.8f, 0.3f); bi.type = Image.Type.Sliced;
        var br = btn.GetComponent<RectTransform>(); br.anchorMin = br.anchorMax = new Vector2(0.5f, 0.08f); br.sizeDelta = new Vector2(380, 70);
        MkText(btn.transform, "Нажми [R] — играть ещё!", 28, Color.white, new Vector2(0.5f, 0.5f), Vector2.zero, FontStyle.Bold);

        winPanel.SetActive(false);
    }

    void MkSpinInd(int i, Transform p, Vector2 a, Vector2 o, Color c)
    {
        var ind = new GameObject("Spin" + i); ind.transform.SetParent(p, false);
        var r = ind.AddComponent<RectTransform>(); r.anchorMin = r.anchorMax = a; r.anchoredPosition = o; r.sizeDelta = new Vector2(140, 140);
        var bg = new GameObject("BG"); bg.transform.SetParent(ind.transform, false);
        var bgi = bg.AddComponent<Image>(); bgi.color = new Color(0, 0, 0, 0.5f); bgi.sprite = circle;
        var bgr = bg.GetComponent<RectTransform>(); bgr.anchorMin = Vector2.zero; bgr.anchorMax = Vector2.one; bgr.offsetMin = bgr.offsetMax = Vector2.zero;
        var fl = new GameObject("Fill"); fl.transform.SetParent(ind.transform, false);
        var fi = fl.AddComponent<Image>(); fi.color = c; fi.sprite = circle; fi.type = Image.Type.Filled; fi.fillMethod = Image.FillMethod.Radial360; fi.fillOrigin = (int)Image.Origin360.Top; fi.fillAmount = 1;
        var fr = fl.GetComponent<RectTransform>(); fr.anchorMin = Vector2.zero; fr.anchorMax = Vector2.one; fr.offsetMin = new Vector2(8, 8); fr.offsetMax = new Vector2(-8, -8);
        spinFill[i] = fi;
        string[] keys = { "V", "ENTER", "B", "N9" };
        spinLbl[i] = MkText(ind.transform, playerNames[i] + "\n[" + keys[i] + "]", 20, Color.white, new Vector2(0.5f, 0.5f), Vector2.zero, FontStyle.Bold);
        spinInd[i] = ind;
    }

    void Update()
    {
        if (players != null)
        {
            for (int i = 0; i < players.Length && i < spinFill.Length; i++)
            {
                // ФИX: добавлена проверка spinLbl[i] на null — иначе исключение блокировало обработку R
                if (players[i] == null || spinFill[i] == null || spinLbl[i] == null) continue;

                float rem = players[i].GetSpinCooldownRemaining();
                spinFill[i].fillAmount = Mathf.Clamp01(1f - rem / players[i].spinCooldown);
                var c = spinFill[i].color; c.a = rem <= 0 ? 1f : 0.5f; spinFill[i].color = c;
                string[] keys = { "V", "ENT", "B", "N9" };
                spinLbl[i].text = rem > 0
                    ? playerNames[i] + "\n" + rem.ToString("F1") + "с"
                    : playerNames[i] + "\n[" + keys[i] + "]ГОТОВ!";
            }
        }

        // Рестарт по R — теперь работает корректно, т.к. PlayerController
        // не обрабатывает ввод пока раунд неактивен (см. PlayerController.Update)
        if (winPanel != null && winPanel.activeSelf && Input.GetKeyDown(KeyCode.R))
        {
            var gm = FindObjectOfType<GameManager>();
            if (gm == null) return;

            Vector3[] sp = { spawn1, spawn2, spawn3, spawn4 };
            for (int i = 0; i < gm.players.Length && i < sp.Length; i++)
                gm.players[i].ResetPlayer(sp[i]);

            gm.StartRound();
        }
    }

    public void UpdateTimer(int s) { if (timerText != null) { timerText.text = s.ToString(); timerText.color = Color.white; } }

    public void ShowElimination(int pn)
    {
        if (eliminationText != null)
        {
            eliminationText.text = "✘ " + Name(pn) + " УПАЛ В ЛАВУ! ✘";
            eliminationText.color = PColor(pn);
            StartCoroutine(HideElim());
        }
    }

    IEnumerator HideElim() { yield return new WaitForSeconds(2f); if (eliminationText != null) eliminationText.text = ""; }

    public void ShowWinScreen(int w, int[] sc, System.Collections.Generic.List<int> rank)
    {
        if (winPanel == null) return;
        winPanel.SetActive(true);
        winText.text = w > 0 ? Name(w) + "!" : "НИЧЬЯ";
        string s = "";
        string[] pl = { "🏆 Победитель", "2 место", "3 место", "4 место" };
        for (int i = 0; i < rank.Count; i++) s += pl[i] + "  —  " + Name(rank[i]) + "\n";
        scoresText.supportRichText = true; scoresText.text = s;
        if (controlsText != null) controlsText.gameObject.SetActive(false);
        foreach (var ind in spinInd) if (ind != null) ind.SetActive(false);
    }

    public void HideWinScreen()
    {
        if (winPanel != null) winPanel.SetActive(false);
        if (controlsText != null) controlsText.gameObject.SetActive(true);
        foreach (var ind in spinInd) if (ind != null) ind.SetActive(true);
    }

    string Name(int pn) { int i = pn - 1; return i >= 0 && i < playerNames.Length ? playerNames[i] : "Игрок " + pn; }
    Color PColor(int pn) { switch (pn) { case 1: return player1Color; case 2: return player2Color; case 3: return player3Color; case 4: return player4Color; default: return Color.white; } }

    Text MkText(Transform p, string txt, int sz, Color c, Vector2 a, Vector2 pos, FontStyle fs)
    {
        var o = new GameObject("T"); o.transform.SetParent(p, false);
        var r = o.AddComponent<RectTransform>(); r.anchorMin = r.anchorMax = a; r.anchoredPosition = pos; r.sizeDelta = new Vector2(1600, 300);
        var t = o.AddComponent<Text>(); t.text = txt; t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); t.fontSize = sz; t.color = c; t.alignment = TextAnchor.MiddleCenter; t.fontStyle = fs;
        t.horizontalOverflow = HorizontalWrapMode.Overflow; t.verticalOverflow = VerticalWrapMode.Overflow;
        var ol = o.AddComponent<Outline>(); ol.effectColor = Color.black; ol.effectDistance = new Vector2(2, -2);
        return t;
    }

    Sprite MakeCircle() { int s = 128; var t = new Texture2D(s, s); var c = new Vector2(s / 2, s / 2); float r = s / 2f - 2; for (int x = 0; x < s; x++) for (int y = 0; y < s; y++) t.SetPixel(x, y, Vector2.Distance(new Vector2(x, y), c) <= r ? Color.white : Color.clear); t.Apply(); return Sprite.Create(t, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f)); }
    Sprite MakeRounded() { int s = 128, cr = 24; var t = new Texture2D(s, s); for (int x = 0; x < s; x++) for (int y = 0; y < s; y++) { bool i = true; if (x < cr && y < cr) i = Vector2.Distance(new Vector2(x, y), new Vector2(cr, cr)) <= cr; else if (x > s - cr && y < cr) i = Vector2.Distance(new Vector2(x, y), new Vector2(s - cr, cr)) <= cr; else if (x < cr && y > s - cr) i = Vector2.Distance(new Vector2(x, y), new Vector2(cr, s - cr)) <= cr; else if (x > s - cr && y > s - cr) i = Vector2.Distance(new Vector2(x, y), new Vector2(s - cr, s - cr)) <= cr; t.SetPixel(x, y, i ? Color.white : Color.clear); } t.Apply(); return Sprite.Create(t, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 100, 0, SpriteMeshType.FullRect, new Vector4(cr, cr, cr, cr)); }
}