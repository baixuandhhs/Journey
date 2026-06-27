using UnityEngine;

public class LeafElevator : MonoBehaviour
{
    [Header("=== 设置 ===")]
    public Transform targetPoint;      // 电梯到达的终点
    public float moveSpeed = 3f;       // 移动速度
    public AttributeType requiredAttribute = AttributeType.Green; // 必须是绿色

    [Header("=== 视觉反馈 ===")]
    public Color inactiveColor = Color.gray; // 未激活时的颜色（可选）
    private Color activeColor = Color.white;

    private Vector3 startPos;
    private bool isPlayerOn = false;
    private SpriteRenderer sr;

    void Start()
    {
        startPos = transform.position;
        sr = GetComponent<SpriteRenderer>();
        activeColor = sr.color;

        // 初始状态：如果不是绿色，可以稍微调暗一点
        sr.color = inactiveColor;
    }

    void Update()
    {
        // 目标点选择：玩家站上来且属性正确则去终点，否则回原点
        Vector3 destination = isPlayerOn ? targetPoint.position : startPos;

        // 执行平滑位移
        transform.position = Vector3.MoveTowards(transform.position, destination, moveSpeed * Time.deltaTime);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            PlayerController pc = collision.gameObject.GetComponent<PlayerController>();


            if (pc != null && pc.currentAttribute == requiredAttribute)
            {
                // 属性正确：激活电梯
                collision.transform.SetParent(transform); // 让玩家跟着叶子走
                isPlayerOn = true;
                sr.color = activeColor; // 恢复亮色
                Debug.Log("<color=green>属性匹配：叶子电梯启动！</color>");
            }
            else
            {
                Debug.Log("<color=red>属性不匹配：需要绿色属性</color>");
            }
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            // 玩家离开
            collision.transform.SetParent(null);
            isPlayerOn = false;
            sr.color = inactiveColor; // 变回未激活颜色
        }
    }
}