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
        Destroy(gameObject);
    }
}
