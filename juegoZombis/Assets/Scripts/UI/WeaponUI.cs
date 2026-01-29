using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UI para mostrar munición y crosshair
/// </summary>
public class WeaponUI : MonoBehaviour
{
    [Header("Referencias UI")]
    public TextMeshProUGUI ammoText;
    public Image crosshair;
    
    [Header("Referencia al Arma")]
    public FPSWeaponController weapon;
    
    void Update()
    {
        if (weapon != null && ammoText != null)
        {
            ammoText.text = weapon.GetAmmoText();
        }
    }
}
