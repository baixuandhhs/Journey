using UnityEngine;

/// <summary>
/// 可拾取物品触发器。玩家进入触发器后自动拾取到背包。
/// 在 Inspector 中拖入对应的 ItemData 资产来确定物品类型。
/// </summary>
public class PotionItem : MonoBehaviour
{
    [Tooltip("拖入对应的 ItemData 资产（如 RedPotion、BluePotion）")]
    public ItemData itemData;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        if (itemData == null)
        {
            Debug.LogWarning($"[PotionItem] {gameObject.name} 的 itemData 未设置！");
            return;
        }

        var invManager = InventoryManager.Instance;
        if (invManager == null || invManager.Inventory == null)
        {
            Debug.LogError("[PotionItem] InventoryManager 未找到！");
            return;
        }

        int added = invManager.Inventory.AddItem(itemData, 1);
        if (added > 0)
        {
            Debug.Log($"拾取了 {itemData.itemName}");

            // 自动保存
            SaveManager.SaveGame(invManager.Inventory, invManager.PlayerStats);

            Destroy(gameObject);
        }
        else
        {
            Debug.Log($"背包已满，无法拾取 {itemData.itemName}");
        }
    }
}
