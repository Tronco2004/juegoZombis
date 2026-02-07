using UnityEngine;

/// <summary>
/// Script de debug - ponlo en el Player para ver qué está mirando
/// </summary>
public class RaycastDebug : MonoBehaviour
{
    void Update()
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            cam = FindObjectOfType<Camera>();
        }
        
        if (cam == null)
        {
            Debug.LogError("NO HAY CÁMARA!");
            return;
        }
        
        Ray ray = cam.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));
        RaycastHit hit;
        
        // Dibujar rayo en Scene view
        Debug.DrawRay(ray.origin, ray.direction * 10f, Color.green);
        
        if (Physics.Raycast(ray, out hit, 10f))
        {
            Debug.Log("MIRANDO A: " + hit.transform.name + " (distancia: " + hit.distance + ")");
        }
        else
        {
            Debug.Log("No golpea nada");
        }
    }
}
