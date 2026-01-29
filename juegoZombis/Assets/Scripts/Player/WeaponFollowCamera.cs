using UnityEngine;

/// <summary>
/// Hace que el arma siga el movimiento vertical de la cámara (arriba/abajo)
/// </summary>
public class WeaponFollowCamera : MonoBehaviour
{
    [Header("Referencias")]
    public Transform mainCamera;
    
    [Header("Configuración")]
    [Tooltip("Cuánto se mueve el arma con la cámara (0-1)")]
    [Range(0f, 1f)]
    public float weaponFollowAmount = 0.8f;
    
    [Tooltip("Velocidad de suavizado")]
    public float smoothSpeed = 8f;

    // Posición inicial del arma
    private Vector3 initialPosition;
    private float currentYOffset = 0f;

    void Start()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main?.transform;
        }

        // Guarda la posición inicial del arma
        initialPosition = transform.localPosition;
    }

    void Update()
    {
        if (mainCamera == null) return;

        // Obtener el ángulo vertical de la cámara (pitch)
        float cameraAngle = mainCamera.localEulerAngles.x;
        
        // Convertir de 0-360 a -180 a 180
        if (cameraAngle > 180f)
            cameraAngle -= 360f;

        // Calcular el desplazamiento Y basado en el ángulo de la cámara
        // Cuando mira arriba (ángulo negativo), el arma sube
        // Cuando mira abajo (ángulo positivo), el arma baja
        float targetYOffset = -cameraAngle * 0.02f * weaponFollowAmount;

        // Suavizar el movimiento
        currentYOffset = Mathf.Lerp(currentYOffset, targetYOffset, Time.deltaTime * smoothSpeed);

        // Aplicar la nueva posición - SOLO cambiar Y, mantener X y Z
        Vector3 newPosition = initialPosition;
        newPosition.y = initialPosition.y + currentYOffset;
        // X y Z permanecen igual
        transform.localPosition = newPosition;
    }
}
