using UnityEngine;

/// <summary>
/// Trigger genérico para cambiar la música al entrar en una zona.
/// Funciona con cualquier zona: casa, zona militar, bosque, etc.
///
/// USO:
///   1. Crea un GameObject vacío en la zona (ej: "ZonaMilitarMusic")
///   2. Añádele un Box Collider y marca "Is Trigger"
///   3. Escálalo para que cubra toda la zona
///   4. Añade este script al GameObject
///   5. Arrastra el AudioClip de la zona al campo "Zone Music"
///   6. Ajusta el volumen
///   7. ¡Listo! Al entrar suena la música de zona, al salir vuelve la normal
/// </summary>
public class ZoneMusicTrigger : MonoBehaviour
{
    [Header("=== MÚSICA DE ZONA ===")]
    [Tooltip("Música/sonido ambiental de esta zona")]
    public AudioClip zoneMusic;
    
    [Tooltip("Volumen de la música de esta zona")]
    [Range(0f, 1f)]
    public float zoneMusicVolume = 0.4f;
    
    [Tooltip("¿Volver a la música normal al salir de la zona?")]
    public bool revertOnExit = true;
    
    [Header("=== OPCIONES ===")]
    [Tooltip("Nombre de la zona (para debug)")]
    public string zoneName = "Zona";

    void Start()
    {
        // Verificar que tiene collider trigger
        Collider col = GetComponent<Collider>();
        if (col == null)
        {
            Debug.LogError($"[ZoneMusicTrigger] '{zoneName}': ¡Necesita un Collider con isTrigger!");
            return;
        }
        if (!col.isTrigger)
        {
            Debug.LogWarning($"[ZoneMusicTrigger] '{zoneName}': El Collider no es Trigger. Activándolo.");
            col.isTrigger = true;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        if (GameMusicManager.Instance != null && zoneMusic != null)
        {
            GameMusicManager.Instance.PlayZoneMusic(zoneMusic, zoneMusicVolume);
            Debug.Log($"[ZoneMusicTrigger] Entrando en '{zoneName}'");
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (!revertOnExit) return;

        if (GameMusicManager.Instance != null)
        {
            GameMusicManager.Instance.ReturnToGameMusic();
            Debug.Log($"[ZoneMusicTrigger] Saliendo de '{zoneName}'");
        }
    }

    void OnDrawGizmos()
    {
        // Dibujar zona en el editor
        Gizmos.color = new Color(0f, 0.8f, 0.2f, 0.15f);
        Collider col = GetComponent<Collider>();
        if (col != null && col is BoxCollider box)
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(box.center, box.size);
            Gizmos.color = new Color(0f, 0.8f, 0.2f, 0.5f);
            Gizmos.DrawWireCube(box.center, box.size);
        }
    }
}
