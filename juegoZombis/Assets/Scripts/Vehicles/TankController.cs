using UnityEngine;
using System.Collections;

/// <summary>
/// Controlador de tanque — Movimiento arcade con torreta independiente controlada por ratón.
/// Ponlo en el GameObject raíz del tanque (el casco/chasis).
///
/// Controles (mientras conduces):
///   W / S           → Avanzar / Retroceder
///   A / D           → Girar chasis izquierda / derecha
///   Ratón           → Rotar la torreta (cabezal) de forma independiente
///   Click izquierdo → Disparar misil hacia el punto del raycast
///   Left Shift      → Turbo
/// </summary>
public class TankController : MonoBehaviour
{
    [Header("=== MOVIMIENTO DEL CHASIS ===")]
    [Tooltip("Velocidad máxima de avance")]
    public float maxSpeed = 12f;
    [Tooltip("Aceleración")]
    public float acceleration = 8f;
    [Tooltip("Frenado cuando no hay input")]
    public float deceleration = 12f;
    [Tooltip("Velocidad de giro del chasis (A/D)")]
    public float turnSpeed = 40f;
    [Tooltip("Multiplicador de turbo (Shift)")]
    public float turboMultiplier = 1.5f;
    [Tooltip("Velocidad máxima marcha atrás (fracción de maxSpeed)")]
    [Range(0.1f, 1f)]
    public float reverseSpeedFraction = 0.4f;

    [Header("=== COLISIÓN ===")]
    [Tooltip("Radio de la esfera de colisión frontal del tanque. Ajústalo al ancho/largo del modelo.")]
    public float tankCollisionRadius = 2.2f;
    [Tooltip("Altura del origen del SphereCast (desde el suelo)")]
    public float collisionCheckHeight = 0.8f;
    [Tooltip("Capas con las que colisiona el tanque. Por defecto todas excepto Triggers.")]
    public LayerMask collisionMask = ~0;
    [Tooltip("Máximo ángulo de pendiente que el tanque puede subir (grados)")]
    public float maxSlopeAngle = 35f;
    [Tooltip("Máximo que el tanque puede subir por segundo (metros). Evita que trepe edificios.")]
    public float maxClimbSpeed = 2f;

    [Header("=== TORRETA (CABEZAL) ===")]
    [Tooltip("Transform de la torreta — se rota con el ratón de forma independiente al chasis")]
    public Transform turret;
    [Tooltip("Velocidad de rotación de la torreta al seguir el ratón")]
    public float turretRotationSpeed = 120f;
    [Tooltip("Suavizado de la rotación de la torreta")]
    public float turretSmoothing = 8f;
    [Tooltip("Buscar torreta automáticamente por nombre si no se asigna")]
    public bool autoFindTurret = true;

    [Header("=== DISPARO DE MISILES ===")]
    [Tooltip("Prefab del misil (debe tener MissileController)")]
    public GameObject missilePrefab;
    [Tooltip("Punto de disparo — punta del cañón. Se crea automáticamente si está vacío")]
    public Transform firePoint;
    [Tooltip("Crear firePoint automáticamente en la punta del cañón")]
    public bool autoCreateFirePoint = true;
    [Tooltip("Offset local del firePoint respecto a la torreta si se crea automáticamente")]
    public Vector3 firePointOffset = new Vector3(0f, 0.5f, 3f);
    [Tooltip("Cadencia de disparo (segundos entre misiles)")]
    public float fireRate = 1.5f;
    [Tooltip("Distancia máxima del raycast para apuntar")]
    public float maxAimDistance = 500f;
    [Tooltip("Capas que el raycast puede impactar")]
    public LayerMask aimLayerMask = ~0; // Todo por defecto

    [Header("=== POSICIONES ===")]
    [Tooltip("Punto donde se sienta el conductor")]
    public Transform driverSeat;
    [Tooltip("Punto donde sale el jugador al bajar")]
    public Transform exitPoint;

    [Header("=== CÁMARA ===")]
    [Tooltip("Cámara del tanque (se crea automáticamente si está vacío)")]
    public Camera tankCamera;
    [Tooltip("Distancia de la cámara al tanque")]
    public float cameraDistance = 12f;
    [Tooltip("Altura de la cámara sobre el tanque")]
    public float cameraHeight = 8f;
    [Tooltip("Altura del punto al que mira la cámara")]
    public float cameraLookAtHeight = 2f;
    [Tooltip("Suavizado del movimiento de la cámara")]
    public float cameraSmoothSpeed = 8f;
    [Tooltip("Sensibilidad del ratón para la cámara")]
    public float mouseSensitivity = 3f;
    [Tooltip("Ángulo mínimo vertical de la cámara (mirar abajo)")]
    public float minPitch = 5f;
    [Tooltip("Ángulo máximo vertical de la cámara (mirar arriba)")]
    public float maxPitch = 60f;

