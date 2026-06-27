using UnityEngine;
using System.Collections;
using Cinemachine;

public class PlayerCombat : MonoBehaviour
{
    [Header("=== 基础数值 ===")]
    public float playerBaseDamage = 20f;
    public LayerMask enemyLayers;

    [Header("=== 连击设置 (2段) ===")]
    public int comboStep = 0;
    public float attackCooldown = 0.3f;
    public float comboResetTime = 0.8f;
    private float nextAttackTime;
    private float lastAttackTime;
    private bool canMelee = true;

    [Header("=== F 突刺设置 ===")]
    public float thrustDistance = 10f;
    public float thrustDuration = 0.2f;
    public float thrustManaCost = 20f;
    private bool isThrusting = false;

    [Header("=== 视觉与判定引用 ===")]
    public GameObject normalMagicBall;
    public GameObject bigMagicBall;
    public Transform firePoint;
    public Transform attackPoint;
    public float meleeRange = 1.5f;

    [Header("=== 音频设置 ===")]
    public AudioSource sfxSource;
    public AudioClip meleeSound;
    public AudioClip rangedSound;
    public AudioClip bigBallSound;
    public AudioClip thrustSound;

    [Header("=== 打击反馈 ===")]
    public float normalHitStop = 0.05f;
    private CinemachineImpulseSource impulseSource;
    private PlayerStats stats;
    private PlayerController controller;
    private Rigidbody2D rb;

    void Start()
    {
        stats = GetComponent<PlayerStats>();
        controller = GetComponent<PlayerController>();
        rb = GetComponent<Rigidbody2D>();
        impulseSource = GetComponent<CinemachineImpulseSource>();

        // 法球命中屏幕震动
        MagicBall.OnBallImpact += HandleBallImpact;
    }

    void OnDestroy()
    {
        MagicBall.OnBallImpact -= HandleBallImpact;
    }

    void HandleBallImpact(Vector3 impactPos)
    {
        if (impulseSource != null)
        {
            impulseSource.transform.position = impactPos;
            impulseSource.GenerateImpulse();
        }
    }

    void Update()
    {
        if (Time.timeScale <= 0.01f || isThrusting) return;

        // 连击超时重置
        if (Time.time - lastAttackTime > comboResetTime && canMelee)
        {
            if (comboStep != 0) { comboStep = 0; if (controller.animator) controller.animator.SetInteger("ComboStep", 0); }
        }

        if (controller.currentMode == WeaponMode.Melee) HandleMeleeInput();
        else HandleRangedInput();

        CheckStomp();
    }

    void HandleMeleeInput()
    {
        // 支持长按左键连击
        if (Input.GetMouseButton(0) && Time.time >= nextAttackTime && canMelee) MeleeAttack();
        if (Input.GetKeyDown(KeyCode.F)) StartCoroutine(PerformThrust());
    }

    void MeleeAttack()
    {
        nextAttackTime = Time.time + attackCooldown;
        lastAttackTime = Time.time;

        PlaySFX(meleeSound); // 播放随机音调音效

        if (controller.animator)
        {
            controller.animator.SetInteger("ComboStep", comboStep);
            controller.animator.SetTrigger("Attack");
        }

        // 伤害计算
        float dmgMult = (comboStep == 1) ? 1.0f : 0.75f;
        if (controller.currentAttribute == AttributeType.Red) dmgMult *= 2.0f;

        Collider2D[] enemies = Physics2D.OverlapCircleAll(attackPoint.position, meleeRange, enemyLayers);
        if (enemies.Length > 0) TriggerFeedback(normalHitStop, comboStep == 1);

        foreach (Collider2D e in enemies)
        {
            EnemyStats enemy = e.GetComponent<EnemyStats>();
            if (enemy != null)
            {
                enemy.TakeDamage(playerBaseDamage * dmgMult, controller.currentAttribute);
                // 属性增益
                if (controller.currentAttribute == AttributeType.Blue) stats.RestoreMana(2f); // 战士回2蓝
                if (controller.currentAttribute == AttributeType.Green) stats.Heal(5f);       // 战士回5血
            }
        }

        if (comboStep == 0) comboStep = 1;
        else StartCoroutine(ComboEndLock());
    }

