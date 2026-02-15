using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

public class NuclearBomb : MonoBehaviour
{
    public float blastRadius = 100f;
    public float maxDamage = 999f;
    public float flashIntensity = 3f;
    public float flashDuration = 0.5f;
    public float timeToVictory = 10f;
    
    private Light explosionLight;
    
    void Start()
    {
        if (explosionLight == null)
        {
            GameObject lightObj = new GameObject("Light");
            lightObj.transform.parent = transform;
            explosionLight = lightObj.AddComponent<Light>();
            explosionLight.type = LightType.Point;
            explosionLight.intensity = 0;
            explosionLight.range = 200;
        }
    }
    
    public void Detonate()
    {
        StartCoroutine(Explode());
    }
    
    IEnumerator Explode()
    {
        if (explosionLight != null)
        {
            float t = 0f;
            while (t < flashDuration)
            {
                t += Time.deltaTime;
                explosionLight.intensity = Mathf.Lerp(flashIntensity, 0, t / flashDuration);
                yield return null;
            }
            explosionLight.intensity = 0;
        }
        
        DamageEnemies();
        
        if (Camera.main != null)
            StartCoroutine(Shake());
        
        yield return new WaitForSeconds(timeToVictory);
        SceneManager.LoadScene("PantallaVictoria");
    }
    
    void DamageEnemies()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, blastRadius);
        
        foreach (Collider hit in hits)
        {
            EnemyHealth enemy = hit.GetComponent<EnemyHealth>();
            if (enemy != null)
            {
                float dist = Vector3.Distance(transform.position, hit.transform.position);
                float falloff = 1f - (dist / blastRadius);
                float dmg = maxDamage * falloff;
                enemy.TakeDamage((int)dmg);
            }
            
            Rigidbody rb = hit.GetComponent<Rigidbody>();
            if (rb != null && !rb.isKinematic)
            {
                Vector3 dir = (hit.transform.position - transform.position).normalized;
                rb.velocity = dir * 20f;
            }
        }
    }
    
    IEnumerator Shake()
    {
        Vector3 origPos = Camera.main.transform.localPosition;
        float t = 0f;
        
        while (t < 0.5f)
        {
            t += Time.deltaTime;
            float x = Random.Range(-1f, 1f) * 0.5f;
            float y = Random.Range(-1f, 1f) * 0.5f;
            Camera.main.transform.localPosition = origPos + new Vector3(x, y, 0);
            yield return null;
        }
        
        Camera.main.transform.localPosition = origPos;
    }
}