    [Header("=== EFECTOS (Opcional) ===")]
    [Tooltip("Sonido del motor")]
    public AudioClip engineSound;
    [Tooltip("Sonido al arrancar el tanque")]
    public AudioClip startupSound;
    [Tooltip("Sonido al apagar el tanque")]
    public AudioClip shutdownSound;
    [Tooltip("Sonido al disparar")]
    public AudioClip fireSound;
    [Tooltip("Partículas de polvo al moverse")]
    public ParticleSystem dustTrailEffect;
    [Tooltip("Partículas de fogonazo al disparar (se pone en firePoint)")]
    public ParticleSystem muzzleFlashEffect;

    [Header("=== DAÑO POR IMPACTO ===")]
    [Tooltip("Activar daño al impactar a alta velocidad")]
    public bool enableCrashDamage = true;
    [Tooltip("Velocidad mínima para recibir daño al impactar")]
    public float crashSpeedThreshold = 10f;
    [Tooltip("Daño por unidad de velocidad al impactar")]
    public float crashDamageMultiplier = 3f;

    [Header("=== MODELO ===")]
    [Tooltip("Offset de yaw para compensar modelos FBX (0 o 180)")]
    public float modelYawOffset = 0f;

    [Header("=== RUEDAS / ORUGAS ===")]
    [Tooltip("Buscar ruedas y orugas automáticamente por nombre")]
    public bool autoFindWheels = true;
    [Tooltip("Ruedas del tanque (se buscan automáticamente si está vacío)")]
    public Transform[] wheels;
    [Tooltip("Eje de rotación de las ruedas (normalmente X local)")]
    public Vector3 wheelRotationAxis = Vector3.right;
    [Tooltip("Multiplicador de velocidad de giro de las ruedas")]
    public float wheelSpeedMultiplier = 300f;

    [Space(5)]
    [Tooltip("Renderers de las orugas (SM_T34_Track_L, SM_T34_Track_R). Se buscan automáticamente.")]
    public Renderer[] trackRenderers;
    [Tooltip("Velocidad de desplazamiento UV de las orugas")]
    public float trackScrollSpeed = 0.5f;
    [Tooltip("Eje UV a desplazar: X=true (horizontal), Y=false (vertical)")]
    public bool trackScrollAxisX = true;
    private Material[] trackMaterials; // instancias propias para no modificar el asset
    private float trackScrollOffset = 0f;

    // ── Estado interno ──
    private bool isBeingDriven;
    private float currentSpeed;
    private float yaw; // Heading del chasis
    private float turretYaw; // Heading de la torreta (independiente)
    private float targetTurretYaw;
    private float lastFireTime;

    // Cámara orbital
    private float camYaw;   // Ángulo horizontal de la cámara (controlado por ratón)
    private float camPitch; // Ángulo vertical de la cámara (controlado por ratón)

    // Jugador
    private Transform driver;
    private CharacterController driverCC;
    private MonoBehaviour driverFPC;
    private Camera playerCamera;

    // Audio
    private AudioSource audioSrc;
    private AudioSource fireSfxSource;
    private bool engineAudioPlaying;

    // Física
    private Rigidbody rb;

    // Raycast para apuntado
    private Vector3 aimPoint;

    // ══════════════════════════════════════════════════════════════════
    //  UNITY LIFECYCLE
    // ══════════════════════════════════════════════════════════════════

    void Start()
    {
        // Buscar torreta automáticamente
        if (autoFindTurret && turret == null)
            TryFindTurret();

        // Guardar heading inicial
        yaw = transform.eulerAngles.y - modelYawOffset;
        turretYaw = turret != null ? turret.eulerAngles.y : yaw;
        targetTurretYaw = turretYaw;

        // Buscar ruedas automáticamente
        if (autoFindWheels && (wheels == null || wheels.Length == 0))
            TryFindWheels();

        // Configurar Rigidbody — SIEMPRE kinematic, el movimiento es manual
        rb = GetComponent<Rigidbody>();
        if (rb == null)
            rb = gameObject.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
        rb.freezeRotation = true;
        rb.mass = 2000f;
        rb.interpolation = RigidbodyInterpolation.None;

        // Asegurar que el tanque tenga colliders para que el jugador no lo atraviese
        EnsureTankColliders();

        // AudioSource para el motor
        audioSrc = GetComponent<AudioSource>();
        if (audioSrc == null)
        {
            audioSrc = gameObject.AddComponent<AudioSource>();
            audioSrc.loop = true;
            audioSrc.playOnAwake = false;
            audioSrc.spatialBlend = 1f;
            audioSrc.maxDistance = 80f;
        }

        // AudioSource secundario para disparos
        fireSfxSource = gameObject.AddComponent<AudioSource>();
        fireSfxSource.loop = false;
        fireSfxSource.playOnAwake = false;
        fireSfxSource.spatialBlend = 1f;
        fireSfxSource.maxDistance = 120f;

        // Auto-crear asiento del conductor
        if (driverSeat == null)
        {
            GameObject go = new GameObject("DriverSeat");
            go.transform.SetParent(transform);
            go.transform.localPosition = new Vector3(0f, 2f, 0f);
            driverSeat = go.transform;
        }

        // Auto-crear punto de salida
        if (exitPoint == null)
        {
            GameObject go = new GameObject("ExitPoint");
            go.transform.SetParent(transform);
            go.transform.localPosition = new Vector3(3.5f, 0f, 0f);
            exitPoint = go.transform;
        }

        // Auto-crear punto de disparo
        if (firePoint == null && autoCreateFirePoint)
        {
            CreateFirePoint();
        }

        // Auto-crear cámara (objeto independiente)
        if (tankCamera == null)
        {
            GameObject go = new GameObject("TankCamera");
            tankCamera = go.AddComponent<Camera>();
            go.AddComponent<AudioListener>();
        }
        tankCamera.gameObject.SetActive(false);

        // Inicializar tiempo de disparo
        lastFireTime = -fireRate;
    }

