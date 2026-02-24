using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;

/// <summary>
/// Pantalla de Victoria y Derrota — diseño profesional.
/// Se auto-genera, no necesita ningún prefab.
///
/// DERROTA:  se activa cuando el jugador muere (PlayerHealth.isDead == true)
/// VICTORIA: se activa cuando se completan todas las oleadas
///           o puedes llamar a GameResultScreen.Instance.ShowVictory()
/// </summary>
public class GameResultScreen : MonoBehaviour
{
    public static GameResultScreen Instance { get; private set; }

    /// <summary>True cuando se está mostrando una pantalla de resultado (derrota/victoria).
    /// Otros scripts (armas, movimiento…) deben comprobar esto para bloquear input.</summary>
    public static bool IsGameOver { get; private set; } = false;

    [Header("=== ESCENAS ===")]
    [Tooltip("Nombre exacto de la escena del menú principal")]
    public string mainMenuSceneName = "SampleScene";

    [Header("=== VICTORIA ===")]
    [Tooltip("Número máximo de oleadas. Al llegar aquí se muestra la victoria (0 = desactivado)")]
    public int maxWavesForVictory = 10;

    [Header("=== TIEMPOS ===")]
    [Tooltip("Tiempo de espera antes de mostrar la pantalla de derrota")]
    public float defeatDelay = 2f;
    [Tooltip("Duración del fade in")]
    public float fadeDuration = 1.0f;

    [Header("=== AUDIO ===")]
    [Tooltip("Canción/sonido que suena al perder. Asigna desde el Inspector.")]
    public AudioClip defeatMusic;
    [Tooltip("Volumen de la canción de derrota (0-1)")]
    [Range(0f, 1f)]
    public float defeatMusicVolume = 0.7f;

    private AudioSource audioSource;

