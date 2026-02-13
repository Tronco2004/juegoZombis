using UnityEngine;

/// <summary>
/// Zonas lógicas del mapa. Cada SpawnPoint pertenece a una zona.
/// </summary>
public enum SpawnZone
{
    Zona1,
    Zona2,
    Mansion,
    AtrasMansion,
    Zona3_Infinitos
}

/// <summary>
/// Punto de spawn para zombis. Coloca varios de estos en la escena para definir
/// las zonas donde pueden aparecer/reaparecer los zombis.
/// El ZombieSpawner los detecta automáticamente.
/// </summary>
public class ZombieSpawnPoint : MonoBehaviour
{
    [Header("Zona")]
    [Tooltip("¿A qué zona del mapa pertenece este punto de spawn?")]
    public SpawnZone zone = SpawnZone.Zona1;

    [Header("Zona de Spawn")]
    [Tooltip("Tamaño de la zona rectangular de spawn (ancho X, alto Y, profundidad Z)")]
    public Vector3 spawnZoneSize = new Vector3(20f, 0f, 20f);

    [Tooltip("Desplazamiento del centro de la zona respecto al transform")]
    public Vector3 spawnZoneOffset = Vector3.zero;

    [Header("Rango de Activación")]
    [Tooltip("Distancia máxima a la que el jugador 'activa' este punto de spawn. " +
             "Si el jugador está dentro de este radio, los zombis pueden aparecer aquí.")]
    public float activationRange = 60f;

    [Header("Gizmos")]
    public Color gizmoColor = new Color(0f, 1f, 0f, 0.25f);
    public Color rangeGizmoColor = new Color(1f, 1f, 0f, 0.08f);

    /// <summary>
    /// Centro real de la zona de spawn en coordenadas mundo.
    /// </summary>
    public Vector3 Center => transform.position + spawnZoneOffset;

    /// <summary>
    /// ¿Este spawn point es de la zona infinita (Zona3)?
    /// </summary>
    public bool IsInfiniteZone => zone == SpawnZone.Zona3_Infinitos;

    /// <summary>
    /// Devuelve una posición aleatoria dentro de la zona de spawn.
    /// </summary>
    public Vector3 GetRandomSpawnPosition()
    {
        Vector3 center = Center;
        Vector3 half = spawnZoneSize * 0.5f;

        float x = Random.Range(center.x - half.x, center.x + half.x);
        float y = center.y + Random.Range(-half.y, half.y);
        float z = Random.Range(center.z - half.z, center.z + half.z);

        return new Vector3(x, y, z);
    }

    /// <summary>
    /// ¿Está el jugador dentro del rango de activación de este spawn point?
    /// </summary>
    public bool IsPlayerInRange(Vector3 playerPosition)
    {
        return Vector3.Distance(Center, playerPosition) <= activationRange;
    }

    /// <summary>
    /// Distancia desde este punto al jugador.
    /// </summary>
    public float DistanceTo(Vector3 position)
    {
        return Vector3.Distance(Center, position);
    }

    // ─── Gizmos ───────────────────────────────────────────────

    /// <summary>
    /// Color automático según zona para distinguirlas fácil en el editor.
    /// </summary>
    Color GetZoneGizmoColor()
    {
        switch (zone)
        {
            case SpawnZone.Zona1:           return new Color(0f, 1f, 0f, 0.25f);    // Verde
            case SpawnZone.Zona2:           return new Color(0f, 0.5f, 1f, 0.25f);  // Azul
            case SpawnZone.Mansion:         return new Color(1f, 0.5f, 0f, 0.25f);  // Naranja
            case SpawnZone.AtrasMansion:    return new Color(0.8f, 0f, 0.8f, 0.25f);// Púrpura
            case SpawnZone.Zona3_Infinitos: return new Color(1f, 0f, 0f, 0.35f);    // Rojo intenso
            default:                        return gizmoColor;
        }
    }

    void OnDrawGizmos()
    {
        Color zoneColor = GetZoneGizmoColor();
        zoneColor.a *= 0.5f;
        Gizmos.color = zoneColor;
        Gizmos.DrawCube(Center, spawnZoneSize);
    }

    void OnDrawGizmosSelected()
    {
        Color zoneColor = GetZoneGizmoColor();

        // Zona de spawn sólida
        Gizmos.color = zoneColor;
        Gizmos.DrawCube(Center, spawnZoneSize);

        // Borde de la zona
        Color wire = zoneColor;
        wire.a = 1f;
        Gizmos.color = wire;
        Gizmos.DrawWireCube(Center, spawnZoneSize);

        // Radio de activación
        Color range = rangeGizmoColor;
        if (IsInfiniteZone) range = new Color(1f, 0f, 0f, 0.12f);
        Gizmos.color = range;
        Gizmos.DrawWireSphere(Center, activationRange);
    }
}
