using UnityEngine;

public class Thorn : MonoBehaviour
{
    [Header("=== 伤害设置 ===")]
    public float damage = 20f;         // 伤害值
    public float knockbackForce = 15f; // 弹开力度

    [Header("=== 音效设置 ===")]
    public AudioClip hitSound;         

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            PlayerStats stats = collision.GetComponent<PlayerStats>();
            PlayerController controller = collision.GetComponent<PlayerController>();
            Rigidbody2D rb = collision.GetComponent<Rigidbody2D>();

            // 只有非无敌状态才处理逻辑，防止音效和伤害在无敌帧内重复触发
            if (stats != null && !stats.GetInvincible())
            {
                // 1. 执行扣血
                stats.TakeDamage(damage);

                // 2. 锁定玩家操作（防止按键抵消弹开力）
                if (controller != null) controller.ApplyKnockbackLock();

            
                AudioSource playerAudio = collision.GetComponent<AudioSource>();
                if (playerAudio != null && hitSound != null)
                {
                    // 设置随机音调 (0.9 到 1.1 之间)，让声音不单调
                    playerAudio.pitch = Random.Range(0.9f, 1.1f);
                    playerAudio.PlayOneShot(hitSound);
                }

                // 4. 计算弹开方向并执行
                if (rb != null)
                {
                    Vector2 dir = (collision.transform.position - transform.position).normalized;
                    if (dir.y < 0.5f) dir.y = 0.6f; // 强制给一个斜向上的力，防止贴地滑行

                    rb.velocity = Vector2.zero; // 先清空当前速度
                    rb.AddForce(dir * knockbackForce, ForceMode2D.Impulse);
                }

                // 5. 震屏反馈
                PlayerCombat combat = collision.GetComponent<PlayerCombat>();
                if (combat != null)
                {
                    combat.TriggerFeedback(0.1f, true);
                }
            }
        }
    }
}