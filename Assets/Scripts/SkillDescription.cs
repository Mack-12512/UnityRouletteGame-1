using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;
public class SkillDescription : MonoBehaviour
{
    public GameObject descriptionPanel;
    public TMP_Text descriptionText;

    private RectTransform panelRect;

    void Start()
    {
        panelRect = descriptionPanel.GetComponent<RectTransform>();
        Hide();
    }

    void Update()
    {
        if (descriptionPanel.activeSelf && Mouse.current != null)
        {
            Vector2 mousePos = Mouse.current.position.ReadValue();

            // 설명창 왼쪽 아래 꼭짓점을 마우스에 붙임
            panelRect.position = mousePos;
        }
    }

    public void Show(string description)
    {
        if (string.IsNullOrEmpty(description))
        {
            Hide();
            return;
        }

        descriptionText.text = description;
        descriptionPanel.SetActive(true);
    }

    public void Hide()
    {
        descriptionPanel.SetActive(false);
    }
}