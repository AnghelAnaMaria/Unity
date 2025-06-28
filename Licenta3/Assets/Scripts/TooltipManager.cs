using UnityEngine;
using TMPro;

public class TooltipManager : MonoBehaviour
{
    public TextMeshProUGUI tooltipText;
    private RectTransform rectTransform;

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        gameObject.SetActive(false);
    }

    void Update()
    {
        if (!gameObject.activeSelf) return;

        UpdateTooltipPosition();
    }

    public void ShowTooltip(string text)
    {
        tooltipText.text = text;
        UpdateTooltipPosition();
        gameObject.SetActive(true);
    }

    public void HideTooltip()
    {
        gameObject.SetActive(false);
    }

    private void UpdateTooltipPosition()
    {
        Vector2 anchoredPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            transform.parent as RectTransform,      // canvasul
            Input.mousePosition,                    // poz mouse
            null,                                   // pentru Overlay = null
            out anchoredPos
        );

        rectTransform.anchoredPosition = anchoredPos + new Vector2(20f, -30f);
    }
}
