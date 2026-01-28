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

    // Variables privadas
    private CharacterController controller;
    private Vector3 moveDirection = Vector3.zero;
    private float rotationX = 0f;
    private bool isRunning = false;

    // Propiedad pública para saber si está en el suelo
    public bool IsGrounded => controller.isGrounded;
    public bool IsRunning => isRunning;
    public bool IsMoving => controller.velocity.magnitude > 0.1f;

    void Start()
    {
        controller = GetComponent<CharacterController>();

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
    }

    void Update()
    {
        HandleMovement();
        HandleMouseLook();
        HandleCursor();
    }

    void HandleMovement()
    {
        // Obtener input de movimiento SIEMPRE
        float moveX = Input.GetAxisRaw("Horizontal"); // A/D - sin suavizado
        float moveZ = Input.GetAxisRaw("Vertical");   // W/S - sin suavizado

        // Verificar si está corriendo
        isRunning = Input.GetKey(KeyCode.LeftShift);
        float currentSpeed = isRunning ? runSpeed : walkSpeed;

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
