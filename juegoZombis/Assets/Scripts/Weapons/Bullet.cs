using UnityEngine;

public class Bullet : MonoBehaviour
{
    public float speed = 200f;
    public float damage = 25f;
    public float lifetime = 3f;
    public GameObject impactEffect;
    
    [HideInInspector]
    public bool velocitySetExternally = false; // Si ya se configuró la velocidad desde fuera
    
    private Rigidbody rb;
    
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        
        // Solo aplicar velocidad si no fue configurada desde FPSWeaponController
        if (rb != null && !velocitySetExternally)
        {
            rb.useGravity = false;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.velocity = transform.forward * speed;
        }
        
        Destroy(gameObject, lifetime);
    }
    
    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
            return;
        
        // Intentar hacer daño al enemigo
        EnemyHealth enemy = collision.gameObject.GetComponentInParent<EnemyHealth>();
        if (enemy != null)
        {
            enemy.TakeDamage(damage);
        }
        
        // Efecto de impacto
        if (impactEffect != null)
        {
            ContactPoint contact = collision.contacts[0];
            Instantiate(impactEffect, contact.point, Quaternion.LookRotation(contact.normal));
        }
        
        Destroy(gameObject);
    }
}
