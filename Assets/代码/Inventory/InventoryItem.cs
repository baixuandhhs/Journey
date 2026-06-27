/// <summary>
/// 运行时背包槽位数据结构。
/// 每个 InventoryItem 代表背包中一个格子的状态。
/// </summary>
[System.Serializable]
public class InventoryItem
{
    /// <summary>物品定义引用（null 表示空槽位）</summary>
    public ItemData itemData;

    /// <summary>当前堆叠数量</summary>
    public int count;

    /// <summary>该槽位是否为空</summary>
    public bool IsEmpty => itemData == null || count <= 0;

    /// <summary>是否还有堆叠空间</summary>
    public bool HasStackSpace => itemData != null && count < itemData.maxStack;

    /// <summary>剩余可堆叠数量</summary>
    public int RemainingStackSpace => itemData != null ? itemData.maxStack - count : 0;

    /// <summary>
    /// 判断是否可以与此 ItemData 堆叠
    /// </summary>
    public bool CanStackWith(ItemData other)
    {
        if (itemData == null || other == null) return false;
        return itemData == other && count < itemData.maxStack;
    }

    /// <summary>
    /// 向此槽位添加数量，返回实际添加的数量（可能受 maxStack 限制）
    /// </summary>
    public int Add(int amount)
    {
        if (itemData == null || amount <= 0) return 0;
        int canAdd = RemainingStackSpace;
        int actual = amount < canAdd ? amount : canAdd;
        count += actual;
        return actual;
    }

    /// <summary>
    /// 从此槽位移除数量，返回实际移除的数量
    /// </summary>
    public int Remove(int amount)
    {
        if (itemData == null || amount <= 0) return 0;
        int actual = amount < count ? amount : count;
        count -= actual;
        if (count <= 0)
        {
            itemData = null;
            count = 0;
        }
        return actual;
    }

    /// <summary>
    /// 从此槽位拆分出指定数量的物品为新槽位
    /// </summary>
    public InventoryItem Split(int amount)
    {
        if (itemData == null || amount <= 0 || amount >= count) return null;
        count -= amount;
        return new InventoryItem { itemData = itemData, count = amount };
    }

    /// <summary>
    /// 清空槽位
    /// </summary>
    public void Clear()
    {
        itemData = null;
        count = 0;
    }

    /// <summary>
    /// 创建深拷贝
    /// </summary>
    public InventoryItem Clone()
    {
        return new InventoryItem { itemData = itemData, count = count };
    }

    public override string ToString()
    {
        return IsEmpty ? "[空]" : $"{itemData.itemName} x{count}";
    }
}
