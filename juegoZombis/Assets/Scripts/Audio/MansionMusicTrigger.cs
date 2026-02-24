using UnityEngine;

/// <summary>
/// Trigger que cambia la música al entrar/salir de la mansión.
///
/// USO:
///   1. Crea un GameObject vacío dentro de la mansión (ej: "MansionMusicZone")
///   2. Añádele un Box Collider (o cualquier Collider) y marca "Is Trigger"
///   3. Escálalo para que cubra TODA la mansión por dentro
///   4. Añade este script al mismo GameObject
///   5. ¡Listo! Cuando el jugador entre, cambia la música. Cuando salga, vuelve.
/// </summary>
public class MansionMusicTrigger : MonoBehaviour
{
    [Tooltip("¿Volver a la música normal al salir de la mansión?")]
    public bool revertOnExit = true;

    void Start()
    {
        // Verificar que tiene collider trigger
        Collider col = GetComponent<Collider>();
        if (col == null)
        {
            Debug.LogError("[MansionMusicTrigger] ¡Necesita un Collider con isTrigger!");
            return;
        }
        if (!col.isTrigger)
        {
            Debug.LogWarning("[MansionMusicTrigger] El Collider no es Trigger. Activándolo.");
            col.isTrigger = true;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        if (GameMusicManager.Instance != null)
        {
            GameMusicManager.Instance.EnterMansion();
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (!revertOnExit) return;

        if (GameMusicManager.Instance != null)
        {
            GameMusicManager.Instance.ExitMansion();
        }
    }

    void OnDrawGizmos()
    {
        // Dibujar zona en el editor
        Gizmos.color = new Color(0.5f, 0f, 1f, 0.15f);
        Collider col = GetComponent<Collider>();
        if (col != null && col is BoxCollider box)
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(box.center, box.size);
            Gizmos.color = new Color(0.5f, 0f, 1f, 0.5f);
            Gizmos.DrawWireCube(box.center, box.size);
        }
    }
}
