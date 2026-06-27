using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 法师投射物——普攻法球与大法球。
/// 视觉：光环脉动 + 自旋 + 拖尾 + 命中爆发。
/// </summary>
public class MagicBall : MonoBehaviour
{
    /// <summary>法球命中事件（用于屏幕震动等外部反馈）</summary>
    public static System.Action<Vector3> OnBallImpact;
    [Header("=== 基础设置 ===")]
    public bool isBigBall = false;
    public float speed = 12f;
    public float lifeTime = 3f;
    public float explosionRadius = 2.5f;

    [Header("=== 视觉组件 ===")]
    public SpriteRenderer spriteRenderer;

    [Header("=== 脉冲动画 ===")]
    [Range(0.5f, 1.5f)]
    public float pulseAmount = 0.12f;
    public float pulseSpeed = 8f;

    [Header("=== 旋转 ===")]
    public float spinSpeed = 360f;

    [Header("=== 命中粒子 ===")]
    public Sprite impactParticleSprite;
    public int impactParticleCount = 8;
    public float impactParticleSpeed = 3f;
    public float impactParticleLife = 0.5f;
    public Color impactColor = Color.white;

    // 内部状态
    private PlayerStats playerStats;
    private AttributeType ballAttribute;
    private float baseDamage;
    private List<EnemyStats> hitList = new List<EnemyStats>();
    private Vector3 baseScale;
    private GameObject glowChild;
    private List<GameObject> orbitParticles = new List<GameObject>();
    private TrailRenderer trailRenderer;

