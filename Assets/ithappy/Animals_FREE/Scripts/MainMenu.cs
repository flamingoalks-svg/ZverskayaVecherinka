using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Главное меню + экран обучения.
/// Появляется при запуске игры. Показывает управление, правила, и кнопку старта.
/// 
/// КАК ИСПОЛЬЗОВАТЬ:
/// 1. Создай пустой объект → назови MainMenu
/// 2. Повесь этот скрипт
/// 3. В Inspector перетащи GameManager в поле gameManager
/// Меню появится автоматически и поставит игру на паузу до нажатия старта.
/// </summary>
public class MainMenu : MonoBehaviour
{
    [Header("Ссылки")]
    public GameManager gameManager;

    private Canvas canvas;
    private GameObject menuPanel;
    private GameObject tutorialPanel;
    private bool gameStarted = false;

    private Sprite roundedRect;

    void Start()
    {
        roundedRect = CreateRoundedRectSprite();
        CreateMenuUI();

        // Пауза до старта
        Time.timeScale = 0f;

        if (gameManager == null)
            gameManager = FindObjectOfType<GameManager>();
    }

    void CreateMenuUI()
    {
        // Canvas
        GameObject canvasObj = new GameObject("MenuCanvas");
        canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasObj.AddComponent<GraphicRaycaster>();
        if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }
        // =================================

        // ===== ГЛАВНОЕ МЕНЮ =====
        menuPanel = CreatePanel(canvasObj.transform, new Color(0.15f, 0.1f, 0.25f, 0.95f));

        // Заголовок
        CreateStyledText(menuPanel.transform, "ПОЛ — ЭТО ЛАВА!", 72,
            new Color(1f, 0.4f, 0.1f), new Vector2(0.5f, 0.85f), FontStyle.Bold);

        // Подзаголовок
        CreateStyledText(menuPanel.transform, "Зверская Вечеринка", 42,
            new Color(1f, 0.8f, 0.3f), new Vector2(0.5f, 0.75f), FontStyle.Bold);

        // Описание
        CreateStyledText(menuPanel.transform,
            "Прыгай по мебели, толкай соперников и не касайся пола!\nПоследний не упавший — победитель!",
            24, Color.white, new Vector2(0.5f, 0.63f), FontStyle.Normal);

        // Кнопка ИГРАТЬ
        CreateButton(menuPanel.transform, "ИГРАТЬ", new Color(0.2f, 0.8f, 0.3f),
            new Vector2(0.5f, 0.45f), new Vector2(350, 80), () => {
                menuPanel.SetActive(false);
                tutorialPanel.SetActive(true);
            });

        // Кнопка ВЫХОД
        CreateButton(menuPanel.transform, "ВЫХОД", new Color(0.8f, 0.2f, 0.2f),
            new Vector2(0.5f, 0.3f), new Vector2(350, 80), () => {
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
            });

        // Версия
        CreateStyledText(menuPanel.transform, "Прототип мини-игры «Царь дивана» v1.0", 16,
            new Color(1, 1, 1, 0.4f), new Vector2(0.5f, 0.08f), FontStyle.Normal);

        // ===== ЭКРАН ОБУЧЕНИЯ =====
        tutorialPanel = CreatePanel(canvasObj.transform, new Color(0.1f, 0.15f, 0.25f, 0.95f));

        CreateStyledText(tutorialPanel.transform, "КАК ИГРАТЬ", 52,
            new Color(1f, 0.8f, 0.2f), new Vector2(0.5f, 0.92f), FontStyle.Bold);

        // Правила
        string rules =
            "Правила:\n" +
            "• Прыгай по мебели — пол это лава!\n" +
            "• Толкай соперников, чтобы они упали\n" +
            "• Не задерживайся на одном месте — мебель сбросит!\n" +
            "• Собирай бонусы — светящиеся шарики над мебелью\n" +
            "• Последний не упавший — победитель!";

        CreateStyledText(tutorialPanel.transform, rules, 22,
            Color.white, new Vector2(0.5f, 0.73f), FontStyle.Normal);

        // Управление — таблица
        string controls =
            "УПРАВЛЕНИЕ:\n\n" +
            "Тигр (Игрок 1):     WASD | F толчок | R прыжок | T бег | V вихрь | Z/X камера\n" +
            "Собака (Игрок 2):  Стрелки | RShift | RCtrl | RAlt | Enter | ,/. камера\n" +
            "Кот (Игрок 3):       IJKL | H толчок | Y прыжок | G бег | B вихрь | N/M камера\n" +
            "Пингвин (Игрок 4): Num8456 | Num1 | Num3 | Num7 | Num9 | Num//Num*\n\n" +
            "Двойной прыжок: нажми прыжок дважды\n" +
            "Комбо-рывок: прыжок → толчок в воздухе (летишь вперёд!)\n" +
            "Супер-рывок: двойной прыжок → толчок (максимальная дальность!)";

