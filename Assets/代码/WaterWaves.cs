using UnityEngine;
using UnityEngine.U2D; // 必须引用 2D 命名空间

public class WaterWaves : MonoBehaviour
{
    private SpriteShapeController shapeController;
    private Spline spline;

    [Header("=== 波动设置 ===")]
    public float waveSpeed = 2f;    // 波动速度
    public float waveHeight = 0.2f; // 波动幅度
    public float waveLength = 0.5f; // 波长（点与点之间的差异）

    // 存储这些点的初始高度
    private float[] baseHeights;

    void Start()
    {
        shapeController = GetComponent<SpriteShapeController>();
        spline = shapeController.spline;

        // 记录所有点的初始 Y 坐标
        baseHeights = new float[spline.GetPointCount()];
        for (int i = 0; i < spline.GetPointCount(); i++)
        {
            baseHeights[i] = spline.GetPosition(i).y;
        }
    }

    void Update()
    {
        // 遍历所有的点（假设前几个点是水面，如果你只有水面动，需要筛选索引）
        for (int i = 0; i < spline.GetPointCount(); i++)
        {
            
            // 我们通过 i * waveLength 让每个点起伏的时间错开，形成“流水”感
            float offset = Mathf.Sin(Time.time * waveSpeed + i * waveLength) * waveHeight;

            Vector3 pos = spline.GetPosition(i);
            pos.y = baseHeights[i] + offset;

            spline.SetPosition(i, pos);
        }

       
        shapeController.RefreshSpriteShape();
    }
}