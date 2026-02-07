using UnityEngine;
using TMPro;

public class ZombieSpawner : MonoBehaviour
{
    [Header("Prefab y Puntos de Spawn")]
    public GameObject zombiePrefab;
    public Transform[] spawnPoints;

    [Header("Oleadas")]
    public int baseZombies = 10; // Oleada 1 = 10 + 1 = 11
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

        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogError("[ZombieSpawner] No hay spawnPoints asignados.");
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

    void SpawnZombie()
    {
        Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
        GameObject zombie = Instantiate(zombiePrefab, spawnPoint.position, spawnPoint.rotation);
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
}
