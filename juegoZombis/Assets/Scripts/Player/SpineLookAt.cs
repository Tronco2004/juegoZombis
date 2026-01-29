using UnityEngine;

/// <summary>
/// Hace que el torso del personaje siga la rotación vertical de la cámara
/// </summary>
public class SpineLookAt : MonoBehaviour
{
    [Header("Referencias")]
    public Transform playerCamera;
    public Transform spinebone;

    [Header("Configuración")]
    [Range(0f, 1f)]
    public float spineInfluence = 0.5f;
    public float maxAngle = 30f;

    private Animator animator;

    void Start()
    {
        animator = GetComponent<Animator>();
        
        if (playerCamera == null)
            playerCamera = Camera.main?.transform;
    }

    // OnAnimatorIK se llama cuando el Animator tiene IK Pass activado
    void LateUpdate()
    {
        if (spinebone == null || playerCamera == null) return;

        // Obtener ángulo de la cámara
        float cameraAngle = playerCamera.localEulerAngles.x;
        if (cameraAngle > 180f) cameraAngle -= 360f;
        
        // Limitar
        cameraAngle = Mathf.Clamp(cameraAngle, -maxAngle, maxAngle);
        
        // Aplicar influencia
        float finalAngle = cameraAngle * spineInfluence;

        // Rotar sobre el eje X del MUNDO (horizontal, para inclinar adelante/atrás)
        // Usamos el transform.right del personaje principal, no del hueso
        Vector3 rotationAxis = transform.right;
        
        // Guardar rotación actual del spine (puesta por la animación)
        Quaternion currentRotation = spinebone.rotation;
        
        // Crear rotación adicional
        Quaternion additionalRotation = Quaternion.AngleAxis(finalAngle, rotationAxis);
        
        // Combinar: primero la animación, luego nuestra rotación
        spinebone.rotation = additionalRotation * currentRotation;
    }
}

