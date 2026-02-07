using UnityEngine;
using System.Collections;

public class VallaPurchasable : MonoBehaviour
{
    [Header("CONFIG")]
    public int price = 1000;
    public string itemName = "Valla";
    public float moveDistance = 5f;
    public float moveDuration = 0.5f;
    
    private bool purchased = false;
    private Camera cam;
    private AudioSource audioSource;
    
    void Start()
    {
        Debug.Log("[VALLA] INICIADO");
        cam = Camera.main;
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
    }
    
    void Update()
    {
        if (purchased) return;
        
        if (cam == null) return;
        
        Ray ray = cam.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2));
        RaycastHit hit;
        
        if (Physics.Raycast(ray, out hit, 5f))
        {
            if (IsPartOfMe(hit.transform))
            {
                if (Input.GetKeyDown(KeyCode.E))
                {
                    TryBuy();
                }
            }
        }
    }
    
    bool IsPartOfMe(Transform t)
    {
        while (t != null)
        {
            if (t == transform) return true;
            t = t.parent;
        }
        return false;
    }
    
    void TryBuy()
    {
        Debug.Log("[VALLA] E PRESIONADO");
        
        if (PlayerMoney.Instance == null)
        {
            Debug.LogError("[VALLA] NO HAY PlayerMoney");
            return;
        }
        
        Debug.Log($"[VALLA] Dinero actual: ${PlayerMoney.Instance.currentMoney}, Precio: ${price}");
        
        if (PlayerMoney.Instance.SpendMoney(price))
        {
            Debug.Log("[VALLA] COMPRADO!");
            purchased = true;
            StartCoroutine(MoveUp());
        }
        else
        {
            Debug.Log("[VALLA] NO HAY DINERO SUFICIENTE");
        }
    }
    
    IEnumerator MoveUp()
    {
        Debug.Log("[VALLA] SUBIENDO...");
        float elapsed = 0f;
        Vector3 start = transform.position;
        Vector3 end = start + Vector3.up * moveDistance;
        
        while (elapsed < moveDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / moveDuration;
            transform.position = Vector3.Lerp(start, end, t);
            yield return null;
        }
        
        transform.position = end;
        Debug.Log("[VALLA] COMPLETADO");
    }
}
