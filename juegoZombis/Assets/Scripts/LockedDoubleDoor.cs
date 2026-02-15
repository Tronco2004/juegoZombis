using UnityEngine;
using UnityEngine.AI;
using System.Collections;

/// <summary>
/// Puertas dobles con cerradura - Requiere una llave específica para abrir
/// Similar a DoubleDoor pero con sistema de llave
/// </summary>
public class LockedDoubleDoor : MonoBehaviour
{
    [Header("=== PUERTAS ===")]
    public Transform leftDoor;
    public Transform rightDoor;
    
    [Header("=== CONFIGURACIÓN ===")]
    public float openAngle = 90f;
    public float openSpeed = 2f;
    public float interactionRange = 1.5f;
    public KeyCode interactKey = KeyCode.E;
    
    [Header("=== CERRADURA ===")]
    [Tooltip("Nombre de la llave requerida (debe coincidir con KeyItem)")]
    public string requiredKeyName = "LlaveCasa";
    [Tooltip("¿Consumir la llave al usarla?")]
    public bool consumeKey = false;
    [Tooltip("Mensaje cuando está cerrada sin llave")]
    public string lockedMessage = "Puerta cerrada - Necesitas una llave";
    [Tooltip("Mensaje cuando tienes la llave")]
    public string hasKeyMessage = "Pulsa E - Abrir con llave";
    
    [Header("=== DIRECCIÓN ===")]
    public bool alwaysOpenOutward = true;
    public bool invertOpenDirection = false;

    [Header("=== NAVMESH (Zombies pasan por puertas abiertas) ===")]
    [Tooltip("Activar para que los zombies puedan pasar cuando la puerta está abierta")]
    public bool useNavMeshObstacle = true;
    [Tooltip("Tamaño del obstáculo NavMesh para cada puerta")]
    public Vector3 obstacleSize = new Vector3(1.5f, 2.5f, 0.3f);
    [Tooltip("Centro del obstáculo NavMesh relativo a cada puerta")]
    public Vector3 obstacleCenter = Vector3.zero;

    [Header("=== AUDIO ===")]
    public AudioClip openSound;
    public AudioClip closeSound;
    public AudioClip lockedSound; // Sonido cuando intenta abrir sin llave
    public AudioClip unlockSound; // Sonido al desbloquear

    private bool isOpen = false;
    private bool isAnimating = false;
    private bool isUnlocked = false;
    private bool playerInRange = false;
    private Transform player;
    
    private Quaternion leftClosedRotation;
    private Quaternion rightClosedRotation;
    
    private AudioSource audioSource;
    private GUIStyle promptStyle;
    private GUIStyle shadowStyle;
    
    // NavMeshObstacles para cada puerta
    private NavMeshObstacle leftObstacle;
    private NavMeshObstacle rightObstacle;

    void Start()
    {
        if (leftDoor != null)
            leftClosedRotation = leftDoor.localRotation;
        if (rightDoor != null)
            rightClosedRotation = rightDoor.localRotation;
        
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }
        else
        {
            PlayerMoney pm = FindObjectOfType<PlayerMoney>();
            if (pm != null) player = pm.transform;
        }
        
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.spatialBlend = 0.5f;
        audioSource.playOnAwake = false;
        audioSource.volume = 1f;

        // Configurar NavMeshObstacles para que los zombies puedan pasar cuando la puerta está abierta
        if (useNavMeshObstacle)
        {
            SetupNavMeshObstacles();
        }

        promptStyle = new GUIStyle();
        promptStyle.fontSize = 26;
        promptStyle.fontStyle = FontStyle.Bold;
        promptStyle.alignment = TextAnchor.MiddleCenter;
        promptStyle.normal.textColor = Color.white;

