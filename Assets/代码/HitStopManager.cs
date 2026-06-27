using UnityEngine;
using System.Collections;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    private Coroutine hitStopCoroutine; // 用来记录当前正在运行的顿帧协程

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // 调用这个方法触发顿帧
    public void DoHitStop(float duration)
    {
        // 1. 如果 duration 太小，直接跳过
        if (duration <= 0) return;

        // 2. 如果之前已经在顿帧，先停止旧的，防止时间线冲突
        if (hitStopCoroutine != null)
        {
            StopCoroutine(hitStopCoroutine);
        }

        // 3. 开启新的顿帧
        hitStopCoroutine = StartCoroutine(HitStopRoutine(duration));
    }

    private IEnumerator HitStopRoutine(float duration)
    {
        // 核心优化 1：不要设为绝对的 0，设为 0.01f 
        // 这样可以避免某些 Unity 物理引擎因为除以 0 而产生的逻辑死锁
        Time.timeScale = 0.01f;

        // 核心优化 2：必须使用 Realtime
        float pauseEndTime = Time.realtimeSinceStartup + duration;

        while (Time.realtimeSinceStartup < pauseEndTime)
        {
            // 只要现实时间没到，就一直等待
            yield return null;
        }

        // 4. 恢复时间
        Time.timeScale = 1f;
        hitStopCoroutine = null;
    }

    // 辅助：如果你以后想做“死亡重开”，也可以写在这里
    public void RestartLevel()
    {
        Time.timeScale = 1f; // 确保加载新场景前恢复正常速度
        UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }
}