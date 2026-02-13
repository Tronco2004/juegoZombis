using UnityEngine;

/// <summary>
/// Trigger de diálogo al salir de la casa grande.
/// Colócalo en un GameObject con un Box Collider (isTrigger = true)
/// en la salida de la casa grande para que al pasar se muestre el mensaje.
/// </summary>
public class HouseExitDialogue : MonoBehaviour
{
    [Header("=== DIÁLOGO ===")]
    [Tooltip("Texto que se muestra al salir de la casa")]
    [TextArea(2, 5)]
    public string dialogueText = "Tiene que haber alguna forma de pasar a la siguiente parte.";

    [Tooltip("Duración del diálogo en pantalla (segundos)")]
    public float displayDuration = 5f;

    [Header("=== CONFIGURACIÓN ===")]
    [Tooltip("¿Solo mostrar el diálogo una vez?")]
    public bool triggerOnce = true;

    [Tooltip("Retraso antes de mostrar el texto (segundos)")]
    public float delay = 0.5f;

    [Header("=== AUDIO (Opcional) ===")]
    [Tooltip("Sonido al mostrar el diálogo")]
    public AudioClip dialogueSound;
    [Range(0f, 1f)]
    public float soundVolume = 0.8f;

    private bool hasTriggered = false;

    void Start()
    {
        // Asegurar que tiene un collider trigger
        Collider col = GetComponent<Collider>();
        if (col == null)
        {
            // Crear uno automáticamente
            BoxCollider box = gameObject.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = new Vector3(3f, 3f, 1f);
            Debug.LogWarning("[HouseExitDialogue] Se creó un BoxCollider automáticamente. Ajústalo en el editor.");
        }
        else if (!col.isTrigger)
        {
            col.isTrigger = true;
        }

        // Asegurar que existe el DialogueManager
        DialogueManager.EnsureExists();
    }

    void OnTriggerEnter(Collider other)
    {
        // Solo reaccionar al jugador
        if (!other.CompareTag("Player")) return;

        // Si ya se mostró y solo se activa una vez
        if (triggerOnce && hasTriggered) return;

        hasTriggered = true;

        if (delay > 0f)
        {
            StartCoroutine(ShowWithDelay());
        }
        else
        {
            ShowDialogue();
        }
    }

    private System.Collections.IEnumerator ShowWithDelay()
    {
        yield return new WaitForSeconds(delay);
        ShowDialogue();
    }

    private void ShowDialogue()
    {
        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.ShowDialogue(dialogueText, displayDuration);
        }

        // Sonido opcional
        if (dialogueSound != null)
        {
            AudioSource.PlayClipAtPoint(dialogueSound, transform.position, soundVolume);
        }

        Debug.Log("[HouseExitDialogue] Diálogo mostrado: " + dialogueText);
    }

    /// <summary>
    /// Permite resetear el trigger para que se pueda mostrar de nuevo
    /// </summary>
    public void ResetTrigger()
    {
        hasTriggered = false;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = hasTriggered ? new Color(0.5f, 0.5f, 0.5f, 0.3f) : new Color(0f, 1f, 1f, 0.3f);
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            if (col is BoxCollider box)
            {
                Gizmos.DrawCube(box.center, box.size);
                Gizmos.DrawWireCube(box.center, box.size);
            }
        }
    }
}
