using UnityEngine;

/// <summary>
/// Singleton para mostrar diálogos/textos en pantalla.
/// Colócalo en un GameObject vacío en la escena (o se crea solo).
/// Otros scripts llaman DialogueManager.Instance.ShowDialogue("texto", duracion);
/// </summary>
public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }

    [Header("=== ESTILO ===")]
    [Tooltip("Tamaño de la fuente del diálogo")]
    public int fontSize = 28;
    [Tooltip("Color del texto")]
    public Color textColor = Color.white;
    [Tooltip("Color de la sombra")]
    public Color shadowColor = new Color(0, 0, 0, 0.9f);
    [Tooltip("Color del fondo semi-transparente")]
    public Color backgroundColor = new Color(0, 0, 0, 0.6f);

    [Header("=== CONFIGURACIÓN ===")]
    [Tooltip("Duración por defecto de los diálogos (segundos)")]
    public float defaultDuration = 4f;
    [Tooltip("Tiempo de fade in/out (segundos)")]
    public float fadeDuration = 0.5f;

    // Estado interno
    private bool isShowing = false;
    private string currentText = "";
    private float displayTimer = 0f;
    private float totalDuration = 0f;
    private float alpha = 0f;

    // Estilos
    private GUIStyle dialogueStyle;
    private GUIStyle shadowStyle;
    private Texture2D bgTexture;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Crear textura para el fondo
        bgTexture = new Texture2D(1, 1);
        bgTexture.SetPixel(0, 0, Color.white);
        bgTexture.Apply();
    }

    void Update()
    {
        if (!isShowing) return;

        displayTimer -= Time.deltaTime;

        // Calcular alpha con fade in/out
        float timeElapsed = totalDuration - displayTimer;
        if (timeElapsed < fadeDuration)
        {
            // Fade in
            alpha = Mathf.Clamp01(timeElapsed / fadeDuration);
        }
        else if (displayTimer < fadeDuration)
        {
            // Fade out
            alpha = Mathf.Clamp01(displayTimer / fadeDuration);
        }
        else
        {
            alpha = 1f;
        }

        if (displayTimer <= 0f)
        {
            isShowing = false;
            currentText = "";
            alpha = 0f;
        }
    }

    void OnGUI()
    {
        if (!isShowing || alpha <= 0f) return;

        // Inicializar estilos si es necesario
        if (dialogueStyle == null)
        {
            dialogueStyle = new GUIStyle();
            dialogueStyle.fontSize = fontSize;
            dialogueStyle.fontStyle = FontStyle.Bold;
            dialogueStyle.alignment = TextAnchor.MiddleCenter;
            dialogueStyle.wordWrap = true;
            dialogueStyle.normal.textColor = textColor;

            shadowStyle = new GUIStyle(dialogueStyle);
            shadowStyle.normal.textColor = shadowColor;
        }

        // Tamaño y posición del cuadro de diálogo (parte inferior de la pantalla)
        float boxWidth = Screen.width * 0.7f;
        float boxHeight = 80f;
        float boxX = (Screen.width - boxWidth) / 2f;
        float boxY = Screen.height - boxHeight - 80f; // 80px desde abajo
        float padding = 15f;

        Rect bgRect = new Rect(boxX - padding, boxY - padding, boxWidth + padding * 2, boxHeight + padding * 2);
        Rect textRect = new Rect(boxX, boxY, boxWidth, boxHeight);

        // Dibujar fondo semi-transparente
        Color bgCol = backgroundColor;
        bgCol.a *= alpha;
        GUI.color = bgCol;
        GUI.DrawTexture(bgRect, bgTexture, ScaleMode.StretchToFill, true, 0f);

        // Sombra del texto
        Color sCol = shadowColor;
        sCol.a *= alpha;
        GUI.color = sCol;
        GUI.Label(new Rect(textRect.x + 2, textRect.y + 2, textRect.width, textRect.height), currentText, shadowStyle);

        // Texto principal
        Color tCol = textColor;
        tCol.a *= alpha;
        GUI.color = tCol;
        dialogueStyle.normal.textColor = tCol;
        GUI.Label(textRect, currentText, dialogueStyle);

        // Restaurar color
        GUI.color = Color.white;
    }

    // ============== API PÚBLICA ==============

    /// <summary>
    /// Muestra un diálogo en pantalla durante la duración indicada.
    /// </summary>
    public void ShowDialogue(string text, float duration = -1f)
    {
        if (duration < 0f) duration = defaultDuration;

        currentText = text;
        totalDuration = duration;
        displayTimer = duration;
        isShowing = true;
        alpha = 0f;

        Debug.Log("[DialogueManager] Mostrando: \"" + text + "\" durante " + duration + "s");
    }

    /// <summary>
    /// Oculta el diálogo actual inmediatamente.
    /// </summary>
    public void HideDialogue()
    {
        isShowing = false;
        currentText = "";
        alpha = 0f;
    }

    /// <summary>
    /// ¿Se está mostrando un diálogo actualmente?
    /// </summary>
    public bool IsShowingDialogue()
    {
        return isShowing;
    }

    /// <summary>
    /// Crea la instancia si no existe (para otros scripts que lo necesiten)
    /// </summary>
    public static void EnsureExists()
    {
        if (Instance == null)
        {
            GameObject go = new GameObject("DialogueManager");
            go.AddComponent<DialogueManager>();
            Debug.Log("[DialogueManager] Creado automáticamente.");
        }
    }
}
