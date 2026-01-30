using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UI para mostrar munición, crosshair, stamina y puntos
/// </summary>
public class WeaponUI : MonoBehaviour
{
    [Header("Referencias UI - Munición")]
    public TextMeshProUGUI ammoText;
    public Image crosshair;
    
    [Header("Referencias UI - Puntos")]
    public TextMeshProUGUI pointsText;
    
    [Header("Referencias UI - Stamina")]
    public RectTransform staminaBar; // RectTransform de la barra de stamina
    public Image staminaBarImage; // Image para cambiar el color
    public TextMeshProUGUI staminaText; // Texto opcional para mostrar valor
    public float staminaBarMaxWidth = 200f; // Ancho máximo de la barra en píxeles
    
    [Header("Colores de Stamina")]
    public Color staminaFullColor = Color.green;
    public Color staminaLowColor = Color.red;
    public float lowStaminaThreshold = 0.3f; // 30% = color rojo
    
    [Header("Referencia al Arma")]
    public FPSWeaponController weapon;
    
    [Header("Referencia al Jugador")]
    public FirstPersonController player;
    
    private WeaponSwitcher weaponSwitcher;
    
    void Start()
    {
        // Buscar el WeaponSwitcher para obtener el arma actual
        weaponSwitcher = FindObjectOfType<WeaponSwitcher>();
        
        // Buscar automáticamente el arma si no está asignada
        if (weapon == null)
        {
            weapon = FindObjectOfType<FPSWeaponController>();
            if (weapon != null)
            {
                Debug.Log("[WeaponUI] FPSWeaponController encontrado automáticamente");
            }
        }
        
        // Buscar automáticamente el jugador si no está asignado
        if (player == null)
        {
            player = FindObjectOfType<FirstPersonController>();
            if (player != null)
            {
                Debug.Log("[WeaponUI] FirstPersonController encontrado automáticamente");
            }
        }
        
        // Suscribirse a cambios de puntos
        if (PlayerPoints.Instance != null)
        {
            PlayerPoints.Instance.OnPointsChanged += UpdatePointsUI;
            UpdatePointsUI(PlayerPoints.Instance.CurrentPoints);
        }
    }
    
    void OnDestroy()
    {
        if (PlayerPoints.Instance != null)
        {
            PlayerPoints.Instance.OnPointsChanged -= UpdatePointsUI;
        }
    }
    
    void Update()
    {
        UpdateAmmoUI();
        UpdateStaminaUI();
    }
    
    void UpdateAmmoUI()
    {
        // Usar el arma actual del WeaponSwitcher
        FPSWeaponController currentWeapon = weapon;
        if (weaponSwitcher != null && weaponSwitcher.CurrentWeapon != null)
        {
            currentWeapon = weaponSwitcher.CurrentWeapon;
        }
        
        if (currentWeapon != null && ammoText != null)
        {
            ammoText.text = currentWeapon.GetAmmoText();
        }
        else if (ammoText != null && currentWeapon == null)
        {
            ammoText.text = "Sin arma";
        }
    }
    
    void UpdatePointsUI(int points)
    {
        if (pointsText != null)
        {
            pointsText.text = points.ToString();
        }
    }
    
    void UpdateStaminaUI()
    {
        if (player == null) return;
        
        float staminaPercent = player.StaminaPercentage;
        
        // Actualizar barra de stamina cambiando su ancho
        if (staminaBar != null)
        {
            // Cambiar el ancho de la barra según el porcentaje de stamina
            float newWidth = staminaBarMaxWidth * staminaPercent;
            staminaBar.sizeDelta = new Vector2(newWidth, staminaBar.sizeDelta.y);
        }
        
        // Cambiar color según el nivel de stamina
        if (staminaBarImage != null)
        {
            if (staminaPercent <= lowStaminaThreshold)
            {
                staminaBarImage.color = staminaLowColor;
            }
            else
            {
                staminaBarImage.color = Color.Lerp(staminaLowColor, staminaFullColor, (staminaPercent - lowStaminaThreshold) / (1f - lowStaminaThreshold));
            }
        }
        
        // Actualizar texto de stamina (opcional)
        if (staminaText != null)
        {
            staminaText.text = Mathf.RoundToInt(player.currentStamina) + " / " + Mathf.RoundToInt(player.maxStamina);
        }
    }
}
