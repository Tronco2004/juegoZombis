using UnityEngine;
using System.Collections;

/// <summary>
/// Tipo de animación al comprar
/// </summary>
public enum AnimationType
{
    Door,
    MoveUp,
    Disappear
}

/// <summary>
/// Objeto interactivo comprable (puertas, vallas, etc.)
/// Acércate y pulsa E para comprar
/// </summary>
public class InteractablePurchasable : MonoBehaviour
{
    [Header("=== COMPRA ===")]
    public int price = 1000;
    public string objectName = "Valla";

    [Header("=== INTERACCIÓN ===")]
    public float interactionDistance = 10f;
    public KeyCode interactKey = KeyCode.E;

    [Header("=== ANIMACIÓN ===")]
    public AnimationType animationType = AnimationType.Disappear;
    public float moveDistance = 5f;
    public float openAngle = 90f;
    public float animationDuration = 0.5f;

    [Header("=== AUDIO ===")]
    public AudioClip successSound;
    public AudioClip errorSound;

    [Header("=== DEBUG ===")]
    public bool showDebugInfo = true;

    private bool isPurchased = false;
    private bool isAnimating = false;
    private bool isNearby = false;
    private Transform player;
    private AudioSource audioSource;
    private GUIStyle labelStyle;

    void Start()
    {
        Debug.Log($"[VALLA] START: {objectName} en {gameObject.name}");

        // Buscar jugador
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj == null)
        {
            // Buscar por componente
            PlayerMoney pm = FindObjectOfType<PlayerMoney>();
            if (pm != null) playerObj = pm.gameObject;
        }
        if (playerObj == null)
        {
            // Buscar por nombre
            playerObj = GameObject.Find("Player");
        }

        if (playerObj != null)
        {
            player = playerObj.transform;
            Debug.Log($"[VALLA] Jugador encontrado: {playerObj.name}");
        }
        else
        {
            Debug.LogError("[VALLA] ERROR: No se encontró al jugador!");
        }

        // Audio
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 1f;
            audioSource.playOnAwake = false;
        }

        // Estilo GUI
        labelStyle = new GUIStyle();
        labelStyle.fontSize = 20;
        labelStyle.normal.textColor = Color.white;
        labelStyle.wordWrap = true;
    }

    void Update()
    {
        if (isPurchased || isAnimating) return;

        // Si no hay jugador, seguir buscando
        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
            return;
        }

        // Detección SIMPLE: solo distancia
        float dist = Vector3.Distance(transform.position, player.position);
        isNearby = dist <= interactionDistance;

        if (isNearby && Input.GetKeyDown(interactKey))
        {
            TryPurchase();
        }
    }

    void TryPurchase()
    {
        Debug.Log($"[VALLA] Intentando comprar {objectName}...");

        if (PlayerMoney.Instance == null)
        {
            Debug.LogError("[VALLA] PlayerMoney.Instance es NULL!");
            return;
        }

        if (!PlayerMoney.Instance.HasEnoughMoney(price))
        {
            Debug.Log($"[VALLA] Sin dinero! Tienes: {PlayerMoney.Instance.currentMoney}, necesitas: {price}");
            if (errorSound != null) audioSource.PlayOneShot(errorSound);
            return;
        }

        if (PlayerMoney.Instance.SpendMoney(price))
        {
            Debug.Log($"[VALLA] ¡{objectName} COMPRADO! -{price}$");
            if (successSound != null) audioSource.PlayOneShot(successSound);
            isPurchased = true;

            switch (animationType)
            {
                case AnimationType.Disappear:
                    Destroy(gameObject);
                    break;
                case AnimationType.MoveUp:
                    StartCoroutine(MoveUpAndDestroy());
                    break;
                case AnimationType.Door:
                    StartCoroutine(OpenDoor());
                    break;
            }
        }
    }

    IEnumerator MoveUpAndDestroy()
    {
        isAnimating = true;
        Vector3 start = transform.position;
        Vector3 end = start + Vector3.up * moveDistance;
        float t = 0f;

        while (t < animationDuration)
        {
            t += Time.deltaTime;
            transform.position = Vector3.Lerp(start, end, t / animationDuration);
            yield return null;
        }

        yield return new WaitForSeconds(0.3f);
        Destroy(gameObject);
    }

    IEnumerator OpenDoor()
    {
        isAnimating = true;
        Quaternion startRot = transform.localRotation;
        Quaternion endRot = startRot * Quaternion.Euler(0, openAngle, 0);
        float t = 0f;

        while (t < animationDuration)
        {
            t += Time.deltaTime;
            transform.localRotation = Quaternion.Lerp(startRot, endRot, t / animationDuration);
            yield return null;
        }

        isAnimating = false;
    }

    void OnGUI()
    {
        if (isPurchased) return;

        // === SIEMPRE mostrar debug para diagnosticar ===
        if (showDebugInfo)
        {
            string playerInfo = player != null ? player.name : "NO ENCONTRADO";
            float dist = player != null ? Vector3.Distance(transform.position, player.position) : -1f;
            string moneyInfo = PlayerMoney.Instance != null ? PlayerMoney.Instance.currentMoney.ToString() : "NULL";

            GUI.color = Color.green;
            GUI.Label(new Rect(10, 10, 500, 200),
                $"=== VALLA DEBUG ===\n" +
                $"Script activo: SI\n" +
                $"Player: {playerInfo}\n" +
                $"Distancia: {dist:F1} / {interactionDistance}\n" +
                $"Cerca: {isNearby}\n" +
                $"Dinero: {moneyInfo}\n" +
                $"Precio: {price}",
                labelStyle);
        }

        // === Cartel de compra ===
        if (isNearby && !isAnimating)
        {
            bool canAfford = PlayerMoney.Instance != null &&
                             PlayerMoney.Instance.HasEnoughMoney(price);

            string texto = canAfford
                ? $"[E] Comprar {objectName} (${price})"
                : $"{objectName} (${price}) - SIN DINERO";

            // Estilo del cartel
            GUIStyle style = new GUIStyle(GUI.skin.box);
            style.fontSize = 30;
            style.fontStyle = FontStyle.Bold;
            style.alignment = TextAnchor.MiddleCenter;
            style.normal.textColor = canAfford ? Color.yellow : Color.red;
            style.normal.background = Texture2D.grayTexture;

            float w = 500;
            float h = 60;
            float x = (Screen.width - w) / 2f;
            float y = Screen.height / 2f + 50;

            GUI.Label(new Rect(x, y, w, h), texto, style);
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, interactionDistance);
    }
}
