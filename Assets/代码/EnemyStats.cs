using UnityEngine;
using System.Collections;

public class EnemyStats : MonoBehaviour
{
    [Header("=== 基础属性 ===")]
    public AttributeType enemyAttribute;
    public float maxHealth = 100f;
    public float currentHealth;
    public float knockbackForce = 5f;

    [Header("=== 特效引用 ===")]
    public GameObject hitParticlePrefab; // 拖入刚才做的 HitEffect_FX
    public SpriteRenderer spriteRenderer;

    private Color originalColor;
    private Rigidbody2D rb;
    private bool isDead = false;
    public AudioClip hitSound;
    void Start()
    {
        currentHealth = maxHealth;
        rb = GetComponent<Rigidbody2D>();
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();

        // 记录初始颜色
        originalColor = spriteRenderer.color;
    }

    public void TakeDamage(float damage, AttributeType playerAttr)
    {
        if (isDead) return;
        currentHealth -= damage;

        // 1. 生成并染色受击粒子
        SpawnHitParticle(playerAttr);

        // 2. 触发闪白和缩放
        StopAllCoroutines();
        StartCoroutine(HitFeedbackRoutine());

        // 3. 击退逻辑
        ApplyKnockback();

        if (currentHealth <= 0) Die();
        AudioSource audio = GetComponent<AudioSource>();
        if (audio != null && hitSound != null)
        {
            // ✅ 增加随机音调
            audio.pitch = Random.Range(0.85f, 1.15f); // 怪物受击可以幅度大一点，更有打击感
            audio.PlayOneShot(hitSound);
        }
        //播放打中怪物的声音（比如：肉体撞击声）
      
        if (GetComponent<AudioSource>())
            GetComponent<AudioSource>().PlayOneShot(hitSound);
    }

    void SpawnHitParticle(AttributeType playerAttr)
    {
        if (hitParticlePrefab == null) return;

        // 在怪物位置生成粒子
        GameObject fx = Instantiate(hitParticlePrefab, transform.position, Quaternion.identity);

        // 让粒子的喷射方向背向玩家
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            Vector2 dir = (transform.position - player.transform.position).normalized;
            fx.transform.right = dir; // 让粒子的“右方向”指向击退方向
        }

        // 根据玩家的属性给粒子染色
        var main = fx.GetComponent<ParticleSystem>().main;
        main.startColor = GetAttributeColor(playerAttr);
    }

    IEnumerator HitFeedbackRoutine()
    {
        // 缩放：受击时微微压扁
        Vector3 originalScale = transform.localScale;
        transform.localScale = new Vector3(originalScale.x * 1.2f, originalScale.y * 0.8f, originalScale.z);

        // 闪白
        spriteRenderer.color = Color.white;

        yield return new WaitForSeconds(0.1f);

        // 恢复
        spriteRenderer.color = originalColor;
        transform.localScale = originalScale;
    }

    // 辅助工具：获取属性对应的颜色
    Color GetAttributeColor(AttributeType attr)
    {
        switch (attr)
        {
            case AttributeType.Red: return Color.red;
            case AttributeType.Blue: return Color.cyan;
            case AttributeType.Green: return Color.green;
            default: return Color.white;
        }
    }

    // ... 击退 ApplyKnockback 和死亡 Die 逻辑保持不变 ...
    void ApplyKnockback()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            Vector2 dir = (transform.position - player.transform.position).normalized;
            rb.velocity = Vector2.zero;
            rb.AddForce(dir * knockbackForce, ForceMode2D.Impulse);
        }
    }

    void Die()
    {
        isDead = true;
        // 可以在这里生成一个更大的爆炸
        Destroy(gameObject);
    }
}