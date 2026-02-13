using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Controlador global de oleadas de zombis con sistema de ZONAS.
///
/// ZONAS NORMALES (Zona1, Zona2, Mansion, AtrasMansion):
///   - Oleadas clásicas: se spawnean X zombis, cuando mueren todos pasa la siguiente oleada.
///   - Los zombis aparecen en los SpawnPoints de la zona en la que esté el jugador.
///   - Si el jugador cambia de zona, los zombis lejanos se reubican al spawn más cercano.
///
/// ZONA INFINITA (Zona3_Infinitos):
///   - Mientras el jugador esté en esa zona, se spawnean zombis sin parar
///     (hasta un máximo simultáneo configurable). No hay oleadas.
///   - Al salir de la zona los zombis infinitos se destruyen o reubican.
///
/// Solo debe haber UNO en la escena.
/// </summary>
public class ZombieSpawner : MonoBehaviour
{
    // ─── Singleton ────────────────────────────────────────────
    public static ZombieSpawner Instance { get; private set; }

    [Header("Prefab")]
    public GameObject zombiePrefab;

    [Header("Spawn Points (auto-detect si vacío)")]
    public List<ZombieSpawnPoint> spawnPoints = new List<ZombieSpawnPoint>();

    // ── Oleadas (zonas normales) ──────────────────────────────
    [Header("Oleadas (Zonas normales)")]
    public int baseZombies = 10;
    public float timeBetweenSpawns = 0.4f;
    public float timeBetweenWaves = 3f;

    [Header("Escalado por oleada")]
    public float baseHealth = 100f;
    public float healthPerWave = 20f;
    public float baseDamage = 20f;
    public int damageIncreaseEveryWaves = 4;
    public float damageBonusPerStep = 5f;

    // ── Zona Infinita ─────────────────────────────────────────
    [Header("Zona 3 — Zombis Infinitos")]
    [Tooltip("Máximo de zombis vivos a la vez en la zona infinita")]
    public int infiniteZoneMaxAlive = 20;
    [Tooltip("Tiempo entre cada spawn en la zona infinita")]
    public float infiniteSpawnInterval = 1.5f;
    [Tooltip("Vida de los zombis en zona infinita (fija, no escala por oleada)")]
    public float infiniteZoneHealth = 150f;
    [Tooltip("Daño de los zombis en zona infinita")]
    public float infiniteZoneDamage = 25f;

    // ── Reubicación ───────────────────────────────────────────
    [Header("Reubicación de Zombis")]
    public float relocateDistance = 70f;
    public float relocateCheckInterval = 2f;

    // ── UI ────────────────────────────────────────────────────
    [Header("UI Oleada")]
    public TextMeshProUGUI waveText;
    public bool createWaveTextIfMissing = true;
    public string wavePrefix = "Oleada: ";
    public Vector2 waveTextAnchoredPos = new Vector2(20f, -20f);
    public Vector2 waveTextSize = new Vector2(300f, 60f);
    public int waveTextFontSize = 32;
    public Color waveTextColor = Color.white;

    // ══════════════════════════════════════════════════════════
    //  ESTADO INTERNO
    // ══════════════════════════════════════════════════════════

    private int currentWave = 0;
    private int aliveZombiesWave = 0;          // zombis de oleada vivos
    private int aliveZombiesInfinite = 0;      // zombis de zona infinita vivos
    private Transform playerTransform;

    private List<ZombieAI> activeZombies = new List<ZombieAI>();
    private Dictionary<SpawnZone, List<ZombieSpawnPoint>> pointsByZone = new Dictionary<SpawnZone, List<ZombieSpawnPoint>>();

    private SpawnZone currentPlayerZone;
    private bool playerInInfiniteZone = false;
    private Coroutine infiniteSpawnCoroutine;

    // ─── Propiedades ──────────────────────────────────────────
    public int CurrentWave => currentWave;
    public SpawnZone CurrentPlayerZone => currentPlayerZone;
    public Transform PlayerTransform => playerTransform;

    // ══════════════════════════════════════════════════════════
    //  UNITY LIFECYCLE
    // ══════════════════════════════════════════════════════════

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[ZombieSpawner] Ya existe otra instancia. Destruyendo esta.");
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        // Auto-detectar spawn points
        if (spawnPoints.Count == 0)
            spawnPoints.AddRange(FindObjectsOfType<ZombieSpawnPoint>());