    void Update()
    {
        if (isBeingDriven)
        {
            HandleCameraInput();
            HandleEngine();
            Drive();
            RotateTurret();
            HandleShooting();

            // Mantener al conductor en el asiento
            if (driver != null && driverSeat != null)
            {
                driver.position = driverSeat.position;
                driver.rotation = driverSeat.rotation;
            }
        }

        HandleDustTrail();
        SpinWheels();
        ScrollTracks();
    }

    void FixedUpdate()
    {
        // No necesitamos FixedUpdate — todo el movimiento es en Update con transform
    }

    void LateUpdate()
    {
        if (isBeingDriven)
            FollowCamera();
    }

    void OnDestroy()
    {
        if (tankCamera != null && tankCamera.transform.parent == null)
            Destroy(tankCamera.gameObject);
    }

    // ══════════════════════════════════════════════════════════════════
    //  MOTOR / AUDIO
    // ══════════════════════════════════════════════════════════════════

    void HandleEngine()
    {
        if (audioSrc == null || engineSound == null) return;

        float speedRatio = Mathf.Abs(currentSpeed) / maxSpeed;

        if (!engineAudioPlaying)
        {
            audioSrc.clip = engineSound;
            audioSrc.Play();
            engineAudioPlaying = true;
        }

        audioSrc.pitch = 0.6f + speedRatio * 0.8f;
        audioSrc.volume = 0.4f + speedRatio * 0.6f;
    }

    // ══════════════════════════════════════════════════════════════════
    //  CONDUCCIÓN DEL CHASIS — W adelante, S atrás, A/D girar
    // ══════════════════════════════════════════════════════════════════

    void Drive()
    {
        if (PauseManager.IsPaused) return;

        float inputFwd = -Input.GetAxis("Vertical");    // W/S invertido
        float inputTurn = Input.GetAxis("Horizontal");  // A/D
        bool turbo = Input.GetKey(KeyCode.LeftShift);
        float speedMul = turbo ? turboMultiplier : 1f;

        // ── A/D giran el chasis ──
        yaw += inputTurn * turnSpeed * Time.deltaTime;

        // ── W/S aceleran / frenan ──
        float targetSpeed = inputFwd * maxSpeed * speedMul;

        // Marcha atrás más lenta
        if (inputFwd < 0f)
            targetSpeed = inputFwd * maxSpeed * reverseSpeedFraction;

        if (Mathf.Abs(inputFwd) > 0.01f)
            currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, acceleration * Time.deltaTime);
        else
            currentSpeed = Mathf.MoveTowards(currentSpeed, 0f, deceleration * Time.deltaTime);

        // ── Aplicar rotación del chasis ──
        transform.rotation = Quaternion.Euler(0f, yaw + modelYawOffset, 0f);

        // ── Mover el tanque (siempre hacia donde mira el chasis) ──
        Vector3 headingDir = Quaternion.Euler(0f, yaw + modelYawOffset, 0f) * Vector3.forward;
        Vector3 rawDelta = headingDir * currentSpeed * Time.deltaTime;
        Vector3 safeDelta = CheckTankCollision(rawDelta);

        // Si la colisión canceló todo el movimiento, frenar el tanque
        if (safeDelta.sqrMagnitude < 0.0001f && rawDelta.sqrMagnitude > 0.0001f)
            currentSpeed = Mathf.MoveTowards(currentSpeed, 0f, deceleration * 2f * Time.deltaTime);

        transform.position += safeDelta;

