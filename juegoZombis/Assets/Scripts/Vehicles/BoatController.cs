using UnityEngine;

/// <summary>
/// Controlador de barco - Permite al jugador conducir el barco
/// Ponlo en el GameObject del barco
/// </summary>
public class BoatController : MonoBehaviour
{
    [Header("=== CONFIGURACIÓN DE MOVIMIENTO ===")]
    [Tooltip("Velocidad máxima del barco")]
    public float maxSpeed = 15f;
    [Tooltip("Aceleración del barco")]
    public float acceleration = 5f;
    [Tooltip("Desaceleración cuando no se pulsa nada")]
    public float deceleration = 3f;
    [Tooltip("Velocidad de giro")]
    public float turnSpeed = 30f;
    [Tooltip("El barco solo gira cuando se mueve")]
    public bool turnOnlyWhenMoving = true;
    [Tooltip("Usar la dirección de la cámara para moverse (recomendado)")]
    public bool useCameraDirection = true;
    
    [Header("=== POSICIÓN DEL CONDUCTOR ===")]
    [Tooltip("Punto donde se sienta el jugador")]
    public Transform driverSeat;
    [Tooltip("Punto donde sale el jugador al bajar")]
    public Transform exitPoint;
    
    [Header("=== EFECTOS (Opcional) ===")]
    [Tooltip("Partículas de agua/estela")]
    public ParticleSystem wakeEffect;
    [Tooltip("Sonido del motor")]
    public AudioClip engineSound;
    [Tooltip("Sonido al arrancar")]
    public AudioClip startSound;
    
    [Header("=== FÍSICA DEL AGUA ===")]
    [Tooltip("Simular flotación (subir/bajar suavemente)")]
    public bool simulateFloating = true;
    [Tooltip("Amplitud del movimiento de flotación")]
    public float floatAmplitude = 0.15f;
    [Tooltip("Velocidad de flotación")]
    public float floatSpeed = 1.5f;
    [Tooltip("Solo permitir movimiento sobre agua (con tag 'Water')")]
    public bool onlyMoveOnWater = true;
    [Tooltip("Distancia para detectar agua debajo del barco")]
    public float waterCheckDistance = 5f;
    
    [Header("=== CÁMARA ===")]
    [Tooltip("Usar cámara en tercera persona al conducir")]
    public bool useBoatCamera = true;
    [Tooltip("Cámara del barco (créala manualmente como hija del barco)")]
    public Camera boatCamera;
    
    // Estados
    private bool isBeingDriven = false;
    private float currentSpeed = 0f;
    private Transform driver; // El jugador
    private CharacterController driverController;
    private MonoBehaviour driverMovementScript; // Script de movimiento del jugador
    private Camera playerCamera;
    private bool isOnWater = true;
    
    // Para flotación
    private Vector3 startPosition;
    private float floatOffset = 0f;
    
    // Audio
    private AudioSource audioSource;
    private bool engineRunning = false;
    
    // Referencia al script de interacción
    private BoatInteraction interaction;

    void Start()
    {
        startPosition = transform.position;
        
        // Crear AudioSource si no existe
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.loop = true;
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1f;
        }
        
        // Buscar script de interacción
        interaction = GetComponent<BoatInteraction>();
        
        // Crear posición del conductor si no existe
        if (driverSeat == null)
        {
            GameObject seat = new GameObject("DriverSeat");
            seat.transform.SetParent(transform);
            seat.transform.localPosition = new Vector3(0, 1.5f, 0);
            driverSeat = seat.transform;
        }
        
        // Crear punto de salida si no existe
        if (exitPoint == null)
        {
            GameObject exit = new GameObject("ExitPoint");
            exit.transform.SetParent(transform);
            exit.transform.localPosition = new Vector3(2f, 1f, 0);
            exitPoint = exit.transform;
        }
        
