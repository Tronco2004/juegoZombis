using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Zona de activación de spawns basada en trigger.
///
/// SETUP en Unity:
/// 1. Crea un GameObject vacío y ponle este script.
/// 2. Añade un Collider (Box/Sphere/Capsule)  márcalo como "Is Trigger".
/// 3. Asígnale la SpawnZone que representa esta zona.
/// 4. Al inicio DESACTIVA automáticamente todos los ZombieSpawnPoint de esa zona.
/// 5. Cuando el jugador entre en el trigger, los ACTIVA todos y notifica al ZombieSpawner.
/// 6. Cuando el jugador salga, los DESACTIVA todos y notifica al ZombieSpawner.
///
/// Asegúrate de que el tag del jugador sea "Player".
/// </summary>
[RequireComponent(typeof(Collider))]
public class ZombieActivationZone : MonoBehaviour
{
    [Header("Zona que activa este trigger")]
    public SpawnZone zone = SpawnZone.Zona1A;

    [Header("Opciones")]
    [Tooltip("Tag del jugador para detectar la entrada.")]
    public string playerTag = "Player";

    [Tooltip("Color del gizmo de esta zona (se asigna automáticamente según la zona si se deja en negro).")]
    public Color gizmoColor = Color.clear;

    // Todos los ZombieSpawnPoint de la escena que pertenecen a esta zona
    private List<ZombieSpawnPoint> controlledPoints = new List<ZombieSpawnPoint>();

    void Awake()
    {
        // Garantizar que el collider sea trigger
        Collider col = GetComponent<Collider>();
        if (!col.isTrigger)
        {
            col.isTrigger = true;
            Debug.LogWarning($"[ZombieActivationZone] '{name}': Collider puesto a isTrigger=true automáticamente.");
        }
    }

    void Start()
    {
        // Cachear todos los ZombieSpawnPoint de la escena que coincidan con esta zona
        ZombieSpawnPoint[] all = FindObjectsOfType<ZombieSpawnPoint>();
        controlledPoints.Clear();
        foreach (var sp in all)
        {
            if (sp.zone == zone)
                controlledPoints.Add(sp);
        }

        // Desactivarlos al inicio: solo spawnan cuando el jugador esté dentro
        SetPointsActive(false);

        Debug.Log($"[ZombieActivationZone] '{name}' controla {controlledPoints.Count} spawn point(s) de {zone}. Desactivados al inicio.");
    }

    //  Detección 

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;

        // ── Zona 3: funciona como TOGGLE (puerta de entrada/salida) ──
        if (zone == SpawnZone.Zona3_Infinitos)
        {
            bool currentlyInZone3 = ZombieSpawner.Instance != null &&
                                    ZombieSpawner.Instance.CurrentPlayerZone == SpawnZone.Zona3_Infinitos;

            if (!currentlyInZone3)
            {
                // ENTRAR en Zona 3
                ZombieActivationZone[] allZones = FindObjectsOfType<ZombieActivationZone>();
                foreach (var z in allZones)
                    if (z != this) z.ForceDeactivate();

                SetPointsActive(true);
                Debug.Log($"[ZombieActivationZone] TOGGLE Zona 3 → ACTIVADA");

                if (ZombieSpawner.Instance != null)
                    ZombieSpawner.Instance.NotifyZoneEntered(zone);
            }
            // Si ya está en Zona 3, no hacemos nada al entrar al trigger.
            // Solo se sale al volver a cruzar (OnTriggerExit).
            return;
        }

        // ── Zonas normales: comportamiento clásico ──
        // Desactivar TODAS las demás zonas antes de activar esta
        ZombieActivationZone[] allOtherZones = FindObjectsOfType<ZombieActivationZone>();
        foreach (var z in allOtherZones)
        {
            if (z != this) z.ForceDeactivate();
        }

        SetPointsActive(true);
        Debug.Log($"[ZombieActivationZone] Jugador ENTRÓ en {zone} → {controlledPoints.Count} spawn points ACTIVADOS.");

        if (ZombieSpawner.Instance != null)
            ZombieSpawner.Instance.NotifyZoneEntered(zone);
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;

