using UnityEngine;

/// <summary>
/// Controlador de primera persona para el jugador
/// Requiere: CharacterController en el mismo GameObject
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class FirstPersonController : MonoBehaviour
{
    [Header("Movimiento")]
    [Tooltip("Velocidad al caminar")]
    public float walkSpeed = 4f;
    [Tooltip("Velocidad al correr")]
    public float runSpeed = 8f;
    [Tooltip("Fuerza del salto")]
    public float jumpForce = 8f;
    [Tooltip("Gravedad aplicada al jugador")]
    public float gravity = 20f;

    [Header("Cámara")]
    [Tooltip("Referencia a la cámara (hijo del jugador)")]
    public Transform playerCamera;
    [Tooltip("Sensibilidad del ratón")]
    public float mouseSensitivity = 2f;
    [Tooltip("Límite de rotación vertical hacia arriba")]
    public float maxLookUp = 80f;
    [Tooltip("Límite de rotación vertical hacia abajo")]
    public float maxLookDown = -80f;

    [Header("Sonidos (Opcional)")]
    public AudioSource footstepAudio;
    public AudioClip[] footstepSounds;

    [Header("Agacharse")]
    [Tooltip("Tecla para agacharse")]
    public KeyCode crouchKey = KeyCode.LeftControl;
    [Tooltip("Altura del jugador al estar de pie")]
    public float standingHeight = 2f;
    [Tooltip("Altura del jugador agachado")]
    public float crouchHeight = 1f;
    [Tooltip("Velocidad al agacharse")]
    public float crouchSpeed = 2f;
    [Tooltip("Velocidad de transición al agacharse/levantarse")]
    public float crouchTransitionSpeed = 10f;

    [Header("Stamina")]
    [Tooltip("Stamina máxima del jugador")]
    public float maxStamina = 100f;
    [Tooltip("Stamina actual del jugador")]
    public float currentStamina = 100f;
    [Tooltip("Stamina que se gasta por segundo al correr")]
    public float staminaDrainRate = 15f;
    [Tooltip("Stamina que se recupera por segundo al caminar")]
    public float staminaRegenWalking = 5f;
    [Tooltip("Stamina que se recupera por segundo al estar quieto")]
    public float staminaRegenIdle = 10f;

    // Variables privadas
    private CharacterController controller;
    private Vector3 moveDirection = Vector3.zero;
    private float rotationX = 0f;
    private bool isRunning = false;
    private bool isCrouching = false;
    private float currentHeight;
    private Vector3 cameraStandingPos;

    // Propiedad pública para saber si está en el suelo
    public bool IsGrounded => controller.isGrounded;
    public bool IsRunning => isRunning;
    public bool IsCrouching => isCrouching;
    public bool IsMoving => controller.velocity.magnitude > 0.1f;
    public float StaminaPercentage => currentStamina / maxStamina;
    public bool HasStamina => currentStamina >= 1f; // Mínimo 1 de stamina para poder correr

    void Start()
    {
        controller = GetComponent<CharacterController>();

        // Guardar la altura original del CharacterController (NO modificarla)
        if (standingHeight <= 0)
        {
            standingHeight = controller.height;
        }
        
        // Guardar la altura actual para referencia
        currentHeight = controller.height;
        
        // NO modificar la altura ni el centro del controller aquí
        // Dejar que Unity use los valores configurados en el Inspector

        // Bloquear y ocultar el cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Si no se asignó cámara, buscar en los hijos
        if (playerCamera == null)
        {
            Camera cam = GetComponentInChildren<Camera>();
            if (cam != null)
                playerCamera = cam.transform;
        }

        // Guardar posición original de la cámara
        if (playerCamera != null)
        {
            cameraStandingPos = playerCamera.localPosition;
        }
        
        Debug.Log("[FirstPersonController] Standing Height: " + standingHeight + " | Crouch Height: " + crouchHeight);
    }

    void Update()
    {
        HandleMovement();
        HandleCrouch();
        HandleMouseLook();
        HandleCursor();
        HandleStamina();
    }

    void HandleMovement()
    {
        // Obtener input de movimiento SIEMPRE
        float moveX = Input.GetAxisRaw("Horizontal"); // A/D - sin suavizado
        float moveZ = Input.GetAxisRaw("Vertical");   // W/S - sin suavizado

        // Verificar si está corriendo (solo si tiene stamina y NO está agachado)
        bool wantsToRun = Input.GetKey(KeyCode.LeftShift);
        bool isMoving = (moveX != 0 || moveZ != 0);
        isRunning = wantsToRun && HasStamina && isMoving && !isCrouching;
        
        // Velocidad según estado
        float currentSpeed;
        if (isCrouching)
            currentSpeed = crouchSpeed;
        else if (isRunning)
            currentSpeed = runSpeed;
        else
            currentSpeed = walkSpeed;

        // Calcular dirección de movimiento relativa al jugador
        Vector3 move = transform.right * moveX + transform.forward * moveZ;
        move = move.normalized * currentSpeed;

        // Mantener la velocidad Y (gravedad/salto)
        float verticalVelocity = moveDirection.y;

        // Aplicar movimiento horizontal
        moveDirection.x = move.x;
        moveDirection.z = move.z;

        // Si está en el suelo
        if (controller.isGrounded)
        {
            // Resetear velocidad vertical cuando toca el suelo
            verticalVelocity = -2f; // Pequeño valor negativo para mantenerlo pegado

            // Saltar
            if (Input.GetButtonDown("Jump"))
            {
                verticalVelocity = jumpForce;
            }
        }

        // Aplicar gravedad
        verticalVelocity -= gravity * Time.deltaTime;
        moveDirection.y = verticalVelocity;

        // Mover el personaje
        controller.Move(moveDirection * Time.deltaTime);
    }

    void HandleCrouch()
    {
        // Detectar si quiere agacharse (mantener presionado Control)
        bool wantsToCrouch = Input.GetKey(crouchKey);
        
        // Cambiar estado de agachado
        if (wantsToCrouch)
        {
            isCrouching = true;
        }
        else
        {
            // Solo levantarse si hay espacio
            if (CanStandUp())
            {
                isCrouching = false;
            }
        }
        
        // Altura objetivo
        float targetHeight = isCrouching ? crouchHeight : standingHeight;
        
        // Solo modificar si está agachándose o levantándose
        if (Mathf.Abs(controller.height - targetHeight) > 0.01f)
        {
            // Transición suave de altura
            float newHeight = Mathf.Lerp(controller.height, targetHeight, Time.deltaTime * crouchTransitionSpeed);
            
            // Calcular cuánto cambia la altura
            float heightChange = newHeight - controller.height;
            
            // Ajustar la altura
            controller.height = newHeight;
            
            // Mover el jugador hacia abajo/arriba para compensar el cambio de altura
            // Esto evita que "flote" o se hunda en el suelo
            if (isCrouching)
            {
                // Al agacharse, el centro baja
                transform.position += new Vector3(0, heightChange / 2f, 0);
            }
            else
            {
                // Al levantarse, el centro sube
                transform.position += new Vector3(0, heightChange / 2f, 0);
            }
        }
        
        // Ajustar posición de la cámara
        if (playerCamera != null)
        {
            float heightRatio = controller.height / standingHeight;
            Vector3 targetCamPos = new Vector3(cameraStandingPos.x, cameraStandingPos.y * heightRatio, cameraStandingPos.z);
            playerCamera.localPosition = Vector3.Lerp(playerCamera.localPosition, targetCamPos, Time.deltaTime * crouchTransitionSpeed);
        }
    }
    
    bool CanStandUp()
    {
        // Verificar si hay algo encima que impida levantarse
        float checkDistance = standingHeight - crouchHeight;
        Vector3 checkStart = transform.position + Vector3.up * controller.height;
        
        return !Physics.Raycast(checkStart, Vector3.up, checkDistance);
    }

    void HandleStamina()
    {
        // Obtener input para saber si se está moviendo
        float moveX = Input.GetAxisRaw("Horizontal");
        float moveZ = Input.GetAxisRaw("Vertical");
        bool isMoving = (moveX != 0 || moveZ != 0);

        float previousStamina = currentStamina;

        if (isRunning)
        {
            // Gastar stamina al correr
            float staminaLost = staminaDrainRate * Time.deltaTime;
            currentStamina -= staminaLost;
            
            // La stamina no puede ser negativa
            if (currentStamina < 0f)
            {
                currentStamina = 0f;
            }
            
            Debug.Log($"[STAMINA] Corriendo - Perdida: {staminaLost:F2} | Stamina actual: {currentStamina:F2}/{maxStamina}");
            
            // Si la stamina llega a 0, dejar de correr
            if (currentStamina <= 0f)
            {
                Debug.Log("[STAMINA] ¡Sin stamina! El jugador ya no puede correr.");
            }
        }
        else if (isMoving)
        {
            // Recuperar stamina lentamente al caminar (+5 por segundo)
            float staminaGained = staminaRegenWalking * Time.deltaTime;
            currentStamina += staminaGained;
            
            // No superar el máximo
            if (currentStamina > maxStamina)
            {
                currentStamina = maxStamina;
            }
            
            if (previousStamina < maxStamina)
            {
                Debug.Log($"[STAMINA] Caminando - Ganada: {staminaGained:F2} | Stamina actual: {currentStamina:F2}/{maxStamina}");
            }
        }
        else
        {
            // Recuperar stamina más rápido al estar quieto (+10 por segundo)
            float staminaGained = staminaRegenIdle * Time.deltaTime;
            currentStamina += staminaGained;
            
            // No superar el máximo
            if (currentStamina > maxStamina)
            {
                currentStamina = maxStamina;
            }
            
            if (previousStamina < maxStamina)
            {
                Debug.Log($"[STAMINA] Quieto - Ganada: {staminaGained:F2} | Stamina actual: {currentStamina:F2}/{maxStamina}");
            }
        }
    }

    void HandleMouseLook()
    {
        if (playerCamera == null) return;

        // Obtener input del ratón
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        // Rotación horizontal (rotar todo el cuerpo del jugador)
        transform.Rotate(Vector3.up * mouseX);

        // Rotación vertical (solo la cámara)
        rotationX -= mouseY;
        rotationX = Mathf.Clamp(rotationX, maxLookDown, maxLookUp);
        playerCamera.localRotation = Quaternion.Euler(rotationX, 0f, 0f);
    }

    void HandleCursor()
    {
        // Presionar Escape para liberar el cursor
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        // Click para volver a bloquear el cursor
        if (Input.GetMouseButtonDown(0) && Cursor.lockState != CursorLockMode.Locked)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    /// <summary>
    /// Método para cambiar la sensibilidad desde el menú de opciones
    /// </summary>
    public void SetMouseSensitivity(float sensitivity)
    {
        mouseSensitivity = sensitivity;
    }
}
