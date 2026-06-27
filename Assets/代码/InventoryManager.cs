using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 背包系统中枢控制器（单例）。
/// 负责：B 键开关面板、Q/E 快捷使用、动态槽位管理、协调 UI 层与数据层。
/// </summary>
public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }

    [Header("=== 背包面板 ===")]
    public GameObject backpackPanel;
    public RectTransform slotContainer;     // GridLayoutGroup 所在的容器
    public GameObject slotPrefab;           // 槽位预制体（可选，不设置则代码生成）

    [Header("=== 快捷栏物品（Q / E 键对应） ===")]
    public ItemData quickRedItem;   // Q 键快捷物品
    public ItemData quickBlueItem;  // E 键快捷物品

    [Header("=== 背包设置 ===")]
    [Min(1)]
    public int maxSlots = 20;

    [Header("=== 面板动画 ===")]
    [Range(0.05f, 0.5f)]
    public float openAnimDuration = 0.2f;

    /// <summary>背包数据层（对外暴露）</summary>
    public Inventory Inventory { get; private set; }

    /// <summary>玩家属性引用（对外暴露给 SaveManager 等）</summary>
    public PlayerStats PlayerStats { get; private set; }

    // 内部状态
    private bool isOpen = false;
    private List<InventorySlotUI> slotUIs = new List<InventorySlotUI>();
    private CanvasGroup backpackCanvasGroup;
    private Coroutine animCoroutine;

    // ==================== 生命周期 ====================

    void Awake()
    {
        // 单例
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // 初始化数据层
        Inventory = new Inventory(maxSlots);
        Inventory.OnInventoryChanged += OnInventoryDataChanged;
    }

    void Start()
    {
        // 查找玩家（优先通过 Player 标签查找，避免找到其他挂载了 PlayerStats 的对象）
        var playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            PlayerStats = playerObj.GetComponent<PlayerStats>();
        if (PlayerStats == null)
            PlayerStats = FindObjectOfType<PlayerStats>();

        // 尝试从存档加载
        if (SaveManager.HasSaveData())
        {
            SaveManager.LoadInventory(Inventory);
            Debug.Log("[InventoryManager] 已从存档恢复背包数据");
        }

        // 准备背包面板
        if (backpackPanel != null)
        {
            backpackCanvasGroup = backpackPanel.GetComponent<CanvasGroup>();
            if (backpackCanvasGroup == null)
                backpackCanvasGroup = backpackPanel.AddComponent<CanvasGroup>();
            backpackPanel.SetActive(false);
        }

        // 如果未指定容器，尝试从 BackpackPanel 查找
        if (slotContainer == null && backpackPanel != null)
        {
            slotContainer = backpackPanel.GetComponent<RectTransform>();
        }

        // 动态创建槽位
        CreateAllSlots();

        // 初始刷新
        RefreshAllSlots();
    }

    void Update()
    {
        // B 键开关背包
        if (Input.GetKeyDown(KeyCode.B))
            ToggleBackpack();

        // Q 键快捷使用
        if (Input.GetKeyDown(KeyCode.Q))
            UseQuickItem(quickRedItem);

        // E 键快捷使用
        if (Input.GetKeyDown(KeyCode.E))
            UseQuickItem(quickBlueItem);
    }

    void OnDestroy()
    {
        if (Inventory != null)
            Inventory.OnInventoryChanged -= OnInventoryDataChanged;
    }

    // ==================== 背包开关 ====================

    public void ToggleBackpack()
    {
        isOpen = !isOpen;

        if (animCoroutine != null)
            StopCoroutine(animCoroutine);

        if (isOpen)
        {
            backpackPanel.SetActive(true);
            RefreshAllSlots(); // 打开时刷新
            animCoroutine = StartCoroutine(AnimatePanelOpen());
        }
        else
        {
            animCoroutine = StartCoroutine(AnimatePanelClose());
        }
    }

    /// <summary>强制关闭背包（如暂停时）</summary>
    public void CloseBackpack()
    {
        if (!isOpen) return;
        isOpen = false;
        if (animCoroutine != null) StopCoroutine(animCoroutine);
        if (backpackPanel != null) backpackPanel.SetActive(false);
        if (backpackCanvasGroup != null) backpackCanvasGroup.alpha = 1f;
    }

    // ==================== 快捷使用 ====================

    /// <summary>使用快捷栏物品</summary>
    public void UseQuickItem(ItemData data)
    {
        if (data == null) return;
        if (PlayerStats == null) return;

        // 检查是否有足够的生命/法力空间
        if (data.effect.healthRestore > 0 && PlayerStats.currentHealth >= PlayerStats.maxHealth)
            return; // 血量已满，不浪费
        if (data.effect.manaRestore > 0 && PlayerStats.currentMana >= PlayerStats.maxMana)
            return; // 蓝量已满，不浪费

        // 单次遍历：直接尝试消耗，无需先 GetItemCount 再 UseItem
        if (Inventory.TryConsumeItem(data))
        {
            if (data.effect.healthRestore > 0)
                PlayerStats.Heal(data.effect.healthRestore);
            if (data.effect.manaRestore > 0)
                PlayerStats.RestoreMana(data.effect.manaRestore);

            Debug.Log($"[InventoryManager] 快捷使用了 {data.itemName}");

            // 自动保存
            SaveManager.SaveGame(Inventory, PlayerStats);
        }
    }

    // ==================== 槽位管理 ====================

    /// <summary>创建所有槽位</summary>
    private void CreateAllSlots()
    {
        // 清除旧的槽位列表
        foreach (var oldSlot in slotUIs)
        {
            if (oldSlot != null && oldSlot.gameObject != null)
                Destroy(oldSlot.gameObject);
        }
        slotUIs.Clear();

        // 清除 slotContainer 中所有已有的子对象（包括场景预置的旧 Slot）
        if (slotContainer != null)
        {
            foreach (Transform child in slotContainer)
            {
                Destroy(child.gameObject);
            }
        }
        else
        {
            Debug.LogError("[InventoryManager] slotContainer 未设置！无法创建槽位。");
            return;
        }

        for (int i = 0; i < Inventory.maxSlots; i++)
        {
            CreateSlot(i);
        }
    }

    /// <summary>创建单个槽位 GameObject</summary>
    private GameObject CreateSlot(int index)
    {
        GameObject slotObj;

        if (slotPrefab != null)
        {
            // 使用预制体
            slotObj = Instantiate(slotPrefab, slotContainer);
        }
        else
        {
            // 代码动态创建槽位
            slotObj = CreateSlotProgrammatic(index);
        }

        slotObj.name = $"Slot_{index}";

        // 获取或添加 InventorySlotUI 组件
        var slotUI = slotObj.GetComponent<InventorySlotUI>();
        if (slotUI == null)
            slotUI = slotObj.AddComponent<InventorySlotUI>();
        slotUI.slotIndex = index;

        slotUIs.Add(slotUI);
        return slotObj;
    }

    /// <summary>纯代码创建槽位（当无 slotPrefab 时）</summary>
    private GameObject CreateSlotProgrammatic(int index)
    {
        var slotObj = new GameObject($"Slot_{index}", typeof(RectTransform));

        // RectTransform
        var rt = slotObj.GetComponent<RectTransform>();
        rt.SetParent(slotContainer, false);
        rt.sizeDelta = new Vector2(80, 80);

        // 背景 Image
        var bg = slotObj.AddComponent<Image>();
        bg.color = new Color(0.15f, 0.15f, 0.15f, 0.6f);

        // 边框高亮 Image（子对象）
        var borderObj = new GameObject("Border", typeof(RectTransform), typeof(Image));
        borderObj.transform.SetParent(slotObj.transform, false);
        var borderRt = borderObj.GetComponent<RectTransform>();
        borderRt.anchorMin = Vector2.zero;
        borderRt.anchorMax = Vector2.one;
        borderRt.offsetMin = new Vector2(-2, -2);
        borderRt.offsetMax = new Vector2(2, 2);
        var borderImg = borderObj.GetComponent<Image>();
        borderImg.color = new Color(1, 1, 1, 0f);
        borderImg.raycastTarget = false;

        // 物品图标 Image（子对象）
        var iconObj = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        iconObj.transform.SetParent(slotObj.transform, false);
        var iconRt = iconObj.GetComponent<RectTransform>();
        iconRt.anchorMin = new Vector2(0.1f, 0.1f);
        iconRt.anchorMax = new Vector2(0.9f, 0.9f);
        iconRt.offsetMin = Vector2.zero;
        iconRt.offsetMax = Vector2.zero;
        var iconImg = iconObj.GetComponent<Image>();
        iconImg.raycastTarget = false;
        iconImg.preserveAspect = true;
        iconObj.SetActive(false);

        // 数量文本（子对象）
        var countObj = new GameObject("Count", typeof(RectTransform), typeof(TextMeshProUGUI));
        countObj.transform.SetParent(slotObj.transform, false);
        var countRt = countObj.GetComponent<RectTransform>();
        countRt.anchorMin = new Vector2(1, 0);
        countRt.anchorMax = new Vector2(1, 0);
        countRt.pivot = new Vector2(1, 0);
        countRt.anchoredPosition = new Vector2(-2, 2);
        var countTmp = countObj.GetComponent<TextMeshProUGUI>();
        countTmp.fontSize = 14;
        countTmp.color = Color.white;
        countTmp.alignment = TextAlignmentOptions.BottomRight;
        countTmp.fontStyle = FontStyles.Bold;
        countObj.SetActive(false);

        // 设置 InventorySlotUI 引用
        var slotUI = slotObj.AddComponent<InventorySlotUI>();
        slotUI.itemIcon = iconImg;
        slotUI.countText = countTmp;
        slotUI.slotBackground = bg;
        slotUI.highlightBorder = borderImg;
        slotUI.normalColor = new Color(0.15f, 0.15f, 0.15f, 0.6f);
        slotUI.highlightColor = new Color(0.3f, 0.3f, 0.25f, 0.8f);

        return slotObj;
    }

    // ==================== UI 刷新 ====================

    /// <summary>数据变更回调：刷新所有槽位 + HUD</summary>
    private void OnInventoryDataChanged()
    {
        RefreshAllSlots();
        UpdateHUD();
    }

    /// <summary>刷新所有槽位 UI</summary>
    public void RefreshAllSlots()
    {
        for (int i = 0; i < slotUIs.Count && i < Inventory.items.Count; i++)
        {
            if (slotUIs[i] != null)
                slotUIs[i].RefreshUI(Inventory.items[i]);
        }
    }

    /// <summary>更新 HUD 快捷栏药水数量</summary>
    private void UpdateHUD()
    {
        if (PlayerStats == null) return;

        int redCount = Inventory.GetItemCount(quickRedItem);
        int blueCount = Inventory.GetItemCount(quickBlueItem);
        PlayerStats.UpdatePotionHUD(redCount, blueCount);
    }

    // ==================== 面板动画 ====================

    private IEnumerator AnimatePanelOpen()
    {
        if (backpackPanel == null || backpackCanvasGroup == null) yield break;

        float elapsed = 0f;
        backpackCanvasGroup.alpha = 0f;
        backpackPanel.transform.localScale = new Vector3(0.9f, 0.9f, 1f);

        while (elapsed < openAnimDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / openAnimDuration;
            // Ease-out
            t = 1f - (1f - t) * (1f - t);

            backpackCanvasGroup.alpha = Mathf.Lerp(0f, 1f, t);
            backpackPanel.transform.localScale = Vector3.Lerp(new Vector3(0.9f, 0.9f, 1f), Vector3.one, t);
            yield return null;
        }

        backpackCanvasGroup.alpha = 1f;
        backpackPanel.transform.localScale = Vector3.one;
    }

    private IEnumerator AnimatePanelClose()
    {
        if (backpackPanel == null || backpackCanvasGroup == null) yield break;

        float elapsed = 0f;
        float startAlpha = backpackCanvasGroup.alpha;

        while (elapsed < openAnimDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / openAnimDuration;
            // Ease-in
            t = t * t;

            backpackCanvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, t);
            backpackPanel.transform.localScale = Vector3.Lerp(Vector3.one, new Vector3(0.9f, 0.9f, 1f), t);
            yield return null;
        }

        backpackPanel.SetActive(false);
        backpackCanvasGroup.alpha = 1f;
        backpackPanel.transform.localScale = Vector3.one;
    }
}
