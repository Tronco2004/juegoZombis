using UnityEngine;
using System.Collections;

/// <summary>
/// Caja interactiva - Pulsa E cerca para comprar munición o salud.
/// La caja se ABRE con animación al comprar.
/// REQUISITOS: Box Collider con "Is Trigger" marcado en la caja.
///             El Player debe tener el Tag "Player".
///             PlayerMoney.cs en el Player.
/// </summary>
public class InteractableBoxAnimated : MonoBehaviour
{
    [Header("=== TIPO DE CAJA ===")]
    public BoxType boxType = BoxType.Ammo;

    [Header("=== CONFIGURACIÓN ===")]
    public int price = 100;
    public int ammoAmount = 30;
    public int healthAmount = 50;

    [Header("=== ANIMACIÓN DE APERTURA ===")]
    [Tooltip("Arrastra aquí la TAPA de la caja (el objeto hijo que debe rotar)")]
    public Transform boxLid;
    [Tooltip("Ángulo de apertura de la tapa (prueba valores positivos o negativos según la orientación)")]
    public float openAngle = -110f;
    [Tooltip("Eje de rotación LOCAL de la tapa: X, Y o Z")]
    public enum RotationAxis { X, Y, Z }
    public RotationAxis rotationAxis = RotationAxis.X;  // X suele ser el correcto para tapas
    [Tooltip("Duración de la animación de apertura")]
    public float openDuration = 0.5f;
    [Tooltip("Tiempo que la caja permanece abierta antes de cerrarse")]
    public float timeBeforeClose = 1.5f;

    [Header("=== ANIMACIÓN DE REBOTE ===")]
    [Tooltip("Duración del efecto de rebote")]
    public float bounceDuration = 0.4f;
    [Tooltip("Intensidad del rebote")]
    public float bounceIntensity = 0.15f;

    [Header("=== AUDIO (Opcional) ===")]
    public AudioClip buySound;
    public AudioClip noMoneySound;

    // Estados internos
    private bool playerInRange = false;
    private bool isAnimating = false;
    private AudioSource audioSource;
    private GUIStyle promptStyle;
    private GUIStyle shadowStyle;
    
    // Rotaciones de la tapa
    private Quaternion lidClosedRotation;
    private Quaternion lidOpenRotation;

    public enum BoxType
    {
        Ammo,
        Health
    }

    void Start()
    {
        // Configurar audio
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.spatialBlend = 0.3f; // Más 2D para que se escuche bien de cerca
        audioSource.playOnAwake = false;
        audioSource.volume = 1f;
        audioSource.minDistance = 2f;
        audioSource.maxDistance = 15f;

        // Verificar que tiene un Trigger Collider
        Collider col = GetComponent<Collider>();
        if (col == null)
        {
            // Crear uno automáticamente
            BoxCollider box = gameObject.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = new Vector3(2f, 2f, 2f);
            Debug.LogWarning("[InteractableBox] Se creó un Box Collider Trigger automáticamente en " + gameObject.name);
        }
        else if (!col.isTrigger)
        {
            // Si tiene collider pero NO es trigger, añadir uno extra
            BoxCollider triggerCol = gameObject.AddComponent<BoxCollider>();
            triggerCol.isTrigger = true;
            triggerCol.size = new Vector3(2f, 2f, 2f);
            Debug.LogWarning("[InteractableBox] El collider no era Trigger. Se añadió un segundo collider Trigger en " + gameObject.name);
        }

        // Configurar rotaciones de la tapa
        if (boxLid != null)
        {
            lidClosedRotation = boxLid.localRotation;
            // Calcular rotación según el eje seleccionado
            Vector3 rotationEuler = Vector3.zero;
            switch (rotationAxis)
            {
                case RotationAxis.X:
                    rotationEuler = new Vector3(openAngle, 0, 0);
                    break;
                case RotationAxis.Y:
                    rotationEuler = new Vector3(0, openAngle, 0);
                    break;
                case RotationAxis.Z:
                    rotationEuler = new Vector3(0, 0, openAngle);
                    break;
            }
            
            lidOpenRotation = lidClosedRotation * Quaternion.Euler(rotationEuler);
            Debug.Log("[InteractableBox] Tapa configurada en " + gameObject.name + " - Eje: " + rotationAxis);
        }
        else
        {
            Debug.LogWarning("[InteractableBox] No hay tapa asignada en " + gameObject.name + ". Se usará animación de rebote.");
        }

        // Estilos de texto
        promptStyle = new GUIStyle();
        promptStyle.fontSize = 26;
        promptStyle.fontStyle = FontStyle.Bold;
        promptStyle.alignment = TextAnchor.MiddleCenter;
        promptStyle.normal.textColor = Color.white;

        shadowStyle = new GUIStyle(promptStyle);
        shadowStyle.normal.textColor = Color.black;

        Debug.Log("[InteractableBox] Caja '" + gameObject.name + "' lista. Tipo: " + boxType);
    }