    // ── Estado interno ─────────────────────────────────────────
    private bool resultShown = false;
    private Canvas canvas;
    private GameObject defeatPanel;
    private GameObject victoryPanel;
    private float gameStartTime;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
        IsGameOver = false;
    }

    void Start()
    {
        gameStartTime = Time.time;
        // AudioSource para la música de derrota
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.loop = false;
        BuildUI();
        NuclearBomb.OnNuclearDetonated += ShowVictory;
    }

    void OnDestroy()
    {
        if (NuclearBomb.OnNuclearDetonated != null)
            NuclearBomb.OnNuclearDetonated -= ShowVictory;
    }

    void Update()
    {
        if (resultShown) return;
        if (PlayerHealth.Instance != null && PlayerHealth.Instance.isDead)
        {
            StartCoroutine(ShowDefeatDelayed());
        }
    }

    // ══════════════════════════════════════════════════════════
    //  MÉTODOS PÚBLICOS
    // ══════════════════════════════════════════════════════════

    public void ShowVictory()
    {
        if (resultShown) return;
        resultShown = true;
        IsGameOver = true;
        StopGameMusic();
        PauseGame();
        UpdateStats(victoryPanel);
        StartCoroutine(FadeInPanel(victoryPanel));
        Debug.Log("[GameResult] ¡VICTORIA!");
    }

    public void ShowDefeat()
    {
        if (resultShown) return;
        resultShown = true;
        IsGameOver = true;
        StopGameMusic();
        PauseGame();
        PlayDefeatMusic();
        UpdateStats(defeatPanel);
        StartCoroutine(FadeInPanel(defeatPanel));
        Debug.Log("[GameResult] DERROTA.");
    }

    // ══════════════════════════════════════════════════════════
    //  LÓGICA INTERNA
    // ══════════════════════════════════════════════════════════

    IEnumerator ShowDefeatDelayed()
    {
        resultShown = true;
        IsGameOver = true;
        StopGameMusic();
        yield return new WaitForSecondsRealtime(defeatDelay);
        PauseGame();
        PlayDefeatMusic();
        UpdateStats(defeatPanel);
        StartCoroutine(FadeInPanel(defeatPanel));
        Debug.Log("[GameResult] DERROTA.");
    }

    void PauseGame()
    {
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void PlayDefeatMusic()
    {
        if (defeatMusic != null && audioSource != null)
        {
            audioSource.clip = defeatMusic;
            audioSource.volume = defeatMusicVolume;
            audioSource.ignoreListenerPause = true;  // Suena incluso con timeScale=0
            audioSource.Play();
        }
    }

    void StopGameMusic()
    {
        // Para la música de fondo para que no se solape con la de derrota/victoria
        if (GameMusicManager.Instance != null)
        {
            GameMusicManager.Instance.StopMusic();
        }
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
            cg.alpha = Mathf.SmoothStep(0f, 1f, t / fadeDuration);
            yield return null;
        }
        cg.alpha = 1f;
    }

    void UpdateStats(GameObject panel)
    {
        // Buscar los textos de stats dentro del panel y actualizarlos
        TextMeshProUGUI[] texts = panel.GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach (var tmp in texts)
        {
            if (tmp.gameObject.name == "StatsPoints")
            {
                int pts = PlayerPoints.Instance != null ? PlayerPoints.Instance.CurrentPoints : 0;
                tmp.text = pts.ToString("N0");
            }
            else if (tmp.gameObject.name == "StatsWave")
            {
                int wave = ZombieSpawner.Instance != null ? ZombieSpawner.Instance.CurrentWave : 0;
                tmp.text = wave.ToString();
            }
            else if (tmp.gameObject.name == "StatsTime")
            {
                float elapsed = Time.time - gameStartTime;
                int mins = Mathf.FloorToInt(elapsed / 60f);
                int secs = Mathf.FloorToInt(elapsed % 60f);
                tmp.text = $"{mins:00}:{secs:00}";
            }
        }
    }

    void GoToMainMenu()
    {
        IsGameOver = false;
        if (audioSource != null) audioSource.Stop();
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
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasObj.AddComponent<GraphicRaycaster>();

        // Paneles
        defeatPanel = BuildDefeatPanel(canvasObj.transform);
        victoryPanel = BuildVictoryPanel(canvasObj.transform);

        defeatPanel.SetActive(false);
        victoryPanel.SetActive(false);
    }

    // ────────────────────────────────────────────────────
    //  PANTALLA DE DERROTA
    // ────────────────────────────────────────────────────
    GameObject BuildDefeatPanel(Transform parent)
    {
        // Overlay oscuro
        GameObject overlay = CreatePanel(parent, "DefeatOverlay");
        Image overlayImg = overlay.AddComponent<Image>();
        overlayImg.color = new Color(0f, 0f, 0f, 0.85f);
        SetFullStretch(overlay.GetComponent<RectTransform>());

        // Contenedor central
        GameObject container = CreatePanel(overlay.transform, "Container");
        RectTransform crt = container.GetComponent<RectTransform>();
        crt.anchorMin = crt.anchorMax = crt.pivot = new Vector2(0.5f, 0.5f);
        crt.sizeDelta = new Vector2(680, 520);

        // ── Línea superior decorativa roja ──
        GameObject topLine = CreatePanel(container.transform, "TopLine");
        Image topLineImg = topLine.AddComponent<Image>();
        topLineImg.color = new Color(0.8f, 0.15f, 0.15f, 1f);
        RectTransform tlr = topLine.GetComponent<RectTransform>();
        tlr.anchorMin = new Vector2(0.1f, 1f);
        tlr.anchorMax = new Vector2(0.9f, 1f);
        tlr.pivot = new Vector2(0.5f, 1f);
        tlr.sizeDelta = new Vector2(0, 4);
        tlr.anchoredPosition = Vector2.zero;

        // ── Icono calavera (emoji texto) ──
        CreateTMP(container.transform, "Skull", "☠",
            new Color(0.85f, 0.2f, 0.2f, 1f), 64, FontStyles.Normal,
            new Vector2(0.5f, 1f), new Vector2(100, 80), new Vector2(0, -15));

        // ── Título DERROTA ──
        CreateTMP(container.transform, "Title", "DERROTA",
            new Color(0.9f, 0.2f, 0.2f, 1f), 58, FontStyles.Bold,
            new Vector2(0.5f, 1f), new Vector2(600, 70), new Vector2(0, -85));

        // ── Subtítulo ──
        CreateTMP(container.transform, "Subtitle", "Los muertos han reclamado el mundo...",
            new Color(0.7f, 0.7f, 0.7f, 1f), 22, FontStyles.Italic,
            new Vector2(0.5f, 1f), new Vector2(600, 35), new Vector2(0, -155));

        // ── Línea separadora ──
        GameObject sepLine = CreatePanel(container.transform, "SepLine");
        Image sepImg = sepLine.AddComponent<Image>();
        sepImg.color = new Color(1f, 1f, 1f, 0.15f);
        RectTransform slr = sepLine.GetComponent<RectTransform>();
        slr.anchorMin = slr.anchorMax = slr.pivot = new Vector2(0.5f, 1f);
        slr.sizeDelta = new Vector2(500, 1);
        slr.anchoredPosition = new Vector2(0, -195);

        // ── Estadísticas ──
        float statsY = -215f;
        float rowH = 40f;

        // Fila: Puntos
        CreateStatRow(container.transform, "⭐  PUNTOS", "StatsPoints", "0",
            new Color(1f, 0.85f, 0.2f, 1f), statsY);
        statsY -= rowH;

        // Fila: Oleada
        CreateStatRow(container.transform, "🌊  OLEADA ALCANZADA", "StatsWave", "0",
            new Color(0.4f, 0.8f, 1f, 1f), statsY);
        statsY -= rowH;

        // Fila: Tiempo
        CreateStatRow(container.transform, "⏱  TIEMPO SOBREVIVIDO", "StatsTime", "00:00",
            new Color(0.6f, 0.9f, 0.6f, 1f), statsY);

        // ── Botones ──
        CreateStyledButton(container.transform, "BtnMenu", "MENÚ PRINCIPAL",
            new Color(0.25f, 0.25f, 0.3f, 1f), new Color(0.35f, 0.35f, 0.4f, 1f),
            new Vector2(0.5f, 0f), new Vector2(320, 55), new Vector2(0, 80),
            GoToMainMenu);

        // ── Línea inferior decorativa ──
        GameObject botLine = CreatePanel(container.transform, "BotLine");
        Image botLineImg = botLine.AddComponent<Image>();
        botLineImg.color = new Color(0.8f, 0.15f, 0.15f, 1f);
        RectTransform blr = botLine.GetComponent<RectTransform>();
        blr.anchorMin = new Vector2(0.1f, 0f);
        blr.anchorMax = new Vector2(0.9f, 0f);
        blr.pivot = new Vector2(0.5f, 0f);
        blr.sizeDelta = new Vector2(0, 4);
        blr.anchoredPosition = Vector2.zero;

        return overlay;
    }

    // ────────────────────────────────────────────────────
    //  PANTALLA DE VICTORIA
    // ────────────────────────────────────────────────────
    GameObject BuildVictoryPanel(Transform parent)
    {
        // Overlay oscuro
        GameObject overlay = CreatePanel(parent, "VictoryOverlay");
        Image overlayImg = overlay.AddComponent<Image>();
        overlayImg.color = new Color(0f, 0f, 0f, 0.85f);
        SetFullStretch(overlay.GetComponent<RectTransform>());

        // Contenedor central
        GameObject container = CreatePanel(overlay.transform, "Container");
        RectTransform crt = container.GetComponent<RectTransform>();
        crt.anchorMin = crt.anchorMax = crt.pivot = new Vector2(0.5f, 0.5f);
        crt.sizeDelta = new Vector2(680, 520);

        // ── Línea superior dorada ──
        GameObject topLine = CreatePanel(container.transform, "TopLine");
        Image topLineImg = topLine.AddComponent<Image>();
        topLineImg.color = new Color(1f, 0.85f, 0.2f, 1f);
        RectTransform tlr = topLine.GetComponent<RectTransform>();
        tlr.anchorMin = new Vector2(0.1f, 1f);
        tlr.anchorMax = new Vector2(0.9f, 1f);
        tlr.pivot = new Vector2(0.5f, 1f);
        tlr.sizeDelta = new Vector2(0, 4);
        tlr.anchoredPosition = Vector2.zero;

        // ── Icono trofeo ──
        CreateTMP(container.transform, "Trophy", "🏆",
            new Color(1f, 0.85f, 0.2f, 1f), 64, FontStyles.Normal,
            new Vector2(0.5f, 1f), new Vector2(100, 80), new Vector2(0, -15));

        // ── Título ──
        CreateTMP(container.transform, "Title", "¡VICTORIA!",
            new Color(1f, 0.85f, 0.2f, 1f), 58, FontStyles.Bold,
            new Vector2(0.5f, 1f), new Vector2(600, 70), new Vector2(0, -85));

        // ── Subtítulo ──
        CreateTMP(container.transform, "Subtitle", "Has sobrevivido y salvado el mundo",
            new Color(0.75f, 0.9f, 0.75f, 1f), 22, FontStyles.Italic,
            new Vector2(0.5f, 1f), new Vector2(600, 35), new Vector2(0, -155));

        // ── Separador ──
        GameObject sepLine = CreatePanel(container.transform, "SepLine");
        Image sepImg = sepLine.AddComponent<Image>();
        sepImg.color = new Color(1f, 1f, 1f, 0.15f);
        RectTransform slr = sepLine.GetComponent<RectTransform>();
        slr.anchorMin = slr.anchorMax = slr.pivot = new Vector2(0.5f, 1f);
        slr.sizeDelta = new Vector2(500, 1);
        slr.anchoredPosition = new Vector2(0, -195);

        // ── Estadísticas ──
        float statsY = -215f;
        float rowH = 40f;

        CreateStatRow(container.transform, "⭐  PUNTOS", "StatsPoints", "0",
            new Color(1f, 0.85f, 0.2f, 1f), statsY);
        statsY -= rowH;

        CreateStatRow(container.transform, "🌊  OLEADAS COMPLETADAS", "StatsWave", "0",
            new Color(0.4f, 0.8f, 1f, 1f), statsY);
        statsY -= rowH;

        CreateStatRow(container.transform, "⏱  TIEMPO TOTAL", "StatsTime", "00:00",
            new Color(0.6f, 0.9f, 0.6f, 1f), statsY);

        // ── Botones ──
        CreateStyledButton(container.transform, "BtnMenu", "MENÚ PRINCIPAL",
            new Color(0.25f, 0.25f, 0.3f, 1f), new Color(0.35f, 0.35f, 0.4f, 1f),
            new Vector2(0.5f, 0f), new Vector2(320, 55), new Vector2(0, 80),
            GoToMainMenu);

        // ── Línea inferior dorada ──
        GameObject botLine = CreatePanel(container.transform, "BotLine");
        Image botLineImg = botLine.AddComponent<Image>();
        botLineImg.color = new Color(1f, 0.85f, 0.2f, 1f);
        RectTransform blr = botLine.GetComponent<RectTransform>();
        blr.anchorMin = new Vector2(0.1f, 0f);
        blr.anchorMax = new Vector2(0.9f, 0f);
        blr.pivot = new Vector2(0.5f, 0f);
        blr.sizeDelta = new Vector2(0, 4);
        blr.anchoredPosition = Vector2.zero;

        return overlay;
    }

    // ══════════════════════════════════════════════════════════
    //  HELPERS
    // ══════════════════════════════════════════════════════════

    GameObject CreatePanel(Transform parent, string name)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        return go;
    }

    TextMeshProUGUI CreateTMP(Transform parent, string name, string text,
        Color color, float fontSize, FontStyles style,
        Vector2 anchor, Vector2 size, Vector2 pos)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.color = color;
        tmp.fontSize = fontSize;
        tmp.fontStyle = style;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.enableWordWrapping = true;
        tmp.overflowMode = TextOverflowModes.Ellipsis;

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = anchor;
        rt.sizeDelta = size;
        rt.anchoredPosition = pos;

        return tmp;
    }

    void CreateStatRow(Transform parent, string label, string valueName, string defaultValue,
        Color valueColor, float yPos)
    {
        // Label (izquierda)
        GameObject labelGO = new GameObject("Label_" + valueName);
        labelGO.transform.SetParent(parent, false);
        TextMeshProUGUI labelTmp = labelGO.AddComponent<TextMeshProUGUI>();
        labelTmp.text = label;
        labelTmp.color = new Color(0.8f, 0.8f, 0.8f, 0.9f);
        labelTmp.fontSize = 22;
        labelTmp.fontStyle = FontStyles.Normal;
        labelTmp.alignment = TextAlignmentOptions.Left;

        RectTransform lr = labelGO.GetComponent<RectTransform>();
        lr.anchorMin = lr.anchorMax = lr.pivot = new Vector2(0.5f, 1f);
        lr.sizeDelta = new Vector2(460, 35);
        lr.anchoredPosition = new Vector2(-40f, yPos);

        // Value (derecha)
        GameObject valueGO = new GameObject(valueName);
        valueGO.transform.SetParent(parent, false);
        TextMeshProUGUI valueTmp = valueGO.AddComponent<TextMeshProUGUI>();
        valueTmp.text = defaultValue;
        valueTmp.color = valueColor;
        valueTmp.fontSize = 26;
        valueTmp.fontStyle = FontStyles.Bold;
        valueTmp.alignment = TextAlignmentOptions.Right;

        RectTransform vr = valueGO.GetComponent<RectTransform>();
        vr.anchorMin = vr.anchorMax = vr.pivot = new Vector2(0.5f, 1f);
        vr.sizeDelta = new Vector2(460, 35);
        vr.anchoredPosition = new Vector2(40f, yPos);
    }

    void CreateStyledButton(Transform parent, string name, string label,
        Color normalColor, Color hoverColor,
        Vector2 anchor, Vector2 size, Vector2 pos,
        UnityEngine.Events.UnityAction onClick)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);

        Image img = go.AddComponent<Image>();
        img.color = normalColor;

        Button btn = go.AddComponent<Button>();
        ColorBlock cb = btn.colors;
        cb.normalColor = normalColor;
        cb.highlightedColor = hoverColor;
        cb.pressedColor = new Color(hoverColor.r + 0.1f, hoverColor.g + 0.1f, hoverColor.b + 0.1f, 1f);
        cb.selectedColor = normalColor;
        cb.fadeDuration = 0.1f;
        btn.colors = cb;

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = anchor;
        rt.sizeDelta = size;
        rt.anchoredPosition = pos;

        btn.onClick.AddListener(onClick);

        // Texto del botón
        GameObject textGO = new GameObject("Label");
        textGO.transform.SetParent(go.transform, false);
        TextMeshProUGUI tmp = textGO.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.color = Color.white;
        tmp.fontSize = 24;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        RectTransform tr = textGO.GetComponent<RectTransform>();
        tr.anchorMin = Vector2.zero;
        tr.anchorMax = Vector2.one;
        tr.offsetMin = tr.offsetMax = Vector2.zero;
    }

    void SetFullStretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.pivot = new Vector2(0.5f, 0.5f);
    }
}
