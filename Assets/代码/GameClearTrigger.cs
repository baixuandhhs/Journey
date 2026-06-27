using UnityEngine;
using UnityEngine.SceneManagement;

public class GameClearTrigger : MonoBehaviour
{
    [Header("=== 通关设置 ===")]
    public GameObject clearPanel; // 在 Inspector 中拖入你的 ClearPanel
    public bool freezeTime = true; // 是否通关时暂停时间

    private void OnTriggerEnter2D(Collider2D other)
    {
        // 1. 确保只有玩家触发
        if (other.CompareTag("Player"))
        {
            Debug.Log("<color=green>触碰水晶：触发通关逻辑</color>");
            GameClear(other.gameObject);
        }
    }

    void GameClear(GameObject player)
    {
        // 2. 显示 UI 面板
        if (clearPanel != null)
        {
            clearPanel.SetActive(true);
        }
        else
        {
            Debug.LogError("错误：ClearPanel 未在 Inspector 中分配！");
        }

        // 3. 停止玩家移动（防止角色在通关界面后还能操作）
        var controller = player.GetComponent<PlayerController>();
        if (controller != null)
        {
            controller.enabled = false;
        }

        // 4. 清除玩家速度，防止惯性移动
        var rb = player.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.simulated = false; // 停止物理模拟
        }

        // 5. 处理时间与鼠标
        if (freezeTime)
        {
            Time.timeScale = 0f;
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}