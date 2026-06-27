using UnityEngine;

public class IdleAfterInactivity : MonoBehaviour
{
    [Tooltip("3秒无操作后进入待机")]
    public float idleDelay = 3f;

    private Animator animator;
    private Rigidbody2D rb;
    private bool isIdle = false;
    private float lastMoveTime;

    void Start()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        // 初始为非待机状态（或根据需求设为 true）
        SetIdle(false);
    }

    void Update()
    {
        // 检测是否正在移动（可根据你的移动逻辑调整）
        bool isMoving = Mathf.Abs(rb.velocity.x) > 0.1f || Mathf.Abs(rb.velocity.y) > 0.1f;

        if (isMoving)
        {
            lastMoveTime = Time.time; // 更新最后移动时间
            if (isIdle)
                SetIdle(false); // 退出待机
        }
        else
        {
            // 如果超过 idleDelay 秒没动，进入待机
            if (!isIdle && Time.time - lastMoveTime >= idleDelay)
            {
                SetIdle(true);
            }
        }
    }

    void SetIdle(bool idle)
    {
        isIdle = idle;
        animator.SetBool("IsIdle", idle);
    }
}