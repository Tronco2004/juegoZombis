using UnityEngine;

/// Script de diagnóstico - SI ESTO NO FUNCIONA, HAY UN ERROR GRAVE
public class SimpleTest : MonoBehaviour
{
    void Awake()
    {
        Debug.Log("=== AWAKE EJECUTADO ===");
    }
    
    void Start()
    {
        Debug.Log("=== START EJECUTADO ===");
    }
    
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            Debug.Log("=== E PRESIONADO ===");
        }
    }
}
