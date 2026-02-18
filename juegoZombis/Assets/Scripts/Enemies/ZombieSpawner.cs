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

    // ── Zona 3 — Sistema de oleadas especial ──────────────────
    [Header("Zona 3 — Oleadas con Zombi Especial")]
    [Tooltip("Máximo de zombis vivos a la vez en Zona 3")]
    public int zone3MaxZombies = 30;
    [Tooltip("Cuando queden este nº de zombis, se rellena hasta el máximo")]
    public int zone3RefillThreshold = 15;
    [Tooltip("Cada cuántos rellenos aparece el zombi especial")]
    public int zone3CyclesForSpecial = 3;
    [Tooltip("Tiempo entre spawns individuales en Zona 3")]
    public float zone3SpawnInterval = 0.3f;

    [Header("Zona 3 — Stats de zombis normales")]
    public float zone3BaseHealth = 150f;
    public float zone3BaseDamage = 25f;
    [Tooltip("Vida de los zombis DESPUÉS de que aparezca el especial")]
    public float zone3BoostedHealth = 300f;
    [Tooltip("Daño de los zombis DESPUÉS de que aparezca el especial")]
    public float zone3BoostedDamage = 35f;

    [Header("Zona 3 — Zombi Especial")]
    [Tooltip("Prefab del zombi especial (si vacío usa el zombiePrefab normal pero más grande)")]
    public GameObject zone3SpecialPrefab;
    [Tooltip("Vida del zombi especial")]
    public float zone3SpecialHealth = 1500f;
    [Tooltip("Daño del zombi especial")]
    public float zone3SpecialDamage = 50f;
    [Tooltip("Escala del zombi especial (multiplicador)")]
    public float zone3SpecialScale = 2f;
    [Tooltip("Color emisivo del zombi especial para distinguirlo")]
    public Color zone3SpecialColor = new Color(1f, 0.2f, 0.2f, 1f);

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
    private int aliveZombiesZone3 = 0;         // zombis de zona 3 vivos
    private int zone3RefillCount = 0;          // nº de veces que se ha rellenado
    private bool zone3SpecialAlive = false;    // ¿hay un especial vivo ahora?
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

        // ── Gestionar zona 3 ──
        bool wasInfinite = (oldZone == SpawnZone.Zona3_Infinitos);
        bool isInfinite  = (newZone == SpawnZone.Zona3_Infinitos);

        if (isInfinite && !wasInfinite)
        {
            // Entró en Zona 3: DESTRUIR TODOS los zombis del mapa
            playerInInfiniteZone = true;
            DestroyAllZombies();

            // Resetear estado de Zona 3
            zone3RefillCount = 0;
            zone3SpecialAlive = false;

            if (infiniteSpawnCoroutine == null)
                infiniteSpawnCoroutine = StartCoroutine(Zone3WaveLoop());
        }
        else if (!isInfinite && wasInfinite)
        {
            // Salió de Zona 3: parar spawn y destruir zombis de zona 3
            playerInInfiniteZone = false;
            if (infiniteSpawnCoroutine != null)
            {
                StopCoroutine(infiniteSpawnCoroutine);
                infiniteSpawnCoroutine = null;
            }
            DestroyZone3Zombies();
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
    //  SPAWN LOOP — ZONA 3 (oleadas con especial)
    // ══════════════════════════════════════════════════════════

    IEnumerator Zone3WaveLoop()
    {
        Debug.Log("[ZombieSpawner] ¡Zona 3 activada! Spawneando oleada inicial...");

        // Spawn inicial: llenar hasta zone3MaxZombies
        yield return StartCoroutine(SpawnZone3Batch(zone3MaxZombies, false));

        while (playerInInfiniteZone)
        {
            // Esperar hasta que queden 'zone3RefillThreshold' o menos
            while (aliveZombiesZone3 > zone3RefillThreshold && playerInInfiniteZone)
                yield return new WaitForSeconds(0.5f);

            if (!playerInInfiniteZone) break;

            zone3RefillCount++;
            Debug.Log($"[ZombieSpawner] Zona 3 — Relleno #{zone3RefillCount}");

            // ¿Toca zombi especial?
            bool spawnSpecial = (zone3RefillCount % zone3CyclesForSpecial == 0);

            if (spawnSpecial)
            {
                Debug.Log("[ZombieSpawner] ¡¡ZOMBI ESPECIAL!! Boosteando zombis existentes.");
                // Boostear TODOS los zombis vivos de zona 3
                BoostZone3Zombies();
                // Spawnear el especial
                SpawnZone3Special();
            }

            // Rellenar hasta el máximo
            int toSpawn = zone3MaxZombies - aliveZombiesZone3;
            if (toSpawn > 0)
                yield return StartCoroutine(SpawnZone3Batch(toSpawn, spawnSpecial));
        }
    }

    /// <summary>
    /// Spawnea un lote de zombis de zona 3 uno a uno con intervalo.
    /// </summary>
    IEnumerator SpawnZone3Batch(int count, bool boosted)
    {
        for (int i = 0; i < count; i++)
        {
            if (!playerInInfiniteZone) break;
            SpawnZone3Zombie(boosted);
            yield return new WaitForSeconds(zone3SpawnInterval);
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
    //  SPAWN — ZONA 3 (zombi normal)
    // ══════════════════════════════════════════════════════════

    void SpawnZone3Zombie(bool boosted)
    {
        if (playerTransform == null) FindPlayer();

        List<ZombieSpawnPoint> z3Points = pointsByZone[SpawnZone.Zona3_Infinitos];
        if (z3Points.Count == 0) return;

        ZombieSpawnPoint point = (playerTransform != null)
            ? GetClosestInList(playerTransform.position, z3Points)
            : z3Points[Random.Range(0, z3Points.Count)];

        GameObject zombie = SpawnZombieAt(point);
        if (zombie == null) return;

        aliveZombiesZone3++;

        // Configurar zombi de zona 3
        ZombieWaveMember member = zombie.GetComponent<ZombieWaveMember>();
        if (member == null) member = zombie.AddComponent<ZombieWaveMember>();
        member.spawner = this;
        member.isInfiniteZombie = true; // usa el flag para identificar zombis de zona 3

        float hp  = boosted ? zone3BoostedHealth : zone3BaseHealth;
        float dmg = boosted ? zone3BoostedDamage  : zone3BaseDamage;

        EnemyHealth eh = zombie.GetComponent<EnemyHealth>();
        if (eh != null)
        {
            eh.useRandomHealth = false;
            eh.maxHealth = hp;
            eh.currentHealth = eh.maxHealth;
        }

        ZombieAI ai = zombie.GetComponent<ZombieAI>();
        if (ai != null)
        {
            ai.damage = dmg;
            activeZombies.Add(ai);
        }
    }

    // ══════════════════════════════════════════════════════════
    //  SPAWN — ZONA 3 (zombi ESPECIAL)
    // ══════════════════════════════════════════════════════════

    void SpawnZone3Special()
    {
        if (playerTransform == null) FindPlayer();

        List<ZombieSpawnPoint> z3Points = pointsByZone[SpawnZone.Zona3_Infinitos];
        if (z3Points.Count == 0) return;

        ZombieSpawnPoint point = (playerTransform != null)
            ? GetClosestInList(playerTransform.position, z3Points)
            : z3Points[Random.Range(0, z3Points.Count)];

        // Usar prefab especial si existe, si no el normal
        GameObject prefab = zone3SpecialPrefab != null ? zone3SpecialPrefab : zombiePrefab;
        if (prefab == null) return;

        Vector3 pos = point.GetRandomSpawnPosition();
        Quaternion rot = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
        GameObject zombie = Instantiate(prefab, pos, rot);
        if (zombie == null) return;

        aliveZombiesZone3++;

        // Escala gigante
        zombie.transform.localScale = Vector3.one * zone3SpecialScale;

        // Color distintivo
        foreach (Renderer rend in zombie.GetComponentsInChildren<Renderer>())
        {
            foreach (Material mat in rend.materials)
            {
                mat.color = zone3SpecialColor;
                if (mat.HasProperty("_EmissionColor"))
                {
                    mat.EnableKeyword("_EMISSION");
                    mat.SetColor("_EmissionColor", zone3SpecialColor * 0.3f);
                }
            }
        }

        // Configurar como miembro de zona 3
        ZombieWaveMember member = zombie.GetComponent<ZombieWaveMember>();
        if (member == null) member = zombie.AddComponent<ZombieWaveMember>();
        member.spawner = this;
        member.isInfiniteZombie = true;

        EnemyHealth eh = zombie.GetComponent<EnemyHealth>();
        if (eh != null)
        {
            eh.useRandomHealth = false;
            eh.maxHealth = zone3SpecialHealth;
            eh.currentHealth = eh.maxHealth;
        }

        ZombieAI ai = zombie.GetComponent<ZombieAI>();
        if (ai != null)
        {
            ai.damage = zone3SpecialDamage;
            activeZombies.Add(ai);
        }

        zone3SpecialAlive = true;
        Debug.Log($"[ZombieSpawner] ¡ZOMBI ESPECIAL spawneado! Vida={zone3SpecialHealth}, Escala={zone3SpecialScale}");
    }

    // ══════════════════════════════════════════════════════════
    //  BOOST — Subir vida a zombis existentes de Zona 3
    // ══════════════════════════════════════════════════════════

    void BoostZone3Zombies()
    {
        int boosted = 0;
        foreach (ZombieAI zombie in activeZombies)
        {
            if (zombie == null) continue;
            ZombieWaveMember member = zombie.GetComponent<ZombieWaveMember>();
            if (member == null || !member.isInfiniteZombie) continue;

            EnemyHealth eh = zombie.GetComponent<EnemyHealth>();
            if (eh != null)
            {
                // Solo boostear si no es el especial (que ya tiene mucha vida)
                if (eh.maxHealth < zone3SpecialHealth)
                {
                    float oldMax = eh.maxHealth;
                    eh.maxHealth = zone3BoostedHealth;
                    // Curar proporcionalmente
                    float ratio = eh.currentHealth / Mathf.Max(oldMax, 1f);
                    eh.currentHealth = eh.maxHealth * ratio;
                }
            }

            zombie.damage = zone3BoostedDamage;
            boosted++;
        }
        Debug.Log($"[ZombieSpawner] {boosted} zombis boosteados a {zone3BoostedHealth} HP / {zone3BoostedDamage} DMG");
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
    //  DESTRUIR ZOMBIS — ZONA 3 (al salir)
    // ══════════════════════════════════════════════════════════

    void DestroyZone3Zombies()
    {
        Debug.Log("[ZombieSpawner] Saliendo de Zona 3 — eliminando zombis de zona 3.");

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
        aliveZombiesZone3 = 0;
        zone3SpecialAlive = false;
    }

    // ══════════════════════════════════════════════════════════
    //  DESTRUIR TODOS LOS ZOMBIS (al entrar en zona 3)
    // ══════════════════════════════════════════════════════════

    void DestroyAllZombies()
    {
        Debug.Log("[ZombieSpawner] Entrando en Zona 3 — ELIMINANDO TODOS los zombis del mapa.");

        for (int i = activeZombies.Count - 1; i >= 0; i--)
        {
            ZombieAI zombie = activeZombies[i];
            if (zombie == null) { activeZombies.RemoveAt(i); continue; }
            Destroy(zombie.gameObject);
        }
        activeZombies.Clear();
        aliveZombiesWave = 0;
        aliveZombiesZone3 = 0;
        zone3SpecialAlive = false;
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
            aliveZombiesZone3 = Mathf.Max(0, aliveZombiesZone3 - 1);
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
