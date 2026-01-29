using UnityEngine;

public class Bullet : MonoBehaviour
{
    public float speed = 200f;
    public float damage = 25f;
    public float lifetime = 3f;
    public GameObject impactEffect;
    
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
        if (collision.gameObject.CompareTag("Player"))
            return;
        
        Destroy(gameObject);
    }
}
