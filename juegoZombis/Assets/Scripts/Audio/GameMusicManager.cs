using UnityEngine;
using System.Collections;

/// <summary>
/// Gestor de música del juego.
/// Reproduce música de fondo durante la partida y cambia a una música
/// especial cuando el jugador entra en la mansión.
///
/// USO:
///   1. Añade este script a un GameObject vacío (ej: "MusicManager")
///   2. Asigna los AudioClips desde el Inspector:
///      - "Game Music"    → música que suena al jugar (ej: BLACK OPS 2 ZOMBIES)
///      - "Mansion Music" → música especial de la mansión (ej: Sonido al entrar casa)
///   3. ¡Listo! La música de mansión se activa automáticamente cuando
///      el jugador entra en un trigger con el tag "MansionMusicZone"
///      o puedes llamar a GameMusicManager.Instance.EnterMansion() / ExitMansion()
/// </summary>
public class GameMusicManager : MonoBehaviour
{
    public static GameMusicManager Instance { get; private set; }

    [Header("=== MÚSICA DE JUEGO ===")]
    [Tooltip("Música que suena durante la partida normal")]
    public AudioClip gameMusic;
    [Tooltip("Volumen de la música de juego (0-1)")]
    [Range(0f, 1f)]
    public float gameMusicVolume = 0.3f;

    [Header("=== MÚSICA DE MANSIÓN ===")]
    [Tooltip("Música especial que suena dentro de la mansión")]
    public AudioClip mansionMusic;
    [Tooltip("Volumen de la música de mansión (0-1)")]
    [Range(0f, 1f)]
    public float mansionMusicVolume = 0.45f;

    [Header("=== TRANSICIÓN ===")]
    [Tooltip("Duración del crossfade entre músicas (segundos)")]
    public float crossfadeDuration = 1.5f;

    [Header("=== OPCIONES ===")]
    [Tooltip("¿Empezar la música al iniciar la escena?")]
    public bool playOnStart = true;
    [Tooltip("¿Loop en ambas músicas?")]
    public bool loopMusic = true;

    // Dos AudioSources para crossfade suave
    private AudioSource sourceA;
    private AudioSource sourceB;
    private AudioSource currentSource;
    private bool isInMansion = false;
    private Coroutine fadeCoroutine;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Crear los dos AudioSources
        sourceA = gameObject.AddComponent<AudioSource>();
        sourceA.playOnAwake = false;
        sourceA.loop = loopMusic;
        sourceA.spatialBlend = 0f; // 2D
        sourceA.priority = 200; // Prioridad BAJA para no silenciar sonidos de zombies

        sourceB = gameObject.AddComponent<AudioSource>();
        sourceB.playOnAwake = false;
        sourceB.loop = loopMusic;
        sourceB.spatialBlend = 0f; // 2D
        sourceB.priority = 200; // Prioridad BAJA

        currentSource = sourceA;
    }

    void Start()
    {
        if (playOnStart && gameMusic != null)
        {
            PlayGameMusic();
        }
    }

    // ══════════════════════════════════════════════════════════
    //  MÉTODOS PÚBLICOS
    // ══════════════════════════════════════════════════════════

    /// <summary>Reproduce la música normal de juego.</summary>
    public void PlayGameMusic()
    {
        if (gameMusic == null) return;
        CrossfadeTo(gameMusic, gameMusicVolume);
        isInMansion = false;
        Debug.Log("[GameMusic] ♪ Música de juego");
    }

    /// <summary>Cambia a la música de la mansión con crossfade.</summary>
    public void EnterMansion()
    {
        if (isInMansion) return;
        if (mansionMusic == null)
        {
            Debug.LogWarning("[GameMusic] No hay música de mansión asignada.");
            return;
        }
        isInMansion = true;
        CrossfadeTo(mansionMusic, mansionMusicVolume);
        Debug.Log("[GameMusic] ♪ Música de mansión");
    }

    /// <summary>Vuelve a la música normal de juego con crossfade.</summary>
    public void ExitMansion()
    {
        if (!isInMansion) return;
        isInMansion = false;
        if (gameMusic != null)
        {
            CrossfadeTo(gameMusic, gameMusicVolume);
            Debug.Log("[GameMusic] ♪ Volviendo a música de juego");
        }
    }

    /// <summary>Para toda la música con fade out.</summary>
    public void StopMusic()
    {
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(FadeOut(currentSource, crossfadeDuration));
    }

    /// <summary>¿Está actualmente en la mansión?</summary>
    public bool IsInMansion => isInMansion;

    /// <summary>
    /// Cambia a cualquier música/sonido ambiental con crossfade.
    /// Úsalo para zonas personalizadas (militar, bosque, etc.)
    /// </summary>
    public void PlayZoneMusic(AudioClip zoneClip, float volume)
    {
        if (zoneClip == null) return;
        CrossfadeTo(zoneClip, volume);
        Debug.Log($"[GameMusic] ♪ Música de zona: {zoneClip.name}");
    }

    /// <summary>
    /// Vuelve a la música normal de juego (para usar al salir de una zona).
    /// </summary>
    public void ReturnToGameMusic()
    {
        isInMansion = false;
        if (gameMusic != null)
        {
            CrossfadeTo(gameMusic, gameMusicVolume);
            Debug.Log("[GameMusic] ♪ Volviendo a música de juego");
        }
    }

    // ══════════════════════════════════════════════════════════
    //  CROSSFADE
    // ══════════════════════════════════════════════════════════

    void CrossfadeTo(AudioClip newClip, float targetVolume)
    {
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);

        // Elegir el source que NO está sonando
        AudioSource nextSource = (currentSource == sourceA) ? sourceB : sourceA;
        AudioSource prevSource = currentSource;

        // Configurar el nuevo source
        nextSource.clip = newClip;
        nextSource.volume = 0f;
        nextSource.loop = loopMusic;
        nextSource.Play();

        currentSource = nextSource;

        fadeCoroutine = StartCoroutine(DoCrossfade(prevSource, nextSource, targetVolume));
    }

    IEnumerator DoCrossfade(AudioSource fadeOutSource, AudioSource fadeInSource, float targetVolume)
    {
        float t = 0f;
        float startVolOut = fadeOutSource.volume;

        while (t < crossfadeDuration)
        {
            t += Time.unscaledDeltaTime;
            float progress = t / crossfadeDuration;

            // Fade out del anterior
            fadeOutSource.volume = Mathf.Lerp(startVolOut, 0f, progress);
            // Fade in del nuevo
            fadeInSource.volume = Mathf.Lerp(0f, targetVolume, progress);

            yield return null;
        }

        fadeOutSource.volume = 0f;
        fadeOutSource.Stop();
        fadeInSource.volume = targetVolume;
        fadeCoroutine = null;
    }

    IEnumerator FadeOut(AudioSource source, float duration)
    {
        float startVol = source.volume;
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            source.volume = Mathf.Lerp(startVol, 0f, t / duration);
            yield return null;
        }
        source.volume = 0f;
        source.Stop();

        // Parar el otro también
        AudioSource other = (source == sourceA) ? sourceB : sourceA;
        other.volume = 0f;
        other.Stop();
    }
}
