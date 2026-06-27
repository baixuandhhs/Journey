using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 背包槽位 UI 组件。
/// 负责单个格子的显示、拖拽交互、悬停高亮和右键菜单触发。
/// </summary>
public class InventorySlotUI : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler,
    IDropHandler,
    IPointerEnterHandler, IPointerExitHandler,
    IPointerClickHandler
{
    [Header("=== UI 引用 ===")]
    public Image itemIcon;          // 物品图标
    public TextMeshProUGUI countText; // 数量文本
    public Image slotBackground;    // 槽位背景（用于高亮）
    public Image highlightBorder;   // 高亮边框（悬停时显示）

    [Header("=== 高亮设置 ===")]
    public Color normalColor = new Color(1, 1, 1, 0.3f);
    public Color highlightColor = new Color(1f, 0.9f, 0.5f, 0.6f);
    public Color normalBorderColor = new Color(1, 1, 1, 0f);
    public Color highlightBorderColor = new Color(1f, 0.85f, 0.3f, 0.8f);
    [Range(0.5f, 1.5f)]
    public float highlightScale = 1.05f;

    [Header("=== 拖拽设置 ===")]
    [Range(0.3f, 1f)]
    public float dragAlpha = 0.6f;
    public Vector2 dragIconSize = new Vector2(64, 64);

    [Header("=== 运行时数据 ===")]
    public int slotIndex;           // 在背包中的槽位索引
    public InventoryItem currentItem; // 当前槽位数据

    // 内部状态
    private Canvas parentCanvas;
    private GameObject dragGhost;
    private Vector3 originalScale;

    void Awake()
    {
        if (slotBackground == null)
            slotBackground = GetComponent<Image>();
        if (parentCanvas == null)
            parentCanvas = GetComponentInParent<Canvas>();
        originalScale = transform.localScale;

        ClearSlot();
    }

    /// <summary>根据 InventoryItem 数据刷新 UI 显示</summary>
    public void RefreshUI(InventoryItem item)
    {
        currentItem = item;

        if (item == null || item.IsEmpty)
        {
            ClearSlot();
            return;
        }

        if (itemIcon != null)
        {
            itemIcon.sprite = item.itemData.icon;
            itemIcon.gameObject.SetActive(true);
            itemIcon.preserveAspect = true;
        }

        if (countText != null)
        {
            // 只有数量 > 1 才显示数字（单个物品无需显示 x1）
            if (item.count > 1)
            {
                countText.text = item.count.ToString();
                countText.gameObject.SetActive(true);
            }
            else
            {
                countText.gameObject.SetActive(false);
            }
        }
    }

    /// <summary>清空槽位显示</summary>
    public void ClearSlot()
    {
        currentItem = null;
        if (itemIcon != null)
        {
            itemIcon.sprite = null;
            itemIcon.gameObject.SetActive(false);
        }
        if (countText != null)
            countText.gameObject.SetActive(false);
    }

    // ==================== 拖拽接口 ====================

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (currentItem == null || currentItem.IsEmpty) return;

        // 创建拖拽幽灵图标
        CreateDragGhost();

        // 原始槽位图标变暗
        if (itemIcon != null)
        {
            var c = itemIcon.color;
            c.a = dragAlpha;
            itemIcon.color = c;
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (dragGhost != null && parentCanvas != null)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentCanvas.transform as RectTransform,
                eventData.position,
                eventData.pressEventCamera,
                out Vector2 localPos);
            dragGhost.transform.localPosition = localPos;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        // 销毁幽灵
        if (dragGhost != null)
        {
            Destroy(dragGhost);
            dragGhost = null;
        }

        // 恢复图标透明度
        if (itemIcon != null)
        {
            var c = itemIcon.color;
            c.a = 1f;
            itemIcon.color = c;
        }

        // 检测拖放目标
        if (eventData.pointerEnter != null)
        {
            var targetSlot = eventData.pointerEnter.GetComponent<InventorySlotUI>();
            HandleDropOnSlot(targetSlot);
        }
    }

    // ==================== 接收拖放 ====================

    public void OnDrop(PointerEventData eventData)
    {
        var sourceSlot = eventData.pointerDrag?.GetComponent<InventorySlotUI>();
        HandleDropOnSlot(sourceSlot);
    }

    // ==================== 悬停 ====================

    public void OnPointerEnter(PointerEventData eventData)
    {
        // 背景高亮
        if (slotBackground != null)
            slotBackground.color = highlightColor;

        // 边框高亮
        if (highlightBorder != null)
            highlightBorder.color = highlightBorderColor;

        // 缩放
        transform.localScale = originalScale * highlightScale;

        // 显示 Tooltip
        if (currentItem != null && !currentItem.IsEmpty)
        {
            var tooltip = InventoryTooltip.Instance;
            if (tooltip != null)
                tooltip.Show(currentItem.itemData, eventData.position);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // 恢复背景
        if (slotBackground != null)
            slotBackground.color = normalColor;

        // 恢复边框
        if (highlightBorder != null)
            highlightBorder.color = normalBorderColor;

        // 恢复缩放
        transform.localScale = originalScale;

        // 隐藏 Tooltip
        var tooltip = InventoryTooltip.Instance;
        if (tooltip != null)
            tooltip.Hide();
    }

    // ==================== 点击 ====================

    public void OnPointerClick(PointerEventData eventData)
    {
        if (currentItem == null || currentItem.IsEmpty) return;

        // 右键 → 弹出菜单
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            var ctxMenu = ContextMenu.Instance;
            if (ctxMenu != null)
                ctxMenu.Show(slotIndex, currentItem, eventData.position);
        }
    }

    // ==================== 私有方法 ====================

    /// <summary>统一的拖放处理（OnEndDrag 与 OnDrop 共用）</summary>
    private void HandleDropOnSlot(InventorySlotUI sourceSlot)
    {
        if (sourceSlot == null || sourceSlot == this) return;
        if (sourceSlot.currentItem == null || sourceSlot.currentItem.IsEmpty) return;

        var inv = InventoryManager.Instance?.Inventory;
        if (inv == null) return;

        if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
        {
            int halfCount = sourceSlot.currentItem.count / 2;
            if (halfCount > 0)
                inv.SplitStack(sourceSlot.slotIndex, halfCount);
        }
        else
        {
            inv.MoveItem(sourceSlot.slotIndex, slotIndex);
        }
    }

    private void CreateDragGhost()
    {
        if (currentItem == null || currentItem.IsEmpty) return;

        dragGhost = new GameObject("DragGhost", typeof(RectTransform), typeof(Image));
        dragGhost.transform.SetParent(parentCanvas.transform, false);
        dragGhost.transform.SetAsLastSibling(); // 确保在最上层

        var rt = dragGhost.GetComponent<RectTransform>();
        rt.sizeDelta = dragIconSize;

        var img = dragGhost.GetComponent<Image>();
        img.sprite = currentItem.itemData.icon;
        img.raycastTarget = false; // 不阻挡射线
        var c = img.color;
        c.a = dragAlpha;
        img.color = c;

        // 初始位置
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentCanvas.transform as RectTransform,
            Input.mousePosition,
            null,
            out Vector2 localPos);
        dragGhost.transform.localPosition = localPos;
    }
}
