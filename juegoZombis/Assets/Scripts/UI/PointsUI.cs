using UnityEngine;
using TMPro;

/// <summary>
/// UI para mostrar los puntos del jugador
/// </summary>
public class PointsUI : MonoBehaviour
{
    [Header("Referencias")]
    public TextMeshProUGUI pointsText;
    
    [Header("Formato")]
    public string prefix = ""; // Puede ser "$" o "Puntos: "
    public bool animateOnChange = true;
    public float animationScale = 1.3f;
    public float animationDuration = 0.2f;
    
    private Vector3 originalScale;
    private Coroutine scaleCoroutine;
    
    void Start()
    {
        // Buscar el texto si no está asignado
        if (pointsText == null)
        {
            pointsText = GetComponent<TextMeshProUGUI>();
        }
        
        if (pointsText != null)
        {
            originalScale = pointsText.transform.localScale;
        }
        
        // Suscribirse a cambios de puntos
        if (PlayerPoints.Instance != null)
        {
            PlayerPoints.Instance.OnPointsChanged += UpdatePointsDisplay;
            UpdatePointsDisplay(PlayerPoints.Instance.CurrentPoints);
        }
        else
        {
            // Esperar a que se cree el PlayerPoints
            StartCoroutine(WaitForPlayerPoints());
        }
    }
    
    System.Collections.IEnumerator WaitForPlayerPoints()
    {
        while (PlayerPoints.Instance == null)
        {
            yield return null;
        }
        
        PlayerPoints.Instance.OnPointsChanged += UpdatePointsDisplay;
        UpdatePointsDisplay(PlayerPoints.Instance.CurrentPoints);
    }
    
    void OnDestroy()
    {
        if (PlayerPoints.Instance != null)
        {
            PlayerPoints.Instance.OnPointsChanged -= UpdatePointsDisplay;
        }
    }
    
    void UpdatePointsDisplay(int points)
    {
        if (pointsText != null)
        {
            pointsText.text = prefix + points.ToString();
            
            if (animateOnChange)
            {
                AnimateScale();
            }
        }
    }
    
    void AnimateScale()
    {
        if (scaleCoroutine != null)
        {
            StopCoroutine(scaleCoroutine);
        }
        scaleCoroutine = StartCoroutine(ScaleAnimation());
    }
    
    System.Collections.IEnumerator ScaleAnimation()
    {
        float elapsed = 0f;
        float halfDuration = animationDuration / 2f;
        
        // Crecer
        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / halfDuration;
            pointsText.transform.localScale = Vector3.Lerp(originalScale, originalScale * animationScale, t);
            yield return null;
        }
        
        // Volver al tamaño original
        elapsed = 0f;
        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / halfDuration;
            pointsText.transform.localScale = Vector3.Lerp(originalScale * animationScale, originalScale, t);
            yield return null;
        }
        
        pointsText.transform.localScale = originalScale;
    }
}
