using UnityEngine;
using UnityEngine.U2D;
using System.Collections.Generic;

[RequireComponent(typeof(SpriteShapeController))]
public class InteractiveWater : MonoBehaviour
{
    // 内部类：代表水面上的一个弹簧点
    [System.Serializable]
    public class WaterSpring
    {
        public float velocity;
        public float force;
        public float height;
        public float targetHeight;

        public void UpdateSpring(float stiffness, float dampening)
        {
            float x = height - targetHeight;
            float loss = -dampening * velocity;
            force = -stiffness * x + loss;
            velocity += force;
            height += velocity;
        }
    }

    [Header("=== 弹簧物理设置 ===")]
    public float stiffness = 0.1f;   // 弹性系数
    public float dampening = 0.03f;  // 阻尼
    public float spread = 0.05f;     // 波浪扩散速度

    [Header("=== 交互设置 ===")]
    public float splashVelocityMult = 0.1f; // 玩家入水时的波浪大小

    private SpriteShapeController shapeController;
    private Spline spline;
    private List<WaterSpring> springs = new List<WaterSpring>();

    // 记录哪些索引的点是水面（顶部）
    private List<int> waterSurfaceIndices = new List<int>();

    void Start()
    {
        shapeController = GetComponent<SpriteShapeController>();
        spline = shapeController.spline;
        SetupSprings();
    }

    void SetupSprings()
    {
        springs.Clear();
        waterSurfaceIndices.Clear();

        // 逻辑：遍历样条线上的所有点
        // 假设 Y 轴坐标大于水池中心点的都算作水面点
        for (int i = 0; i < spline.GetPointCount(); i++)
        {
            float yPos = spline.GetPosition(i).y;
            // 如果该点在水池的中部以上，则赋予弹簧逻辑
            if (yPos > 0)
            {
                WaterSpring spring = new WaterSpring();
                spring.targetHeight = yPos;
                spring.height = yPos;
                springs.Add(spring);
                waterSurfaceIndices.Add(i);
            }
        }
    }

    void FixedUpdate()
    {
        // 1. 更新所有弹簧的自身震动
        for (int i = 0; i < springs.Count; i++)
        {
            springs[i].UpdateSpring(stiffness, dampening);
        }

        // 2. 波浪传播：扩散到邻居
        float[] leftDeltas = new float[springs.Count];
        float[] rightDeltas = new float[springs.Count];

        for (int j = 0; j < 8; j++) // 多次迭代让传播更远
        {
            for (int i = 0; i < springs.Count; i++)
            {
                if (i > 0)
                {
                    leftDeltas[i] = spread * (springs[i].height - springs[i - 1].height);
                    springs[i - 1].velocity += leftDeltas[i];
                }
                if (i < springs.Count - 1)
                {
                    rightDeltas[i] = spread * (springs[i].height - springs[i + 1].height);
                    springs[i + 1].velocity += rightDeltas[i];
                }
            }
        }

        // 3. 将物理数据应用回 SpriteShape
        for (int i = 0; i < waterSurfaceIndices.Count; i++)
        {
            int splineIdx = waterSurfaceIndices[i];
            Vector3 pos = spline.GetPosition(splineIdx);
            pos.y = springs[i].height;
            spline.SetPosition(splineIdx, pos);
        }

        shapeController.RefreshSpriteShape();
    }

   
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            Rigidbody2D rb = other.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                // 找出离玩家最近的一个水面点
                float minPlayerDist = float.MaxValue;
                int closestIdx = -1;

                for (int i = 0; i < springs.Count; i++)
                {
                    float dist = Mathf.Abs(other.transform.position.x - (transform.position.x + spline.GetPosition(waterSurfaceIndices[i]).x));
                    if (dist < minPlayerDist)
                    {
                        minPlayerDist = dist;
                        closestIdx = i;
                    }
                }

                if (closestIdx != -1)
                {
                    // 根据入水速度向下压弹簧
                    springs[closestIdx].velocity = rb.velocity.y * splashVelocityMult;
                }
            }
        }
    }
}