        if (spawnPoints.Count == 0)
        {
            Debug.LogError("[ZombieSpawner] ¡No hay ningún ZombieSpawnPoint en la escena!");
            return;
        }

        // Clasificar por zona
        BuildZoneDictionary();

        // Jugador
        FindPlayer();

        // Detectar zona inicial
        currentPlayerZone = DetectPlayerZone();
        Debug.Log($"[ZombieSpawner] Zona inicial del jugador: {currentPlayerZone}");

        // UI
        if (createWaveTextIfMissing && waveText == null)
            TryCreateWaveText();

        if (zombiePrefab == null)
        {
            Debug.LogError("[ZombieSpawner] No hay zombiePrefab asignado.");
            return;
        }

        StartCoroutine(SpawnLoop());
        StartCoroutine(RelocateLoop());
        StartCoroutine(ZoneCheckLoop());
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // ══════════════════════════════════════════════════════════
    //  CLASIFICAR SPAWN POINTS POR ZONA
    // ══════════════════════════════════════════════════════════

    void BuildZoneDictionary()
    {
        pointsByZone.Clear();
        foreach (SpawnZone z in System.Enum.GetValues(typeof(SpawnZone)))
            pointsByZone[z] = new List<ZombieSpawnPoint>();

        foreach (var p in spawnPoints)
        {
            if (p != null)
                pointsByZone[p.zone].Add(p);
        }

        foreach (var kv in pointsByZone)
            Debug.Log($"[ZombieSpawner] Zona {kv.Key}: {kv.Value.Count} puntos de spawn");
    }

    // ══════════════════════════════════════════════════════════
    //  DETECCIÓN DE ZONA DEL JUGADOR
    // ══════════════════════════════════════════════════════════

    /// <summary>
    /// Comprueba continuamente en qué zona está el jugador.
    /// Si cambia de zona, gestiona la transición (parar/arrancar infinitos, etc.).
    /// </summary>
    IEnumerator ZoneCheckLoop()
    {
        yield return new WaitForSeconds(0.5f);

        while (true)
        {
            yield return new WaitForSeconds(1f);

            if (playerTransform == null)
            {
                FindPlayer();
                continue;
            }

            SpawnZone newZone = DetectPlayerZone();
            if (newZone != currentPlayerZone)
            {
                OnPlayerChangedZone(currentPlayerZone, newZone);
                currentPlayerZone = newZone;
            }
        }
    }

    /// <summary>
    /// Detecta la zona actual del jugador basándose en qué SpawnPoint
    /// tiene al jugador dentro de su rango de activación (el más cercano gana).
    /// </summary>
    SpawnZone DetectPlayerZone()
    {
        if (playerTransform == null) return currentPlayerZone;

        Vector3 pos = playerTransform.position;
        ZombieSpawnPoint closest = null;
        float minDist = float.MaxValue;

        foreach (var point in spawnPoints)
        {
            if (point == null) continue;
            if (!point.IsPlayerInRange(pos)) continue;

            float d = point.DistanceTo(pos);
            if (d < minDist)
            {
                minDist = d;
                closest = point;
            }
        }

        // Si está en rango de alguno, usa su zona
        if (closest != null) return closest.zone;

        // Si no está en rango de ninguno, devolver la zona del punto más cercano
        closest = GetClosestSpawnPoint(pos);
        return closest != null ? closest.zone : currentPlayerZone;
    }

    /// <summary>
    /// Callback cuando el jugador cambia de zona.
    /// </summary>
    void OnPlayerChangedZone(SpawnZone oldZone, SpawnZone newZone)
    {
        Debug.Log($"[ZombieSpawner] Jugador cambió de zona: {oldZone} → {newZone}");

        // ── Gestionar zona infinita ──
        bool wasInfinite = (oldZone == SpawnZone.Zona3_Infinitos);
        bool isInfinite  = (newZone == SpawnZone.Zona3_Infinitos);

        if (isInfinite && !wasInfinite)
        {
            // Entró en zona infinita: arrancar spawn continuo
            playerInInfiniteZone = true;
            if (infiniteSpawnCoroutine == null)
                infiniteSpawnCoroutine = StartCoroutine(InfiniteSpawnLoop());
        }
        else if (!isInfinite && wasInfinite)
        {
            // Salió de zona infinita: parar spawn y destruir zombis infinitos
            playerInInfiniteZone = false;
            if (infiniteSpawnCoroutine != null)
            {
                StopCoroutine(infiniteSpawnCoroutine);
                infiniteSpawnCoroutine = null;
            }
            DestroyInfiniteZombies();
        }

        // Los zombis de oleada lejanos se reubicarán solos en el RelocateLoop
    }