    void Awake()
    {
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb) rb.gravityScale = 0;
        trailRenderer = GetComponent<TrailRenderer>();
        Destroy(gameObject, lifeTime);
    }

    public void Init(AttributeType attr, float damage, Color themeColor, PlayerStats stats, float moveDir)
    {
        ballAttribute = attr;
        baseDamage = damage;
        playerStats = stats;

        // 主球体颜色
        if (spriteRenderer) spriteRenderer.color = themeColor;
        baseScale = transform.localScale;

        // 镜像方向
        transform.localScale = new Vector3(
            moveDir * Mathf.Abs(transform.localScale.x),
            transform.localScale.y,
            transform.localScale.z
        );

        // 拖尾
        SetupTrail(themeColor);

        // 光环
        CreateGlowChild(themeColor);

        // 环绕粒子
        if (!isBigBall)
            CreateOrbitParticles(themeColor, 2);
        else
            CreateOrbitParticles(themeColor, 4);

        // 启动脉冲和旋转
        StartCoroutine(PulseAnimation());

        // 发射
        GetComponent<Rigidbody2D>().velocity = new Vector2(moveDir * speed, 0);
    }

    void Update()
    {
        // 持续自旋
        if (spriteRenderer != null)
            spriteRenderer.transform.Rotate(0, 0, spinSpeed * Time.deltaTime);
    }

    // ==================== 视觉辅助 ====================

    /// <summary>配置拖尾效果</summary>
    private void SetupTrail(Color themeColor)
    {
        TrailRenderer tr = GetComponent<TrailRenderer>();
        if (tr == null) return;

        tr.startColor = themeColor;
        // 尾端渐隐到完全透明
        tr.endColor = new Color(themeColor.r, themeColor.g, themeColor.b, 0f);
        tr.Clear();

        // 拖尾宽度曲线：起始宽，末端窄
        AnimationCurve widthCurve = new AnimationCurve();
        widthCurve.AddKey(0f, isBigBall ? 0.6f : 0.35f);
        widthCurve.AddKey(1f, 0f);
        tr.widthCurve = widthCurve;
        tr.widthMultiplier = 1f;
    }

    /// <summary>创建光环子对象（半透明大球）</summary>
    private void CreateGlowChild(Color themeColor)
    {
        glowChild = new GameObject("Glow", typeof(SpriteRenderer));
        glowChild.transform.SetParent(transform, false);
        glowChild.transform.localPosition = Vector3.zero;
        glowChild.transform.localScale = Vector3.one * (isBigBall ? 2.0f : 1.6f);

        var glowSr = glowChild.GetComponent<SpriteRenderer>();
        glowSr.sprite = spriteRenderer != null ? spriteRenderer.sprite : null;
        glowSr.color = new Color(themeColor.r, themeColor.g, themeColor.b, 0.25f);
        glowSr.sortingOrder = spriteRenderer != null ? spriteRenderer.sortingOrder - 1 : -1;
    }

    /// <summary>创建环绕粒子（在球体外围旋转的小光点）</summary>
    private void CreateOrbitParticles(Color themeColor, int count)
    {
        for (int i = 0; i < count; i++)
        {
            var orbit = new GameObject($"Orbit_{i}", typeof(SpriteRenderer));
            orbit.transform.SetParent(transform, false);

            var sr = orbit.GetComponent<SpriteRenderer>();
            sr.sprite = spriteRenderer != null ? spriteRenderer.sprite : null;
            sr.color = new Color(themeColor.r, themeColor.g, themeColor.b, 0.6f);
            sr.sortingOrder = 10;
            orbit.transform.localScale = Vector3.one * 0.25f;

            orbitParticles.Add(orbit);
        }
    }

    /// <summary>脉冲缩放动画</summary>
    private IEnumerator PulseAnimation()
    {
        float timer = 0f;
        while (true)
        {
            timer += Time.deltaTime * pulseSpeed;
            float scale = 1f + Mathf.Sin(timer) * pulseAmount;

            if (spriteRenderer != null)
                spriteRenderer.transform.localScale = baseScale * scale;

            if (glowChild != null)
            {
                float glowBase = isBigBall ? 2.0f : 1.6f;
                glowChild.transform.localScale = Vector3.one * glowBase * (1f + Mathf.Sin(timer) * pulseAmount * 1.5f);
            }

            // 环绕粒子旋转
            float orbitRadius = isBigBall ? 0.8f : 0.5f;
            for (int i = 0; i < orbitParticles.Count; i++)
            {
                float angle = timer * 3f + (i * Mathf.PI * 2f / orbitParticles.Count);
                orbitParticles[i].transform.localPosition = new Vector2(
                    Mathf.Cos(angle) * orbitRadius,
                    Mathf.Sin(angle) * orbitRadius
                );
            }

            yield return null;
        }
    }

    /// <summary>命中时生成粒子爆发</summary>
    private void SpawnImpactBurst(Vector2 hitPoint)
    {
        for (int i = 0; i < impactParticleCount; i++)
        {
            var p = new GameObject("ImpactParticle", typeof(SpriteRenderer));
            p.transform.position = hitPoint;

            var sr = p.GetComponent<SpriteRenderer>();
            sr.sprite = impactParticleSprite != null ? impactParticleSprite :
                        (spriteRenderer != null ? spriteRenderer.sprite : null);
            sr.color = impactColor != Color.white ? impactColor :
                       (spriteRenderer != null ? spriteRenderer.color : Color.white);
            sr.sortingOrder = 15;

            // 随机方向 + 速度
            float angle = (360f / impactParticleCount) * i + Random.Range(-20f, 20f);
            Vector2 dir = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));

            // 给粒子加一个简单的飞出脚本
            StartCoroutine(FlyParticle(p, dir, impactParticleSpeed, impactParticleLife));
        }

        // 爆发时短暂闪光
        if (spriteRenderer != null)
        {
            var flash = Instantiate(glowChild, hitPoint, Quaternion.identity);
            var flashSr = flash.GetComponent<SpriteRenderer>();
            flashSr.color = Color.white;
            flashSr.sortingOrder = 20;
            flash.transform.localScale = Vector3.one * 3f;
            StartCoroutine(FadeAndDestroy(flash, 0.2f));
        }
    }

    private IEnumerator FlyParticle(GameObject particle, Vector2 dir, float speed, float lifetime)
    {
        float elapsed = 0f;
        var sr = particle.GetComponent<SpriteRenderer>();

        while (elapsed < lifetime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / lifetime;

            particle.transform.position += (Vector3)(dir * speed * Time.deltaTime);
            // 减速
            dir *= 0.96f;

            // 渐隐
            if (sr != null)
            {
                Color c = sr.color;
                c.a = 1f - t;
                sr.color = c;
            }

            // 缩小
            float s = 1f - t * 0.6f;
            particle.transform.localScale = Vector3.one * s;

            yield return null;
        }

        Destroy(particle);
    }

    private IEnumerator FadeAndDestroy(GameObject obj, float duration)
    {
        float elapsed = 0f;
        var sr = obj.GetComponent<SpriteRenderer>();
        Color start = sr.color;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            if (sr != null)
            {
                Color c = start;
                c.a = start.a * (1f - elapsed / duration);
                sr.color = c;
            }
            yield return null;
        }

        Destroy(obj);
    }

    void OnDestroy()
    {
        StopAllCoroutines();
    }

    // ==================== 碰撞逻辑 ====================

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") || collision.CompareTag("Projectile") || collision.isTrigger) return;

        if (collision.CompareTag("Enemy"))
        {
            EnemyStats enemy = collision.GetComponent<EnemyStats>();
            if (enemy != null && !hitList.Contains(enemy))
            {
                hitList.Add(enemy);
                HandleImpact(enemy);
                if (!isBigBall) Destroy(gameObject);
            }
        }

        if (collision.gameObject.layer == LayerMask.NameToLayer("Ground"))
        {
            Destroy(gameObject);
        }
    }

    void HandleImpact(EnemyStats enemy)
    {
        if (enemy == null || playerStats == null) return;

        float damageMult = (ballAttribute == AttributeType.Red) ? 2.0f : 1.0f;
        enemy.TakeDamage(baseDamage * damageMult, ballAttribute);

        // 属性增益
        if (ballAttribute == AttributeType.Blue && !isBigBall)
            playerStats.RestoreMana(10f);
        if (ballAttribute == AttributeType.Green)
            playerStats.Heal(isBigBall ? 10f : 2f);

        // 屏幕震动
        OnBallImpact?.Invoke(enemy.transform.position);

        // 命中爆发效果
        SpawnImpactBurst(enemy.transform.position);
    }
}