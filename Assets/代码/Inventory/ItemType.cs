/// <summary>
/// 物品类型枚举
/// </summary>
public enum ItemType
{
    Consumable, // 消耗品（药水、食物等）
    Equipment,  // 装备（武器、防具等）
    Material,   // 材料（合成素材等）
    Quest       // 任务物品
}

/// <summary>
/// 物品效果类型
/// </summary>
public enum ItemEffectType
{
    None,
    Heal,         // 回复生命
    RestoreMana,  // 回复蓝量
    Buff,         // 增益效果
    Damage        // 伤害类
}