    // ══════════════════════════════════════════════════════════
    //  SPAWN LOOP — OLEADAS (zonas normales)
    // ══════════════════════════════════════════════════════════

    IEnumerator SpawnLoop()
    {
        while (true)
        {
            currentWave++;
            int zombiesThisWave = baseZombies + currentWave;
            UpdateWaveUI();

            for (int i = 0; i < zombiesThisWave; i++)
            {
                SpawnWaveZombie();
                yield return new WaitForSeconds(timeBetweenSpawns);
            }

            // Esperar a que mueran todos los de oleada
            while (aliveZombiesWave > 0)
                yield return null;

            yield return new WaitForSeconds(timeBetweenWaves);
        }
    }

    // ══════════════════════════════════════════════════════════
    //  SPAWN LOOP — ZONA INFINITA
    // ══════════════════════════════════════════════════════════

    IEnumerator InfiniteSpawnLoop()
    {
        Debug.Log("[ZombieSpawner] ¡Zona infinita activada! Spawneando sin parar...");

        while (playerInInfiniteZone)
        {
            if (aliveZombiesInfinite < infiniteZoneMaxAlive)
            {
                SpawnInfiniteZombie();
            }
            yield return new WaitForSeconds(infiniteSpawnInterval);
        }
    }

    // ══════════════════════════════════════════════════════════
    //  RELOCATE LOOP
    // ══════════════════════════════════════════════════════════

    IEnumerator RelocateLoop()
    {
        yield return new WaitForSeconds(1f);

        while (true)
        {
            yield return new WaitForSeconds(relocateCheckInterval);

            if (playerTransform == null) { FindPlayer(); continue; }

            Vector3 playerPos = playerTransform.position;

            // Buscar el spawn point más cercano al jugador de su zona actual (no infinita)
            ZombieSpawnPoint target = GetClosestSpawnPointInZone(playerPos, currentPlayerZone);
            if (target == null) target = GetClosestSpawnPoint(playerPos);
            if (target == null) continue;

            for (int i = activeZombies.Count - 1; i >= 0; i--)
            {
                ZombieAI zombie = activeZombies[i];
                if (zombie == null) { activeZombies.RemoveAt(i); continue; }

                float dist = Vector3.Distance(zombie.transform.position, playerPos);
                if (dist > relocateDistance)
                {
                    RelocateZombie(zombie, target);
                }
            }
        }
    }

    // ══════════════════════════════════════════════════════════
    //  SPAWN — OLEADA
    // ══════════════════════════════════════════════════════════

    void SpawnWaveZombie()
    {
        if (playerTransform == null) FindPlayer();

        // Elegir punto en la zona actual del jugador (excluyendo infinita)
        SpawnZone targetZone = currentPlayerZone;
        if (targetZone == SpawnZone.Zona3_Infinitos)
        {
            // Si el jugador está en la zona infinita, spawnear oleada en la zona normal más cercana
            targetZone = GetClosestNonInfiniteZone();
        }

        ZombieSpawnPoint point = (playerTransform != null)
            ? GetClosestSpawnPointInZone(playerTransform.position, targetZone)
            : GetRandomPointInZone(targetZone);

        if (point == null) point = GetClosestSpawnPoint(playerTransform != null ? playerTransform.position : transform.position);
        if (point == null) return;

        GameObject zombie = SpawnZombieAt(point);
        if (zombie == null) return;

        aliveZombiesWave++;
        ConfigureWaveZombie(zombie);
    }

    // ══════════════════════════════════════════════════════════
    //  SPAWN — INFINITO
    // ══════════════════════════════════════════════════════════

