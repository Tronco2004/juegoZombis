using UnityEngine;

/// <summary>
/// Barra de vida que aparece sobre los enemigos - Versión CORREGIDA con SpriteRenderer
/// Se añade automáticamente desde EnemyHealth
/// </summary>
public class EnemyHealthBar : MonoBehaviour
{
    [Header("Configuración")]
    public float barWidth = 1.2f;
    public float barHeight = 0.15f;
    public float heightAboveEnemy = 2.2f;
    public bool alwaysVisible = false;
    public float visibleDuration = 3f;
    
    [Header("Colores")]
    public Color backgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.9f);
    public Color healthColorFull = new Color(0.2f, 0.9f, 0.2f, 1f);
    public Color healthColorMid = new Color(0.9f, 0.9f, 0.2f, 1f);
    public Color healthColorLow = new Color(0.9f, 0.2f, 0.2f, 1f);
    
    private Transform target;
    private EnemyHealth enemyHealth;
    private float lastDamageTime;
    private bool isVisible = false;
    
    // Sprites para mejor billboard
    private SpriteRenderer bgSprite;
    private SpriteRenderer fgSprite;
    private Transform fgTransform;
    private static Sprite whiteSprite;
    
    public void Initialize(Transform enemyTransform, EnemyHealth health)
    {
        target = enemyTransform;
        enemyHealth = health;
        lastDamageTime = -visibleDuration;
        
        CreateHealthBar();
    }
    
    void CreateHealthBar()
    {
        // Crear sprite blanco una sola vez (compartido)
        if (whiteSprite == null)
        {
            Texture2D whiteTex = new Texture2D(4, 4);
            Color[] colors = new Color[16];
            for (int i = 0; i < 16; i++) colors[i] = Color.white;
            whiteTex.SetPixels(colors);
            whiteTex.Apply();
            whiteSprite = Sprite.Create(whiteTex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
        }
        
        // Crear fondo
        GameObject bgObj = new GameObject("HealthBar_BG");
        bgObj.transform.SetParent(transform);
        bgObj.transform.localPosition = Vector3.zero;
        bgObj.transform.localScale = new Vector3(barWidth, barHeight, 1f);
        
        bgSprite = bgObj.AddComponent<SpriteRenderer>();
        bgSprite.sprite = whiteSprite;
        bgSprite.color = backgroundColor;
        bgSprite.sortingOrder = 100;
        
        // Crear barra de vida (foreground)
        GameObject fgObj = new GameObject("HealthBar_FG");
        fgObj.transform.SetParent(transform);
        fgObj.transform.localPosition = new Vector3(0, 0, -0.001f);
        fgObj.transform.localScale = new Vector3(barWidth * 0.95f, barHeight * 0.7f, 1f);
        fgTransform = fgObj.transform;
        
        fgSprite = fgObj.AddComponent<SpriteRenderer>();
        fgSprite.sprite = whiteSprite;
        fgSprite.color = healthColorFull;
        fgSprite.sortingOrder = 101;
        
        SetVisible(true);
    }
    
    void LateUpdate()
    {
        if (target == null || enemyHealth == null)
        {
            Destroy(gameObject);
            return;
        }
        
        // Posicionar sobre el enemigo
        transform.position = target.position + Vector3.up * heightAboveEnemy;
        
        // BILLBOARD: Hacer que siempre mire a la cámara
        if (Camera.main != null)
        {
            // Mirar hacia la cámara
            Vector3 lookDir = transform.position - Camera.main.transform.position;
            transform.rotation = Quaternion.LookRotation(lookDir);
        }
        
        // Actualizar barra de vida
        UpdateHealthBar();
        
        // Visibilidad
        float healthPercent = enemyHealth.currentHealth / enemyHealth.maxHealth;
        
        if (alwaysVisible)
        {
            if (!isVisible) SetVisible(true);
        }
        else
        {
            bool shouldShow = (Time.time - lastDamageTime < visibleDuration) || (healthPercent < 0.99f);
            
            if (shouldShow && !isVisible)
            {
                SetVisible(true);
            }
            else if (!shouldShow && isVisible)
            {
                SetVisible(false);
            }
        }
    }
    
    void UpdateHealthBar()
    {
        if (enemyHealth == null || fgSprite == null || fgTransform == null) return;
        
        float healthPercent = Mathf.Clamp01(enemyHealth.currentHealth / enemyHealth.maxHealth);
        
        // Escalar la barra según la vida
        float fullWidth = barWidth * 0.95f;
        fgTransform.localScale = new Vector3(fullWidth * healthPercent, barHeight * 0.7f, 1f);
        
        // Mover para que se reduzca desde la derecha (pivote a la izquierda)
        float offset = (fullWidth - fullWidth * healthPercent) / 2f;
        fgTransform.localPosition = new Vector3(-offset, 0, -0.001f);
        
        // Color según porcentaje
        Color healthColor;
        if (healthPercent > 0.6f)
        {
            healthColor = healthColorFull;
        }
        else if (healthPercent > 0.3f)
        {
            healthColor = Color.Lerp(healthColorLow, healthColorMid, (healthPercent - 0.3f) / 0.3f);
        }
        else
        {
            healthColor = healthColorLow;
        }
        
        fgSprite.color = healthColor;
    }
    
    public void OnDamaged()
    {
        lastDamageTime = Time.time;
        if (!isVisible && !alwaysVisible)
        {
            SetVisible(true);
        }
    }
    
    void SetVisible(bool visible)
    {
        isVisible = visible;
        if (bgSprite != null) bgSprite.enabled = visible;
        if (fgSprite != null) fgSprite.enabled = visible;
    }
}
