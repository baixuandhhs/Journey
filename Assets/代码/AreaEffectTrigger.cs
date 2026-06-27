using UnityEngine;
using System.Collections;

public class AreaEffectTrigger : MonoBehaviour
{
    [Header("=== 引用设置 ===")]
    public GameObject rainParticles; // 下雨粒子系统物体
    public AudioSource rainAudio;    // 环境音源物体

    [Header("=== 效果设置 ===")]
    public float fadeDuration = 1.5f;   // 淡入淡出时长

    private float maxVolume;            // 记录初始音量
    private AudioClip rainClip;         // 备份雨声资源
    private Coroutine currentFadeCoroutine;
    private bool isPlayerInside = false; // 玩家是否在区域内

    void Awake()
    {
        if (rainAudio != null)
        {
            maxVolume = rainAudio.volume;
            rainClip = rainAudio.clip; // 备份磁带

            // 初始化状态：静音并清空磁带
            rainAudio.volume = 0;
            rainAudio.Stop();
            rainAudio.clip = null;
            rainAudio.playOnAwake = false;
        }

        if (rainParticles) rainParticles.SetActive(false);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("<color=cyan>进入区域：启动雨水效果</color>");
            isPlayerInside = true;
            if (rainParticles) rainParticles.SetActive(true);

            // 重新装载磁带并淡入
            if (rainAudio != null) rainAudio.clip = rainClip;
            StartFade(maxVolume);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("<color=yellow>离开区域：停止雨水效果</color>");
            isPlayerInside = false;
            if (rainParticles) rainParticles.SetActive(false);

            // 淡出并卸载磁带
            StartFade(0f);
        }
    }

    void StartFade(float targetVol)
    {
        if (!gameObject.activeInHierarchy) return;

        if (rainAudio == null) return;
        if (currentFadeCoroutine != null) StopCoroutine(currentFadeCoroutine);
        currentFadeCoroutine = StartCoroutine(FadeAudioRoutine(targetVol));
    }

    private IEnumerator FadeAudioRoutine(float targetVol)
    {
        // 如果是淡入，确保开始播放
        if (targetVol > 0 && !rainAudio.isPlaying && isPlayerInside)
        {
            rainAudio.Play();
        }

        float startVol = rainAudio.volume;
        float timer = 0;

        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            rainAudio.volume = Mathf.Lerp(startVol, targetVol, timer / fadeDuration);
            yield return null;
        }

        rainAudio.volume = targetVol;

        // 如果完全淡出
        if (!isPlayerInside || targetVol <= 0.05f)
        {
            rainAudio.volume = 0;
            rainAudio.Stop();
            rainAudio.clip = null; 
        }

        currentFadeCoroutine = null;
    }
}