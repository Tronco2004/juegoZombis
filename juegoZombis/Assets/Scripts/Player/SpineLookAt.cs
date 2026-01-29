using UnityEngine;

/// <summary>
/// Hace que el torso del personaje siga la rotación vertical de la cámara
/// </summary>
public class SpineLookAt : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("La cámara del jugador")]
    public Transform playerCamera;
    
    [Tooltip("El hueso del spine a rotar (spine_01 o spine_02)")]
    public Transform spinebone;

    [Header("Configuración")]
    [Tooltip("Cuánto afecta la rotación de la cámara al spine (0-1)")]
    [Range(0f, 1f)]
    public float spineInfluence = 0.6f;
    
    [Tooltip("Límite de rotación hacia arriba (grados)")]
    public float maxLookUp = 40f;
    
    [Tooltip("Límite de rotación hacia abajo (grados)")]
    public float maxLookDown = 40f;

    [Tooltip("Suavizado del movimiento")]
    public float smoothSpeed = 10f;

    // Rotación actual aplicada
    private float currentAngle = 0f;

    void Start()
    {
        // Auto-buscar cámara si no está asignada
        if (playerCamera == null)
        {
            playerCamera = Camera.main?.transform;
        }

        // Auto-buscar spine si no está asignado
        if (spinebone == null)
        {
            spinebone = FindSpineBone();
        }

        if (spinebone == null)
        {
            Debug.LogError("SpineLookAt: No se encontró el hueso del spine. Asígnalo manualmente.");
        }
    }

    Transform FindSpineBone()
    {
        // Buscar huesos comunes del spine
        string[] boneNames = { "spine_02", "spine_01", "Spine2", "Spine1", "Chest", "chest" };
        
        foreach (string boneName in boneNames)
        {
            Transform bone = FindChildRecursive(transform, boneName);
            if (bone != null)
            {
                Debug.Log("SpineLookAt: Usando hueso " + boneName);
                return bone;
            }
        }
        return null;
    }

    Transform FindChildRecursive(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name)
                return child;
            
            Transform found = FindChildRecursive(child, name);
            if (found != null)
                return found;
        }
        return null;
    }

    // LateUpdate se ejecuta después de las animaciones
    void LateUpdate()
    {
        if (spinebone == null || playerCamera == null) return;

        // Obtener el ángulo vertical de la cámara (pitch)
        float cameraAngle = playerCamera.localEulerAngles.x;
        
        // Convertir de 0-360 a -180 a 180
        if (cameraAngle > 180f)
            cameraAngle -= 360f;

        // Limitar el ángulo
        float targetAngle = Mathf.Clamp(cameraAngle, -maxLookUp, maxLookDown);
        
        // Aplicar la influencia
        targetAngle *= spineInfluence;

        // Suavizar
        currentAngle = Mathf.Lerp(currentAngle, targetAngle, Time.deltaTime * smoothSpeed);

        // Aplicar rotación al spine (rotación local en X)
        spinebone.localRotation *= Quaternion.Euler(currentAngle, 0f, 0f);
    }
}
