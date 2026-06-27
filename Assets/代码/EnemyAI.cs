using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyAI : MonoBehaviour
{
    [Header("=== 移动设置 ===")]
    public float moveSpeed = 2f;
    private bool movingRight = true;

    [Header("=== 检测设置 ===")]
    public Transform wallCheck;
    public Transform ledgeCheck;
    public float checkRadius = 0.15f;

    [Header("=== 图层设置 ===")]
    public LayerMask groundLayer;   // 仅用于地面检测
    public LayerMask obstacleLayer; // 用于墙壁+同伴检测（需勾选 Ground 和 Enemies）

    private Rigidbody2D rb;
    private Collider2D myCollider; // 用于排除自身检测

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        myCollider = GetComponent<Collider2D>();

        // 自动初始化朝向
        if (transform.localScale.x < 0) movingRight = false;
    }

    void FixedUpdate()
    {
        // 1. 基本移动
        rb.velocity = new Vector2(movingRight ? moveSpeed : -moveSpeed, rb.velocity.y);

        // 2. 环境检测
        // 检测前方是否有墙或者同伴
        Collider2D wallOrAlly = Physics2D.OverlapCircle(wallCheck.position, checkRadius, obstacleLayer);

        // 核心优化：检测到的碰撞体必须存在，且不是自己
        bool isBlocked = wallOrAlly != null && wallOrAlly != myCollider;

        // 检测前方是否是悬崖（没踩到地）
        bool isLedge = !Physics2D.OverlapCircle(ledgeCheck.position, checkRadius, groundLayer);

        // 3. 掉头逻辑
        if (isBlocked || isLedge)
        {
            Flip();
        }
    }

    void Flip()
    {
        movingRight = !movingRight;
        // 翻转缩放来实现转向
        Vector3 scaler = transform.localScale;
        scaler.x = movingRight ? Mathf.Abs(scaler.x) : -Mathf.Abs(scaler.x);
        transform.localScale = scaler;
    }

    // 在编辑器里画出检测范围
    void OnDrawGizmosSelected()
    {
        if (wallCheck != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(wallCheck.position, checkRadius);
        }
        if (ledgeCheck != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(ledgeCheck.position, checkRadius);
        }
    }
}