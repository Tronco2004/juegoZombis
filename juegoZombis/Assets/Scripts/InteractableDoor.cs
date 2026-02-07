using UnityEngine;
using System.Collections;

/// <summary>
/// Puerta interactiva - pulsa E mirando a la puerta para abrir/cerrar.
/// </summary>
public class InteractableDoor : MonoBehaviour
{
    [Header("=== CONFIGURACIÓN ===")]
    public float openAngle = 90f;
    public float animationDuration = 0.5f;
    public bool openTowardsPlayer = true;
    public float interactionDistance = 3f;
    
    [Header("=== AUDIO (Opcional) ===")]
    public AudioClip openSound;
    public AudioClip closeSound;
    
    // Estados
    private bool isOpen = false;
    private bool isAnimating = false;
    private bool isLooking = false;
    
    // Rotaciones
    private Quaternion closedRotation;
    private Quaternion openRotation;
    
    // Componentes
    private AudioSource audioSource;
    private Camera playerCamera;
    private GUIStyle promptStyle;
    
    void Start()
    {
        closedRotation = transform.localRotation;
        openRotation = closedRotation * Quaternion.Euler(0, openAngle, 0);
        
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.spatialBlend = 1f;
        audioSource.playOnAwake = false;
        
        playerCamera = Camera.main;
        
        promptStyle = new GUIStyle();
        promptStyle.fontSize = 24;
        promptStyle.fontStyle = FontStyle.Bold;
        promptStyle.alignment = TextAnchor.MiddleCenter;
        promptStyle.normal.textColor = Color.white;
        
        // Añadir colliders a todos los hijos que tengan renderer
        SetupColliders();
    }
    
    void SetupColliders()
    {
        // Asegurar que este objeto y sus hijos pueden ser detectados
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer r in renderers)
        {
            if (r.GetComponent<Collider>() == null)
            {
                MeshFilter mf = r.GetComponent<MeshFilter>();
                if (mf != null && mf.sharedMesh != null)
                {
                    MeshCollider mc = r.gameObject.AddComponent<MeshCollider>();
                    mc.sharedMesh = mf.sharedMesh;
                }
            }
        }
    }
    
    void Update()
    {
        isLooking = false;
        
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
            if (playerCamera == null)
            {
                // Buscar cualquier cámara
                playerCamera = FindObjectOfType<Camera>();
                if (playerCamera == null)
                {
                    Debug.LogWarning("InteractableDoor: No se encontró ninguna cámara!");
                    return;
                }
            }
        }
        
        // Raycast desde el centro de la pantalla
        Ray ray = playerCamera.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));
        RaycastHit hit;
        
        // Debug: dibujar el rayo
        Debug.DrawRay(ray.origin, ray.direction * interactionDistance, Color.red);
        
        if (Physics.Raycast(ray, out hit, interactionDistance))
        {
            Debug.Log("Raycast golpea: " + hit.transform.name);
            
            // Verificar si golpeamos esta puerta o cualquier hijo
            if (IsPartOfThisDoor(hit.transform))
            {
                isLooking = true;
                Debug.Log("¡PUERTA DETECTADA! Pulsa E");
                
                if (Input.GetKeyDown(KeyCode.E) && !isAnimating)
                {
                    Debug.Log("E presionada - abriendo puerta");
                    ToggleDoor();
                }
            }
        }
    }
    
    bool IsPartOfThisDoor(Transform hitTransform)
    {
        // Subir por la jerarquía para ver si encontramos esta puerta
        Transform current = hitTransform;
        while (current != null)
        {
            if (current == transform)
                return true;
            current = current.parent;
        }
        return false;
    }
    
    void ToggleDoor()
    {
        if (isOpen)
        {
            StartCoroutine(AnimateDoor(false));
        }
        else
        {
            if (openTowardsPlayer && playerCamera != null)
            {
                Vector3 toPlayer = playerCamera.transform.position - transform.position;
                float dot = Vector3.Dot(transform.forward, toPlayer);
                float angle = dot > 0 ? openAngle : -openAngle;
                openRotation = closedRotation * Quaternion.Euler(0, angle, 0);
            }
            StartCoroutine(AnimateDoor(true));
        }
    }
    
    IEnumerator AnimateDoor(bool opening)
    {
        isAnimating = true;
        
        if (opening && openSound != null)
            audioSource.PlayOneShot(openSound);
        else if (!opening && closeSound != null)
            audioSource.PlayOneShot(closeSound);
        
        Quaternion startRot = transform.localRotation;
        Quaternion endRot = opening ? openRotation : closedRotation;
        
        float elapsed = 0f;
        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / animationDuration;
            t = t * t * (3f - 2f * t); // Suavizado
            transform.localRotation = Quaternion.Lerp(startRot, endRot, t);
            yield return null;
        }
        
        transform.localRotation = endRot;
        isOpen = opening;
        isAnimating = false;
    }
    
    void OnGUI()
    {
        if (isLooking && promptStyle != null)
        {
            string texto = isOpen ? "Pulsa E para cerrar" : "Pulsa E para abrir";
            
            GUI.color = Color.black;
            GUI.Label(new Rect(Screen.width / 2 - 148, Screen.height / 2 + 52, 300, 50), texto, promptStyle);
            
            GUI.color = Color.white;
            GUI.Label(new Rect(Screen.width / 2 - 150, Screen.height / 2 + 50, 300, 50), texto, promptStyle);
        }
    }
}
