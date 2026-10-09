using UnityEngine;
using UnityEngine.EventSystems;

public class ItemHover : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler
{
    public int itemSlot;
    public BattleManager battleManager;

    public void OnPointerEnter(PointerEventData eventData)
    {
        string description =
            battleManager.GetItemDescription(itemSlot);

        if (!string.IsNullOrEmpty(description))
        {
            battleManager.ShowItemDescription(description);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        battleManager.HideItemDescription();
    }
}