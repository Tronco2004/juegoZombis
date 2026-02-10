using UnityEngine;

/// <summary>
/// Pantalla individual del Simon Says.
/// Cada pantalla tiene un hijo (Quad/Plane) con un color que simula la pantalla encendida.
/// 
/// SETUP EN UNITY:
/// 1. Coloca un modelo PantallaSimon en la escena.
/// 2. Crea un hijo (GameObject > 3D Object > Quad) y posiciónalo sobre la pantalla.
/// 3. Crea un Material con el color deseado (Rojo/Verde/Azul/Amarillo), Emission ON.
/// 4. Asigna el material al Quad y arrastra el Quad al campo "colorPanel" del inspector.
/// 5. Pon el Tag correspondiente: "SimonRojo", "SimonVerde", "SimonAzul", "SimonAmarillo".
/// 6. El Quad empieza desactivado (apagado).
/// </summary>
public class SimonSaysScreen : MonoBehaviour
{
    public enum SimonColor
    {
        Rojo,
        Verde,
        Azul,
        Amarillo
    }

    [Header("=== CONFIGURACIÓN ===")]
    [Tooltip("Color que representa esta pantalla")]
    public SimonColor screenColor = SimonColor.Rojo;

    [Tooltip("El GameObject hijo que simula la pantalla encendida (Quad con material de color)")]
    public GameObject colorPanel;

    [Header("=== AUDIO (Opcional) ===")]
    [Tooltip("Sonido al encender la pantalla")]
    public AudioClip screenOnSound;

    [Tooltip("Sonido al seleccionar (acierto)")]
    public AudioClip correctSound;

    [Tooltip("Sonido al fallar")]
    public AudioClip wrongSound;

    // Estado
    private bool isLit = false;
    private AudioSource audioSource;
    private Renderer panelRenderer;
    private Color originalEmissionColor;

    void Start()
    {
        // Audio
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 1f;
            audioSource.playOnAwake = false;
        }

        // Panel de color: empieza apagado
        if (colorPanel != null)
        {
            panelRenderer = colorPanel.GetComponent<Renderer>();
            if (panelRenderer != null && panelRenderer.material.HasProperty("_EmissionColor"))
            {
                originalEmissionColor = panelRenderer.material.GetColor("_EmissionColor");
            }
            colorPanel.SetActive(false);
        }
        else
        {
            Debug.LogWarning("[SimonScreen] No hay colorPanel asignado en " + gameObject.name);
        }
    }

    /// <summary>
    /// Enciende la pantalla (activa el panel de color).
    /// </summary>
    public void Encender()
    {
        if (colorPanel == null) return;
        colorPanel.SetActive(true);
        isLit = true;
        PlaySound(screenOnSound);
    }

    /// <summary>
    /// Apaga la pantalla (desactiva el panel de color).
    /// </summary>
    public void Apagar()
    {
        if (colorPanel == null) return;
        colorPanel.SetActive(false);
        isLit = false;
    }

    /// <summary>
    /// Parpadea la pantalla brevemente para dar feedback visual.
    /// </summary>
    public void Parpadear(float duracion = 0.3f)
    {
        StartCoroutine(ParpadeoCoroutine(duracion));
    }

    private System.Collections.IEnumerator ParpadeoCoroutine(float duracion)
    {
        Encender();
        yield return new WaitForSeconds(duracion);
        Apagar();
    }

    /// <summary>
    /// Flash rápido de feedback (acierto o error).
    /// </summary>
    public void FlashFeedback(bool acierto)
    {
        PlaySound(acierto ? correctSound : wrongSound);
        Parpadear(acierto ? 0.3f : 0.6f);
    }

    public bool IsLit => isLit;

    private void PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null)
            audioSource.PlayOneShot(clip);
    }
}
