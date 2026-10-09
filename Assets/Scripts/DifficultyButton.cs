using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DifficultyButton : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler
{
    public float moveDistance = 30f;
    public float moveSpeed = 10f;

    public Color normalColor = Color.white;
    public Color hoverColor = Color.yellow;

    public AudioSource audioSource;
    public AudioClip hoverSound;

    private RectTransform rectTransform;
    private Image image;

    private Vector2 originalPosition;
    private Vector2 targetPosition;

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        image = GetComponent<Image>();

        originalPosition = rectTransform.anchoredPosition;
        targetPosition = originalPosition;
    }

    void Update()
    {
        rectTransform.anchoredPosition = Vector2.Lerp(
            rectTransform.anchoredPosition,
            targetPosition,
            Time.deltaTime * moveSpeed
        );
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        targetPosition = originalPosition + Vector2.left * moveDistance;

        image.color = hoverColor;

        if (audioSource != null && hoverSound != null)
        {
            audioSource.PlayOneShot(hoverSound);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        targetPosition = originalPosition;
        image.color = normalColor;
    }
}