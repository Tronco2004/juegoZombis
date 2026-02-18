using UnityEngine;

/// <summary>
/// Controlador de misil — Se mueve en línea recta hacia el punto objetivo.
/// Se destruye al impactar o al pasar su tiempo de vida.
/// Hace daño a enemigos con EnemyHealth y daño por explosión en área.
///
/// Ponlo en el prefab del misil junto con un Rigidbody y un Collider.
/// </summary>
public class MissileController : MonoBehaviour
{
    [Header("=== MOVIMIENTO ===")]
    [Tooltip("Velocidad del misil")]
    public float speed = 60f;
    [Tooltip("Tiempo de vida antes de autodestruirse")]
    public float lifetime = 5f;

    [Header("=== DAÑO ===")]
    [Tooltip("Daño directo al impactar")]
    public float directDamage = 100f;
    [Tooltip("Radio de explosión (0 = sin daño en área)")]
    public float explosionRadius = 5f;
    [Tooltip("Daño máximo de la explosión en área")]
    public float explosionDamage = 50f;
    [Tooltip("Fuerza de la explosión aplicada a rigidbodies cercanos")]
    public float explosionForce = 300f;

    [Header("=== EFECTOS (Opcional) ===")]
    [Tooltip("Prefab de la explosión (partículas) — se instancia al impactar")]
    public GameObject explosionEffectPrefab;
    [Tooltip("Sonido de la explosión")]
    public AudioClip explosionSound;
    [Tooltip("Volumen del sonido de explosión")]
    [Range(0f, 1f)]
    public float explosionVolume = 1f;
    [Tooltip("Partículas de estela del misil")]
    public ParticleSystem trailEffect;

    [Header("=== CAPAS ===")]
    [Tooltip("Capas afectadas por la explosión")]
    public LayerMask explosionLayerMask = ~0;

    // Estado interno
    private Vector3 targetPoint;
    private bool hasTarget;
    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }

        rb.useGravity = false;
        rb.isKinematic = false;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.velocity = transform.forward * speed;

        // Autodestucción por tiempo
        Destroy(gameObject, lifetime);
    }

    /// <summary>
    /// Configura el punto objetivo del misil.
    /// Llamado por TankController al instanciar el misil.
    /// </summary>
    public void SetTarget(Vector3 point)
    {
        targetPoint = point;
        hasTarget = true;

        // Orientar hacia el objetivo
        Vector3 dir = (targetPoint - transform.position).normalized;
        if (dir.sqrMagnitude > 0.01f)
        {
            transform.rotation = Quaternion.LookRotation(dir);
        }
    }

    void FixedUpdate()
    {
        // Mantener velocidad constante en la dirección de avance
        if (rb != null)
        {
            rb.velocity = transform.forward * speed;
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        Explode(collision.contacts[0].point);
    }

    void OnTriggerEnter(Collider other)
    {
        // También funciona si el collider es trigger
        Explode(transform.position);
    }

    void Explode(Vector3 explosionPoint)
    {
        // Daño directo al objeto impactado
        // (se maneja en OnCollisionEnter a través del contacto)

        // Daño en área
        if (explosionRadius > 0f)
        {
            Collider[] colliders = Physics.OverlapSphere(explosionPoint, explosionRadius, explosionLayerMask);

            foreach (Collider col in colliders)
            {
                // Daño a enemigos
                EnemyHealth enemy = col.GetComponentInParent<EnemyHealth>();
                if (enemy != null)
                {
                    // Calcular daño basado en distancia
                    float dist = Vector3.Distance(explosionPoint, col.transform.position);
                    float damageRatio = 1f - Mathf.Clamp01(dist / explosionRadius);
                    float damage = explosionDamage * damageRatio;

                    // Si es impacto directo, sumar daño directo
                    if (dist < 1f)
                        damage += directDamage;

                    enemy.TakeDamage(damage);
                }

                // Fuerza a rigidbodies cercanos
                Rigidbody otherRb = col.GetComponent<Rigidbody>();
                if (otherRb != null && otherRb != rb)
                {
                    otherRb.AddExplosionForce(explosionForce, explosionPoint, explosionRadius);
                }
            }
        }

        // Efecto visual de explosión
        if (explosionEffectPrefab != null)
        {
            GameObject fx = Instantiate(explosionEffectPrefab, explosionPoint, Quaternion.identity);
            Destroy(fx, 5f);
        }

        // Sonido de explosión
        if (explosionSound != null)
        {
            AudioSource.PlayClipAtPoint(explosionSound, explosionPoint, explosionVolume);
        }

        // Separar partículas de estela para que se desvanezcan naturalmente
        if (trailEffect != null)
        {
            trailEffect.transform.SetParent(null);
            trailEffect.Stop();
            Destroy(trailEffect.gameObject, trailEffect.main.startLifetime.constantMax);
        }

        // Destruir el misil
        Destroy(gameObject);
    }

    // ══════════════════════════════════════════════════════════════════
    //  GIZMOS
    // ══════════════════════════════════════════════════════════════════

    void OnDrawGizmosSelected()
    {
        // Radio de explosión
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, explosionRadius);

        // Dirección de vuelo
        Gizmos.color = Color.red;
        Gizmos.DrawRay(transform.position, transform.forward * 3f);
    }
}