    void Update()
    {
        // Permitir comprar múltiples veces (solo verificar que no esté animando)
        if (playerInRange && !isAnimating && Input.GetKeyDown(KeyCode.E))
        {
            TryPurchase();
        }
    }

    void TryPurchase()
    {
        PlayerMoney pm = PlayerMoney.GetOrCreate();
        Debug.Log($"[InteractableBox] Intentando comprar, dinero actual: {pm.currentMoney}, precio: {price}");

        if (pm.SpendMoney(price))
        {
            // COMPRA EXITOSA
            Debug.Log("[InteractableBox] ¡Compra exitosa! -$" + price);

            // Dar item
            if (boxType == BoxType.Ammo)
            {
                // Buscar el WeaponSwitcher del jugador para dar munición a TODAS las armas
                WeaponSwitcher switcher = FindObjectOfType<WeaponSwitcher>();
                if (switcher != null && switcher.weapons != null)
                {
                    foreach (FPSWeaponController weapon in switcher.weapons)
                    {
                        if (weapon != null)
                        {
                            weapon.AddAmmo(ammoAmount);
                        }
                    }
                    Debug.Log("[InteractableBox] +" + ammoAmount + " munición a todas las armas");
                }
                else
                {
                    // Fallback: buscar el arma activa directamente
                    FPSWeaponController activeWeapon = FindObjectOfType<FPSWeaponController>();
                    if (activeWeapon != null)
                    {
                        activeWeapon.AddAmmo(ammoAmount);
                        Debug.Log("[InteractableBox] +" + ammoAmount + " munición al arma activa");
                    }
                    else
                    {
                        Debug.LogWarning("[InteractableBox] No se encontró ningún arma para dar munición");
                    }
                }
            }
            else
            {
                Debug.Log("[InteractableBox] +" + healthAmount + " vida");
                // Buscar PlayerHealth en toda la escena
                PlayerHealth health = FindObjectOfType<PlayerHealth>();
                if (health != null)
                {
                    health.Heal(healthAmount);
                }
            }

            // Sonido
            if (buySound != null) audioSource.PlayOneShot(buySound, 1f);

            // ANIMACIÓN DE APERTURA (y luego cierre automático)
            if (boxLid != null)
            {
                StartCoroutine(OpenAndCloseLidAnimation());
            }
            else
            {
                StartCoroutine(BounceAnimation());
            }
        }
        else
        {
            Debug.Log("[InteractableBox] No tienes suficiente dinero. Necesitas $" + price);
            if (noMoneySound != null) audioSource.PlayOneShot(noMoneySound, 1f);

            // Pequeño shake para indicar que no se puede
            StartCoroutine(ShakeAnimation());
        }
    }

    IEnumerator BounceAnimation()
    {
        isAnimating = true;
        Vector3 originalScale = transform.localScale;
        Vector3 originalPos = transform.localPosition;
        float elapsed = 0f;

        // Fase 1: Encoger un poco
        while (elapsed < bounceDuration * 0.3f)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / (bounceDuration * 0.3f);
            transform.localScale = Vector3.Lerp(originalScale, originalScale * (1f - bounceIntensity), t);
            yield return null;
        }

