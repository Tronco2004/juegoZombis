using UnityEngine;

/// <summary>
/// Script simple de TEST para verificar que el raycast funciona
/// Asigna esto al GameObject de la valla temporalmente
/// </summary>
public class PurchasableTest : MonoBehaviour
{
    public int price = 1000;
    public string objectName = "Valla";
    public AnimationType animationType = AnimationType.MoveUp;
    public float moveDistance = 5f;
    public float animationDuration = 0.5f;
    
    private Camera playerCamera;
    private bool purchased = false;
    
    void Start()
    {
        playerCamera = Camera.main;
        Debug.Log($"[TEST] ✓ PurchasableTest iniciado en: {gameObject.name}");
        Debug.Log($"[TEST] Objetos con Collider en {gameObject.name}: {GetComponent<Collider>() != null}");
        
        // Revisar colliders en hijos
        Collider[] collidersInChildren = GetComponentsInChildren<Collider>();
        Debug.Log($"[TEST] Colliders totales (incluyendo hijos): {collidersInChildren.Length}");
        foreach (var col in collidersInChildren)
        {
            Debug.Log($"[TEST] - {col.gameObject.name} tiene {col.GetType().Name}");
        }
    }
    
    void Update()
    {
        if (purchased) return;
        
        if (playerCamera == null) return;
        
        // Raycast
        Ray ray = playerCamera.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));
        RaycastHit hit;
        
        Debug.DrawRay(ray.origin, ray.direction * 10, Color.yellow);
        
        if (Physics.Raycast(ray, out hit, 10))
        {
            // Mirar qué golpeamos
            if (hit.transform.IsChildOf(transform) || hit.transform == transform)
            {
                Debug.Log($"[TEST] ✓ Raycast golpeó {objectName}!");
                
                if (Input.GetKeyDown(KeyCode.E))
                {
                    Debug.Log($"[TEST] ➤ E PRESIONADO en {objectName}!");
                    DoPurchase();
                }
            }
        }
    }
    
    void DoPurchase()
    {
        Debug.Log($"[TEST] Comprando {objectName}...");
        
        if (PlayerMoney.Instance == null)
        {
            Debug.LogError($"[TEST] ✗ PlayerMoney NO EXISTE en la escena!");
            return;
        }
        
        if (PlayerMoney.Instance.SpendMoney(price))
        {
            Debug.Log($"[TEST] ✓ ¡Comprado!");
            purchased = true;
            
            if (animationType == AnimationType.MoveUp)
            {
                StartCoroutine(MoveUp());
            }
        }
        else
        {
            Debug.Log($"[TEST] ✗ No hay dinero! Tienes: ${PlayerMoney.Instance.currentMoney}");
        }
    }
    
    System.Collections.IEnumerator MoveUp()
    {
        Debug.Log($"[TEST] ► Subiendo {moveDistance}m...");
        float elapsed = 0;
        Vector3 start = transform.position;
        Vector3 end = start + Vector3.up * moveDistance;
        
        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / animationDuration);
            transform.position = Vector3.Lerp(start, end, t);
            yield return null;
        }
        
        transform.position = end;
        Debug.Log($"[TEST] ✓ Animación completada!");
    }
    
    void OnGUI()
    {
        GUI.Label(new Rect(10, 10, 300, 100), 
            $"[TEST] {objectName}\n" +
            $"Comprado: {purchased}\n" +
            $"Precio: ${price}");
    }
}
