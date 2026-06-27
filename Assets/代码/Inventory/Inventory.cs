using System;
using System.Collections.Generic;

/// <summary>
/// 背包数据层核心（纯 C# 类，非 MonoBehaviour）。
/// 管理物品列表的增删改查，所有数据变更通过事件通知 UI 层。
/// </summary>
[System.Serializable]
public class Inventory
{
    /// <summary>最大槽位数</summary>
    public int maxSlots;

    /// <summary>物品槽位列表（索引对应 UI 槽位位置）</summary>
    public List<InventoryItem> items;

    /// <summary>背包数据变更事件（UI 层订阅此事件来刷新）</summary>
    public event System.Action OnInventoryChanged;

    /// <summary>
    /// 创建背包实例
    /// </summary>
    /// <param name="maxSlots">最大槽位数，默认 20</param>
    public Inventory(int maxSlots = 20)
    {
        this.maxSlots = maxSlots;
        items = new List<InventoryItem>(maxSlots);
        for (int i = 0; i < maxSlots; i++)
        {
            items.Add(new InventoryItem());
        }
    }

    // ==================== 查询 ====================

    /// <summary>获取指定槽位的物品</summary>
    public InventoryItem GetItem(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= items.Count) return null;
        return items[slotIndex];
    }

    /// <summary>查找物品在背包中的总数量</summary>
    public int GetItemCount(ItemData data)
    {
        if (data == null) return 0;
        int total = 0;
        foreach (var item in items)
        {
            if (!item.IsEmpty && item.itemData == data)
                total += item.count;
        }
        return total;
    }

    /// <summary>背包中是否有指定物品</summary>
    public bool HasItem(ItemData data)
    {
        return GetItemCount(data) > 0;
    }

    /// <summary>查找第一个空槽位索引，没有则返回 -1</summary>
    public int FindEmptySlot()
    {
        for (int i = 0; i < items.Count; i++)
        {
            if (items[i].IsEmpty) return i;
        }
        return -1;
    }

    /// <summary>查找与指定 ItemData 相同且有堆叠空间的槽位索引</summary>
    public int FindStackableSlot(ItemData data)
    {
        if (data == null) return -1;
        for (int i = 0; i < items.Count; i++)
        {
            if (items[i].CanStackWith(data)) return i;
        }
        return -1;
    }

    /// <summary>背包是否已满</summary>
    public bool IsFull => FindEmptySlot() == -1;

    // ==================== 添加 ====================

    /// <summary>
    /// 向背包添加物品，自动优先堆叠到已有同类物品的槽位。
    /// </summary>
    /// <returns>实际成功添加的数量</returns>
    public int AddItem(ItemData data, int count = 1)
    {
        if (data == null || count <= 0) return 0;

        int remaining = count;
        int totalAdded = 0;

        // 第一步：优先填充已有同类物品且有空间的槽位
        for (int i = 0; i < items.Count && remaining > 0; i++)
        {
            if (items[i].CanStackWith(data))
            {
                int added = items[i].Add(remaining);
                remaining -= added;
                totalAdded += added;
            }
        }

        // 第二步：剩余数量放入空槽位
        while (remaining > 0)
        {
            int emptySlot = FindEmptySlot();
            if (emptySlot == -1) break; // 背包满了

            items[emptySlot].itemData = data;
            int added = items[emptySlot].Add(remaining);
            remaining -= added;
            totalAdded += added;
        }

        if (totalAdded > 0)
        {
            NotifyChanged();
        }

        return totalAdded;
    }

    // ==================== 移除 ====================

    /// <summary>
    /// 从指定槽位移除物品
    /// </summary>
    public int RemoveItem(int slotIndex, int count = 1)
    {
        if (slotIndex < 0 || slotIndex >= items.Count) return 0;
        if (items[slotIndex].IsEmpty) return 0;

        int removed = items[slotIndex].Remove(count);
        if (removed > 0)
        {
            NotifyChanged();
        }
        return removed;
    }

    /// <summary>
    /// 移除指定类型的物品（从任意槽位，优先数量最少的槽位）
    /// </summary>
    public int RemoveItem(ItemData data, int count = 1)
    {
        if (data == null || count <= 0) return 0;

        int remaining = count;
        int totalRemoved = 0;

        // 从数量最少的槽位开始移除，避免产生零散槽位
        for (int pass = 0; pass < 2 && remaining > 0; pass++)
        {
            for (int i = 0; i < items.Count && remaining > 0; i++)
            {
                if (!items[i].IsEmpty && items[i].itemData == data)
                {
                    // 第一遍：移除不满堆叠的槽位；第二遍：移除满堆叠的槽位
                    bool isFullStack = items[i].count >= items[i].itemData.maxStack;
                    if ((pass == 0 && !isFullStack) || (pass == 1 && isFullStack))
                    {
                        int removed = items[i].Remove(remaining);
                        remaining -= removed;
                        totalRemoved += removed;
                    }
                }
            }
        }

        if (totalRemoved > 0)
        {
            NotifyChanged();
        }
        return totalRemoved;
    }

    // ==================== 交换 / 移动 / 拆分 ====================

    /// <summary>交换两个槽位的物品</summary>
    public void SwapSlots(int fromIndex, int toIndex)
    {
        if (fromIndex < 0 || fromIndex >= items.Count) return;
        if (toIndex < 0 || toIndex >= items.Count) return;
        if (fromIndex == toIndex) return;

        var temp = items[fromIndex];
        items[fromIndex] = items[toIndex];
        items[toIndex] = temp;

        NotifyChanged();
    }

    /// <summary>
    /// 将 fromIndex 的物品移动到 toIndex。
    /// 如果目标槽有同类物品则合并堆叠；否则交换位置。
    /// </summary>
    public void MoveItem(int fromIndex, int toIndex)
    {
        if (fromIndex < 0 || fromIndex >= items.Count) return;
        if (toIndex < 0 || toIndex >= items.Count) return;
        if (fromIndex == toIndex) return;

        var fromItem = items[fromIndex];
        var toItem = items[toIndex];

        if (fromItem.IsEmpty) return;

        // 目标为空，直接移动
        if (toItem.IsEmpty)
        {
            items[toIndex] = fromItem;
            items[fromIndex] = toItem;
            NotifyChanged();
            return;
        }

        // 同类物品，尝试合并
        if (fromItem.itemData == toItem.itemData && toItem.HasStackSpace)
        {
            int space = toItem.RemainingStackSpace;
            int toMove = fromItem.count <= space ? fromItem.count : space;
            fromItem.Remove(toMove);
            toItem.Add(toMove);
            // 如果 from 槽清空了就不需要额外处理
            NotifyChanged();
            return;
        }

        // 不同类型或目标已满，交换
        SwapSlots(fromIndex, toIndex);
    }

    /// <summary>拆分堆叠物品到空槽位</summary>
    public bool SplitStack(int slotIndex, int splitCount)
    {
        if (slotIndex < 0 || slotIndex >= items.Count) return false;
        if (items[slotIndex].IsEmpty) return false;
        if (items[slotIndex].count <= splitCount) return false; // 不能全部分走
        if (splitCount <= 0) return false;

        int emptySlot = FindEmptySlot();
        if (emptySlot == -1) return false; // 没有空槽位

        var splitItem = items[slotIndex].Split(splitCount);
        if (splitItem != null)
        {
            items[emptySlot] = splitItem;
            NotifyChanged();
            return true;
        }
        return false;
    }

    // ==================== 使用物品 ====================

    /// <summary>
    /// 尝试消耗一个消耗品（单次遍历，无额外查询开销）。
    /// </summary>
    /// <returns>是否成功消耗</returns>
    public bool TryConsumeItem(ItemData data)
    {
        if (data == null || data.itemType != ItemType.Consumable) return false;

        for (int i = 0; i < items.Count; i++)
        {
            if (!items[i].IsEmpty && items[i].itemData == data)
            {
                items[i].Remove(1);
                NotifyChanged();
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// 查找并使用指定类型的物品（消耗品）。
    /// </summary>
    /// <returns>使用的物品数据，供调用方执行效果；失败返回 null</returns>
    public ItemData UseItem(ItemData data)
    {
        if (data == null || data.itemType != ItemType.Consumable) return null;

        int total = GetItemCount(data);
        if (total <= 0) return null;

        // 从任意槽位移除 1 个
        int removed = RemoveItem(data, 1);
        return removed > 0 ? data : null;
    }

    // ==================== 排序 ====================

    /// <summary>
    /// 整理背包：将物品移到前面，空槽位移到后面。
    /// 按 itemID 排序，同类物品相邻。
    /// </summary>
    public void SortItems()
    {
        if (items.Count <= 1) return;

        // 就地排序：空槽位排到后面，非空按 itemID 升序
        items.Sort((a, b) =>
        {
            bool aEmpty = a.IsEmpty;
            bool bEmpty = b.IsEmpty;
            if (aEmpty && bEmpty) return 0;
            if (aEmpty) return 1;
            if (bEmpty) return -1;
            return string.Compare(a.itemData.itemID, b.itemData.itemID);
        });

        NotifyChanged();
    }

    // ==================== 序列化辅助 ====================

    /// <summary>背包中物品槽位数量（含空槽）</summary>
    public int SlotCount => items.Count;

    /// <summary>非空槽位数量</summary>
    public int UsedSlotCount
    {
        get
        {
            int count = 0;
            foreach (var item in items)
                if (!item.IsEmpty) count++;
            return count;
        }
    }

    // ==================== 内部方法 ====================

    private void NotifyChanged()
    {
        OnInventoryChanged?.Invoke();
    }
}
