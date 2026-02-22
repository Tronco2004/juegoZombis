using UnityEngine;
using System.Collections;

/// <summary>
/// Sistema de vida del tanque.
/// - 1000 HP por defecto.
/// - Los zombis pueden atacarlo directamente.
/// - Si llega a 0 HP, explota y se activa el GAME OVER.
///
/// Ponlo en el mismo GameObject que TankController.
/// </summary>
public class TankHealth : MonoBehaviour
{
    public static TankHealth Instance { get; private set; }

    [Header("=== VIDA ===")]
    [Tooltip("Vida máxima del tanque")]
    public float maxHealth = 1000f;
    [HideInInspector]
    public float currentHealth;
    [HideInInspector]
    public bool isDestroyed = false;

    [Header("=== EXPLOSIÓN ===")]
    [Tooltip("Sistema de partículas de explosión (opcional). Se separa del tanque al morir.")]
    public ParticleSystem explosionEffect;
    [Tooltip("Segundo efecto de fuego/humo persistente (opcional)")]
    public ParticleSystem fireEffect;
    [Tooltip("Sonido de explosión")]
    public AudioClip explosionSound;
    [Tooltip("Segundos antes de desactivar el GameObject del tanque tras explotar")]
    public float destroyDelay = 3f;

    [Header("=== UI (Opcional) ===")]
    [Tooltip("Slider del HUD para mostrar la vida del tanque. Asignar desde el Inspector.")]
    public UnityEngine.UI.Slider healthBar;
    [Tooltip("Texto con la vida numérica (ej. '750 / 1000'). Opcional.")]
    public TMPro.TextMeshProUGUI healthText;

    [Header("=== DEBUG ===")]
    public bool debugMode = true;

    // ─── Componentes ──────────────────────────────────────────
    private AudioSource audioSrc;

    // ══════════════════════════════════════════════════════════
    //  UNITY LIFECYCLE
    // ══════════════════════════════════════════════════════════

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
        {
            Destroy(this);
            return;
        }
    }

    void Start()
    {
        currentHealth = maxHealth;

        audioSrc = GetComponent<AudioSource>();
        if (audioSrc == null)
            audioSrc = gameObject.AddComponent<AudioSource>();

        UpdateUI();
    }

    // ══════════════════════════════════════════════════════════
    //  API PÚBLICA
    // ══════════════════════════════════════════════════════════

    /// <summary>
    /// Aplica daño al tanque. Si llega a 0 HP, explota y activa el game over.
    /// </summary>
    public void TakeDamage(float amount)
    {
        if (isDestroyed) return;

        currentHealth -= amount;
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
        UpdateUI();

        if (debugMode)
            Debug.Log($"[TankHealth] Tanque recibió {amount} de daño. Vida: {currentHealth}/{maxHealth}");

        if (currentHealth <= 0f)
            Die();
    }

    /// <summary>
    /// Cura el tanque la cantidad indicada.
    /// </summary>
    public void Heal(float amount)
    {
        if (isDestroyed) return;
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
        UpdateUI();
    }

    /// <summary>
    /// Porcentaje de vida restante (0–1).
    /// </summary>
    public float HealthPercent => currentHealth / maxHealth;

    // ══════════════════════════════════════════════════════════
    //  MUERTE / EXPLOSIÓN
    // ══════════════════════════════════════════════════════════

    void Die()
    {
        isDestroyed = true;
        Debug.Log("[TankHealth] ¡El tanque ha sido destruido! — GAME OVER");

        // ── Sacar al jugador del tanque si está dentro ────────
        TankController tc = GetComponent<TankController>();
        if (tc != null && tc.IsBeingDriven())
            tc.ExitTank();

        // ── Efectos visuales y de sonido ──────────────────────
        PlayExplosionEffects();

        // ── Game Over: matar al jugador ──────────────────────
        if (PlayerHealth.Instance != null)
            PlayerHealth.Instance.TakeDamage(9999f, transform.position);

        // ── Desactivar el tanque después de un tiempo ─────────
        StartCoroutine(DisableAfterDelay());
    }

    void PlayExplosionEffects()
    {
        // Partículas de explosión
        if (explosionEffect != null)
        {
            explosionEffect.transform.SetParent(null); // Separar del tanque para que no se destruya con él
            explosionEffect.Play();
        }

        // Fuego/humo persistente
        if (fireEffect != null)
        {
            fireEffect.transform.SetParent(null);
            fireEffect.Play();
        }

        // Sonido
        if (explosionSound != null && audioSrc != null)
        {
            // Separar el AudioSource para que el sonido se complete aunque el tanque se desactive
            audioSrc.transform.SetParent(null);
            audioSrc.PlayOneShot(explosionSound);
        }
    }

    IEnumerator DisableAfterDelay()
    {
        // Desactivar el movimiento inmediatamente
        TankController tc = GetComponent<TankController>();
        if (tc != null) tc.enabled = false;

        TankInteraction ti = GetComponent<TankInteraction>();
        if (ti != null) ti.enabled = false;

        yield return new WaitForSeconds(destroyDelay);

        gameObject.SetActive(false);
    }

    // ══════════════════════════════════════════════════════════
    //  UI
    // ══════════════════════════════════════════════════════════

    void UpdateUI()
    {
        if (healthBar != null)
            healthBar.value = HealthPercent;

        if (healthText != null)
            healthText.text = $"{Mathf.CeilToInt(currentHealth)} / {Mathf.CeilToInt(maxHealth)}";
    }

    // ══════════════════════════════════════════════════════════
    //  GIZMOS
    // ══════════════════════════════════════════════════════════

    void OnDrawGizmosSelected()
    {
        // Mostrar radio de ataque de los zombis al tanque (mismo que ZombieAI.attackRange, ~2.5)
        Gizmos.color = new Color(1f, 0.3f, 0f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, 2.5f);
    }
}