    void SpawnInfiniteZombie()
    {
        if (playerTransform == null) FindPlayer();

        List<ZombieSpawnPoint> infPoints = pointsByZone[SpawnZone.Zona3_Infinitos];
        if (infPoints.Count == 0) return;

        ZombieSpawnPoint point = (playerTransform != null)
            ? GetClosestInList(playerTransform.position, infPoints)
            : infPoints[Random.Range(0, infPoints.Count)];

        GameObject zombie = SpawnZombieAt(point);
        if (zombie == null) return;

        aliveZombiesInfinite++;

        // Configurar zombi infinito con stats fijos
        ZombieWaveMember member = zombie.GetComponent<ZombieWaveMember>();
        if (member == null) member = zombie.AddComponent<ZombieWaveMember>();
        member.spawner = this;
        member.isInfiniteZombie = true;

        EnemyHealth eh = zombie.GetComponent<EnemyHealth>();
        if (eh != null)
        {
            eh.useRandomHealth = false;
            eh.maxHealth = infiniteZoneHealth;
            eh.currentHealth = eh.maxHealth;
        }

        ZombieAI ai = zombie.GetComponent<ZombieAI>();
        if (ai != null)
        {
            ai.damage = infiniteZoneDamage;
            activeZombies.Add(ai);
        }
    }

    // ══════════════════════════════════════════════════════════
    //  SPAWN GENÉRICO
    // ══════════════════════════════════════════════════════════

    GameObject SpawnZombieAt(ZombieSpawnPoint point)
    {
        if (point == null || zombiePrefab == null) return null;

        Vector3 pos = point.GetRandomSpawnPosition();
        Quaternion rot = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
        return Instantiate(zombiePrefab, pos, rot);
    }

    void ConfigureWaveZombie(GameObject zombie)
    {
        ZombieWaveMember member = zombie.GetComponent<ZombieWaveMember>();
        if (member == null) member = zombie.AddComponent<ZombieWaveMember>();
        member.spawner = this;
        member.isInfiniteZombie = false;

        EnemyHealth eh = zombie.GetComponent<EnemyHealth>();
        if (eh != null)
        {
            eh.useRandomHealth = false;
            eh.maxHealth = baseHealth + (currentWave - 1) * healthPerWave;
            eh.currentHealth = eh.maxHealth;
        }

        ZombieAI ai = zombie.GetComponent<ZombieAI>();
        if (ai != null)
        {
            int steps = damageIncreaseEveryWaves > 0 ? currentWave / damageIncreaseEveryWaves : 0;
            ai.damage = baseDamage + steps * damageBonusPerStep;
            activeZombies.Add(ai);
        }
    }

    // ══════════════════════════════════════════════════════════
    //  DESTRUIR ZOMBIS INFINITOS (al salir de la zona)
    // ══════════════════════════════════════════════════════════

    void DestroyInfiniteZombies()
    {
        Debug.Log("[ZombieSpawner] Saliendo de zona infinita — eliminando zombis infinitos.");

        for (int i = activeZombies.Count - 1; i >= 0; i--)
        {
            ZombieAI zombie = activeZombies[i];
            if (zombie == null) { activeZombies.RemoveAt(i); continue; }

            ZombieWaveMember member = zombie.GetComponent<ZombieWaveMember>();
            if (member != null && member.isInfiniteZombie)
            {
                activeZombies.RemoveAt(i);
                Destroy(zombie.gameObject);
            }
        }
        aliveZombiesInfinite = 0;
    }

    // ══════════════════════════════════════════════════════════
    //  REUBICACIÓN
    // ══════════════════════════════════════════════════════════

    public void RelocateZombie(ZombieAI zombie, ZombieSpawnPoint targetPoint)
    {
        if (zombie == null || targetPoint == null) return;

        Vector3 newPos = targetPoint.GetRandomSpawnPosition();

        UnityEngine.AI.NavMeshAgent agent = zombie.GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (agent != null && agent.isOnNavMesh)
            agent.Warp(newPos);
        else
            zombie.transform.position = newPos;

        zombie.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
        zombie.ResetDetection();

        Debug.Log($"[ZombieSpawner] Zombi '{zombie.name}' reubicado → '{targetPoint.name}' ({targetPoint.zone})");
    }

    // ══════════════════════════════════════════════════════════
    //  UTILIDADES — BÚSQUEDA DE SPAWN POINTS
    // ══════════════════════════════════════════════════════════

    public ZombieSpawnPoint GetClosestSpawnPoint(Vector3 position)
    {
        return GetClosestInList(position, spawnPoints);
    }

    public ZombieSpawnPoint GetClosestSpawnPointInZone(Vector3 position, SpawnZone zone)
    {
        if (!pointsByZone.ContainsKey(zone) || pointsByZone[zone].Count == 0) return null;
        return GetClosestInList(position, pointsByZone[zone]);
    }

