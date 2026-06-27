using UnityEngine;
using TMPro;

/// <summary>
/// 物品提示框。鼠标悬停在背包物品上时显示名称、描述和效果信息。
/// </summary>
public class InventoryTooltip : MonoBehaviour
{
    public static InventoryTooltip Instance { get; private set; }

    [Header("=== UI 引用 ===")]
    public GameObject tooltipPanel;
    public TextMeshProUGUI itemNameText;
    public TextMeshProUGUI itemTypeText;
    public TextMeshProUGUI descriptionText;
    public TextMeshProUGUI effectText;

    [Header("=== 外观设置 ===")]
    public Vector2 offset = new Vector2(16, -16);
    public float paddingFromEdge = 20f;
    public Color consumableColor = new Color(0.4f, 0.9f, 0.4f);
    public Color equipmentColor = new Color(0.4f, 0.6f, 1f);
    public Color materialColor = new Color(0.8f, 0.8f, 0.8f);
    public Color questColor = new Color(1f, 0.85f, 0.3f);

    private RectTransform panelRect;
    private Canvas parentCanvas;
    private ItemData currentData;
    private float updateTimer;

    void Awake()
    {
        Instance = this;

        if (tooltipPanel == null)
            tooltipPanel = transform.Find("TooltipPanel")?.gameObject;
        if (tooltipPanel != null)
            panelRect = tooltipPanel.GetComponent<RectTransform>();
        if (parentCanvas == null)
            parentCanvas = GetComponentInParent<Canvas>();

        Hide();
    }

    void Update()
    {
        if (currentData == null || tooltipPanel == null || !tooltipPanel.activeSelf) return;

        updateTimer += Time.unscaledDeltaTime;
        if (updateTimer >= 0.05f) // 每秒最多 20 次更新
        {
            updateTimer = 0f;
            UpdatePosition();
        }
    }

    /// <summary>显示物品提示</summary>
    public void Show(ItemData data, Vector2 screenPos)
    {
        if (data == null || tooltipPanel == null) return;

        currentData = data;

        // 名称
        if (itemNameText != null)
        {
            itemNameText.text = data.itemName;
            itemNameText.color = GetTypeColor(data.itemType);
        }

        // 类型
        if (itemTypeText != null)
        {
            itemTypeText.text = GetTypeLabel(data.itemType);
            itemTypeText.color = GetTypeColor(data.itemType);
        }

        // 描述
        if (descriptionText != null)
            descriptionText.text = data.description;

        // 效果
        if (effectText != null)
        {
            effectText.text = BuildEffectText(data);
            effectText.gameObject.SetActive(!string.IsNullOrEmpty(effectText.text));
        }

        tooltipPanel.SetActive(true);
        UpdatePosition(screenPos);
    }

    /// <summary>隐藏提示框</summary>
    public void Hide()
    {
        currentData = null;
        if (tooltipPanel != null)
            tooltipPanel.SetActive(false);
    }

    /// <summary>更新位置（不带参数时跟随鼠标，带参数时定位到指定位置）</summary>
    public void UpdatePosition()
    {
        UpdatePosition(Input.mousePosition);
    }

    public void UpdatePosition(Vector2 screenPos)
    {
        if (panelRect == null || parentCanvas == null) return;

        // 将屏幕坐标转换为 Canvas 本地坐标
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentCanvas.transform as RectTransform,
            screenPos + offset,
            parentCanvas.worldCamera,
            out Vector2 localPos);

        // 边界检测：确保不超出屏幕
        Vector2 panelSize = panelRect.sizeDelta;
        RectTransform canvasRect = parentCanvas.transform as RectTransform;
        float canvasWidth = canvasRect.sizeDelta.x;
        float canvasHeight = canvasRect.sizeDelta.y;

        // 右边界
        float maxX = canvasWidth / 2f - panelSize.x - paddingFromEdge;
        float minX = -canvasWidth / 2f + paddingFromEdge;
        localPos.x = Mathf.Clamp(localPos.x, minX, maxX);

        // 下边界
        float maxY = canvasHeight / 2f - paddingFromEdge;
        float minY = -canvasHeight / 2f + panelSize.y + paddingFromEdge;
        localPos.y = Mathf.Clamp(localPos.y, minY, maxY);

        panelRect.localPosition = localPos;
    }

    // ==================== 辅助方法 ====================

    private string GetTypeLabel(ItemType type)
    {
        switch (type)
        {
            case ItemType.Consumable: return "消耗品";
            case ItemType.Equipment: return "装备";
            case ItemType.Material: return "材料";
            case ItemType.Quest: return "任务物品";
            default: return "其他";
        }
    }

    private Color GetTypeColor(ItemType type)
    {
        switch (type)
        {
            case ItemType.Consumable: return consumableColor;
            case ItemType.Equipment: return equipmentColor;
            case ItemType.Material: return materialColor;
            case ItemType.Quest: return questColor;
            default: return Color.white;
        }
    }

    private string BuildEffectText(ItemData data)
    {
        if (data == null || !data.HasEffect) return "";

        var sb = new System.Text.StringBuilder();
        if (data.effect.healthRestore > 0)
            sb.AppendLine($"<color=#ff6666>❤ 回复 {data.effect.healthRestore} 生命</color>");
        if (data.effect.manaRestore > 0)
            sb.AppendLine($"<color=#6666ff>◆ 回复 {data.effect.manaRestore} 法力</color>");
        if (data.effect.duration > 0)
            sb.AppendLine($"持续 {data.effect.duration:F1} 秒");

        return sb.ToString().TrimEnd();
    }
}
