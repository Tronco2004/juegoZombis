using UnityEngine;
using System.Collections;

/// <summary>
/// Controlador de helicóptero — Controles arcade sencillos.
/// Ponlo en el GameObject raíz del helicóptero.
///
/// Controles (mientras pilotas):
///   W / S           → Avanzar / Retroceder
///   A / D           → Girar izquierda / derecha
///   Espacio         → Ascender
///   Left Ctrl       → Descender
///   Left Shift      → Turbo
///
/// La cámara se posiciona automáticamente detrás y arriba.
/// Si el modelo FBX mira hacia atrás, pon modelYawOffset = 180.
/// </summary>
public class HelicopterController : MonoBehaviour
{
    [Header("=== MOVIMIENTO ===")]
    [Tooltip("Velocidad máxima de avance")]
    public float maxSpeed = 30f;
    [Tooltip("Aceleración")]
    public float acceleration = 15f;
    [Tooltip("Frenado cuando no hay input")]
    public float deceleration = 8f;
    [Tooltip("Velocidad de giro (A/D)")]
    public float turnSpeed = 60f;
    [Tooltip("Multiplicador de turbo (Shift)")]
    public float turboMultiplier = 1.6f;

    [Header("=== ALTITUD ===")]
    [Tooltip("Velocidad de ascenso/descenso")]
    public float verticalSpeed = 12f;
    [Tooltip("Altura máxima permitida")]
    public float maxAltitude = 200f;
    [Tooltip("Altura mínima sobre el suelo")]
    public float minGroundClearance = 2f;
    [Tooltip("Gravedad cuando el motor está apagado")]
    public float gravity = 15f;

    [Header("=== INCLINACIÓN VISUAL ===")]
    [Tooltip("Ángulo de inclinación al avanzar/retroceder")]
    public float pitchTiltAngle = 15f;
    [Tooltip("Ángulo de inclinación al girar")]
    public float rollTiltAngle = 15f;
    [Tooltip("Suavizado de la inclinación")]
    public float tiltSmoothing = 5f;

    [Header("=== MODELO ===")]
    [Tooltip("Offset de yaw para compensar modelos FBX que miran hacia atrás (0 o 180)")]
    public float modelYawOffset = 180f;

    [Header("=== ROTORES ===")]
    [Tooltip("Transform del rotor principal (arriba)")]
    public Transform mainRotor;
    [Tooltip("Transform del rotor de cola")]
    public Transform tailRotor;
    [Tooltip("Velocidad de giro del rotor principal")]
    public float mainRotorSpeed = 1500f;
    [Tooltip("Velocidad de giro del rotor de cola")]
    public float tailRotorSpeed = 2500f;
    [Tooltip("Eje de rotación del rotor principal (normalmente Y)")]
    public Vector3 mainRotorAxis = Vector3.forward;
    [Tooltip("Eje de rotación del rotor de cola (normalmente X)")]
    public Vector3 tailRotorAxis = Vector3.right;

    [Header("=== POSICIONES ===")]
    [Tooltip("Punto donde se sienta el piloto")]
    public Transform pilotSeat;
    [Tooltip("Punto donde sale el jugador al bajar")]
    public Transform exitPoint;

    [Header("=== CÁMARA ===")]
    [Tooltip("Cámara del helicóptero (se crea automáticamente si está vacío)")]
    public Camera helicopterCamera;
    [Tooltip("Distancia de la cámara detrás del helicóptero")]
    public float cameraDistance = 6f;
    [Tooltip("Altura de la cámara sobre el helicóptero")]
    public float cameraHeight = 2.5f;
    [Tooltip("Altura del punto al que mira la cámara (sobre el centro del heli)")]
    public float cameraLookAtHeight = 1f;
    [Tooltip("Suavizado de la cámara")]
    public float cameraSmoothSpeed = 5f;
    [Tooltip("Ajuste extra de ángulo de la cámara (prueba 0, 90, 180, 270 hasta que quede detrás)")]
    public float cameraAngleOffset = 0f;