    IEnumerator PerformThrust()
    {
        if (isThrusting || !stats.ConsumeMana(thrustManaCost)) yield break;
        isThrusting = true;
        PlaySFX(thrustSound);

        float originalGravity = rb.gravityScale;
        float moveDir = Mathf.Sign(transform.localScale.x); // 根据缩放确定方向

        Physics2D.IgnoreLayerCollision(LayerMask.NameToLayer("Player"), LayerMask.NameToLayer("Enemies"), true);
        if (controller.animator) controller.animator.SetTrigger("Thrust");

        // 路径检测
        RaycastHit2D[] hits = Physics2D.BoxCastAll(transform.position, new Vector2(1, 2), 0, Vector2.right * moveDir, thrustDistance, enemyLayers);
        foreach (var hit in hits)
        {
            EnemyStats es = hit.collider.GetComponent<EnemyStats>();
            if (es != null)
            {
                float m = (controller.currentAttribute == AttributeType.Red) ? 2.0f : 1.0f;
                es.TakeDamage(playerBaseDamage * m, controller.currentAttribute);
                if (controller.currentAttribute == AttributeType.Green) stats.Heal(5f);
            }
        }

        rb.gravityScale = 0;
        rb.velocity = new Vector2(moveDir * (thrustDistance / thrustDuration), 0);
        yield return new WaitForSeconds(thrustDuration);

        Physics2D.IgnoreLayerCollision(LayerMask.NameToLayer("Player"), LayerMask.NameToLayer("Enemies"), false);
        rb.gravityScale = originalGravity;
        rb.velocity = Vector2.zero;
        isThrusting = false;
    }

    void HandleRangedInput()
    {
        if (Input.GetMouseButtonDown(0)) SpawnMagicBall(false);
        if (Input.GetKeyDown(KeyCode.F)) SpawnMagicBall(true);
    }

    private void SpawnMagicBall(bool isBig)
    {
        float mana = isBig ? 20f : 0f;
        if (!stats.ConsumeMana(mana)) return;

        PlaySFX(isBig ? bigBallSound : rangedSound);

        GameObject prefab = isBig ? bigMagicBall : normalMagicBall;
        float moveDir = Mathf.Sign(transform.localScale.x);
        // 生成时不设旋转，由MagicBall内部根据moveDir处理
        GameObject ballObj = Instantiate(prefab, firePoint.position, Quaternion.identity);

        MagicBall ballScript = ballObj.GetComponent<MagicBall>();
        if (ballScript != null)
            ballScript.Init(controller.currentAttribute, playerBaseDamage, controller.attributeBlock.GetColor(), stats, moveDir);
    }

    void CheckStomp()
    {
        if (!controller.GetIsGrounded() && rb.velocity.y < -0.5f)
        {
            RaycastHit2D hit = Physics2D.CircleCast(controller.groundCheck.position, 0.4f, Vector2.down, 0.1f, enemyLayers);
            if (hit.collider != null)
            {
                EnemyStats enemy = hit.collider.GetComponent<EnemyStats>();
                if (enemy != null)
                {
                    float m = (controller.currentAttribute == AttributeType.Red) ? 40f : 1f;
                    enemy.TakeDamage(playerBaseDamage * m, controller.currentAttribute);
                    rb.velocity = new Vector2(rb.velocity.x, 8f);
                    TriggerFeedback(0.05f, false);
                }
            }
        }
    }

    // 通用音效播放器（含随机音调）
    void PlaySFX(AudioClip clip)
    {
        if (sfxSource && clip)
        {
            sfxSource.pitch = Random.Range(0.9f, 1.1f);
            sfxSource.PlayOneShot(clip);
        }
    }

    public void TriggerFeedback(float duration, bool shake)
    {
        if (GameManager.Instance != null) GameManager.Instance.DoHitStop(duration);
        if (shake && impulseSource != null) impulseSource.GenerateImpulse();
    }

    IEnumerator ComboEndLock() { canMelee = false; yield return new WaitForSeconds(0.2f); comboStep = 0; if (controller.animator) controller.animator.SetInteger("ComboStep", 0); canMelee = true; }
    public bool GetIsThrusting() => isThrusting;
}
