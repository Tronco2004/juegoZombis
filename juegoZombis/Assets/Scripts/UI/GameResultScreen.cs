using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;

/// <summary>
/// Pantalla de Victoria y Derrota.
/// Se auto-genera, no necesita ningún prefab.
///
/// DERROTA:  se activa cuando el jugador muere (PlayerHealth.isDead == true)
/// VICTORIA: se activa cuando se completan todas las oleadas (configurable)
///           o puedes llamar a GameResultScreen.Instance.ShowVictory() manualmente
///           desde cualquier parte del código.
/// </summary>
public class GameResultScreen : MonoBehaviour
{
    public static GameResultScreen Instance { get; private set; }

    [Header("=== ESCENAS ===")]
    [Tooltip("Nombre exacto de la escena del menú principal")]
    public string mainMenuSceneName = "SampleScene";

    [Header("=== VICTORIA ===")]
    [Tooltip("Número máximo de oleadas. Al llegar aquí se muestra la victoria (0 = desactivado)")]
    public int maxWavesForVictory = 10;

    [Header("=== TIEMPOS ===")]
    [Tooltip("Tiempo de espera antes de mostrar la pantalla de derrota")]
    public float defeatDelay = 2f;
    [Tooltip("Duración del fade in de la pantalla de resultado")]
    public float fadeDuration = 0.8f;

    [Header("=== COLORES ===")]
    public Color defeatBgColor    = new Color(0.45f, 0.05f, 0.05f, 0.97f);
    public Color victoryBgColor   = new Color(0.05f, 0.35f, 0.05f, 0.97f);
    public Color titleColor       = Color.white;
    public Color subtitleColor    = new Color(0.9f, 0.9f, 0.9f, 1f);
    public Color buttonColor      = new Color(0.1f, 0.1f, 0.1f, 1f);
    public Color buttonHoverColor = new Color(0.25f, 0.25f, 0.25f, 1f);
    public Color buttonTextColor  = Color.white;

    // ── Estado interno ─────────────────────────────────────────
    private bool resultShown = false;
    private Canvas canvas;
    private GameObject defeatPanel;
    private GameObject victoryPanel;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    void Start()
    {
        BuildUI();
        
        // Suscribirse al evento de detonación de la bomba nuclear
        NuclearBomb.OnNuclearDetonated += ShowVictory;
    }
    
    void OnDestroy()
    {
        // Desuscribirse del evento
        if (NuclearBomb.OnNuclearDetonated != null)
            NuclearBomb.OnNuclearDetonated -= ShowVictory;
    }

    void Update()
    {
        if (resultShown) return;

        // ── Comprobar derrota ──────────────────────────────────
        if (PlayerHealth.Instance != null && PlayerHealth.Instance.isDead)
        {
            StartCoroutine(ShowDefeatDelayed());
        }
    }

    // ══════════════════════════════════════════════════════════
    //  MÉTODOS PÚBLICOS
    // ══════════════════════════════════════════════════════════

    /// <summary>Muestra la pantalla de victoria. Llama esto cuando el jugador gana.</summary>
    public void ShowVictory()
    {
        if (resultShown) return;
        resultShown = true;
        PauseGame();
        StartCoroutine(FadeInPanel(victoryPanel));
        Debug.Log("[GameResult] ¡VICTORIA!");
    }

    /// <summary>Muestra la pantalla de derrota. Llama esto cuando el jugador muere.</summary>
    public void ShowDefeat()
    {
        if (resultShown) return;
        resultShown = true;
        PauseGame();
        StartCoroutine(FadeInPanel(defeatPanel));
        Debug.Log("[GameResult] DERROTA.");
    }

    // ══════════════════════════════════════════════════════════
    //  LÓGICA INTERNA
    // ══════════════════════════════════════════════════════════

    IEnumerator ShowDefeatDelayed()
    {
        resultShown = true; // marca ya para evitar doble ejecución
        yield return new WaitForSecondsRealtime(defeatDelay);
        PauseGame();
        StartCoroutine(FadeInPanel(defeatPanel));
        Debug.Log("[GameResult] DERROTA.");
    }