        // Desactivar cámara del barco al inicio
        if (boatCamera != null)
            boatCamera.gameObject.SetActive(false);
    }

    void Update()
    {
        // Flotación
        if (simulateFloating)
        {
            floatOffset = Mathf.Sin(Time.time * floatSpeed) * floatAmplitude;
        }
        
        if (isBeingDriven)
        {
            HandleDriving();
            
            // Mantener al jugador en el asiento
            if (driver != null && driverSeat != null)
            {
                driver.position = driverSeat.position;
                driver.rotation = driverSeat.rotation;
            }
        }
        
        // Aplicar flotación
        if (simulateFloating && !isBeingDriven)
        {
            Vector3 pos = transform.position;
            pos.y = startPosition.y + floatOffset;
            transform.position = pos;
        }
    }
    
    void HandleDriving()
    {
        // Verificar si hay agua debajo
        if (onlyMoveOnWater)
        {
            isOnWater = CheckForWater();
        }
        
        // Input de movimiento
        float moveInput = Input.GetAxis("Vertical"); // W/S o Flechas
        float turnInput = Input.GetAxis("Horizontal"); // A/D o Flechas
        
        // Si no hay agua y está activada la restricción, frenar gradualmente
        if (onlyMoveOnWater && !isOnWater)
        {
            // Frenar rápidamente
            currentSpeed = Mathf.MoveTowards(currentSpeed, 0, deceleration * 3f * Time.deltaTime);
            
            // No permitir acelerar hacia adelante si no hay agua
            if (moveInput > 0) moveInput = 0;
        }
        
        // Acelerar/Desacelerar
        if (moveInput != 0)
        {
            currentSpeed += moveInput * acceleration * Time.deltaTime;
            currentSpeed = Mathf.Clamp(currentSpeed, -maxSpeed * 0.5f, maxSpeed);
        }
        else
        {
            // Desacelerar gradualmente
            if (currentSpeed > 0)
            {
                currentSpeed -= deceleration * Time.deltaTime;
                if (currentSpeed < 0) currentSpeed = 0;
            }
            else if (currentSpeed < 0)
            {
                currentSpeed += deceleration * Time.deltaTime;
                if (currentSpeed > 0) currentSpeed = 0;
            }
        }
        
        // Girar (solo si se mueve o está configurado para girar siempre)
        if (!turnOnlyWhenMoving || Mathf.Abs(currentSpeed) > 0.1f)
        {
            transform.Rotate(0, turnInput * turnSpeed * Time.deltaTime, 0);
        }
        
        // Calcular la dirección de movimiento
        Vector3 moveDirection;
        
        if (useCameraDirection && boatCamera != null)
        {
            // Usar la dirección de la cámara (hacia donde mira la cámara)
            Vector3 cameraForward = boatCamera.transform.forward;
            cameraForward.y = 0; // Solo en el plano horizontal
            cameraForward.Normalize();
            moveDirection = cameraForward;
        }
        else
        {
            // Usar la dirección del barco
            moveDirection = transform.forward;
        }
        
        // Mover el barco
        Vector3 movement = moveDirection * currentSpeed * Time.deltaTime;
        
        // Aplicar flotación mientras se conduce
        if (simulateFloating)
        {
            movement.y = (startPosition.y + floatOffset) - transform.position.y;
        }
        
        transform.position += movement;
        
        // Actualizar posición base para flotación
        startPosition = new Vector3(transform.position.x, startPosition.y, transform.position.z);
        
        // Efectos de estela
        if (wakeEffect != null)
        {
            if (Mathf.Abs(currentSpeed) > 1f && !wakeEffect.isPlaying)
                wakeEffect.Play();
            else if (Mathf.Abs(currentSpeed) <= 1f && wakeEffect.isPlaying)
                wakeEffect.Stop();
        }
        
        // Sonido del motor
        if (engineSound != null && audioSource != null)
        {
            audioSource.pitch = 0.8f + (Mathf.Abs(currentSpeed) / maxSpeed) * 0.4f;
        }
    }
    
    /// <summary>
    /// Verifica si hay agua debajo del barco
    /// </summary>
    bool CheckForWater()
    {
        // Lanzar rayo hacia abajo y hacia adelante
        Vector3[] checkPoints = new Vector3[]
        {
            transform.position,                          // Centro
            transform.position + transform.forward * 3f  // Proa (adelante)
        };
        
        foreach (Vector3 point in checkPoints)
        {
            RaycastHit hit;
            if (Physics.Raycast(point + Vector3.up * 2f, Vector3.down, out hit, waterCheckDistance + 2f))
            {
                // Si golpea algo con tag "Water", un trigger, o tiene "water" en el nombre
                string objectName = hit.collider.gameObject.name.ToLower();
                if (hit.collider.CompareTag("Water") || hit.collider.isTrigger || objectName.Contains("water"))
                {
                    return true;
                }
                
                // Si NO golpea nada sólido (solo agua normalmente es trigger o no tiene collider)
                // Consideramos que hay agua si el raycast no golpea el terreno
            }
            else
            {
                // No golpeó nada = probablemente hay agua (sin collider)
                return true;
            }
        }
        
        // Golpeó algo sólido (tierra) = no hay agua
        return false;
    }
    
    /// <summary>
    /// El jugador se sube al barco
    /// </summary>
    public void EnterBoat(Transform playerTransform)
    {
        if (isBeingDriven) return;
        
        driver = playerTransform;
        isBeingDriven = true;
        currentSpeed = 0f;
        
        // Desactivar el CharacterController del jugador
        driverController = driver.GetComponent<CharacterController>();
        if (driverController != null)
        {
            driverController.enabled = false;
        }
        
        // Desactivar el script de movimiento del jugador
        driverMovementScript = driver.GetComponent<MonoBehaviour>();
        FirstPersonController fpc = driver.GetComponent<FirstPersonController>();
        if (fpc != null)
        {
            fpc.enabled = false;
            driverMovementScript = fpc;
        }
        
        // Guardar referencia a la cámara del jugador
        playerCamera = driver.GetComponentInChildren<Camera>();
        
        // Activar cámara del barco
        if (useBoatCamera && boatCamera != null)
        {
            // Desactivar cámara del jugador
            if (playerCamera != null) 
            {
                playerCamera.gameObject.SetActive(false);
            }
            
            // Desactivar AudioListener del jugador
            AudioListener playerListener = driver.GetComponentInChildren<AudioListener>();
            if (playerListener != null) playerListener.enabled = false;
            
            // Activar la cámara del barco
            boatCamera.gameObject.SetActive(true);
            
            Debug.Log("[BoatController] Cámara del barco activada");
        }
        
        // Decirle al HUD que use la cámara del barco para la brújula
        if (GameHUD.Instance != null)
        {
            Camera camToUse = boatCamera != null ? boatCamera : GetComponentInChildren<Camera>();
            if (camToUse != null)
                GameHUD.Instance.SetHeadingOverride(camToUse.transform);
            else
                GameHUD.Instance.SetHeadingOverride(transform); // Fallback: orientación del barco
        }
        
        // Sonido de arranque
        if (startSound != null)
        {
            AudioSource.PlayClipAtPoint(startSound, transform.position);
        }
        
        // Iniciar sonido del motor
        if (engineSound != null && audioSource != null)
        {
            audioSource.clip = engineSound;
            audioSource.Play();
            engineRunning = true;
        }
        
        Debug.Log("[BoatController] Jugador subió al barco");
    }
    
    /// <summary>
    /// El jugador se baja del barco
    /// </summary>
    public void ExitBoat()
    {
        if (!isBeingDriven || driver == null) return;
        
        isBeingDriven = false;
        
        // Buscar el mejor punto de salida (superficie sólida más cercana)
        Vector3 exitPosition = FindBestExitPosition();
        driver.position = exitPosition;
        
        // Reactivar el CharacterController
        if (driverController != null)
        {
            driverController.enabled = true;
        }
        
        // Reactivar el script de movimiento
        if (driverMovementScript != null)
        {
            driverMovementScript.enabled = true;
        }
        
        // Volver a la cámara del jugador
        if (useBoatCamera)
        {
            // Desactivar cámara del barco
            if (boatCamera != null)
            {
                boatCamera.gameObject.SetActive(false);
            }
            
            // Reactivar cámara del jugador
            if (playerCamera != null) 
            {
                playerCamera.gameObject.SetActive(true);
                
                // Pasar el override a la cámara del jugador ANTES de limpiarlo
                // para evitar frames donde Camera.main es null
                if (GameHUD.Instance != null)
                {
                    GameHUD.Instance.SetHeadingOverride(playerCamera.transform);
                }
            }
            
            // Reactivar AudioListener del jugador
            AudioListener playerListener = driver.GetComponentInChildren<AudioListener>(true);
            if (playerListener != null) playerListener.enabled = true;
        }
        
        // Parar sonido del motor
        if (audioSource != null && engineRunning)
        {
            audioSource.Stop();
            engineRunning = false;
        }
        
        // Limpiar override al siguiente frame (Camera.main ya estará disponible)
        if (GameHUD.Instance != null)
        {
            StartCoroutine(ClearHeadingOverrideNextFrame());
        }
        
        driver = null;
        driverController = null;
        driverMovementScript = null;
        
        Debug.Log("[BoatController] Jugador bajó del barco");
    }
    
    /// <summary>
    /// Busca el mejor punto de salida (superficie sólida más cercana)
    /// Lanza rayos en varias direcciones y elige la que tenga suelo
    /// </summary>
    Vector3 FindBestExitPosition()
    {
        float checkDistance = 6f; // Distancia máxima para buscar (aumentado)
        float checkHeight = 5f;   // Altura desde donde lanzar el rayo (aumentado)
        
        // Direcciones a comprobar (derecha, izquierda, adelante, atrás, diagonales)
        Vector3[] directions = new Vector3[]
        {
            transform.right,           // Derecha
            -transform.right,          // Izquierda
            transform.forward,         // Adelante
            -transform.forward,        // Atrás
            (transform.right + transform.forward).normalized,    // Diagonal derecha-adelante
            (-transform.right + transform.forward).normalized,   // Diagonal izquierda-adelante
            (transform.right - transform.forward).normalized,    // Diagonal derecha-atrás
            (-transform.right - transform.forward).normalized    // Diagonal izquierda-atrás
        };
        
        Vector3 bestPosition = transform.position + Vector3.up * 2f; // Posición por defecto
        float closestDistance = float.MaxValue;
        bool foundGround = false;
        
        foreach (Vector3 dir in directions)
        {
            // Probar a diferentes distancias
            for (float dist = 2f; dist <= checkDistance; dist += 1.5f)
            {
                // Punto de inicio del raycast (al lado del barco, elevado)
                Vector3 checkPoint = transform.position + dir * dist + Vector3.up * checkHeight;
                
                // Lanzar rayo hacia abajo para buscar suelo
                RaycastHit hit;
                if (Physics.Raycast(checkPoint, Vector3.down, out hit, checkHeight + 5f))
                {
                    // Verificar que no sea agua
                    string objectName = hit.collider.gameObject.name.ToLower();
                    bool isWater = hit.collider.CompareTag("Water") || hit.collider.isTrigger || objectName.Contains("water");
                    
                    if (!isWater)
                    {
                        float distanceFromBoat = Vector3.Distance(transform.position, hit.point);
                        
                        // Elegir el punto más cercano al barco que tenga suelo
                        if (distanceFromBoat < closestDistance)
                        {
                            closestDistance = distanceFromBoat;
                            bestPosition = hit.point + Vector3.up * 1f; // Un poco elevado
                            foundGround = true;
                            Debug.Log("[BoatController] Suelo encontrado en: " + hit.collider.name);
                        }
                    }
                }
            }
        }
        
        // Si no encontró suelo, usar el exitPoint configurado o posición por defecto
        if (!foundGround)
        {
            if (exitPoint != null)
            {
                bestPosition = exitPoint.position;
            }
            else
            {
                bestPosition = transform.position + transform.right * 2f + Vector3.up;
            }
            Debug.LogWarning("[BoatController] No se encontró suelo cercano, usando posición por defecto");
        }
        else
        {
            Debug.Log("[BoatController] Salida encontrada a " + closestDistance.ToString("F1") + "m del barco");
        }
        
        return bestPosition;
    }
    
    /// <summary>
    /// Verifica si el barco está siendo conducido
    /// </summary>
    public bool IsBeingDriven()
    {
        return isBeingDriven;
    }
    
    /// <summary>
    /// Obtiene la velocidad actual
    /// </summary>
    public float GetCurrentSpeed()
    {
        return currentSpeed;
    }
    
    void OnDrawGizmosSelected()
    {
        // Dibujar posición del conductor
        if (driverSeat != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(driverSeat.position, 0.5f);
            Gizmos.DrawLine(transform.position, driverSeat.position);
        }
        
        // Dibujar punto de salida
        if (exitPoint != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(exitPoint.position, 0.5f);
        }
        
        // Dibujar dirección del barco
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(transform.position, transform.forward * 3f);
    }
    
    System.Collections.IEnumerator ClearHeadingOverrideNextFrame()
    {
        // Esperar 2 frames para asegurar que Camera.main ya detecta la cámara del jugador
        yield return null;
        yield return null;
        if (GameHUD.Instance != null)
        {
            GameHUD.Instance.SetHeadingOverride(null);
        }
    }
}
