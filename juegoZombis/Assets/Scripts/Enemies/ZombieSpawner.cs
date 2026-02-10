using UnityEngine;
using TMPro;

public class ZombieSpawner : MonoBehaviour
{
    [Header("Prefab")]
    public GameObject zombiePrefab;

    [Header("Zona de Spawn (delimitable desde el Editor)")]
    [Tooltip("Tamaño de la zona rectangular de spawn (ancho X, alto Y, profundidad Z)")]
    public Vector3 spawnZoneSize = new Vector3(20f, 0f, 20f);
    [Tooltip("Desplazamiento del centro de la zona respecto al transform del spawner")]
    public Vector3 spawnZoneOffset = Vector3.zero;
    [Tooltip("Color del Gizmo de la zona en el editor")]
    public Color gizmoColor = new Color(1f, 0f, 0f, 0.3f);

    [Header("Oleadas")]
    public int baseZombies = 10;
    public float timeBetweenSpawns = 0.4f;
    public float timeBetweenWaves = 3f;

    [Header("Escalado")]
    public float baseHealth = 100f;
    public float healthPerWave = 20f;
    public float baseDamage = 20f;
    public int damageIncreaseEveryWaves = 4;
    public float damageBonusPerStep = 5f;

    [Header("UI Oleada")]
    public TextMeshProUGUI waveText;
    public bool createWaveTextIfMissing = true;
    public string wavePrefix = "Oleada: ";
    public Vector2 waveTextAnchoredPos = new Vector2(20f, -20f);
    public Vector2 waveTextSize = new Vector2(300f, 60f);
    public int waveTextFontSize = 32;
    public Color waveTextColor = Color.white;

    private int currentWave = 0;
    private int aliveZombies = 0;

    void Start()
    {
        if (createWaveTextIfMissing && waveText == null)
        {
            TryCreateWaveText();
        }

        if (zombiePrefab == null)
        {
            Debug.LogError("[ZombieSpawner] No hay zombiePrefab asignado.");
            return;
        }

        StartCoroutine(SpawnLoop());
    }

    System.Collections.IEnumerator SpawnLoop()
    {
        while (true)
        {
            currentWave++;
            int zombiesThisWave = baseZombies + currentWave;
            UpdateWaveUI();

            for (int i = 0; i < zombiesThisWave; i++)
            {
                SpawnZombie();
                yield return new WaitForSeconds(timeBetweenSpawns);
            }

            while (aliveZombies > 0)
            {
                yield return null;
            }

            yield return new WaitForSeconds(timeBetweenWaves);
        }
    }

    /// <summary>
    /// Devuelve una posición aleatoria dentro de la zona de spawn definida.
    /// </summary>
    Vector3 GetRandomSpawnPosition()
    {
        Vector3 center = transform.position + spawnZoneOffset;
        Vector3 halfSize = spawnZoneSize * 0.5f;

        float x = Random.Range(center.x - halfSize.x, center.x + halfSize.x);
        float y = center.y + Random.Range(-halfSize.y, halfSize.y);
        float z = Random.Range(center.z - halfSize.z, center.z + halfSize.z);

        return new Vector3(x, y, z);
    }

    void SpawnZombie()
    {
        Vector3 spawnPos = GetRandomSpawnPosition();
        Quaternion spawnRot = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
        GameObject zombie = Instantiate(zombiePrefab, spawnPos, spawnRot);
        aliveZombies++;

        ZombieWaveMember member = zombie.GetComponent<ZombieWaveMember>();
        if (member == null)
        {
            member = zombie.AddComponent<ZombieWaveMember>();
        }
        member.spawner = this;

        EnemyHealth enemyHealth = zombie.GetComponent<EnemyHealth>();
        if (enemyHealth != null)
        {
            enemyHealth.useRandomHealth = false;
            enemyHealth.maxHealth = baseHealth + (currentWave - 1) * healthPerWave;
            enemyHealth.currentHealth = enemyHealth.maxHealth;
        }

        ZombieAI ai = zombie.GetComponent<ZombieAI>();
        if (ai != null)
        {
            int steps = damageIncreaseEveryWaves > 0 ? currentWave / damageIncreaseEveryWaves : 0;
            float damageBonus = steps * damageBonusPerStep;
            ai.damage = baseDamage + damageBonus;
        }
    }

    void UpdateWaveUI()
    {
        if (waveText != null)
        {
            waveText.text = wavePrefix + currentWave;
        }
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

    public void NotifyZombieDestroyed()
    {
        aliveZombies = Mathf.Max(0, aliveZombies - 1);
    }

    /// <summary>
    /// Dibuja la zona de spawn en el editor para que puedas verla y ajustarla.
    /// </summary>
    void OnDrawGizmosSelected()
    {
        Vector3 center = transform.position + spawnZoneOffset;

        // Cubo semitransparente
        Gizmos.color = gizmoColor;
        Gizmos.DrawCube(center, spawnZoneSize);

        // Borde del cubo
        Color wireColor = gizmoColor;
        wireColor.a = 1f;
        Gizmos.color = wireColor;
        Gizmos.DrawWireCube(center, spawnZoneSize);
    }
}