    void PauseGame()
    {
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;
    }

    IEnumerator FadeInPanel(GameObject panel)
    {
        panel.SetActive(true);
        CanvasGroup cg = panel.GetComponent<CanvasGroup>();
        if (cg == null) cg = panel.AddComponent<CanvasGroup>();

        cg.alpha = 0f;
        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.unscaledDeltaTime;
            cg.alpha = Mathf.Clamp01(t / fadeDuration);
            yield return null;
        }
        cg.alpha = 1f;
    }

    void Restart()
    {
        Restart_Internal();
    }

    public void Restart_Internal()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    void GoToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneName);
    }

    // ══════════════════════════════════════════════════════════
    //  CONSTRUCCIÓN DE UI
    // ══════════════════════════════════════════════════════════

    void BuildUI()
    {
        // Canvas
        GameObject canvasObj = new GameObject("GameResultCanvas");
        canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode  = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200; // Por encima del menú de pausa

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode        = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasObj.AddComponent<GraphicRaycaster>();

        // Paneles
        defeatPanel  = BuildPanel(canvasObj.transform, defeatBgColor,
            "DERROTA",       "El mundo fue consumido por los muertos...",
            "REINTENTAR", "MENÚ PRINCIPAL");

        victoryPanel = BuildPanel(canvasObj.transform, victoryBgColor,
            "¡VICTORIA!",    "Has sobrevivido y salvado el mundo.",
            "JUGAR DE NUEVO", "MENÚ PRINCIPAL");

        defeatPanel.SetActive(false);
        victoryPanel.SetActive(false);
    }

    GameObject BuildPanel(Transform parent, Color bgColor,
                          string title, string subtitle,
                          string btn1Text, string btn2Text)
    {
        // Fondo semitransparente que cubre toda la pantalla
        GameObject overlay = CreateImage(parent, "Overlay",
            new Color(0, 0, 0, 0.5f),
            Vector2.zero, new Vector2(1920, 1080));
        SetFullStretch(overlay.GetComponent<RectTransform>());

        // Panel central
        GameObject panel = CreateImage(overlay.transform, "Panel",
            bgColor, Vector2.zero, new Vector2(700, 420));
        RectTransform pr = panel.GetComponent<RectTransform>();
        pr.anchorMin = pr.anchorMax = pr.pivot = new Vector2(0.5f, 0.5f);
        pr.anchoredPosition = Vector2.zero;

        // Borde superior decorativo
        GameObject topBar = CreateImage(panel.transform, "TopBar",
            new Color(1f, 1f, 1f, 0.15f), Vector2.zero, new Vector2(700, 6));
        RectTransform tbr = topBar.GetComponent<RectTransform>();
        tbr.anchorMin = new Vector2(0f, 1f);
        tbr.anchorMax = new Vector2(1f, 1f);
        tbr.pivot     = new Vector2(0.5f, 1f);
        tbr.offsetMin = tbr.offsetMax = Vector2.zero;

        // Título
        GameObject titleGO = CreateTMPro(panel.transform, "Title", title,
            titleColor, 72, FontStyles.Bold);
        RectTransform tr = titleGO.GetComponent<RectTransform>();
        tr.anchorMin = tr.anchorMax = tr.pivot = new Vector2(0.5f, 1f);
        tr.sizeDelta = new Vector2(650, 90);
        tr.anchoredPosition = new Vector2(0, -50);

        // Subtítulo
        GameObject subGO = CreateTMPro(panel.transform, "Subtitle", subtitle,
            subtitleColor, 28, FontStyles.Normal);
        RectTransform sr = subGO.GetComponent<RectTransform>();
        sr.anchorMin = sr.anchorMax = sr.pivot = new Vector2(0.5f, 1f);
        sr.sizeDelta = new Vector2(620, 60);
        sr.anchoredPosition = new Vector2(0, -155);

        // Estadísticas (puntos obtenidos)
        string statsStr = PlayerPoints.Instance != null
            ? $"Puntos conseguidos: {PlayerPoints.Instance.CurrentPoints}"
            : "";
        GameObject statsGO = CreateTMPro(panel.transform, "Stats", statsStr,
            new Color(1f, 0.85f, 0.2f, 1f), 32, FontStyles.Bold);
        RectTransform str2 = statsGO.GetComponent<RectTransform>();
        str2.anchorMin = str2.anchorMax = str2.pivot = new Vector2(0.5f, 1f);
        str2.sizeDelta = new Vector2(620, 50);
        str2.anchoredPosition = new Vector2(0, -225);

        // Botón 1 — Reintentar / Jugar de nuevo
        GameObject b1 = CreateButton(panel.transform, "Btn1", btn1Text,
            new Vector2(0, -300), new Vector2(280, 60));
        b1.GetComponent<Button>().onClick.AddListener(Restart);

        // Botón 2 — Menú principal
        GameObject b2 = CreateButton(panel.transform, "Btn2", btn2Text,
            new Vector2(0, -375), new Vector2(280, 60));
        b2.GetComponent<Button>().onClick.AddListener(GoToMainMenu);

        // Hint de tecla
        GameObject hintGO = CreateTMPro(panel.transform, "Hint",
            "Presiona R para reintentar",
            new Color(1f, 1f, 1f, 0.4f), 20, FontStyles.Italic);
        RectTransform hr = hintGO.GetComponent<RectTransform>();
        hr.anchorMin = hr.anchorMax = hr.pivot = new Vector2(0.5f, 0f);
        hr.sizeDelta = new Vector2(620, 30);
        hr.anchoredPosition = new Vector2(0, 20);

        // Listener de tecla R en el overlay
        overlay.AddComponent<RestartOnKeyBehaviour>().screen = this;

        return overlay;
    }

    // ── Helpers de creación ─────────────────────────────────────
    GameObject CreateImage(Transform parent, string name, Color color,
                           Vector2 pos, Vector2 size)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        Image img = go.AddComponent<Image>();
        img.color = color;
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = size;
        rt.anchoredPosition = pos;
        return go;
    }

    GameObject CreateTMPro(Transform parent, string name, string text,
                           Color color, int fontSize, FontStyles style)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text      = text;
        tmp.color     = color;
        tmp.fontSize  = fontSize;
        tmp.fontStyle = style;
        tmp.alignment = TextAlignmentOptions.Center;
        return go;
    }

    GameObject CreateButton(Transform parent, string name, string label,
                            Vector2 pos, Vector2 size)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        Image img    = go.AddComponent<Image>();
        img.color    = buttonColor;
        Button btn   = go.AddComponent<Button>();

        ColorBlock cb = btn.colors;
        cb.normalColor      = buttonColor;
        cb.highlightedColor = buttonHoverColor;
        cb.pressedColor     = new Color(0.4f, 0.4f, 0.4f, 1f);
        btn.colors          = cb;

        RectTransform rt  = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 1f);
        rt.sizeDelta      = size;
        rt.anchoredPosition = pos;

        // Texto del botón
        GameObject textGO = new GameObject("Label");
        textGO.transform.SetParent(go.transform, false);
        TextMeshProUGUI tmp = textGO.AddComponent<TextMeshProUGUI>();
        tmp.text      = label;
        tmp.color     = buttonTextColor;
        tmp.fontSize  = 26;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        RectTransform tr = textGO.GetComponent<RectTransform>();
        tr.anchorMin = Vector2.zero;
        tr.anchorMax = Vector2.one;
        tr.offsetMin = tr.offsetMax = Vector2.zero;

        return go;
    }

    void SetFullStretch(RectTransform rt)
    {
        rt.anchorMin        = Vector2.zero;
        rt.anchorMax        = Vector2.one;
        rt.offsetMin        = Vector2.zero;
        rt.offsetMax        = Vector2.zero;
        rt.pivot            = new Vector2(0.5f, 0.5f);
    }
}

// ── Componente auxiliar para capturar la tecla R ────────────────
public class RestartOnKeyBehaviour : MonoBehaviour
{
    public GameResultScreen screen;
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R) && gameObject.activeSelf)
        {
            screen?.Restart_Internal();
        }
    }
}