        CreateStyledText(tutorialPanel.transform, controls, 18,
            new Color(0.8f, 0.9f, 1f), new Vector2(0.5f, 0.4f), FontStyle.Normal);

        // Бонусы
        string bonuses =
            "БОНУСЫ (шарики над мебелью):\n" +
            "Голубой = Полёт (прыжок↑ бег↓ толчок=отмена) | Ледяной = Заморозка врага\n" +
            "Фиолетовый = Невидимость | Розовый = Телепорт | Золотой = Гигант (толчок x3)";

        CreateStyledText(tutorialPanel.transform, bonuses, 18,
            new Color(1f, 0.9f, 0.7f), new Vector2(0.5f, 0.17f), FontStyle.Normal);

        // Кнопка СТАРТ
        CreateButton(tutorialPanel.transform, "НАЧАТЬ ИГРУ!", new Color(0.2f, 0.8f, 0.3f),
            new Vector2(0.5f, 0.05f), new Vector2(400, 70), () => {
                StartGame();
            });

        tutorialPanel.SetActive(false);
    }

    void StartGame()
    {
        Time.timeScale = 1f;
        gameStarted = true;
        canvas.gameObject.SetActive(false);

        if (gameManager != null)
            gameManager.StartRound();
    }

    // === UI хелперы ===

    GameObject CreatePanel(Transform parent, Color color)
    {
        GameObject panel = new GameObject("Panel");
        panel.transform.SetParent(parent, false);
        Image img = panel.AddComponent<Image>();
        img.color = color;
        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        return panel;
    }

    Text CreateStyledText(Transform parent, string content, int fontSize, Color color, Vector2 anchor, FontStyle style)
    {
        GameObject obj = new GameObject("Text");
        obj.transform.SetParent(parent, false);
        RectTransform rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(1600, 300);

        Text text = obj.AddComponent<Text>();
        text.text = content;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = fontSize;
        text.color = color;
        text.alignment = TextAnchor.MiddleCenter;
        text.fontStyle = style;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;

        Outline outline = obj.AddComponent<Outline>();
        outline.effectColor = Color.black;
        outline.effectDistance = new Vector2(2, -2);

        return text;
    }

    void CreateButton(Transform parent, string label, Color bgColor, Vector2 anchor, Vector2 size, UnityEngine.Events.UnityAction onClick)
    {
        GameObject btnObj = new GameObject("Button");
        btnObj.transform.SetParent(parent, false);

        RectTransform rect = btnObj.AddComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = size;

        Image img = btnObj.AddComponent<Image>();
        img.sprite = roundedRect;
        img.color = bgColor;
        img.type = Image.Type.Sliced;

        Button btn = btnObj.AddComponent<Button>();
        btn.targetGraphic = img;

        // Hover эффект
        ColorBlock colors = btn.colors;
        colors.highlightedColor = bgColor * 1.2f;
        colors.pressedColor = bgColor * 0.8f;
        btn.colors = colors;

        btn.onClick.AddListener(onClick);

        // Текст кнопки
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(btnObj.transform, false);
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        Text text = textObj.AddComponent<Text>();
        text.text = label;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 32;
        text.color = Color.white;
        text.alignment = TextAnchor.MiddleCenter;
        text.fontStyle = FontStyle.Bold;

        Outline outline = textObj.AddComponent<Outline>();
        outline.effectColor = Color.black;
        outline.effectDistance = new Vector2(1, -1);
    }

    Sprite CreateRoundedRectSprite()
    {
        int size = 128; int cr = 24;
        Texture2D tex = new Texture2D(size, size);
        for (int x = 0; x < size; x++)
            for (int y = 0; y < size; y++)
            {
                bool inside = true;
                if (x < cr && y < cr) inside = Vector2.Distance(new Vector2(x, y), new Vector2(cr, cr)) <= cr;
                else if (x > size - cr && y < cr) inside = Vector2.Distance(new Vector2(x, y), new Vector2(size - cr, cr)) <= cr;
                else if (x < cr && y > size - cr) inside = Vector2.Distance(new Vector2(x, y), new Vector2(cr, size - cr)) <= cr;
                else if (x > size - cr && y > size - cr) inside = Vector2.Distance(new Vector2(x, y), new Vector2(size - cr, size - cr)) <= cr;
                tex.SetPixel(x, y, inside ? Color.white : Color.clear);
            }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f),
            100f, 0, SpriteMeshType.FullRect, new Vector4(cr, cr, cr, cr));
    }
}