    [Header("=== EFECTOS (Opcional) ===")]
    [Tooltip("Sonido del motor/rotor")]
    public AudioClip engineSound;
    [Tooltip("Sonido al arrancar")]
    public AudioClip startupSound;
    [Tooltip("Sonido al apagar")]
    public AudioClip shutdownSound;
    [Tooltip("Partículas de polvo al estar cerca del suelo")]
    public ParticleSystem dustEffect;
    [Tooltip("Distancia al suelo para activar el polvo")]
    public float dustActivationHeight = 8f;

    [Header("=== DAÑO POR IMPACTO ===")]
    [Tooltip("Activar daño al impactar")]
    public bool enableCrashDamage = true;
    [Tooltip("Velocidad mínima para recibir daño al impactar")]
    public float crashSpeedThreshold = 8f;
    [Tooltip("Daño por unidad de velocidad al impactar")]
    public float crashDamageMultiplier = 5f;

    // ── Estado interno ──
    private bool isBeingPiloted;
    private bool engineOn;
    private float rotorPower;       // 0 a 1
    private float spoolTime = 2f;   // Segundos para arrancar/apagar rotores
    private float currentSpeed;     // Velocidad horizontal actual
    private float vSpeed;           // Velocidad vertical actual

    // Orientación
    private float yaw;              // Heading REAL del helicóptero (donde apunta/vuela)
    private float initialPitch;     // Rotación X original del modelo FBX
    private float initialRoll;      // Rotación Z original del modelo FBX
    private float tiltPitch;        // Inclinación visual actual (avanzar/retroceder)
    private float tiltRoll;         // Inclinación visual actual (girar)

    // Jugador
    private Transform pilot;
    private CharacterController pilotCC;
    private MonoBehaviour pilotFPC;
    private Camera playerCamera;

    // Audio
    private AudioSource audioSrc;
    private bool engineAudioPlaying;

    // Física
    private Rigidbody rb;
    private BoxCollider boxCol;

    // ══════════════════════════════════════════════════════════════════
    //  UNITY LIFECYCLE
    // ══════════════════════════════════════════════════════════════════

    void Start()
    {
        // Guardar rotación original del modelo FBX
        initialPitch = transform.eulerAngles.x;
        initialRoll  = transform.eulerAngles.z;
        // Heading real = rotación visual Y - offset del modelo
        yaw = transform.eulerAngles.y - modelYawOffset;

        // Configurar Rigidbody — NO kinematic para que Unity detecte colisiones
        rb = GetComponent<Rigidbody>();
        if (rb == null)
            rb = gameObject.AddComponent<Rigidbody>();
        rb.freezeRotation = true;
        rb.isKinematic = false;
        rb.useGravity = false;
        rb.mass = 500f; // Pesado para que no lo empujen zombis/objetos
        rb.drag = 1f;
        rb.angularDrag = 10f;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        // Cachear BoxCollider
        boxCol = GetComponent<BoxCollider>();

        // AudioSource
        audioSrc = GetComponent<AudioSource>();
        if (audioSrc == null)
        {
            audioSrc = gameObject.AddComponent<AudioSource>();
            audioSrc.loop = true;
            audioSrc.playOnAwake = false;
            audioSrc.spatialBlend = 1f;
            audioSrc.maxDistance = 100f;
        }

        // Auto-crear asiento del piloto
        if (pilotSeat == null)
        {
            GameObject go = new GameObject("PilotSeat");
            go.transform.SetParent(transform);
            go.transform.localPosition = new Vector3(0.5f, 1f, 1.5f);
            pilotSeat = go.transform;
        }

        // Auto-crear punto de salida
        if (exitPoint == null)
        {
            GameObject go = new GameObject("ExitPoint");
            go.transform.SetParent(transform);
            go.transform.localPosition = new Vector3(3f, 0f, 0f);
            exitPoint = go.transform;
        }

        // Auto-crear cámara (objeto independiente, NO hijo del helicóptero)
        if (helicopterCamera == null)
        {
            GameObject go = new GameObject("HelicopterCamera");
            helicopterCamera = go.AddComponent<Camera>();
            go.AddComponent<AudioListener>();
        }
        helicopterCamera.gameObject.SetActive(false);
    }

