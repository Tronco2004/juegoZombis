using UnityEngine;
using UnityEngine.AI;
using System.Collections;

/// <summary>
/// Puertas dobles interactivas - Se abren las dos a la vez hacia afuera del jugador
/// Pon este script en un GameObject PADRE vacío que contenga las dos puertas
/// </summary>
public class DoubleDoor : MonoBehaviour
{
    [Header("=== PUERTAS ===")]
    [Tooltip("Puerta izquierda (arrastra aquí)")]
    public Transform leftDoor;
    [Tooltip("Puerta derecha (arrastra aquí)")]
    public Transform rightDoor;
    
    [Header("=== CONFIGURACIÓN ===")]
    [Tooltip("Ángulo de apertura de las puertas")]
    public float openAngle = 90f;
    [Tooltip("Velocidad de apertura/cierre")]
    public float openSpeed = 3f;
    [Tooltip("Distancia máxima para interactuar")]
    public float interactionRange = 0.8f;
    [Tooltip("Tecla para interactuar")]
    public KeyCode interactKey = KeyCode.E;
    
    [Header("=== DIRECCIÓN DE APERTURA ===")]
    [Tooltip("Si está activado, las puertas se abren siempre hacia el lado contrario al jugador (se abren alejándose de ti, funciona desde ambos lados). Si está desactivado, siempre se abren en la misma dirección fija.")]
    public bool alwaysOpenOutward = true;
    [Tooltip("Invertir la dirección de apertura")]
    public bool invertOpenDirection = false;
    
    [Header("=== EJE DE ROTACIÓN (Visagras) ===")]
    [Tooltip("Eje sobre el que rotan las puertas cuando usan visagras. Por defecto Y (0,1,0) = giro horizontal. Prueba (1,0,0) si gira mal.")]
    public Vector3 hingeAxis = Vector3.up;
    
    [Header("=== PRECIO (Opcional) ===")]
    [Tooltip("Precio para abrir (0 = gratis)")]
    public int price = 0;

    [Header("=== NAVMESH (Zombies pasan por puertas abiertas) ===")]
    [Tooltip("Activar para que los zombies puedan pasar cuando la puerta está abierta. Añade NavMeshObstacle con Carve a cada puerta.")]
    public bool useNavMeshObstacle = true;
    [Tooltip("Tamaño del obstáculo NavMesh para cada puerta (ajustar según el tamaño de la puerta)")]
    public Vector3 obstacleSize = new Vector3(1.5f, 2.5f, 0.3f);
    [Tooltip("Centro del obstáculo NavMesh relativo a cada puerta")]
    public Vector3 obstacleCenter = Vector3.zero;

    [Header("=== AUDIO (Opcional) ===")]
    public AudioClip openSound;
    public AudioClip closeSound;
    public AudioClip lockedSound; // Sonido cuando está bloqueada

    // Estados
    private bool isOpen = false;
    private bool isAnimating = false;
    private bool isPurchased = false;
    private bool playerInRange = false;
    private bool isLocked = false; // Si está bloqueada, no se puede abrir
    private Transform player;
    
    // NavMeshObstacles para cada puerta
    private NavMeshObstacle leftObstacle;
    private NavMeshObstacle rightObstacle;
    
    // Rotaciones originales
    private Quaternion leftClosedRotation;
    private Quaternion rightClosedRotation;
    
    // Visagras (puntos de bisagra)
    private Transform leftVisagra;
    private Transform rightVisagra;
    private Vector3 leftClosedPos;
    private Vector3 rightClosedPos;
    
    private AudioSource audioSource;
    private GUIStyle promptStyle;
    private GUIStyle shadowStyle;

