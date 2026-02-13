using UnityEngine;

/// <summary>
/// Intercambia un GameObject (hangar cerrado) por otro (hangar abierto).
/// Pensado para el hangar cuya puerta no se puede animar:
///   - Coloca ambos prefabs en la misma posición/rotación.
///   - El "abierto" empieza desactivado.
///   - Cuando se llama a Swap(), desactiva el cerrado y activa el abierto.
///
/// Se puede llamar desde SimonSaysManager, UnityEvents, o cualquier otro script.
/// </summary>
public class HangarDoorSwap : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("El GameObject del hangar con la puerta CERRADA (se desactivará)")]
    public GameObject hangarCerrado;

    [Tooltip("El GameObject del hangar con la puerta ABIERTA (se activará)")]
    public GameObject hangarAbierto;

    [Header("Efectos (Opcional)")]
    [Tooltip("Sonido al abrir la puerta del hangar")]
    public AudioClip openSound;

    [Tooltip("Efecto de partículas al abrir (polvo, etc.)")]
    public ParticleSystem openEffect;

    [Header("Estado")]
    [Tooltip("¿Ya se ha abierto?")]
    public bool isOpen = false;

    private AudioSource audioSource;

    void Start()
    {
        // Asegurar estado inicial: cerrado activo, abierto inactivo
        if (hangarCerrado != null) hangarCerrado.SetActive(true);
        if (hangarAbierto != null) hangarAbierto.SetActive(false);

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 1f;
            audioSource.playOnAwake = false;
        }
    }

    /// <summary>
    /// Intercambia el hangar cerrado por el abierto.
    /// Llamar desde SimonSaysManager.Victoria() o desde un UnityEvent.
    /// </summary>
    public void Swap()
    {
        if (isOpen) return; // Ya está abierto, no hacer nada
        isOpen = true;

        if (hangarCerrado != null) hangarCerrado.SetActive(false);
        if (hangarAbierto != null) hangarAbierto.SetActive(true);

        // Sonido
        if (openSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(openSound);
        }

        // Efecto de partículas
        if (openEffect != null)
        {
            openEffect.Play();
        }

        Debug.Log("[HangarDoorSwap] ¡Puerta del hangar abierta!");
    }

    /// <summary>
    /// Cierra el hangar (por si acaso quieres revertirlo).
    /// </summary>
    public void Close()
    {
        if (!isOpen) return;
        isOpen = false;

        if (hangarCerrado != null) hangarCerrado.SetActive(true);
        if (hangarAbierto != null) hangarAbierto.SetActive(false);

        Debug.Log("[HangarDoorSwap] Puerta del hangar cerrada.");
    }
}