    void Update()
    {
        SpinRotors();

        if (isBeingPiloted)
        {
            UpdateEngine();
            Fly();

            // Mantener al piloto en el asiento
            if (pilot != null && pilotSeat != null)
            {
                pilot.position = pilotSeat.position;
                pilot.rotation = pilotSeat.rotation;
            }
        }
        else
        {
            // Frenar rotores gradualmente
            if (rotorPower > 0f)
                rotorPower = Mathf.MoveTowards(rotorPower, 0f, Time.deltaTime / (spoolTime * 2f));

            // Gravedad si está en el aire
            if (!IsGrounded())
            {
                vSpeed -= gravity * Time.deltaTime;
                rb.velocity = new Vector3(0f, vSpeed, 0f);
            }
            else
            {
                vSpeed = 0f;
                currentSpeed = 0f;
                rb.velocity = Vector3.zero;
            }

            // Mantener derecho (sin inclinación)
            transform.rotation = Quaternion.Euler(initialPitch, yaw + modelYawOffset, initialRoll);
        }

        HandleDust();
    }

    void LateUpdate()
    {
        // Cámara en LateUpdate para máxima suavidad
        if (isBeingPiloted)
            FollowCamera();
    }

    void OnDestroy()
    {
        // Limpiar cámara si fue auto-creada (objeto independiente)
        if (helicopterCamera != null && helicopterCamera.transform.parent == null)
            Destroy(helicopterCamera.gameObject);
    }

    // ══════════════════════════════════════════════════════════════════
    //  MOTOR
    // ══════════════════════════════════════════════════════════════════

    void UpdateEngine()
    {
        if (engineOn)
            rotorPower = Mathf.MoveTowards(rotorPower, 1f, Time.deltaTime / spoolTime);
        else
        {
            rotorPower = Mathf.MoveTowards(rotorPower, 0f, Time.deltaTime / spoolTime);
            if (!IsGrounded())
                vSpeed -= gravity * 0.5f * Time.deltaTime;
        }

        // Audio del motor
        if (audioSrc == null || engineSound == null) return;

        if (rotorPower > 0.01f)
        {
            if (!engineAudioPlaying)
            {
                audioSrc.clip = engineSound;
                audioSrc.Play();
                engineAudioPlaying = true;
            }
            audioSrc.pitch  = 0.5f + rotorPower * 0.8f;
            audioSrc.volume = 0.3f + rotorPower * 0.7f;
        }
        else if (engineAudioPlaying)
        {
            audioSrc.Stop();
            engineAudioPlaying = false;
        }
    }

    // ══════════════════════════════════════════════════════════════════
    //  VUELO
    // ══════════════════════════════════════════════════════════════════

    void Fly()
    {
        if (PauseManager.IsPaused) return;

        float power = Mathf.Clamp01(rotorPower);

        // Sin potencia suficiente → caer
        if (power < 0.5f)
        {
            if (!IsGrounded())
            {
                vSpeed -= gravity * (1f - power) * Time.deltaTime;
                rb.velocity = new Vector3(0f, vSpeed, 0f);
            }
            else
            {
                rb.velocity = Vector3.zero;
            }
            ApplyTilt(0f, 0f);
            return;
        }

        // ── Input ──
        float inputFwd  = Input.GetAxis("Vertical");     // W/S
        float inputTurn = Input.GetAxis("Horizontal");    // A/D

        float inputLift = 0f;
        if (Input.GetKey(KeyCode.Space))       inputLift =  1f;   // Subir
        if (Input.GetKey(KeyCode.LeftControl)) inputLift = -1f;   // Bajar

        bool turbo = Input.GetKey(KeyCode.LeftShift);
        float speedMul = turbo ? turboMultiplier : 1f;

        // ── Girar (A/D cambian el heading) ──
        yaw += inputTurn * turnSpeed * Time.deltaTime;

        // ── Avanzar / Retroceder (W/S en la dirección del heading) ──
        float targetSpeed = inputFwd * maxSpeed * speedMul;
        if (Mathf.Abs(inputFwd) > 0.01f)
            currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, acceleration * Time.deltaTime);
        else
            currentSpeed = Mathf.MoveTowards(currentSpeed, 0f, deceleration * Time.deltaTime);

