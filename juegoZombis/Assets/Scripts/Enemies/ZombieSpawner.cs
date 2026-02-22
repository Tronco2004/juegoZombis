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

    // ── Anti-spawn en FOV ─────────────────────────────────────
    [Header("Spawn — No spawnear en visión del jugador")]
    [Tooltip("Si está activado, los zombis no aparecen dentro del campo de visión de la cámara")]
    public bool preventSpawnInView = true;
    [Tooltip("Intentos de buscar una posición fuera del FOV antes de spawnear igualmente")]
    public int spawnViewRetries = 8;
    [Tooltip("Margen extra sobre los bordes del viewport (0 = exactamente el FOV, 0.05 = 5% más)")]
    [Range(0f, 0.3f)]
    public float viewportMargin = 0.05f;

    [Tooltip("Distancia máxima en metros entre el punto de spawn y el jugador. Spawns más lejos se cancelan para evitar zombis dispersos.")]
    public float maxSpawnDistanceToPlayer = 200f;

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
    private int _waveSpawnIndex = 0;           // reservado (ya no se usa, sustituido por _spawnQueue)
    private Queue<ZombieSpawnPoint> _spawnQueue = new Queue<ZombieSpawnPoint>();
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

        // Zona inicial: la zona del punto de spawn más cercano al jugador.
        // Los ZombieActivationZone actualizarán esto en runtime via triggers.
        currentPlayerZone = GetFallbackZone();
        Debug.Log($"[ZombieSpawner] Zona inicial del jugador: {currentPlayerZone}");

        // UI
        if (createWaveTextIfMissing && waveText == null)
            TryCreateWaveText();

        if (zombiePrefab == null)
        {
            Debug.LogError("[ZombieSpawner] No hay zombiePrefab asignado.");
            return;
        }

        StartCoroutine(SpawnLoop()); // se pausa solo cuando playerInInfiniteZone=true
        StartCoroutine(RelocateLoop());
        StartCoroutine(ZoneCheckLoop());

        // Si el jugador ya empieza en Zona 3, activarla directamente
        if (currentPlayerZone == SpawnZone.Zona3_Infinitos)
        {
            playerInInfiniteZone = true;
            zone3RefillCount = 0;
            zone3SpecialAlive = false;
            infiniteSpawnCoroutine = StartCoroutine(Zone3WaveLoop());
            Debug.Log("[ZombieSpawner] Jugador empieza en Zona 3 — activando spawn infinito.");
        }
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
    // ─── Sistema de zonas por trigger ────────────────────────────────────────

    /// <summary>
    /// Llamado por ZombieActivationZone cuando el jugador entra en un trigger.
    /// Cambia la zona activa del spawner.
    /// </summary>
    public void NotifyZoneEntered(SpawnZone zone)
    {
        if (zone == currentPlayerZone) return;
        Debug.Log($"[ZombieSpawner] NotifyZoneEntered: {currentPlayerZone} → {zone}");
        OnPlayerChangedZone(currentPlayerZone, zone);
        currentPlayerZone = zone;
    }

    /// <summary>
    /// Llamado por ZombieActivationZone cuando el jugador sale de un trigger.
    /// Vuelve a la zona del spawn point físicamente más cercano como fallback.
    /// </summary>
    public void NotifyZoneExited(SpawnZone zone)
    {
        // Solo actuamos si salimos de la zona actualmente activa
        if (zone != currentPlayerZone) return;

        // Fallback: zona del punto de spawn más cercano al jugador
        SpawnZone fallback = GetFallbackZone();
        Debug.Log($"[ZombieSpawner] NotifyZoneExited: {zone} → fallback {fallback}");
        OnPlayerChangedZone(currentPlayerZone, fallback);
        currentPlayerZone = fallback;
    }

    /// <summary>
    /// Devuelve la zona del punto de spawn más cercano al jugador (usado como fallback).
    /// </summary>
    SpawnZone GetFallbackZone()
    {
        if (playerTransform == null) return SpawnZone.Zona1A;

        Vector3 pos = playerTransform.position;
        ZombieSpawnPoint closest = GetClosestSpawnPoint(pos);
        return closest != null ? closest.zone : SpawnZone.Zona1A;
    }

    // ─── Loop de comprobación (solo para mantener playerTransform válido) ─────

    IEnumerator ZoneCheckLoop()
    {
        yield return new WaitForSeconds(0.5f);

        while (true)
        {
            yield return new WaitForSeconds(2f);

            // Únicamente nos aseguramos de tener la referencia al jugador
            if (playerTransform == null)
            {
                FindPlayer();
            }
        }
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

            if (infiniteSpawnCoroutine != null)
            {
                StopCoroutine(infiniteSpawnCoroutine);
                infiniteSpawnCoroutine = null;
            }
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
            // Pausar mientras el jugador esté en Zona 3
            while (playerInInfiniteZone)
                yield return new WaitForSeconds(1f);

            // Esperar a que haya al menos un spawn point activo antes de iniciar la oleada
            bool hasActivePoints = false;
            while (!hasActivePoints)
            {
                foreach (var p in spawnPoints)
                {
                    if (p != null && p.isActive) { hasActivePoints = true; break; }
                }
                if (!hasActivePoints)
                    yield return new WaitForSeconds(1f);
            }

            currentWave++;
            _waveSpawnIndex = 0;
            aliveZombiesWave = 0;  // limpiar contador por si hay residuo de la oleada anterior
            int zombiesThisWave = baseZombies + currentWave;
            UpdateWaveUI();

            // Construir cola barajada con distribución uniforme entre spawn points
            BuildSpawnQueue(zombiesThisWave);

            Debug.Log($"[ZombieSpawner] === OLEADA {currentWave} === Zombis: {zombiesThisWave}, Puntos en cola: {_spawnQueue.Count}");

            // Spawnear exactamente zombiesThisWave zombis.
            // Si un intento falla, NO avanzamos el contador — reintentamos en el siguiente tick.
            int spawned = 0;
            while (spawned < zombiesThisWave)
            {
                if (playerInInfiniteZone) break;

                // Esperar si no hay puntos activos
                while (!playerInInfiniteZone && !HasActiveSpawnPoints())
                    yield return new WaitForSeconds(1f);

                if (playerInInfiniteZone) break;

                int antesDeSpawn = aliveZombiesWave;
                SpawnWaveZombie();

                // Solo avanzar el contador si realmente se creó un zombi
                if (aliveZombiesWave > antesDeSpawn)
                {
                    spawned++;
                    Debug.Log($"[ZombieSpawner] Spawneado {spawned}/{zombiesThisWave}");
                    yield return new WaitForSeconds(timeBetweenSpawns);
                }
                else
                {
                    // Spawn fallido — esperar un poco antes de reintentar
                    yield return new WaitForSeconds(0.5f);
                }
            }

            Debug.Log($"[ZombieSpawner] Oleada {currentWave} completada. Esperando que mueran {aliveZombiesWave} zombis...");

            // Esperar a que mueran todos
            while (aliveZombiesWave > 0)
                yield return null;

            Debug.Log($"[ZombieSpawner] Oleada {currentWave} limpia. Siguiente en {timeBetweenWaves}s.");
            yield return new WaitForSeconds(timeBetweenWaves);
        }
    }

    bool HasActiveSpawnPoints()
    {
        foreach (var p in spawnPoints)
            if (p != null && p.isActive) return true;
        return false;
    }

    // ══════════════════════════════════════════════════════════
    //  SPAWN LOOP — ZONA 3 (oleadas con especial)
    // ══════════════════════════════════════════════════════════

    IEnumerator Zone3WaveLoop()
    {
        Debug.Log($"[ZombieSpawner] ¡Zona 3 activada! Spawneando {zone3MaxZombies} zombis iniciales...");

        // Spawn inicial: llenar hasta zone3MaxZombies
        yield return StartCoroutine(SpawnZone3Batch(zone3MaxZombies, false));

        Debug.Log($"[ZombieSpawner] Zona 3 — Batch inicial completado. Vivos: {aliveZombiesZone3}. Esperando que bajen a {zone3RefillThreshold}...");

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

            for (int i = activeZombies.Count - 1; i >= 0; i--)
            {
                ZombieAI zombie = activeZombies[i];
                if (zombie == null) { activeZombies.RemoveAt(i); continue; }

                // Los zombis de mansión son independientes: nunca los reubicamos
                if (zombie.isMansionZombie) continue;

                float dist = Vector3.Distance(zombie.transform.position, playerPos);
                if (dist > relocateDistance)
                {
                    // Reubicar al spawn point más cercano de SU PROPIA zona
                    // activeOnly=false: puede ir a un punto inactivo, solo espera allí
                    ZombieSpawnPoint target = GetClosestInList(zombie.transform.position,
                        pointsByZone.ContainsKey(zombie.spawnZone) ? pointsByZone[zombie.spawnZone] : spawnPoints,
                        activeOnly: false);
                    if (target == null) target = GetClosestInList(zombie.transform.position, spawnPoints, false);
                    if (target == null) continue;
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
        // Si la cola está vacía, reconstruir con 1 entry (puede pasar si cambió la zona a mitad de oleada)
        if (_spawnQueue.Count == 0)
        {
            BuildSpawnQueue(1);
            if (_spawnQueue.Count == 0)
            {
                Debug.LogWarning("[ZombieSpawner] SpawnWaveZombie: cola vacía y sin puntos activos.");
                return;
            }
        }

        ZombieSpawnPoint point = _spawnQueue.Dequeue();

        // Verificar que el punto sigue activo (puede haberse desactivado mientras esperaba)
        if (point == null || !point.isActive)
        {
            ZombieSpawnPoint fallback = spawnPoints.Find(p => p != null && p.isActive);
            if (fallback == null)
            {
                Debug.LogWarning("[ZombieSpawner] SpawnWaveZombie: punto inactivo y sin fallback.");
                return;
            }
            point = fallback;
        }

        Debug.Log($"[ZombieSpawner] Spawneando en '{point.name}' (zona {point.zone}) | Cola restante: {_spawnQueue.Count}");

        GameObject zombie = SpawnZombieAt(point);
        if (zombie == null)
        {
            Debug.LogWarning($"[ZombieSpawner] SpawnZombieAt devolvió null para '{point.name}'.");
            return;
        }

        aliveZombiesWave++;
        ConfigureWaveZombie(zombie);
    }

    /// <summary>
    /// Construye una cola barajada de spawn points con distribución uniforme.
    /// Si hay 10 zombis y 3 puntos: cada punto recibe ~3-4 entradas, luego todo se baraja.
    /// </summary>
    void BuildSpawnQueue(int totalZombies)
    {
        _spawnQueue.Clear();

        // Zona objetivo
        SpawnZone targetZone = currentPlayerZone == SpawnZone.Zona3_Infinitos
            ? GetClosestNonInfiniteZone() : currentPlayerZone;

        // Puntos activos de la zona
        List<ZombieSpawnPoint> activePoints = null;
        if (pointsByZone.ContainsKey(targetZone))
            activePoints = pointsByZone[targetZone].FindAll(p => p != null && p.isActive);

        if (activePoints == null || activePoints.Count == 0)
            activePoints = spawnPoints.FindAll(p => p != null && p.isActive);

        if (activePoints == null || activePoints.Count == 0)
        {
            Debug.LogWarning($"[ZombieSpawner] BuildSpawnQueue: no hay puntos activos para zona {targetZone}.");
            return;
        }

        // Distribución uniforme: cada punto recibe floor(total/n) entradas;
        // el resto (total % n) se añade uno a uno a los primeros puntos
        List<ZombieSpawnPoint> flat = new List<ZombieSpawnPoint>(totalZombies);
        int perPoint  = totalZombies / activePoints.Count;
        int remainder = totalZombies % activePoints.Count;

        for (int i = 0; i < activePoints.Count; i++)
        {
            int count = perPoint + (i < remainder ? 1 : 0);
            for (int j = 0; j < count; j++)
                flat.Add(activePoints[i]);
        }

        // Fisher-Yates shuffle para orden aleatorio
        for (int i = flat.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            ZombieSpawnPoint tmp = flat[i];
            flat[i] = flat[j];
            flat[j] = tmp;
        }

        foreach (var p in flat)
            _spawnQueue.Enqueue(p);

        Debug.Log($"[ZombieSpawner] Cola construida: {_spawnQueue.Count} zombis repartidos entre {activePoints.Count} puntos activos.");
    }

    // ══════════════════════════════════════════════════════════
    //  SPAWN — ZONA 3 (zombi normal)
    // ══════════════════════════════════════════════════════════

    void SpawnZone3Zombie(bool boosted)
    {
        if (playerTransform == null) FindPlayer();

        List<ZombieSpawnPoint> z3Points = pointsByZone[SpawnZone.Zona3_Infinitos];
        if (z3Points.Count == 0)
        {
            Debug.LogError("[ZombieSpawner] ¡NO hay ZombieSpawnPoints con zona=Zona3_Infinitos! " +
                           "Crea al menos uno en la escena y asígnale la zona Zona3_Infinitos.");
            return;
        }

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

        StripLODGroups(zombie);
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
            ai.spawnZone = point.zone;
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

        // ── Anti-spawn en FOV ──────────────────────────────────
        // Solo tiene sentido si el punto tiene radio (posición variable).
        // Con spawnRadius=0 el punto es fijo: reintentar no sirve de nada.
        if (preventSpawnInView && point.spawnRadius > 0f)
        {
            for (int i = 0; i < spawnViewRetries; i++)
            {
                if (!IsVisibleToPlayer(pos)) break;
                pos = point.GetRandomSpawnPosition();
            }
        }

        // ── Distancia MÁXIMA al jugador ───────────────────────
        if (playerTransform != null &&
            Vector3.Distance(pos, playerTransform.position) > maxSpawnDistanceToPlayer)
        {
            Debug.LogWarning($"[ZombieSpawner] Spawn cancelado: punto '{point.name}' está a más de "
                + maxSpawnDistanceToPlayer + "m del jugador.");
            return null;
        }

        Quaternion rot = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
        GameObject spawnedObj = Instantiate(zombiePrefab, pos, rot);

        // Asignar zona al ZombieAI para poder reubicarlo correctamente después
        if (spawnedObj != null)
        {
            StripLODGroups(spawnedObj);
            ZombieAI spawnedAI = spawnedObj.GetComponent<ZombieAI>();
            if (spawnedAI != null) spawnedAI.spawnZone = point.zone;
        }

        return spawnedObj;
    }

    /// <summary>
    /// Devuelve true si la posición worldPos es visible para la cámara del jugador
    /// (está dentro del viewport Y no hay obstáculo entre la cámara y el punto).
    /// </summary>
    bool IsVisibleToPlayer(Vector3 worldPos)
    {
        Camera cam = Camera.main;
        if (cam == null) return false;

        // ── 1. Comprobación de viewport (¿está dentro del FOV?) ──
        Vector3 vp = cam.WorldToViewportPoint(worldPos);

        // z < 0 significa que está detrás de la cámara → no visible
        if (vp.z < 0f) return false;

        float margin = viewportMargin;
        bool inFrustum = vp.x >= -margin && vp.x <= 1f + margin &&
                         vp.y >= -margin && vp.y <= 1f + margin;

        if (!inFrustum) return false;

        // ── 2. Comprobación de niebla ──────────────────────────────
        Vector3 camPos   = cam.transform.position;
        Vector3 dir      = worldPos - camPos;
        float   distance = dir.magnitude;

        if (RenderSettings.fog)
        {
            float fogVisibilityLimit = GetFogVisibilityDistance();
            if (fogVisibilityLimit > 0f && distance >= fogVisibilityLimit)
            {
                // El punto está dentro del frustum pero oculto por la niebla → seguro spawnear
                return false;
            }
        }

        // ── 3. Raycast de oclusión (¿hay algo tapándolo?) ──────────
        // Si un objeto sólido (que no sea el propio spawner) se interpone, no es visible
        if (Physics.Raycast(camPos, dir.normalized, out RaycastHit hit, distance))
        {
            // hay obstáculo entre la cámara y el punto → posición oculta → seguro spawnear
            return false;
        }

        // Está dentro del frustum, sin niebla suficiente y sin obstáculos → VISIBLE
        return true;
    }

    /// <summary>
    /// Devuelve la distancia a partir de la cual la niebla hace que un objeto sea
    /// prácticamente invisible (≤1% de visibilidad).
    /// Retorna -1 si la niebla está desactivada o el modo no requiere límite.
    /// </summary>
    float GetFogVisibilityDistance()
    {
        switch (RenderSettings.fogMode)
        {
            case FogMode.Linear:
                // En niebla lineal, fogEndDistance = opacidad total
                return RenderSettings.fogEndDistance;

            case FogMode.Exponential:
                // exp(-d * density) ≤ 0.01  →  d ≥ ln(100) / density ≈ 4.605 / density
                if (RenderSettings.fogDensity > 0f)
                    return 4.605f / RenderSettings.fogDensity;
                break;

            case FogMode.ExponentialSquared:
                // exp(-(d * density)^2) ≤ 0.01  →  d ≥ sqrt(ln(100)) / density ≈ 2.146 / density
                if (RenderSettings.fogDensity > 0f)
                    return 2.146f / RenderSettings.fogDensity;
                break;
        }
        return -1f;
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
        Debug.Log("[ZombieSpawner] Entrando en Zona 3 — ELIMINANDO TODOS los zombis del mapa (excepto mansión).");

        for (int i = activeZombies.Count - 1; i >= 0; i--)
        {
            ZombieAI zombie = activeZombies[i];
            if (zombie == null) { activeZombies.RemoveAt(i); continue; }

            // Los zombis de mansión son completamente independientes: no tocarlos
            if (zombie.isMansionZombie) continue;

            activeZombies.RemoveAt(i);
            Destroy(zombie.gameObject);
        }
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
        // Solo puntos activos
        var active = pointsByZone[zone].FindAll(p => p != null && p.isActive);
        if (active.Count == 0) return null;
        return active[Random.Range(0, active.Count)];
    }

    /// <param name="activeOnly">Si true (por defecto), ignora los puntos con isActive=false.
    /// Usar false en reubicación de zombis para que vuelvan a su punto aunque esté inactivo.</param>
    ZombieSpawnPoint GetClosestInList(Vector3 position, List<ZombieSpawnPoint> list, bool activeOnly = true)
    {
        ZombieSpawnPoint closest = null;
        float minDist = float.MaxValue;
        foreach (var p in list)
        {
            if (p == null) continue;
            if (activeOnly && !p.isActive) continue;   // saltar puntos inactivos al spawnear
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
        if (playerTransform == null) return SpawnZone.Zona1A;

        Vector3 pos = playerTransform.position;
        ZombieSpawnPoint best = null;
        float minDist = float.MaxValue;

        foreach (var p in spawnPoints)
        {
            if (p == null || p.IsInfiniteZone) continue;
            float d = p.DistanceTo(pos);
            if (d < minDist) { minDist = d; best = p; }
        }

        return best != null ? best.zone : SpawnZone.Zona1A;
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

    // ══════════════════════════════════════════════════════════
    //  STRIP LOD GROUPS — Eliminar LODs con materiales rotos
    // ══════════════════════════════════════════════════════════

    /// <summary>
    /// Elimina los LODGroup del zombi instanciado para evitar que LOD1/LOD2
    /// (que no tienen materiales asignados) se muestren blancos a distancia.
    /// Solo mantiene los renderers de LOD0, destruye los de LOD1/2.
    /// </summary>
    void StripLODGroups(GameObject zombie)
    {
        LODGroup[] lodGroups = zombie.GetComponentsInChildren<LODGroup>();
        if (lodGroups == null || lodGroups.Length == 0) return;

        foreach (LODGroup lodGroup in lodGroups)
        {
            LOD[] lods = lodGroup.GetLODs();

            // Destruir los renderers de LOD1, LOD2, etc. (todo excepto LOD0)
            for (int i = 1; i < lods.Length; i++)
            {
                if (lods[i].renderers == null) continue;
                foreach (Renderer rend in lods[i].renderers)
                {
                    if (rend != null)
                        Destroy(rend.gameObject);
                }
            }

            // Eliminar el componente LODGroup
            Destroy(lodGroup);
        }
    }
}
