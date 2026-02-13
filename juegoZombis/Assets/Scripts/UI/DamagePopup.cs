using UnityEngine;

/// <summary>
/// Muestra números de daño flotantes cuando se golpea a un enemigo
/// Este script va en un Canvas que está en modo World Space
/// </summary>
public class DamagePopup : MonoBehaviour
{
    [Header("Configuración")]
    public float moveSpeed = 1f;
    public float fadeSpeed = 1f;
    public float lifetime = 1f;
    
    private TextMesh textMesh;
    private Color textColor;
    private float timer;
    private Vector3 moveDirection;
    
    void Awake()
    {
        textMesh = GetComponent<TextMesh>();
        if (textMesh == null)
        {
            textMesh = gameObject.AddComponent<TextMesh>();
        }
    }
    
    public void Setup(float damageAmount, bool isHeadshot, bool isCritical = false)
    {
        // Configurar texto - TAMAÑO GRANDE para que sea visible
        textMesh.text = Mathf.RoundToInt(damageAmount).ToString();
        textMesh.fontSize = isHeadshot ? 80 : 60; // Más grande
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.alignment = TextAlignment.Center;
        textMesh.characterSize = 0.5f; // MUCHO más grande para que se vea bien
        textMesh.fontStyle = FontStyle.Bold; // Negrita para mejor visibilidad
        
        // Color según tipo de daño
        if (isHeadshot)
        {
            textColor = Color.yellow; // Headshot = amarillo brillante
            textMesh.text = damageAmount.ToString("F0") + "!";
        }
        else if (isCritical)
        {
            textColor = new Color(1f, 0.5f, 0f); // Crítico = naranja
        }
        else
        {
            textColor = Color.red; // Normal = rojo
        }
        
        textMesh.color = textColor;
        
        // Dirección aleatoria de movimiento (solo hacia arriba y lados)
        moveDirection = new Vector3(Random.Range(-0.3f, 0.3f), 1f, 0f).normalized;
        
        timer = lifetime;
        
        // Escala inicial - MÁS GRANDE
        transform.localScale = Vector3.one * 0.15f;
        
        // Destruir después del tiempo de vida
        Destroy(gameObject, lifetime + 0.5f);
    }
    
    void Update()
    {
        // Mover hacia arriba en espacio local (relativo a la cámara)
        if (Camera.main != null)
        {
            // Mover en la dirección correcta respecto a la cámara
            Vector3 worldMoveDir = Camera.main.transform.TransformDirection(moveDirection);
            transform.position += worldMoveDir * moveSpeed * Time.deltaTime;
            
            // Billboard CORRECTO: Mirar hacia la cámara (no hacia donde mira la cámara)
            Vector3 dirToCamera = Camera.main.transform.position - transform.position;
            dirToCamera.y = 0; // Mantener vertical
            if (dirToCamera != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(-dirToCamera);
            }
        }
        else
        {
            transform.position += Vector3.up * moveSpeed * Time.deltaTime;
        }
        
        // Fade out
        timer -= Time.deltaTime;
        if (timer < lifetime * 0.5f)
        {
            float alpha = timer / (lifetime * 0.5f);
            textMesh.color = new Color(textColor.r, textColor.g, textColor.b, alpha);
        }
        
        // Escalar un poco al principio
        if (timer > lifetime * 0.8f)
        {
            float scale = 1f + (timer - lifetime * 0.8f) * 2f;
            transform.localScale = Vector3.one * scale * 0.15f; // Escala más grande
        }
    }
    
    /// <summary>
    /// Crea un popup de daño en una posición
    /// </summary>
    public static void Create(Vector3 position, float damage, bool isHeadshot)
    {
        // Crear objeto
        GameObject popup = new GameObject("DamagePopup");
        popup.transform.position = position + Vector3.up * 1f + Random.insideUnitSphere * 0.3f;
        
        // Añadir componentes
        DamagePopup dp = popup.AddComponent<DamagePopup>();
        dp.Setup(damage, isHeadshot);
    }
}
