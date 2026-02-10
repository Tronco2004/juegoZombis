using UnityEngine;

public class BulletController : MonoBehaviour
{
    public float speed = 200f;
    public float lifetime = 3f;
    
    private Rigidbody rb;
    
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.velocity = transform.forward * speed;
        }
        Destroy(gameObject, lifetime);
    }
    
    void OnCollisionEnter(Collision collision)
    {
        // Intentar hacer daño al enemigo
        EnemyHealth enemy = collision.gameObject.GetComponentInParent<EnemyHealth>();
        if (enemy != null)
        {
            enemy.TakeDamage(speed); // Usa speed como daño base
        }
        
        Destroy(gameObject);
    }
}
