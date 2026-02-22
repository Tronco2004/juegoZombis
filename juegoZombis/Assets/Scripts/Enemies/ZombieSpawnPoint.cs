using UnityEngine;

/// <summary>
/// Zonas lógicas del mapa.
/// </summary>
public enum SpawnZone
{
    Zona1A,          // Primera sección de la zona 1
    Zona1B,          // Segunda sección de la zona 1
    Zona1C,          // Tercera sección de la zona 1
    Zona2,
    Mansion,
    AtrasMansion,
    Zona3_Infinitos
}

/// <summary>
/// Punto exacto donde spawneará un zombi.
///
/// SETUP en Unity:
/// 1. Crea un GameObject vacío y ponle este script.
/// 2. Colócalo EXACTAMENTE donde quieres que aparezca el zombi.
/// 3. Asígnale la SpawnZone a la que pertenece.
/// 4. El ZombieActivationZone de esa zona lo activará automáticamente.
///
/// Los zombis spawnean en transform.position exactamente.
/// Radio opcional para una variación mínima (déjalo en 0 para posición 100% fija).
/// </summary>
public class ZombieSpawnPoint : MonoBehaviour
{
    [Header("Zona")]
    [Tooltip("Zona a la que pertenece este punto de spawn.")]
    public SpawnZone zone = SpawnZone.Zona1A;

    [Header("Estado")]
    [Tooltip("Solo los puntos activos son usados por el spawner. La ZombieActivationZone de esta zona lo controla automáticamente.")]
    public bool isActive = true;

    [Header("Variación (opcional)")]
    [Tooltip("Radio de variación aleatoria alrededor del punto exacto. 0 = posición fija.")]
    public float spawnRadius = 0f;

    //  Propiedades 

    /// <summary>¿Este punto pertenece a la zona infinita?</summary>
    public bool IsInfiniteZone => zone == SpawnZone.Zona3_Infinitos;

    /// <summary>Devuelve la posición de spawn exacta (con variación opcional).</summary>
    public Vector3 GetSpawnPosition()
    {
        if (spawnRadius <= 0f)
            return transform.position;

        Vector2 circle = Random.insideUnitCircle * spawnRadius;
        return transform.position + new Vector3(circle.x, 0f, circle.y);
    }

    /// <summary>Alias para compatibilidad con el ZombieSpawner.</summary>
    public Vector3 GetRandomSpawnPosition() => GetSpawnPosition();

    /// <summary>Distancia desde este punto a una posición.</summary>
    public float DistanceTo(Vector3 position) => Vector3.Distance(transform.position, position);

    /// <summary>El jugador siempre está "en rango" si la zona se gestiona por triggers.</summary>
    public bool IsPlayerInRange(Vector3 playerPosition) => true;

    //  Gizmos 

    Color GetZoneColor()
    {
        switch (zone)
        {
            case SpawnZone.Zona1A:          return new Color(0.0f, 1.0f, 0.2f, 0.9f);  // verde vivo
            case SpawnZone.Zona1B:          return new Color(0.2f, 0.8f, 0.0f, 0.9f);  // verde medio
            case SpawnZone.Zona1C:          return new Color(0.5f, 1.0f, 0.0f, 0.9f);  // verde lima
            case SpawnZone.Zona2:           return new Color(0.0f, 0.5f, 1.0f, 0.9f);
            case SpawnZone.Mansion:         return new Color(1.0f, 0.5f, 0.0f, 0.9f);
            case SpawnZone.AtrasMansion:    return new Color(0.8f, 0.0f, 0.8f, 0.9f);
            case SpawnZone.Zona3_Infinitos: return new Color(1.0f, 0.0f, 0.0f, 0.9f);
            default:                        return Color.white;
        }
    }

    void OnDrawGizmos()
    {
        Color c = GetZoneColor();
        // Punto inactivo: se dibuja gris y semitransparente
        if (!isActive) c = new Color(0.4f, 0.4f, 0.4f, 0.35f);
        else c.a = 0.85f;
        Gizmos.color = c;

        // Cruz indicando la posición exacta
        float s = 0.4f;
        Vector3 p = transform.position;
        Gizmos.DrawLine(p + Vector3.left  * s, p + Vector3.right   * s);
        Gizmos.DrawLine(p + Vector3.back  * s, p + Vector3.forward * s);
        Gizmos.DrawLine(p + Vector3.down  * s, p + Vector3.up      * s);
        Gizmos.DrawSphere(p, 0.18f);

        if (spawnRadius > 0f)
        {
            c.a = 0.12f;
            Gizmos.color = c;
            Gizmos.DrawSphere(p, spawnRadius);
        }
    }

    void OnDrawGizmosSelected()
    {
        Color c = GetZoneColor();
        Gizmos.color = c;
        Gizmos.DrawSphere(transform.position, 0.28f);

        if (spawnRadius > 0f)
        {
            c.a = 0.25f;
            Gizmos.color = c;
            Gizmos.DrawWireSphere(transform.position, spawnRadius);
        }
    }
}
