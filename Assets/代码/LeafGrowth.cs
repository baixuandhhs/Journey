using UnityEngine;

public class LeafGrowth : MonoBehaviour
{
    [Header("=== 引用设置 ===")]
    public Transform targetPoint;    // 终点位置
    public Transform rootPoint;      // 起点位置（LeafSystem）
    public Transform vineStem;       // 需要被拉伸的藤蔓图片

    [Header("=== 生长设置 ===")]
    public float growSpeed = 3f;     // 生长速度
    public bool returnBack = true;   // 角色离开后是否缩回

    private Vector3 startPos;
    private bool isPlayerOn = false;
    private float initialStemScaleY;

    void Start()
    {
        startPos = transform.position;
        if (vineStem != null) initialStemScaleY = vineStem.localScale.y;
    }

    void Update()
    {
        if (isPlayerOn)
        {
            // 向目标点生长
            MoveAndStretch(targetPoint.position);
        }
        else if (returnBack)
        {
            // 缩回原点
            MoveAndStretch(startPos);
        }
    }

    void MoveAndStretch(Vector3 goal)
    {
        // 1. 移动叶子头部
        transform.position = Vector3.MoveTowards(transform.position, goal, growSpeed * Time.deltaTime);

        // 计算起点到当前头部的距离
        if (vineStem != null && rootPoint != null)
        {
            float currentDist = Vector3.Distance(rootPoint.position, transform.position);
            // 动态修改藤蔓的缩放，使其填满间隙
            // 注意：这里假设你的藤蔓图片是上下拉伸，所以改 localScale.y
            vineStem.localScale = new Vector3(vineStem.localScale.x, currentDist, vineStem.localScale.z);

            // 旋转藤蔓指向头部
            Vector3 dir = transform.position - rootPoint.position;
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            vineStem.rotation = Quaternion.Euler(0, 0, angle - 90); // -90取决于你图片的方向
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            // 重点：让玩家成为子物体，由于 LeafHead 本身不缩放（只位移），
            // 玩家不会被拉长
            collision.transform.SetParent(transform);
            isPlayerOn = true;
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            collision.transform.SetParent(null);
            isPlayerOn = false;
        }
    }
}