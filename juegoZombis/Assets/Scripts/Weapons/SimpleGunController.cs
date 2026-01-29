using UnityEngine;

public class SimpleGunController : MonoBehaviour
{
    public Camera playerCamera;
    public int ammo = 17;
    
    void Update()
    {
        if (Input.GetMouseButtonDown(0) && ammo > 0)
        {
            ammo--;
            Debug.Log("DISPARO! Munición: " + ammo);
        }
        
        if (Input.GetKeyDown(KeyCode.R))
        {
            ammo = 17;
            Debug.Log("RECARGADO! Munición: " + ammo);
        }
    }
}
