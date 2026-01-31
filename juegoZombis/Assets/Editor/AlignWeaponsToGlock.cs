using UnityEngine;
using UnityEditor;

public class AlignWeaponsToGlock : MonoBehaviour
{
    [MenuItem("Tools/Alinear Armas a la Glock")]
    static void AlignWeapons()
    {
        // Buscar la Glock en la escena
        GameObject glock = GameObject.Find("G17 Pistol - Animated");
        if (glock == null)
        {
            glock = GameObject.Find("Glock");
            if (glock == null)
            {
                glock = GameObject.Find("Pipa");
                if (glock == null)
                {
                    // Buscar cualquier objeto con "G17" o "Pistol" en el nombre
                    FPSWeaponController[] weapons = FindObjectsOfType<FPSWeaponController>(true);
                    foreach (var w in weapons)
                    {
                        if (w.weaponName.ToLower().Contains("pistol") || 
                            w.weaponName.ToLower().Contains("glock") ||
                            w.gameObject.name.ToLower().Contains("g17"))
                        {
                            glock = w.gameObject;
                            break;
                        }
                    }
                }
            }
        }
        
        if (glock == null)
        {
            Debug.LogError("No se encontró la Glock! Asegúrate de que esté en la escena.");
            return;
        }
        
        Vector3 glockPos = glock.transform.localPosition;
        Quaternion glockRot = glock.transform.localRotation;
        
        Debug.Log($"Posición de referencia (Glock): {glockPos}");
        Debug.Log($"Rotación de referencia (Glock): {glockRot.eulerAngles}");
        
        // Buscar todas las armas con FPSWeaponController
        FPSWeaponController[] allWeapons = FindObjectsOfType<FPSWeaponController>(true);
        int aligned = 0;
        
        foreach (var weapon in allWeapons)
        {
            if (weapon.gameObject == glock)
                continue;
                
            Undo.RecordObject(weapon.transform, "Alinear arma");
            
            weapon.transform.localPosition = glockPos;
            weapon.transform.localRotation = glockRot;
            
            Debug.Log($"✓ {weapon.weaponName} alineada a la posición de la Glock");
            aligned++;
        }
        
        Debug.Log($"=== {aligned} armas alineadas ===");
    }
    
    [MenuItem("Tools/Mostrar Posiciones de Armas")]
    static void ShowWeaponPositions()
    {
        FPSWeaponController[] weapons = FindObjectsOfType<FPSWeaponController>(true);
        
        Debug.Log("=== Posiciones de Armas ===");
        foreach (var weapon in weapons)
        {
            Debug.Log($"{weapon.weaponName}: Pos={weapon.transform.localPosition} Rot={weapon.transform.localRotation.eulerAngles}");
        }
    }
}
