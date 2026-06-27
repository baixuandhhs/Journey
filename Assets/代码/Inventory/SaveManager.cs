using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// JSON 存档管理器。负责背包数据和玩家状态的序列化与反序列化。
/// </summary>
public static class SaveManager
{
    /// <summary>存档文件路径</summary>
    public static string SavePath => Path.Combine(Application.persistentDataPath, "journey_save.json");

    // ItemData 缓存（从 Resources 加载）
    private static Dictionary<string, ItemData> itemDataCache;
    private static bool cacheBuilt = false;

    // ==================== 存档数据结构 ====================

    [System.Serializable]
    public class SaveData
    {
        public List<SlotSaveData> inventory = new List<SlotSaveData>();
        public float playerHealth;
        public float playerMana;
        public float playerPosX;
        public float playerPosY;
        public string saveTimestamp;
    }

    [System.Serializable]
    public class SlotSaveData
    {
        public string itemID;
        public int count;
    }

    // ==================== 公共方法 ====================

    /// <summary>是否存在存档文件</summary>
    public static bool HasSaveData()
    {
        return File.Exists(SavePath);
    }

    /// <summary>保存游戏状态</summary>
    public static void SaveGame(Inventory inventory, PlayerStats stats, Vector3? playerPos = null)
    {
        if (inventory == null) return;

        var saveData = new SaveData();

        // 保存背包数据
        foreach (var item in inventory.items)
        {
            if (!item.IsEmpty)
            {
                saveData.inventory.Add(new SlotSaveData
                {
                    itemID = item.itemData.itemID,
                    count = item.count
                });
            }
        }

        // 保存玩家状态
        if (stats != null)
        {
            saveData.playerHealth = stats.currentHealth;
            saveData.playerMana = stats.currentMana;
        }

        // 保存位置
        if (playerPos.HasValue)
        {
            saveData.playerPosX = playerPos.Value.x;
            saveData.playerPosY = playerPos.Value.y;
        }
        else if (stats != null)
        {
            saveData.playerPosX = stats.transform.position.x;
            saveData.playerPosY = stats.transform.position.y;
        }

        saveData.saveTimestamp = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        // 写入文件
        try
        {
            string json = JsonUtility.ToJson(saveData, true);
            File.WriteAllText(SavePath, json);
            Debug.Log($"[SaveManager] 游戏已保存 → {SavePath}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[SaveManager] 保存失败: {e.Message}");
        }
    }

    /// <summary>加载背包数据到指定 Inventory 实例</summary>
    public static bool LoadInventory(Inventory inventory)
    {
        if (inventory == null || !HasSaveData()) return false;

        try
        {
            string json = File.ReadAllText(SavePath);
            var saveData = JsonUtility.FromJson<SaveData>(json);
            if (saveData == null || saveData.inventory == null) return false;

            // 清空当前背包
            foreach (var item in inventory.items)
                item.Clear();

            // 恢复物品
            foreach (var slot in saveData.inventory)
            {
                if (string.IsNullOrEmpty(slot.itemID) || slot.count <= 0) continue;

                var itemData = GetItemDataByID(slot.itemID);
                if (itemData != null)
                {
                    inventory.AddItem(itemData, slot.count);
                }
                else
                {
                    Debug.LogWarning($"[SaveManager] 无法找到物品: {slot.itemID}，已跳过");
                }
            }

            Debug.Log($"[SaveManager] 背包数据已加载 ({saveData.inventory.Count} 种物品)");
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[SaveManager] 加载失败: {e.Message}");
            return false;
        }
    }

    /// <summary>加载完整游戏状态（背包 + 玩家属性 + 位置）</summary>
    public static SaveData LoadGame()
    {
        if (!HasSaveData()) return null;

        try
        {
            string json = File.ReadAllText(SavePath);
            return JsonUtility.FromJson<SaveData>(json);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[SaveManager] 加载失败: {e.Message}");
            return null;
        }
    }

    /// <summary>应用存档中的玩家状态</summary>
    public static void ApplyPlayerState(PlayerStats stats, SaveData saveData)
    {
        if (stats == null || saveData == null) return;

        stats.currentHealth = saveData.playerHealth > 0 ? saveData.playerHealth : stats.maxHealth;
        stats.currentMana = saveData.playerMana > 0 ? saveData.playerMana : stats.maxMana;
        stats.UpdateHealthUI();

        // 恢复位置
        if (saveData.playerPosX != 0 || saveData.playerPosY != 0)
        {
            stats.transform.position = new Vector3(saveData.playerPosX, saveData.playerPosY, stats.transform.position.z);
        }

        Debug.Log($"[SaveManager] 玩家状态已恢复 (HP: {stats.currentHealth}, MP: {stats.currentMana})");
    }

    /// <summary>删除存档</summary>
    public static void DeleteSave()
    {
        if (File.Exists(SavePath))
        {
            File.Delete(SavePath);
            Debug.Log("[SaveManager] 存档已删除");
        }
    }

    // ==================== ItemData 查找 ====================

    /// <summary>根据 itemID 查找 ItemData 资产</summary>
    public static ItemData GetItemDataByID(string itemID)
    {
        BuildItemCache();

        if (itemDataCache != null && itemDataCache.TryGetValue(itemID, out var data))
            return data;

        Debug.LogWarning($"[SaveManager] ItemData 缓存中未找到: {itemID}");
        return null;
    }

    /// <summary>构建 ItemData 缓存（从 Resources/Items/ 加载所有 ItemData）</summary>
    private static void BuildItemCache()
    {
        if (cacheBuilt) return;

        itemDataCache = new Dictionary<string, ItemData>();

        // 从 Resources/Items/ 加载所有 ItemData
        var loadedItems = Resources.LoadAll<ItemData>("Items");
        foreach (var item in loadedItems)
        {
            if (!string.IsNullOrEmpty(item.itemID))
            {
                if (itemDataCache.ContainsKey(item.itemID))
                {
                    Debug.LogWarning($"[SaveManager] 重复的 itemID: {item.itemID}");
                }
                else
                {
                    itemDataCache[item.itemID] = item;
                }
            }
        }

        cacheBuilt = true;
        Debug.Log($"[SaveManager] ItemData 缓存已构建，共 {itemDataCache.Count} 个物品");
    }

    /// <summary>强制刷新缓存（在运行时动态添加物品后调用）</summary>
    public static void RefreshCache()
    {
        cacheBuilt = false;
        itemDataCache = null;
        BuildItemCache();
    }
}