    ZombieSpawnPoint GetRandomPointInZone(SpawnZone zone)
    {
        if (!pointsByZone.ContainsKey(zone) || pointsByZone[zone].Count == 0) return null;
        var list = pointsByZone[zone];
        return list[Random.Range(0, list.Count)];
    }

    ZombieSpawnPoint GetClosestInList(Vector3 position, List<ZombieSpawnPoint> list)
    {
        ZombieSpawnPoint closest = null;
        float minDist = float.MaxValue;
        foreach (var p in list)
        {
            if (p == null) continue;
            float d = p.DistanceTo(position);
            if (d < minDist) { minDist = d; closest = p; }
        }
        return closest;
    }

    /// <summary>
    /// Devuelve la zona normal más cercana al jugador (excluyendo Zona3_Infinitos).
    /// </summary>
    SpawnZone GetClosestNonInfiniteZone()
    {
        if (playerTransform == null) return SpawnZone.Zona1;

        Vector3 pos = playerTransform.position;
        ZombieSpawnPoint best = null;
        float minDist = float.MaxValue;

        foreach (var p in spawnPoints)
        {
            if (p == null || p.IsInfiniteZone) continue;
            float d = p.DistanceTo(pos);
            if (d < minDist) { minDist = d; best = p; }
        }

        return best != null ? best.zone : SpawnZone.Zona1;
    }

    /// <summary>
    /// Devuelve el SpawnPoint más cercano al jugador dentro de su rango de activación.
    /// </summary>
    public ZombieSpawnPoint GetActiveSpawnPointNearPlayer()
    {
        if (playerTransform == null)
        {
            FindPlayer();
            if (playerTransform == null) return spawnPoints.Count > 0 ? spawnPoints[0] : null;
        }

        Vector3 playerPos = playerTransform.position;
        ZombieSpawnPoint bestInRange = null;
        ZombieSpawnPoint closestOverall = null;
        float minDistInRange = float.MaxValue;
        float minDistOverall = float.MaxValue;

        foreach (var point in spawnPoints)
        {
            if (point == null) continue;
            float dist = point.DistanceTo(playerPos);

            if (dist < minDistOverall)
            {
                minDistOverall = dist;
                closestOverall = point;
            }

            if (point.IsPlayerInRange(playerPos) && dist < minDistInRange)
            {
                minDistInRange = dist;
                bestInRange = point;
            }
        }

        return bestInRange != null ? bestInRange : closestOverall;
    }

    // ══════════════════════════════════════════════════════════
    //  NOTIFICACIONES
    // ══════════════════════════════════════════════════════════

    public void NotifyZombieDestroyed(bool isInfinite)
    {
        if (isInfinite)
            aliveZombiesInfinite = Mathf.Max(0, aliveZombiesInfinite - 1);
        else
            aliveZombiesWave = Mathf.Max(0, aliveZombiesWave - 1);
    }

    /// <summary>Retrocompatibilidad — asume zombi de oleada.</summary>
    public void NotifyZombieDestroyed()
    {
        aliveZombiesWave = Mathf.Max(0, aliveZombiesWave - 1);
    }

    public void UnregisterZombie(ZombieAI zombie)
    {
        activeZombies.Remove(zombie);
    }

    // ══════════════════════════════════════════════════════════
    //  JUGADOR
    // ══════════════════════════════════════════════════════════

    void FindPlayer()
    {
        GameObject obj = GameObject.FindGameObjectWithTag("Player");
        if (obj != null) playerTransform = obj.transform;
    }

    // ══════════════════════════════════════════════════════════
    //  UI
    // ══════════════════════════════════════════════════════════

    void UpdateWaveUI()
    {
        if (waveText != null)
            waveText.text = wavePrefix + currentWave;
    }

    void TryCreateWaveText()
    {
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            Debug.LogWarning("[ZombieSpawner] No hay Canvas en la escena para crear el texto.");
            return;
        }

        GameObject textObj = new GameObject("WaveText");
        textObj.transform.SetParent(canvas.transform, false);

        waveText = textObj.AddComponent<TextMeshProUGUI>();
        RectTransform rect = waveText.rectTransform;
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = waveTextAnchoredPos;
        rect.sizeDelta = waveTextSize;

        waveText.fontSize = waveTextFontSize;
        waveText.color = waveTextColor;
        waveText.alignment = TextAlignmentOptions.TopLeft;
        waveText.text = wavePrefix + "0";
    }
}