        // Fase 2: Expandir (rebote)
        elapsed = 0f;
        Vector3 shrunkScale = transform.localScale;
        while (elapsed < bounceDuration * 0.4f)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / (bounceDuration * 0.4f);
            transform.localScale = Vector3.Lerp(shrunkScale, originalScale * (1f + bounceIntensity), t);
            // Subir un poco
            transform.localPosition = originalPos + Vector3.up * (bounceIntensity * t);
            yield return null;
        }

        // Fase 3: Volver al tamaño original
        elapsed = 0f;
        Vector3 expandedScale = transform.localScale;
        Vector3 currentPos = transform.localPosition;
        while (elapsed < bounceDuration * 0.3f)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / (bounceDuration * 0.3f);
            transform.localScale = Vector3.Lerp(expandedScale, originalScale, t);
            transform.localPosition = Vector3.Lerp(currentPos, originalPos, t);
            yield return null;
        }

        transform.localScale = originalScale;
        transform.localPosition = originalPos;
        isAnimating = false;
    }

    // ANIMACIÓN DE APERTURA Y CIERRE AUTOMÁTICO
    IEnumerator OpenAndCloseLidAnimation()
    {
        isAnimating = true;
        float elapsed = 0f;

        Debug.Log("[InteractableBox] Abriendo tapa de " + gameObject.name);

        // ABRIR
        while (elapsed < openDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0, 1, elapsed / openDuration);
            boxLid.localRotation = Quaternion.Slerp(lidClosedRotation, lidOpenRotation, t);
            yield return null;
        }
        boxLid.localRotation = lidOpenRotation;

        Debug.Log("[InteractableBox] Tapa abierta! Cerrando en " + timeBeforeClose + " segundos...");

        // ESPERAR
        yield return new WaitForSeconds(timeBeforeClose);

        // CERRAR
        elapsed = 0f;
        while (elapsed < openDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0, 1, elapsed / openDuration);
            boxLid.localRotation = Quaternion.Slerp(lidOpenRotation, lidClosedRotation, t);
            yield return null;
        }
        boxLid.localRotation = lidClosedRotation;

        Debug.Log("[InteractableBox] Tapa cerrada! Lista para volver a comprar.");
        isAnimating = false;
    }

    IEnumerator ShakeAnimation()
    {
        isAnimating = true;
        Vector3 originalPos = transform.localPosition;
        float elapsed = 0f;
        float shakeDuration = 0.3f;

        while (elapsed < shakeDuration)
        {
            elapsed += Time.deltaTime;
            float x = Random.Range(-0.05f, 0.05f);
            float z = Random.Range(-0.05f, 0.05f);
            transform.localPosition = originalPos + new Vector3(x, 0, z);
            yield return null;
        }

        transform.localPosition = originalPos;
        isAnimating = false;
    }

    // DETECCIÓN POR TRIGGER - el jugador entra/sale de la zona
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = true;
            Debug.Log("[InteractableBox] Jugador cerca de " + gameObject.name);
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
        }
    }

    // TEXTO EN PANTALLA
    void OnGUI()
    {
        if (!playerInRange) return;

        string texto;
        if (isAnimating)
        {
            texto = "Abriendo...";
        }
        else
        {
            string item = boxType == BoxType.Ammo ? "Munición +" + ammoAmount : "Botiquín +" + healthAmount;
            texto = "Pulsa E - " + item + " ($" + price + ")";
        }

        float x = Screen.width / 2f - 200;
        float y = Screen.height / 2f + 60;

        // Sombra
        GUI.color = Color.black;
        GUI.Label(new Rect(x + 2, y + 2, 400, 50), texto, shadowStyle);

        // Texto
        GUI.color = isAnimating ? Color.gray : Color.yellow;
        GUI.Label(new Rect(x, y, 400, 50), texto, promptStyle);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = boxType == BoxType.Ammo ? Color.green : Color.red;
        Gizmos.DrawWireCube(transform.position, new Vector3(2f, 2f, 2f));
    }
}