    void Start()
    {
        // Guardar rotaciones cerradas
        if (leftDoor != null)
        {
            leftClosedRotation = leftDoor.localRotation;
            leftClosedPos = leftDoor.position;
            // Buscar visagra dentro de la puerta izquierda
            leftVisagra = FindVisagra(leftDoor, "visagraL");
            if (leftVisagra != null)
                Debug.Log("[DoubleDoor] Visagra izquierda encontrada: " + leftVisagra.name);
        }
        if (rightDoor != null)
        {
            rightClosedRotation = rightDoor.localRotation;
            rightClosedPos = rightDoor.position;
            // Buscar visagra dentro de la puerta derecha
            rightVisagra = FindVisagra(rightDoor, "visagraR");
            if (rightVisagra != null)
                Debug.Log("[DoubleDoor] Visagra derecha encontrada: " + rightVisagra.name);
        }
        
        // Buscar jugador
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }
        else
        {
            // Buscar por PlayerMoney
            PlayerMoney pm = FindObjectOfType<PlayerMoney>();
            if (pm != null) player = pm.transform;
        }
        
        // Configurar audio
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        // Configurar siempre el AudioSource
        audioSource.spatialBlend = 0.5f; // Mezcla 2D/3D para que se escuche bien
        audioSource.playOnAwake = false;
        audioSource.volume = 1f;
        audioSource.minDistance = 1f;
        audioSource.maxDistance = 15f;
        
        // Si el precio es 0, ya está "comprada"
        if (price <= 0)
        {
            isPurchased = true;
        }

        // Configurar NavMeshObstacles para que los zombies puedan pasar cuando la puerta está abierta
        if (useNavMeshObstacle)
        {
            SetupNavMeshObstacles();
        }

        // Estilos de texto
        promptStyle = new GUIStyle();
        promptStyle.fontSize = 26;
        promptStyle.fontStyle = FontStyle.Bold;
        promptStyle.alignment = TextAnchor.MiddleCenter;
        promptStyle.normal.textColor = Color.white;

        shadowStyle = new GUIStyle(promptStyle);
        shadowStyle.normal.textColor = Color.black;
        
