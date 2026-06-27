using UnityEngine;

/// <summary>
/// 物品效果数据（可序列化结构体）
/// </summary>
[System.Serializable]
public struct EffectData
{
    [Tooltip("生命恢复量")]
    public float healthRestore;

    [Tooltip("蓝量恢复量")]
    public float manaRestore;

    [Tooltip("效果持续时间（秒），0 表示瞬时效果")]
    public float duration;

    [Tooltip("效果类型")]
    public ItemEffectType effectType;
}

/// <summary>
/// ScriptableObject 物品定义。
/// 在 Project 窗口中右键 → Create → Journey → Item Data 创建新物品。
/// </summary>
[CreateAssetMenu(menuName = "Journey/Item Data", fileName = "NewItem")]
public class ItemData : ScriptableObject
{
    [Header("=== 基本信息 ===")]
    [Tooltip("物品唯一标识符，如 potion_red")]
    public string itemID;

    [Tooltip("显示名称")]
    public string itemName;

    [Tooltip("描述文本（显示在 Tooltip 中）")]
    [TextArea(2, 4)]
    public string description;

    [Tooltip("物品图标")]
    public Sprite icon;

    [Tooltip("物品类型")]
    public ItemType itemType;

    [Header("=== 堆叠设置 ===")]
    [Tooltip("最大堆叠数量")]
    [Min(1)]
    public int maxStack = 99;

    [Header("=== 效果 ===")]
    [Tooltip("物品使用效果")]
    public EffectData effect;

    [Header("=== 价格（预留） ===")]
    [Tooltip("购买价格")]
    public int buyPrice;

    [Tooltip("出售价格")]
    public int sellPrice;

    /// <summary>
    /// 该物品是否有可使用效果
    /// </summary>
    public bool HasEffect => effect.effectType != ItemEffectType.None;
}