        // ── Pegar al suelo con raycast ──
        SnapToGround();
    }

    // ══════════════════════════════════════════════════════════
    //  DETECCIÓN DE COLISIONES HORIZONTALES
    // ══════════════════════════════════════════════════════════

    /// <summary>
    /// Comprueba si el movimiento delta chocaría con algo y devuelve
    /// el movimiento seguro (deslizando a lo largo de la pared si es posible).
    /// </summary>
    Vector3 CheckTankCollision(Vector3 delta)
    {
        if (delta.sqrMagnitude < 0.0001f) return delta;

        // Desactivar nuestros propios colliders para no detectarlos
        Collider[] own = GetComponentsInChildren<Collider>(true);
        foreach (var c in own) c.enabled = false;

        // Desactivar colliders del conductor
        Collider[] driverCols = null;
        if (driver != null)
        {
            driverCols = driver.GetComponentsInChildren<Collider>(true);
            foreach (var c in driverCols) c.enabled = false;
        }

        // Origen del SphereCast: dejamos margen sobre el suelo para que
        // bordillos, rampas y desniveles del terreno no bloqueen al tanque.
        float groundClearance = 1f;
        float originHeight = tankCollisionRadius + groundClearance;
        Vector3 origin = transform.position + Vector3.up * originHeight;
        Vector3 dir    = delta.normalized;
        float   dist   = delta.magnitude;

        RaycastHit hit;
        Vector3 result = delta;

        bool blocked = Physics.SphereCast(
            origin, tankCollisionRadius, dir, out hit, dist,
            collisionMask, QueryTriggerInteraction.Ignore);

        if (blocked)
        {
            // Si hit.distance ≈ 0 significa que la esfera empezó dentro de un objeto
            // (ej. borde del hangar, muro cercano). Permitir moverse para no quedar atascado.
            if (hit.distance < 0.01f)
            {
                result = delta;
            }
            else
            {
                // Distancia segura hasta el impacto (sin penetrar)
                float safeDistance = Mathf.Max(0f, hit.distance - 0.05f);

                // Intento de deslizamiento a lo largo de la pared
                Vector3 slide = Vector3.ProjectOnPlane(delta, hit.normal);
                slide.y = 0f; // mantener solo movimiento horizontal

                // Comprobar si el deslizamiento también está bloqueado
                bool slideBlocked = Physics.SphereCast(
                    origin, tankCollisionRadius, slide.normalized, out _,
                    slide.magnitude, collisionMask, QueryTriggerInteraction.Ignore);

                if (!slideBlocked && slide.sqrMagnitude > 0.0001f)
                    result = slide;
                else
                    result = dir * safeDistance; // avanzar hasta el contacto y detenerse
            }
        }

        // Reactivar colliders del conductor
        if (driverCols != null)
            foreach (var c in driverCols) c.enabled = true;

        // Reactivar colliders propios
        foreach (var c in own) c.enabled = true;

        return result;
    }

    /// <summary>Lanza un rayo hacia abajo para pegar el tanque al suelo</summary>
    void SnapToGround()
    {
        // Desactivar colliders del tanque
        Collider[] tankColliders = GetComponentsInChildren<Collider>(true);
        foreach (var col in tankColliders)
            col.enabled = false;

        // Desactivar colliders del conductor (¡clave! si no, el raycast lo golpea y sube infinitamente)
        Collider[] driverColliders = null;
        if (driver != null)
        {
            driverColliders = driver.GetComponentsInChildren<Collider>(true);
            foreach (var col in driverColliders)
                col.enabled = false;
        }

        // Rayo desde la posición actual del tanque (no demasiado arriba para no golpear techos)
        // Usamos un origen bajo (0.5m) para evitar que el rayo empiece dentro de un techo de hangar
        Vector3 rayOrigin = transform.position + Vector3.up * 0.5f;
        RaycastHit hit;

        if (Physics.Raycast(rayOrigin, Vector3.down, out hit, 10f, collisionMask, QueryTriggerInteraction.Ignore))
        {
            float targetY = hit.point.y;
            float currentY = transform.position.y;
            float diff = targetY - currentY;

            // Comprobar el ángulo de la superficie
            float slopeAngle = Vector3.Angle(hit.normal, Vector3.up);

            if (slopeAngle > maxSlopeAngle && diff > 0.1f)
            {
                // Superficie demasiado empinada y el tanque intentaría SUBIR → bloquear
                // No cambiar Y, y frenar el tanque
                currentSpeed = Mathf.MoveTowards(currentSpeed, 0f, deceleration * 3f * Time.deltaTime);
            }
            else
            {
                // Limitar cuánto sube por frame (bajar es libre)
                if (diff > 0f)
                {
                    float maxClimb = maxClimbSpeed * Time.deltaTime;
                    diff = Mathf.Min(diff, maxClimb);
                }

                Vector3 pos = transform.position;
                pos.y = currentY + diff;
                transform.position = pos;
            }
        }

        // Reactivar colliders del conductor
        if (driverColliders != null)
        {
            foreach (var col in driverColliders)
                col.enabled = true;
        }

        // Reactivar colliders del tanque
        foreach (var col in tankColliders)
            col.enabled = true;
    }

    // ══════════════════════════════════════════════════════════════════
    //  ROTACIÓN DE LA TORRETA (apunta al centro de la pantalla / raycast)
    // ══════════════════════════════════════════════════════════════════

    void RotateTurret()
    {
        if (turret == null || PauseManager.IsPaused) return;

        Camera cam = tankCamera != null && tankCamera.gameObject.activeInHierarchy
            ? tankCamera
            : Camera.main;

        if (cam == null) return;

        // Raycast desde el CENTRO de la pantalla — donde apunta la cámara
        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, maxAimDistance, aimLayerMask))
        {
            aimPoint = hit.point;
        }
        else
        {
            aimPoint = ray.origin + ray.direction * maxAimDistance;
        }

        // Calcular dirección horizontal desde la torreta al punto de mira
        Vector3 dirToTarget = aimPoint - turret.position;
        dirToTarget.y = 0f;

        if (dirToTarget.sqrMagnitude > 0.01f)
        {
            targetTurretYaw = Quaternion.LookRotation(dirToTarget).eulerAngles.y;
        }

        // Suavizar rotación de la torreta
        turretYaw = Mathf.LerpAngle(turretYaw, targetTurretYaw, Time.deltaTime * turretSmoothing);

        // Aplicar rotación a la torreta (WORLD, independiente del chasis)
        turret.rotation = Quaternion.Euler(0f, turretYaw, 0f);
    }

    // ══════════════════════════════════════════════════════════════════
    //  DISPARO DE MISILES
    // ══════════════════════════════════════════════════════════════════

    void HandleShooting()
    {
        if (PauseManager.IsPaused) return;

        if (Input.GetMouseButton(0) && Time.time >= lastFireTime + fireRate)
        {
            FireMissile();
            lastFireTime = Time.time;
        }
    }

    void FireMissile()
    {
        if (missilePrefab == null)
        {
            Debug.LogWarning("[TankController] No hay prefab de misil asignado.");
            return;
        }

        if (firePoint == null)
        {
            Debug.LogWarning("[TankController] No hay punto de disparo (firePoint) asignado.");
            return;
        }

        // Dirección = hacia donde apunta el cañón (forward del firePoint)
        Vector3 direction = firePoint.forward;
        aimPoint = firePoint.position + direction * maxAimDistance;

        // Instanciar misil en la punta del cañón, mirando hacia adelante
        GameObject missile = Instantiate(missilePrefab, firePoint.position, Quaternion.LookRotation(direction));

        // Configurar el misil si tiene MissileController
        MissileController mc = missile.GetComponent<MissileController>();
        if (mc != null)
        {
            mc.SetShooter(gameObject); // Ignorar colliders del tanque
            mc.SetTarget(aimPoint);
        }

        // Efecto de fogonazo
        if (muzzleFlashEffect != null)
        {
            muzzleFlashEffect.Play();
        }

        // Sonido de disparo
        if (fireSound != null && fireSfxSource != null)
        {
            fireSfxSource.PlayOneShot(fireSound);
        }
    }

    // ══════════════════════════════════════════════════════════════════
    //  CÁMARA ORBITAL (controlada por ratón)
    // ══════════════════════════════════════════════════════════════════

    void HandleCameraInput()
    {
        if (PauseManager.IsPaused) return;

        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        camYaw += mouseX;
        camPitch -= mouseY;
        // Evitar que se voltee (eso invierte los controles)
        camPitch = Mathf.Clamp(camPitch, -89f, 89f);
    }

    void FollowCamera()
    {
        if (tankCamera == null) return;

        // Calcular posición orbital alrededor del tanque
        Vector3 tankCenter = transform.position + Vector3.up * cameraLookAtHeight;

        // Dirección desde el tanque hacia la cámara (esférica)
        Quaternion rotation = Quaternion.Euler(camPitch, camYaw, 0f);
        Vector3 offset = rotation * new Vector3(0f, 0f, -cameraDistance);

        Vector3 targetPos = tankCenter + offset;

        // Suavizar posición
        tankCamera.transform.position = Vector3.Lerp(
            tankCamera.transform.position, targetPos, Time.deltaTime * cameraSmoothSpeed);

        // Mirar al centro del tanque
        tankCamera.transform.LookAt(tankCenter);
    }

    // ══════════════════════════════════════════════════════════════════
    //  BUSCAR TORRETA AUTOMÁTICAMENTE
    // ══════════════════════════════════════════════════════════════════

    void TryFindTurret()
    {
        string[] turretNames = { "Turret", "turret", "Cabezal", "cabezal", "Tower", "tower",
                                  "TurretBase", "turret_base", "gun_turret", "Head" };

        foreach (string name in turretNames)
        {
            Transform found = FindChildRecursive(transform, name);
            if (found != null)
            {
                turret = found;
                Debug.Log($"[TankController] Torreta encontrada: {found.name}");
                return;
            }
        }

        Debug.LogWarning("[TankController] No se encontró torreta automáticamente. " +
                        "Asigna el Transform de la torreta manualmente en el Inspector.");
    }

    Transform FindChildRecursive(Transform parent, string searchName)
    {
        foreach (Transform child in parent)
        {
            if (child.name.ToLower().Contains(searchName.ToLower()))
                return child;
            Transform found = FindChildRecursive(child, searchName);
            if (found != null)
                return found;
        }
        return null;
    }

    void FindAllChildrenRecursive(Transform parent, string searchName, System.Collections.Generic.List<Transform> results)
    {
        foreach (Transform child in parent)
        {
            if (child.name.ToLower().Contains(searchName.ToLower()))
                results.Add(child);
            FindAllChildrenRecursive(child, searchName, results);
        }
    }

    // ══════════════════════════════════════════════════════════════════
    //  RUEDAS / ORUGAS
    // ══════════════════════════════════════════════════════════════════

    void TryFindWheels()
    {
        // ── Buscar RUEDAS ──
        var found = new System.Collections.Generic.List<Transform>();

        string[] wheelNames = { "wheel", "rueda", "koleso", "road_wheel", "sprocket",
                                 "idler", "drive_wheel", "roller", "Wheel", "Koleso" };

        foreach (string name in wheelNames)
        {
            FindAllChildrenRecursive(transform, name, found);
        }

        // Eliminar duplicados (puede que "wheel" y "Wheel" encuentren lo mismo)
        var unique = new System.Collections.Generic.HashSet<Transform>(found);
        if (unique.Count > 0)
        {
            wheels = new Transform[unique.Count];
            unique.CopyTo(wheels);
            Debug.Log($"[TankController] {wheels.Length} ruedas encontradas automáticamente.");
        }
        else
        {
            Debug.LogWarning("[TankController] No se encontraron ruedas. Asígnalas manualmente o revisa los nombres del modelo.");
        }

        // ── Buscar ORUGAS (tracks) ──
        if (trackRenderers == null || trackRenderers.Length == 0)
        {
            var tracks = new System.Collections.Generic.List<Renderer>();
            string[] trackNames = { "track", "oruga", "tread", "belt" };

            foreach (string tname in trackNames)
            {
                var trackTransforms = new System.Collections.Generic.List<Transform>();
                FindAllChildrenRecursive(transform, tname, trackTransforms);
                foreach (Transform t in trackTransforms)
                {
                    Renderer r = t.GetComponent<Renderer>();
                    if (r != null) tracks.Add(r);
                }
            }

            if (tracks.Count > 0)
            {
                trackRenderers = tracks.ToArray();
                Debug.Log($"[TankController] {trackRenderers.Length} orugas encontradas automáticamente.");
            }
        }

        // Crear instancias de material propias para las orugas
        InitTrackMaterials();
    }

    void InitTrackMaterials()
    {
        if (trackRenderers == null || trackRenderers.Length == 0) return;

        trackMaterials = new Material[trackRenderers.Length];
        for (int i = 0; i < trackRenderers.Length; i++)
        {
            if (trackRenderers[i] != null)
            {
                // .material devuelve una INSTANCIA propia — no modifica el asset compartido
                trackMaterials[i] = trackRenderers[i].material;
            }
        }
    }

    void SpinWheels()
    {
        if (wheels == null || wheels.Length == 0) return;
        if (!isBeingDriven && Mathf.Abs(currentSpeed) < 0.01f) return;

        float rotAmount = currentSpeed * wheelSpeedMultiplier * Time.deltaTime;

        foreach (Transform wheel in wheels)
        {
            if (wheel != null)
                wheel.Rotate(wheelRotationAxis, rotAmount, Space.Self);
        }
    }

    void ScrollTracks()
    {
        if (trackMaterials == null || trackMaterials.Length == 0) return;
        if (!isBeingDriven && Mathf.Abs(currentSpeed) < 0.01f) return;

        // Acumular offset basándose en la velocidad actual
        trackScrollOffset += (currentSpeed / maxSpeed) * trackScrollSpeed * Time.deltaTime;
        // Mantener el offset en rango 0-1 para evitar pérdida de precisión
        trackScrollOffset %= 1f;

        Vector2 offset = trackScrollAxisX
            ? new Vector2(trackScrollOffset, 0f)
            : new Vector2(0f, trackScrollOffset);

        foreach (Material mat in trackMaterials)
        {
            if (mat != null)
                mat.mainTextureOffset = offset;
        }
    }

    // ══════════════════════════════════════════════════════════════════
    //  CREAR PUNTO DE DISPARO AUTOMÁTICO
    // ══════════════════════════════════════════════════════════════════

    void CreateFirePoint()
    {
        Transform parent = turret != null ? turret : transform;

        // Intentar encontrar el cañón primero
        string[] cannonNames = { "Cannon", "cannon", "Barrel", "barrel", "Gun", "gun",
                                  "Canon", "canon", "Cañon", "cañon", "Muzzle", "muzzle" };

        Transform cannon = null;
        foreach (string name in cannonNames)
        {
            cannon = FindChildRecursive(parent, name);
            if (cannon != null) break;
        }

        GameObject fp = new GameObject("FirePoint");

        if (cannon != null)
        {
            // Colocar en la punta del cañón
            fp.transform.SetParent(cannon);
            Renderer rend = cannon.GetComponent<Renderer>();
            if (rend != null)
            {
                // Poner en el extremo frontal del cañón
                fp.transform.localPosition = new Vector3(0f, 0f, rend.bounds.extents.z);
            }
            else
            {
                fp.transform.localPosition = new Vector3(0f, 0f, 2f);
            }
            Debug.Log($"[TankController] FirePoint creado en el cañón: {cannon.name}");
        }
        else
        {
            // Poner como hijo de la torreta con offset configurable
            fp.transform.SetParent(parent);
            fp.transform.localPosition = firePointOffset;
            Debug.Log("[TankController] FirePoint creado con offset por defecto en la torreta.");
        }

        fp.transform.localRotation = Quaternion.identity;
        firePoint = fp.transform;
    }

    // ══════════════════════════════════════════════════════════════════
    //  EFECTOS
    // ══════════════════════════════════════════════════════════════════

    void HandleDustTrail()
    {
        if (dustTrailEffect == null) return;

        bool moving = isBeingDriven && Mathf.Abs(currentSpeed) > 1f;

        if (moving && !dustTrailEffect.isPlaying) dustTrailEffect.Play();
        if (!moving && dustTrailEffect.isPlaying) dustTrailEffect.Stop();

        if (moving)
        {
            var emission = dustTrailEffect.emission;
            emission.rateOverTime = 20f * (Mathf.Abs(currentSpeed) / maxSpeed);
        }
    }

    // ══════════════════════════════════════════════════════════════════
    //  COLISIONES
    // ══════════════════════════════════════════════════════════════════

    void OnCollisionEnter(Collision collision)
    {
        if (!enableCrashDamage || !isBeingDriven) return;

        float impactSpeed = collision.relativeVelocity.magnitude;
        if (impactSpeed > crashSpeedThreshold)
        {
            float damage = (impactSpeed - crashSpeedThreshold) * crashDamageMultiplier;

            if (driver != null)
            {
                PlayerHealth ph = driver.GetComponent<PlayerHealth>();
                if (ph != null)
                    ph.TakeDamage(Mathf.RoundToInt(damage));
            }

            currentSpeed *= 0.3f;
        }
    }

    // ══════════════════════════════════════════════════════════════════
    //  SUBIR / BAJAR DEL TANQUE
    // ══════════════════════════════════════════════════════════════════

    /// <summary>El jugador sube al tanque</summary>
    public void EnterTank(Transform playerTransform)
    {
        if (isBeingDriven) return;

        driver = playerTransform;
        isBeingDriven = true;
        currentSpeed = 0f;

        // Calcular heading desde la rotación visual actual
        yaw = transform.eulerAngles.y - modelYawOffset;
        turretYaw = turret != null ? turret.eulerAngles.y : yaw;
        targetTurretYaw = turretYaw;

        // Inicializar cámara orbital — detrás del tanque
        camYaw = yaw + modelYawOffset + 180f;
        camPitch = 25f;

        // Desactivar controles del jugador
        driverCC = driver.GetComponent<CharacterController>();
        if (driverCC != null) driverCC.enabled = false;

        FirstPersonController fpc = driver.GetComponent<FirstPersonController>();
        if (fpc != null)
        {
            fpc.enabled = false;
            driverFPC = fpc;
        }

        playerCamera = driver.GetComponentInChildren<Camera>();

        // Activar cámara del tanque
        if (tankCamera != null)
        {
            if (playerCamera != null)
                playerCamera.gameObject.SetActive(false);

            AudioListener playerListener = driver.GetComponentInChildren<AudioListener>();
            if (playerListener != null) playerListener.enabled = false;

            // Snap instantáneo de la cámara
            Quaternion camRot = Quaternion.Euler(camPitch, camYaw, 0f);
            Vector3 camOffset = camRot * new Vector3(0f, 0f, -cameraDistance);
            tankCamera.transform.position = transform.position + Vector3.up * cameraLookAtHeight + camOffset;
            tankCamera.transform.LookAt(transform.position + Vector3.up * cameraLookAtHeight);

            tankCamera.gameObject.SetActive(true);
        }

        // HUD
        if (GameHUD.Instance != null)
        {
            Camera cam = tankCamera != null ? tankCamera : GetComponentInChildren<Camera>();
            GameHUD.Instance.SetHeadingOverride(cam != null ? cam.transform : transform);
        }

        // Hacer al jugador invulnerable dentro del tanque
        PlayerHealth ph = driver.GetComponent<PlayerHealth>();
        if (ph != null) ph.isInVehicle = true;

        // Bloquear cursor al centro para control de cámara
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (startupSound != null)
            AudioSource.PlayClipAtPoint(startupSound, transform.position);

        Debug.Log("[Tanque] Jugador subió");
    }

    /// <summary>El jugador baja del tanque</summary>
    public void ExitTank()
    {
        if (!isBeingDriven || driver == null) return;

        isBeingDriven = false;

        // Detener
        currentSpeed = 0f;

        // Parar audio del motor
        if (audioSrc != null && engineAudioPlaying)
        {
            audioSrc.Stop();
            engineAudioPlaying = false;
        }

        // Quitar invulnerabilidad
        PlayerHealth ph = driver.GetComponent<PlayerHealth>();
        if (ph != null) ph.isInVehicle = false;

        // Colocar al jugador en posición de salida
        driver.position = FindExitPosition();

        if (driverCC != null) driverCC.enabled = true;
        if (driverFPC != null) driverFPC.enabled = true;

        // Desactivar cámara del tanque, reactivar cámara del jugador
        if (tankCamera != null)
            tankCamera.gameObject.SetActive(false);

        if (playerCamera != null)
        {
            playerCamera.gameObject.SetActive(true);
            if (GameHUD.Instance != null)
                GameHUD.Instance.SetHeadingOverride(playerCamera.transform);
        }

        AudioListener playerListener = driver.GetComponentInChildren<AudioListener>(true);
        if (playerListener != null) playerListener.enabled = true;

        if (shutdownSound != null)
            AudioSource.PlayClipAtPoint(shutdownSound, transform.position);

        if (GameHUD.Instance != null)
            StartCoroutine(ClearHeadingOverride());

        driver = null;
        driverCC = null;
        driverFPC = null;

        Debug.Log("[Tanque] Jugador bajó");
    }

    Vector3 FindExitPosition()
    {
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
            for (float d = 3f; d <= 7f; d += 2f)
            {
                Vector3 checkPos = transform.position + dir * d + Vector3.up * 3f;
                RaycastHit hit;
                if (Physics.Raycast(checkPos, Vector3.down, out hit, 10f))
                {
                    if (!hit.collider.transform.IsChildOf(transform))
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
        }

        return best;
    }

    // ══════════════════════════════════════════════════════════════════
    //  API PÚBLICA
    // ══════════════════════════════════════════════════════════════════

    public bool IsBeingDriven() => isBeingDriven;
    public float GetCurrentSpeed() => Mathf.Abs(currentSpeed);
    public float GetTurretAngle() => turretYaw;
    public Vector3 GetAimPoint() => aimPoint;

    // ══════════════════════════════════════════════════════════════════
    //  GIZMOS
    // ══════════════════════════════════════════════════════════════════

    void OnDrawGizmosSelected()
    {
        // Asiento
        if (driverSeat != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(driverSeat.position, 0.5f);
        }

        // Punto de salida
        if (exitPoint != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(exitPoint.position, 0.5f);
        }

        // Punto de disparo
        if (firePoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(firePoint.position, 0.3f);
            Gizmos.DrawRay(firePoint.position, firePoint.forward * 5f);
        }

        // Dirección del chasis
        Gizmos.color = Color.blue;
        float gizmoYaw = Application.isPlaying ? yaw : transform.eulerAngles.y - modelYawOffset;
        Gizmos.DrawRay(transform.position, Quaternion.Euler(0f, gizmoYaw, 0f) * Vector3.forward * 5f);

        // Dirección de la torreta
        if (turret != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawRay(turret.position, turret.forward * 8f);
        }

        // Punto de mira
        if (Application.isPlaying && isBeingDriven)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(aimPoint, 0.5f);
        }
    }

    IEnumerator ClearHeadingOverride()
    {
        yield return null;
        yield return null;
        if (GameHUD.Instance != null)
            GameHUD.Instance.SetHeadingOverride(null);
    }

    // ══════════════════════════════════════════════════════════════════
    //  COLLIDERS DEL TANQUE (para que el jugador no lo atraviese)
    // ══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Asegura que el tanque tenga un BoxCollider sólido en el root que cubra
    /// todo el casco. Los MeshColliders del FBX tienen huecos por donde el
    /// jugador puede colarse, así que SIEMPRE añadimos un BoxCollider envolvente.
    /// </summary>
    void EnsureTankColliders()
    {
        // Comprobar si ya hay un BoxCollider en el root (para no duplicar)
        BoxCollider existingBox = GetComponent<BoxCollider>();
        if (existingBox != null)
        {
            Debug.Log("[TankController] Ya tiene BoxCollider en el root.");
            return;
        }

        // Calcular bounds del tanque usando todos los renderers
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
        {
            BoxCollider box = gameObject.AddComponent<BoxCollider>();
            box.center = new Vector3(0f, 1f, 0f);
            box.size = new Vector3(3f, 2f, 6f);
            Debug.Log("[TankController] BoxCollider genérico creado (sin renderers).");
            return;
        }

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);

        BoxCollider autoBox = gameObject.AddComponent<BoxCollider>();
        autoBox.center = transform.InverseTransformPoint(bounds.center);
        autoBox.size = new Vector3(
            Mathf.Abs(transform.InverseTransformVector(bounds.size).x),
            Mathf.Abs(transform.InverseTransformVector(bounds.size).y),
            Mathf.Abs(transform.InverseTransformVector(bounds.size).z)
        );

        Debug.Log("[TankController] BoxCollider envolvente creado. Center=" + autoBox.center + " Size=" + autoBox.size);
    }
}
