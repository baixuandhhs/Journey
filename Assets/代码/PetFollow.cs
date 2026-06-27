using UnityEngine;

public class PetFollow : MonoBehaviour
{
    [Header("=== 目标设置 ===")]
    public Transform target;            // 拖入 Player 物体
    public Vector3 offset = new Vector3(-1.2f, 1.3f, 0f); // 停留在主角侧上方的距离

    [Header("=== 跟随平滑度 ===")]
    public float smoothTime = 0.25f;    // 跟随延迟时间，数值越大越有“肉感”

    [Header("=== 悬浮呼吸设置 ===")]
    public float bobSpeed = 2.0f;       // 浮动速度
    public float bobAmount = 0.15f;     // 浮动幅度（上下距离）

    private Vector3 currentVelocity;    // SmoothDamp 内部使用的速度变量
    private SpriteRenderer sr;
    private AttributeBlock attrBlock;   // 引用你原有的属性脚本

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        attrBlock = GetComponent<AttributeBlock>();

        // 如果没指定目标，自动寻找 Player 标签物体
        if (target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) target = player.transform;
        }

        // 初始位置直接同步一次，防止开局从原点飞过来
        if (target != null)
        {
            transform.position = target.position + offset;
        }
    }

    // 使用 LateUpdate 确保猫头鹰在玩家移动完成后再更新位置，防止画面抖动
    void LateUpdate()
    {
        if (target == null) return;

        // 1. 处理镜像跟随：根据玩家 scale.x 自动决定猫头鹰停在左边还是右边
        float playerFacing = target.localScale.x;
        Vector3 targetOffset = new Vector3(offset.x * playerFacing, offset.y, offset.z);
        Vector3 destination = target.position + targetOffset;

        // 2. 计算悬浮呼吸位移 (Sine波)
        float verticalBob = Mathf.Sin(Time.time * bobSpeed) * bobAmount;
        destination.y += verticalBob;

        // 3. 执行平滑移动
        // SmoothDamp 模拟了带阻尼的物理跟随，效果比 Lerp 更像生物
        transform.position = Vector3.SmoothDamp(transform.position, destination, ref currentVelocity, smoothTime);

        // 4. 让猫头鹰贴图随玩家朝向翻转
        // 我们翻转 Sprite 而不是 Scale，这样不会影响子物体（比如灯光或粒子）
        if (playerFacing > 0) sr.flipX = false;
        else if (playerFacing < 0) sr.flipX = true;
    }

    // 如果你希望在吸取属性时猫头鹰有视觉反馈，可以由 PlayerController 调用此方法
    public void PlayPickupEffect()
    {
        StopAllCoroutines();
        StartCoroutine(PickupRoutine());
    }

    private System.Collections.IEnumerator PickupRoutine()
    {
        // 瞬间放大一下，产生“吃到了能量”的视觉感
        transform.localScale = Vector3.one * 1.4f;
        yield return new WaitForSeconds(0.1f);
        transform.localScale = Vector3.one;
    }


    
}