        // ── Zona 3: TOGGLE — al salir del trigger siendo zona 3 activa → desactivar ──
        if (zone == SpawnZone.Zona3_Infinitos)
        {
            bool currentlyInZone3 = ZombieSpawner.Instance != null &&
                                    ZombieSpawner.Instance.CurrentPlayerZone == SpawnZone.Zona3_Infinitos;

            if (currentlyInZone3)
            {
                // El jugador cruza la puerta de vuelta → SALIR de Zona 3
                SetPointsActive(false);
                Debug.Log($"[ZombieActivationZone] TOGGLE Zona 3 → DESACTIVADA (jugador salió por la puerta)");

                if (ZombieSpawner.Instance != null)
                    ZombieSpawner.Instance.NotifyZoneExited(zone);
            }
            return;
        }

        // ── Zonas normales: no desactivar al salir ──
        // Los puntos se desactivan únicamente cuando el jugador ENTRA en otra zona.
        Debug.Log($"[ZombieActivationZone] Jugador SALIÓ de {zone} (spawns siguen activos hasta entrar en otra zona).");

        if (ZombieSpawner.Instance != null)
            ZombieSpawner.Instance.NotifyZoneExited(zone);
    }

    //  Control de puntos 

    void SetPointsActive(bool active)
    {
        foreach (var sp in controlledPoints)
        {
            if (sp != null) sp.isActive = active;
        }
    }

    /// <summary>
    /// Desactiva esta zona desde fuera (llamado automáticamente cuando el jugador entra en otra zona).
    /// </summary>
    public void ForceDeactivate()
    {
        SetPointsActive(false);
    }

    /// <summary>
    /// Fuerza la re-búsqueda de spawn points (úsalo si creas puntos en runtime).
    /// </summary>
    public void RefreshControlledPoints()
    {
        controlledPoints.Clear();
        ZombieSpawnPoint[] all = FindObjectsOfType<ZombieSpawnPoint>();
        foreach (var sp in all)
            if (sp.zone == zone) controlledPoints.Add(sp);
    }

    //  Gizmos 

    Color GetZoneColor()
    {
        if (gizmoColor != Color.clear && gizmoColor.a > 0f) return gizmoColor;

        switch (zone)
        {
            case SpawnZone.Zona1A:          return new Color(0.0f, 1.0f, 0.2f, 0.30f);
            case SpawnZone.Zona1B:          return new Color(0.2f, 0.8f, 0.0f, 0.30f);
            case SpawnZone.Zona1C:          return new Color(0.5f, 1.0f, 0.0f, 0.30f);
            case SpawnZone.Zona2:           return new Color(0.0f, 0.5f, 1.0f, 0.30f);
            case SpawnZone.Mansion:         return new Color(1.0f, 0.5f, 0.0f, 0.30f);
            case SpawnZone.AtrasMansion:    return new Color(0.8f, 0.0f, 0.8f, 0.30f);
            case SpawnZone.Zona3_Infinitos: return new Color(1.0f, 0.0f, 0.0f, 0.30f);
            default: return new Color(1f, 1f, 1f, 0.20f);
        }
    }

    void OnDrawGizmos()
    {
        Collider col = GetComponent<Collider>();
        if (col == null) return;

        Color c = GetZoneColor();
        Gizmos.matrix = transform.localToWorldMatrix;

        Gizmos.color = c;
        if (col is BoxCollider box)
        {
            Gizmos.DrawCube(box.center, box.size);
            c.a = Mathf.Min(c.a + 0.5f, 1f);
            Gizmos.color = c;
            Gizmos.DrawWireCube(box.center, box.size);
        }
        else if (col is SphereCollider sphere)
        {
            Gizmos.DrawSphere(sphere.center, sphere.radius);
            c.a = Mathf.Min(c.a + 0.5f, 1f);
            Gizmos.color = c;
            Gizmos.DrawWireSphere(sphere.center, sphere.radius);
        }
        else
        {
            Gizmos.DrawCube(Vector3.zero, Vector3.one);
        }

        Gizmos.matrix = Matrix4x4.identity;
    }
}