        // Limitar marcha atrás a la mitad
        currentSpeed = Mathf.Clamp(currentSpeed, -maxSpeed * 0.5f, maxSpeed * speedMul);

        Vector3 headingDir = Quaternion.Euler(0f, yaw + modelYawOffset + cameraAngleOffset, 0f) * Vector3.forward;
        Vector3 movement = headingDir * currentSpeed * Time.deltaTime;

        // ── Ascender / Descender ──
        if (inputLift != 0f)
            vSpeed = Mathf.MoveTowards(vSpeed, inputLift * verticalSpeed, verticalSpeed * 2f * Time.deltaTime);
        else
            vSpeed = Mathf.MoveTowards(vSpeed, 0f, verticalSpeed * Time.deltaTime);

        // Limitar altitud
        if (transform.position.y >= maxAltitude && vSpeed > 0f) vSpeed = 0f;
        if (GroundDistance() < minGroundClearance && vSpeed < 0f) vSpeed = 0f;

        // Aplicar velocidad — Unity maneja las colisiones con paredes automáticamente
        Vector3 desiredVelocity = headingDir * currentSpeed + Vector3.up * vSpeed;
        rb.velocity = desiredVelocity;

        // ── Inclinación visual (pura estética) ──
        ApplyTilt(-inputFwd * pitchTiltAngle, -inputTurn * rollTiltAngle);
    }

    void ApplyTilt(float targetPitch, float targetRoll)
    {
        tiltPitch = Mathf.Lerp(tiltPitch, targetPitch, Time.deltaTime * tiltSmoothing);
        tiltRoll  = Mathf.Lerp(tiltRoll,  targetRoll,  Time.deltaTime * tiltSmoothing);

        // Rotación = base del FBX + inclinación visual + heading + offset modelo
        transform.rotation = Quaternion.Euler(
            initialPitch + tiltPitch,
            yaw + modelYawOffset,
            initialRoll  + tiltRoll);
    }

    // ══════════════════════════════════════════════════════════════════
    //  CÁMARA
    // ══════════════════════════════════════════════════════════════════

    void FollowCamera()
    {
        if (helicopterCamera == null) return;

        // Dirección visual del morro (solo yaw, sin pitch/roll del modelo)
        Vector3 visualForward = Quaternion.Euler(0f, yaw + modelYawOffset + cameraAngleOffset, 0f) * Vector3.forward;

        // Cámara en la cola: DETRÁS del morro + arriba
        Vector3 targetPos = transform.position - visualForward * cameraDistance + Vector3.up * cameraHeight;

        helicopterCamera.transform.position = Vector3.Lerp(
            helicopterCamera.transform.position, targetPos, Time.deltaTime * cameraSmoothSpeed);

        // Mirar al helicóptero
        helicopterCamera.transform.LookAt(transform.position + Vector3.up * cameraLookAtHeight);
    }

    // ══════════════════════════════════════════════════════════════════
    //  ROTORES
    // ══════════════════════════════════════════════════════════════════

    void SpinRotors()
    {
        if (mainRotor != null)
            mainRotor.Rotate(mainRotorAxis, rotorPower * mainRotorSpeed * Time.deltaTime, Space.Self);

        if (tailRotor != null)
            tailRotor.Rotate(tailRotorAxis, rotorPower * tailRotorSpeed * Time.deltaTime, Space.Self);
    }

    // ══════════════════════════════════════════════════════════════════
    //  EFECTOS
    // ══════════════════════════════════════════════════════════════════

    void HandleDust()
    {
        if (dustEffect == null) return;

        float gd = GroundDistance();
        bool emit = rotorPower > 0.3f && gd < dustActivationHeight;

        if (emit && !dustEffect.isPlaying)  dustEffect.Play();
        if (!emit && dustEffect.isPlaying)  dustEffect.Stop();

        if (emit)
        {
            var e = dustEffect.emission;
            e.rateOverTime = 30f * Mathf.InverseLerp(dustActivationHeight, 0f, gd) * rotorPower;
        }
    }

    // ══════════════════════════════════════════════════════════════════
    //  UTILIDADES
    // ══════════════════════════════════════════════════════════════════

    float GroundDistance()
    {
        // Lanzar rayo hacia abajo ignorando los colliders del propio helicóptero
        float maxCheck = maxAltitude + 50f;
        RaycastHit[] hits = Physics.RaycastAll(transform.position, Vector3.down, maxCheck);
        float minDist = maxCheck;
        foreach (var hit in hits)
        {
            // Ignorar colliders del helicóptero
            if (hit.collider.transform.IsChildOf(transform) || hit.collider.transform == transform)
                continue;
            if (hit.distance < minDist)
                minDist = hit.distance;
        }
        return minDist;
    }

    bool IsGrounded() => GroundDistance() < minGroundClearance + 0.5f;



    // ══════════════════════════════════════════════════════════════════
    //  SUBIR / BAJAR DEL HELICÓPTERO
    // ══════════════════════════════════════════════════════════════════

    /// <summary>El jugador sube al helicóptero</summary>
    public void EnterHelicopter(Transform playerTransform)
    {
        if (isBeingPiloted) return;

        pilot = playerTransform;
        isBeingPiloted = true;
        currentSpeed = 0f;
        vSpeed = 0f;
        tiltPitch = 0f;
        tiltRoll = 0f;

        // Calcular heading real desde la rotación visual actual
        yaw = transform.eulerAngles.y - modelYawOffset;
        transform.rotation = Quaternion.Euler(initialPitch, yaw + modelYawOffset, initialRoll);

        // Desactivar controles del jugador
        pilotCC = pilot.GetComponent<CharacterController>();
        if (pilotCC != null) pilotCC.enabled = false;

        FirstPersonController fpc = pilot.GetComponent<FirstPersonController>();
        if (fpc != null)
        {
            fpc.enabled = false;
            pilotFPC = fpc;
        }

        playerCamera = pilot.GetComponentInChildren<Camera>();

        // Activar cámara del helicóptero
        if (helicopterCamera != null)
        {
            if (playerCamera != null)
                playerCamera.gameObject.SetActive(false);

            AudioListener playerListener = pilot.GetComponentInChildren<AudioListener>();
            if (playerListener != null) playerListener.enabled = false;

            // Snap instantáneo de la cámara
            Vector3 visualFwd = Quaternion.Euler(0f, yaw + modelYawOffset + cameraAngleOffset, 0f) * Vector3.forward;
            helicopterCamera.transform.position = transform.position
                - visualFwd * cameraDistance + Vector3.up * cameraHeight;
            helicopterCamera.transform.LookAt(transform.position + Vector3.up * cameraLookAtHeight);

            helicopterCamera.gameObject.SetActive(true);
        }

        // HUD
        if (GameHUD.Instance != null)
        {
            Camera cam = helicopterCamera != null ? helicopterCamera : GetComponentInChildren<Camera>();
            GameHUD.Instance.SetHeadingOverride(cam != null ? cam.transform : transform);
        }

        // Hacer al jugador invulnerable dentro del helicóptero
        PlayerHealth ph = pilot.GetComponent<PlayerHealth>();
        if (ph != null) ph.isInVehicle = true;

        engineOn = true;

        if (startupSound != null)
            AudioSource.PlayClipAtPoint(startupSound, transform.position);

        Debug.Log("[Helicóptero] Jugador subió");
    }

    /// <summary>El jugador baja del helicóptero (solo cerca del suelo)</summary>
    public void ExitHelicopter()
    {
        if (!isBeingPiloted || pilot == null) return;

        // Solo puede bajar cerca del suelo
        if (GroundDistance() > minGroundClearance + 3f)
        {
            Debug.LogWarning("[Helicóptero] ¡Demasiado alto para bajar!");
            return;
        }

        isBeingPiloted = false;
        engineOn = false;

        // Detener el helicóptero
        rb.velocity = Vector3.zero;
        currentSpeed = 0f;
        vSpeed = 0f;

        // Quitar invulnerabilidad del vehículo
        PlayerHealth ph = pilot.GetComponent<PlayerHealth>();
        if (ph != null) ph.isInVehicle = false;

        // Colocar al jugador en posición de salida
        pilot.position = FindExitPosition();

        if (pilotCC != null)  pilotCC.enabled = true;
        if (pilotFPC != null) pilotFPC.enabled = true;

        // Desactivar cámara heli, reactivar cámara jugador
        if (helicopterCamera != null)
            helicopterCamera.gameObject.SetActive(false);

        if (playerCamera != null)
        {
            playerCamera.gameObject.SetActive(true);
            if (GameHUD.Instance != null)
                GameHUD.Instance.SetHeadingOverride(playerCamera.transform);
        }

        AudioListener playerListener = pilot.GetComponentInChildren<AudioListener>(true);
        if (playerListener != null) playerListener.enabled = true;

        if (shutdownSound != null)
            AudioSource.PlayClipAtPoint(shutdownSound, transform.position);

        if (GameHUD.Instance != null)
            StartCoroutine(ClearHeadingOverride());

        pilot = null;
        pilotCC = null;
        pilotFPC = null;

        Debug.Log("[Helicóptero] Jugador bajó");
    }

    Vector3 FindExitPosition()
    {
        // Buscar suelo en 8 direcciones alrededor del helicóptero
        Vector3[] dirs = {
            transform.right, -transform.right,
            transform.forward, -transform.forward,
            (transform.right + transform.forward).normalized,
            (-transform.right + transform.forward).normalized,
            (transform.right - transform.forward).normalized,
            (-transform.right - transform.forward).normalized
        };

        Vector3 best = exitPoint != null ? exitPoint.position : transform.position + Vector3.up * 2f;
        float bestDist = float.MaxValue;

        foreach (Vector3 dir in dirs)
        {
            for (float d = 2.5f; d <= 6f; d += 1.5f)
            {
                Vector3 checkPos = transform.position + dir * d + Vector3.up * 5f;
                RaycastHit hit;
                if (Physics.Raycast(checkPos, Vector3.down, out hit, 15f))
                {
                    float dist = Vector3.Distance(transform.position, hit.point);
                    if (dist < bestDist)
                    {
                        bestDist = dist;
                        best = hit.point + Vector3.up * 1f;
                    }
                }
            }
        }

        return best;
    }

    // ══════════════════════════════════════════════════════════════════
    //  COLISIONES
    // ══════════════════════════════════════════════════════════════════

    void OnCollisionEnter(Collision collision)
    {
        if (!enableCrashDamage || !isBeingPiloted) return;

        float impactSpeed = collision.relativeVelocity.magnitude;
        if (impactSpeed > crashSpeedThreshold)
        {
            float damage = (impactSpeed - crashSpeedThreshold) * crashDamageMultiplier;

            if (pilot != null)
            {
                PlayerHealth ph = pilot.GetComponent<PlayerHealth>();
                if (ph != null)
                    ph.TakeDamage(Mathf.RoundToInt(damage));
            }

            currentSpeed *= 0.3f;
            vSpeed *= 0.3f;
        }
    }

    // ══════════════════════════════════════════════════════════════════
    //  API PÚBLICA
    // ══════════════════════════════════════════════════════════════════

    public bool IsBeingPiloted() => isBeingPiloted;
    public float GetCurrentSpeed() => Mathf.Abs(currentSpeed);
    public float GetAltitude() => GroundDistance();
    public float GetVerticalSpeed() => vSpeed;
    public float GetRotorPower() => rotorPower;
    public bool IsEngineOn() => engineOn;

    // ══════════════════════════════════════════════════════════════════
    //  GIZMOS
    // ══════════════════════════════════════════════════════════════════

    void OnDrawGizmosSelected()
    {
        if (pilotSeat != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(pilotSeat.position, 0.5f);
        }

        if (exitPoint != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(exitPoint.position, 0.5f);
        }

        // Dirección real de avance (heading)
        Gizmos.color = Color.blue;
        float gizmoYaw = Application.isPlaying ? yaw : transform.eulerAngles.y - modelYawOffset;
        Gizmos.DrawRay(transform.position, Quaternion.Euler(0f, gizmoYaw, 0f) * Vector3.forward * 5f);
    }

    IEnumerator ClearHeadingOverride()
    {
        yield return null;
        yield return null;
        if (GameHUD.Instance != null)
            GameHUD.Instance.SetHeadingOverride(null);
    }
}
