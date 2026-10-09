using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class BattleHover : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler
{
    public float scaleAmount = 1.08f;
    public float scaleSpeed = 10f;

    public Color normalColor = Color.white;
    public Color hoverColor = Color.yellow;

    public AudioSource audioSource;
    public AudioClip hoverSound;

    private RectTransform rectTransform;
    private Image image;

    private Vector3 originalScale;
    private Vector3 targetScale;

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        image = GetComponent<Image>();

        originalScale = rectTransform.localScale;
        targetScale = originalScale;
    }

    void Update()
    {
        rectTransform.localScale = Vector3.Lerp(
            rectTransform.localScale,
            targetScale,
            Time.deltaTime * scaleSpeed
        );
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        targetScale = originalScale * scaleAmount;

        image.color = hoverColor;

        if (audioSource != null && hoverSound != null)
        {
            audioSource.PlayOneShot(hoverSound);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        targetScale = originalScale;
        image.color = normalColor;
    }
}