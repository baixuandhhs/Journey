using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 右键菜单。在背包物品上右键时弹出"使用/丢弃/拆分"操作面板。
/// </summary>
public class ContextMenu : MonoBehaviour
{
    public static ContextMenu Instance { get; private set; }

    [Header("=== UI 引用 ===")]
    public GameObject menuPanel;
    public Button useButton;
    public Button dropButton;
    public Button splitButton;
    public TextMeshProUGUI useButtonText;
    public TextMeshProUGUI dropButtonText;

    [Header("=== 拆分面板 ===")]
    public GameObject splitPanel;
    public Slider splitSlider;
    public TextMeshProUGUI splitCountText;
    public Button splitConfirmButton;
    public Button splitCancelButton;

    [Header("=== 外观设置 ===")]
    public Vector2 offset = new Vector2(10, -10);

    private int currentSlotIndex;
    private InventoryItem currentItem;
    private Canvas parentCanvas;
    private RectTransform menuRect;

    void Awake()
    {
        Instance = this;

        if (menuPanel == null)
            menuPanel = transform.Find("ContextMenuPanel")?.gameObject;
        if (menuPanel != null)
            menuRect = menuPanel.GetComponent<RectTransform>();
        if (parentCanvas == null)
            parentCanvas = GetComponentInParent<Canvas>();

        // 绑定按钮事件
        if (useButton != null) useButton.onClick.AddListener(OnUseClicked);
        if (dropButton != null) dropButton.onClick.AddListener(OnDropClicked);
        if (splitButton != null) splitButton.onClick.AddListener(OnSplitClicked);
        if (splitConfirmButton != null) splitConfirmButton.onClick.AddListener(OnSplitConfirm);
        if (splitCancelButton != null) splitCancelButton.onClick.AddListener(OnSplitCancel);
        if (splitSlider != null) splitSlider.onValueChanged.AddListener(OnSliderChanged);

        Hide();
    }

    void Update()
    {
        // 点击菜单外部关闭
        if (menuPanel != null && menuPanel.activeSelf)
        {
            if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1))
            {
                // 检查鼠标是否点击在菜单上
                if (!RectTransformUtility.RectangleContainsScreenPoint(
                    menuRect, Input.mousePosition, parentCanvas?.worldCamera))
                {
                    Hide();
                }
            }
        }
    }

    /// <summary>显示右键菜单</summary>
    public void Show(int slotIndex, InventoryItem item, Vector2 screenPos)
    {
        if (item == null || item.IsEmpty) return;

        currentSlotIndex = slotIndex;
        currentItem = item;

        // 更新按钮状态
        bool isConsumable = item.itemData.itemType == ItemType.Consumable;
        bool hasEffect = item.itemData.HasEffect;

        if (useButton != null)
        {
            useButton.interactable = isConsumable && hasEffect;
            if (useButtonText != null)
                useButtonText.color = useButton.interactable ? Color.white : Color.gray;
        }

        if (dropButton != null)
        {
            if (dropButtonText != null)
                dropButtonText.text = $"丢弃 {item.itemData.itemName}";
        }

        if (splitButton != null)
            splitButton.interactable = item.count > 1;

        // 定位
        if (menuRect != null && parentCanvas != null)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentCanvas.transform as RectTransform,
                screenPos + offset,
                parentCanvas.worldCamera,
                out Vector2 localPos);
            menuRect.localPosition = localPos;
        }

        menuPanel.SetActive(true);
        splitPanel.SetActive(false);
    }

    /// <summary>隐藏菜单</summary>
    public void Hide()
    {
        if (menuPanel != null)
            menuPanel.SetActive(false);
        if (splitPanel != null)
            splitPanel.SetActive(false);
        currentItem = null;
        currentSlotIndex = -1;
    }

    // ==================== 按钮回调 ====================

    private void OnUseClicked()
    {
        if (currentItem == null || currentItem.IsEmpty) return;

        var inv = InventoryManager.Instance?.Inventory;
        var stats = InventoryManager.Instance?.PlayerStats;

        if (inv == null || stats == null) return;

        // 使用物品
        if (currentItem.itemData.effect.healthRestore > 0)
            stats.Heal(currentItem.itemData.effect.healthRestore);
        if (currentItem.itemData.effect.manaRestore > 0)
            stats.RestoreMana(currentItem.itemData.effect.manaRestore);

        inv.RemoveItem(currentSlotIndex, 1);
        Debug.Log($"使用了 {currentItem.itemData.itemName}");

        Hide();
    }

    private void OnDropClicked()
    {
        if (currentItem == null || currentItem.IsEmpty) return;

        var inv = InventoryManager.Instance?.Inventory;
        if (inv != null)
        {
            // 丢弃全部数量
            int count = currentItem.count;
            inv.RemoveItem(currentSlotIndex, count);
            Debug.Log($"丢弃了 {currentItem.itemData.itemName} x{count}");
        }

        Hide();
    }

    private void OnSplitClicked()
    {
        if (currentItem == null || currentItem.IsEmpty || currentItem.count <= 1) return;

        // 显示拆分面板
        if (splitPanel != null && splitSlider != null && splitCountText != null)
        {
            int maxSplit = currentItem.count - 1;
            splitSlider.minValue = 1;
            splitSlider.maxValue = maxSplit;
            splitSlider.value = maxSplit / 2;
            splitCountText.text = ((int)splitSlider.value).ToString();
            splitPanel.SetActive(true);
        }
    }

    private void OnSliderChanged(float value)
    {
        if (splitCountText != null)
            splitCountText.text = ((int)value).ToString();
    }

    private void OnSplitConfirm()
    {
        var inv = InventoryManager.Instance?.Inventory;
        if (inv != null && currentItem != null)
        {
            int splitCount = (int)splitSlider.value;
            if (inv.SplitStack(currentSlotIndex, splitCount))
                Debug.Log($"拆分了 {currentItem.itemData.itemName} x{splitCount}");
        }
        Hide();
    }

    private void OnSplitCancel()
    {
        // 只关闭拆分面板，保留主菜单
        if (splitPanel != null)
            splitPanel.SetActive(false);
    }
}
