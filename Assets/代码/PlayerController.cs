using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("=== 移动与动画 ===")]
    public float moveSpeed = 6f;
    public float jumpForce = 7f;
    public Animator animator;

    [Header("=== 地面/墙壁检测 ===")]
    public Transform groundCheck;
    public Transform wallCheck;
    public LayerMask groundLayer;
    public LayerMask wallLayer;
    public float wallCheckDistance = 0.6f;
    public float rayDistance = 0.2f; [Header("=== 物理数值 ===")]
    public float wallSlidingSpeed = 2f;
    public Vector2 wallJumpForce = new Vector2(5f, 10f); [Header("=== 状态属性 ===")]
    public WeaponMode currentMode = WeaponMode.Ranged;
    public AttributeType currentAttribute = AttributeType.None;
    public int maxExtraJumps = 1;
    private int extraJumps;
    private bool isGrounded;
    private bool isWallSliding;
    private float wallDirForJump;

    private float wallIgnoreTimer;
    private float knockbackTimer; [Header("=== 特效与视觉 ===")]
    public AttributeBlock attributeBlock;
    public ParticleSystem jumpParticles;
    public ParticleSystem runParticles;

    private Rigidbody2D rb;
    private PlayerCombat playerCombat;
    private float horizontalInput;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        playerCombat = GetComponent<PlayerCombat>();
        if (animator == null) animator = GetComponent<Animator>();

        extraJumps = maxExtraJumps;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
    }

    void Update()
    {
     
        if (Time.timeScale <= 0.01f) return;

        horizontalInput = Input.GetAxisRaw("Horizontal");

        if (wallIgnoreTimer > 0) wallIgnoreTimer -= Time.deltaTime;
        if (knockbackTimer > 0) knockbackTimer -= Time.deltaTime;

        CheckSurroundings();
        HandleWallSliding();
        HandleAnimations();
        HandleDirection();
        HandleRunParticles();

        if (Input.GetButtonDown("Jump"))
        {
            if (isGrounded) ExecuteJump();
            else if (isWallSliding) ExecuteWallJump();
            else if (extraJumps > 0) { extraJumps--; ExecuteJump(); }
        }

        if (Input.GetKeyDown(KeyCode.X) && isWallSliding) DropFromWall();

        if (Input.GetKey(KeyCode.R) && Input.GetKeyDown(KeyCode.S)) GetAttributeFromBelow();
        if (Input.GetKeyUp(KeyCode.R) && !Input.GetKey(KeyCode.S)) ToggleWeaponMode();
    }

    void FixedUpdate()
    {
        
        if (Time.timeScale <= 0.01f)
        {
            rb.velocity = Vector2.zero;
            return;
        }

        if (knockbackTimer > 0) return;
        if (playerCombat != null && playerCombat.GetIsThrusting()) return;

        if (!isWallSliding)
        {
            Vector2 moveDir = new Vector2(horizontalInput, 0);
            RaycastHit2D slopeHit = Physics2D.Raycast(transform.position, Vector2.down, 1.2f, groundLayer);
            if (slopeHit.collider != null)
            {
                Vector2 slopeNormal = slopeHit.normal;
                Vector2 perpendicularDir = Vector2.Perpendicular(slopeNormal).normalized;
                if (slopeNormal != Vector2.up) moveDir = perpendicularDir * -horizontalInput;
            }

            float targetY = (isGrounded && rb.velocity.y <= 0.1f) ? (moveDir.y * moveSpeed) : rb.velocity.y;
            rb.velocity = new Vector2(moveDir.x * moveSpeed, targetY);
        }
    }

    private void HandleRunParticles()
    {
        if (runParticles == null) return;

        bool isActuallyRunning = isGrounded && horizontalInput != 0 && Mathf.Abs(rb.velocity.y) < 0.1f && !isWallSliding;

        if (isActuallyRunning)
        {
            if (!runParticles.isPlaying) runParticles.Play();
        }
        else
        {
            if (runParticles.isPlaying) runParticles.Stop();
        }
    }

    private void DropFromWall()
    {
        isWallSliding = false;
        wallIgnoreTimer = 0.5f;
        rb.velocity = new Vector2(rb.velocity.x, 0);
    }

    public void ApplyKnockbackLock() { knockbackTimer = 0.4f; }

    private void CheckSurroundings()
    {
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, 0.15f, groundLayer);
        if (isGrounded) { extraJumps = maxExtraJumps; wallIgnoreTimer = 0; }

        bool wallRight = Physics2D.Raycast(wallCheck.position, Vector2.right, wallCheckDistance, wallLayer);
        bool wallLeft = Physics2D.Raycast(wallCheck.position, Vector2.left, wallCheckDistance, wallLayer);

        isWallSliding = (wallRight || wallLeft) && !isGrounded && rb.velocity.y < 0 && wallIgnoreTimer <= 0;

        if (wallRight) wallDirForJump = -1;
        else if (wallLeft) wallDirForJump = 1;
    }

    private void HandleWallSliding()
    {
        if (isWallSliding)
        {
            rb.velocity = new Vector2(rb.velocity.x, Mathf.Clamp(rb.velocity.y, -wallSlidingSpeed, float.MaxValue));
            float faceDir = (wallDirForJump == -1) ? 1f : -1f;
            transform.localScale = new Vector3(faceDir, 1, 1);
        }
    }

    private void HandleDirection()
    {
        if (isWallSliding || (playerCombat != null && playerCombat.GetIsThrusting()) || knockbackTimer > 0) return;
        if (horizontalInput > 0) transform.localScale = new Vector3(1, 1, 1);
        else if (horizontalInput < 0) transform.localScale = new Vector3(-1, 1, 1);
    }

    private void ExecuteJump()
    {
        rb.velocity = new Vector2(rb.velocity.x, 0f);
        rb.velocity = new Vector2(rb.velocity.x, jumpForce);
        if (jumpParticles) jumpParticles.Play();
        isGrounded = false;
        if (runParticles != null) runParticles.Stop();
    }

    private void ExecuteWallJump()
    {
        wallIgnoreTimer = 0.15f;
        rb.velocity = new Vector2(wallJumpForce.x * wallDirForJump, wallJumpForce.y);
        transform.localScale = new Vector3(wallDirForJump, 1, 1);
        if (jumpParticles) jumpParticles.Play();
        isWallSliding = false;
    }

    void ToggleWeaponMode()
    {
        currentMode = (currentMode == WeaponMode.Melee) ? WeaponMode.Ranged : WeaponMode.Melee;
        if (animator != null)
        {
            int staffIdx = animator.GetLayerIndex("StaffLayer");
            if (staffIdx != -1) animator.SetLayerWeight(staffIdx, currentMode == WeaponMode.Melee ? 1f : 0f);
        }
    }

    void GetAttributeFromBelow()
    {
        RaycastHit2D hit = Physics2D.CircleCast(groundCheck.position, 0.2f, Vector2.down, 0.1f, groundLayer);
        if (hit.collider != null)
        {
            GroundColor ground = hit.collider.GetComponentInParent<GroundColor>();
            if (ground != null)
            {
                currentAttribute = ConvertToEnum(ground.ys);
                if (attributeBlock != null) attributeBlock.SetColor(ground.groundColor);
            }
        }
    }

    AttributeType ConvertToEnum(string ys)
    {
        string input = ys.ToLower();
        if (input.Contains("红") || input.Contains("red")) return AttributeType.Red;
        if (input.Contains("蓝") || input.Contains("blue")) return AttributeType.Blue;
        if (input.Contains("绿") || input.Contains("green")) return AttributeType.Green;
        return AttributeType.None;
    }

    private void HandleAnimations()
    {
        if (animator == null) return;
        animator.SetBool("IsRunning", horizontalInput != 0);
        animator.SetBool("IsGrounded", isGrounded);
        animator.SetBool("IsWallSliding", isWallSliding);
        animator.SetFloat("yVelocity", rb.velocity.y);
    }

    public bool GetIsGrounded() => isGrounded;

    void OnDrawGizmosSelected()
    {
        if (groundCheck != null) { Gizmos.color = Color.yellow; Gizmos.DrawWireSphere(groundCheck.position, 0.15f); }
        if (wallCheck != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(wallCheck.position, wallCheck.position + Vector3.right * wallCheckDistance);
            Gizmos.DrawLine(wallCheck.position, wallCheck.position + Vector3.left * wallCheckDistance);
        }
    }
}