        Debug.Log("[DoubleDoor] Puertas dobles configuradas. Left: " + (leftDoor != null) + " | Right: " + (rightDoor != null)
            + " | VisagraL: " + (leftVisagra != null) + " | VisagraR: " + (rightVisagra != null));
    }

    /// <summary>
    /// Busca una visagra por nombre dentro de una puerta (recursivo)
    /// </summary>
    private Transform FindVisagra(Transform door, string preferredName)
    {
        // Buscar por nombre exacto
        foreach (Transform child in door)
        {
            if (child.name.Equals(preferredName, System.StringComparison.OrdinalIgnoreCase))
                return child;
        }
        // Buscar conteniendo "visagra" o "bisagra"
        foreach (Transform child in door)
        {
            string lower = child.name.ToLower();
            if (lower.Contains("visagra") || lower.Contains("bisagra") || lower.Contains("visabra"))
                return child;
        }
        // Buscar recursivo
        foreach (Transform child in door)
        {
            Transform found = FindVisagra(child, preferredName);
            if (found != null) return found;
        }
        return null;
    }

    void Update()
    {
        if (player == null) return;
        
        // Verificar distancia al punto central (el padre)
        float distance = GetClosestDistance();
        playerInRange = distance <= interactionRange;
        
        // Interacción
        if (playerInRange && Input.GetKeyDown(interactKey) && !isAnimating)
        {
            TryInteract();
        }
    }
    
    float GetClosestDistance()
    {
        if (player == null) return float.MaxValue;
        
        float dist = Vector3.Distance(transform.position, player.position);
        
        // También verificar distancia a cada puerta
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
        // Si está bloqueada, no se puede abrir
        if (isLocked)
        {
            PlaySound(lockedSound);
            Debug.Log("[DoubleDoor] Puerta bloqueada");
            return;
        }
        
        // Si tiene precio y no está comprada
        if (price > 0 && !isPurchased)
        {
            if (PlayerMoney.Instance != null && PlayerMoney.Instance.SpendMoney(price))
            {
                isPurchased = true;
                Debug.Log("[DoubleDoor] Puertas compradas por $" + price);
                ToggleDoors();
            }
            else
            {
                Debug.Log("[DoubleDoor] No tienes suficiente dinero. Necesitas $" + price);
            }
        }
        else
        {
            ToggleDoors();
        }
    }

    void ToggleDoors()
    {
        if (isAnimating) return;
        StartCoroutine(AnimateDoors(!isOpen));
    }
    
    /// <summary>
    /// Fuerza la apertura de las puertas desde un script externo (ej: CuadroElectrico)
    /// </summary>
    public void ForceOpen()
    {
        if (!isOpen && !isAnimating)
        {
            isPurchased = true; // Saltarse el precio
            isLocked = false;   // Desbloquear si estaba bloqueada
            StartCoroutine(AnimateDoors(true));
            Debug.Log("[DoubleDoor] Puertas abiertas forzosamente (externo)");
            
            // Quitar marker de la brújula al abrir la puerta
            if (GameHUD.Instance != null)
            {
                GameHUD.Instance.RemoveCompassMarker("puerta_electrica");
            }
        }
    }

    /// <summary>
    /// Fuerza el cierre de las puertas (usado por TrapHouseTrigger)
    /// </summary>
    public void ForceClose()
    {
        if (isOpen && !isAnimating)
        {
            StartCoroutine(AnimateDoors(false));
            Debug.Log("[DoubleDoor] Puertas cerradas forzosamente");
        }
    }
    
    /// <summary>
    /// Bloquea las puertas para que no se puedan abrir
    /// </summary>
    public void LockDoors()
    {
        isLocked = true;
        Debug.Log("[DoubleDoor] Puertas bloqueadas");
    }
    
    /// <summary>
    /// Desbloquea las puertas
    /// </summary>
    public void UnlockDoors()
    {
        isLocked = false;
        Debug.Log("[DoubleDoor] Puertas desbloqueadas");
    }

    IEnumerator AnimateDoors(bool opening)
    {
        isAnimating = true;
        
        // Al abrir → desactivar obstáculos para que los zombies puedan pasar
        // Al cerrar → se reactivan al final de la animación
        if (opening)
        {
            StartCoroutine(SetNavMeshObstaclesActiveCoroutine(false));
        }
        
        // Determinar la dirección de apertura
        float direction;
        
        if (alwaysOpenOutward && player != null)
        {
            // Abrir alejándose del jugador: detecta de qué lado está el jugador
            // y abre hacia el lado contrario (funciona desde ambos lados)
            Vector3 doorForward = transform.forward;
            Vector3 toPlayer = (player.position - transform.position).normalized;
            float dot = Vector3.Dot(doorForward, toPlayer);
            // Si el jugador está delante (dot > 0), abrir hacia atrás (-1)
            // Si el jugador está detrás (dot < 0), abrir hacia delante (1)
            direction = dot > 0 ? -1f : 1f;
        }
        else
        {
            // Dirección fija: siempre abrir hacia el forward del padre
            direction = 1f;
        }
        
        // Opción para invertir la dirección
        if (invertOpenDirection)
        {
            direction = -direction;
        }
        
        // Calcular rotaciones objetivo
        // Puerta izquierda gira en sentido contrario a la derecha
        Quaternion leftTarget, rightTarget;
        
        if (opening)
        {
            // Abrir: cada puerta gira hacia afuera (alejándose del centro)
            leftTarget = leftClosedRotation * Quaternion.Euler(0, -openAngle * direction, 0);
            rightTarget = rightClosedRotation * Quaternion.Euler(0, openAngle * direction, 0);
        }
        else
        {
            // Cerrar: volver a la posición original
            leftTarget = leftClosedRotation;
            rightTarget = rightClosedRotation;
        }
        
        // Sonido
        PlaySound(opening ? openSound : closeSound);
        
        // Sonido
        // (ya se reprodujo arriba)
        
        // Si hay visagras, usar RotateAround; si no, usar localRotation clásica
        bool useVisagras = (leftVisagra != null || rightVisagra != null);
        
        float elapsed = 0f;
        float duration = 1f / openSpeed;
        float leftAngleAccum = 0f;
        float rightAngleAccum = 0f;
        float leftTargetAngle = opening ? -openAngle * direction : 0f;
        float rightTargetAngle = opening ? openAngle * direction : 0f;
        
        // Para el modo clásico (sin visagras)
        Quaternion leftStart = leftDoor != null ? leftDoor.localRotation : Quaternion.identity;
        Quaternion rightStart = rightDoor != null ? rightDoor.localRotation : Quaternion.identity;
        
        // Resetear posición/rotación al cerrar con visagras
        if (!opening && useVisagras)
        {
            // Guardar ángulos actuales para interpolar a 0
            leftAngleAccum = 0f; // Se calculará con t
            rightAngleAccum = 0f;
        }
        
        // Guardar estado previo para RotateAround incremental
        float prevT = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0, 1, elapsed / duration);
            
            if (useVisagras)
            {
                // RotateAround incremental: rotar solo el delta desde el frame anterior
                float deltaT = t - prevT;
                
                if (leftDoor != null && leftVisagra != null)
                {
                    leftDoor.RotateAround(leftVisagra.position, hingeAxis, opening ? leftTargetAngle * deltaT : -leftTargetAngle * deltaT);
                }
                else if (leftDoor != null)
                {
                    leftDoor.localRotation = Quaternion.Slerp(leftStart, leftTarget, t);
                }
                
                if (rightDoor != null && rightVisagra != null)
                {
                    rightDoor.RotateAround(rightVisagra.position, hingeAxis, opening ? rightTargetAngle * deltaT : -rightTargetAngle * deltaT);
                }
                else if (rightDoor != null)
                {
                    rightDoor.localRotation = Quaternion.Slerp(rightStart, rightTarget, t);
                }
                
                prevT = t;
            }
            else
            {
                // Modo clásico sin visagras
                if (leftDoor != null)
                    leftDoor.localRotation = Quaternion.Slerp(leftStart, leftTarget, t);
                if (rightDoor != null)
                    rightDoor.localRotation = Quaternion.Slerp(rightStart, rightTarget, t);
            }
            
            yield return null;
        }
        
        // Asegurar posición/rotación final
        if (!useVisagras)
        {
            if (leftDoor != null) leftDoor.localRotation = leftTarget;
            if (rightDoor != null) rightDoor.localRotation = rightTarget;
        }
        else if (!opening)
        {
            // Al cerrar, asegurar que vuelven a la posición original exacta
            if (leftDoor != null)
            {
                leftDoor.localRotation = leftClosedRotation;
                leftDoor.position = leftClosedPos;
            }
            if (rightDoor != null)
            {
                rightDoor.localRotation = rightClosedRotation;
                rightDoor.position = rightClosedPos;
            }
        }
        
        isOpen = opening;
        isAnimating = false;
        
        // Al cerrar → reactivar obstáculos para bloquear a los zombies
        if (!opening)
        {
            SetNavMeshObstaclesActive(true);
        }
        
        Debug.Log("[DoubleDoor] Puertas " + (isOpen ? "abiertas" : "cerradas"));
    }

    /// <summary>
    /// Desactiva los obstáculos con delay para que el NavMesh se actualice
    /// Luego notifica a los zombis cercanos para que recalculen rutas
    /// </summary>
    IEnumerator SetNavMeshObstaclesActiveCoroutine(bool active)
    {
        if (leftObstacle != null) leftObstacle.enabled = active;
        if (rightObstacle != null) rightObstacle.enabled = active;
        
        Debug.Log("[DoubleDoor] NavMeshObstacles " + (active ? "ACTIVADOS (bloqueando)" : "DESACTIVADOS (paso libre)"));
        
        // Dar tiempo al NavMesh para recalcular
        yield return new WaitForSeconds(0.1f);
        
        // Si se abrieron, notificar a zombis cercanos que recalculen rutas
        if (!active)
        {
            NotifyNearbyZombies();
        }
    }

    /// <summary>
    /// Notifica a los zombis cercanos que recalculen su ruta
    /// (soluciona el problema de quedarse atrapados en puertas)
    /// </summary>
    void NotifyNearbyZombies()
    {
        float notifyRadius = 20f; // Radio de búsqueda
        
        // Buscar todos los ZombieAI cercanos
        ZombieAI[] zombies = FindObjectsOfType<ZombieAI>();
        
        int notifiedCount = 0;
        foreach (ZombieAI zombie in zombies)
        {
            if (zombie == null) continue;
            
            float distance = Vector3.Distance(zombie.transform.position, transform.position);
            if (distance <= notifyRadius)
            {
                // Forzar recálculo de ruta al zombi
                NavMeshAgent agent = zombie.GetComponent<NavMeshAgent>();
                if (agent != null && agent.isActiveAndEnabled)
                {
                    agent.ResetPath(); // Limpiar ruta actual
                    agent.CalculatePath(agent.destination, new NavMeshPath()); // Recalcular
                    notifiedCount++;
                }
            }
        }
        
        if (notifiedCount > 0)
        {
            Debug.Log("[DoubleDoor] Notificados " + notifiedCount + " zombis para recalcular rutas");
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
            leftObstacle.enabled = true; // Empezar bloqueando (puerta cerrada)
            Debug.Log("[DoubleDoor] NavMeshObstacle configurado en puerta izquierda");
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
            rightObstacle.enabled = true; // Empezar bloqueando (puerta cerrada)
            Debug.Log("[DoubleDoor] NavMeshObstacle configurado en puerta derecha");
        }
    }

    /// <summary>
    /// Activa o desactiva los NavMeshObstacles de las puertas
    /// </summary>
    void SetNavMeshObstaclesActive(bool active)
    {
        if (leftObstacle != null) leftObstacle.enabled = active;
        if (rightObstacle != null) rightObstacle.enabled = active;
        Debug.Log("[DoubleDoor] NavMeshObstacles " + (active ? "activados (bloqueando)" : "desactivados (paso libre)"));
    }

    void PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null)
        {
            audioSource.Stop(); // Detener cualquier sonido anterior
            audioSource.clip = clip;
            audioSource.Play();
            Debug.Log("[DoubleDoor] Reproduciendo sonido: " + clip.name);
        }
        else
        {
            Debug.LogWarning("[DoubleDoor] No se pudo reproducir sonido. Clip: " + (clip != null) + " | AudioSource: " + (audioSource != null));
        }
    }

    // TEXTO EN PANTALLA
    void OnGUI()
    {
        if (!playerInRange || isAnimating) return;
        
        if (promptStyle == null)
        {
            promptStyle = new GUIStyle();
            promptStyle.fontSize = 26;
            promptStyle.fontStyle = FontStyle.Bold;
            promptStyle.alignment = TextAnchor.MiddleCenter;
            promptStyle.normal.textColor = Color.white;

            shadowStyle = new GUIStyle(promptStyle);
            shadowStyle.normal.textColor = Color.black;
        }

        string texto;
        if (price > 0 && !isPurchased)
        {
            texto = "Pulsa E - Abrir puertas ($" + price + ")";
        }
        else
        {
            texto = isOpen ? "Pulsa E - Cerrar puertas" : "Pulsa E - Abrir puertas";
        }

        float x = Screen.width / 2f - 200;
        float y = Screen.height / 2f + 60;

        GUI.color = Color.black;
        GUI.Label(new Rect(x + 2, y + 2, 400, 50), texto, shadowStyle);

        GUI.color = Color.yellow;
        GUI.Label(new Rect(x, y, 400, 50), texto, promptStyle);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, interactionRange);
        
        // Mostrar dirección "adelante"
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(transform.position, transform.forward * 2f);
        Gizmos.color = Color.red;
        Gizmos.DrawRay(transform.position, -transform.forward * 2f);
    }
}