        shadowStyle = new GUIStyle(promptStyle);
        shadowStyle.normal.textColor = Color.black;
    }

    void Update()
    {
        if (player == null) return;
        
        float distance = GetClosestDistance();
        playerInRange = distance <= interactionRange;
        
        if (playerInRange && Input.GetKeyDown(interactKey) && !isAnimating)
        {
            TryInteract();
        }
    }
    
    float GetClosestDistance()
    {
        if (player == null) return float.MaxValue;
        
        float dist = Vector3.Distance(transform.position, player.position);
        
        if (leftDoor != null)
        {
            float leftDist = Vector3.Distance(leftDoor.position, player.position);
            if (leftDist < dist) dist = leftDist;
        }
        if (rightDoor != null)
        {
            float rightDist = Vector3.Distance(rightDoor.position, player.position);
            if (rightDist < dist) dist = rightDist;
        }
        
        return dist;
    }

    void TryInteract()
    {
        // Si ya está desbloqueada, abrir/cerrar normalmente
        if (isUnlocked)
        {
            ToggleDoors();
            return;
        }
        
        // Verificar si tiene la llave
        if (PlayerInventory.Instance != null && PlayerInventory.Instance.HasKey(requiredKeyName))
        {
            // Desbloquear la puerta
            isUnlocked = true;
            
            // Consumir llave si está configurado
            if (consumeKey)
            {
                PlayerInventory.Instance.RemoveKey(requiredKeyName);
            }
            
            PlaySound(unlockSound);
            Debug.Log("[LockedDoubleDoor] ¡Puerta desbloqueada con " + requiredKeyName + "!");
            
            // Abrir automáticamente al desbloquear
            StartCoroutine(DelayedOpen());
        }
        else
        {
            // No tiene la llave
            PlaySound(lockedSound);
            Debug.Log("[LockedDoubleDoor] Puerta cerrada. Necesitas: " + requiredKeyName);
        }
    }
    
    IEnumerator DelayedOpen()
    {
        yield return new WaitForSeconds(0.3f);
        ToggleDoors();
    }

    void ToggleDoors()
    {
        if (isAnimating) return;
        StartCoroutine(AnimateDoors(!isOpen));
    }

    IEnumerator AnimateDoors(bool opening)
    {
        isAnimating = true;
        
        // Al abrir → desactivar obstáculos para que los zombies puedan pasar
        if (opening)
        {
            SetNavMeshObstaclesActive(false);
        }
        
        float direction;
        
        if (alwaysOpenOutward && player != null)
        {
            // Abrir alejándose del jugador: detecta de qué lado está
            Vector3 doorForward = transform.forward;
            Vector3 toPlayer = (player.position - transform.position).normalized;
            float dot = Vector3.Dot(doorForward, toPlayer);
            direction = dot > 0 ? -1f : 1f;
        }
        else
        {
            direction = 1f;
        }
        
        if (invertOpenDirection)
        {
            direction = -direction;
        }
        
        Quaternion leftTarget, rightTarget;
        
        if (opening)
        {
            leftTarget = leftClosedRotation * Quaternion.Euler(0, -openAngle * direction, 0);
            rightTarget = rightClosedRotation * Quaternion.Euler(0, openAngle * direction, 0);
        }
        else
        {
            leftTarget = leftClosedRotation;
            rightTarget = rightClosedRotation;
        }
        
        PlaySound(opening ? openSound : closeSound);
        
        Quaternion leftStart = leftDoor != null ? leftDoor.localRotation : Quaternion.identity;
        Quaternion rightStart = rightDoor != null ? rightDoor.localRotation : Quaternion.identity;
        
        float elapsed = 0f;
        float duration = 1f / openSpeed;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0, 1, elapsed / duration);
            
            if (leftDoor != null)
                leftDoor.localRotation = Quaternion.Slerp(leftStart, leftTarget, t);
            if (rightDoor != null)
                rightDoor.localRotation = Quaternion.Slerp(rightStart, rightTarget, t);
            
            yield return null;
        }
        
        if (leftDoor != null) leftDoor.localRotation = leftTarget;
        if (rightDoor != null) rightDoor.localRotation = rightTarget;
        
        isOpen = opening;
        isAnimating = false;
        
        // Al cerrar → reactivar obstáculos para bloquear a los zombies
        if (!opening)
        {
            SetNavMeshObstaclesActive(true);
        }
    }

    /// <summary>
    /// Configura NavMeshObstacles en las puertas para que los zombies detecten puertas abiertas/cerradas
    /// </summary>
    void SetupNavMeshObstacles()
    {
        if (leftDoor != null)
        {
            leftObstacle = leftDoor.GetComponent<NavMeshObstacle>();
            if (leftObstacle == null)
                leftObstacle = leftDoor.gameObject.AddComponent<NavMeshObstacle>();
            
            leftObstacle.shape = NavMeshObstacleShape.Box;
            leftObstacle.size = obstacleSize;
            leftObstacle.center = obstacleCenter;
            leftObstacle.carving = true;
            leftObstacle.carvingMoveThreshold = 0.1f;
            leftObstacle.carvingTimeToStationary = 0.2f;
            leftObstacle.enabled = true;
            Debug.Log("[LockedDoubleDoor] NavMeshObstacle configurado en puerta izquierda");
        }
        
        if (rightDoor != null)
        {
            rightObstacle = rightDoor.GetComponent<NavMeshObstacle>();
            if (rightObstacle == null)
                rightObstacle = rightDoor.gameObject.AddComponent<NavMeshObstacle>();
            
            rightObstacle.shape = NavMeshObstacleShape.Box;
            rightObstacle.size = obstacleSize;
            rightObstacle.center = obstacleCenter;
            rightObstacle.carving = true;
            rightObstacle.carvingMoveThreshold = 0.1f;
            rightObstacle.carvingTimeToStationary = 0.2f;
            rightObstacle.enabled = true;
            Debug.Log("[LockedDoubleDoor] NavMeshObstacle configurado en puerta derecha");
        }
    }

    /// <summary>
    /// Activa o desactiva los NavMeshObstacles de las puertas
    /// </summary>
    void SetNavMeshObstaclesActive(bool active)
    {
        if (leftObstacle != null) leftObstacle.enabled = active;
        if (rightObstacle != null) rightObstacle.enabled = active;
        Debug.Log("[LockedDoubleDoor] NavMeshObstacles " + (active ? "activados (bloqueando)" : "desactivados (paso libre)"));
    }

    void PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null)
        {
            audioSource.Stop();
            audioSource.clip = clip;
            audioSource.Play();
        }
    }

    void OnGUI()
    {
        if (!playerInRange || isAnimating) return;
        
        string texto;
        Color textColor;
        
        if (isUnlocked)
        {
            texto = isOpen ? "Pulsa E - Cerrar puertas" : "Pulsa E - Abrir puertas";
            textColor = Color.yellow;
        }
        else if (PlayerInventory.Instance != null && PlayerInventory.Instance.HasKey(requiredKeyName))
        {
            texto = hasKeyMessage;
            textColor = Color.green;
        }
        else
        {
            texto = lockedMessage;
            textColor = Color.red;
        }

        float x = Screen.width / 2f - 200;
        float y = Screen.height / 2f + 60;

        GUI.color = Color.black;
        GUI.Label(new Rect(x + 2, y + 2, 400, 50), texto, shadowStyle);

        GUI.color = textColor;
        GUI.Label(new Rect(x, y, 400, 50), texto, promptStyle);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = isUnlocked ? Color.green : Color.red;
        Gizmos.DrawWireSphere(transform.position, interactionRange);
    }
}
